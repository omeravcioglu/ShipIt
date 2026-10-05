using System;

namespace IndieStudio.Simulation
{
    /// <summary>Founder and employee skills. The first five are craft disciplines that do project work.</summary>
    public enum Skill
    {
        Programming,
        Design,
        Art,
        Audio,
        Writing,
        Business,
        Marketing,
        Leadership,
        Negotiation,
        Production,
    }

    public static class SkillInfo
    {
        public static readonly Skill[] All = (Skill[])Enum.GetValues(typeof(Skill));
        public static readonly Skill[] Craft = { Skill.Programming, Skill.Design, Skill.Art, Skill.Audio, Skill.Writing };

        public static bool IsCraft(this Skill skill) => skill <= Skill.Writing;

        /// <summary>Hours of practice needed to go from <paramref name="level"/> to level + 1 (70 × L²).</summary>
        public static double HoursForLevel(int level) => Tuning.XpHoursCoefficient * Math.Max(1, level) * Math.Max(1, level);

        /// <summary>Advances a 1–10 skill by hours of practice, crossing level boundaries correctly.</summary>
        public static double AddPractice(double skill, double hours)
        {
            while (hours > 0 && skill < 10)
            {
                int level = Math.Max(1, (int)Math.Floor(skill));
                double costLeft = HoursForLevel(level) * (level + 1 - skill);
                if (hours >= costLeft)
                {
                    hours -= costLeft;
                    skill = level + 1;
                }
                else
                {
                    skill += hours / HoursForLevel(level);
                    hours = 0;
                }
            }
            return Math.Min(skill, 10.0);
        }
    }
}
