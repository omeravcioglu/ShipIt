using System;
using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    public enum ProjectScope { Tiny, Small }

    public enum ProjectStatus { InDevelopment, ReadyToRelease, Released, Cancelled }

    public enum StorePlatform { None, ScratchIo, Vapor }

    public sealed class Workstream
    {
        public Skill Discipline;
        public double Required;
        public double Done;
        /// <summary>Σ(work × task quality), so Quality is the work-weighted average (Design/05 §7).</summary>
        public double QualitySum;
        /// <summary>
        /// The named features this discipline's work is split into ("Hero sprite", "Level 1" …). They are built
        /// in order and share <see cref="Required"/> equally, so they are a view of the work, not extra state.
        /// </summary>
        public readonly List<string> Features = new List<string>();

        public double Remaining => Required - Done > 1e-9 ? Required - Done : 0;
        public double Quality => Done > 0 ? QualitySum / Done : 0;
        public bool IsDone => Remaining <= 0;

        public int FeatureCount => Features.Count > 0 ? Features.Count : 1;
        public double FeatureSize => Required / FeatureCount;
        public int FeaturesDone => IsDone ? FeatureCount : Math.Min(FeatureCount, (int)Math.Floor(Done / FeatureSize + 1e-9));
        /// <summary>The feature being built now (the last one once the discipline is finished).</summary>
        public int CurrentFeature => Math.Min(FeatureCount - 1, FeaturesDone);

        public string FeatureName(int index) =>
            Features.Count == 0 ? Discipline.ToString() : Features[Math.Max(0, Math.Min(Features.Count - 1, index))];

        public double FeatureProgress(int index)
        {
            if (IsDone || FeatureSize <= 0) return 1;
            double progress = (Done - index * FeatureSize) / FeatureSize;
            return progress < 0 ? 0 : progress > 1 ? 1 : progress;
        }
    }

    public sealed class Project
    {
        public int Id;
        public string Title;
        public int IdeaId;
        public GenreId Genre;
        public string Theme;
        public string Hook;
        public ArtStyleId ArtStyle;
        public ProjectScope Scope;
        public ProjectStatus Status = ProjectStatus.InDevelopment;
        public int StartDay;
        public int CompleteDay = -1;

        // Hidden idea values carried from the idea card.
        public double FunPotential;
        public double HookStrength;
        public bool JamProven;

        public readonly List<Workstream> Workstreams = new List<Workstream>();
        /// <summary>The estimate shown when the project started (includes the planning-fallacy bias).</summary>
        public double InitialEstimatePw;
        public double PolishPw;
        public double BugsCreated;
        public double BugsFixed;
        public bool BoughtAssetPack;

        // Store & release
        public StorePlatform Platform = StorePlatform.None;
        public int StorePageDay = -1;
        public double Wishlists;
        public double Price;
        public int ReleaseDay = -1;

        // Next Fest and the demo (Design/10 §4–5)
        /// <summary>The day the Next Fest it's signed up for starts, or -1.</summary>
        public int FestDay = -1;
        /// <summary>It has had its Next Fest (once per game).</summary>
        public bool FestDone;
        public double DemoProgress;
        public double DemoBugs;
        /// <summary>How good the demo looked when the fest started, and the wishlists that week was going to bring.</summary>
        public double FestQuality;
        public double FestTotal;
        public double FestWishlists;
        public double DemoPlayers;

        // Results
        public ReleaseResult Result;
        public double UnitsSold;
        public double GrossRevenue;
        public double NetRevenue;
        public double NetUnpaid;
        public int PostMortemDay = -1;

        public double TotalRequired => Workstreams.Sum(w => w.Required);
        public double TotalDone => Workstreams.Sum(w => w.Done);
        public double TotalRemaining => Workstreams.Sum(w => w.Remaining);
        public double Progress => TotalRequired <= 0 ? 0 : TotalDone / TotalRequired;
        public bool AllWorkDone => Workstreams.All(w => w.IsDone);
        public double BugsRemaining => BugsCreated - BugsFixed > 0 ? BugsCreated - BugsFixed : 0;
        public bool HasStorePage => StorePageDay >= 0;
        public bool IsReleased => Status == ProjectStatus.Released;
        public Workstream Stream(Skill discipline) => Workstreams.FirstOrDefault(w => w.Discipline == discipline);
        public int FeatureCount => Workstreams.Sum(w => w.FeatureCount);
        public int FeaturesDone => Workstreams.Sum(w => w.FeaturesDone);
    }

    /// <summary>
    /// Everything decided at launch, and how launch week moved it (Design/10 §7). The pillar scores stay hidden
    /// until the post-mortem. Reviews arrive every day in the players' current <see cref="Mood"/>, so hotfixes
    /// and answers in the first days, when most copies sell, move the score the most.
    /// </summary>
    public sealed class ReleaseResult
    {
        public readonly Dictionary<Pillar, double> Pillars = new Dictionary<Pillar, double>();
        /// <summary>Stability and quality now (hotfixes raise them); <see cref="LaunchStability"/> and <see cref="LaunchQuality"/> were at launch.</summary>
        public double Stability;
        public double Quality;
        /// <summary>The share of positive reviews so far: every day's reviews, written in that day's <see cref="Mood"/>.</summary>
        public double PositiveShare;
        public int ReviewCount;
        public double LaunchStability;
        public double LaunchQuality;
        /// <summary>How players feel about the game today: the share of today's reviews that will be positive.</summary>
        public double Mood;
        public double LaunchMood;
        /// <summary>Review points won by answering players during launch (capped).</summary>
        public double Goodwill;
        /// <summary>Word of mouth: sales from here on, relative to the plan made at launch (1 = as planned).</summary>
        public double SalesMultiplier = 1;
        public double ReviewVotes;
        public double PositiveVotes;
        public int Hotfixes;
        public double HotfixedBugs;
        public int Regressions;
        public int Answers;
        public double PushUnits;
        /// <summary>Critic reviews, each published on its own day of launch week (ScoreHub).</summary>
        public readonly List<CriticReview> Critics = new List<CriticReview>();
        /// <summary>What each day of launch week sold (for the daily report).</summary>
        public readonly List<LaunchDay> LaunchDays = new List<LaunchDay>();
        public bool Viral;
        public double LaunchWishlists;
        public double ForecastLow;
        public double ForecastHigh;
        /// <summary>Planned units per month since launch (month 0 = launch month).</summary>
        public readonly List<double> MonthlyUnits = new List<double>();

        public string ReviewLabel
        {
            get
            {
                if (ReviewCount < 10) return "Too few reviews";
                if (PositiveShare >= 0.95 && ReviewCount >= 500) return "Overwhelmingly Positive";
                if (PositiveShare >= 0.80) return ReviewCount >= 50 ? "Very Positive" : "Positive";
                if (PositiveShare >= 0.70) return "Mostly Positive";
                if (PositiveShare >= 0.40) return "Mixed";
                if (PositiveShare >= 0.20) return "Mostly Negative";
                return "Overwhelmingly Negative";
            }
        }

        /// <summary>The critics published by <paramref name="daysSinceLaunch"/>.</summary>
        public IEnumerable<CriticReview> CriticsBy(int daysSinceLaunch) => Critics.Where(c => c.Day <= daysSinceLaunch);

        /// <summary>The ScoreHub average of the critics published so far, or -1 with fewer than three of them.</summary>
        public int ScoreHub(int daysSinceLaunch)
        {
            var published = CriticsBy(daysSinceLaunch).ToList();
            return published.Count < 3 ? -1 : (int)Math.Round(published.Average(c => c.Score));
        }

        public static string ScoreHubLabel(int score) =>
            score < 0 ? "Not enough critics yet" : score >= 90 ? "Universal acclaim" : score >= 75 ? "Generally favorable" : score >= 50 ? "Mixed or average" : "Generally unfavorable";
    }

    /// <summary>A critic's review of a released game (ScoreHub, Design/11 §1).</summary>
    public sealed class CriticReview
    {
        public string Outlet;
        public int Score;
        /// <summary>Days after launch that it's published.</summary>
        public int Day;
        public string Quote;
    }

    /// <summary>One day of launch week in numbers.</summary>
    public sealed class LaunchDay
    {
        public double Units;
        public double Net;
        public double PositiveShare;
        public int Reviews;
    }
}
