using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>Launch week (Design/10 §7): hotfixes, answering players, launch posts, critics, and word of mouth.</summary>
    public class LaunchWeekTests
    {
        /// <summary>A tiny game built in a rush (bugs and all) and launched on scratch.io.</summary>
        static GameSimulation Launched(ulong seed, out Project game)
        {
            var sim = new GameSimulation(seed);
            sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            game = sim.State.ActiveProject;
            for (int guard = 0; game.Status != ProjectStatus.ReadyToRelease && guard < 300; guard++)
            {
                if (sim.IsNight) sim.Sleep();
                sim.PerformSlot(PlanBlock.Develop);
            }
            Assert.AreEqual(ProjectStatus.ReadyToRelease, game.Status);
            Assert.IsTrue(sim.OpenStorePage(game.Id, StorePlatform.ScratchIo, out string error), error);
            Assert.IsTrue(sim.Release(game.Id, 2.99, out error), error);
            Assert.Greater(game.BugsRemaining, 3, "A rushed game launches with bugs");
            return sim;
        }

        /// <summary>Spends every slot of the next <paramref name="days"/> days on one activity (or rests).</summary>
        static void Spend(GameSimulation sim, int days, PlanBlock activity)
        {
            for (int d = 0; d < days; d++)
            {
                while (!sim.IsNight) sim.PerformSlot(activity);
                sim.Sleep();
            }
        }

        [Test]
        public void LeftAlone_ALaunchGoesAsPlanned()
        {
            var sim = Launched(31, out var game);
            var r = game.Result;
            double launchMood = r.LaunchMood;
            Spend(sim, 29, PlanBlock.Rest);
            Assert.AreEqual(launchMood, r.Mood, 1e-12);
            Assert.AreEqual(launchMood, r.PositiveShare, 1e-12, "Reviews stay where they started");
            Assert.AreEqual(1.0, r.SalesMultiplier, 1e-12, "No word of mouth either way");
            Assert.AreEqual(7, r.LaunchDays.Count, "Each day of launch week is logged");
            Assert.That(r.LaunchDays.Sum(d => d.Units), Is.GreaterThan(0));
        }

        [Test]
        public void Hotfixes_RaiseTheReviews_EarlyFixesMost()
        {
            double Final(int restDaysFirst, int hotfixDays)
            {
                var sim = Launched(32, out var game);
                Spend(sim, restDaysFirst, PlanBlock.Rest);
                Spend(sim, hotfixDays, PlanBlock.Hotfix(game.Id));
                Spend(sim, 28 - restDaysFirst - hotfixDays, PlanBlock.Rest);
                Assert.AreEqual(hotfixDays > 0, game.Result.Hotfixes > 0);
                return game.Result.PositiveShare;
            }

            double none = Final(0, 0);
            double early = Final(0, 2);
            double late = Final(5, 2);
            TestContext.WriteLine($"Positive reviews after 30 days: untouched {none:P1}, late hotfixes {late:P1}, early hotfixes {early:P1}");
            Assert.Greater(late, none, "Any hotfix helps");
            Assert.Greater(early, late, "…but most players arrive in the first days");
        }

        [Test]
        public void Hotfixes_MakePlayersHappierAndWordOfMouthStronger()
        {
            var sim = Launched(33, out var game);
            var r = game.Result;
            double bugs = game.BugsRemaining;
            var report = sim.PerformSlot(PlanBlock.Hotfix(game.Id));
            Assert.AreEqual($"Hotfixed {game.Title}", report.Title);
            Assert.Less(game.BugsRemaining, bugs + 5, "Bugs squashed (or a few new ones from a rushed fix)");
            Assert.Greater(r.Stability, r.LaunchStability - 1.3);
            if (r.Regressions == 0)
            {
                Assert.Greater(r.Mood, r.LaunchMood, "Fewer bugs, happier players");
                Assert.Greater(r.SalesMultiplier, 1, "…who tell their friends");
            }
            Assert.That(sim.RegressionChance, Is.InRange(Tuning.RegressionMin, Tuning.RegressionBase));
        }

        [Test]
        public void AnsweringPlayers_WinsGoodwillUpToAPoint()
        {
            var sim = Launched(34, out var game);
            var r = game.Result;
            double followers = sim.State.Audience.Followers;
            Spend(sim, 3, PlanBlock.Answer(game.Id));
            Assert.That(r.Answers, Is.GreaterThanOrEqualTo(6), "Launch day's remaining slots, then two full days");
            Assert.AreEqual(Tuning.GoodwillMax, r.Goodwill, 1e-9, "Goodwill is capped");
            Assert.AreEqual(r.LaunchMood + Tuning.GoodwillMax, r.Mood, 1e-9);
            Assert.Greater(sim.State.Audience.Followers, followers, "People follow a developer who listens");
        }

        [Test]
        public void LaunchPosts_SellCopiesRightAway()
        {
            var sim = Launched(35, out var game);
            double units = game.UnitsSold;
            var report = sim.PerformSlot(PlanBlock.Post(MarketingPost.Screenshot));
            Assert.Greater(game.Result.PushUnits, 0);
            Assert.AreEqual(units + game.Result.PushUnits, game.UnitsSold, 1e-9);
            Assert.IsTrue(report.Lines.Any(l => l.StartsWith("Launch momentum")));

            Spend(sim, 8, PlanBlock.Rest);
            double pushed = game.Result.PushUnits;
            var after = sim.PerformSlot(PlanBlock.Post(MarketingPost.Screenshot));
            Assert.AreEqual(pushed, game.Result.PushUnits, "After launch week, posts are back to normal");
            Assert.IsFalse(after.Lines.Any(l => l.StartsWith("Launch momentum")));
        }

        [Test]
        public void LaunchWeek_EndsWithASummary()
        {
            var sim = Launched(36, out var game);
            Assert.AreEqual(game, sim.LaunchWeekGame);
            Spend(sim, 7, PlanBlock.Rest);
            Assert.IsNull(sim.LaunchWeekGame, "Seven days later it's over");
            Assert.AreEqual(game, sim.PatchableGame, "…but patches are possible until the post-mortem");
            var summary = sim.State.Inbox.Messages.SingleOrDefault(m => m.EventId == "launch_week_over");
            Assert.IsNotNull(summary);
            TestContext.WriteLine(summary.Body);
            StringAssert.Contains("copies in seven days", summary.Body);
        }

        [Test]
        public void Critics_ArePublishedOverTheWeek()
        {
            var game = new Project { Id = 9, Title = "Lantern Hollow", Genre = GenreId.Horror, Scope = ProjectScope.Small, Platform = StorePlatform.Vapor, Wishlists = 1600, Price = 14.99 };
            game.Workstreams.Add(new Workstream { Discipline = Skill.Design, Required = 1, Done = 1 });
            game.Workstreams[0].Features.Add("Jump scares");
            var critics = CriticWriter.For(game, 0.7, 9);
            Assert.AreEqual(6, critics.Count, "A wishlisted Vapor game draws a crowd");
            CollectionAssert.AreEqual(critics.Select(c => c.Score), CriticWriter.For(game, 0.7, 9).Select(c => c.Score), "Same game, same critics");
            Assert.AreEqual(1, critics.Min(c => c.Day));
            Assert.AreEqual(5, critics.Max(c => c.Day));
            Assert.That(critics.Average(c => c.Score), Is.InRange(65, 90), "Good games get good scores");
            Assert.Less(CriticWriter.For(game, 0.3, 4).Average(c => c.Score), critics.Average(c => c.Score), "…and buggy, weak ones don't");

            var result = new ReleaseResult();
            result.Critics.AddRange(critics);
            Assert.AreEqual(-1, result.ScoreHub(0), "Nothing out on launch day");
            Assert.That(result.ScoreHub(5), Is.InRange(15, 97));
            foreach (var c in critics) TestContext.WriteLine($"Day {c.Day}: {c.Outlet} {c.Score} — {c.Quote}");

            game.Platform = StorePlatform.ScratchIo;
            Assert.AreEqual(1, CriticWriter.For(game, 0.7, 9).Count, "A scratch.io game gets one blogger");
        }

        [Test]
        public void Save_Version3StillLoads()
        {
            var sim = Launched(37, out var game);
            Spend(sim, 3, PlanBlock.Rest);
            using (var stream = new MemoryStream())
            {
                SaveGame.Write(sim.State, stream, 3);
                stream.Position = 0;
                var loaded = GameSimulation.Load(stream).State.Projects.Single(p => p.Id == game.Id).Result;
                Assert.AreEqual(game.Result.PositiveShare, loaded.LaunchMood, 1e-12, "Older saves keep the reviews they had");
                Assert.AreEqual(loaded.PositiveShare, loaded.Mood, 1e-12);
                Assert.AreEqual(1.0, loaded.SalesMultiplier);
                Assert.AreEqual(game.UnitsSold * Tuning.ReviewRate, loaded.ReviewVotes, 1e-9);
            }
        }
    }
}
