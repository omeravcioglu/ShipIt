using System;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>Idea cards, research and game jams (Design/04, Design/03 §5).</summary>
    public sealed partial class GameSimulation
    {
        const double ResearchPointsPerIdea = 12;

        /// <summary>The range the player sees for an idea's fun potential (hidden truth ± knowledge-based spread).</summary>
        public static void FunEstimate(IdeaCard idea, out double low, out double high)
        {
            if (idea.Knowledge <= 0)
            {
                low = 0;
                high = 1;
                return;
            }
            // Deterministic per idea so the range doesn't jitter between frames.
            double bias = ((idea.Id * 7919) % 100 / 100.0 - 0.5) * idea.EstimateSpread;
            low = Rules.Clamp(idea.FunPotential - idea.EstimateSpread + bias, 0, 1);
            high = Rules.Clamp(idea.FunPotential + idea.EstimateSpread + bias, 0, 1);
        }

        IdeaCard GenerateIdea(string source, int knowledge)
        {
            var rng = State.RngIdeas;
            var genre = rng.PickWeighted(GameContent.Genres, g => g.Market);
            string title;
            do title = $"{rng.Pick(GameContent.TitleFirst)} {rng.Pick(GameContent.TitleSecond)}";
            while (State.Ideas.Any(i => i.Title == title) && State.Ideas.Count < 300);

            var idea = new IdeaCard
            {
                Id = State.NextId++,
                Title = title,
                Genre = genre.Id,
                Theme = rng.Pick(GameContent.Themes),
                Hook = rng.Pick(GameContent.Hooks),
                Source = source,
                CreatedDay = State.Day,
                FunPotential = Rules.Clamp(0.55 + rng.Gaussian() * 0.15, 0.15, 0.95),
                HookStrength = Rules.Clamp(0.5 + rng.Gaussian() * 0.18, 0.1, 0.95),
                Knowledge = knowledge,
            };
            if (genre.StreamerAppeal >= 4) idea.Traits.Add("Streamer friendly");
            if (genre.Market <= 2) idea.Traits.Add("Niche");
            if (genre.Market >= 4) idea.Traits.Add("Big audience");
            if (rng.Chance(0.25)) idea.Traits.Add("Trending");
            if (knowledge >= 2 && idea.HookStrength >= 0.65) idea.Traits.Add("Strong hook");
            return idea;
        }

        void AddIdea(IdeaCard idea) => State.Ideas.Add(idea);

        void DoResearch(int day)
        {
            var f = State.Founder;
            State.ResearchPoints += Tuning.BlockHours * (0.6 + 0.1 * f.Get(Skill.Design));
            if (_report != null) _report.Title = "Played games and read reviews";
            GainXp(Skill.Design, Tuning.BlockHours * 0.5);
            if (State.ResearchPoints < ResearchPointsPerIdea)
            {
                Note($"Research notes: {State.ResearchPoints:0}/{ResearchPointsPerIdea:0} toward your next insight.");
                return;
            }
            State.ResearchPoints -= ResearchPointsPerIdea;

            var unknown = State.UnusedIdeas.Where(i => i.Knowledge < 2).OrderBy(i => i.Knowledge).FirstOrDefault();
            if (unknown != null && State.RngIdeas.Chance(0.5))
            {
                unknown.Knowledge++;
                if (unknown.Knowledge >= 2 && unknown.HookStrength >= 0.65 && !unknown.Traits.Contains("Strong hook")) unknown.Traits.Add("Strong hook");
                FunEstimate(unknown, out double low, out double high);
                Note($"A clearer read on {unknown.Title}: fun potential {Rules.Pct(low)}–{Rules.Pct(high)}.");
                Post("research", MessageKind.Info, $"Research: a closer look at {unknown.Title}",
                    $"Playing similar games and reading their reviews narrowed your read on it: fun potential {Rules.Pct(low)}–{Rules.Pct(high)}.");
                return;
            }

            var idea = GenerateIdea("Research", 1);
            AddIdea(idea);
            Note($"New idea: {idea.Title} ({GameContent.Genre(idea.Genre).Name}).");
            Post("research", MessageKind.Info, $"New idea: {idea.Title}",
                $"{GameContent.Genre(idea.Genre).Name} · {idea.Theme}: \"{idea.Hook}.\" It's in your Idea Notebook.");
        }

        // ------------------------------------------------------------------ jams

        void DoJam(int day, int block, double performance)
        {
            var f = State.Founder;
            double productivity = Rules.Productivity(State).Value;
            Skill skill = SkillInfo.Craft[(block + day) % 3];
            State.JamProgress += Rules.Efficiency(f.Get(skill)) * productivity * performance;
            if (_report != null)
            {
                _report.Title = "Game jam!";
                _report.HasSkill = true;
                _report.Skill = skill;
            }
            GainXp(skill, Tuning.BlockHours * productivity * Tuning.JamXpMultiplier);
            State.CurrentWeek.JamBlocks++;

            bool lastBlockOfJam = GameCalendar.DayOfWeek(day) == 6 && block == Evening;
            if (lastBlockOfJam) FinishJam(day);
            else Note("The jam game is coming together. It's judged on Sunday night.");
        }

        void FinishJam(int day)
        {
            var rng = State.RngEvents;
            var f = State.Founder;
            double craft = SkillInfo.Craft.Take(3).Average(s => f.Get(s));
            double score = State.JamProgress * (0.6 + 0.08 * craft) + rng.Gaussian() * 0.8;
            int entrants = 2400 + rng.Range(0, 900);
            int rank = (int)Rules.Clamp(entrants * Math.Exp(-score / 2.2), 1, entrants);
            State.JamProgress = 0;

            var idea = GenerateIdea("Game jam", 3);
            idea.JamProven = true;
            idea.FunPotential = Rules.Clamp(idea.FunPotential + 0.05, 0, 0.95);
            idea.Traits.Add("Jam-proven");
            AddIdea(idea);

            double followers = rng.LogNormal(60, 0.8);
            string extra = "";
            if (rng.Chance(0.02 + 0.01 * Math.Max(0, 7 - Math.Log(rank + 1))))
            {
                followers += rng.LogNormal(1500, 0.6);
                idea.HookStrength = Rules.Clamp(idea.HookStrength + 0.2, 0, 0.95);
                idea.Traits.Add("Viral hook");
                extra = " A streamer found it and it blew up — people are asking for a full version!";
            }
            State.Audience.Followers += followers;
            f.Morale = Rules.Clamp(f.Morale + 6, 0, 100);
            Post("jam_result", MessageKind.Info, $"Jam result: #{rank:#,0} of {entrants:#,0}",
                $"Your jam game \"{idea.Title}\" is done. +{followers:0} followers, morale +6. It's now a Jam-proven idea in your notebook (10% less work if you develop it).{extra}");
        }
    }
}
