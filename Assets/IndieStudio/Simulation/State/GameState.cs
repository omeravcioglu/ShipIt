using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>The complete state of one playthrough. Plain data; all rules live in GameSimulation.</summary>
    public sealed class GameState
    {
        public ulong Seed;
        public int Day;
        /// <summary>The part of <see cref="Day"/> the player is in: 0 morning, 1 afternoon, 2 evening, 3 = night (all used).</summary>
        public int Slot;
        /// <summary>True once the start of <see cref="Day"/> (payments, bills, Monday's offers) has run.</summary>
        public bool DayStarted;
        /// <summary>What each slot of the current day achieved, in order.</summary>
        public readonly List<SlotReport> Today = new List<SlotReport>();

        /// <summary>Index of the current story chapter in <see cref="ChapterCatalog.All"/> (Design/16 §4).</summary>
        public int Chapter;
        public int ChapterStartDay;
        /// <summary>Chapter id → the day it was completed.</summary>
        public readonly Dictionary<string, int> ChaptersCompleted = new Dictionary<string, int>();

        /// <summary>Where the first-day tutorial is (the UI's coach reads and writes these, so a save resumes it).</summary>
        public int TutorialStep;
        public int TutorialStepDay;
        public bool TutorialDone;

        public const string DefaultStudioName = "Bedroom Games";

        /// <summary>The player's name for their studio (asked on day one).</summary>
        public string StudioName = DefaultStudioName;

        /// <summary>
        /// Flow (Design/02 §4): consecutive sessions on the same work (the same discipline of your game, or the
        /// same gig) build momentum. <see cref="FlowKey"/> names that work; other work breaks the streak, rest doesn't.
        /// </summary>
        public string FlowKey;
        public int FlowStreak;

        /// <summary>A coffee is waiting to boost the next work session.</summary>
        public bool CoffeeReady;
        public int CoffeesToday;
        public int CatPettedDay = -1;

        public readonly Founder Founder = new Founder();
        public readonly Finance Finance = new Finance();
        public readonly FreelanceState Freelance = new FreelanceState();
        public readonly Audience Audience = new Audience();
        public readonly Inbox Inbox = new Inbox();
        /// <summary>The repeating week used by <see cref="GameSimulation.AdvanceDay"/> (bots, tests and balancing).</summary>
        public WeekPlan Plan = PlanTemplates.Standard();
        public readonly List<PlanOverride> Overrides = new List<PlanOverride>();
        public readonly List<Project> Projects = new List<Project>();
        public readonly List<IdeaCard> Ideas = new List<IdeaCard>();

        /// <summary>One-shot facts ("cooling_pad_offered", "laptop_risky" …).</summary>
        public readonly HashSet<string> Flags = new HashSet<string>();
        /// <summary>Owned shop items and active subscriptions.</summary>
        public readonly HashSet<string> Owned = new HashSet<string>();
        /// <summary>Lessons internalized from post-mortems (Design/17 §5).</summary>
        public readonly HashSet<string> Lessons = new HashSet<string>();
        /// <summary>Discovered facts shown in the Knowledge base, e.g. "Horror × Retro PS1: great fit".</summary>
        public readonly List<string> Knowledge = new List<string>();
        public readonly Dictionary<string, int> EventLastDay = new Dictionary<string, int>();
        public readonly Dictionary<string, int> GoalsCompleted = new Dictionary<string, int>();

        public readonly LifetimeStats Stats = new LifetimeStats();
        public WeekLog CurrentWeek = new WeekLog();
        public WeekLog LastWeek;
        public double ResearchPoints;
        public double JamProgress;
        public int NoisyUntilDay = -1;
        public int BorrowedLaptopUntilDay = -1;
        public int NextId = 1;
        public int ShippedGames;
        public double LifetimeGameNet;
        public string GameOverReason;

        public SimRandom RngGigs;
        public SimRandom RngEvents;
        public SimRandom RngSales;
        public SimRandom RngWork;
        public SimRandom RngIdeas;
        public SimRandom RngMarketing;

        public bool IsGameOver => GameOverReason != null;

        public Project ActiveProject =>
            Projects.FirstOrDefault(p => p.Status == ProjectStatus.InDevelopment || p.Status == ProjectStatus.ReadyToRelease);

        public IEnumerable<Project> ReleasedProjects => Projects.Where(p => p.Status == ProjectStatus.Released);
        public IEnumerable<IdeaCard> UnusedIdeas => Ideas.Where(i => !i.Used);
    }
}
