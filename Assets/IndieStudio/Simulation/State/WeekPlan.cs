using System;
using System.Collections.Generic;

namespace IndieStudio.Simulation
{
    public enum ActivityKind { Rest, Develop, Freelance, Marketing, Learn, Research, DayJob, Jam, Hotfix, Community, Demo }

    /// <summary>
    /// What one 4-hour slot of a day is spent on (Design/02 §4): a morning, an afternoon or an evening.
    /// The player picks one per slot; bots and tests use a repeating <see cref="WeekPlan"/>.
    /// </summary>
    public readonly struct PlanBlock : IEquatable<PlanBlock>
    {
        public readonly ActivityKind Kind;
        public readonly bool HasFocus;
        public readonly Skill Focus;
        /// <summary>Which gig to work on (its id) or which <see cref="MarketingPost"/> to make; 0 = automatic.</summary>
        public readonly int Option;

        public PlanBlock(ActivityKind kind)
        {
            Kind = kind;
            HasFocus = false;
            Focus = Skill.Programming;
            Option = 0;
        }

        public PlanBlock(ActivityKind kind, Skill focus)
        {
            Kind = kind;
            HasFocus = true;
            Focus = focus;
            Option = 0;
        }

        public PlanBlock(ActivityKind kind, int option)
        {
            Kind = kind;
            HasFocus = false;
            Focus = Skill.Programming;
            Option = option;
        }

        /// <summary>Every field at once (save games).</summary>
        internal PlanBlock(ActivityKind kind, bool hasFocus, Skill focus, int option)
        {
            Kind = kind;
            HasFocus = hasFocus;
            Focus = focus;
            Option = option;
        }

        public static readonly PlanBlock Rest = new PlanBlock(ActivityKind.Rest);
        public static readonly PlanBlock Develop = new PlanBlock(ActivityKind.Develop);
        public static readonly PlanBlock Freelance = new PlanBlock(ActivityKind.Freelance);
        public static readonly PlanBlock Marketing = new PlanBlock(ActivityKind.Marketing);
        public static readonly PlanBlock Research = new PlanBlock(ActivityKind.Research);
        /// <summary>Look for new clients instead of working on an accepted gig.</summary>
        public static readonly PlanBlock Prospect = new PlanBlock(ActivityKind.Freelance, -1);

        /// <summary>Build the next feature of one discipline.</summary>
        public static PlanBlock Build(Skill discipline) => new PlanBlock(ActivityKind.Develop, discipline);
        /// <summary>Work on one accepted gig.</summary>
        public static PlanBlock Gig(int gigId) => new PlanBlock(ActivityKind.Freelance, gigId);
        public static PlanBlock Post(MarketingPost post) => new PlanBlock(ActivityKind.Marketing, (int)post);
        public static PlanBlock Study(Skill skill) => new PlanBlock(ActivityKind.Learn, skill);
        /// <summary>Patch a released game (launch week, Design/10 §7).</summary>
        public static PlanBlock Hotfix(int projectId) => new PlanBlock(ActivityKind.Hotfix, projectId);
        /// <summary>Answer players about a released game: forums, reviews, the Discord.</summary>
        public static PlanBlock Answer(int projectId) => new PlanBlock(ActivityKind.Community, projectId);
        /// <summary>Work on the demo for Next Fest.</summary>
        public static PlanBlock Demo(int projectId) => new PlanBlock(ActivityKind.Demo, projectId);

        public bool IsWork => Kind != ActivityKind.Rest;

        public string Label
        {
            get
            {
                switch (Kind)
                {
                    case ActivityKind.Develop: return HasFocus ? $"Dev · {Focus}" : "Dev · Auto";
                    case ActivityKind.Learn: return HasFocus ? $"Learn · {Focus}" : "Learn";
                    case ActivityKind.DayJob: return "Day job";
                    case ActivityKind.Community: return "Answer players";
                    default: return Kind.ToString();
                }
            }
        }

        public bool Equals(PlanBlock other) =>
            Kind == other.Kind && HasFocus == other.HasFocus && (!HasFocus || Focus == other.Focus) && Option == other.Option;
        public override bool Equals(object obj) => obj is PlanBlock other && Equals(other);
        public override int GetHashCode() => (((int)Kind * 31 + (HasFocus ? 1 : 0)) * 31 + (int)Focus) * 31 + Option;
    }

