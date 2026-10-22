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
    /// The demo: chapter 1 of the full game, made small enough to finish in 15–20 minutes. You start a studio, take
    /// Sam's gig, build a Tiny game feature by feature and launch it on scratch.io; the launch-day show is the
    /// ending. Everything is the full game's rules, with fewer doors open:
    /// - Tiny games only, and the demo's Tiny game is a little smaller (about a dozen Build sessions).
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
        /// The demo's Tiny game is this share of a full-game Tiny game: about a dozen Build sessions for a beginner,
        /// so with Sam's gig and the tutorial the demo takes 15–20 minutes (DemoTests measures it).
        /// </summary>
        public const double TinyWorkScale = 0.7;

        /// <summary>Days of the demo left on <paramref name="day"/>, counting that day.</summary>
        public static int DaysLeft(int day) => System.Math.Max(0, Days - day);
    }
}
