using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>
    /// The free demo (DemoRules): chapter 1 in 10–15 minutes, with a win every couple of minutes. Tiny games and scratch.io only, two weeks of calendar,
    /// and it ends with the first launch (or when the two weeks are up). A scripted "typical player" plays it on many
    /// seeds, and a simple playtime model turns the sessions into minutes.
    /// </summary>
    public class DemoTests
    {
        static GameSimulation NewDemo(ulong seed = 7) => new GameSimulation(seed, Edition.Demo);

        static Gig Sam(GameSimulation sim) => sim.State.Freelance.Gigs.First(g => g.Client == ClientType.Friend);

        static void ChooseDefaults(GameSimulation sim)
        {
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
        }

        // ------------------------------------------------------------------ the rules

        [Test]
        public void Demo_StartsLikeTheFullGame()
        {
            var sim = NewDemo();
            Assert.IsTrue(sim.IsDemo);
            Assert.AreEqual(DemoEnd.None, sim.State.DemoEnd);
            Assert.AreEqual(5000, sim.Finance.Cash, 1e-9);
            Assert.AreEqual(2, sim.State.Ideas.Count);
            Assert.AreEqual(180, Sam(sim).Pay, 1e-9);
            StringAssert.Contains("two weeks", sim.State.Inbox.Messages.First(m => m.EventId == "welcome").Body);
            Assert.IsFalse(new GameSimulation(7).IsDemo, "The full game is the default");
        }

        [Test]
        public void Demo_HasTinyGamesOnly_AndTheyAreSmaller()
        {
            var demo = NewDemo();
            var full = new GameSimulation(7);
            var idea = demo.State.Ideas[0];
            Assert.IsNull(demo.StartProject(idea.Id, ProjectScope.Small, ArtStyleId.PixelArt, out string error));
            StringAssert.Contains("full game", error);

            double demoWork = demo.PlanProject(idea, ProjectScope.Tiny, ArtStyleId.PixelArt).TotalRequired;
            double fullWork = full.PlanProject(full.State.Ideas[0], ProjectScope.Tiny, ArtStyleId.PixelArt).TotalRequired;
            Assert.AreEqual(fullWork * DemoRules.TinyWorkScale, demoWork, 1e-9, "Same idea, same seed, the demo's share of the work");
            Assert.IsNotNull(demo.StartProject(idea.Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out error), error);
        }

        [Test]
        public void Demo_LaunchesOnScratchIoOnly()
        {
            var sim = NewDemo();
            var project = sim.StartProject(sim.State.Ideas[0].Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            while (project.Progress < 0.3)
            {
                if (sim.IsNight) sim.Sleep();
                else sim.PerformSlot(PlanBlock.Develop);
            }
            Assert.IsFalse(sim.OpenStorePage(project.Id, StorePlatform.Vapor, out string error));
            StringAssert.Contains("full game", error);
            Assert.IsFalse(sim.SignUpForNextFest(project.Id, out _));
            Assert.IsTrue(sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out error), error);
        }

        [Test]
        public void Demo_EndsWithTheFirstLaunch_AndTimeStops()
        {
            var sim = NewDemo();
            var run = DemoPlayer.Play(sim, 1.0);
            Assert.IsTrue(run.Shipped, "The typical player ships within the demo's two weeks");
            Assert.AreEqual(DemoEnd.Shipped, sim.State.DemoEnd);
            Assert.IsTrue(sim.State.DemoOver);
            Assert.AreEqual(1, sim.State.ShippedGames);
            Assert.IsTrue(sim.State.ChaptersCompleted.ContainsKey("bedroom"), "Shipping completes chapter 1, the demo's goal");

            int day = sim.Day, slot = sim.Slot;
            Assert.IsNull(sim.PerformSlot(PlanBlock.Develop), "Nothing more happens once the demo is over");
            sim.Sleep();
            sim.AdvanceDays(3);
            Assert.AreEqual(day, sim.Day);
            Assert.AreEqual(slot, sim.Slot);
        }

        [Test]
        public void Demo_EndsAfterTwoWeeks_WithAHeadsUpThreeDaysBefore()
        {
            var sim = NewDemo();
            for (int i = 0; i < DemoRules.Days - DemoRules.WarningDaysLeft - 1; i++) sim.Sleep();
            Assert.IsFalse(sim.State.Inbox.Messages.Any(m => m.EventId == "demo_days_left"));
            sim.Sleep(); // the morning of Friday the 12th
            Assert.AreEqual(DemoRules.Days - DemoRules.WarningDaysLeft, sim.Day);
            var warning = sim.State.Inbox.Messages.Single(m => m.EventId == "demo_days_left");
            StringAssert.Contains("Sunday the 14th", warning.Body);

            while (sim.Day < DemoRules.Days - 1)
            {
                Assert.AreEqual(DemoEnd.None, sim.State.DemoEnd);
                sim.Sleep();
            }
            Assert.AreEqual(13, sim.Day, "Sunday the 14th is the last day");
            Assert.AreEqual(0, DemoRules.DaysLeft(DemoRules.Days));
            Assert.AreEqual(1, DemoRules.DaysLeft(sim.Day));
            Assert.IsNotNull(sim.PerformSlot(PlanBlock.Rest), "The last day can still be played");
            sim.Sleep();
            Assert.AreEqual(DemoEnd.OutOfTime, sim.State.DemoEnd);
            Assert.AreEqual(DemoRules.Days, sim.Day);
            Assert.IsFalse(sim.State.DayStarted, "The morning after the demo never starts");
            Assert.IsNull(sim.PerformSlot(PlanBlock.Rest));
        }

        [Test]
        public void Demo_SavesAndLoads_WithItsEnding()
        {
            var sim = NewDemo(11);
            DemoPlayer.Play(sim, 1.0);
            var stream = new MemoryStream();
            sim.Save(stream);
            stream.Position = 0;
            var loaded = GameSimulation.Load(stream);
            Assert.AreEqual(Edition.Demo, loaded.State.Edition);
            Assert.AreEqual(DemoEnd.Shipped, loaded.State.DemoEnd);
            Assert.IsNull(loaded.PerformSlot(PlanBlock.Develop));

            var full = new GameSimulation(11);
            stream = new MemoryStream();
            full.Save(stream);
            stream.Position = 0;
            Assert.AreEqual(Edition.Full, GameSimulation.Load(stream).State.Edition);
        }

        [Test]
        public void FullGame_KeepsSmallGamesVaporAndTime()
        {
            var sim = new GameSimulation(7);
            var project = sim.StartProject(sim.State.Ideas[0].Id, ProjectScope.Small, ArtStyleId.PixelArt, out string error);
            Assert.IsNotNull(project, error);
            sim.AdvanceDays(20);
            Assert.AreEqual(20, sim.Day, "The full game's calendar doesn't stop at two weeks");
            Assert.AreEqual(DemoEnd.None, sim.State.DemoEnd);
            Assert.IsFalse(sim.State.DemoOver);
        }

        // ------------------------------------------------------------------ how long it takes, and how it feels

        /// <summary>
        /// The demo's length and rhythm, played on 40 seeds by three kinds of player. The goal: 10–15 minutes, the first
        /// game shipped well inside the two weeks, a win (a finished feature, a level-up, a payment, a goal, an
        /// achievement) at least every two minutes, and a stretch goal that most players reach but a careless one can
        /// miss.
        /// </summary>
        [Test]
        public void Demo_TakesTenToFifteenMinutes_WithAWinEveryCoupleOfMinutes()
        {
            var typical = Enumerable.Range(1, 40).Select(seed => DemoPlayer.Play(NewDemo((ulong)seed), 1.0)).ToList();
            var careful = Enumerable.Range(1, 40).Select(seed => DemoPlayer.Play(NewDemo((ulong)seed), 1.15)).ToList();
            var careless = Enumerable.Range(1, 40).Select(seed => DemoPlayer.Play(NewDemo((ulong)seed), 0.8, restEvenings: true)).ToList();

            foreach (var (name, runs) in new[] { ("typical (×1.0)", typical), ("careful (×1.15)", careful), ("careless (×0.8, evenings off)", careless) })
            {
                var minutes = runs.Select(r => r.Minutes).OrderBy(m => m).ToList();
                TestContext.WriteLine(
                    $"{name,-30} shipped {runs.Count(r => r.Shipped)}/{runs.Count} · featured {runs.Count(r => r.Featured)}/{runs.Count} · " +
                    $"ships on day {Median(runs.Where(r => r.Shipped).Select(r => (double)r.Day + 1)):0.0} · " +
                    $"build sessions {Median(runs.Select(r => (double)r.BuildSessions)):0} for {Median(runs.Select(r => (double)r.FeaturesBuilt)):0} features · " +
                    $"gig sessions {Median(runs.Select(r => (double)r.GigSessions)):0} · level-ups {Median(runs.Select(r => (double)r.LevelUps)):0} · " +
                    $"achievements {Median(runs.Select(r => (double)r.Achievements)):0} · wins {Median(runs.Select(r => (double)r.Rewards.Count)):0} · " +
                    $"longest wait {Median(runs.Select(r => r.LongestWait)):0} s (worst {runs.Max(r => r.LongestWait):0}) · " +
                    $"about {Median(minutes):0.0} min (range {minutes.First():0.0}–{minutes.Last():0.0})");
            }
            var example = typical[0];
            TestContext.WriteLine("A typical run, minute by minute: " +
                                  string.Join(" · ", example.Rewards.Select(r => $"{r.Seconds / 60:0.0} {r.What}")));

            Assert.AreEqual(typical.Count, typical.Count(r => r.Shipped), "Everyone who plays normally ships");
            Assert.AreEqual(careless.Count, careless.Count(r => r.Shipped), "Even a careless player ships inside the two weeks");
            Assert.That(Median(typical.Select(r => r.Minutes)), Is.InRange(10.0, 15.0), "A typical playthrough lasts 10–15 minutes");
            Assert.That(Median(careful.Select(r => r.Minutes)), Is.GreaterThanOrEqualTo(9.0), "…a careful one isn't over in a flash");
            Assert.That(Median(careless.Select(r => r.Minutes)), Is.LessThanOrEqualTo(16.5), "…and a careless one doesn't drag");
            Assert.That(typical.Max(r => r.Day), Is.LessThan(DemoRules.Days - DemoRules.WarningDaysLeft), "...and ships before the three-days-left note");

            Assert.That(typical.All(r => r.LongestWait <= 120), $"A win at least every two minutes (worst wait {typical.Max(r => r.LongestWait):0} s)");
            Assert.That(Median(typical.Select(r => (double)r.LevelUps)), Is.GreaterThanOrEqualTo(2), "Level-ups happen in the demo");
            Assert.That(typical.All(r => r.FeaturesBuilt >= r.BuildSessions * 0.8), "Most Build sessions finish a feature");
            Assert.AreEqual(typical.Count, typical.Count(r => r.Featured), "The stretch goal is reachable: a typical player gets featured");
            Assert.That(careless.Count(r => r.Featured), Is.LessThan(careless.Count), "…but it's a real goal: a careless player can miss it");
        }

        [Test]
        public void Demo_StretchGoal_LaunchByTheFirstSundayToGetFeatured()
        {
            Project Ready(GameSimulation sim)
            {
                var project = sim.StartProject(sim.State.Ideas[0].Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
                for (int i = 0; project.Status == ProjectStatus.InDevelopment && i < 120; i++)
                {
                    if (sim.IsNight) sim.Sleep();
                    ChooseDefaults(sim);
                    sim.PerformSlot(PlanBlock.Develop);
                }
                if (sim.IsNight) sim.Sleep();
                sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out _);
                return project;
            }

            var early = NewDemo(5);
            var onTime = Ready(early);
            Assert.Less(early.Day, DemoRules.FeatureByDay);
            Assert.IsTrue(early.Release(onTime.Id, 2.99, out _));
            Assert.IsTrue(early.DemoFeatured, "Out by Sunday the 7th: featured");
            Assert.AreEqual(DemoRules.FeatureFollowers, early.State.Audience.Followers, 1e-9, "…with new followers");
            StringAssert.Contains(DemoRules.FeatureOutlet, early.State.Inbox.Messages.Last(m => m.EventId == "launch").Body);

            var late = NewDemo(5);
            var lateGame = Ready(late);
            while (late.Day < DemoRules.FeatureByDay) late.Sleep();
            Assert.IsTrue(late.Release(lateGame.Id, 2.99, out _));
            Assert.IsFalse(late.DemoFeatured, "Out on Monday the 8th: too late for the feature");
            Assert.Greater(GameSimulation.ExpectedLaunchDayUnits(onTime), GameSimulation.ExpectedLaunchDayUnits(lateGame) * 1.3,
                "The feature brings more copies on launch day");

            var full = new GameSimulation(5);
            var fullGame = Ready(full);
            Assert.IsTrue(full.Release(fullGame.Id, 2.99, out _));
            Assert.IsFalse(full.DemoFeatured, "The stretch goal is the demo's");
        }

        [Test]
        public void Demo_QuickStudy_FirstLevelUpComesWithSamsGig()
        {
            var sim = NewDemo(3);
            var sam = Sam(sim);
            Assert.IsTrue(sim.AcceptGig(sam.Id, out _));
            Assert.AreEqual(2, sim.Founder.Level(Skill.Programming));
            for (int i = 0; sam.Status == GigStatus.Active && i < 6; i++)
            {
                if (sim.IsNight) sim.Sleep();
                sim.PerformSlot(PlanBlock.Gig(sam.Id));
            }
            Assert.AreEqual(GigStatus.Completed, sam.Status);
            Assert.AreEqual(2, sam.Sessions, "Sam's gig is two sessions in the demo");
            Assert.AreEqual(3, sim.Founder.Level(Skill.Programming), "…and delivering it levels up Programming");
            Assert.AreEqual(2, new GameSimulation(3).Founder.Level(Skill.Programming), "The full game starts the same levels…");
            Assert.AreEqual(2.0, new GameSimulation(3).Founder.Get(Skill.Programming), 1e-9, "…without the head start");

            sim.Sleep();
            sim.Sleep();
            Assert.IsTrue(sim.State.Inbox.Messages.Any(m => m.EventId == "payment" && m.Title.Contains("$180")), "Two days later, Sam pays: a payment note");
        }

        static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return double.NaN;
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }

        /// <summary>
        /// Plays the demo the way Doug's tutorial and guide lead a new player: Sam's gig on the first morning, the game
        /// started that afternoon, then Sam's gig finished, then every feature built, a scratch.io page and a launch.
        /// Decisions take their default answers. A simple model of real time turns it into minutes, and every win the
        /// game celebrates (a toast, a banner or confetti) is noted with the minute it happens.
        /// </summary>
        public static class DemoPlayer
        {
            // A simple model of real time per action, in seconds (the minigame lengths come from the game).
            const double WireItUpSeconds = 35;        // a Tiny board solved early; the timer allows ~47 s
            const double BugSquashSeconds = 27;       // timed, runs its full length
            const double SessionOverheadSeconds = 15; // pick it in your day, the intro card, the result card
            const double DaySeconds = 12;             // sleep, the morning's toasts
            const double WelcomeSeconds = 45;         // the welcome card and how to play
            const double NameSeconds = 20;            // naming the studio
            const double TutorialSeconds = 55;        // reading Doug's first day, spread over it
            const double PickGameSeconds = 45;        // choosing an idea, a size and a style
            const double LaunchSeconds = 30;          // the store page and the price
            const double ShowSeconds = 20;            // the launch-day show

            public sealed class Run
            {
                public bool Shipped;
                public bool Featured;
                public int Day;
                public int BuildSessions;
                public int GigSessions;
                public int FeaturesBuilt;
                public int LevelUps;
                public int Achievements;
                public double Minutes;
                /// <summary>Every celebrated win, with the second of the playthrough it happens in.</summary>
                public readonly List<(double Seconds, string What)> Rewards = new List<(double, string)>();

                /// <summary>The longest stretch without a win, from the start to the end of the launch show.</summary>
                public double LongestWait
                {
                    get
                    {
                        double longest = 0, last = 0;
                        foreach (var r in Rewards.OrderBy(r => r.Seconds))
                        {
                            longest = Math.Max(longest, r.Seconds - last);
                            last = r.Seconds;
                        }
                        return Math.Max(longest, Minutes * 60 - last);
                    }
                }
            }

            public static Run Play(GameSimulation sim, double performance, bool restEvenings = false)
            {
                var run = new Run();
                var s = sim.State;
                var sam = Sam(sim);
                double t = WelcomeSeconds + NameSeconds;
                run.Rewards.Add((t, "studio named"));
                t += TutorialSeconds;

                int levels = Levels(s), features = 0, steps = ChapterCatalog.All[0].StepsDone(s), payments = 0, gigs = 0;
                int achievements = AchievementCatalog.All.Count(a => a.IsEarned(s));
                void Notice()
                {
                    int nowFeatures = s.Projects.Sum(p => p.FeaturesDone);
                    for (int i = features; i < nowFeatures; i++) run.Rewards.Add((t, "feature complete"));
                    run.FeaturesBuilt += Math.Max(0, nowFeatures - features);
                    features = nowFeatures;
                    int nowLevels = Levels(s);
                    for (int i = levels; i < nowLevels; i++) run.Rewards.Add((t, "level up"));
                    run.LevelUps += Math.Max(0, nowLevels - levels);
                    levels = nowLevels;
                    int nowSteps = ChapterCatalog.All[0].StepsDone(s);
                    for (int i = steps; i < nowSteps; i++) run.Rewards.Add((t, "goal step"));
                    steps = nowSteps;
                    int nowPayments = s.Inbox.Messages.Count(m => m.EventId == "payment");
                    for (int i = payments; i < nowPayments; i++) run.Rewards.Add((t, "paid"));
                    payments = nowPayments;
                    int nowGigs = s.Freelance.Profile.CompletedGigs;
                    for (int i = gigs; i < nowGigs; i++) run.Rewards.Add((t, "gig delivered"));
                    gigs = nowGigs;
                    int nowAchievements = AchievementCatalog.All.Count(a => a.IsEarned(s));
                    for (int i = achievements; i < nowAchievements; i++) run.Rewards.Add((t, "achievement"));
                    run.Achievements += Math.Max(0, nowAchievements - achievements);
                    achievements = nowAchievements;
                }

                Assert.IsTrue(sim.AcceptGig(sam.Id, out string error), error);
                for (int step = 0; !s.DemoOver; step++)
                {
                    if (step > 500) Assert.Fail($"The demo never ended (day {s.Day}, slot {s.Slot}).");
                    ChooseDefaults(sim);
                    if (sim.IsNight)
                    {
                        sim.Sleep();
                        t += DaySeconds;
                        Notice();
                        continue;
                    }
                    if (restEvenings && s.Slot == 2)
                    {
                        sim.PerformSlot(PlanBlock.Rest);
                        t += SessionOverheadSeconds / 2;
                        continue;
                    }

                    var project = s.ActiveProject;
                    bool samLeft = sam.Status == GigStatus.Active;
                    bool firstMorning = s.Day == 0 && s.Slot == 0;
                    if (project == null && !firstMorning)
                    {
                        sim.StartProject(s.UnusedIdeas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out error);
                        Assert.IsNull(error);
                        project = s.ActiveProject;
                        t += PickGameSeconds;
                        Notice();
                    }

                    if (samLeft && (firstMorning || s.Day > 0))
                    {
                        sim.PerformSlot(PlanBlock.Gig(sam.Id), performance);
                        run.GigSessions++;
                        t += BugSquashSeconds + SessionOverheadSeconds;
                    }
                    else if (project != null && project.Status == ProjectStatus.InDevelopment)
                    {
                        sim.PerformSlot(PlanBlock.Develop, performance);
                        run.BuildSessions++;
                        t += WireItUpSeconds + SessionOverheadSeconds;
                    }
                    else if (project != null && project.Status == ProjectStatus.ReadyToRelease)
                    {
                        if (!project.HasStorePage) Assert.IsTrue(sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out error), error);
                        Assert.IsTrue(sim.Release(project.Id, 2.99, out error), error);
                        t += LaunchSeconds;
                        Notice();
                        run.Rewards.Add((t, "launch show"));
                        t += ShowSeconds;
                        continue;
                    }
                    else
                    {
                        sim.PerformSlot(PlanBlock.Rest);
                        t += SessionOverheadSeconds / 2;
                    }
                    Notice();
                }

                run.Shipped = s.DemoEnd == DemoEnd.Shipped;
                run.Featured = sim.DemoFeatured;
                run.Day = sim.Day;
                run.Minutes = t / 60;
                return run;
            }

            static int Levels(GameState s) => SkillInfo.Craft.Sum(k => s.Founder.Level(k));
        }
    }
}