    public sealed class WeekPlan
    {
        public const int DaysPerWeek = 7;
        public const int BlocksPerDay = 3;
        public const int TotalBlocks = DaysPerWeek * BlocksPerDay;

        readonly PlanBlock[] _blocks = new PlanBlock[TotalBlocks];

        public WeekPlan()
        {
            for (int i = 0; i < TotalBlocks; i++) _blocks[i] = PlanBlock.Rest;
        }

        public PlanBlock this[int dayOfWeek, int block]
        {
            get => _blocks[dayOfWeek * BlocksPerDay + block];
            set => _blocks[dayOfWeek * BlocksPerDay + block] = value;
        }

        public int CountWork()
        {
            int count = 0;
            foreach (var b in _blocks) if (b.IsWork) count++;
            return count;
        }

        public int Count(ActivityKind kind)
        {
            int count = 0;
            foreach (var b in _blocks) if (b.Kind == kind) count++;
            return count;
        }

        public WeekPlan Clone()
        {
            var copy = new WeekPlan();
            Array.Copy(_blocks, copy._blocks, TotalBlocks);
            return copy;
        }

        public void CopyFrom(WeekPlan other) => Array.Copy(other._blocks, _blocks, TotalBlocks);

        public static WeekPlan Build(Func<int, int, PlanBlock> blockAt)
        {
            var plan = new WeekPlan();
            for (int d = 0; d < DaysPerWeek; d++)
                for (int b = 0; b < BlocksPerDay; b++)
                    plan[d, b] = blockAt(d, b);
            return plan;
        }
    }

    public sealed class PlanTemplate
    {
        public string Name;
        public string Description;
        public Func<WeekPlan> Create;
    }

    /// <summary>
    /// Starting templates, written as seven "morning afternoon evening" triples from Monday to Sunday.
    /// D = develop, G = gig (freelance), M = marketing, R = research, L = learn, - = rest.
    /// The mixes mirror the strategies in Design/Tools/economy_sim.py.
    /// </summary>
    public static class PlanTemplates
    {
        public static readonly IReadOnlyList<PlanTemplate> All = new[]
        {
            Template("Balanced", "20h gigs · 20h game · 8h marketing (48h)", "DDD|GG-|GM-|GG-|DMD|---|---"),
            Template("All-in", "36h game · 8h marketing (44h). Needs another source of money.", "DDD|DD-|DM-|DD-|DM-|---|---"),
            Template("Survival", "28h gigs · 12h game · 4h marketing (44h)", "GGD|GG-|GD-|GG-|DM-|---|---"),
            Template("Standard 40h", "Weekday mornings and afternoons on the game", "DD-|DD-|DD-|DD-|DD-|---|---"),
        };

        public static WeekPlan Standard() => Parse("DD-|DD-|DD-|DD-|DD-|---|---");
        public static WeekPlan Balanced() => All[0].Create();
        public static WeekPlan AllIn() => All[1].Create();
        public static WeekPlan Survival() => All[2].Create();

        public static WeekPlan Parse(string pattern)
        {
            string[] days = pattern.Split('|');
            if (days.Length != WeekPlan.DaysPerWeek) throw new ArgumentException("A plan pattern needs 7 days", nameof(pattern));
            return WeekPlan.Build((d, b) => FromCode(days[d][b]));
        }

        static PlanTemplate Template(string name, string description, string pattern) =>
            new PlanTemplate { Name = name, Description = description, Create = () => Parse(pattern) };

        static PlanBlock FromCode(char code)
        {
            switch (code)
            {
                case 'D': return PlanBlock.Develop;
                case 'G': return PlanBlock.Freelance;
                case 'M': return PlanBlock.Marketing;
                case 'R': return PlanBlock.Research;
                case 'L': return new PlanBlock(ActivityKind.Learn);
                case '-': return PlanBlock.Rest;
                default: throw new ArgumentException($"Unknown plan code '{code}'");
            }
        }
    }

    /// <summary>A temporary replacement of plan blocks (for example a game-jam weekend).</summary>
    public sealed class PlanOverride
    {
        public int FromDay;
        public int ToDay;
        public ActivityKind Kind;
        /// <summary>Which blocks of each day are overridden (bit 0 = morning … bit 2 = evening).</summary>
        public int BlockMask = 0b111;
    }
}
