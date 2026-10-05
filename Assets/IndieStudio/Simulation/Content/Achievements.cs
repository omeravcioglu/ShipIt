using System;
using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// An achievement (Design/16 §4.3): a badge for a milestone, a bit of mastery, or a story the player lived.
    /// Conditions only read the game state, so they can be checked after every change and work offline. The
    /// game layer sends them to the platform (Steam) under <see cref="Id"/>, which is the achievement's API name.
    /// </summary>
    public sealed class AchievementDef
    {
        /// <summary>The API name in Steamworks (Stats &amp; Achievements), e.g. "ACH_SHIP_IT".</summary>
        public string Id;
        public string Title;
        public string Description;
        /// <summary>Hidden achievements read "Hidden achievement" until they're earned.</summary>
        public bool Hidden;
        public Func<GameState, bool> IsEarned;
        /// <summary>For counters: how far along the player is ("4 of 10"). Null for yes/no achievements.</summary>
        public Func<GameState, double> Progress;
        public double Target;
    }

    /// <summary>
    /// Every achievement, in the order the player tends to earn them. Ids never change once the game ships:
    /// Steam knows them by name. Steam/achievements.md lists them for the Steamworks setup.
    /// </summary>
    public static class AchievementCatalog
    {
        public static readonly IReadOnlyList<AchievementDef> All = new[]
        {
            // The first steps
            A("ACH_ITS_OFFICIAL", "It's Official", "Give your studio a name.",
                s => s.StudioName != GameState.DefaultStudioName),
            A("ACH_RENT_MONEY", "Rent Money", "Deliver your first gig.",
                s => s.Freelance.Profile.CompletedGigs >= 1),
            A("ACH_IT_COMPILES", "It Compiles!", "Finish the first feature of a game.",
                s => s.Projects.Any(p => p.FeaturesDone >= 1)),
            A("ACH_SHIP_IT", "Ship It!", "Release your first game.",
                s => s.ShippedGames >= 1),
            A("ACH_SOMEONE_BOUGHT_IT", "Someone Bought It", "Sell the first copy of a game.",
                s => s.Flags.Contains("first_sale")),
            A("ACH_COMING_SOON", "Coming Soon", "Open a store page on Vapor.",
                s => s.Projects.Any(p => p.Platform == StorePlatform.Vapor)),

            // The road to a real studio
            A("ACH_A_REAL_INDIE_GAME", "A Real Indie Game", "Release a Small game.",
                s => s.ReleasedProjects.Any(p => p.Scope == ProjectScope.Small)),
            Count("ACH_WISHLISTED", "Wishlisted", "Reach 1,000 wishlists on one game.", 1000,
                s => s.Projects.Select(p => Math.Max(p.Wishlists, p.Result?.LaunchWishlists ?? 0)).DefaultIfEmpty(0).Max()),
            Count("ACH_FOUR_DIGITS", "Four Digits", "Sell 1,000 copies of one game.", 1000,
                s => s.ReleasedProjects.Select(p => p.UnitsSold).DefaultIfEmpty(0).Max()),
            A("ACH_VERY_POSITIVE", "Very Positive", "Release a game that players rate Very Positive.",
                s => s.ReleasedProjects.Any(p => p.Result.ReviewCount >= 50 && p.Result.PositiveShare >= 0.8)),
            A("ACH_FULL_TIME_INDIE", "Full-Time Indie", "Live off your games: finish chapter 4.",
                s => s.ChaptersCompleted.ContainsKey("full_time")),

            // Getting good
            A("ACH_BRILLIANT", "Brilliant!", "Earn three stars in a work session.",
                s => s.Stats.BrilliantSessions >= 1),
            Count("ACH_PERFECTIONIST", "Perfectionist", "Earn three stars in 50 work sessions.", 50,
                s => s.Stats.BrilliantSessions),
            A("ACH_IN_THE_ZONE", "In the Zone", "Reach full flow: four sessions in a row on the same work.",
                s => s.FlowStreak >= 4),
            A("ACH_EXPERT", "Expert", "Reach level 5 in a craft skill.",
                s => SkillInfo.Craft.Any(k => s.Founder.Level(k) >= 5)),
            A("ACH_FIVE_STARS", "Five Stars", "Get a perfect 5/5 from a client.",
                s => s.Freelance.Gigs.Any(g => g.Status == GigStatus.Completed && g.ClientScore >= 5)),
            A("ACH_72_HOURS", "72 Hours", "Finish a game jam.",
                s => s.Ideas.Any(i => i.JamProven)),

            // Life in the bedroom
            A("ACH_DOUBLE_SHOT", "Double Shot", "Drink two coffees in one day.",
                s => s.CoffeesToday >= Tuning.CoffeesPerDay),
            Count("ACH_CAT_PERSON", "Cat Person", "Pet the cat on 10 different days.", 10,
                s => s.Stats.CatPets),
            Count("ACH_SELF_CARE", "Self-Care", "Spend seven days on vacation.", 7,
                s => s.Stats.VacationDays),
            A("ACH_BACK_TO_INDIE", "Back to Indie", "Quit a day job to make games again.",
                s => s.EventLastDay.ContainsKey("job_quit")),
            A("ACH_BACK_FROM_THE_BRINK", "Back from the Brink", "Climb back to $5,000 after going into debt.",
                s => s.Finance.EverNegative && s.Finance.Cash >= 5000),
            Hidden("ACH_THE_HARD_WAY", "The Hard Way", "Collapse from burnout.",
                s => s.Stats.Collapses >= 1),
            Hidden("ACH_KILL_YOUR_DARLINGS", "Kill Your Darlings", "Shelve a project.",
                s => s.Projects.Any(p => p.Status == ProjectStatus.Cancelled)),
        };

        public static AchievementDef Get(string id) => All.FirstOrDefault(a => a.Id == id);

        /// <summary>The achievements this studio has earned so far.</summary>
        public static IEnumerable<AchievementDef> Earned(GameState state) => All.Where(a => a.IsEarned(state));

        static AchievementDef A(string id, string title, string description, Func<GameState, bool> isEarned) =>
            new AchievementDef { Id = id, Title = title, Description = description, IsEarned = isEarned };

        static AchievementDef Hidden(string id, string title, string description, Func<GameState, bool> isEarned) =>
            new AchievementDef { Id = id, Title = title, Description = description, IsEarned = isEarned, Hidden = true };

        static AchievementDef Count(string id, string title, string description, double target, Func<GameState, double> progress) =>
            new AchievementDef { Id = id, Title = title, Description = description, Target = target, Progress = progress, IsEarned = s => progress(s) >= target };
    }
}
