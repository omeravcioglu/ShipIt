using System;
using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// Names for the features a project is built from, so development reads as "wiring up the Hero sprite"
    /// rather than "0.07 person-weeks of Art". Picked deterministically from the project's title, so they
    /// don't consume any random stream (the simulation stays reproducible).
    /// </summary>
    public static class FeatureCatalog
    {
        static readonly Dictionary<Skill, string[]> Generic = new Dictionary<Skill, string[]>
        {
            { Skill.Programming, new[] { "Player controls", "Core loop", "Collision", "Camera", "Enemy AI", "Save system", "Menus", "Level loading",
                "Scoring", "Physics", "Particles", "Pause menu", "Settings", "Controller support", "Inventory", "Achievements", "Pathfinding",
                "Animation system", "Input remapping", "Cutscenes", "Localization", "Performance pass", "Crash fixes", "Vapor integration",
                "Leaderboards", "Day/night cycle", "Accessibility options", "Quest system", "Audio engine", "Boss behaviors" } },
            { Skill.Design, new[] { "Core mechanic", "Tutorial", "Forest level", "Cave level", "Castle level", "Difficulty curve", "Boss fight",
                "Progression", "Secrets", "Rewards", "Checkpoints", "Final level", "Sky level", "Night level", "Desert level", "Ice level", "Hub world",
                "Side quests", "Upgrade tree", "Enemy roster", "Puzzle rooms", "Collectibles", "Combat feel", "Pacing pass", "Endgame", "Hard mode",
                "Challenge rooms", "Speedrun routes", "Onboarding", "Economy" } },
            { Skill.Art, new[] { "Hero sprite", "Tileset", "Backgrounds", "Enemy sprites", "UI art", "Title logo", "Animations", "Item icons", "Effects",
                "Props", "Portraits", "Key art", "Boss sprite", "NPCs", "Map screen", "Lighting pass", "Parallax layers", "Water tiles", "Foliage",
                "Interiors", "Idle animations", "Death animations", "Menu backgrounds", "Fonts and HUD", "Store capsule", "Trailer shots",
                "Weather effects", "Cutscene art", "Loading screen", "Particle art" } },
            { Skill.Audio, new[] { "Sound effects", "Main theme", "Ambience", "Menu music", "Boss music", "Jingles", "UI sounds", "Footsteps",
                "Level themes", "Victory fanfare", "Creature sounds", "Weather sounds", "Mix pass", "Voice grunts", "Credits song", "Ending theme" } },
            { Skill.Writing, new[] { "Intro text", "Dialogue", "Item descriptions", "Ending", "Lore notes", "Tutorial text", "Character bios",
                "Store page copy", "NPC chatter", "Quest text", "Signs and notes", "Achievement names", "Boss taunts", "Loading tips", "Journal entries",
                "Credits", "Press kit", "Trailer script", "Epilogue", "Patch notes" } },
        };

        /// <summary>Genre flavor: these come first, before the generic names.</summary>
        static readonly Dictionary<GenreId, Dictionary<Skill, string[]>> ByGenre = new Dictionary<GenreId, Dictionary<Skill, string[]>>
        {
            { GenreId.Platformer, new Dictionary<Skill, string[]> { { Skill.Programming, new[] { "Jump physics" } }, { Skill.Design, new[] { "World 1" } } } },
            { GenreId.Puzzle, new Dictionary<Skill, string[]> { { Skill.Design, new[] { "Puzzle set 1", "Puzzle set 2", "Puzzle set 3" } }, { Skill.Programming, new[] { "Undo system" } } } },
            { GenreId.Horror, new Dictionary<Skill, string[]> { { Skill.Design, new[] { "Jump scares" } }, { Skill.Audio, new[] { "Creepy ambience" } }, { Skill.Art, new[] { "The monster" } } } },
            { GenreId.Roguelike, new Dictionary<Skill, string[]> { { Skill.Programming, new[] { "Procedural levels" } }, { Skill.Design, new[] { "Item synergies" } } } },
            { GenreId.Simulation, new Dictionary<Skill, string[]> { { Skill.Programming, new[] { "Simulation tick" } }, { Skill.Design, new[] { "Balancing" } } } },
            { GenreId.Narrative, new Dictionary<Skill, string[]> { { Skill.Writing, new[] { "Chapter 1", "Chapter 2", "Chapter 3" } }, { Skill.Programming, new[] { "Dialogue system" } } } },
            { GenreId.Cozy, new Dictionary<Skill, string[]> { { Skill.Design, new[] { "Daily routine" } }, { Skill.Art, new[] { "Cozy interiors" } } } },
            { GenreId.Party, new Dictionary<Skill, string[]> { { Skill.Programming, new[] { "Local multiplayer" } }, { Skill.Design, new[] { "Party rules" } } } },
        };

        /// <summary>How many named features a discipline's work should be split into.</summary>
        public static int CountFor(ProjectScope scope, double requiredPw)
        {
            double size = scope == ProjectScope.Tiny ? Tuning.FeatureSizeTiny : Tuning.FeatureSizeSmall;
            return Math.Max(1, (int)Math.Round(requiredPw / size, MidpointRounding.AwayFromZero));
        }

        public static List<string> Pick(string projectTitle, GenreId genre, Skill discipline, int count)
        {
            var names = new List<string>();
            if (ByGenre.TryGetValue(genre, out var flavor) && flavor.TryGetValue(discipline, out var first))
                foreach (var name in first)
                    if (names.Count < count) names.Add(name);

            // The first generic name is the core of the discipline ("Player controls", "Hero sprite"): it comes
            // before the shuffled rest, so games start with their foundations.
            var generic = Generic[discipline];
            if (names.Count < count && !names.Contains(generic[0])) names.Add(generic[0]);
            var pool = new List<string>(generic.Skip(1));
            var rng = new SimRandom(StableHash.Fnv1a64(projectTitle) ^ ((ulong)discipline + 1) * 0x9E3779B97F4A7C15UL, 77);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                var swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }
            foreach (var name in pool)
                if (names.Count < count && !names.Contains(name)) names.Add(name);
            for (int extra = 2; names.Count < count; extra++) names.Add($"{discipline} pass {extra}");
            return names;
        }
    }
}
