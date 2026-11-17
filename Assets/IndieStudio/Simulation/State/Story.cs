namespace IndieStudio.Simulation
{
    /// <summary>What a moment in the studio's story was about (the UI picks an icon for it).</summary>
    public enum StoryKind { Start, Gig, Project, Feature, Store, Launch, Money, Fans, Level, Chapter, Job, Jam, Shop, Setback }

    /// <summary>
    /// One moment worth remembering ("Launched Lantern Hollow on scratch.io"), shown in "Your story so far" so you can
    /// see how far the studio has come.
    /// </summary>
    public sealed class StoryEntry
    {
        public int Day;
        public StoryKind Kind;
        public string Text;
    }
}
