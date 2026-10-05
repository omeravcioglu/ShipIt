using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>The small rhythms of a day: flow, coffee, tired evenings, the cat, the studio's name, player reviews.</summary>
    public class LifeTests
    {
        static GameSimulation WithProject(ulong seed = 7, ProjectScope scope = ProjectScope.Small)
        {
            var sim = new GameSimulation(seed);
            sim.StartProject(sim.State.Ideas.First().Id, scope, ArtStyleId.PixelArt, out _);
            return sim;
        }

        [Test]
        public void Flow_BuildsWithFocusAndBreaksWhenYouSwitch()
        {
            var sim = WithProject();
            var build = PlanBlock.Build(Skill.Programming);
            Assert.AreEqual(0, sim.FlowBonusFor(build), "A first session starts fresh");
            Assert.AreEqual(1.00, sim.PerformSlot(build).Performance, 1e-9);
            Assert.AreEqual(0.05, sim.FlowBonusFor(build), 1e-9, "The next one would get +5%");
            Assert.AreEqual(1.05, sim.PerformSlot(build).Performance, 1e-9);
            sim.PerformSlot(PlanBlock.Rest);
            Assert.AreEqual(2, sim.State.FlowStreak, "Rest keeps the streak");
            sim.Sleep();
            Assert.AreEqual(1.10, sim.PerformSlot(build).Performance, 1e-9, "…even overnight");
            Assert.AreEqual(1.15, sim.PerformSlot(build).Performance, 1e-9);
            Assert.AreEqual(1.15, sim.PerformSlot(build).Performance, 1e-9, "Flow tops out at +15%");
            Assert.AreEqual(0, sim.FlowBonusFor(PlanBlock.Build(Skill.Art)), "Another discipline would start fresh");

            sim.Sleep();
            sim.PerformSlot(PlanBlock.Research);
            Assert.AreEqual(0, sim.State.FlowStreak, "Other work breaks the streak");
            Assert.AreEqual(1.00, sim.PerformSlot(build).Performance, 1e-9);
        }

        [Test]
        public void Coffee_BoostsTheNextWorkSessionForBurnout()
        {
            var sim = WithProject();
            double burnout = sim.Founder.Burnout;
            Assert.IsTrue(sim.DrinkCoffee(out _));
            Assert.AreEqual(burnout + Tuning.CoffeeBurnout, sim.Founder.Burnout, 1e-9);
            Assert.IsFalse(sim.DrinkCoffee(out string full), "One mug at a time");
            Assert.IsNotNull(full);

            sim.PerformSlot(PlanBlock.Rest);
            Assert.IsTrue(sim.State.CoffeeReady, "Resting doesn't drink it");
            var report = sim.PerformSlot(PlanBlock.Build(Skill.Art));
            Assert.AreEqual(Tuning.CoffeeBoost, report.Performance, 1e-9);
            Assert.IsFalse(sim.State.CoffeeReady);

            Assert.IsTrue(sim.DrinkCoffee(out _));
            sim.PerformSlot(PlanBlock.Rest);
            Assert.IsTrue(sim.IsNight);
            sim.Sleep();
            Assert.IsFalse(sim.State.CoffeeReady, "Coffee goes cold overnight");
            Assert.IsTrue(sim.DrinkCoffee(out _));
            sim.PerformSlot(PlanBlock.Research);
            Assert.IsTrue(sim.DrinkCoffee(out _));
            sim.PerformSlot(PlanBlock.Research);
            Assert.IsFalse(sim.DrinkCoffee(out string shaking), "Two a day at most");
            StringAssert.Contains("shaking", shaking);
        }

        [Test]
        public void DayJob_EveningsAreTired()
        {
            var sim = WithProject();
            sim.Founder.Job = DayJob.FullTime;
            sim.Founder.JobStartDay = 0;
            sim.PerformSlot(PlanBlock.Rest);                 // the office takes the morning…
            sim.PerformSlot(PlanBlock.Rest);                 // …and the afternoon
            Assert.IsTrue(sim.IsAfterWork(PlanBlock.Develop));
            var evening = sim.PerformSlot(PlanBlock.Build(Skill.Programming));
            Assert.AreEqual(Tuning.AfterJobFatigue, evening.Performance, 1e-9);
            Assert.IsTrue(evening.Lines.Any(l => l.Contains("Tired")));

            sim.Sleep();
            for (int i = 0; i < 4; i++) sim.AdvanceDay(); // Tuesday to Friday
            sim.StartDay();
            Assert.AreEqual(5, GameCalendar.DayOfWeek(sim.Day), "Saturday");
            Assert.IsFalse(sim.IsAfterWork(PlanBlock.Develop), "Weekends are fresh");
        }

        [Test]
        public void Cat_CheersYouUpOnlyWhenYouAreLow()
        {
            var sim = new GameSimulation(3);
            sim.Founder.Morale = 70;
            sim.PetCat();
            Assert.AreEqual(70, sim.Founder.Morale, 1e-9, "A happy founder just gets a purr");
            sim.Sleep();
            sim.Founder.Morale = 40;
            sim.PetCat();
            Assert.AreEqual(40 + Tuning.CatMorale, sim.Founder.Morale, 1e-9);
            sim.PetCat();
            Assert.AreEqual(40 + Tuning.CatMorale, sim.Founder.Morale, 1e-9, "Once a day");
        }

        [Test]
        public void StudioName_IsTidiedAndSuggested()
        {
            var sim = new GameSimulation(4);
            Assert.AreEqual("Bedroom Games", sim.State.StudioName);
            sim.NameStudio("   ");
            Assert.AreEqual("Bedroom Games", sim.State.StudioName, "An empty name keeps the old one");
            sim.NameStudio("  Moth Lab  ");
            Assert.AreEqual("Moth Lab", sim.State.StudioName);
            sim.NameStudio(new string('x', 60));
            Assert.AreEqual(GameSimulation.StudioNameMaxLength, sim.State.StudioName.Length);
            Assert.AreEqual(sim.SuggestStudioName(2), sim.SuggestStudioName(2), "Suggestions are stable");
            Assert.That(Enumerable.Range(0, 6).Select(sim.SuggestStudioName).Distinct().Count(), Is.GreaterThan(1), "…and varied");
        }

        [Test]
        public void Reviews_NameYourFeaturesAndMatchTheGame()
        {
            var sim = new GameSimulation(11);
            BotPlayer.AllIn().Play(sim, 60);
            var game = sim.State.ReleasedProjects.First();
            var reviews = ReviewWriter.For(game, 8);
            Assert.AreEqual(8, reviews.Count);
            CollectionAssert.AreEqual(reviews.Select(r => r.Text), ReviewWriter.For(game, 8).Select(r => r.Text), "Same game, same reviews");
            var features = game.Workstreams.SelectMany(w => w.Features).ToList();
            Assert.IsTrue(reviews.Any(r => features.Any(f => r.Text.Contains(f))), "Players talk about what you built");
            foreach (var r in reviews) TestContext.WriteLine($"{(r.Positive ? "+" : "-")} {r.Author} ({r.Hours} h): {r.Text}");
            Assert.That(GameSimulation.ExpectedLaunchDayUnits(game), Is.GreaterThan(0));
            Assert.That(GameSimulation.NetPerLaunchCopy(game), Is.GreaterThan(0).And.LessThan(game.Price));
        }

        [Test]
        public void Save_Version1StillLoads()
        {
            var sim = WithProject();
            sim.PerformSlot(PlanBlock.Build(Skill.Programming));
            using (var stream = new MemoryStream())
            {
                SaveGame.Write(sim.State, stream, 1);
                stream.Position = 0;
                var loaded = GameSimulation.Load(stream);
                Assert.AreEqual(sim.Day, loaded.Day);
                Assert.AreEqual(sim.State.ActiveProject.Progress, loaded.State.ActiveProject.Progress, 1e-12);
                Assert.AreEqual("Bedroom Games", loaded.State.StudioName, "Fields newer than version 1 get their defaults");
                Assert.AreEqual(0, loaded.State.FlowStreak);
            }
        }
    }
}
