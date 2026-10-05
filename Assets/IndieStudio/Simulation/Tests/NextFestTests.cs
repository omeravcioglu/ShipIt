using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>Next Fest and demos (Design/10 §4–5): dates, sign-up rules, making the demo, and what it brings.</summary>
    public class NextFestTests
    {
        /// <summary>A Small game, built this far, with a Vapor page.</summary>
        static GameSimulation WithVaporGame(ulong seed, double progress, out Project game)
        {
            var sim = new GameSimulation(seed);
            game = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Small, ArtStyleId.PixelArt, out _);
            foreach (var w in game.Workstreams)
            {
                w.Done = w.Required * progress;
                w.QualitySum = w.Done * 5.5;
            }
            Assert.IsTrue(sim.OpenStorePage(game.Id, StorePlatform.Vapor, out string error), error);
            return sim;
        }

        static void SleepUntil(GameSimulation sim, int day)
        {
            while (sim.Day < day) sim.Sleep();
        }

        [Test]
        public void Fests_AreInFebruaryJuneAndOctober()
        {
            Assert.AreEqual(42, GameSimulation.FestStart(1, 2), "Mon 12 Feb, Year 1");
            Assert.AreEqual(161, GameSimulation.FestStart(1, 6));
            Assert.AreEqual(287, GameSimulation.FestStart(1, 10));
            Assert.AreEqual(0, GameCalendar.DayOfWeek(GameSimulation.FestStart(3, 6)), "Always a Monday");
            Assert.AreEqual(42, GameSimulation.UpcomingFest(0));
            Assert.AreEqual(161, GameSimulation.UpcomingFest(15), "February's sign-ups closed four weeks before");
            Assert.AreEqual(287, GameSimulation.UpcomingFest(134));
            Assert.AreEqual(GameSimulation.FestStart(2, 2), GameSimulation.UpcomingFest(300));
        }

        [Test]
        public void SigningUp_NeedsAVaporPage_AndIsOncePerGame()
        {
            var sim = new GameSimulation(3);
            var tiny = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            sim.OpenStorePage(tiny.Id, StorePlatform.ScratchIo, out _);
            Assert.IsFalse(sim.SignUpForNextFest(tiny.Id, out string error));
            StringAssert.Contains("Vapor", error);

            sim = WithVaporGame(4, 0.4, out var game);
            Assert.IsTrue(sim.SignUpForNextFest(game.Id, out error), error);
            Assert.AreEqual(42, game.FestDay);
            Assert.IsFalse(sim.SignUpForNextFest(game.Id, out error), "Once is enough");
            SleepUntil(sim, game.FestDay + Tuning.FestDays);
            Assert.IsTrue(game.FestDone);
            Assert.IsFalse(sim.SignUpForNextFest(game.Id, out error));
            StringAssert.Contains("once per game", error);
        }

        [Test]
        public void TheDemo_IsMadeInSessions()
        {
            var sim = WithVaporGame(5, 0.26, out var game); // enough for a Vapor page (25%), not for a demo (30%)
            sim.SignUpForNextFest(game.Id, out _);
            var early = sim.PerformSlot(PlanBlock.Demo(game.Id));
            Assert.AreEqual("Too little to show", early.Title, "A demo needs 30% of the game");
            Assert.AreEqual(0, game.DemoProgress);

            foreach (var w in game.Workstreams) w.Done = w.Required * 0.4;
            int sessions = 0;
            while (game.DemoProgress < 1 && sessions < 30)
            {
                if (sim.IsNight) sim.Sleep();
                sim.PerformSlot(PlanBlock.Demo(game.Id));
                sessions++;
            }
            TestContext.WriteLine($"The demo took {sessions} sessions; {game.DemoBugs:0} bugs left in it");
            Assert.That(sessions, Is.InRange(4, 12), "About a week of evenings, or a few full days");
            Assert.That(sim.DemoQuality(game), Is.GreaterThan(0.2));
        }

        [Test]
        public void ABetterDemo_BringsMoreWishlists()
        {
            double Fest(int demoSessions)
            {
                var sim = WithVaporGame(6, 0.45, out var game);
                sim.SignUpForNextFest(game.Id, out _);
                for (int i = 0; i < demoSessions; i++)
                {
                    if (sim.IsNight) sim.Sleep();
                    sim.PerformSlot(PlanBlock.Demo(game.Id));
                }
                double before = game.Wishlists;
                SleepUntil(sim, game.FestDay + Tuning.FestDays);
                Assert.IsTrue(game.FestDone);
                Assert.AreEqual(before + game.FestWishlists, game.Wishlists, 1e-6);
                Assert.IsTrue(sim.State.Inbox.Messages.Any(m => m.EventId == "fest_over"));
                return game.FestWishlists;
            }

            double none = Fest(0), half = Fest(3), full = Fest(10);
            TestContext.WriteLine($"Next Fest wishlists: no demo {none:0}, half a demo {half:0}, a finished demo {full:0}");
            Assert.Greater(none, 0, "The page is still in the list");
            Assert.Greater(half, none * 3, "Any demo beats none");
            Assert.Greater(full, half, "…and a finished one beats a rough one");
            Assert.That(full, Is.InRange(300, 4000), "Worth a few months of posts, not a replacement for them");
        }

        [Test]
        public void ReleasingBeforeTheFest_LeavesIt()
        {
            var sim = WithVaporGame(7, 1.0, out var game);
            sim.SignUpForNextFest(game.Id, out _);
            Assert.AreEqual(ProjectStatus.InDevelopment, game.Status);
            sim.CheckProgress();
            sim.PerformSlot(PlanBlock.Develop); // all work done: content complete
            Assert.AreEqual(ProjectStatus.ReadyToRelease, game.Status);
            SleepUntil(sim, 15);
            Assert.IsTrue(sim.Release(game.Id, 14.99, out string error), error);
            Assert.AreEqual(-1, game.FestDay);
            StringAssert.Contains("left the fest", sim.State.Inbox.Messages.Last(m => m.EventId == "launch").Body);
        }

        [Test]
        public void Save_Version4StillLoads()
        {
            var sim = WithVaporGame(8, 0.4, out var game);
            sim.SignUpForNextFest(game.Id, out _);
            using (var stream = new MemoryStream())
            {
                SaveGame.Write(sim.State, stream, 4);
                stream.Position = 0;
                var loaded = GameSimulation.Load(stream).State.Projects.Single(p => p.Id == game.Id);
                Assert.AreEqual(-1, loaded.FestDay, "Older saves weren't signed up for anything");
                Assert.IsFalse(loaded.FestDone);
            }
        }
    }
}
