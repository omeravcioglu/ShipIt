using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>The player-driven day (Design/02 §4): three slots, each spent on purpose, then sleep.</summary>
    public class SlotTests
    {
        static GameSimulation NewGame(ulong seed = 7) => new GameSimulation(seed);

        [Test]
        public void Day_HasThreeSlotsAndOnlyMovesWhenYouSleep()
        {
            var sim = NewGame();
            Assert.IsTrue(sim.State.DayStarted, "Day one starts right away");
            Assert.AreEqual(0, sim.Slot);

            for (int i = 0; i < 3; i++) Assert.IsNotNull(sim.PerformSlot(PlanBlock.Rest));
            Assert.IsTrue(sim.IsNight);
            Assert.IsNull(sim.PerformSlot(PlanBlock.Rest), "No fourth slot");
            Assert.AreEqual(0, sim.Day, "Time doesn't pass on its own");
            Assert.AreEqual(3, sim.State.Today.Count);

            sim.Sleep();
            Assert.AreEqual(1, sim.Day);
            Assert.AreEqual(0, sim.Slot);
            Assert.AreEqual(0, sim.State.Today.Count);
        }

        [Test]
        public void Slots_PlayedByHandMatchTheWeekPlan()
        {
            var plan = PlanTemplates.Balanced();
            var auto = NewGame(21);
            var manual = NewGame(21);
            auto.ApplyPlan(plan);
            foreach (var sim in new[] { auto, manual })
            {
                var friend = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
                sim.AcceptGig(friend.Id, out _);
                sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            }

            for (int day = 0; day < 21; day++)
            {
                auto.AdvanceDay();
                for (int slot = 0; slot < 3; slot++) manual.PerformSlot(plan[GameCalendar.DayOfWeek(manual.Day), slot]);
                manual.Sleep();
            }
            // AdvanceDay leaves the next day unstarted; start it so both sims stand at the same moment.
            auto.StartDay();

            Assert.AreEqual(auto.Day, manual.Day);
            Assert.AreEqual(auto.Finance.Cash, manual.Finance.Cash, 1e-9);
            Assert.AreEqual(auto.Founder.Burnout, manual.Founder.Burnout, 1e-9);
            Assert.AreEqual(auto.State.ActiveProject.Progress, manual.State.ActiveProject.Progress, 1e-9);
            Assert.AreEqual(auto.State.Inbox.Messages.Count, manual.State.Inbox.Messages.Count);
        }

        [Test]
        public void Performance_ScalesTheWorkASessionDoes()
        {
            double Progress(double performance)
            {
                var sim = NewGame();
                var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Small, ArtStyleId.PixelArt, out _);
                sim.PerformSlot(PlanBlock.Build(Skill.Programming), performance);
                return project.Stream(Skill.Programming).Done;
            }

            Assert.AreEqual(2.0, Progress(Rules.MinigameMultiplier(1)) / Progress(Rules.MinigameMultiplier(0.1)), 0.2);
            Assert.AreEqual(1.0, Rules.MinigameMultiplier(Rules.OrdinaryScore), 1e-9);
            Assert.AreEqual(Tuning.MinigameFloor, Rules.MinigameMultiplier(-1), 1e-9);
        }

        [Test]
        public void Build_ReportsTheFeatureItWorkedOn()
        {
            var sim = NewGame();
            var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            Assert.That(project.FeatureCount, Is.InRange(5, 16), "A tiny game is a handful of named features");
            foreach (var stream in project.Workstreams)
                Assert.AreEqual(stream.Features.Count, stream.Features.Distinct().Count(), "Feature names are unique per discipline");

            var art = project.Stream(Skill.Art);
            string first = art.FeatureName(0);
            var report = sim.PerformSlot(PlanBlock.Build(Skill.Art), 1.2);
            Assert.AreEqual(first, report.Subject);
            Assert.AreEqual(0, report.ProgressBefore, 1e-9);
            Assert.Greater(report.ProgressAfter, 0);
            Assert.IsTrue(report.HasSkill && report.Skill == Skill.Art);

            for (int i = 0; i < 40 && !art.IsDone; i++)
            {
                if (sim.IsNight) sim.Sleep();
                sim.PerformSlot(PlanBlock.Build(Skill.Art), 1.0);
            }
            Assert.AreEqual(art.FeatureCount, art.FeaturesDone);
        }

        [Test]
        public void Gig_CarefulSessionsFinishSoonerAndPleaseTheClient()
        {
            var sim = NewGame();
            var gig = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
            sim.AcceptGig(gig.Id, out _);
            double productivity = Rules.Productivity(sim.State).Value;
            var report = sim.PerformSlot(PlanBlock.Gig(gig.Id), 1.2);
            Assert.AreEqual(Tuning.BlockHours * productivity * 1.2, gig.HoursDone, 1e-9);
            Assert.AreEqual(gig.Title, report.Subject);
            Assert.AreEqual(1.2, gig.AveragePerformance, 1e-9);
        }

        [Test]
        public void DayJob_TakesItsSlotsWhateverYouChoose()
        {
            var sim = NewGame();
            sim.Founder.Job = DayJob.PartTime;
            sim.Founder.JobStartDay = 0;
            sim.PerformSlot(PlanBlock.Rest);                       // Monday morning is yours
            var afternoon = sim.PerformSlot(PlanBlock.Develop);    // Monday afternoon belongs to the café
            Assert.IsTrue(afternoon.Forced);
            Assert.AreEqual(ActivityKind.DayJob, afternoon.Activity.Kind);
            Assert.IsNull(sim.ForcedActivity(), "The evening is free again");
        }

        [Test]
        public void Marketing_PostKindsTradeReliabilityForReach()
        {
            double Followers(MarketingPost post, out double spread)
            {
                var sim = NewGame(5);
                var gains = new System.Collections.Generic.List<double>();
                for (int i = 0; i < 300; i++)
                {
                    if (sim.IsNight) sim.Sleep();
                    double before = sim.State.Audience.Followers;
                    sim.PerformSlot(PlanBlock.Post(post));
                    gains.Add(sim.State.Audience.Followers - before);
                    sim.Founder.Burnout = 10; // keep the founder fresh so only the post kind differs
                }
                double mean = gains.Average();
                spread = System.Math.Sqrt(gains.Average(g => (g - mean) * (g - mean))) / mean;
                return mean;
            }

            double screenshot = Followers(MarketingPost.Screenshot, out double screenshotSpread);
            double devlog = Followers(MarketingPost.Devlog, out double devlogSpread);
            double meme = Followers(MarketingPost.Meme, out double memeSpread);
            TestContext.WriteLine($"followers/post: screenshot {screenshot:0.0} (cv {screenshotSpread:0.00}), devlog {devlog:0.0} (cv {devlogSpread:0.00}), meme {meme:0.0} (cv {memeSpread:0.00})");
            Assert.Greater(devlog, screenshot, "Devlogs grow followers");
            Assert.Less(devlogSpread, screenshotSpread, "…reliably");
            Assert.Greater(memeSpread, screenshotSpread, "Memes are a lottery");
        }

        [Test]
        public void Chapter1_EndsWhenTheFirstGameShips()
        {
            var sim = NewGame();
            Assert.AreEqual("bedroom", sim.CurrentChapter.Id);
            Assert.AreEqual(0, sim.CurrentChapter.StepsDone(sim.State));

            var bot = BotPlayer.Balanced();
            for (int i = 0; i < 200 && sim.State.ShippedGames == 0; i++)
            {
                bot.Act(sim);
                sim.AdvanceDay();
            }
            sim.CheckProgress();
            Assert.AreEqual(1, sim.State.ShippedGames);
            Assert.AreEqual("players", sim.CurrentChapter.Id, "Shipping (with Sam's gig delivered) completes chapter 1");
            Assert.IsTrue(sim.State.Inbox.Messages.Any(m => m.EventId == "chapter_complete"));
        }

        [Test]
        public void Vacation_MakesTheRestOfTodayRest()
        {
            var sim = NewGame();
            sim.PerformSlot(PlanBlock.Research);
            sim.TakeVacation(2);
            var report = sim.PerformSlot(PlanBlock.Develop);
            Assert.IsTrue(report.Forced);
            Assert.AreEqual(ActivityKind.Rest, report.Activity.Kind);
        }
    }
}
