using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// The always-visible top bar (Design/18 §3): the part of the day, date, cash, runway, the founder's morale
    /// and burnout, alert counters that open the inbox, and the current goal. There is no clock to run:
    /// time moves when you spend a slot of the day.
    /// </summary>
    public sealed class Hud
    {
        static readonly string[] TimeClasses = { "time-morning", "time-afternoon", "time-evening", "time-night" };

        readonly StudioUI _studio;
        readonly VisualElement _time;
        readonly Icon _timeIcon;
        readonly Label _timeLabel;
        readonly VisualElement[] _timePips = new VisualElement[3];
        readonly Label _day;
        readonly Label _week;
        readonly VisualElement _cashChip;
        readonly Label _cash;
        readonly Icon _cashArrow;
        readonly Label _cashDelta;
        readonly VisualElement _runwayChip;
        readonly Label _runway;
        readonly VisualElement _runwaySegments;
        readonly Avatar _face;
        readonly VisualElement _moraleFill;
        readonly Label _morale;
        readonly VisualElement _burnoutFill;
        readonly Label _burnout;
        readonly Button _critical;
        readonly Label _criticalCount;
        readonly Button _decisions;
        readonly Label _decisionCount;
        readonly Button _mail;
        readonly Label _mailCount;
        readonly Button _goal;
        readonly Label _goalChapter;
        readonly Label _goalText;
        readonly Label _goalCount;
        readonly VisualElement _goalsPopover;
        readonly VisualElement _menuPopover;

        public Hud(StudioUI studio)
        {
            _studio = studio;
            var tooltip = studio.Tooltip;
            Root = Ui.El("hud");

            _time = Ui.Clickable(studio.ShowHome, "hud-time");
            _time.name = "hud-time";
            _timeIcon = new Icon(Icons.Sunrise, 26f, 2.6f);
            _time.Add(_timeIcon);
            var timeText = Ui.El("hud-time__text");
            _timeLabel = Ui.Display("", "hud-time__label");
            timeText.Add(_timeLabel);
            var pips = Ui.Row("hud-time__pips");
            for (int i = 0; i < 3; i++)
            {
                _timePips[i] = Ui.El("hud-time__pip");
                pips.Add(_timePips[i]);
            }
            timeText.Add(pips);
            _time.Add(timeText);
            tooltip.Attach(_time, TimeTooltip);
            Root.Add(_time);

            var date = Ui.El("hud-date");
            _day = Ui.Display("", "hud-date__day");
            _week = Ui.Text("", "hud-date__sub");
            date.Add(_day);
            date.Add(_week);
            Root.Add(date);
            Root.Add(Ui.El("hud-sep"));

            _cashChip = Ui.Row("hud-chip hud-cash");
            _cashChip.Add(Ui.Text("$", "coin"));
            var cashText = Ui.El("hud-chip__text");
            _cash = Ui.Display("", "hud-cash__value");
            cashText.Add(_cash);
            var delta = Ui.Row("hud-cash__delta");
            _cashArrow = new Icon(Icons.ArrowDown, 12f, 3f);
            delta.Add(_cashArrow);
            _cashDelta = Ui.Text("", "hud-cash__delta-text");
            delta.Add(_cashDelta);
            cashText.Add(delta);
            _cashChip.Add(cashText);
            tooltip.Attach(_cashChip, CashTooltip);
            _cashChip.RegisterCallback<ClickEvent>(e => studio.Show<FinancePage>());
            Root.Add(_cashChip);

            _runwayChip = Ui.Row("hud-chip hud-runway");
            _runwayChip.name = "hud-runway";
            _runwayChip.Add(new Icon(Icons.Hourglass, 28f));
            var runwayText = Ui.El("hud-chip__text");
            var runwayLine = Ui.Row("hud-runway__line");
            runwayLine.Add(Ui.Text("Runway", "hud-runway__label"));
            _runway = Ui.Display("", "hud-runway__value");
            runwayLine.Add(_runway);
            runwayText.Add(runwayLine);
            _runwaySegments = Ui.Segments(0, 5, "fill-good");
            runwayText.Add(_runwaySegments);
            _runwayChip.Add(runwayText);
            tooltip.Attach(_runwayChip, RunwayTooltip);
            _runwayChip.RegisterCallback<ClickEvent>(e => studio.Show<FinancePage>());
            Root.Add(_runwayChip);

            var founder = Ui.Row("hud-chip hud-founder");
            var portrait = Ui.El("portrait hud-founder__face bg-mustard-light");
            _face = new Avatar(AvatarLook.Founder, Mood.Happy, 42f);
            portrait.Add(_face);
            founder.Add(portrait);
            var meters = Ui.El("hud-founder__meters");
            meters.Add(MiniMeter("Morale", "fill-good", out _moraleFill, out _morale, MoraleTooltip));
            meters.Add(MiniMeter("Burnout", "fill-warn", out _burnoutFill, out _burnout, BurnoutTooltip));
            founder.Add(meters);
            founder.RegisterCallback<ClickEvent>(e => studio.Show<SkillsPage>());
            Root.Add(founder);

            Root.Add(Ui.Grow());

            _critical = Alert(Icons.Bell, "hud-alert--critical", out _criticalCount, () => studio.ShowInbox(InboxFolder.Critical));
            tooltip.Attach(_critical, () => "Critical news: money, health, broken hardware.");
            _decisions = Alert(Icons.Scale, "hud-alert--decision", out _decisionCount, () => studio.ShowInbox(InboxFolder.Decisions));
            tooltip.Attach(_decisions, () => "Decisions waiting for your answer.");
            _mail = Alert(Icons.Mail, "hud-alert--mail", out _mailCount, () => studio.ShowInbox(InboxFolder.All));
            tooltip.Attach(_mail, () => "Unread mail.");

            _goal = Ui.Clickable(ToggleGoals, "hud-goal");
            _goal.name = "hud-goal";
            _goal.Add(new Icon(Icons.Target, 26f, 2.6f));
            var goalText = Ui.El("hud-goal__text");
            _goalChapter = Ui.Text("", "hud-goal__chapter");
            goalText.Add(_goalChapter);
            _goalText = Ui.Display("", "hud-goal__title");
            goalText.Add(_goalText);
            _goal.Add(goalText);
            _goalCount = Ui.Text("", "hud-goal__count");
            _goal.Add(_goalCount);
            Root.Add(_goal);

            var menu = Ui.IconBtn(Icons.Menu, "", ToggleMenu, "hud-alert hud-menu", 22f);
            menu.name = "hud-menu";
            tooltip.Attach(menu, () => Sim.State.IsDemo
                ? "Menu: how to play, relaxed minigames, the tutorial, your studio's name, starting the demo over, and the full game."
                : "Menu: your account and achievements, how to play, relaxed minigames, saving, the tutorial, your studio's name, a new studio.");
            Root.Add(menu);

            _goalsPopover = Ui.El("goals-popover hidden");
            Root.Add(_goalsPopover);
            _menuPopover = Ui.El("goals-popover menu-popover hidden");
            _menuPopover.name = "menu-popover";
            Root.Add(_menuPopover);
        }

        public VisualElement Root { get; }
        GameSimulation Sim => _studio.Sim;

        VisualElement MiniMeter(string label, string fillClass, out VisualElement fill, out Label value, System.Func<string> help)
        {
            var row = Ui.Row("mini-meter");
            row.Add(Ui.Text(label, "mini-meter__label"));
            var bar = Ui.Bar(0, fillClass, 10);
            bar.AddToClassList("mini-meter__bar");
            fill = bar[0];
            row.Add(bar);
            value = Ui.Text("", "mini-meter__value");
            row.Add(value);
            _studio.Tooltip.Attach(row, help);
            return row;
        }

        Button Alert(string icon, string classes, out Label count, System.Action onClick)
        {
            var button = Ui.IconBtn(icon, "", onClick, "hud-alert " + classes, 22f);
            count = Ui.Text("", "badge");
            button.Add(count);
            Root.Add(button);
            return button;
        }

        public void Refresh()
        {
            var s = Sim.State;
            var f = s.Founder;
            var fin = s.Finance;
            int slot = Sim.IsNight ? 3 : s.Slot;
            foreach (var c in TimeClasses) _time.EnableInClassList(c, c == Fmt.SlotClass(slot));
            _timeIcon.Path = Fmt.SlotIcon(slot);
            _timeLabel.text = Fmt.SlotName(slot);
            for (int i = 0; i < 3; i++)
            {
                _timePips[i].EnableInClassList("hud-time__pip--done", i < slot);
                _timePips[i].EnableInClassList("hud-time__pip--now", i == slot);
            }

            _day.text = Fmt.DayMonth(s.Day);
            _week.text = $"Year {GameCalendar.Year(s.Day)} · Week {Fmt.WeekOfYear(s.Day)}";

            _cash.text = Fmt.Money(fin.Cash);
            _cashChip.EnableInClassList("hud-cash--negative", fin.Cash < 0);
            double week = fin.Transactions.Where(t => t.Day >= s.Day - 7).Sum(t => t.Amount);
            _cashDelta.text = (week == 0 ? "no change" : Fmt.SignedMoney(week)) + " this week";
            _cashArrow.Path = week >= 0 ? Icons.ArrowUp : Icons.ArrowDown;
            _cashArrow.EnableInClassList("hidden", week == 0);
            _cashChip.EnableInClassList("hud-cash--up", week > 0);
            _cashChip.EnableInClassList("hud-cash--down", week < 0);

            double runway = fin.RunwayDays();
            _runway.text = Fmt.Days(runway);
            int segments = Fmt.RunwaySegments(runway);
            string runwayClass = Fmt.RunwayClass(runway);
            for (int i = 0; i < _runwaySegments.childCount; i++)
            {
                var segment = _runwaySegments[i];
                segment.EnableInClassList("fill-good", i < segments && runwayClass == "fill-good");
                segment.EnableInClassList("fill-warn", i < segments && runwayClass == "fill-warn");
                segment.EnableInClassList("fill-bad", i < segments && runwayClass == "fill-bad");
            }
            _runwayChip.EnableInClassList("hud-runway--danger", runway < 30);

            _face.Mood = Avatar.FounderMood(s);
            Ui.SetFill(_moraleFill, f.Morale / 100);
            _morale.text = f.Morale.ToString("0");
            Ui.SetFill(_burnoutFill, f.Burnout / 100);
            _burnout.text = f.Burnout.ToString("0");
            _burnoutFill.EnableInClassList("fill-bad", f.Burnout >= Tuning.VacationAdviceBurnout);

            SetCount(_critical, _criticalCount, s.Inbox.Messages.Count(m => m.Kind == MessageKind.Critical && !m.IsRead));
            SetCount(_decisions, _decisionCount, s.Inbox.Pending.Count());
            SetCount(_mail, _mailCount, s.Inbox.UnreadCount);

            var chapter = GoalChapter;
            _goalChapter.text = s.IsDemo
                ? (s.ShippedGames > 0 ? "DEMO · COMPLETE" : s.DemoOver ? "DEMO · OVER" : $"DEMO · {StudioUI.DemoDaysLeftText(s.Day).ToUpperInvariant()}")
                : chapter.Steps.Count > 0 ? $"CHAPTER {chapter.Number} · GOAL" : "SANDBOX";
            _goal.EnableInClassList("hud-goal--demo", s.IsDemo);
            _goalText.text = chapter.Goal;
            _goalCount.text = chapter.Steps.Count > 0 ? $"{chapter.StepsDone(s)}/{chapter.Steps.Count}" : "";
            _goalCount.EnableInClassList("hidden", chapter.Steps.Count == 0);
            if (!_goalsPopover.ClassListContains("hidden")) BuildGoals();
        }

        /// <summary>The chapter the goal shows. The demo is chapter 1 from start to end, even once its game is out.</summary>
        ChapterDef GoalChapter => Sim.State.IsDemo ? ChapterCatalog.All[0] : Sim.CurrentChapter;

        string TimeTooltip()
        {
            var s = Sim.State;
            if (Sim.IsNight) return "Night. Every part of today is used: go to sleep to start tomorrow.";
            return $"{Fmt.SlotName(s.Slot)}: part {s.Slot + 1} of 3 of today. Time moves only when you choose what to do with it.";
        }

        static void SetCount(Button button, Label badge, int count)
        {
            badge.text = count > 99 ? "99+" : count.ToString();
            badge.EnableInClassList("hidden", count == 0);
            button.EnableInClassList("hud-alert--quiet", count == 0);
        }

        void ToggleGoals()
        {
            bool open = _goalsPopover.ClassListContains("hidden");
            CloseMenus();
            _goalsPopover.EnableInClassList("hidden", !open);
            if (open) BuildGoals();
            _studio.Coach?.Refresh();
        }

        public void ToggleMenu()
        {
            bool open = _menuPopover.ClassListContains("hidden");
            CloseMenus();
            _menuPopover.EnableInClassList("hidden", !open);
            if (open) BuildMenu();
            _studio.Coach?.Refresh();
        }

        public void CloseMenus()
        {
            bool wasOpen = IsMenuOpen;
            _goalsPopover.AddToClassList("hidden");
            _menuPopover.AddToClassList("hidden");
            if (wasOpen) _studio.Coach?.Refresh();
        }

        /// <summary>The goals or the menu popover is open (Doug's tutorial box steps aside).</summary>
        public bool IsMenuOpen => !_goalsPopover.ClassListContains("hidden") || !_menuPopover.ClassListContains("hidden");

        void BuildMenu()
        {
            var game = _studio.Game;
            _menuPopover.Clear();
            _menuPopover.Add(Ui.Text("MENU", "card__title"));
            _menuPopover.Add(Account(game));
            _menuPopover.Add(Ui.Divider());

            bool relaxed = game.Settings.RelaxedMinigames;
            var toggle = Ui.Clickable(() =>
            {
                game.Settings.RelaxedMinigames = !game.Settings.RelaxedMinigames;
                BuildMenu();
            }, "menu-toggle" + (relaxed ? " menu-toggle--on" : ""));
            toggle.name = "menu-relaxed";
            var track = Ui.El("switch");
            track.Add(Ui.El("switch__knob"));
            toggle.Add(track);
            var words = Ui.El("menu-toggle__words");
            words.Add(Ui.Text("Relaxed minigames", "menu-toggle__title"));
            words.Add(Ui.Text("No timers: take as long as you like. A perfect session counts a little less than a fast one.", "menu-toggle__sub"));
            toggle.Add(words);
            _menuPopover.Add(toggle);

            _menuPopover.Add(Ui.Divider());
            string saved = game.Saves == null ? "Saving is off in this session."
                : game.LastSaved.HasValue ? $"Saved automatically at {game.LastSaved.Value:HH:mm}."
                : "Your studio saves itself after every change.";
            _menuPopover.Add(Ui.Text(saved, "menu-note"));
            var buttons = Ui.Row("menu-buttons");
            bool demo = Sim.State.IsDemo;
            var howTo = Ui.Btn("How to play", _studio.ShowHowToPlay, "btn btn--small");
            howTo.name = "menu-how-to-play";
            buttons.Add(howTo);
            var tutorial = Ui.Btn("Replay the tutorial", () =>
            {
                CloseMenus();
                _studio.Coach.Restart();
            }, "btn btn--small");
            tutorial.name = "menu-tutorial";
            buttons.Add(tutorial);
            var rename = Ui.Btn("Rename your studio", _studio.ShowNameStudio, "btn btn--small");
            rename.name = "menu-rename";
            buttons.Add(rename);
            var fresh = Ui.Btn(demo ? "Restart the demo…" : "Start a new studio…", _studio.ConfirmNewStudio, "btn btn--small btn--danger");
            fresh.name = "menu-new";
            buttons.Add(fresh);
            if (demo)
            {
                var wishlist = Ui.IconBtn(Icons.Heart, "Wishlist the full game", () => GameEdition.OpenFullGameStorePage(game.Platform), "btn btn--small btn--primary", 14f);
                wishlist.name = "menu-wishlist";
                buttons.Add(wishlist);
            }
            if (!UnityEngine.Application.isEditor)
                buttons.Add(Ui.Btn("Quit", () =>
                {
                    game.SaveNow();
                    UnityEngine.Application.Quit();
                }, "btn btn--small"));
            _menuPopover.Add(buttons);
        }

        /// <summary>Who's playing (Steam, or offline and why), where the saves go, and the achievements.</summary>
        VisualElement Account(GameController game)
        {
            var platform = game.Platform;
            bool signedIn = platform.IsSignedIn;
            var box = Ui.El("menu-account");
            box.name = "menu-account";
            var head = Ui.Row("menu-account__head");
            head.Add(Ui.Tile(signedIn ? Icons.Person : Icons.Cloud, "menu-account__tile " + (signedIn ? "bg-blue" : "bg-cream"), 18f));
            var words = Ui.El("menu-account__words");
            words.Add(Ui.Text(signedIn ? $"Signed in to {platform.Name} as {platform.Player?.DisplayName}" : "Playing offline", "menu-account__title"));
            words.Add(Ui.Text(CloudStatus(game), "menu-account__sub"));
            head.Add(words);
            box.Add(head);
            if (Sim.State.IsDemo)
            {
                box.Add(Ui.Text("This is the free demo: your first two weeks. Achievements come with the full game.", "menu-account__sub"));
                return box;
            }
            var achievements = Ui.IconBtn(Icons.Trophy, $"Achievements {game.Achievements.UnlockedCount}/{AchievementCatalog.All.Count}",
                _studio.ShowAchievements, "btn btn--small menu-account__achievements", 16f);
            achievements.name = "menu-achievements";
            box.Add(achievements);
            return box;
        }

        static string CloudStatus(GameController game)
        {
            var platform = game.Platform;
            var saves = game.Saves;
            string cloud = platform.Name == "Steam" ? "Steam Cloud" : "Cloud saves";
            if (saves == null) return "Nothing is saved in this session.";
            if (!saves.HasCloud)
                return (platform.Problem != null ? platform.Problem + " " : "") + "Saves and achievements stay on this computer.";
            if (!saves.CloudEnabled) return $"{cloud} is turned off for this game, so saves stay on this computer.";
            if (saves.CloudProblem != null) return saves.CloudProblem;
            return saves.LastUploaded.HasValue ? $"{cloud}: backed up at {saves.LastUploaded.Value:HH:mm}." : $"{cloud} is on: your studio follows you to any computer.";
        }

        void BuildGoals()
        {
            var s = Sim.State;
            var chapter = GoalChapter;
            _goalsPopover.Clear();
            _goalsPopover.Add(Ui.Text(chapter.Steps.Count > 0 ? $"CHAPTER {chapter.Number}: {chapter.Title.ToUpperInvariant()}" : "WHAT'S NEXT", "card__title"));
            _goalsPopover.Add(Ui.Display(chapter.Goal, "goals-popover__goal"));
            bool nextMarked = false;
            foreach (var step in chapter.Steps)
            {
                bool done = step.IsDone(s);
                bool next = !done && !nextMarked;
                nextMarked |= next;
                _goalsPopover.Add(GoalRow(step.Text, done, next));
            }
            if (chapter.Steps.Count == 0)
                _goalsPopover.Add(Ui.Text("You've finished every chapter written so far. Hiring, publishers and a real studio arrive in a future update.", "goals-popover__note"));

            _goalsPopover.Add(Ui.Divider());
            if (s.IsDemo)
            {
                _goalsPopover.Add(Ui.Text("THE DEMO", "card__title"));
                _goalsPopover.Add(Ui.Text(s.ShippedGames > 0
                    ? "You shipped your first game: that's the demo complete."
                    : $"Your first two weeks: {StudioUI.DemoDaysLeftText(s.Day)}. Doug points at what's next, and the full game carries on from here.", "goals-popover__note"));
                return;
            }
            _goalsPopover.Add(Ui.Text($"SIDE GOALS · {s.GoalsCompleted.Count}/{GoalCatalog.All.Count}", "card__title"));
            foreach (var goal in GoalCatalog.All.Where(g => !s.GoalsCompleted.ContainsKey(g.Id)).Take(4))
                _goalsPopover.Add(GoalRow(goal.Title, false, false));
        }

        static VisualElement GoalRow(string text, bool done, bool next)
        {
            var row = Ui.Row("goal" + (done ? " goal--done" : "") + (next ? " goal--next" : ""));
            var box = Ui.El("goal__box");
            if (done) box.Add(new Icon(Icons.Check, 14f, 3.2f));
            else if (next) box.Add(new Icon(Icons.ArrowRight, 12f, 3f));
            row.Add(box);
            row.Add(Ui.Text(text, "goal__text"));
            return row;
        }

        string CashTooltip()
        {
            var fin = Sim.State.Finance;
            var next = fin.Scheduled.OrderBy(p => p.Day).FirstOrDefault();
            string pending = next == null ? "Nobody owes you money right now."
                : $"Owed to you: {Fmt.Money(fin.PendingIncome)} (next: {Fmt.Money(next.Amount)} on {GameCalendar.FormatShort(next.Day)}).";
            string debt = fin.Cash < 0 ? $"\nYou're on the credit card: 2% interest a month. The studio closes past -{Fmt.Money(fin.CreditLimit)}." : "";
            return $"Money in the bank.\n{pending}\nEvery 1st of the month: {Fmt.Money(fin.MonthlyRecurringTotal)} for rent, food, bills and subscriptions.{debt}\nClick for your finances.";
        }

        string RunwayTooltip()
        {
            var fin = Sim.State.Finance;
            return $"How long your cash lasts with no new income:\n{Fmt.Money(System.Math.Max(0, fin.Cash))} ÷ {Fmt.Money(fin.MonthlyRecurringTotal)} per month.\n" +
                   $"Money owed to you ({Fmt.Money(fin.PendingIncome)}) is not counted until it arrives.\n" +
                   $"Under {Tuning.MoneyAnxietyRunwayDays:0} days, money stress adds {Tuning.MoneyAnxietyBurnout:0} burnout every week.";
        }

        string BurnoutTooltip()
        {
            var s = Sim.State;
            var last = s.LastWeek;
            string lastWeek = last == null ? "" : $"\nLast week you worked {last.HoursWorked} hours ({last.WeekendWorkBlocks} weekend sessions).";
            return $"Burnout {s.Founder.Burnout:0}/100. Builds up above {Tuning.SustainableWeeklyHours:0} hours a week (+{Tuning.BurnoutPerHourOver} per extra hour, +{Tuning.BurnoutPerWeekendBlock} per weekend session) " +
                   $"and drops below it (-{Tuning.BurnoutRecoveryPerHourUnder} per hour). A week off recovers about 22.{lastWeek}\n" +
                   $"Right now your output is ×{Rules.BurnoutFactor(s.Founder.Burnout):0.00} from burnout. Above {Tuning.CollapseBurnout:0} you can collapse for two weeks.";
        }

        string MoraleTooltip()
        {
            var f = Sim.State.Founder;
            return $"Morale {f.Morale:0}/100 changes your output (×{Rules.MoraleFactor(f.Morale):0.00} now).\n" +
                   "Up: progress on your game, shipping, sales, friends, weekends off. Down: money stress, failed gigs, burnout, bad news.";
        }
    }

    /// <summary>The app bar along the bottom (Design/18 §1: "apps on an operating-system desktop").</summary>
    public sealed class Dock
    {
        readonly StudioUI _studio;
        readonly System.Collections.Generic.Dictionary<Page, Button> _buttons = new System.Collections.Generic.Dictionary<Page, Button>();
        readonly System.Collections.Generic.Dictionary<Page, Label> _badges = new System.Collections.Generic.Dictionary<Page, Label>();

        public Dock(StudioUI studio)
        {
            _studio = studio;
            Root = Ui.Row("dock");
        }

        public VisualElement Root { get; }

        public void Add(Page page)
        {
            var button = Ui.Clickable(() => _studio.Show(page), "dock-btn " + page.AppClass);
            button.name = "dock-" + page.AppClass.Replace("app-", "");
            var tile = Ui.El("dock-btn__tile");
            tile.Add(new Icon(page.IconPath, 24f));
            button.Add(tile);
            button.Add(Ui.Text(page.Title, "dock-btn__label"));
            var badge = Ui.Text("", "badge hidden");
            button.Add(badge);
            var lockTile = Ui.Tile(Icons.Lock, "dock-btn__lock hidden", 11f);
            lockTile.pickingMode = PickingMode.Ignore;
            button.Add(lockTile);
            _studio.Tooltip.Attach(button, () => _studio.IsLocked(page) ? $"{page.Title}: in the full game. {_studio.FullGameNote(page)}" : null);
            _buttons[page] = button;
            _badges[page] = badge;
            Root.Add(button);
        }

        public void SetActive(Page page)
        {
            foreach (var pair in _buttons) pair.Value.EnableInClassList("dock-btn--active", pair.Key == page);
        }

        public void Refresh()
        {
            var s = _studio.Sim.State;
            foreach (var pair in _buttons)
            {
                bool locked = _studio.IsLocked(pair.Key);
                pair.Value.EnableInClassList("dock-btn--locked", locked);
                pair.Value.Q(className: "dock-btn__lock")?.EnableInClassList("hidden", !locked);
            }
            foreach (var pair in _badges)
            {
                string text = "";
                switch (pair.Key)
                {
                    case InboxPage _:
                        text = s.Inbox.UnreadCount > 0 ? (s.Inbox.UnreadCount > 99 ? "99+" : s.Inbox.UnreadCount.ToString()) : "";
                        break;
                    case GigsPage _:
                        int offers = s.Freelance.Offers.Count();
                        text = offers > 0 ? offers.ToString() : "";
                        break;
                    case ProjectPage _:
                        text = s.ActiveProject != null && s.ActiveProject.Status == ProjectStatus.ReadyToRelease ? "!" : "";
                        break;
                }
                pair.Value.text = text;
                pair.Value.EnableInClassList("hidden", text.Length == 0);
            }
        }
    }
}
