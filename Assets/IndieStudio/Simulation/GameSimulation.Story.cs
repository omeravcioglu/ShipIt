using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// The studio's story: the milestones worth looking back on (your first gig, every launch, a hundred followers…),
    /// so the player can see how far they've come. The room and "Your story so far" are built from it.
    /// </summary>
    public sealed partial class GameSimulation
    {
        /// <summary>Adds a moment to the studio's story, dated today.</summary>
        void Remember(StoryKind kind, string text) => State.Story.Add(new StoryEntry { Day = State.Day, Kind = kind, Text = text });

        /// <summary>Fan milestones reached so far (each pins a note to the corkboard in the room).</summary>
        public int FanMilestonesReached => Tuning.FanMilestones.Count(m => State.Audience.Followers >= m);

        /// <summary>Story moments that come from crossing a line rather than from one action: follower milestones.</summary>
        void CheckStoryMilestones()
        {
            foreach (int milestone in Tuning.FanMilestones)
            {
                if (State.Audience.Followers < milestone || milestone < 100) continue;
                if (State.Flags.Add($"story:fans_{milestone}")) Remember(StoryKind.Fans, $"Reached {milestone:#,0} followers.");
            }
        }

        static string Ordinal(int n)
        {
            if (n % 100 >= 11 && n % 100 <= 13) return n + "th";
            switch (n % 10)
            {
                case 1: return n + "st";
                case 2: return n + "nd";
                case 3: return n + "rd";
                default: return n + "th";
            }
        }
    }
}
