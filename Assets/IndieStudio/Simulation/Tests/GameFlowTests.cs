using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    public class GameFlowTests
    {
        static GameSimulation NewGame(ulong seed = 7) => new GameSimulation(seed);

        static WeekPlan AllRest() => WeekPlan.Build((d, b) => PlanBlock.Rest);

        static void ChooseDefaults(GameSimulation sim)
        {
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
        }

        [Test]
        public void NewGame_MatchesTheDesignedStart()
        {
            var sim = NewGame();
            Assert.AreEqual(5000, sim.Finance.Cash, 1e-9);
            Assert.AreEqual(1460, sim.Finance.MonthlyRecurringTotal, 1e-9);
            Assert.AreEqual(2, sim.State.Ideas.Count);
            Assert.IsTrue(sim.State.Freelance.Offers.Any(g => g.Client == ClientType.Friend && g.Pay == 180));
            Assert.IsTrue(sim.State.Inbox.Messages.Any(m => m.EventId == "welcome"));
            Assert.AreEqual(2.0, sim.Founder.Get(Skill.Programming), 1e-9);
        }

        [Test]
        public void Rent_IsChargedOnTheFirstOfTheMonth()
        {
            var sim = NewGame();
            sim.ApplyPlan(AllRest());
            sim.AdvanceDays(31);
            Assert.AreEqual(5000, sim.Finance.Cash, 1e-9, "January is prepaid");
            sim.AdvanceDay(); // 1 February
            Assert.AreEqual(5000 - 1460, sim.Finance.Cash, 1e-9);
            Assert.AreEqual(1, sim.Finance.Reports.Count);
        }

        [Test]
        public void Burnout_48HourWeekMatchesDesign()
        {
            var sim = NewGame();
            sim.ApplyPlan(PlanTemplates.Balanced());
            double before = sim.Founder.Burnout;
            sim.AdvanceToNextWeek();
            // 48h → +0.35 × (48 − 45); weekday evenings carry no extra penalty (Design/02 §5).
            Assert.AreEqual(before + 0.35 * 3, sim.Founder.Burnout, 1e-9);
        }

        [Test]
        public void Burnout_WeekendWorkCostsExtra()
        {
            var sim = NewGame();
            sim.ApplyPlan(PlanTemplates.Parse("DD-|DD-|DD-|DD-|DD-|DD-|---"));
            double before = sim.Founder.Burnout;
            sim.AdvanceToNextWeek();
            // 48h → +1.05, plus two Saturday blocks × 0.5.
            Assert.AreEqual(before + 0.35 * 3 + 0.5 * 2, sim.Founder.Burnout, 1e-9);
        }

        [Test]
        public void Rest_RecoversBurnout()
        {
            var sim = NewGame();
            sim.Founder.Burnout = 50;
            sim.ApplyPlan(AllRest());
            sim.AdvanceToNextWeek();
            Assert.AreEqual(50 - 22.5, sim.Founder.Burnout, 1e-9);
        }

        [Test]
        public void Gig_AcceptWorkAndGetPaid()
        {
            var sim = NewGame();
            var gig = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
            Assert.IsTrue(sim.AcceptGig(gig.Id, out _));
            sim.ApplyPlan(PlanTemplates.Parse("GGG|GG-|---|---|---|---|---"));
            sim.AdvanceDays(3);
            Assert.AreEqual(GigStatus.Completed, gig.Status);
            Assert.AreEqual(1, sim.State.Freelance.Profile.CompletedGigs);
            sim.AdvanceDays(gig.PaymentDelayDays + 1);
            Assert.AreEqual(5000 + 180, sim.Finance.Cash, 1e-9, "Paid a couple of days after delivery; January rent is prepaid");
            Assert.IsTrue(sim.State.GoalsCompleted.ContainsKey("first_gig"));
        }

        [Test]
        public void Gig_IgnoredGigFailsAfterADeadlineWeek()
        {
            var sim = NewGame();
            var gig = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
            sim.AcceptGig(gig.Id, out _);
            sim.ApplyPlan(AllRest());
            sim.AdvanceDays(gig.DurationDays + 9);
            Assert.AreEqual(GigStatus.Failed, gig.Status);
            Assert.AreEqual(1.0, sim.State.Freelance.Profile.Rating, 1e-9);
        }

        [Test]
        public void Project_FromIdeaToReleaseToPostMortem()
        {
            var sim = NewGame();
            var idea = sim.State.Ideas.First();
            var project = sim.StartProject(idea.Id, ProjectScope.Tiny, ArtStyleId.Minimalist, out string error);
            Assert.IsNotNull(project, error);
            Assert.Less(project.InitialEstimatePw, project.TotalRequired, "New studios underestimate (planning fallacy)");

            sim.ApplyPlan(PlanTemplates.AllIn());
            for (int i = 0; i < 400 && project.Status == ProjectStatus.InDevelopment; i++)
            {
                ChooseDefaults(sim);
                sim.AdvanceDay();
            }
            Assert.AreEqual(ProjectStatus.ReadyToRelease, project.Status);
            Assert.IsFalse(sim.Release(project.Id, 2.99, out _), "Needs a store page first");
            Assert.IsTrue(sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out error), error);
            Assert.IsTrue(sim.Release(project.Id, 2.99, out error), error);
            Assert.AreEqual(1, sim.State.ShippedGames);

            for (int i = 0; i < 31; i++)
            {
                ChooseDefaults(sim);
                sim.AdvanceDay();
            }
            Assert.Greater(project.UnitsSold, 0);
            Assert.Greater(project.NetRevenue, 0);
            Assert.IsTrue(project.Result.Pillars.ContainsKey(Pillar.Gameplay));
            Assert.IsTrue(sim.State.Inbox.Messages.Any(m => m.EventId == "post_mortem"));
            Assert.Greater(sim.State.Knowledge.Count, 0, "The post-mortem reveals the genre × style fit");
        }

        [Test]
        public void PostMortem_ChoosingALessonAppliesIt()
        {
            var sim = NewGame();
            BotPlayer.AllIn().Play(sim, 200);
            var pm = sim.State.Inbox.Messages.FirstOrDefault(m => m.EventId == "post_mortem");
            Assert.IsNotNull(pm, "The bot should have shipped and received a post-mortem within 200 days");
            Assert.AreEqual(1, sim.State.Lessons.Count);
            Assert.IsTrue(pm.Outcome.StartsWith("Lesson learned"));
        }

        [Test]
        public void Vapor_StorePageNeedsProgressAndTwoWeeksBeforeLaunch()
        {
            var sim = NewGame();
            var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            Assert.IsFalse(sim.OpenStorePage(project.Id, StorePlatform.Vapor, out _), "Needs 25% progress");

            sim.ApplyPlan(PlanTemplates.AllIn());
            while (project.Progress < 0.3)
            {
                ChooseDefaults(sim);
                sim.AdvanceDay();
            }
            double cash = sim.Finance.Cash;
            Assert.IsTrue(sim.OpenStorePage(project.Id, StorePlatform.Vapor, out string error), error);
            Assert.AreEqual(cash - 100, sim.Finance.Cash, 1e-9);

            while (project.Status == ProjectStatus.InDevelopment)
            {
                ChooseDefaults(sim);
                sim.AdvanceDay();
            }
            Assert.Greater(project.Wishlists, 0, "Marketing blocks add wishlists once the page is live");
            if (sim.Day - project.StorePageDay < Tuning.VaporMinStorePageDays)
                Assert.IsFalse(sim.Release(project.Id, 3.99, out _));
        }

        [TestCase(GenreId.Puzzle)]
        [TestCase(GenreId.Horror)]
        [TestCase(GenreId.Platformer)]
        public void Quality_ABeginnersTinyGameIsRoughNotWorthless(GenreId genre)
        {
            // Design/02 + Design/05: early games may be bad, but a first tiny game should land around "Mixed".
            var sim = NewGame();
            var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            project.Genre = genre;
            project.FunPotential = 0.55;
            project.HookStrength = 0.5;
            foreach (var w in project.Workstreams)
            {
                double q = Rules.TaskQuality(sim.Founder.Get(w.Discipline), Rules.TaskDifficulty(sim.State, project, w.Discipline), 0);
                w.Done = w.Required;
                w.QualitySum = w.Required * q;
            }
            project.BugsCreated = 10;

            var result = Rules.EvaluateQuality(sim.State, project, 2.99);
            double positive = Rules.PositiveShare(result.Quality, 2.99, ProjectScope.Tiny);
            TestContext.WriteLine($"{genre}: quality {result.Quality:0.00}, positive {positive:0.00}");
            Assert.That(positive, Is.InRange(0.25, 0.7));
        }

        [Test]
        public void Simulation_IsDeterministicForASeed()
        {
            var a = NewGame(99);
            var b = NewGame(99);
            BotPlayer.Balanced().Play(a, 300);
            BotPlayer.Balanced().Play(b, 300);
            Assert.AreEqual(a.Finance.Cash, b.Finance.Cash, 1e-9);
            Assert.AreEqual(a.State.Inbox.Messages.Count, b.State.Inbox.Messages.Count);
            Assert.AreEqual(a.Founder.Get(Skill.Programming), b.Founder.Get(Skill.Programming), 1e-9);
        }

        [Test]
        public void Bankruptcy_EndsTheGameAtTheCreditLimit()
        {
            var sim = NewGame();
            sim.ApplyPlan(AllRest());
            for (int i = 0; i < 400 && !sim.State.IsGameOver; i++)
            {
                foreach (var m in sim.State.Inbox.Pending.ToList())
                    sim.ChooseOption(m.Id, m.EventId == "parents_loan" ? 1 : m.DefaultOption);
                sim.AdvanceDay();
            }
            Assert.IsTrue(sim.State.IsGameOver);
            Assert.Less(sim.Finance.Cash, -Tuning.CreditLimit);
        }
    }
}
