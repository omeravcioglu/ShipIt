using System;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// Launch week (Design/10 §7), played a slot at a time like everything else. For seven days after a release,
    /// the numbers come in every day and the player decides what the game needs:
    /// <list type="bullet">
    /// <item>Hotfixes raise stability, and with it the players' mood.</item>
    /// <item>Answering players wins goodwill and followers.</item>
    /// <item>Posts carry launch momentum and sell copies.</item>
    /// </list>
    /// Reviews are written every day in the mood of the day. Most copies sell in the first days, so early fixes
    /// count the most, and word of mouth carries the final mood into every month of sales after it.
    /// </summary>
    public sealed partial class GameSimulation
    {
        /// <summary>The game in its launch week (the newest, if two overlap), or null.</summary>
        public Project LaunchWeekGame =>
            State.ReleasedProjects.Where(p => DaysSinceLaunch(p) < Tuning.LaunchWeekDays).OrderByDescending(p => p.ReleaseDay).FirstOrDefault();

        /// <summary>The newest game that can still be patched and answered for (until its post-mortem), or null.</summary>
        public Project PatchableGame =>
            State.ReleasedProjects.Where(p => DaysSinceLaunch(p) < Tuning.PatchWindowDays).OrderByDescending(p => p.ReleaseDay).FirstOrDefault();

        /// <summary>Days since the game came out (0 on launch day); a large number for unreleased games.</summary>
        public int DaysSinceLaunch(Project project) => project == null || !project.IsReleased ? int.MaxValue : State.Day - project.ReleaseDay;

        Project ReleasedGame(PlanBlock plan) =>
            State.ReleasedProjects.FirstOrDefault(p => p.Id == plan.Option && DaysSinceLaunch(p) < Tuning.PatchWindowDays) ?? PatchableGame;

        /// <summary>Called by Release: remembers the launch build and lines up the critics.</summary>
        void StartLaunch(Project project)
        {
            var r = project.Result;
            r.LaunchStability = r.Stability;
            r.LaunchQuality = r.Quality;
            r.Mood = r.LaunchMood = r.PositiveShare;
            r.Critics.AddRange(CriticWriter.For(project, r.Quality, r.Stability));
        }

        /// <summary>How players feel after a hotfix or an answer, and the word of mouth that follows from it.</summary>
        void UpdateMood(Project project)
        {
            var r = project.Result;
            double launchPenalty = Math.Max(0.1, 1 - 0.03 * (10 - r.LaunchStability));
            r.Stability = Rules.Clamp(10 - project.BugsRemaining * 0.25, 1, 10);
            r.Quality = Rules.Clamp(r.LaunchQuality / launchPenalty * (1 - 0.03 * (10 - r.Stability)), 0.05, 0.98);
            r.Mood = Rules.Clamp(Rules.PositiveShare(r.Quality, project.Price, project.Scope) + r.Goodwill, 0.02, 0.99);
            r.SalesMultiplier = WordOfMouth(r.Mood) / WordOfMouth(r.LaunchMood);
        }

        static double WordOfMouth(double mood) => Tuning.WordOfMouthBase + Tuning.WordOfMouthSlope * mood;

        /// <summary>The chance that a hotfix breaks something else, at the founder's Programming skill.</summary>
        public double RegressionChance => Math.Max(Tuning.RegressionMin, Tuning.RegressionBase - Tuning.RegressionPerSkill * Founder.Get(Skill.Programming));

        void DoHotfix(int day, PlanBlock plan, double performance)
        {
            var project = ReleasedGame(plan);
            if (project == null)
            {
                if (_report != null) _report.Title = "Nothing to patch";
                Note("Hotfixes are for a game in its first month after launch.");
                return;
            }
            var r = project.Result;
            var f = State.Founder;
            double productivity = Rules.Productivity(State).Value;
            double moodBefore = r.Mood;
            double bugsBefore = project.BugsRemaining;
            SetSubject($"Hotfixed {project.Title}", "Review mood", Skill.Programming, moodBefore);

            double pw = Tuning.BlockHours / Tuning.PersonWeekHours * Rules.Efficiency(f.Get(Skill.Programming)) * productivity * performance;
            double fixedBugs = Math.Min(bugsBefore, pw * Tuning.BugsFixedPerPolishPw);
            project.BugsFixed += fixedBugs;
            r.HotfixedBugs += fixedBugs;
            r.Hotfixes++;

            // A rushed fix can break something else; careful sessions and skill make it rarer.
            double regression = RegressionChance * (performance < 1 ? 1.5 : 1);
            if (State.RngWork.Chance(regression))
            {
                double broken = Math.Round(State.RngWork.Range(2.0, 5.0));
                project.BugsCreated += broken;
                r.Regressions++;
                Note($"The patch broke something else: {broken:0} new bugs. Rushed fixes do that.");
            }

            UpdateMood(project);
            if (_report != null) _report.ProgressAfter = r.Mood;
            Note($"Squashed {fixedBugs:0} bugs ({project.BugsRemaining:0} left). Players' mood: {Rules.Pct(moodBefore)} → {Rules.Pct(r.Mood)} positive.");
            if (DaysSinceLaunch(project) <= 1) Note("Early fixes count the most: most players are still arriving.");
            GainXp(Skill.Programming, Tuning.BlockHours * productivity);
        }

        void DoCommunity(int day, PlanBlock plan, double performance)
        {
            var project = ReleasedGame(plan);
            if (project == null)
            {
                if (_report != null) _report.Title = "No players to answer yet";
                Note("Answering players is for a game in its first month after launch.");
                return;
            }
            var r = project.Result;
            double moodBefore = r.Mood;
            SetSubject($"Answered players about {project.Title}", "Review mood", Skill.Marketing, moodBefore);
            double gain = Math.Max(0, Math.Min(Tuning.GoodwillMax - r.Goodwill, Tuning.GoodwillPerAnswer * performance));
            r.Goodwill += gain;
            r.Answers++;
            UpdateMood(project);
            if (_report != null) _report.ProgressAfter = r.Mood;

            bool launchWeek = DaysSinceLaunch(project) < Tuning.LaunchWeekDays;
            double followers = Math.Max(2, State.Audience.Followers * 0.02) * performance * (launchWeek ? 1.5 : 1);
            State.Audience.Followers += followers;
            State.CurrentWeek.FollowersGained += followers;
            Note(gain > 1e-6
                ? $"Players notice you listen: mood {Rules.Pct(moodBefore)} → {Rules.Pct(r.Mood)} positive."
                : "They've heard from you. More answers won't move the reviews now.");
            Note($"+{followers:0} followers ({State.Audience.Followers:#,0} total)");
            GainXp(Skill.Marketing, Tuning.BlockHours * 0.6);
            GainXp(Skill.Writing, Tuning.BlockHours * 0.4);
        }

        /// <summary>A post during launch week: it reaches further, and some of the people it reaches buy right away.</summary>
        double LaunchPush(Project launch, double followers, double luck, int day)
        {
            var r = launch.Result;
            double priceFactor = Math.Pow(Rules.FairPrice(launch.Scope) / Math.Max(0.99, launch.Price), 0.8);
            double wishlisters = launch.Platform == StorePlatform.Vapor ? r.LaunchWishlists * Tuning.LaunchPushWishlistRate * luck : 0;
            double units = (followers * Tuning.LaunchPushBuyRate + wishlisters) * (0.5 + r.Quality) * priceFactor;
            SellCopies(launch, units, day);
            r.PushUnits += units;
            return units;
        }

        /// <summary>Sells copies of a released game today: money, and reviews written in the players' current mood.</summary>
        void SellCopies(Project project, double units, int day)
        {
            var r = project.Result;
            int t = day - project.ReleaseDay;
            bool discounted = project.Platform == StorePlatform.Vapor && t < Tuning.LaunchDiscountDays;
            double gross = units * project.Price * (discounted ? 1 - Tuning.LaunchDiscount : 1);
            double fee = project.Platform == StorePlatform.Vapor ? Tuning.VaporPlatformFee : Tuning.ScratchPlatformFee;
            double net = gross * (1 - Tuning.RefundRate) * (1 - fee);

            project.UnitsSold += units;
            project.GrossRevenue += gross;
            project.NetRevenue += net;
            project.NetUnpaid += net;
            State.LifetimeGameNet += net;
            r.ReviewCount = (int)(project.UnitsSold * Tuning.ReviewRate);
            double reviews = units * Tuning.ReviewRate;
            r.ReviewVotes += reviews;
            r.PositiveVotes += reviews * r.Mood;
            double share = r.ReviewVotes > 0 ? r.PositiveVotes / r.ReviewVotes : r.Mood;
            r.PositiveShare = Math.Abs(share - r.Mood) < 1e-9 ? r.Mood : share;

            if (t >= 0 && t < Tuning.LaunchWeekDays)
            {
                while (r.LaunchDays.Count <= t) r.LaunchDays.Add(new LaunchDay());
                var log = r.LaunchDays[t];
                log.Units += units;
                log.Net += net;
                log.PositiveShare = r.PositiveShare;
                log.Reviews = r.ReviewCount;
            }

            if (project.UnitsSold >= 1 && State.Flags.Add("first_sale"))
            {
                State.Founder.Morale = Rules.Clamp(State.Founder.Morale + 5, 0, 100);
                Post("first_sale", MessageKind.Info, "Your first sale!",
                    $"Someone, somewhere, paid ${project.Price:0.00} for {project.Title}. Morale +5.");
            }
        }

        /// <summary>The morning after the seventh day: how launch week went.</summary>
        void FinishLaunchWeeks(int day)
        {
            foreach (var project in State.ReleasedProjects.Where(p => day - p.ReleaseDay == Tuning.LaunchWeekDays).ToList())
            {
                var r = project.Result;
                double units = r.LaunchDays.Sum(d => d.Units);
                double net = r.LaunchDays.Sum(d => d.Net);
                int scoreHub = r.ScoreHub(Tuning.LaunchWeekDays);
                string reviews = r.ReviewCount == 0 ? "none yet"
                    : r.ReviewCount < 10 ? $"{r.ReviewCount} so far, too few for a score"
                    : $"{r.ReviewLabel} ({Rules.Pct(r.PositiveShare)} of {r.ReviewCount})";
                string critics = scoreHub >= 0 ? $" ScoreHub: {scoreHub} ({ReleaseResult.ScoreHubLabel(scoreHub).ToLowerInvariant()})."
                    : r.Critics.Count > 0 ? $" {r.Critics[0].Outlet} gave it {r.Critics[0].Score}." : "";
                var work = new System.Collections.Generic.List<string>();
                if (r.Hotfixes > 0) work.Add($"{r.Hotfixes} hotfix{(r.Hotfixes == 1 ? "" : "es")} ({r.HotfixedBugs:0} bugs)");
                if (r.Answers > 0) work.Add($"answered players {r.Answers} time{(r.Answers == 1 ? "" : "s")}");
                if (r.PushUnits >= 0.5) work.Add($"{r.PushUnits:0} copies sold by launch posts");
                string effort = work.Count > 0 ? "\nYour launch week: " + string.Join(", ", work) + "." : "\nYou let the game speak for itself this week.";
                string mouth = Math.Abs(r.SalesMultiplier - 1) < 0.005 ? ""
                    : $"\nWord of mouth: from here on the game sells ×{r.SalesMultiplier:0.00} what it would have.";
                Post("launch_week_over", MessageKind.Info, $"Launch week is over: {project.Title}",
                    $"{units:#,0} copies in seven days, {Rules.Money(net)} earned. Reviews: {reviews}.{critics}{effort}{mouth}\nThe post-mortem comes on day 30.",
                    project.Id);
            }
        }
    }
}
