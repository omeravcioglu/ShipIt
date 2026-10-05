using System;
using System.Collections.Generic;

namespace IndieStudio.Simulation
{
    public enum DayJob { None, PartTime, FullTime }

    public enum Housing { Apartment, SharedFlat, Parents }

    public sealed class Founder
    {
        public readonly Dictionary<Skill, double> Skills = new Dictionary<Skill, double>();
        public double Burnout = 10;
        public double Morale = 70;

        /// <summary>Collapse: all blocks are rest until this day (inclusive).</summary>
        public int ForcedRestUntilDay = -1;
        /// <summary>A chosen vacation: all blocks are rest until this day (inclusive).</summary>
        public int VacationUntilDay = -1;

        public DayJob Job = DayJob.None;
        public int JobStartDay = -1;
        public Housing Housing = Housing.Apartment;

        /// <summary>A purchased course boosts XP in one skill for a number of hours.</summary>
        public Skill CourseSkill;
        public double CourseHoursLeft;

        public Founder()
        {
            foreach (var skill in SkillInfo.All) Skills[skill] = 1;
        }

        public double Get(Skill skill) => Skills[skill];
        public int Level(Skill skill) => (int)Math.Floor(Skills[skill] + 1e-9);

        public bool IsResting(int day) => day <= ForcedRestUntilDay || day <= VacationUntilDay;
    }

    /// <summary>Lifetime counters for the UI and for balance diagnostics.</summary>
    public sealed class LifetimeStats
    {
        public int WorkBlocks;
        public int IdleFreelanceBlocks;
        public int IdleDevelopBlocks;
        public int VacationDays;
        public int Collapses;
        public int AnxiousWeeks;
        /// <summary>Days the cat was petted (achievements).</summary>
        public int CatPets;
        /// <summary>Work sessions played well enough for three stars (achievements).</summary>
        public int BrilliantSessions;
    }

    /// <summary>What happened during the current week; used for the weekly burnout and morale update.</summary>
    public sealed class WeekLog
    {
        public int WorkBlocks;
        public int RestBlocks;
        public int WeekendWorkBlocks;
        public int JamBlocks;
        public int WeekendRestBlocks;
        public double ProjectPw;
        public double FreelanceHours;
        public double FreelanceEarned;
        public int IdleFreelanceBlocks;
        public int IdleDevelopBlocks;
        public double FollowersGained;
        public double WishlistsGained;
        /// <summary>Set when the week closes on Sunday night: how burnout and morale moved.</summary>
        public double BurnoutChange;
        public double MoraleChange;

        public int HoursWorked => WorkBlocks * Tuning.BlockHours;
    }
}
