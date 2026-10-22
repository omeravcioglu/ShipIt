using System;
using System.Collections.Generic;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// The whole interface (Design/18 §1): the HUD, your day on the left, the studio diorama or an app window,
    /// the dock, and the overlays (tooltips, toasts, the tutorial coach, decisions and minigames). Built in code
    /// with UI Toolkit; styles live in Resources/IndieStudio/Styles.uss.
    /// </summary>
    public sealed partial class StudioUI
    {
        readonly GameController _game;
        readonly VisualElement _root;
        readonly TodayPanel _today;
        readonly MinigameHost _minigames;
        readonly Coach _coach;
        readonly VisualElement _homeLayer;
        readonly VisualElement _windowLayer;
        readonly VisualElement _window;
        readonly Icon _windowIcon;
        readonly Label _windowTitle;
        readonly Label _windowNote;
        readonly ScrollView _content;
        readonly VisualElement _toasts;
        readonly VisualElement _modalLayer;
        readonly Tooltip _tooltip;
        readonly Hud _hud;
        readonly Dock _dock;
        readonly List<Page> _pages = new List<Page>();
        Page _current;
        string _windowClass;
        Message _modalMessage;
        LaunchShow _launch;
        bool _gameOverShown;
        /// <summary>A modal that isn't mail (welcome back, confirmations) is open.</summary>
        bool _systemModalOpen;

        public StudioUI(GameController game, VisualElement root)
        {
            _game = game;
            var styles = Resources.Load<StyleSheet>("IndieStudio/Styles");
            if (styles != null) root.styleSheets.Add(styles);

            var app = Ui.El("app");
            root.Add(app);
            _root = app;
            var desktop = Ui.El("desktop");
            desktop.style.backgroundImage = new StyleBackground(Textures.DesktopDots);
            app.Add(desktop);
            _tooltip = new Tooltip(app); // first: the HUD and pages attach tooltips while they build

            _homeLayer = Ui.El("home-layer");
            app.Add(_homeLayer);

            _windowLayer = Ui.El("window-layer");
            app.Add(_windowLayer);
            _window = Ui.El("window");
            _windowLayer.Add(_window);
            var bar = Ui.El("window__bar");
            _window.Add(bar);
            var iconTile = Ui.El("window__icon");
            _windowIcon = new Icon(Icons.Home, 24f);
            iconTile.Add(_windowIcon);
            bar.Add(iconTile);
            _windowTitle = Ui.Display("", "window__title");
            bar.Add(_windowTitle);
            _windowNote = Ui.Text("", "window__note");
            bar.Add(_windowNote);
            bar.Add(Ui.Grow());
            var close = Ui.IconBtn(Icons.Close, "", ShowHome, "window__close", 16f);
            _tooltip.Attach(close, () => "Back to your room");
            bar.Add(close);
            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.AddToClassList("window__body");
            _window.Add(_content);

            _today = new TodayPanel(this);
            app.Add(_today.Root);

            // Toasts sit under the HUD so its menus open on top of them.
            var overlay = Ui.El("overlay");
            overlay.pickingMode = PickingMode.Ignore;
            app.Add(overlay);
            _toasts = Ui.El("toast-layer");
            _toasts.pickingMode = PickingMode.Ignore;
            overlay.Add(_toasts);

            _hud = new Hud(this);
            app.Add(_hud.Root);
            _dock = new Dock(this);
            app.Add(_dock.Root);

            var coachLayer = Ui.El("overlay");
            app.Add(coachLayer);
            _modalLayer = Ui.El("overlay");
            _modalLayer.pickingMode = PickingMode.Ignore;
            app.Add(_modalLayer);
            var minigameLayer = Ui.El("overlay");
            minigameLayer.pickingMode = PickingMode.Ignore;
            app.Add(minigameLayer);
            _minigames = new MinigameHost(this, minigameLayer);

            AddPage(new HomePage(this));
            AddPage(new DashboardPage(this));
            AddPage(new InboxPage(this));
            AddPage(new ProjectPage(this));
            AddPage(new GigsPage(this));
            AddPage(new IdeasPage(this));
            AddPage(new FinancePage(this));
            AddPage(new SkillsPage(this));
            AddPage(new ShopPage(this));
            _coach = new Coach(this, coachLayer);
            Show(_pages[0]);
            game.Achievements.Unlocked += AchievementToast;
        }

        public GameController Game => _game;
        public GameSimulation Sim => _game.Sim;
        public Tooltip Tooltip => _tooltip;
        /// <summary>The app root, for finding elements by name (the tutorial's pointer, tests).</summary>
        public VisualElement Root => _root;
        public TodayPanel Today => _today;
        public Hud Hud => _hud;
        public MinigameHost Minigames => _minigames;
        public Coach Coach => _coach;
        public bool HasBlockingModal => _modalMessage != null || _systemModalOpen || Sim.State.IsGameOver;
        /// <summary>The launch-day show, while it's on screen.</summary>
        public LaunchShow LaunchShow => _launch;
        public IReadOnlyList<Page> Pages => _pages;
        public Page Current => _current;

        void AddPage(Page page)
        {
            _pages.Add(page);
            _dock.Add(page);
        }

        public T Page<T>() where T : Page => _pages.OfType<T>().First();

        public void Show(Page page)
        {
            if (IsLocked(page))
            {
                ShowLocked(page);
                return;
            }
            _current = page;
            _dock.SetActive(page);
            _hud.CloseMenus();
            _homeLayer.EnableInClassList("hidden", !page.IsHome);
            _windowLayer.EnableInClassList("hidden", page.IsHome);
            if (page.IsHome)
            {
                if (page.Root.parent != _homeLayer) _homeLayer.Add(page.Root);
            }
            else
            {
                if (_windowClass != null) _window.RemoveFromClassList(_windowClass);
                _windowClass = page.AppClass;
                _window.AddToClassList(_windowClass);
                _windowIcon.Path = page.IconPath;
                _content.Clear();
                _content.Add(page.Root);
                _content.scrollOffset = Vector2.zero;
            }
            page.Refresh();
            UpdateWindowBar();
            _coach?.Refresh();
        }

        public void Show<T>() where T : Page => Show(Page<T>());

        public void ShowHome() => Show(_pages[0]);

        public void ShowInbox(InboxFolder folder)
        {
            Page<InboxPage>().SetFolder(folder);
            Show<InboxPage>();
        }

        void UpdateWindowBar()
        {
            if (_current == null || _current.IsHome) return;
            _windowTitle.text = string.IsNullOrEmpty(_current.WindowTitle) ? _current.Title : _current.WindowTitle;
            _windowNote.text = _current.BarNote ?? "";
            _windowNote.EnableInClassList("hidden", string.IsNullOrEmpty(_current.BarNote));
        }

        /// <summary>Re-renders what changed. Called after every player action.</summary>
        public void Refresh()
        {
            Sim.CheckProgress();
            _hud.Refresh();
            _dock.Refresh();
            _today.Refresh();
            _current?.Refresh();
            UpdateWindowBar();
            ShowNextModal();
            _coach?.Refresh();
        }

        /// <summary>Runs a player command, then refreshes everything.</summary>
        public void Do(Action command)
        {
            command();
            _game.NotifyChanged();
        }

        // ------------------------------------------------------------------ spending the day

        /// <summary>
        /// Spends the current slot on an activity. Work that is played as a minigame opens it first and is
        /// applied with the minigame's score; everything else happens at once and shows up as a note.
        /// </summary>
        public void Play(PlanBlock activity)
        {
            if (Sim.State.IsGameOver || HasBlockingModal || _minigames.IsOpen || Sim.IsNight) return;
            var block = Sim.ForcedActivity() ?? activity;
            var minigame = Activities.For(Sim, block, _game.Settings.RelaxedMinigames);
            if (minigame == null)
            {
                var report = Sim.PerformSlot(block);
                if (report != null) SlotToast(report);
                _game.NotifyChanged();
                return;
            }
            _minigames.Open(minigame, score => Sim.PerformSlot(block, Rules.MinigameMultiplier(score)));
            _coach?.Refresh();
        }

        /// <summary>Rests through whatever is left of today.</summary>
        public void CallItADay()
        {
            if (Sim.State.IsGameOver || _minigames.IsOpen) return;
            Sim.RestForTheDay();
            _game.NotifyChanged();
        }

        /// <summary>Goes to bed: tomorrow starts, with its mail, payments and (on Mondays) a summary of last week.</summary>
        public void Sleep()
        {
            if (Sim.State.IsGameOver || _minigames.IsOpen || HasBlockingModal) return;
            Sim.Sleep();
            LaunchMorningToast();
            if (GameCalendar.IsMonday(Sim.Day) && Sim.State.LastWeek != null)
            {
                var week = Sim.State.LastWeek;
                Toast(Icons.Calendar, "act-rest", "Last week",
                    $"{week.HoursWorked}h of work. Burnout {Signed(week.BurnoutChange)}, morale {Signed(week.MoraleChange)}." +
                    (week.FreelanceEarned > 0 ? $" Gigs delivered: {Fmt.Money(week.FreelanceEarned)}." : ""));
            }
            _game.NotifyChanged();
        }

        /// <summary>Each morning of launch week: yesterday's numbers, and any critic who published today.</summary>
        void LaunchMorningToast()
        {
            var game = Sim.LaunchWeekGame;
            if (game == null) return;
            int day = Sim.DaysSinceLaunch(game);
            var r = game.Result;
            if (day <= 0 || r.LaunchDays.Count < day) return;
            var yesterday = r.LaunchDays[day - 1];
            string critics = string.Join(" ", r.Critics.Where(c => c.Day == day).Select(c => $"{c.Outlet} gave it {c.Score}."));
            Toast(Icons.Rocket, "bg-orange", $"Launch day {day + 1} of {Tuning.LaunchWeekDays}: {game.Title}",
                $"Yesterday: {yesterday.Units:0} copies, {Fmt.Money(yesterday.Net)}. Players' mood: {Fmt.Pct(r.Mood)} positive. {critics}".Trim());
        }

        static string Signed(double value) => (value >= 0 ? "+" : "") + value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Releases a game and puts on the launch-day show.</summary>
        public void Launch(Project project, double price)
        {
            if (Sim.State.IsGameOver || HasBlockingModal || _minigames.IsOpen) return;
            if (!Sim.Release(project.Id, price, out string error))
            {
                Notify(Icons.Warning, "bg-red-light", "Not yet", error);
                _game.NotifyChanged();
                return;
            }
            ShowLaunchDay(project);
            _game.NotifyChanged();
        }

        /// <summary>The launch-day show for a released game (news waits until it's over).</summary>
        public void ShowLaunchDay(Project project)
        {
            if (project?.Result == null) return;
            _launch?.Stop();
            _modalLayer.Clear();
            _modalMessage = null; // a decision that was on screen comes back after the show
            _systemModalOpen = true;
            _hud.CloseMenus();
            _launch = new LaunchShow(this, project, () =>
            {
                _launch = null;
                CloseSystemModal();
            });
            _modalLayer.Add(_launch.Root);
            _modalLayer.pickingMode = PickingMode.Position;
            _coach?.Refresh();
        }

        /// <summary>A toast with an icon tile (coffee, the cat, small confirmations).</summary>
        public void Notify(string icon, string tileClass, string title, string body) => Toast(icon, tileClass, title, body);

        void SlotToast(SlotReport report)
        {
            string body = report.Lines.Count > 0 ? string.Join(" ", report.Lines.Take(2)) : "";
            Toast(Fmt.ActivityIcon(report.Activity.Kind), report.HasSkill ? Fmt.SkillClass(report.Skill) : Fmt.ActivityClass(report.Activity.Kind),
                $"{Fmt.SlotName(report.Slot)}: {report.Title}", body);
        }

        public void OnNewGame()
        {
            _minigames?.Close(false);
            _modalMessage = null;
            _launch = null;
            _systemModalOpen = false;
            _gameOverShown = false;
            _modalLayer.Clear();
            _modalLayer.pickingMode = PickingMode.Ignore;
            _toasts.Clear();
            _demoEndShown = false;
            ShowHome();
            if (Sim.State.IsDemo) ShowDemoIntro();
        }

        /// <summary>
        /// Once the UI exists: settle a cloud-save conflict first, then greet a returning player, or explain a save
        /// that couldn't be loaded.
        /// </summary>
        public void OnStarted()
        {
            if (_game.PendingConflict != null)
            {
                ShowCloudConflict();
                return;
            }
            if (_game.ResumedFromSave && Sim.State.DemoOver) ShowDemoEnd();
            else if (_game.ResumedFromSave) ShowWelcomeBack();
            else if (Sim.State.IsDemo) ShowDemoIntro();
            else if (_game.LoadProblem != null)
            {
                var modal = OpenSystemModal(MessageKind.Critical, Doug(Mood.Tired), "Your save couldn't be loaded", _game.LoadProblem);
                AddChoices(modal, ("Start fresh", null, true, () => CloseSystemModal()));
            }
            _game.LoadProblem = null;
        }

        /// <summary>The player's name on the platform (Steam), or null when offline.</summary>
        public string PlayerName => _game.Platform.IsSignedIn ? _game.Platform.Player?.DisplayName : null;

        string CloudName => _game.Platform.Name == "Steam" ? "Steam Cloud" : "the cloud";

        void ShowWelcomeBack()
        {
            var s = Sim.State;
            var chapter = Sim.CurrentChapter;
            int slot = Sim.IsNight ? 3 : s.Slot;
            string goal = chapter.Steps.Count > 0
                ? $"Chapter {chapter.Number}: {chapter.Goal.ToLowerInvariant()} ({chapter.StepsDone(s)} of {chapter.Steps.Count} steps done)."
                : "You've finished every chapter written so far.";
            string games = s.ShippedGames == 0 ? "" : s.ShippedGames == 1 ? " One game shipped." : $" {s.ShippedGames} games shipped.";
            if (s.IsDemo) games += $" {DemoDaysLeftText(s.Day)} in the demo.";
            string hello = PlayerName != null ? $"Hi {PlayerName}! " : "";
            string body = $"{hello}It's {GameCalendar.Format(s.Day)}, {Fmt.SlotName(slot).ToLowerInvariant()}. " +
                          $"{Fmt.Money(s.Finance.Cash)} in the bank, {Fmt.Days(s.Finance.RunwayDays())} of runway.\n{goal}{games}" +
                          (_game.LoadedFromCloud ? $"\nThis studio came from {CloudName}: you last played it on another computer." : "") +
                          (_game.LoadProblem != null ? "\n" + _game.LoadProblem : "");
            _game.LoadProblem = null;
            var modal = OpenSystemModal(MessageKind.Info, Doug(Mood.Happy), "Welcome back to " + s.StudioName, body);
            AddChoices(modal,
                ("Continue", "Pick up where you left off.", true, () => CloseSystemModal()),
                (s.IsDemo ? "Start the demo over" : "Start a new studio", "A new seed, a new bedroom, $5,000.", false, ConfirmNewStudio));
        }

        /// <summary>
        /// Two different studios: the one in the cloud and the one on this computer both changed since they last
        /// matched (offline play on one computer and online play on another). The player keeps one; the other is
        /// kept as a backup file.
        /// </summary>
        void ShowCloudConflict()
        {
            var conflict = _game.PendingConflict;
            var modal = OpenSystemModal(MessageKind.Decision, Doug(Mood.Focus), "Which studio do you want to keep?",
                $"The save in {CloudName} isn't the one on this computer, and both have changed since they last matched. " +
                "Maybe you played offline here and on another computer too. Pick the one to keep playing. The other is kept as a backup file, never deleted.");
            var cards = Ui.Row("conflict");
            cards.Add(SaveCard(CloudName == "the cloud" ? "IN THE CLOUD" : "STEAM CLOUD", conflict.Cloud, conflict.CloudSaved));
            cards.Add(SaveCard("THIS COMPUTER", conflict.Local, conflict.LocalSaved));
            modal.Add(cards);
            bool cloudNewer = (conflict.CloudSaved ?? DateTime.MinValue) >= (conflict.LocalSaved ?? DateTime.MinValue);
            AddChoices(modal,
                ("Keep the cloud save", "Continue the studio from your other computer.", cloudNewer, () => SettleConflict(true)),
                ("Keep this computer's save", $"It replaces the one in {CloudName}.", !cloudNewer, () => SettleConflict(false)));
        }

        void SettleConflict(bool keepCloud)
        {
            CloseSystemModal(false);
            _game.ResolveConflict(keepCloud);
            ShowWelcomeBack();
        }

        static VisualElement SaveCard(string where, GameSimulation sim, DateTime? saved)
        {
            var s = sim.State;
            var card = Ui.El("conflict__card");
            card.Add(Ui.Text(where, "conflict__where"));
            card.Add(Ui.Display(s.StudioName, "conflict__name"));
            card.Add(Ui.Text($"Day {s.Day + 1} · {GameCalendar.Format(s.Day)}", "conflict__line"));
            string games = s.ShippedGames == 1 ? "1 game shipped" : $"{s.ShippedGames} games shipped";
            card.Add(Ui.Text($"{Fmt.Money(s.Finance.Cash)} · {games} · chapter {Math.Min(s.Chapter + 1, ChapterCatalog.All.Count)}", "conflict__line"));
            if (saved.HasValue) card.Add(Ui.Text($"Saved {saved.Value:d MMM, HH:mm}", "conflict__saved"));
            return card;
        }

        /// <summary>Every achievement: the ones you have, and how close you are to the rest.</summary>
        public void ShowAchievements()
        {
            if (HasBlockingModal || _minigames.IsOpen) return;
            _hud.CloseMenus();
            var platform = _game.Platform;
            int unlocked = _game.Achievements.UnlockedCount;
            string where = platform.Name == "Steam" ? "on Steam" : "on this computer";
            var modal = OpenSystemModal(MessageKind.Info, Doug(Mood.Happy), "Achievements",
                $"{unlocked} of {AchievementCatalog.All.Count} unlocked {where}. Hidden ones reveal themselves when they happen.");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("achievements");
            scroll.name = "achievements";
            var columns = Ui.Row("achievements__columns");
            var left = Ui.El("achievements__column achievements__column--left");
            var right = Ui.El("achievements__column");
            columns.Add(left);
            columns.Add(right);
            for (int i = 0; i < AchievementCatalog.All.Count; i++)
            {
                var achievement = AchievementCatalog.All[i];
                (i % 2 == 0 ? left : right).Add(AchievementView.Row(achievement, platform.IsAchievementUnlocked(achievement.Id), Sim.State));
            }
            scroll.Add(columns);
            modal.Add(scroll);
            AddChoices(modal, ("Close", null, true, () => CloseSystemModal()));
        }

        /// <summary>Asks before replacing the current studio with a new one.</summary>
        public void ConfirmNewStudio()
        {
            CloseSystemModal(false);
            _hud.CloseMenus();
            var modal = OpenSystemModal(MessageKind.Critical, Doug(Mood.Panic), Sim.State.IsDemo ? "Start the demo over?" : "Start a new studio?",
                "Your current studio will be replaced by a new one, and the old save with it. This can't be undone.");
            AddChoices(modal,
                ("Keep playing", null, false, () => CloseSystemModal()),
                ("Start over", "A new seed, a new bedroom, $5,000.", true, () =>
                {
                    CloseSystemModal(false);
                    _game.NewGame((ulong)DateTime.Now.Ticks);
                }));
        }

        /// <summary>Renames the studio (the menu, or clicking the sign in the room).</summary>
        public void ShowNameStudio()
        {
            if (HasBlockingModal || _minigames.IsOpen) return;
            _hud.CloseMenus();
            var modal = OpenSystemModal(MessageKind.Info, Doug(Mood.Happy), "Name your studio",
                "Every studio needs a name. It goes on the sign above your window, on your posters and on launch day.");
            var field = new StudioNameField(this);
            field.AddToClassList("modal__field");
            modal.Add(field);
            AddChoices(modal,
                ("That's the name", null, true, () =>
                {
                    field.Apply();
                    CloseSystemModal();
                }),
                ("Cancel", null, false, () => CloseSystemModal()));
        }

        static Sender Doug(Mood mood) => new Sender { Name = "Old Dev Doug", Look = AvatarLook.Mentor, Mood = mood, Background = "bg-mustard-light" };

        VisualElement OpenSystemModal(MessageKind kind, Sender sender, string title, string body)
        {
            _systemModalOpen = true;
            return OpenModal(kind, sender, title, body, Sim.Day, -1);
        }

        void AddChoices(VisualElement modal, params (string label, string detail, bool primary, Action onClick)[] choices)
        {
            var row = Ui.El("modal__choices");
            for (int i = 0; i < choices.Length; i++)
            {
                var c = choices[i];
                var button = Choice(c.label, c.detail, c.primary, c.onClick);
                button.name = "choice-" + i;
                row.Add(button);
            }
            modal.Add(row);
        }

        /// <summary>Closes whatever dialog of the interface's own is open (tests; the mail modals close by answering).</summary>
        public void DismissDialog()
        {
            if (_systemModalOpen) CloseSystemModal();
        }

        void CloseSystemModal(bool refresh = true)
        {
            _systemModalOpen = false;
            _launch?.Stop();
            _launch = null;
            _modalLayer.Clear();
            _modalLayer.pickingMode = PickingMode.Ignore;
            if (refresh) _game.NotifyChanged();
        }

        public void OnMessagePosted(Message message)
        {
            if (message.Kind == MessageKind.Info || message.Kind == MessageKind.Opportunity) Toast(message);
        }

        void Toast(Message message)
        {
            string body = message.Body.Length > 120 ? message.Body.Substring(0, 117) + "…" : message.Body;
            AddToast(Senders.For(message).Portrait(40f, "toast__tile"), message.Title, body);
        }

        void Toast(string icon, string tileClass, string title, string body) =>
            AddToast(Ui.Tile(icon, "toast__tile " + tileClass, 20f), title, body);

        void AchievementToast(AchievementDef achievement)
        {
            var toast = AddToast(AchievementView.Badge(achievement, true, 20f, "toast__tile"), "Achievement unlocked: " + achievement.Title, achievement.Description);
            toast.AddToClassList("toast--achievement");
            toast.name = "toast-achievement";
        }

        VisualElement AddToast(VisualElement tile, string title, string body)
        {
            while (_toasts.childCount >= 3) _toasts.RemoveAt(0);
            var toast = Ui.El("toast");
            toast.Add(tile);
            var text = Ui.El("toast__text");
            text.Add(Ui.Text(title, "toast__title"));
            if (!string.IsNullOrEmpty(body)) text.Add(Ui.Text(body, "toast__body"));
            toast.Add(text);
            IgnorePointer(toast);
            _toasts.Add(toast);
            toast.schedule.Execute(() => toast.RemoveFromHierarchy()).StartingIn(5000);
            return toast;
        }

        static void IgnorePointer(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            foreach (var child in element.Children()) IgnorePointer(child);
        }

        // ------------------------------------------------------------------ decision modal

        void ShowNextModal()
        {
            if (_minigames.IsOpen || _systemModalOpen) return; // news waits until the session or the dialog is over
            if (Sim.State.DemoOver)
            {
                if (!_demoEndShown) ShowDemoEnd();
                return;
            }
            if (Sim.State.IsGameOver)
            {
                if (!_gameOverShown) ShowGameOver();
                return;
            }
            if (_modalMessage != null)
            {
                if (IsBlocking(_modalMessage)) return;
                _modalLayer.Clear(); // resolved elsewhere (for example from the Inbox)
                _modalLayer.pickingMode = PickingMode.Ignore;
                _modalMessage = null;
            }
            var next = Sim.State.Inbox.Messages.FirstOrDefault(m => m.NeedsDecision)
                       ?? Sim.State.Inbox.Messages.FirstOrDefault(m => m.Kind == MessageKind.Critical && !m.IsRead);
            if (next == null) return;

            _modalMessage = next;
            var modal = OpenModal(next.Kind, Senders.For(next), next.Title, next.Body, next.Day, next.NeedsDecision ? next.ExpiresDay : -1);
            var choices = Ui.El("modal__choices");
            modal.Add(choices);
            if (next.Options.Count == 0)
            {
                choices.Add(Choice("OK", null, true, () => CloseModal(() => Sim.MarkRead(next.Id))));
            }
            else
            {
                for (int i = 0; i < next.Options.Count; i++)
                {
                    int option = i;
                    var o = next.Options[i];
                    choices.Add(Choice(o.Label, o.Detail, i == 0, () => CloseModal(() => Sim.ChooseOption(next.Id, option))));
                }
                if (next.ExpiresDay >= 0)
                    modal.Add(Ui.Text($"If you don't decide by {GameCalendar.FormatShort(next.ExpiresDay)}: \"{next.Options[next.DefaultOption].Label}\".", "modal__note"));
            }
        }

        VisualElement OpenModal(MessageKind kind, Sender sender, string title, string body, int day, int expiresDay)
        {
            _modalLayer.Clear();
            var backdrop = Ui.El("modal-backdrop");
            var modal = Ui.El("modal modal--" + kind.ToString().ToLowerInvariant());
            backdrop.Add(modal);

            var head = Ui.El("modal__head");
            modal.Add(head);
            head.Add(sender.Portrait(104f, "modal__portrait"));
            var info = Ui.El("modal__info");
            head.Add(info);
            var chips = Ui.Row("modal__chips");
            chips.Add(Ui.Pill(Fmt.KindIcon(kind), Fmt.KindLabel(kind), "pill--kind " + Fmt.KindClass(kind)));
            if (expiresDay >= 0) chips.Add(Ui.Pill(Icons.Clock, "Decide by " + GameCalendar.FormatShort(expiresDay), "pill--ink"));
            chips.Add(Ui.Text(sender.Name + " · " + GameCalendar.FormatShort(day), "modal__from"));
            info.Add(chips);
            info.Add(Ui.Display(title, "modal__title"));

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("modal__scroll");
            scroll.Add(Ui.Text(body, "modal__body"));
            modal.Add(scroll);

            _modalLayer.Add(backdrop);
            _modalLayer.pickingMode = PickingMode.Position;
            _coach?.Refresh(); // Doug steps aside while a dialog is open
            return modal;
        }

        static Button Choice(string label, string detail, bool primary, Action onClick)
        {
            var button = Ui.Clickable(onClick, "choice" + (primary ? " choice--primary" : ""));
            button.Add(Ui.Text(label, "choice__label"));
            if (!string.IsNullOrEmpty(detail)) button.Add(Ui.Text(detail, "choice__detail"));
            return button;
        }

        void CloseModal(Action command)
        {
            _modalLayer.Clear();
            _modalLayer.pickingMode = PickingMode.Ignore;
            var message = _modalMessage;
            _modalMessage = null;
            command();
            if (message != null && !string.IsNullOrEmpty(message.Outcome))
                Toast(new Message { Title = message.Title, Body = message.Outcome, Kind = MessageKind.Info, EventId = message.EventId });
            _game.NotifyChanged();
        }

        void ShowGameOver()
        {
            _gameOverShown = true;
            var sender = new Sender { Name = "You", Look = AvatarLook.Founder, Mood = Mood.Panic, Background = "bg-red-light" };
            var modal = OpenModal(MessageKind.Critical, sender, "The studio is closed", Sim.State.GameOverReason, Sim.Day, -1);
            var choices = Ui.El("modal__choices");
            choices.Add(Choice("Start a new studio", "A new seed, a new bedroom, $5,000.", true, () => _game.NewGame((ulong)DateTime.Now.Ticks)));
            modal.Add(choices);
        }

        static bool IsBlocking(Message m) => m.NeedsDecision || (m.Kind == MessageKind.Critical && m.Options.Count == 0 && !m.IsRead);
    }

    /// <summary>A floating "Why?" bubble. Text is produced lazily when the pointer enters.</summary>
    public sealed class Tooltip
    {
        readonly VisualElement _layer;
        readonly Label _label;

        public Tooltip(VisualElement layer)
        {
            _layer = layer;
            _label = Ui.Text("", "tooltip");
            _label.pickingMode = PickingMode.Ignore;
            _label.style.display = DisplayStyle.None;
            layer.Add(_label);
        }

        public void Attach(VisualElement target, Func<string> text)
        {
            target.RegisterCallback<PointerEnterEvent>(e => Show(text(), e.position));
            target.RegisterCallback<PointerMoveEvent>(e => Move(e.position));
            target.RegisterCallback<PointerLeaveEvent>(e => Hide());
            target.RegisterCallback<DetachFromPanelEvent>(e => Hide());
        }

        void Show(string text, Vector3 position)
        {
            if (string.IsNullOrEmpty(text)) return;
            _label.text = text;
            _label.style.display = DisplayStyle.Flex;
            _label.BringToFront();
            Move(position);
        }

        void Move(Vector3 position)
        {
            float width = _layer.resolvedStyle.width;
            float height = _layer.resolvedStyle.height;
            float x = position.x + 16;
            if (x > width - 400) x = Mathf.Max(0, position.x - 406);
            float y = position.y + 20;
            if (y > height - 140) y = Mathf.Max(0, position.y - 150);
            _label.style.left = x;
            _label.style.top = y;
        }

        void Hide() => _label.style.display = DisplayStyle.None;
    }

    /// <summary>Base class for the apps in the dock. Each one renders into a window (or, for Home, the room).</summary>
    public abstract class Page
    {
        protected Page(StudioUI studio)
        {
            Studio = studio;
            Root = Ui.El("page");
        }

        protected StudioUI Studio { get; }
        protected GameSimulation Sim => Studio.Sim;
        protected GameState State => Studio.Sim.State;
        public VisualElement Root { get; }

        /// <summary>The app's name in the dock and, by default, in the window's title bar.</summary>
        public abstract string Title { get; }
        public abstract string IconPath { get; }
        /// <summary>USS class that colors this app's dock tile and title bar, e.g. "app-gigs".</summary>
        public abstract string AppClass { get; }
        public virtual bool IsHome => false;

        /// <summary>Overrides the title bar text (for example with the project's name).</summary>
        public string WindowTitle { get; protected set; }
        /// <summary>A short note shown as a chip next to the title.</summary>
        public string BarNote { get; protected set; }

        /// <summary>Rebuilds the page from the current state.</summary>
        public abstract void Refresh();

        protected void Header(string title, string subtitle)
        {
            WindowTitle = title;
            if (!string.IsNullOrEmpty(subtitle)) Root.Add(Ui.Text(subtitle, "page-intro"));
        }

        /// <summary>A button that runs a simulation command and refreshes the UI.</summary>
        protected Button Command(string text, Action action, string classes = "btn") => Ui.Btn(text, () => Studio.Do(action), classes);

        protected void Why(VisualElement target, Func<string> text) => Studio.Tooltip.Attach(target, text);
    }
}
