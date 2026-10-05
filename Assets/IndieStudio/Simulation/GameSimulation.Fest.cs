using System;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// Vapor Next Fest and demos (Design/10 §4–5): three times a year (February, June, October), unreleased games
    /// show a demo for a week and collect wishlists. It's **once per game**, so going in too early (a rough game,
    /// an unfinished demo) wastes the one chance; the demo costs Build sessions the game itself doesn't get.
    /// Signing up closes four weeks before the fest. The demo is made in sessions (Bug Squash on the demo build):
    /// each one gets it further along and squashes the bugs players would hit in the first twenty minutes.
    /// </summary>
    public sealed partial class GameSimulation
    {
        static readonly int[] FestMonths = { 2, 6, 10 };

        /// <summary>The day a Next Fest starts: the first Monday on or after the 10th of its month (Year 1 = 2001).</summary>
        public static int FestStart(int year, int month)
        {
            var date = new DateTime(2000 + year, month, 10);
            while (date.DayOfWeek != System.DayOfWeek.Monday) date = date.AddDays(1);
            return (int)(date - GameCalendar.ToDate(0)).TotalDays;
        }

        /// <summary>The next Next Fest that's still open for sign-ups on <paramref name="day"/>.</summary>
        public static int UpcomingFest(int day)
        {
            for (int year = GameCalendar.Year(day); ; year++)
                foreach (int month in FestMonths)
                {
                    int start = FestStart(year, month);
                    if (start - Tuning.FestSignUpDays >= day) return start;
                }
        }

        /// <summary>The fest a game is in right now (its week), or false.</summary>
        public bool InFest(Project project) =>
            project != null && project.FestDay >= 0 && !project.FestDone && State.Day >= project.FestDay && State.Day < project.FestDay + Tuning.FestDays;

        /// <summary>The game showing its demo at Next Fest this week, or null.</summary>
        public Project FestGame => State.Projects.FirstOrDefault(InFest);

        /// <summary>A game signed up for a fest that hasn't started yet: its demo can be worked on.</summary>
        public bool CanWorkOnDemo(Project project) =>
            project != null && !project.IsReleased && project.Status != ProjectStatus.Cancelled && project.FestDay > State.Day && !project.FestDone;

        public bool SignUpForNextFest(int projectId, out string error)
        {
            error = null;
            var project = State.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null || project.IsReleased || project.Status == ProjectStatus.Cancelled) { error = "Next Fest is for unreleased games."; return false; }
            if (project.Platform != StorePlatform.Vapor) { error = "Next Fest is on Vapor: open a Vapor page first."; return false; }
            if (project.FestDone) { error = "Next Fest is once per game, and this one has had its turn."; return false; }
            if (project.FestDay >= 0) { error = "Already signed up."; return false; }
            project.FestDay = UpcomingFest(State.Day);
            var date = GameCalendar.ToDate(project.FestDay);
            Post("fest_signup", MessageKind.Info, $"Signed up for the {GameCalendar.MonthNames[date.Month - 1]} Next Fest",
                $"{project.Title} is in Next Fest from {GameCalendar.FormatShort(project.FestDay)} to {GameCalendar.FormatShort(project.FestDay + Tuning.FestDays - 1)}. " +
                $"Players will try a demo of it: make the demo before then (Build your game → Make the demo). " +
                "A better game and a polished demo bring more wishlists. An unfinished demo still helps a little; no demo helps very little. It's once per game.",
                project.Id);
            return true;
        }

        /// <summary>How good the demo would look if the fest started today (0–1): the game as built so far, how finished the demo is, and its bugs.</summary>
        public double DemoQuality(Project project)
        {
            if (project == null || project.DemoProgress <= 0) return 0;
            var built = Rules.EvaluateQuality(State, project, Rules.FairPrice(project.Scope));
            double withoutGameBugs = built.Quality / Math.Max(0.1, 1 - 0.03 * (10 - built.Stability));
            double demoStability = Rules.Clamp(10 - project.DemoBugs * 0.25, 1, 10);
            return Rules.Clamp(withoutGameBugs * (1 - 0.03 * (10 - demoStability)) * (0.55 + 0.45 * project.DemoProgress), 0.02, 0.98);
        }

        void DoDemo(int day, PlanBlock plan, double performance)
        {
            var project = State.Projects.FirstOrDefault(p => p.Id == plan.Option) ?? State.ActiveProject;
            if (!CanWorkOnDemo(project))
            {
                if (_report != null) _report.Title = "No demo to make";
                Note("Demos are for a game signed up for an upcoming Next Fest.");
                return;
            }
            if (project.Progress < Tuning.DemoMinProgress)
            {
                if (_report != null) _report.Title = "Too little to show";
                Note($"Build more of the game first: a demo needs it at least {Rules.Pct(Tuning.DemoMinProgress)} done.");
                return;
            }
            var f = State.Founder;
            double productivity = Rules.Productivity(State).Value;
            if (project.DemoProgress <= 0) project.DemoBugs = project.BugsRemaining * Tuning.DemoBugShare;
            double before = project.DemoProgress;
            SetSubject($"Worked on the {project.Title} demo", "Demo", Skill.Design, before);
            double effort = Rules.Efficiency(Math.Max(f.Get(Skill.Programming), f.Get(Skill.Design))) * productivity * performance;
            project.DemoProgress = Math.Min(1, project.DemoProgress + Tuning.DemoProgressPerSession * effort);
            double fixedBugs = Math.Min(project.DemoBugs, Tuning.DemoBugsFixedPerSession * effort);
            project.DemoBugs -= fixedBugs;
            if (_report != null) _report.ProgressAfter = project.DemoProgress;

            int daysLeft = project.FestDay - State.Day;
            Note(project.DemoProgress >= 1 && before < 1
                ? $"The demo is ready! {project.DemoBugs:0} bugs left in it; more sessions polish it further."
                : $"Demo {Rules.Pct(project.DemoProgress)} ready, {project.DemoBugs:0} bugs left in it.");
            Note($"Next Fest starts in {daysLeft} day{(daysLeft == 1 ? "" : "s")}. The demo would look {Rules.Pct(DemoQuality(project))} good today.");
            GainXp(Skill.Design, Tuning.BlockHours * productivity * 0.6);
            GainXp(Skill.Programming, Tuning.BlockHours * productivity * 0.4);
        }

        /// <summary>The morning a fest starts: the demo goes live and what it will bring is settled.</summary>
        void StartFests(int day)
        {
            foreach (var project in State.Projects.Where(p => p.FestDay == day && !p.FestDone && !p.IsReleased && p.Status != ProjectStatus.Cancelled).ToList())
            {
                var genre = GameContent.Genre(project.Genre);
                project.FestQuality = DemoQuality(project);
                double appeal = project.DemoProgress <= 0 ? Tuning.FestNoDemoShare : Rules.Clamp(Math.Pow(project.FestQuality / 0.55, 2), 0.1, 3);
                double audience = 1 + State.Audience.Followers / 3000;
                project.FestTotal = Tuning.FestBaseWishlists * appeal * (genre.Market / 3.0) * audience * State.RngMarketing.LogNormal(1, 0.45);
                string demo = project.DemoProgress <= 0 ? "There's no demo, so it's just your page in the list."
                    : project.DemoProgress < 1 ? $"Your demo is {Rules.Pct(project.DemoProgress)} finished: it goes up as it is."
                    : "Your demo is live.";
                Post("fest_start", MessageKind.Info, $"Next Fest starts today: {project.Title}",
                    $"For seven days, players browse hundreds of demos and wishlist what they like. {demo} Posts this week add more wishlists than usual.", project.Id);
            }
        }

        /// <summary>Each night of the fest: the day's wishlists. The busiest days are the first ones.</summary>
        void ProcessFests(int day)
        {
            double totalWeight = Enumerable.Range(0, Tuning.FestDays).Sum(d => Math.Exp(-d / 3.0));
            foreach (var project in State.Projects.Where(p => p.FestDay >= 0 && !p.FestDone && p.Status != ProjectStatus.Cancelled && day >= p.FestDay && day < p.FestDay + Tuning.FestDays).ToList())
            {
                int t = day - project.FestDay;
                double wishlists = project.FestTotal * Math.Exp(-t / 3.0) / totalWeight;
                project.Wishlists += wishlists;
                project.FestWishlists += wishlists;
                project.DemoPlayers += project.DemoProgress <= 0 ? 0 : wishlists * Tuning.DemoPlayersPerWishlist;
                State.CurrentWeek.WishlistsGained += wishlists;
                if (t < Tuning.FestDays - 1) continue;
                project.FestDone = true;
                string players = project.DemoPlayers >= 1 ? $" {project.DemoPlayers:#,0} people played your demo." : "";
                Post("fest_over", MessageKind.Info, $"Next Fest is over: {project.Title}",
                    $"+{project.FestWishlists:#,0} wishlists in seven days ({project.Wishlists:#,0} now).{players} " +
                    (project.FestQuality >= 0.6 ? "A strong showing: players and streamers noticed." : project.FestQuality >= 0.4 ? "A decent showing." : "A quiet week: the demo didn't stand out.") +
                    " Next Fest is once per game.", project.Id);
            }
        }

        /// <summary>A game released before its fest leaves it (Next Fest is for unreleased games). True if it did.</summary>
        bool LeaveFestOnRelease(Project project)
        {
            if (project.FestDay < 0 || project.FestDone) return false;
            if (State.Day >= project.FestDay)
            {
                project.FestDone = true; // released during its fest week: the fest ends there
                return false;
            }
            project.FestDay = -1;
            return true;
        }
    }
}
