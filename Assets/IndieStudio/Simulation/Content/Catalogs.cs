using System;
using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// A purchasable upgrade. Every item names the problem it solves (Design/08 §3): the game makes
    /// you feel the problem first, then the item becomes desirable.
    /// </summary>
    public sealed class ShopItem
    {
        public string Id;
        public string Name;
        public string Description;
        public string Solves;
        public double Price;
        public double Monthly;
        public TxCategory Category;
    }

    public static class ShopCatalog
    {
        public static readonly IReadOnlyList<ShopItem> Items = new[]
        {
            new ShopItem { Id = "cooling_pad", Name = "Laptop cooling pad", Price = 120, Category = TxCategory.Hardware,
                Description = "Keeps the old laptop from overheating.", Solves = "Overheating and sudden laptop failures" },
            new ShopItem { Id = "backups", Name = "Cloud backup", Monthly = 10, Category = TxCategory.Software,
                Description = "Nightly backups of your projects.", Solves = "Losing weeks of work when hardware dies" },
            new ShopItem { Id = "ergonomic_chair", Name = "Ergonomic chair", Price = 400, Category = TxCategory.Hardware,
                Description = "Burnout builds up 10% slower.", Solves = "Back pain from working on a kitchen chair" },
            new ShopItem { Id = "gaming_pc", Name = "Gaming PC", Price = 1800, Monthly = 20, Category = TxCategory.Hardware,
                Description = "3D builds and bakes stop eating your hours (+$20/month electricity).", Solves = "Slow 3D work on the laptop (−20% output on 3D projects)" },
            new ShopItem { Id = "pro_art_tools", Name = "Pro art software", Monthly = 35, Category = TxCategory.Software,
                Description = "+10% art speed and quality — only if your Art skill is 4 or higher.", Solves = "Free tools holding a skilled artist back" },
        };

        public static ShopItem Get(string id) => Items.FirstOrDefault(i => i.Id == id);
    }

    public sealed class LessonDef
    {
        public string Id;
        public string Title;
        public string Effect;
    }

    /// <summary>Permanent studio practices learned from post-mortems. Failure teaches the best ones.</summary>
    public static class LessonCatalog
    {
        public static readonly IReadOnlyList<LessonDef> All = new[]
        {
            new LessonDef { Id = "buffer_estimates", Title = "Buffer your estimates", Effect = "Future estimates are 10% more realistic." },
            new LessonDef { Id = "market_early", Title = "Market from day one", Effect = "Marketing sessions gain 15% more followers and wishlists." },
            new LessonDef { Id = "polish_pays", Title = "Polish pays off", Effect = "Polishing is 50% more effective." },
            new LessonDef { Id = "know_your_limits", Title = "Pick styles you can pull off", Effect = "Art-style skill-floor penalties are 30% smaller." },
            new LessonDef { Id = "ship_small", Title = "Ship small, ship often", Effect = "Tiny projects need 10% less work." },
            new LessonDef { Id = "hook_first", Title = "Lead with the hook", Effect = "Store pages convert 10% better." },
        };

        public static LessonDef Get(string id) => All.FirstOrDefault(l => l.Id == id);
    }

    public sealed class ChapterStep
    {
        public string Text;
        public Func<GameState, bool> IsDone;
    }

    public sealed class ChapterDef
    {
        public string Id;
        public int Number;
        public string Title;
        /// <summary>The one-line goal shown in the HUD.</summary>
        public string Goal;
        /// <summary>Said when the chapter is completed.</summary>
        public string Outro;
        public IReadOnlyList<ChapterStep> Steps;

        public bool IsComplete(GameState state) => Steps.Count > 0 && Steps.All(s => s.IsDone(state));
        public int StepsDone(GameState state) => Steps.Count(s => s.IsDone(state));
    }

    /// <summary>
    /// The story goals (Design/16 §4): one clear thing to work toward at any time, broken into steps.
    /// Side goals (<see cref="GoalCatalog"/>) run alongside. The last chapter has no steps: it is the sandbox.
    /// </summary>
    public static class ChapterCatalog
    {
        public static readonly IReadOnlyList<ChapterDef> All = new[]
        {
            new ChapterDef
            {
                Id = "bedroom", Number = 1, Title = "The Bedroom", Goal = "Ship your first game",
                Outro = "You made a game and people can buy it. Most people who dream about this never get here. Morale +8.",
                Steps = new[]
                {
                    Step("Deliver your first gig", s => s.Freelance.Profile.CompletedGigs >= 1),
                    Step("Start your first game", s => s.Projects.Count > 0),
                    Step("Build every feature", s => s.Projects.Any(p => p.Status == ProjectStatus.ReadyToRelease || p.IsReleased)),
                    Step("Open a store page", s => s.Projects.Any(p => p.HasStorePage)),
                    Step("Launch it", s => s.ShippedGames >= 1),
                },
            },
            new ChapterDef
            {
                Id = "players", Number = 2, Title = "Finding Players", Goal = "Find your players",
                Outro = "Strangers play your games and some of them follow what you do next. That's the start of an audience. Morale +8.",
                Steps = new[]
                {
                    Step("Read your first post-mortem", s => s.Projects.Any(p => p.PostMortemDay >= 0)),
                    Step("Reach 150 followers", s => s.Audience.Followers >= 150),
                    Step("Open a Vapor store page", s => s.Projects.Any(p => p.Platform == StorePlatform.Vapor)),
                    Step("Earn $500 from your games", s => s.LifetimeGameNet >= 500),
                },
            },
            new ChapterDef
            {
                Id = "real_indie", Number = 3, Title = "A Real Indie Game", Goal = "Ship a Small game",
                Outro = "A real indie game with real reviews. You're not a hobbyist any more. Morale +8.",
                Steps = new[]
                {
                    Step("Start a Small project", s => s.Projects.Any(p => p.Scope == ProjectScope.Small)),
                    Step("Reach 500 wishlists on it", s => s.Projects.Any(p => p.Scope == ProjectScope.Small && (p.Result?.LaunchWishlists ?? p.Wishlists) >= 500)),
                    Step("Launch it", s => s.ReleasedProjects.Any(p => p.Scope == ProjectScope.Small)),
                    Step("Get 70% positive reviews", s => s.ReleasedProjects.Any(p => p.Scope == ProjectScope.Small && p.Result.ReviewCount >= 10 && p.Result.PositiveShare >= 0.7)),
                },
            },
            new ChapterDef
            {
                Id = "full_time", Number = 4, Title = "Full-Time Indie", Goal = "Live off your games",
                Outro = "Your games pay the bills. Next comes the hard part: growing beyond yourself. Morale +8.",
                Steps = new[]
                {
                    Step("Reach 1,000 followers", s => s.Audience.Followers >= 1000),
                    Step("Have a month where games pay all the bills", s => s.Finance.Reports.Any(r =>
                        r.ByCategory.TryGetValue(TxCategory.GameSales, out double sales) && sales >= s.Finance.MonthlyRecurringTotal)),
                    Step("Earn $10,000 from your games", s => s.LifetimeGameNet >= 10000),
                },
            },
            new ChapterDef
            {
                Id = "next", Number = 5, Title = "To Be Continued", Goal = "Keep growing",
                Outro = "", Steps = new ChapterStep[0],
            },
        };

        static ChapterStep Step(string text, Func<GameState, bool> isDone) => new ChapterStep { Text = text, IsDone = isDone };
    }

    public sealed class GoalDef
    {
        public string Id;
        public string Title;
        public Func<GameState, bool> IsComplete;
    }

    /// <summary>Short-term side objectives ("there's always another goal", Design/16 §4).</summary>
    public static class GoalCatalog
    {
        public static readonly IReadOnlyList<GoalDef> All = new[]
        {
            new GoalDef { Id = "first_gig", Title = "Finish your first gig", IsComplete = s => s.Freelance.Profile.CompletedGigs >= 1 },
            new GoalDef { Id = "first_project", Title = "Start your first game", IsComplete = s => s.Projects.Count > 0 },
            new GoalDef { Id = "store_page", Title = "Open a store page", IsComplete = s => s.Projects.Any(p => p.HasStorePage) },
            new GoalDef { Id = "first_release", Title = "Ship your first game", IsComplete = s => s.ShippedGames >= 1 },
            new GoalDef { Id = "followers_500", Title = "Reach 500 followers", IsComplete = s => s.Audience.Followers >= 500 },
            new GoalDef { Id = "craft_3", Title = "Reach level 3 in a craft skill", IsComplete = s => SkillInfo.Craft.Any(k => s.Founder.Level(k) >= 3) },
            new GoalDef { Id = "rating_4", Title = "Earn a 4/5 freelance rating", IsComplete = s => s.Freelance.Profile.RecentScores.Count >= 3 && s.Freelance.Profile.Rating >= 4 },
            new GoalDef { Id = "six_months_black", Title = "Stay out of the red for 6 months", IsComplete = s => s.Day >= 181 && !s.Finance.EverNegative },
            new GoalDef { Id = "game_1000", Title = "Earn $1,000 from your games", IsComplete = s => s.LifetimeGameNet >= 1000 },
            new GoalDef { Id = "wishlists_1000", Title = "Reach 1,000 wishlists on one game", IsComplete = s => s.Projects.Any(p => p.Wishlists >= 1000) },
            new GoalDef { Id = "second_release", Title = "Ship a second game", IsComplete = s => s.ShippedGames >= 2 },
            new GoalDef { Id = "game_10000", Title = "Earn $10,000 from your games", IsComplete = s => s.LifetimeGameNet >= 10000 },
        };
    }
}
