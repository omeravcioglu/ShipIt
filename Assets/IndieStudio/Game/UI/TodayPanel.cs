using System;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Your day, always on the left (Design/02 §4): which part of the day it is, what you can spend it on,
    /// what today has achieved so far, and Sleep once the evening is used. Time never moves on its own.
    /// Flow badges show which work would keep a streak going, and a pill reminds you of a coffee waiting.
    /// </summary>
    public sealed class TodayPanel
    {
        static readonly string[] TimeClasses = { "time-morning", "time-afternoon", "time-evening", "time-night" };
        static readonly Skill[] LearnSkills = { Skill.Programming, Skill.Design, Skill.Art, Skill.Audio, Skill.Writing, Skill.Marketing, Skill.Business };

        readonly StudioUI _studio;
        string _open;

        public TodayPanel(StudioUI studio)
        {
            _studio = studio;
            Root = Ui.El("today");
            Root.name = "today";
        }

        public VisualElement Root { get; }
        /// <summary>The expanded activity ("build", "gig", "market", "learn") or null.</summary>
        public string OpenGroup => _open;
        GameSimulation Sim => _studio.Sim;
        GameState State => _studio.Sim.State;

        public void Toggle(string group)
        {
            _open = _open == group ? null : group;
            Refresh();
        }

        public void Refresh()
        {
            Root.Clear();
            var s = State;
            int slot = Sim.IsNight ? 3 : s.Slot;
            foreach (var c in TimeClasses) Root.EnableInClassList(c, c == Fmt.SlotClass(slot));
            Root.Add(Header(slot));

            var body = new ScrollView(ScrollViewMode.Vertical);
            body.AddToClassList("today__body");
            Root.Add(body);
            if (s.IsGameOver)
            {
                body.Add(Ui.Empty("The studio is closed."));
                return;
            }
            if (Sim.IsNight) BuildNight(body);
            else
            {
                var forced = Sim.ForcedActivity();
                if (forced.HasValue) BuildForced(body, forced.Value);
                else BuildChoices(body, slot);
                BuildLog(body);
            }
            Root.Add(Footer());
        }

        // ------------------------------------------------------------------ header

        VisualElement Header(int slot)
        {
            var s = State;
            var head = Ui.El("today__head");
            var top = Ui.Row("today__top");
            top.Add(Ui.Tile(Fmt.SlotIcon(slot), "today__time-tile", 28f));
            var words = Ui.El("grow");
            words.Add(Ui.Display(Fmt.SlotName(slot).ToUpperInvariant(), "today__time"));
            words.Add(Ui.Text($"{Fmt.DayMonth(s.Day)} · day {s.Day + 1}", "today__date"));
            top.Add(words);
            head.Add(top);

            var pips = Ui.Row("today__pips");
            for (int i = 0; i < 3; i++)
            {
                var pip = Ui.Row("today__pip" + (i < slot ? " today__pip--done" : i == slot ? " today__pip--now" : ""));
                pip.Add(new Icon(i < slot ? Icons.Check : Fmt.SlotIcon(i), 14f, 2.6f));
                pip.Add(Ui.Text(GameCalendar.BlockNames[i], "today__pip-label"));
                pips.Add(pip);
            }
            head.Add(pips);
            return head;
        }

        // ------------------------------------------------------------------ choosing an activity

        void BuildChoices(VisualElement body, int slot)
        {
            var s = State;
            var project = s.ActiveProject;
            body.Add(Ui.Text($"What will you do this {Fmt.SlotName(slot).ToLowerInvariant()}?", "today__question"));
            if (Sim.IsAfterWork(PlanBlock.Develop))
            {
                var tired = Ui.Pill(Icons.Coffee, $"After your shift: work counts ×{Tuning.AfterJobFatigue:0.0} tonight", "pill--warn today__tired");
                _studio.Tooltip.Attach(tired, () => "A day job takes energy as well as hours. Sessions after a shift get less done, which is why keeping the job makes your game so slow.");
                body.Add(tired);
            }
            if (s.CoffeeReady)
            {
                var coffee = Ui.Pill(Icons.Coffee, $"Coffee's ready: your next work session ×{Tuning.CoffeeBoost:0.00}", "pill--soft today__tired");
                coffee.name = "today-coffee";
                _studio.Tooltip.Attach(coffee, () => "You brewed it in your room. Building, gigs and studying use it up; resting doesn't. It goes cold overnight.");
                body.Add(coffee);
            }

            var launch = Sim.LaunchWeekGame;
            var patchable = Sim.PatchableGame;
            var fest = Sim.FestGame;
            if (fest != null) body.Add(FestCard(fest));
            if (launch != null)
            {
                body.Add(LaunchCard(launch));
                LaunchGroup(body, launch, true);
            }

            string buildSub = project == null ? "No game yet: start one in the Project app"
                : project.Status == ProjectStatus.ReadyToRelease ? $"{project.Title} is complete: polish it"
                : $"{project.Title} · {project.FeaturesDone}/{project.FeatureCount} features";
            Group(body, "build", "act-build", Icons.Plug, "act-develop", "Build your game", buildSub, BuildOptions, BuildFlow());
            if (launch == null && patchable != null) LaunchGroup(body, patchable, false);

            var active = s.Freelance.Active.OrderBy(g => g.DeadlineDay).ToList();
            string gigSub = active.Count == 0 ? "No gigs: look for clients"
                : active.Count == 1 ? $"{active[0].Title} · {Fmt.Due(s.Day, active[0].DeadlineDay)}"
                : $"{active.Count} gigs · next {Fmt.Due(s.Day, active[0].DeadlineDay)}";
            Group(body, "gig", "act-gig", Icons.Briefcase, "act-freelance", "Gig work", gigSub, GigOptions,
                active.Select(g => Sim.FlowBonusFor(PlanBlock.Gig(g.Id))).DefaultIfEmpty(0).Max());

            Group(body, "market", "act-market", Icons.Megaphone, "act-marketing", "Marketing", $"{Fmt.Num(s.Audience.Followers)} followers", MarketOptions);
            body.Add(Row("act-research", Icons.Search, "act-research", "Research", "New ideas, and a sharper read on yours", null, false, false,
                () => _studio.Play(PlanBlock.Research)));
            Group(body, "learn", "act-learn", Icons.Graduation, "act-learn", "Learn", "Study a skill: slower now, faster later", LearnOptions,
                LearnSkills.Select(k => Sim.FlowBonusFor(PlanBlock.Study(k))).Max());
            body.Add(Row("act-rest", Icons.Moon, "act-rest", "Rest", $"Burnout {s.Founder.Burnout:0}. Rest brings it down.", null, false, false,
                () => _studio.Play(PlanBlock.Rest)));
        }

        /// <summary>Launch week's choices for a released game: hotfix, answer players, post (Design/10 §7).</summary>
        void LaunchGroup(VisualElement body, Project game, bool launchWeek)
        {
            double flow = Math.Max(Sim.FlowBonusFor(PlanBlock.Hotfix(game.Id)), Sim.FlowBonusFor(PlanBlock.Answer(game.Id)));
            string sub = launchWeek
                ? $"Hotfix, answer players, or post while it's hot"
                : $"{game.Title}: patches and answers until the post-mortem";
            Group(body, "launch", "act-launch", launchWeek ? Icons.Rocket : Icons.Bug, "act-hotfix", launchWeek ? "Launch week" : "Your released game", sub,
                list => LaunchOptions(list, game, launchWeek), flow);
        }

        void LaunchOptions(VisualElement list, Project game, bool launchWeek)
        {
            var r = game.Result;
            var hotfix = PlanBlock.Hotfix(game.Id);
            var answer = PlanBlock.Answer(game.Id);
            list.Add(Option("launch-hotfix", Icons.Bug, "act-hotfix", "Hotfix " + game.Title,
                $"About {game.BugsRemaining:0} bugs left · a rushed fix can break something ({Fmt.Pct(Sim.RegressionChance)})", "Bug Squash",
                () => _studio.Play(hotfix), -1, Sim.FlowBonusFor(hotfix)));
            list.Add(Option("launch-answer", Icons.Chat, "act-community", "Answer players",
                r.Goodwill >= Tuning.GoodwillMax - 1e-6 ? "They've heard from you; it still wins followers" : $"Goodwill so far: +{r.Goodwill * 100:0} review points", "Feedback Sort",
                () => _studio.Play(answer), -1, Sim.FlowBonusFor(answer)));
            if (launchWeek)
                list.Add(Option("launch-post", Icons.Megaphone, "act-marketing", "Post about the launch", "Twice the reach, and some readers buy right away", null,
                    () => _studio.Play(PlanBlock.Post(MarketingPost.Screenshot))));
        }

        /// <summary>The morning numbers of launch week: yesterday's sales, the reviews, the critics.</summary>
        VisualElement LaunchCard(Project game)
        {
            var r = game.Result;
            int day = Sim.DaysSinceLaunch(game);
            var card = Ui.El("launch-card");
            card.name = "today-launch";
            var head = Ui.Row("launch-card__head");
            head.Add(Ui.Tile(Icons.Rocket, "launch-card__tile bg-orange", 18f));
            var words = Ui.El("grow");
            words.Add(Ui.Text($"LAUNCH WEEK · DAY {day + 1} OF {Tuning.LaunchWeekDays}", "launch-card__kicker"));
            words.Add(Ui.Display(game.Title, "launch-card__title"));
            head.Add(words);
            card.Add(head);

            var stats = Ui.Row("launch-card__stats");
            var yesterday = day > 0 && r.LaunchDays.Count >= day ? r.LaunchDays[day - 1] : null;
            stats.Add(LaunchStat(yesterday != null ? Fmt.Num(Math.Round(yesterday.Units)) : "…", day > 0 ? "copies yesterday" : "sales tonight"));
            bool reviewed = r.ReviewCount > 0;
            stats.Add(LaunchStat(Fmt.Pct(reviewed ? r.PositiveShare : r.Mood), reviewed ? $"of {r.ReviewCount} reviews" : "players' mood"));
            int scoreHub = r.ScoreHub(day);
            int published = r.CriticsBy(day).Count();
            stats.Add(LaunchStat(scoreHub >= 0 ? scoreHub.ToString() : $"{published}/{r.Critics.Count}", scoreHub >= 0 ? "ScoreHub" : "critics out"));
            card.Add(stats);

            var latest = r.CriticsBy(day).OrderByDescending(c => c.Day).FirstOrDefault();
            if (latest != null) card.Add(Ui.Text($"{latest.Outlet}, {latest.Score}: \"{latest.Quote}\"", "launch-card__quote"));
            _studio.Tooltip.Attach(card, () =>
                "Launch week: the numbers come in every morning for seven days. Reviews are written every day in the players' current mood, " +
                "so hotfixes count most in the first days, when most players arrive. Answering players wins goodwill, and posts sell copies. " +
                $"Word of mouth carries the mood into every month after (now ×{r.SalesMultiplier:0.00}).");
            return card;
        }

        static VisualElement LaunchStat(string value, string label)
        {
            var box = Ui.El("launch-card__stat");
            box.Add(Ui.Display(value, "launch-card__value"));
            box.Add(Ui.Text(label, "launch-card__label"));
            return box;
        }

        void Group(VisualElement body, string group, string name, string icon, string tileClass, string title, string sub, Action<VisualElement> options, double flow = 0)
        {
            bool open = _open == group;
            var row = Row(name, icon, tileClass, title, sub, null, true, open, () => Toggle(group));
            if (flow > 0) row.Insert(row.childCount - 1, FlowPill(flow, false));
            body.Add(row);
            if (!open) return;
            var list = Ui.El("today__options");
            options(list);
            body.Add(list);
        }

        static Button Row(string name, string icon, string tileClass, string title, string sub, string game, bool expandable, bool open, Action onClick)
        {
            var row = Ui.Clickable(onClick, "act" + (open ? " act--open" : ""));
            row.name = name;
            row.Add(Ui.Tile(icon, "act__tile " + tileClass, 20f));
            var words = Ui.El("act__words");
            words.Add(Ui.Text(title, "act__title"));
            if (!string.IsNullOrEmpty(sub)) words.Add(Ui.Text(sub, "act__sub"));
            row.Add(words);
            if (game != null) row.Add(Ui.Chip(game, "chip--ink act__game"));
            if (expandable) row.Add(new Icon(open ? Icons.ArrowDown : Icons.ArrowRight, 16f, 2.8f));
            return row;
        }

        Button Option(string name, string icon, string tileClass, string title, string sub, string game, Action onClick, double progress = -1, double flow = 0)
        {
            var option = Ui.Clickable(onClick, "opt");
            option.name = name;
            option.Add(Ui.Tile(icon, "opt__tile " + tileClass, 15f));
            var words = Ui.El("opt__words");
            if (flow > 0)
            {
                var line = Ui.Row("opt__head");
                line.Add(Ui.Text(title, "opt__title"));
                line.Add(FlowPill(flow, true));
                words.Add(line);
            }
            else words.Add(Ui.Text(title, "opt__title"));
            if (!string.IsNullOrEmpty(sub)) words.Add(Ui.Text(sub, "opt__sub"));
            if (progress >= 0)
            {
                var bar = Ui.Bar(progress, tileClass, 8);
                bar.AddToClassList("opt__bar");
                words.Add(bar);
            }
            option.Add(words);
            if (game != null)
            {
                var chip = Ui.Chip(game, "chip--ink opt__game");
                _studio.Tooltip.Attach(chip, () => "This session is played as a minigame. Play well and it gets more done.");
                option.Add(chip);
            }
            return option;
        }

        /// <summary>"You're in the zone": the bonus the next session of this work would get from flow.</summary>
        VisualElement FlowPill(double bonus, bool small)
        {
            var pill = Ui.Pill(Icons.Flame, $"Flow +{bonus * 100:0}%", "pill--flow" + (small ? " pill--flow-small" : ""));
            _studio.Tooltip.Attach(pill, () =>
                $"You're in the zone: {State.FlowStreak} session{(State.FlowStreak == 1 ? "" : "s")} in a row on this. Staying on the same work adds " +
                $"{Tuning.FlowPerSession * 100:0}% per session, up to +{Tuning.FlowMaxBonus * 100:0}%. Resting keeps the streak; other work breaks it.");
            return pill;
        }

        double BuildFlow()
        {
            var project = State.ActiveProject;
            if (project == null) return 0;
            if (project.Status == ProjectStatus.ReadyToRelease) return Sim.FlowBonusFor(PlanBlock.Develop);
            return project.Workstreams.Where(w => !w.IsDone).Select(w => Sim.FlowBonusFor(PlanBlock.Build(w.Discipline))).DefaultIfEmpty(0).Max();
        }

        void BuildOptions(VisualElement list)
        {
            var s = State;
            var project = s.ActiveProject;
            if (project == null)
            {
                list.Add(Ui.Text("You need a project to build. Pick an idea, a scope and an art style.", "opt__empty"));
                var open = Ui.Btn("Open the Project app", () => _studio.Show<ProjectPage>(), "btn btn--small");
                open.name = "today-open-project";
                list.Add(open);
                return;
            }
            if (Sim.CanWorkOnDemo(project)) list.Add(DemoOption(project));
            if (project.Status == ProjectStatus.ReadyToRelease)
            {
                list.Add(Option("build-polish", Icons.Bug, "skill-programming", "Polish: squash bugs",
                    $"About {project.BugsRemaining:0} bugs left. Launch it from the Project app.", "Bug Squash", () => _studio.Play(PlanBlock.Develop),
                    flow: Sim.FlowBonusFor(PlanBlock.Develop)));
                return;
            }
            var bottleneck = GameSimulation.Bottleneck(project);
            bool first = true;
            foreach (var w in project.Workstreams.Where(w => !w.IsDone).OrderBy(w => w == bottleneck ? 0 : 1).ThenBy(w => (int)w.Discipline))
            {
                int index = w.CurrentFeature;
                double sessions = Math.Max(1, Math.Ceiling(Sim.EstimatedSessionsForFeature(project, w)));
                string sub = $"{w.Discipline} · ~{sessions:0} session{(sessions > 1 ? "s" : "")} left" + (w == bottleneck ? " · most work left" : "");
                var discipline = w.Discipline;
                list.Add(Option(first ? "build-first" : "build-" + discipline.ToString().ToLowerInvariant(), Fmt.SkillIcon(discipline), Fmt.SkillClass(discipline),
                    w.FeatureName(index), sub, "Wire It Up", () => _studio.Play(PlanBlock.Build(discipline)), w.FeatureProgress(index),
                    Sim.FlowBonusFor(PlanBlock.Build(discipline))));
                first = false;
            }
        }

        /// <summary>Next Fest's demo, while the game is signed up and the fest hasn't started.</summary>
        Button DemoOption(Project project)
        {
            int days = project.FestDay - State.Day;
            bool ready = project.Progress >= Tuning.DemoMinProgress;
            string sub = !ready ? $"Build the game to {Fmt.Pct(Tuning.DemoMinProgress)} first"
                : $"{Fmt.Pct(project.DemoProgress)} done · Next Fest in {days} day{(days == 1 ? "" : "s")}";
            var demo = PlanBlock.Demo(project.Id);
            var option = Option("build-demo", Icons.Play, "act-demo", "Make the demo", sub, "Bug Squash", () => _studio.Play(demo),
                project.DemoProgress, Sim.FlowBonusFor(demo));
            option.SetEnabled(ready);
            _studio.Tooltip.Attach(option, () =>
                "Players try demos during Next Fest and wishlist what they like. Each session gets the demo further along and squashes the bugs " +
                $"players would hit first. If the fest started today, the demo would look {Fmt.Pct(Sim.DemoQuality(project))} good.");
            return option;
        }

        /// <summary>The week of Next Fest: wishlists and demo players so far.</summary>
        VisualElement FestCard(Project game)
        {
            int day = State.Day - game.FestDay;
            var card = Ui.El("launch-card fest-card");
            card.name = "today-fest";
            var head = Ui.Row("launch-card__head");
            head.Add(Ui.Tile(Icons.Play, "launch-card__tile act-demo", 18f));
            var words = Ui.El("grow");
            words.Add(Ui.Text($"NEXT FEST · DAY {day + 1} OF {Tuning.FestDays}", "launch-card__kicker"));
            words.Add(Ui.Display(game.Title, "launch-card__title"));
            head.Add(words);
            card.Add(head);
            var stats = Ui.Row("launch-card__stats");
            stats.Add(LaunchStat(day > 0 ? "+" + Fmt.Num(Math.Round(game.FestWishlists)) : "…", day > 0 ? "wishlists so far" : "wishlists tonight"));
            stats.Add(LaunchStat(Fmt.Num(Math.Round(game.DemoPlayers)), "demo players"));
            stats.Add(LaunchStat(Fmt.Pct(game.FestQuality), "demo quality"));
            card.Add(stats);
            _studio.Tooltip.Attach(card, () => "Next Fest week: players browse demos and wishlist what they like, most of them in the first days. " +
                                               "Posts this week bring 50% more wishlists. It's once per game.");
            return card;
        }

        void GigOptions(VisualElement list)
        {
            var s = State;
            foreach (var gig in s.Freelance.Active.OrderBy(g => g.DeadlineDay))
            {
                int id = gig.Id;
                var option = Option(gig.Client == ClientType.Friend ? "gig-friend" : "gig-" + id, Fmt.SkillIcon(gig.Discipline), Fmt.SkillClass(gig.Discipline),
                    gig.Title, $"{gig.ClientName} · {Fmt.Due(s.Day, gig.DeadlineDay)} · ~{Math.Ceiling(Sim.SessionsLeft(gig)):0} sessions left",
                    Activities.GigGameName(gig.Discipline), () => _studio.Play(PlanBlock.Gig(id)), gig.Progress, Sim.FlowBonusFor(PlanBlock.Gig(id)));
                option.EnableInClassList("opt--urgent", gig.DeadlineDay - s.Day <= 2);
                list.Add(option);
            }
            if (s.Freelance.ActiveCount < Tuning.MaxActiveGigs)
                list.Add(Option("gig-prospect", Icons.Search, "act-freelance", "Look for clients", "Half the time it turns up a new offer", null,
                    () => _studio.Play(PlanBlock.Prospect)));
            int offers = s.Freelance.Offers.Count();
            if (offers > 0)
            {
                var link = Ui.Btn(offers == 1 ? "1 offer waiting in GigGrid" : $"{offers} offers waiting in GigGrid", () => _studio.Show<GigsPage>(), "btn btn--small");
                link.name = "today-open-gigs";
                list.Add(link);
            }
        }

        void MarketOptions(VisualElement list)
        {
            var target = Sim.MarketingTarget();
            bool wishlists = target != null && target.Platform == StorePlatform.Vapor && !target.IsReleased;
            list.Add(Option("post-screenshot", Icons.Star, "act-marketing", "Post screenshots",
                wishlists ? $"Steady. The best way to grow wishlists for {target.Title}" : "Steady reach. Wishlists need a Vapor page", null,
                () => _studio.Play(PlanBlock.Post(MarketingPost.Screenshot))));
            list.Add(Option("post-devlog", Icons.Pen, "act-marketing", "Write a devlog", "Reliable. More followers, fewer wishlists", null,
                () => _studio.Play(PlanBlock.Post(MarketingPost.Devlog))));
            list.Add(Option("post-meme", Icons.Bolt, "act-marketing", "Post a meme", "Usually flops. Sometimes explodes", null,
                () => _studio.Play(PlanBlock.Post(MarketingPost.Meme))));
        }

        void LearnOptions(VisualElement list)
        {
            var f = State.Founder;
            foreach (var skill in LearnSkills)
            {
                var k = skill;
                double value = f.Get(skill);
                bool course = f.CourseHoursLeft > 0 && f.CourseSkill == skill;
                list.Add(Option("learn-" + skill.ToString().ToLowerInvariant(), Fmt.SkillIcon(skill), Fmt.SkillClass(skill), skill.ToString(),
                    $"Level {f.Level(skill)} ({value:0.0})" + (course ? $" · course: {f.CourseHoursLeft:0}h left" : ""), null,
                    () => _studio.Play(PlanBlock.Study(k)), value - Math.Floor(value), Sim.FlowBonusFor(PlanBlock.Study(k))));
            }
        }

        // ------------------------------------------------------------------ forced slots and night

        void BuildForced(VisualElement body, PlanBlock forced)
        {
            var s = State;
            var f = s.Founder;
            string title, text, button, icon, tileClass = Fmt.ActivityClass(forced.Kind);
            switch (forced.Kind)
            {
                case ActivityKind.DayJob:
                    title = f.Job == DayJob.FullTime ? "Office hours" : "Café shift";
                    text = "Your day job owns this part of the day. The salary arrives on the 1st.";
                    button = "Go to work";
                    icon = Icons.Coffee;
                    break;
                case ActivityKind.Jam:
                    title = "Game jam!";
                    text = "This weekend belongs to the jam. Wire up a prototype as fast as you can.";
                    button = "Jam!";
                    icon = Icons.Bolt;
                    break;
                default:
                    icon = Icons.Moon;
                    button = "Rest";
                    if (s.Day <= f.ForcedRestUntilDay)
                    {
                        title = "Doctor's orders";
                        text = $"You collapsed from burnout. Nothing but rest until {GameCalendar.FormatShort(f.ForcedRestUntilDay)}.";
                    }
                    else if (s.Day <= f.VacationUntilDay)
                    {
                        title = "Time off";
                        text = $"You're off until {GameCalendar.FormatShort(f.VacationUntilDay)}. Gig deadlines keep running.";
                    }
                    else
                    {
                        title = "Night out";
                        text = "You promised your friends this evening.";
                    }
                    break;
            }
            var card = Ui.El("today__forced");
            var head = Ui.Row("today__forced-head");
            head.Add(Ui.Tile(icon, "tile--big " + tileClass, 24f));
            head.Add(Ui.Display(title, "today__forced-title"));
            card.Add(head);
            card.Add(Ui.Text(text, "today__forced-text"));
            Button go;
            if (forced.Kind == ActivityKind.Rest && f.IsResting(s.Day))
            {
                // A whole day off is one decision, not three: rest through it and sleep.
                int tomorrow = s.Day + 1;
                go = Ui.IconBtn(Icons.Bed, $"Rest today → {GameCalendar.DayNames[GameCalendar.DayOfWeek(tomorrow)]} {GameCalendar.DayOfMonth(tomorrow)}",
                    () =>
                    {
                        _studio.CallItADay();
                        _studio.Sleep();
                    }, "btn btn--primary today__go", 18f);
            }
            else go = Ui.IconBtn(icon, button, () => _studio.Play(forced), "btn btn--primary today__go", 18f);
            go.name = "today-forced";
            card.Add(go);
            body.Add(card);
        }

        void BuildNight(VisualElement body)
        {
            var s = State;
            body.Add(Ui.Display("Day's done.", "today__night-title"));
            foreach (var report in s.Today) body.Add(LogRow(report));

            if (GameCalendar.DayOfWeek(s.Day) == 6)
            {
                var week = Ui.El("today__weekly");
                week.Add(Ui.Text("SLEEPING CLOSES THE WEEK", "today__weekly-title"));
                double change = WeekBurnoutChange(s);
                week.Add(Ui.KeyValue("Worked this week", $"{s.CurrentWeek.HoursWorked}h"));
                week.Add(Ui.KeyValue("Burnout", (change >= 0 ? "+" : "") + change.ToString("0.0"), change > 0 ? "bad" : "good"));
                body.Add(week);
            }

            int tomorrow = s.Day + 1;
            var sleep = Ui.IconBtn(Icons.Bed, $"Sleep → {GameCalendar.DayNames[GameCalendar.DayOfWeek(tomorrow)]} {GameCalendar.DayOfMonth(tomorrow)}",
                _studio.Sleep, "btn btn--primary btn--big today__sleep", 22f);
            sleep.name = "today-sleep";
            body.Add(sleep);
            string hint = GameCalendar.IsFirstOfMonth(tomorrow) ? $"Tomorrow is the 1st: {Fmt.Money(s.Finance.MonthlyRecurringTotal)} in bills."
                : GameCalendar.IsMonday(tomorrow) ? "Tomorrow is Monday: new gig offers arrive."
                : "Mornings bring mail, payments and news.";
            body.Add(Ui.Text(hint, "today__hint"));
        }

        /// <summary>What closing the week now would do to burnout (Sunday night preview).</summary>
        public static double WeekBurnoutChange(GameState s)
        {
            var log = s.CurrentWeek;
            double hours = log.HoursWorked;
            double delta = hours > Tuning.SustainableWeeklyHours
                ? Tuning.BurnoutPerHourOver * (hours - Tuning.SustainableWeeklyHours)
                : -Tuning.BurnoutRecoveryPerHourUnder * (Tuning.SustainableWeeklyHours - hours);
            delta += Tuning.BurnoutPerWeekendBlock * log.WeekendWorkBlocks + Tuning.BurnoutPerJamBlock * log.JamBlocks;
            if (s.Finance.RunwayDays() < Tuning.MoneyAnxietyRunwayDays) delta += Tuning.MoneyAnxietyBurnout;
            if (delta > 0 && s.Owned.Contains("ergonomic_chair")) delta *= 0.9;
            return Math.Max(-s.Founder.Burnout, Math.Min(100 - s.Founder.Burnout, delta));
        }

        void BuildLog(VisualElement body)
        {
            if (State.Today.Count == 0) return;
            body.Add(Ui.Text("EARLIER TODAY", "today__log-title"));
            foreach (var report in State.Today) body.Add(LogRow(report));
        }

        static VisualElement LogRow(SlotReport report)
        {
            var row = Ui.Row("today__log");
            row.Add(Ui.Tile(Fmt.SlotIcon(report.Slot), "today__log-tile " + (report.HasSkill ? Fmt.SkillClass(report.Skill) : Fmt.ActivityClass(report.Activity.Kind)), 13f));
            var words = Ui.El("grow");
            words.Add(Ui.Text(report.Title, "today__log-title-text"));
            string detail = report.HasProgress ? $"{Fmt.Pct(report.ProgressBefore)} → {Fmt.Pct(report.ProgressAfter)}" : report.Lines.FirstOrDefault();
            if (report.Performance > 1.001 || report.Performance < 0.999) detail = $"×{report.Performance:0.00} · " + detail;
            if (!string.IsNullOrEmpty(detail)) words.Add(Ui.Text(detail, "today__log-detail"));
            row.Add(words);
            return row;
        }

        // ------------------------------------------------------------------ footer

        VisualElement Footer()
        {
            var s = State;
            var foot = Ui.El("today__foot");
            foot.name = "today-week";
            int hours = s.CurrentWeek.HoursWorked;
            var line = Ui.Row("today__week");
            line.Add(Ui.Text("THIS WEEK", "today__week-label"));
            line.Add(Ui.Grow());
            line.Add(Ui.Text($"{hours}h of work · {Tuning.SustainableWeeklyHours:0}h is sustainable", "today__week-value" + (hours > Tuning.SustainableWeeklyHours ? " warn" : "")));
            foot.Add(line);
            var bar = Ui.Bar(hours / 60.0, hours > Tuning.SustainableWeeklyHours ? "fill-warn" : "fill-good", 10);
            var mark = Ui.El("today__week-mark");
            mark.style.left = Length.Percent((float)(Tuning.SustainableWeeklyHours / 60.0 * 100));
            bar.Add(mark);
            foot.Add(bar);
            _studio.Tooltip.Attach(foot, () => $"Every session is 4 hours. Over {Tuning.SustainableWeeklyHours:0} hours in a week, burnout builds up when the week closes on Sunday night; under it, burnout comes down. Weekend work costs a little extra.");

            if (!Sim.IsNight && !s.IsGameOver && Sim.ForcedActivity() == null)
            {
                var bottom = Ui.Row("today__foot-row");
                if (GameCalendar.IsWeekend(s.Day)) bottom.Add(Ui.Text("Weekend: work costs extra burnout", "today__weekend"));
                bottom.Add(Ui.Grow());
                var call = Ui.IconBtn(Icons.Moon, "Call it a day", _studio.CallItADay, "btn btn--small today__call", 14f);
                call.name = "today-call";
                _studio.Tooltip.Attach(call, () => "Rest for the rest of today and skip to the evening's end.");
                bottom.Add(call);
                foot.Add(bottom);
            }
            return foot;
        }
    }
}
