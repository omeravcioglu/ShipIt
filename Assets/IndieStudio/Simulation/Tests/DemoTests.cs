using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>
    /// The free demo (DemoRules): chapter 1 in 15–20 minutes. Tiny games and scratch.io only, two weeks of calendar,
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

        // ------------------------------------------------------------------ how long it takes

        /// <summary>
        /// The demo's length, played by a typical player on 40 seeds. The goal: 15–20 minutes, with the first game
        /// shipped well inside the two weeks. A careless player (every session ×0.8, every evening off) still makes it.
        /// </summary>
        [Test]
        public void Demo_TakesFifteenToTwentyMinutes()
        {
            var typical = Enumerable.Range(1, 40).Select(seed => DemoPlayer.Play(NewDemo((ulong)seed), 1.0)).ToList();
            var careful = Enumerable.Range(1, 40).Select(seed => DemoPlayer.Play(NewDemo((ulong)seed), 1.15)).ToList();
            var careless = Enumerable.Range(1, 40).Select(seed => DemoPlayer.Play(NewDemo((ulong)seed), 0.8, restEvenings: true)).ToList();

            foreach (var (name, runs) in new[] { ("typical (×1.0)", typical), ("careful (×1.15)", careful), ("careless (×0.8, evenings off)", careless) })
            {
                var minutes = runs.Select(r => r.Minutes).OrderBy(m => m).ToList();
                TestContext.WriteLine(
                    $"{name,-30} shipped {runs.Count(r => r.Shipped)}/{runs.Count} · build sessions median {Median(runs.Select(r => (double)r.BuildSessions)):0.0} " +
                    $"(range {runs.Min(r => r.BuildSessions)}–{runs.Max(r => r.BuildSessions)}) · gig sessions {Median(runs.Select(r => (double)r.GigSessions)):0} · " +
                    $"ships on day {Median(runs.Where(r => r.Shipped).Select(r => (double)r.Day + 1)):0.0} · " +
                    $"about {Median(minutes):0.0} min (range {minutes.First():0.0}–{minutes.Last():0.0})");
            }

            Assert.AreEqual(typical.Count, typical.Count(r => r.Shipped), "Everyone who plays normally ships");
            Assert.AreEqual(careless.Count, careless.Count(r => r.Shipped), "Even a careless player ships inside the two weeks");
            double median = Median(typical.Select(r => r.Minutes));
            Assert.That(median, Is.InRange(15.0, 20.0), "A typical playthrough lasts 15–20 minutes");
            Assert.That(typical.Max(r => r.Day), Is.LessThan(DemoRules.Days - DemoRules.WarningDaysLeft), "...and ships before the three-days-left note");
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
        /// Decisions take their default answers.
        /// </summary>
        public static class DemoPlayer
        {
            // A simple model of real time per action, in seconds (the minigame lengths come from the game).
            const double WireItUpSeconds = 35;      // a Tiny board solved early; the timer allows ~47 s
            const double BugSquashSeconds = 27;     // timed, runs its full length
            const double SessionOverheadSeconds = 15; // pick it in your day, the intro card, the result card
            const double DaySeconds = 12;           // sleep, the morning's toasts
            /// <summary>The welcome and how-to-play (45), naming the studio (20), Doug's first day (80), picking the game (45), store page and launch (30), the launch show (20).</summary>
            const double FixedSeconds = 45 + 20 + 80 + 45 + 30 + 20;

            public sealed class Run
            {
                public bool Shipped;
                public int Day;
                public int BuildSessions;
                public int GigSessions;
                public int PolishSessions;
                public double Minutes;
            }

            public static Run Play(GameSimulation sim, double performance, bool restEvenings = false)
            {
                var run = new Run();
                var s = sim.State;
                var sam = Sam(sim);
                Assert.IsTrue(sim.AcceptGig(sam.Id, out string error), error);

                for (int step = 0; !s.DemoOver; step++)
                {
                    if (step > 500) Assert.Fail($"The demo never ended (day {s.Day}, slot {s.Slot}).");
                    ChooseDefaults(sim);
                    if (sim.IsNight)
                    {
                        sim.Sleep();
                        continue;
                    }
                    if (restEvenings && s.Slot == 2)
                    {
                        sim.PerformSlot(PlanBlock.Rest);
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
                    }

                    if (samLeft && (firstMorning || s.Day > 0))
                    {
                        sim.PerformSlot(PlanBlock.Gig(sam.Id), performance);
                        run.GigSessions++;
                    }
                    else if (project != null && project.Status == ProjectStatus.InDevelopment)
                    {
                        sim.PerformSlot(PlanBlock.Develop, performance);
                        run.BuildSessions++;
                    }
                    else if (project != null && project.Status == ProjectStatus.ReadyToRelease)
                    {
                        if (!project.HasStorePage) Assert.IsTrue(sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out error), error);
                        Assert.IsTrue(sim.Release(project.Id, 2.99, out error), error);
                    }
                    else sim.PerformSlot(PlanBlock.Rest);
                }

                run.Shipped = s.DemoEnd == DemoEnd.Shipped;
                run.Day = sim.Day;
                double seconds = FixedSeconds
                                 + run.BuildSessions * (WireItUpSeconds + SessionOverheadSeconds)
                                 + (run.GigSessions + run.PolishSessions) * (BugSquashSeconds + SessionOverheadSeconds)
                                 + (run.Day + 1) * DaySeconds;
                run.Minutes = seconds / 60;
                return run;
            }
        }
    }
}
