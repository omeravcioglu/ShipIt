using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>The studio at a glance: runway, cash, audience, the project, this week, you, goals, mail (Design/18 §4).</summary>
    public sealed class DashboardPage : Page
    {
        public DashboardPage(StudioUI studio) : base(studio) { }

        public override string Title => "Dashboard";
        public override string IconPath => Icons.Dashboard;
        public override string AppClass => "app-dashboard";

        public override void Refresh()
        {
            Root.Clear();
            Header("Dashboard", null);
            BarNote = StageLine();

            var top = Ui.Row("grid-row");
            top.Add(RunwayCard());
            top.Add(CashCard());
            top.Add(AudienceCard());
            Root.Add(top);

            var middle = Ui.Row("grid-row");
            middle.Add(ProjectCard());
            middle.Add(WeekCard());
            middle.Add(FounderCard());
            Root.Add(middle);

            var bottom = Ui.Row("grid-row");
            bottom.Add(GoalsCard());
            bottom.Add(RecentCard());
            Root.Add(bottom);
        }

        string StageLine()
        {
            var s = State;
            if (s.ShippedGames == 0) return "Bedroom developer · nothing shipped yet";
            return $"Bedroom developer · {s.ShippedGames} game{(s.ShippedGames == 1 ? "" : "s")} shipped · {Fmt.Money(s.LifetimeGameNet)} from games";
        }

        VisualElement RunwayCard()
        {
            var fin = State.Finance;
            double runway = fin.RunwayDays();
            var card = Ui.Card("Runway", "col-5");
            var why = Ui.Chip("Why?", "chip--why card__corner");
            Why(why, () => $"{Fmt.Money(System.Math.Max(0, fin.Cash))} in the bank ÷ {Fmt.Money(fin.MonthlyRecurringTotal)} of bills a month.\nMoney owed to you isn't counted until it arrives.");
            card.Add(why);
            var line = Ui.Row("runway-line");
            line.Add(Ui.Display(Fmt.Days(runway), "big-number" + (runway < 30 ? " bad" : "")));
            line.Add(Ui.Segments(Fmt.RunwaySegments(runway), 5, Fmt.RunwayClass(runway)));
            card.Add(line);

            var forecast = CashForecast.Build(State, 90);
            forecast.Range(out double min, out double max);
            var chart = new ForecastChart();
            chart.AddToClassList("spark-chart");
            chart.SetData(forecast, min, max, 3f);
            card.Add(chart);
            var axis = Ui.Row("chart-axis");
            axis.Add(Ui.Text("Today", "chart-axis__label"));
            axis.Add(Ui.Grow());
            axis.Add(Ui.Text(forecast.BrokeIndex > 0 ? "Broke on " + GameCalendar.FormatShort(forecast.DayAt(forecast.BrokeIndex)) + " if nothing changes" : "Stays above $0 for 90 days",
                "chart-axis__label" + (forecast.BrokeIndex > 0 ? " bad" : " good")));
            axis.Add(Ui.Grow());
            axis.Add(Ui.Text("+90 days", "chart-axis__label"));
            card.Add(axis);
            card.RegisterCallback<ClickEvent>(e => Studio.Show<FinancePage>());
            return card;
        }

        VisualElement CashCard()
        {
            var fin = State.Finance;
            var card = Ui.Card("Cash", "col-3 card--mustard");
            card.Add(Ui.Display(Fmt.Money(fin.Cash), "big-number" + (fin.Cash < 0 ? " bad" : "")));
            var last = fin.Reports.LastOrDefault();
            if (last != null)
                card.Add(Ui.KeyValue($"Last month ({GameCalendar.MonthLabel(last.MonthIndex)})", Fmt.SignedMoney(last.Net), last.Net >= 0 ? "good" : "bad"));
            card.Add(Ui.KeyValue("Owed to you", Fmt.Money(fin.PendingIncome), "good"));
            card.Add(Ui.KeyValue("Bills on the 1st", Fmt.Money(fin.MonthlyRecurringTotal)));
            card.Add(Ui.Grow());
            var next = fin.Scheduled.OrderBy(p => p.Day).FirstOrDefault();
            var note = Ui.El("cash-note");
            note.Add(Ui.Text(next != null ? $"Next in: {Fmt.Money(next.Amount)} on {GameCalendar.FormatShort(next.Day)}" : "Nothing owed to you yet", "cash-note__line"));
            note.Add(Ui.Text($"Next bill: {Fmt.Money(fin.MonthlyRecurringTotal)} on the 1st", "cash-note__line"));
            card.Add(note);
            return card;
        }

        VisualElement AudienceCard()
        {
            var s = State;
            var card = Ui.Card("Audience", "col-4");
            var vapor = s.Projects.Where(p => p.Platform == StorePlatform.Vapor && !p.IsReleased && p.Status != ProjectStatus.Cancelled).OrderByDescending(p => p.Wishlists).FirstOrDefault();
            if (vapor != null)
            {
                var line = Ui.Row("runway-line");
                line.Add(Ui.Display(Fmt.Num(vapor.Wishlists), "big-number"));
                line.Add(Ui.Text("wishlists", "big-number__unit"));
                card.Add(line);
                if (s.LastWeek != null && s.LastWeek.WishlistsGained > 0)
                    card.Add(Ui.Pill(Icons.ArrowUp, $"+{Fmt.Num(s.LastWeek.WishlistsGained)} last week", "pill--good"));
                card.Add(Ui.Text("On the way to 1,000", "small muted"));
                card.Add(Ui.Bar(vapor.Wishlists / 1000.0, "fill-teal", 14));
            }
            else
            {
                var line = Ui.Row("runway-line");
                line.Add(Ui.Display(Fmt.Num(s.Audience.Followers), "big-number"));
                line.Add(Ui.Text("followers", "big-number__unit"));
                card.Add(line);
                card.Add(Ui.Text("Wishlists start once your game has a Vapor page.", "small muted"));
            }
            card.Add(Ui.Divider());
            card.Add(Ui.KeyValue("Followers", Fmt.Num(s.Audience.Followers)));
            card.Add(Ui.KeyValue("Gig rating", Fmt.Stars(s.Freelance.Profile.Rating)));
            card.Add(Ui.KeyValue("Games shipped", s.ShippedGames.ToString()));
            return card;
        }

        VisualElement ProjectCard()
        {
            var card = Ui.Card("Your game", "col-5");
            var project = State.ActiveProject;
            if (project == null)
            {
                var head = Ui.Row("card-head");
                head.Add(Ui.Tile(Icons.Gamepad, "bg-teal icon-cream tile--big", 26f));
                var text = Ui.El("grow");
                text.Add(Ui.Display(State.ShippedGames == 0 ? "No project yet" : "Between projects", "card-head__title"));
                text.Add(Ui.Text(State.ShippedGames == 0 ? "Pick an idea and start small." : "Rest, research, then pick the next idea.", "muted"));
                head.Add(text);
                card.Add(head);
                card.Add(Ui.Grow());
                card.Add(Ui.Btn("Start a project", () => Studio.Show<ProjectPage>(), "btn btn--primary"));
                return card;
            }
            var genre = GameContent.Genre(project.Genre);
            var top = Ui.Row("card-head");
            top.Add(Ui.Tile(Icons.Gamepad, "bg-teal icon-cream tile--big", 26f));
            var title = Ui.El("grow");
            title.Add(Ui.Display(project.Title, "card-head__title"));
            title.Add(Ui.Text($"{genre.Name} · {project.Scope} · {GameContent.ArtStyle(project.ArtStyle).Name}", "muted small"));
            top.Add(title);
            top.Add(Ui.Display(Fmt.Pct(project.Progress), "card-head__pct"));
            card.Add(top);
            var bar = Ui.Bar(project.Progress, "fill-stripes", 16);
            bar.Q(className: "bar__fill").style.backgroundImage = new StyleBackground(Textures.Stripes);
            card.Add(bar);
            foreach (var w in project.Workstreams)
            {
                var row = Ui.Row("stream-row");
                row.Add(Ui.Text(w.Discipline.ToString(), "stream-row__name"));
                row.Add(Ui.Bar(w.Required <= 0 ? 1 : w.Done / w.Required, Fmt.SkillClass(w.Discipline), 10));
                row.Add(Ui.Text(w.IsDone ? "done" : $"~{w.Remaining * Rules.EstimateBias(State):0.0} pw", "stream-row__left"));
                card.Add(row);
            }
            var foot = Ui.Row("card-foot");
            if (project.Status == ProjectStatus.ReadyToRelease) foot.Add(Ui.Pill(Icons.Rocket, "Ready to launch", "pill--good"));
            else foot.Add(Ui.Pill(Icons.Plug, $"{project.FeaturesDone}/{project.FeatureCount} features · ~{Sim.EstimatedSessionsRemaining(project):0} sessions left", "pill--ink"));
            if (project.HasStorePage)
                foot.Add(Ui.Pill(Icons.Star, project.Platform == StorePlatform.Vapor ? $"{Fmt.Num(project.Wishlists)} wishlists" : "On scratch.io", "pill--teal"));
            foot.Add(Ui.Grow());
            foot.Add(Ui.Btn("Open project", () => Studio.Show<ProjectPage>(), "btn btn--small"));
            card.Add(foot);
            return card;
        }

        VisualElement WeekCard()
        {
            var s = State;
            var week = s.CurrentWeek;
            var card = Ui.Card("This week so far", "col-3");
            int hours = week.HoursWorked;
            var line = Ui.Row("runway-line");
            line.Add(Ui.Display(hours + "h", "big-number" + (hours > Tuning.SustainableWeeklyHours ? " bad" : "")));
            line.Add(Ui.Text("of work", "big-number__unit"));
            card.Add(line);
            card.Add(Ui.Text($"{Tuning.SustainableWeeklyHours:0}h a week is sustainable. The week closes on Sunday night.", "small muted"));
            if (week.FreelanceHours > 0) card.Add(Ui.KeyValue("On gigs", $"{week.FreelanceHours:0}h"));
            if (week.FollowersGained > 0) card.Add(Ui.KeyValue("New followers", "+" + Fmt.Num(week.FollowersGained)));
            if (week.WeekendWorkBlocks > 0) card.Add(Ui.KeyValue("Weekend sessions", week.WeekendWorkBlocks.ToString(), "warn"));
            var last = s.LastWeek;
            if (last != null)
            {
                card.Add(Ui.Divider());
                card.Add(Ui.KeyValue("Last week", $"{last.HoursWorked}h worked"));
                card.Add(Ui.KeyValue("Burnout", (last.BurnoutChange >= 0 ? "+" : "") + last.BurnoutChange.ToString("0.0"), last.BurnoutChange > 0 ? "bad" : "good"));
                if (last.FreelanceEarned > 0) card.Add(Ui.KeyValue("Gigs delivered", Fmt.Money(last.FreelanceEarned), "good"));
            }
            card.Add(Ui.Grow());
            card.Add(Ui.Btn("Back to your day", Studio.ShowHome, "btn btn--primary"));
            return card;
        }

        VisualElement FounderCard()
        {
            var s = State;
            var f = s.Founder;
            var card = Ui.Card("You", "col-4");
            var head = Ui.Row("card-head");
            head.Add(Ui.Portrait(AvatarLook.Founder, Avatar.FounderMood(s), 74f, "bg-mustard-light portrait--big"));
            var meters = Ui.El("grow");
            meters.Add(Ui.MeterRow("Morale", f.Morale, "fill-good"));
            meters.Add(Ui.MeterRow("Burnout", f.Burnout, f.Burnout >= Tuning.VacationAdviceBurnout ? "fill-bad" : "fill-warn"));
            var output = Rules.Productivity(s);
            var outputRow = Ui.KeyValue("Output", "×" + output.Value.ToString("0.00"));
            Why(outputRow, () => output.Describe("0.00"));
            meters.Add(outputRow);
            head.Add(meters);
            card.Add(head);
            card.Add(Ui.Divider());
            foreach (var skill in SkillInfo.All.OrderByDescending(f.Get).Take(3))
            {
                var row = Ui.Row("stream-row");
                row.Add(Ui.Text(skill.ToString(), "stream-row__name"));
                row.Add(Ui.Bar(f.Get(skill) / 10.0, Fmt.SkillClass(skill), 10));
                row.Add(Ui.Text(f.Get(skill).ToString("0.0"), "stream-row__left"));
                card.Add(row);
            }
            if (f.IsResting(s.Day)) card.Add(Ui.Pill(Icons.Moon, "Resting until " + GameCalendar.FormatShort(System.Math.Max(f.ForcedRestUntilDay, f.VacationUntilDay)), "pill--ink"));
            card.Add(Ui.Grow());
            card.Add(Ui.Btn("Skills", () => Studio.Show<SkillsPage>(), "btn btn--small"));
            return card;
        }

        VisualElement GoalsCard()
        {
            var s = State;
            var chapter = Sim.CurrentChapter;
            var card = Ui.Card(chapter.Steps.Count > 0 ? $"Chapter {chapter.Number}: {chapter.Title}" : "Goals", "col-5");
            card.Add(Ui.Display(chapter.Goal, "card-head__title"));
            foreach (var step in chapter.Steps) card.Add(GoalRow(step.Text, step.IsDone(s)));
            card.Add(Ui.Divider());
            foreach (var goal in GoalCatalog.All.Where(g => !s.GoalsCompleted.ContainsKey(g.Id)).Take(2))
                card.Add(GoalRow(goal.Title, false));
            card.Add(Ui.Text($"Side goals: {s.GoalsCompleted.Count} of {GoalCatalog.All.Count} done", "small muted"));
            return card;
        }

        static VisualElement GoalRow(string title, bool done)
        {
            var row = Ui.Row("goal" + (done ? " goal--done" : ""));
            var box = Ui.El("goal__box");
            if (done) box.Add(new Icon(Icons.Check, 14f, 3.2f));
            row.Add(box);
            row.Add(Ui.Text(title, "goal__text"));
            return row;
        }

        VisualElement RecentCard()
        {
            var card = Ui.Card("Recent mail", "col-7");
            foreach (var m in State.Inbox.Messages.AsEnumerable().Reverse().Take(5))
            {
                var row = Ui.Row("recent" + (m.IsRead ? "" : " recent--unread"));
                row.Add(Senders.For(m).Portrait(34f, "recent__tile"));
                var text = Ui.El("grow recent__text");
                text.Add(Ui.Text(m.Title, "recent__title"));
                text.Add(Ui.Text(GameCalendar.FormatShort(m.Day) + (m.NeedsDecision ? " · needs a decision" : ""), "recent__date"));
                row.Add(text);
                row.Add(Ui.Chip(Fmt.KindLabel(m.Kind), "chip--kind " + Fmt.KindClass(m.Kind)));
                int id = m.Id;
                row.RegisterCallback<ClickEvent>(e =>
                {
                    Studio.Page<InboxPage>().Select(id);
                    Studio.Show<InboxPage>();
                });
                card.Add(row);
            }
            if (State.Inbox.Messages.Count == 0) card.Add(Ui.Empty("Nothing yet."));
            card.Add(Ui.Grow());
            card.Add(Ui.Btn("Open mail", () => Studio.ShowInbox(InboxFolder.All), "btn btn--small"));
            return card;
        }
    }
}
