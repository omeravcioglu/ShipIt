using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    public class CoreRulesTests
    {
        [Test]
        public void Random_SameSeed_SameSequence()
        {
            var a = new SimRandom(42, 7);
            var b = new SimRandom(42, 7);
            for (int i = 0; i < 1000; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void Random_RangeStaysInBounds()
        {
            var rng = new SimRandom(1, 1);
            for (int i = 0; i < 10000; i++)
            {
                int value = rng.Range(3, 9);
                Assert.That(value, Is.InRange(3, 8));
                double d = rng.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0).And.LessThan(1));
            }
        }

        [Test]
        public void Calendar_DayZeroIsMondayFirstOfJanuary()
        {
            Assert.AreEqual("Mon 1 Jan, Year 1", GameCalendar.Format(0));
            Assert.IsTrue(GameCalendar.IsMonday(7));
            Assert.IsTrue(GameCalendar.IsFirstOfMonth(31));
            Assert.AreEqual(1, GameCalendar.MonthIndex(31));
            Assert.AreEqual(31, GameCalendar.FirstDayOfMonthIndex(1));
            Assert.AreEqual(2, GameCalendar.Year(365));
        }

        [Test]
        public void Skills_XpCurveMatchesDesign()
        {
            // Design/02 §6: level L → L+1 costs 70 × L² hours.
            Assert.AreEqual(3.0, SkillInfo.AddPractice(2.0, 280), 1e-9);
            Assert.AreEqual(2.5, SkillInfo.AddPractice(2.0, 140), 1e-9);
            Assert.AreEqual(7.0, SkillInfo.AddPractice(2.0, 280 + 630 + 1120 + 1750 + 2520), 1e-9);
            Assert.AreEqual(10.0, SkillInfo.AddPractice(9.5, 100000), 1e-9);
        }

        [Test]
        public void Freelance_RateMatchesDesign()
        {
            // Design/03 §2: (5 + 4.5 × skill) × (1 + 0.25 × stars/5).
            Assert.AreEqual(14.0, Rules.FreelanceRate(2, 0, RateSetting.Market).Value, 1e-9);
            Assert.AreEqual(40.0, Rules.FreelanceRate(6, 5, RateSetting.Market).Value, 1e-9);
            Assert.AreEqual(14.0 * 1.25, Rules.FreelanceRate(2, 0, RateSetting.Premium).Value, 1e-9);
        }

        [Test]
        public void Work_EfficiencyMatchesDesign()
        {
            Assert.AreEqual(0.73, Rules.Efficiency(2), 1e-9);
            Assert.AreEqual(1.00, Rules.Efficiency(5), 1e-9);
            Assert.AreEqual(1.0, Rules.BurnoutFactor(30), 1e-9);
            Assert.AreEqual(0.84, Rules.BurnoutFactor(70), 1e-9);
        }

        [Test]
        public void Breakdown_ExplainsItsValue()
        {
            var b = new Breakdown("Base", 10).Add("Bonus", 5).Multiply("Half", 0.5);
            Assert.AreEqual(7.5, b.Value, 1e-9);
            Assert.AreEqual(3, b.Lines.Count);
            StringAssert.Contains("Bonus", b.Describe());
        }

        [Test]
        public void Plans_TemplatesHaveTheAdvertisedHours()
        {
            Assert.AreEqual(12, PlanTemplates.Balanced().CountWork());
            Assert.AreEqual(5, PlanTemplates.Balanced().Count(ActivityKind.Freelance));
            Assert.AreEqual(5, PlanTemplates.Balanced().Count(ActivityKind.Develop));
            Assert.AreEqual(11, PlanTemplates.AllIn().CountWork());
            Assert.AreEqual(9, PlanTemplates.AllIn().Count(ActivityKind.Develop));
            Assert.AreEqual(7, PlanTemplates.Survival().Count(ActivityKind.Freelance));
            Assert.AreEqual(10, PlanTemplates.Standard().CountWork());
        }
    }
}
