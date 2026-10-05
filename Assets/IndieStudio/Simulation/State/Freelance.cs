using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    public enum ClientType { Friend, SmallStudio, Agency, Startup, Corporate }

    public enum GigStatus { Offered, Active, Completed, Failed, Expired, Declined }

    public enum RateSetting { Budget, Market, Premium }

    public sealed class Gig
    {
        public int Id;
        public string ClientName;
        public ClientType Client;
        public string Title;
        public Skill Discipline;
        public double RequiredSkill;
        public double Hours;
        public double HoursDone;
        public double Pay;
        public int OfferedDay;
        public int ExpiresDay;
        public int DurationDays;
        public int DeadlineDay = -1;
        public GigStatus Status = GigStatus.Offered;
        public string RiskFlags = "";
        public int ClientScore;
        /// <summary>Goodwill won or lost along the way (for example by how you handled scope creep).</summary>
        public int ScoreModifier;
        public bool LateNoticeSent;
        public bool ScopeCreepChecked;
        /// <summary>Work sessions so far and the sum of their minigame multipliers: clients notice careful work.</summary>
        public int Sessions;
        public double PerformanceSum;

        public double AveragePerformance => Sessions > 0 ? PerformanceSum / Sessions : 1.0;

        // Hidden until you've worked with the client (Design/17).
        public double NonPayRisk;
        public double ScopeCreepChance;
        public int PaymentDelayDays;

        public double Progress => Hours <= 0 ? 0 : HoursDone / Hours;
        public double EffectiveHourlyRate => Hours <= 0 ? 0 : Pay / Hours;
    }

    public sealed class FreelanceProfile
    {
        public int CompletedGigs;
        public int FailedGigs;
        public double TotalHours;
        public double TotalEarned;
        public RateSetting RateSetting = RateSetting.Market;
        public readonly List<int> RecentScores = new List<int>();

        /// <summary>Average of the last ten client scores; 0 means "New" (no rating yet).</summary>
        public double Rating => RecentScores.Count == 0 ? 0 : RecentScores.Average();

        public void AddScore(int score)
        {
            RecentScores.Add(score);
            if (RecentScores.Count > 10) RecentScores.RemoveAt(0);
        }
    }

    public sealed class FreelanceState
    {
        public readonly FreelanceProfile Profile = new FreelanceProfile();
        public readonly List<Gig> Gigs = new List<Gig>();

        public IEnumerable<Gig> Offers => Gigs.Where(g => g.Status == GigStatus.Offered);
        public IEnumerable<Gig> Active => Gigs.Where(g => g.Status == GigStatus.Active);
        public int ActiveCount => Gigs.Count(g => g.Status == GigStatus.Active);
    }
}
