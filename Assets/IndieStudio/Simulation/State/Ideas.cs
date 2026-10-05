using System.Collections.Generic;

namespace IndieStudio.Simulation
{
    public sealed class IdeaCard
    {
        public int Id;
        public string Title;
        public GenreId Genre;
        public string Theme;
        public string Hook;
        public string Source;
        public int CreatedDay;
        public readonly List<string> Traits = new List<string>();
        public bool JamProven;
        public bool Used;

        // Hidden truths (Design/04). The player sees ranges whose width depends on Knowledge.
        public double FunPotential;
        public double HookStrength;

        /// <summary>0 = nothing known, 1 = rough read, 2 = researched, 3 = proven (prototype or jam).</summary>
        public int Knowledge;

        /// <summary>Half-width of the displayed range around the true value.</summary>
        public double EstimateSpread
        {
            get
            {
                switch (Knowledge)
                {
                    case 0: return 1;
                    case 1: return 0.25;
                    case 2: return 0.12;
                    default: return 0.05;
                }
            }
        }
    }

    public sealed class Audience
    {
        public double Followers;
    }
}
