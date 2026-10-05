using System.Collections.Generic;

namespace IndieStudio.Simulation
{
    /// <summary>The kind of post a Marketing session makes. Each trades reliability against reach (Design/09 §4).</summary>
    public enum MarketingPost
    {
        /// <summary>A polished screenshot: the default, best for wishlists.</summary>
        Screenshot,
        /// <summary>A devlog: steady, builds followers, fewer wishlists.</summary>
        Devlog,
        /// <summary>A meme or GIF: usually flops, sometimes explodes.</summary>
        Meme,
    }

    /// <summary>
    /// What one morning, afternoon or evening achieved (Design/02 §4). Shown on the result card after the
    /// slot and in the evening's summary of the day.
    /// </summary>
    public sealed class SlotReport
    {
        /// <summary>Starts the line for each feature a session finished: "Finished: Jump physics (Programming)".</summary>
        public const string FinishedPrefix = "Finished: ";

        public int Day;
        /// <summary>0 = morning, 1 = afternoon, 2 = evening.</summary>
        public int Slot;
        public PlanBlock Activity;
        /// <summary>The work multiplier earned in the minigame; 1 is an ordinary session.</summary>
        public double Performance = 1;
        /// <summary>True when the slot was decided for you: rest days, the day job, a jam weekend.</summary>
        public bool Forced;

        /// <summary>One line, e.g. "Built Hero sprite" or "Worked on Proofread game text".</summary>
        public string Title = "";
        /// <summary>What the session worked on (a feature or a gig), if anything.</summary>
        public string Subject;
        public bool HasSkill;
        public Skill Skill;
        /// <summary>Progress of <see cref="Subject"/> before and after the session (0–1), or −1 when there is none.</summary>
        public double ProgressBefore = -1;
        public double ProgressAfter = -1;
        public readonly List<string> Lines = new List<string>();

        public bool HasProgress => ProgressBefore >= 0 && ProgressAfter >= 0;
    }
}
