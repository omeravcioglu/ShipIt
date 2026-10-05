using System.Linq;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>
    /// A scripted player used by balance tests: follows one week plan, takes gigs, builds a Tiny game
    /// then a Small one, and answers events sensibly. It plays like a competent newcomer, not an optimizer.
    /// </summary>
    public sealed class BotPlayer
    {
        readonly WeekPlan _plan;
        readonly int _maxActiveGigs;
        readonly bool _useVapor;
        readonly int _smallStorePageDays;
        bool _planApplied;

        public BotPlayer(WeekPlan plan, int maxActiveGigs, bool useVapor = true, int smallStorePageDays = 150)
        {
            _plan = plan;
            _maxActiveGigs = maxActiveGigs;
            _useVapor = useVapor;
            _smallStorePageDays = smallStorePageDays;
        }

        public int FirstReleaseDay { get; private set; } = -1;
        public int SecondReleaseDay { get; private set; } = -1;

        public static BotPlayer Balanced() => new BotPlayer(PlanTemplates.Balanced(), 2);
        public static BotPlayer AllIn() => new BotPlayer(PlanTemplates.AllIn(), 0);
        public static BotPlayer Survival() => new BotPlayer(PlanTemplates.Survival(), 3);

        /// <summary>
        /// Keeps a full-time job (weekday mornings and afternoons) and builds on three evenings and a Saturday
        /// morning, with one evening of marketing: about 56 hours a week, like the design's "job + evenings" player.
        /// </summary>
        public static BotPlayer FullTimeJob() =>
            new BotPlayer(PlanTemplates.Parse("--D|--D|--M|--D|---|D--|---"), 0) { _job = DayJob.FullTime };

        DayJob _job = DayJob.None;

        public void Play(GameSimulation sim, int days)
        {
            for (int i = 0; i < days && !sim.State.IsGameOver; i++)
            {
                Act(sim);
                sim.AdvanceDay();
            }
        }

        public void Act(GameSimulation sim)
        {
            var s = sim.State;
            if (!_planApplied)
            {
                sim.ApplyPlan(_plan);
                if (_job != DayJob.None)
                {
                    s.Founder.Job = _job;
                    s.Founder.JobStartDay = s.Day;
                }
                _planApplied = true;
            }

            AnswerMessages(sim);
            TakeGigs(sim);
            ManageProject(sim);

            if (s.Founder.Burnout >= 70 && !s.Founder.IsResting(s.Day)) sim.TakeVacation(7);
        }

        void TakeGigs(GameSimulation sim)
        {
            var s = sim.State;
            while (s.Freelance.ActiveCount < _maxActiveGigs)
            {
                var offer = s.Freelance.Offers
                    .Where(g => g.RequiredSkill <= s.Founder.Get(g.Discipline) + 0.3)
                    .OrderByDescending(g => g.EffectiveHourlyRate)
                    .FirstOrDefault();
                if (offer == null || !sim.AcceptGig(offer.Id, out _)) break;
            }
        }

        void ManageProject(GameSimulation sim)
        {
            var s = sim.State;
            var project = s.ActiveProject;
            if (project == null)
            {
                var idea = s.UnusedIdeas.OrderByDescending(i => i.JamProven).ThenByDescending(i => i.FunPotential).FirstOrDefault();
                if (idea == null) return;
                var scope = s.ShippedGames == 0 ? ProjectScope.Tiny : ProjectScope.Small;
                var started = sim.StartProject(idea.Id, scope, ArtStyleId.PixelArt, out _);
                // Reinvest like a sensible player: buy art for the first real game if the money is there.
                if (started != null && scope == ProjectScope.Small && s.Finance.Cash > 3000) sim.BuyAssetPack(started.Id, out _);
                return;
            }

            if (!project.HasStorePage && project.Progress >= 0.3)
            {
                var platform = _useVapor && s.Finance.Cash > 300 ? StorePlatform.Vapor : StorePlatform.ScratchIo;
                sim.OpenStorePage(project.Id, platform, out _);
            }

            if (project.Status == ProjectStatus.ReadyToRelease && project.HasStorePage)
            {
                int pageDays = s.Day - project.StorePageDay;
                int needed = project.Platform == StorePlatform.ScratchIo ? 0 : project.Scope == ProjectScope.Tiny ? 14 : _smallStorePageDays;
                bool polished = project.BugsRemaining < 3 || s.Day - project.CompleteDay > 28;
                if (pageDays >= needed && polished && sim.Release(project.Id, Rules.FairPrice(project.Scope), out _))
                {
                    if (FirstReleaseDay < 0) FirstReleaseDay = s.Day;
                    else if (SecondReleaseDay < 0) SecondReleaseDay = s.Day;
                }
            }
        }

        static void AnswerMessages(GameSimulation sim)
        {
            var s = sim.State;
            foreach (var m in s.Inbox.Pending.ToList())
            {
                int option = m.DefaultOption;
                switch (m.EventId)
                {
                    case "laptop_overheat": option = s.Finance.Cash > 1000 ? 0 : 1; break;
                    case "vacation_advice": option = 0; break;
                    case "parents_loan": option = 0; break;
                    case "backup_offer": option = s.Finance.Cash > 1500 ? 0 : 1; break;
                    case "gig_scope_creep": option = 0; break;
                    case "post_mortem": option = 0; break;
                    case "rent_increase": option = 0; break;
                }
                sim.ChooseOption(m.Id, option);
            }
        }
    }
}
