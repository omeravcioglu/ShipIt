using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>
    /// "Target curves as automated tests" (Design/20 §5). These run the real simulation with scripted
    /// players and check the early game keeps the shape the design promises (Design/13 §12).
    /// </summary>
    public class BalanceTests
    {
        const int Seeds = 40;
        const int TwoYears = 730;

        sealed class RunSummary
        {
            public bool WentNegative;
            public int FirstNegativeDay = -1;
            public bool Bankrupt;
            public int FirstReleaseDay;
            public int SecondReleaseDay;
            public double CashAtEnd;
            public double MinCash;
            public double LifetimeGameNet;
            public double FreelanceEarned;
            public LifetimeStats Stats;
            public double FinalProgramming;
        }

        static List<RunSummary> Run(Func<BotPlayer> makeBot, int days)
        {
            var runs = new List<RunSummary>();
            for (int seed = 1; seed <= Seeds; seed++)
            {
                var sim = new GameSimulation((ulong)seed);
                var bot = makeBot();
                var summary = new RunSummary { MinCash = sim.Finance.Cash };
                for (int d = 0; d < days && !sim.State.IsGameOver; d++)
                {
                    bot.Act(sim);
                    sim.AdvanceDay();
                    summary.MinCash = Math.Min(summary.MinCash, sim.Finance.Cash);
                    if (sim.Finance.Cash < 0 && summary.FirstNegativeDay < 0) summary.FirstNegativeDay = sim.Day;
                }
                summary.WentNegative = summary.FirstNegativeDay >= 0;
                summary.Bankrupt = sim.State.IsGameOver;
                summary.FirstReleaseDay = bot.FirstReleaseDay;
                summary.SecondReleaseDay = bot.SecondReleaseDay;
                summary.CashAtEnd = sim.Finance.Cash;
                summary.LifetimeGameNet = sim.State.LifetimeGameNet;
                summary.FreelanceEarned = sim.State.Freelance.Profile.TotalEarned;
                summary.Stats = sim.State.Stats;
                summary.FinalProgramming = sim.Founder.Get(Skill.Programming);
                runs.Add(summary);
            }
            return runs;
        }

        static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return double.NaN;
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }

        static void Report(string name, List<RunSummary> runs)
        {
            var released = runs.Where(r => r.FirstReleaseDay >= 0).ToList();
            var second = runs.Where(r => r.SecondReleaseDay >= 0).ToList();
            TestContext.WriteLine(
                $"{name}: negative {runs.Count(r => r.WentNegative)}/{runs.Count} (median day {Median(runs.Where(r => r.WentNegative).Select(r => (double)r.FirstNegativeDay)):0}), " +
                $"bankrupt {runs.Count(r => r.Bankrupt)}, min cash median {Median(runs.Select(r => r.MinCash)):0}, " +
                $"first release median day {Median(released.Select(r => (double)r.FirstReleaseDay)):0} ({released.Count}/{runs.Count}), " +
                $"second release median day {Median(second.Select(r => (double)r.SecondReleaseDay)):0} ({second.Count}/{runs.Count}), " +
                $"cash at end median {Median(runs.Select(r => r.CashAtEnd)):0}, game net median {Median(runs.Select(r => r.LifetimeGameNet)):0}\n" +
                $"    freelance earned median {Median(runs.Select(r => r.FreelanceEarned)):0}, idle gig blocks median {Median(runs.Select(r => (double)r.Stats.IdleFreelanceBlocks)):0}, " +
                $"vacation days median {Median(runs.Select(r => (double)r.Stats.VacationDays)):0}, collapses total {runs.Sum(r => r.Stats.Collapses)}, " +
                $"anxious weeks median {Median(runs.Select(r => (double)r.Stats.AnxiousWeeks)):0}, programming at end median {Median(runs.Select(r => r.FinalProgramming)):0.0}");
        }

        [Test]
        public void AllIn_WithoutOtherIncome_RunsOutOfMoneyEarly()
        {
            var runs = Run(BotPlayer.AllIn, 365);
            Report("All-in", runs);
            // Design/13 §12: going all-in is a funding game — broke within months without outside money.
            Assert.That(runs.Count(r => r.WentNegative && r.FirstNegativeDay <= 200), Is.GreaterThanOrEqualTo(runs.Count * 0.8));
        }

        [Test]
        public void Balanced_SurvivesByAThread_AndShipsEarly()
        {
            var runs = Run(BotPlayer.Balanced, TwoYears);
            Report("Balanced", runs);
            Assert.That(runs.Count(r => r.Bankrupt), Is.LessThanOrEqualTo(runs.Count * 0.1), "Balanced play should rarely go bankrupt");
            Assert.That(Median(runs.Select(r => r.MinCash)), Is.LessThan(Tuning.StartCash * 0.6), "Money should feel tight at some point");
            Assert.That(runs.Count(r => r.FirstReleaseDay >= 0 && r.FirstReleaseDay <= 240), Is.GreaterThanOrEqualTo(runs.Count * 0.8),
                "A Tiny first game should ship within ~8 months");
        }

        [Test]
        public void FullTimeJob_IsSafeButNeverShipsTheSmallGame_QuittingMatters()
        {
            // Design/13 §12: a Small game is a year of real work. Evenings after a full day at the job are too
            // few and too tired to finish one, which is why the premise begins with quitting.
            var job = Run(BotPlayer.FullTimeJob, TwoYears);
            var balanced = Run(BotPlayer.Balanced, TwoYears);
            Report("Full-time job", job);
            Assert.That(job.Count(r => r.Bankrupt), Is.EqualTo(0), "The job is safe");
            Assert.That(job.Count(r => r.FirstReleaseDay >= 0), Is.GreaterThanOrEqualTo(job.Count * 0.8), "A Tiny game fits in the evenings");
            Assert.That(job.Count(r => r.SecondReleaseDay >= 0), Is.LessThanOrEqualTo(job.Count * 0.1), "…a Small game doesn't");
            Assert.That(balanced.Count(r => r.SecondReleaseDay >= 0), Is.GreaterThanOrEqualTo(balanced.Count * 0.8), "Quitting ships it");
        }

        [Test]
        public void Survival_IsSafeButSlower()
        {
            var survival = Run(BotPlayer.Survival, TwoYears);
            var balanced = Run(BotPlayer.Balanced, TwoYears);
            Report("Survival", survival);
            Assert.That(survival.Count(r => r.Bankrupt), Is.EqualTo(0));
            Assert.That(Median(survival.Select(r => r.CashAtEnd)), Is.GreaterThan(Median(balanced.Select(r => r.CashAtEnd))),
                "More gigs → more cash");
            double survivalRelease = Median(survival.Where(r => r.FirstReleaseDay >= 0).Select(r => (double)r.FirstReleaseDay));
            double balancedRelease = Median(balanced.Where(r => r.FirstReleaseDay >= 0).Select(r => (double)r.FirstReleaseDay));
            Assert.That(survivalRelease, Is.GreaterThan(balancedRelease), "…but a later first release");
        }
    }
}
