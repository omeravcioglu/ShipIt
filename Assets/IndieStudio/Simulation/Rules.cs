using System;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// Pure formulas. Anything the UI explains with a "Why?" tooltip returns a <see cref="Breakdown"/>.
    /// References point to the design documents in Design/.
    /// </summary>
    public static class Rules
    {
        public static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        /// <summary>Work efficiency: skill 2 → 0.73, skill 5 → 1.00, skill 10 → 1.45 (Design/05 §5).</summary>
        public static double Efficiency(double skill) => Tuning.EfficiencyBase + Tuning.EfficiencyPerSkill * skill;

        public static double BurnoutFactor(double burnout) =>
            1 - Tuning.BurnoutPenaltyPerPoint * Math.Max(0, burnout - Tuning.BurnoutPenaltyStart);

        public static double MoraleFactor(double morale) => 0.9 + 0.2 * Clamp(morale, 0, 100) / 100.0;

        /// <summary>Burnout, morale, housing noise and a borrowed laptop all scale how much a block achieves.</summary>
        public static Breakdown Productivity(GameState state)
        {
            var f = state.Founder;
            var b = new Breakdown("Base", 1.0)
                .Multiply($"Burnout {f.Burnout:0}", BurnoutFactor(f.Burnout))
                .Multiply($"Morale {f.Morale:0}", MoraleFactor(f.Morale));
            if (state.Day <= state.NoisyUntilDay) b.Multiply("Noisy flatmates", 0.9);
            if (state.Day <= state.BorrowedLaptopUntilDay) b.Multiply("Borrowed laptop", 0.85);
            return b;
        }

        public static double HardwareFactor(GameState state, Project project)
        {
            if (project == null || !GameContent.ArtStyle(project.ArtStyle).Is3D) return 1.0;
            return state.Owned.Contains("gaming_pc") ? Tuning.GamingPc3DFactor : Tuning.Laptop3DPenalty;
        }

        /// <summary>Person-weeks of work one 4-hour Develop block produces in a discipline.</summary>
        public static Breakdown DevelopOutput(GameState state, Project project, Skill discipline)
        {
            double skill = state.Founder.Get(discipline);
            var b = new Breakdown($"{discipline} {skill:0.0} → efficiency {Efficiency(skill):0.00} × 4h", Tuning.BlockHours / Tuning.PersonWeekHours * Efficiency(skill));
            b.Multiply("Productivity (burnout, morale)", Productivity(state).Value);
            double hardware = HardwareFactor(state, project);
            if (hardware < 1) b.Multiply(state.Owned.Contains("gaming_pc") ? "3D builds on a gaming PC" : "3D builds on a cheap laptop", hardware);
            if (discipline == Skill.Art && state.Owned.Contains("pro_art_tools") && skill >= 4) b.Multiply("Pro art tools", 1.1);
            return b;
        }

        public static double TaskDifficulty(GameState state, Project project, Skill discipline)
        {
            if (discipline == Skill.Art)
            {
                double floor = GameContent.ArtStyle(project.ArtStyle).SkillFloor;
                return state.Lessons.Contains("know_your_limits") ? 1 + (floor - 1) * 0.7 : floor;
            }
            return project.Scope == ProjectScope.Tiny ? 2.0 : 3.5;
        }

        /// <summary>
        /// Quality of a task (1–10): skill 2 → 4.0, skill 5 → 6.25, skill 10 → 10, minus half of any gap
        /// to the task's difficulty. Beginners make rough work, not worthless work.
        /// </summary>
        public static double TaskQuality(double skill, double difficulty, double bonus) =>
            Clamp(2.5 + 0.75 * skill - Math.Max(0, difficulty - skill) * 0.5 + bonus, 1, 10);

        /// <summary>Culture-independent percentage ("42%"): invariant-culture "P0" would print "42 %".</summary>
        public static string Pct(double fraction) =>
            Math.Round(fraction * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";

        public static Breakdown FreelanceRate(double skill, double rating, RateSetting setting)
        {
            var b = new Breakdown("Base rate", Tuning.RateBase).Add($"Skill {skill:0.0} × ${Tuning.RatePerSkill:0.00}", Tuning.RatePerSkill * skill);
            if (rating > 0) b.Multiply($"Rating {rating:0.0}/5", 1 + Tuning.RatingBonusMax * rating / 5);
            if (setting == RateSetting.Budget) b.Multiply("Budget pricing", 0.8);
            if (setting == RateSetting.Premium) b.Multiply("Premium pricing", 1.25);
            return b;
        }

        /// <summary>
        /// New studios underestimate work: shown estimates are true × bias (Design/05 §6). The game never
        /// says so; the first post-mortem reveals the overrun.
        /// </summary>
        public static double EstimateBias(GameState state)
        {
            double bias = Tuning.EstimateBiasStart
                          + Tuning.EstimateBiasPerProductionLevel * (state.Founder.Get(Skill.Production) - 1)
                          + Tuning.EstimateBiasPerShippedGame * state.ShippedGames
                          + (state.Lessons.Contains("buffer_estimates") ? 0.10 : 0);
            return Math.Min(Tuning.EstimateBiasMax, bias);
        }

        public static double MarketingFactor(GameState state) =>
            (1 + 0.06 * (state.Founder.Get(Skill.Marketing) - 1)) * (state.Lessons.Contains("market_early") ? 1.15 : 1.0);

        public static double FairPrice(ProjectScope scope) => scope == ProjectScope.Tiny ? 3.99 : 12.99;

        public static double PolishBonus(GameState state, Project project)
        {
            double effectiveness = state.Lessons.Contains("polish_pays") ? 1.5 : 1.0;
            return Math.Min(1.0, project.PolishPw * effectiveness / Math.Max(Tuning.PolishFloorPw, project.TotalRequired * 0.15));
        }

        /// <summary>
        /// Turns a minigame score (0–1) into a work multiplier: 0 → ×0.5, about 0.7 → ×1.0 (an ordinary
        /// session), 1 → ×1.2. Playing well makes a session count for more; it never makes it worthless.
        /// </summary>
        public static double MinigameMultiplier(double score) => Tuning.MinigameFloor + Tuning.MinigameRange * Clamp(score, 0, 1);

        /// <summary>The minigame score that counts as an ordinary session (×1.0).</summary>
        public static double OrdinaryScore => (1.0 - Tuning.MinigameFloor) / Tuning.MinigameRange;

        /// <summary>
        /// Computes the eight pillars and the overall quality (0–1) exactly as launch will judge them.
        /// The UI only ever shows this through a knowledge-dependent range (Design/17).
        /// </summary>
        public static ReleaseResult EvaluateQuality(GameState state, Project project, double price)
        {
            var genre = GameContent.Genre(project.Genre);
            var style = GameContent.ArtStyle(project.ArtStyle);
            double polish = PolishBonus(state, project);
            double Q(Skill s) => project.Stream(s)?.Quality ?? 1;

            var r = new ReleaseResult();
            r.Pillars[Pillar.Gameplay] = Clamp(0.45 * Q(Skill.Design) + 0.25 * Q(Skill.Programming) + 3.0 * project.FunPotential + polish, 1, 10);
            r.Pillars[Pillar.Graphics] = Clamp(Math.Min(style.Ceiling, Q(Skill.Art) + 1.2 * GameContent.StyleFit(project.Genre, project.ArtStyle)) + 0.5 * polish, 1, 10);
            r.Pillars[Pillar.Audio] = Clamp(Q(Skill.Audio) + 0.3 * polish, 1, 10);
            r.Pillars[Pillar.Story] = Clamp(Q(Skill.Writing), 1, 10);
            double valueRatio = FairPrice(project.Scope) / Math.Max(0.99, price);
            r.Pillars[Pillar.Content] = Clamp((project.Scope == ProjectScope.Tiny ? 6.0 : 7.0) + 2.0 * (valueRatio - 1), 1, 10);
            r.Pillars[Pillar.Innovation] = Clamp(3.5 + 5.0 * project.HookStrength + (project.JamProven ? 0.5 : 0) - (project.BoughtAssetPack ? 0.6 : 0), 1, 10);
            r.Stability = Clamp(10 - project.BugsRemaining * 0.25, 1, 10);

            double weighted = genre.Weights.Sum(kv => kv.Value * r.Pillars[kv.Key]) / 10.0;
            r.Quality = Clamp(weighted * (1 - 0.03 * (10 - r.Stability)), 0.05, 0.98);
            return r;
        }

        /// <summary>
        /// Share of positive player reviews for a quality and price (Design/11 §2). Players expect less
        /// from a cheap tiny game, so the neutral point is lower for Tiny scope.
        /// </summary>
        public static double PositiveShare(double quality, double price, ProjectScope scope)
        {
            double value = (FairPrice(scope) - price) / FairPrice(scope) * 0.10;
            double neutral = scope == ProjectScope.Tiny ? 0.42 : 0.5;
            double logistic = 1.0 / (1.0 + Math.Exp(-(quality - neutral) * 7.0));
            return Clamp(logistic + value, 0.02, 0.99);
        }

        public static double StorePageFactor(GameState state, Project project, double quality)
        {
            double pop = GameContent.ArtStyle(project.ArtStyle).ScreenshotPop;
            double factor = (0.6 + 0.8 * quality) * (0.8 + 0.06 * pop);
            return state.Lessons.Contains("hook_first") ? factor * 1.1 : factor;
        }

        public static double ActiveGigCapacityHours(GameState state) =>
            state.Freelance.Active.Sum(g => Math.Max(0, g.Hours - g.HoursDone));

        public static double RemainingWorkEstimate(GameState state, Project project) =>
            project.TotalRemaining * EstimateBias(state);

        public static string Stars(double rating) => rating <= 0 ? "New" : $"{rating:0.0}/5";

        public static string Money(double amount)
        {
            string sign = amount < 0 ? "-" : "";
            double abs = Math.Abs(amount);
            if (abs >= 100000) return $"{sign}${abs / 1000:0}k";
            if (abs >= 10000) return $"{sign}${abs / 1000:0.0}k";
            return $"{sign}${abs:#,0}";
        }
    }
}
