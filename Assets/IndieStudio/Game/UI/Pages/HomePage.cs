using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// The room (Design/18 §1): a diorama that doubles as a status display. You sit at the desk, sleep when
    /// you rest, pizza boxes pile up with burnout, trophies appear when you ship, upgrades show up on the
    /// desk, the savings jar empties with your runway, and the laptop shows your game as it grows. Most
    /// objects are shortcuts to an app; the mug brews coffee and the cat can be petted (Design/02 §4).
    /// </summary>
    public sealed class HomePage : Page
    {
        readonly VisualElement _stage;
        readonly Label _sign;
        readonly VisualElement _posterArt;
        readonly Label _posterCredit;
        readonly Label _posterTitle;
        readonly Label _posterStamp;
        readonly Label _calendarMonth;
        readonly Label _calendarDay;
        readonly Label _calendarWeekday;
        readonly VisualElement _trophies;
        readonly Label _trophyCount;
        readonly VisualElement _jarFill;
        readonly VisualElement _chairWood;
        readonly VisualElement _chairErgo;
        readonly VisualElement _deskFounder;
        readonly Avatar _deskAvatar;
        readonly VisualElement _sleeper;
        readonly Avatar _sleepAvatar;
        readonly Label _zzz;
        readonly VisualElement _monitor;
        readonly VisualElement _tower;
        readonly VisualElement _cooling;
        readonly VisualElement _heat;
        readonly VisualElement _tablet;
        readonly GamePreview _laptopGame;
        readonly VisualElement _mugCoffee;
        readonly Steam _steam;
        readonly Cat _cat;
        readonly Label _catZz;
        readonly Label _phoneBadge;
        readonly VisualElement[] _pizza = new VisualElement[3];
        readonly VisualElement _plant;
        readonly VisualElement _bubble;
        readonly Label _bubbleText;
        readonly VisualElement _founderCard;
        readonly Avatar _cardAvatar;
        readonly Label _cardName;
        readonly Label _cardRole;
        readonly VisualElement _cardMorale;
        readonly VisualElement _cardBurnout;
        readonly Label _cardOutput;
        readonly VisualElement _mentor;
        readonly Label _tip;

        public HomePage(StudioUI studio) : base(studio)
        {
            Root.AddToClassList("home");
            var wall = Ui.El("home__wall");
            wall.style.backgroundImage = new StyleBackground(Textures.Wallpaper);
            Root.Add(wall);
            var floor = Ui.El("home__floor");
            floor.style.backgroundImage = new StyleBackground(Textures.Floorboards);
            Root.Add(floor);
            Add(Root, "home__rail");
            var stage = Ui.El("home__stage");
            Root.Add(stage);
            _stage = stage;

            // --- wall: window, poster, calendar, shelf
            var frame = Add(stage, "dio-window");
            var sky = Add(frame, "dio-window__sky");
            Add(sky, "dio-sun");
            Add(sky, "dio-moon");
            Add(sky, "dio-moon-bite");
            foreach (var star in new[] { "dio-star dio-star--1", "dio-star dio-star--2", "dio-star dio-star--3", "dio-star dio-star--4" }) Add(sky, star);
            Add(sky, "dio-city dio-city--low");
            Add(sky, "dio-city dio-city--a");
            Add(sky, "dio-city dio-city--b");
            Add(sky, "dio-lit dio-lit--1");
            Add(sky, "dio-lit dio-lit--2");
            Add(sky, "dio-lit dio-lit--3");
            Add(frame, "dio-window__bar");

            // The studio's sign hangs above the window.
            var sign = Add(stage, "dio-sign");
            var plank = Hotspot(sign, "dio-sign__plank", Studio.ShowNameStudio, () => $"{State.StudioName}. Click to rename your studio.");
            plank.name = "room-sign";
            Add(plank, "dio-sign__string dio-sign__string--left");
            Add(plank, "dio-sign__string dio-sign__string--right");
            _sign = Ui.Display("", "dio-sign__text");
            _sign.pickingMode = PickingMode.Ignore;
            plank.Add(_sign);

            var poster = Hotspot(stage, "dio-poster", () =>
                {
                    if (State.ActiveProject != null || State.IsDemo) Studio.Show<ProjectPage>();
                    else Studio.Show<IdeasPage>();
                },
                () => State.ActiveProject != null ? $"The poster for {State.ActiveProject.Title}. Click to open the project."
                    : State.IsDemo ? "Room for your first game's poster. Click to start it." : "Room for your next game's poster. Click to browse your ideas.");
            _posterArt = Add(poster, "dio-poster__art");
            Add(_posterArt, "dio-poster__sun");
            Add(_posterArt, "dio-poster__sea");
            _posterCredit = Ui.Text("", "dio-poster__credit");
            _posterCredit.pickingMode = PickingMode.Ignore;
            poster.Add(_posterCredit);
            _posterTitle = Ui.Display("", "dio-poster__title");
            poster.Add(_posterTitle);
            _posterStamp = Ui.Display("", "dio-poster__stamp");
            poster.Add(_posterStamp);
            Add(stage, "dio-pin");

            var calendar = Hotspot(stage, "dio-calendar", () => Studio.Show<DashboardPage>(), () => "Today's date. Click for how your week is going.");
            var calendarTop = Add(calendar, "dio-calendar__top");
            _calendarMonth = Ui.Display("", "dio-calendar__month");
            calendarTop.Add(_calendarMonth);
            _calendarDay = Ui.Display("", "dio-calendar__day");
            calendar.Add(_calendarDay);
            _calendarWeekday = Ui.Text("", "dio-calendar__weekday");
            calendar.Add(_calendarWeekday);

            var shelf = Hotspot(stage, "dio-shelf", () => Studio.Show<ProjectPage>(), ShelfHelp);
            var books = Add(shelf, "dio-books");
            Add(books, "dio-book dio-book--a");
            Add(books, "dio-book dio-book--b");
            Add(books, "dio-book dio-book--c");
            _trophies = Add(shelf, "dio-trophies");
            _trophyCount = Ui.Display("", "dio-trophies__count");
            shelf.Add(_trophyCount);
            Add(shelf, "dio-shelf__board");

            var jar = Hotspot(stage, "dio-jar", () => Studio.Show<FinancePage>(), () => $"Your savings jar: about {Fmt.Days(State.Finance.RunwayDays())} of runway. Click for your finances.");
            _jarFill = Add(jar, "dio-jar__fill");
            jar.Add(Ui.Display("$", "dio-jar__label"));
            Add(jar, "dio-jar__lid");

            // --- floor: rug, bed, desk
            var rug = Add(stage, "dio-rug");
            Add(rug, "dio-rug__ring");

            var bed = Hotspot(stage, "dio-bed", () =>
                {
                    if (Sim.IsNight) Studio.Sleep();
                    else Studio.Play(PlanBlock.Rest);
                },
                () => Sim.IsNight ? "Your bed. Click to sleep." : $"Your bed. Click to rest this {Fmt.SlotName(State.Slot).ToLowerInvariant()}.");
            Add(bed, "dio-bed__head");
            Add(bed, "dio-bed__foot");
            Add(bed, "dio-bed__frame");
            Add(bed, "dio-bed__mattress");
            Add(bed, "dio-bed__pillow");
            _sleeper = Add(bed, "dio-sleeper");
            _sleepAvatar = new Avatar(AvatarLook.Founder, Mood.Tired, 96f);
            _sleeper.Add(_sleepAvatar);
            Add(bed, "dio-bed__blanket");
            _zzz = Ui.Display("z z Z", "dio-zzz");
            bed.Add(_zzz);

            _chairWood = Add(stage, "dio-chair dio-chair--wood");
            Add(_chairWood, "dio-chair__slat");
            _chairErgo = Add(stage, "dio-chair dio-chair--ergo");

            _deskFounder = Hotspot(stage, "dio-founder", ToggleFounderCard, () => "That's you. Click for a quick check-in.");
            _deskAvatar = new Avatar(AvatarLook.FounderWorking, Mood.Focus, 124f);
            _deskFounder.Add(_deskAvatar);

            _tower = Add(stage, "dio-tower");
            var desk = Add(stage, "dio-desk");
            Add(desk, "dio-desk__top");
            Add(desk, "dio-desk__leg dio-desk__leg--left");
            Add(desk, "dio-desk__leg dio-desk__leg--right");
            Add(desk, "dio-desk__drawer");

            _cooling = Add(stage, "dio-cooling");
            var laptop = Hotspot(stage, "dio-laptop", () => Studio.Show<ProjectPage>(), LaptopHelp);
            var screen = Add(laptop, "dio-laptop__screen");
            Add(screen, "dio-code dio-code--1");
            Add(screen, "dio-code dio-code--2");
            Add(screen, "dio-code dio-code--3");
            Add(screen, "dio-code dio-code--4");
            _laptopGame = new GamePreview();
            _laptopGame.AddToClassList("game-preview--mini");
            _laptopGame.AddToClassList("dio-laptop__game");
            screen.Add(_laptopGame);
            Add(laptop, "dio-laptop__base");
            _heat = Ui.Display("~ ~ ~", "dio-heat");
            stage.Add(_heat);

            _monitor = Hotspot(stage, "dio-monitor", () => Studio.Show<ProjectPage>(), () => "Your gaming PC: 3D builds no longer eat your afternoons.");
            var monitorScreen = Add(_monitor, "dio-monitor__screen");
            Add(monitorScreen, "dio-hill dio-hill--a");
            Add(monitorScreen, "dio-hill dio-hill--b");
            Add(_monitor, "dio-monitor__stand");

            _tablet = Add(stage, "dio-tablet");
            var mug = Hotspot(stage, "dio-mug", BrewCoffee, MugHelp);
            mug.name = "room-mug";
            _mugCoffee = Add(mug, "dio-mug__coffee");
            Add(mug, "dio-mug__handle");
            _steam = new Steam();
            stage.Add(_steam);
            var phone = Hotspot(stage, "dio-phone", () => Studio.ShowInbox(InboxFolder.All), () => "Your phone. Click to open your mail.");
            Add(phone, "dio-phone__screen");
            _phoneBadge = Ui.Text("", "badge dio-phone__badge");
            phone.Add(_phoneBadge);

            for (int i = 0; i < _pizza.Length; i++) _pizza[i] = Add(stage, $"dio-pizza dio-pizza--{i + 1}");

            _plant = Add(stage, "dio-plant");
            Add(_plant, "dio-plant__leaf dio-plant__leaf--left");
            Add(_plant, "dio-plant__leaf dio-plant__leaf--right");
            Add(_plant, "dio-plant__pot");

            _cat = new Cat { name = "room-cat", pickingMode = PickingMode.Position };
            _cat.AddToClassList("hotspot");
            _cat.RegisterCallback<ClickEvent>(e => PetCat());
            Why(_cat, CatHelp);
            stage.Add(_cat);
            _catZz = Ui.Display("z z", "dio-cat-zz");
            _catZz.pickingMode = PickingMode.Ignore;
            stage.Add(_catZz);

            _bubble = Add(stage, "bubble");
            _bubbleText = Ui.Text("", "bubble__text");
            _bubble.Add(_bubbleText);

            // --- the founder check-in card
            _founderCard = Ui.El("card founder-card hidden");
            stage.Add(_founderCard);
            var cardHead = Ui.Row("founder-card__head");
            var cardPortrait = Ui.El("portrait bg-mustard-light founder-card__face");
            _cardAvatar = new Avatar(AvatarLook.Founder, Mood.Happy, 58f);
            cardPortrait.Add(_cardAvatar);
            cardHead.Add(cardPortrait);
            var cardName = Ui.El("grow");
            _cardName = Ui.Display("You", "founder-card__name");
            cardName.Add(_cardName);
            _cardRole = Ui.Text("", "founder-card__role");
            cardName.Add(_cardRole);
            cardHead.Add(cardName);
            cardHead.Add(Ui.IconBtn(Icons.Close, "", ToggleFounderCard, "icon-btn", 14f));
            _founderCard.Add(cardHead);
            var moraleRow = Ui.MeterRow("Morale", 0, "fill-good");
            _cardMorale = moraleRow;
            _founderCard.Add(moraleRow);
            var burnoutRow = Ui.MeterRow("Burnout", 0, "fill-warn");
            _cardBurnout = burnoutRow;
            _founderCard.Add(burnoutRow);
            _cardOutput = Ui.Text("", "founder-card__output");
            _founderCard.Add(_cardOutput);
            var cardButtons = Ui.Row("founder-card__buttons");
            var callIt = Ui.Btn("Call it a day", () => { _founderCard.AddToClassList("hidden"); Studio.CallItADay(); }, "btn btn--primary");
            Why(callIt, () => "Rest for whatever is left of today.");
            cardButtons.Add(callIt);
            var vacation = Command("Take 7 days off", () => { Sim.TakeVacation(7); _founderCard.AddToClassList("hidden"); });
            Why(vacation, () => "Starting now, every part of the next 7 days is rest (about -22 burnout). Gig deadlines keep running.");
            cardButtons.Add(vacation);
            _founderCard.Add(cardButtons);

            // --- advice in the corner (the tutorial uses the same spot)
            _mentor = Ui.Row("home__mentor");
            var tipBubble = Ui.El("home__mentor-bubble");
            tipBubble.Add(Ui.Display("Old Dev Doug", "home__mentor-name"));
            _tip = Ui.Text("", "home__mentor-tip");
            tipBubble.Add(_tip);
            _mentor.Add(tipBubble);
            _mentor.Add(Ui.Portrait(AvatarLook.Mentor, Mood.Happy, 64f, "bg-mustard-light home__mentor-face"));
            Root.Add(_mentor);
        }

        public override string Title => "Room";
        public override string IconPath => Icons.Home;
        public override string AppClass => "app-home";
        public override bool IsHome => true;

        static VisualElement Add(VisualElement parent, string classes)
        {
            var element = Ui.El(classes);
            element.pickingMode = PickingMode.Ignore;
            parent.Add(element);
            return element;
        }

        VisualElement Hotspot(VisualElement parent, string classes, System.Action onClick, System.Func<string> help)
        {
            var element = Ui.El(classes + " hotspot");
            element.RegisterCallback<ClickEvent>(e => onClick());
            Why(element, help);
            parent.Add(element);
            return element;
        }

        /// <summary>The mug: brews a coffee for the next work session (Design/02 §4).</summary>
        public void BrewCoffee()
        {
            if (Sim.DrinkCoffee(out string error))
                Studio.Notify(Icons.Coffee, "bg-mustard-light", "Fresh coffee",
                    $"Your next work session today counts ×{Tuning.CoffeeBoost:0.00}. Burnout +{Tuning.CoffeeBurnout:0}.");
            else Studio.Notify(Icons.Coffee, "bg-mustard-light", "No coffee", error);
            Studio.Game.NotifyChanged();
        }

        string MugHelp()
        {
            var s = State;
            if (s.CoffeeReady) return $"A fresh coffee. Your next work session today counts ×{Tuning.CoffeeBoost:0.00}.";
            if (Sim.IsNight) return "Your mug. Coffee this late would only ruin your sleep.";
            if (s.CoffeesToday >= Tuning.CoffeesPerDay) return "Your mug. Two coffees today already: your hands are shaking.";
            return $"Your mug. Click to brew a coffee: your next work session today counts ×{Tuning.CoffeeBoost:0.00}, for +{Tuning.CoffeeBurnout:0} burnout. Two a day at most.";
        }

        /// <summary>The cat: a little morale when you're low, once a day.</summary>
        public void PetCat()
        {
            bool first = State.CatPettedDay != State.Day;
            string text = Sim.PetCat();
            if (first) Celebrate.Hearts(_stage, new Vector2(636f, 690f));
            Studio.Notify(Icons.Heart, "bg-pink", "The cat", text);
            Studio.Game.NotifyChanged();
        }

        string CatHelp()
        {
            var s = State;
            if (s.CatPettedDay == s.Day) return "The cat. Awake now, and judging you a little.";
            return s.Founder.Morale < Tuning.CatMoraleBelow
                ? $"The cat. You look like you could use a cuddle: petting it lifts morale by {Tuning.CatMorale:0} (once a day)."
                : "The cat, fast asleep. Click to pet it.";
        }

        void ToggleFounderCard()
        {
            bool show = _founderCard.ClassListContains("hidden");
            _founderCard.EnableInClassList("hidden", !show);
            if (show) Refresh();
        }

        public override void Refresh()
        {
            var s = State;
            var f = s.Founder;
            var fin = s.Finance;
            var project = s.ActiveProject;
            var last = s.ReleasedProjects.OrderByDescending(p => p.ReleaseDay).FirstOrDefault();
            bool resting = f.IsResting(s.Day);
            var mood = Avatar.FounderMood(s);
            int slot = Sim.IsNight ? 3 : s.Slot;
            Root.EnableInClassList("home--morning", slot == 0);
            Root.EnableInClassList("home--afternoon", slot == 1);
            Root.EnableInClassList("home--evening", slot == 2);
            Root.EnableInClassList("home--night", slot >= 3);

            _sign.text = s.StudioName;

            // Poster: the current game, the last shipped game, or an empty spot.
            var shown = project ?? last;
            _posterArt.EnableInClassList("hidden", shown == null);
            _posterCredit.text = shown == null ? "" : $"A {s.StudioName.ToUpperInvariant()} GAME";
            _posterCredit.EnableInClassList("hidden", shown == null);
            _posterTitle.text = shown == null ? "YOUR GAME HERE" : shown.Title.ToUpperInvariant();
            Root.EnableInClassList("home--no-poster", shown == null);
            _posterStamp.text = project == null && last != null ? "OUT NOW" : project != null && project.Status == ProjectStatus.ReadyToRelease ? "SOON!" : "";
            _posterStamp.EnableInClassList("hidden", _posterStamp.text.Length == 0);

            var date = GameCalendar.ToDate(s.Day);
            _calendarMonth.text = GameCalendar.MonthNames[date.Month - 1].ToUpperInvariant();
            _calendarDay.text = date.Day.ToString();
            _calendarWeekday.text = GameCalendar.DayNames[GameCalendar.DayOfWeek(s.Day)].ToUpperInvariant();

            int trophies = System.Math.Min(3, s.ShippedGames);
            while (_trophies.childCount < trophies) _trophies.Add(Ui.Tile(Icons.Trophy, "dio-trophy", 22f));
            while (_trophies.childCount > trophies) _trophies.RemoveAt(_trophies.childCount - 1);
            _trophyCount.text = s.ShippedGames > 3 ? "x" + s.ShippedGames : "";

            _jarFill.style.height = Length.Percent((float)(System.Math.Min(1.0, fin.RunwayDays() / 180.0) * 100));
            _jarFill.EnableInClassList("dio-jar__fill--low", fin.RunwayDays() < 60);

            bool ergo = s.Owned.Contains("ergonomic_chair");
            _chairWood.EnableInClassList("hidden", ergo);
            _chairErgo.EnableInClassList("hidden", !ergo);
            _deskFounder.EnableInClassList("hidden", resting);
            _deskAvatar.Mood = mood;
            _sleeper.EnableInClassList("hidden", !resting);
            _zzz.EnableInClassList("hidden", !resting);
            _sleepAvatar.Mood = f.Burnout >= Tuning.CollapseBurnout ? Mood.Panic : Mood.Tired;

            bool pc = s.Owned.Contains("gaming_pc");
            _monitor.EnableInClassList("hidden", !pc);
            _tower.EnableInClassList("hidden", !pc);
            _cooling.EnableInClassList("hidden", !s.Owned.Contains("cooling_pad"));
            _heat.EnableInClassList("hidden", !s.Flags.Contains("laptop_risky"));
            _tablet.EnableInClassList("hidden", !s.Owned.Contains("pro_art_tools"));
            _laptopGame.Show(project);
            _laptopGame.EnableInClassList("hidden", project == null);
            _mugCoffee.EnableInClassList("hidden", !s.CoffeeReady);
            _steam.EnableInClassList("hidden", !s.CoffeeReady);
            bool catAwake = s.CatPettedDay == s.Day;
            _cat.Awake = catAwake;
            _catZz.EnableInClassList("hidden", catAwake);
            int unread = s.Inbox.UnreadCount;
            _phoneBadge.text = unread > 9 ? "9+" : unread.ToString();
            _phoneBadge.EnableInClassList("hidden", unread == 0);

            int boxes = f.Burnout >= 70 ? 3 : f.Burnout >= 55 ? 2 : f.Burnout >= 40 ? 1 : 0;
            for (int i = 0; i < _pizza.Length; i++) _pizza[i].EnableInClassList("hidden", i >= boxes);
            _plant.EnableInClassList("dio-plant--wilted", f.Morale < 40);

            string quip = Quip(out bool thought);
            _bubbleText.text = quip;
            _bubble.EnableInClassList("bubble--thought", thought);
            _bubble.EnableInClassList("bubble--bed", resting);

            _cardAvatar.Mood = mood;
            _cardName.text = Studio.PlayerName ?? "You";
            _cardRole.text = f.Job == DayJob.None ? "Bedroom game developer" : "Game developer with a day job";
            Ui.SetFill(_cardMorale.Q(className: "bar__fill"), f.Morale / 100);
            _cardMorale.Q<Label>(className: "meter-row__value").text = f.Morale.ToString("0");
            var burnoutFill = _cardBurnout.Q(className: "bar__fill");
            Ui.SetFill(burnoutFill, f.Burnout / 100);
            burnoutFill.EnableInClassList("fill-bad", f.Burnout >= Tuning.VacationAdviceBurnout);
            _cardBurnout.Q<Label>(className: "meter-row__value").text = f.Burnout.ToString("0");
            _cardOutput.text = $"Output ×{Rules.Productivity(s).Value:0.00} · this week {s.CurrentWeek.HoursWorked}h";

            _tip.text = MentorTip();
            _mentor.EnableInClassList("hidden", Studio.Coach != null && Studio.Coach.IsActive);
        }

        string Quip(out bool thought)
        {
            var s = State;
            var f = s.Founder;
            thought = false;
            if (f.Burnout >= Tuning.CollapseBurnout && f.IsResting(s.Day)) return "Doctor's orders: nothing. At all.";
            if (f.IsResting(s.Day)) return "Five more minutes…";
            thought = true;
            if (s.Finance.Cash < 0) return "Please don't decline, card. Please.";
            if (s.Finance.RunwayDays() < 30) return "rent… rent… rent…";
            if (f.Burnout >= 60) return "one… more… bug…";
            thought = false;
            var project = s.ActiveProject;
            if (project != null && project.Status == ProjectStatus.ReadyToRelease) return "It's DONE. I think?";
            var soon = s.Freelance.Active.Where(g => g.DeadlineDay - s.Day <= 3).OrderBy(g => g.DeadlineDay).FirstOrDefault();
            if (soon != null) return $"Client wants it by {GameCalendar.DayNames[GameCalendar.DayOfWeek(soon.DeadlineDay)]}…";
            if (project != null) return project.Progress < 0.15 ? "Day one of the rest of my life." : "It compiles! Nobody touch anything.";
            if (s.ShippedGames > 0) return "Okay. What's next?";
            return "What should I make?";
        }

        string MentorTip()
        {
            var s = State;
            var f = s.Founder;
            var project = s.ActiveProject;
            if (s.Finance.Cash < 0) return $"You're on the credit card now. Past -{Fmt.Money(s.Finance.CreditLimit)} the bank closes the studio.";
            if (s.Finance.RunwayDays() < Tuning.MoneyAnxietyRunwayDays) return "Under two months of runway. That worry costs you sleep: +2 burnout every week.";
            if (f.Burnout >= Tuning.VacationAdviceBurnout) return "Take a week off. Burnout eats your output long before it knocks you over.";
            if (Sim.IsNight) return "Go to bed. Tomorrow's problems are tomorrow's.";
            if (Sim.LaunchWeekGame != null) return "Launch week! Fix the worst bugs early: most players arrive in the first days, and they write the reviews.";
            var demoGame = s.Projects.FirstOrDefault(p => Sim.CanWorkOnDemo(p));
            if (demoGame != null && demoGame.FestDay - s.Day <= 14 && demoGame.DemoProgress < 1)
                return $"Next Fest is in {demoGame.FestDay - s.Day} days and the demo is {Fmt.Pct(demoGame.DemoProgress)} done. Players only get one first look.";
            if (project != null && project.Status == ProjectStatus.ReadyToRelease) return "Done is better than perfect. Open the project and pick a price.";
            if (project == null && s.ShippedGames == 0) return "Start tiny. A small game you finish teaches more than a big one you don't.";
            if (project != null && !project.HasStorePage && project.Progress >= 0.25) return "Open a store page early. Wishlists take months to grow.";
            var late = s.Freelance.Active.FirstOrDefault(g => g.DeadlineDay - s.Day <= 2);
            if (late != null) return $"\"{late.Title}\" is {Fmt.Due(s.Day, late.DeadlineDay)}. Clients remember.";
            if (s.Freelance.Offers.Any() && s.Freelance.ActiveCount < 2) return "There are gig offers waiting in GigGrid. Rent doesn't pay itself.";
            switch (Fmt.WeekOfYear(s.Day) % 4)
            {
                case 0: return "Nobody tells you this, but your first estimate is always wrong.";
                case 1: return "Weekends off keep morale up. Evenings are fair game.";
                case 2: return "Take your time with each session. Careful work counts for more.";
                default: return "Hover over any number to see why it is what it is.";
            }
        }

        string ShelfHelp()
        {
            var s = State;
            if (s.ShippedGames == 0) return "An empty trophy shelf. Ship a game to put something on it.";
            return $"{s.ShippedGames} game{(s.ShippedGames == 1 ? "" : "s")} shipped, {Fmt.Money(s.LifetimeGameNet)} earned from them. Click to see your releases.";
        }

        string LaptopHelp()
        {
            var s = State;
            string risk = s.Flags.Contains("laptop_risky") ? "\nIt's running hot. A cooling pad would help." : "";
            return (s.ActiveProject != null ? $"{s.ActiveProject.Title}, so far: the screen shows your game as it grows. Click to open the project." : "Your trusty old laptop. Click to start a project.") + risk;
        }
    }
}
