namespace IndieStudio.Simulation
{
    /// <summary>Which game this is: the full game, or the free demo.</summary>
    public enum Edition
    {
        Full = 0,
        Demo = 1,
    }

    /// <summary>How the demo ended.</summary>
    public enum DemoEnd
    {
        None = 0,
        /// <summary>You shipped your first game: the demo's goal, and its climax.</summary>
        Shipped = 1,
        /// <summary>The demo's two weeks ran out first.</summary>
        OutOfTime = 2,
    }

    /// <summary>
    /// The demo: chapter 1 of the full game, made small enough to finish in 10–15 minutes, with a win every minute or
    /// two. You start a studio, take Sam's gig, build a Tiny game feature by feature and launch it on scratch.io; the
    /// launch-day show is the ending. Everything is the full game's rules, with fewer doors open:
    /// - Tiny games only, and the demo's Tiny game is smaller: about one Build session per feature, so most sessions
    ///   end with "Feature complete!".
    /// - A quick study: a few skills start just short of their next level, so the first level-ups come within minutes.
    /// - A stretch goal: launch by Sunday the 7th and a games blog features the game on launch day.
    /// - scratch.io only: Vapor pages, wishlists and Next Fest belong to the full game.
    /// - Two weeks of in-game time (Monday 1 to Sunday 14 January), with a heads-up three days before the end.
    /// The interface hides the rest (marketing, research, learning, the shop, finances and skills screens).
    /// </summary>
    public static class DemoRules
    {
        /// <summary>The demo's calendar: the morning of this day (Monday 15 January) never comes.</summary>
        public const int Days = 14;

        /// <summary>A note from Doug the morning this many days of the demo are left, if the game isn't out yet.</summary>
        public const int WarningDaysLeft = 3;

        /// <summary>
        /// The demo's Tiny game is this share of a full-game Tiny game: about one Build session per feature for a new
        /// player, so with Sam's gig and the tutorial the demo takes 10–15 minutes (DemoTests measures it).
        /// </summary>
        public const double TinyWorkScale = 0.5;

        /// <summary>Sam's gig is shorter in the demo: two sessions of Bug Squash instead of three.</summary>
        public const double SamGigHours = 8;

        /// <summary>
        /// The demo's founder is a quick study: these skills start just short of their next level, so Programming
        /// levels up as Sam's gig is delivered, and Art and Audio while you build (each one a small celebration).
        /// </summary>
        public static readonly (Skill Skill, double Value)[] StartingSkills =
        {
            (Skill.Programming, 2.975), (Skill.Design, 2), (Skill.Art, 1.9), (Skill.Writing, 1.5), (Skill.Audio, 1.95),
        };

        /// <summary>
        /// The stretch goal: launch before this day (by Sunday the 7th, the end of the first week) and a games blog
        /// features the game on launch day. A typical player makes it with days to spare; a careless one might not.
        /// </summary>
        public const int FeatureByDay = 7;
        public const string FeatureOutlet = "PixelPost";
        /// <summary>What the feature brings: this many times the first month's copies, and new followers.</summary>
        public const double FeatureSalesBoost = 1.6;
        public const int FeatureFollowers = 25;
        /// <summary>Set when the game was featured (kept in the state's flags, so it's saved).</summary>
        public const string FeaturedFlag = "demo_featured";

        /// <summary>Days of the demo left on <paramref name="day"/>, counting that day.</summary>
        public static int DaysLeft(int day) => System.Math.Max(0, Days - day);
    }
}
