using System.Collections.Generic;

namespace IndieStudio.Simulation
{
    public enum GenreId { Platformer, Puzzle, Horror, Roguelike, Simulation, Narrative, Cozy, Party }

    public enum ArtStyleId { PixelArt, Minimalist, Cartoon, HandPainted, LowPoly, RetroPS1 }

    public enum Pillar { Gameplay, Graphics, Audio, Story, Content, Innovation }

    public sealed class GenreDef
    {
        public GenreId Id;
        public string Name;
        /// <summary>Baseline audience, 1–5.</summary>
        public double Market;
        public double StreamerAppeal;
        /// <summary>Review weights per pillar (sum to 1), from Design/05 §2.1.</summary>
        public Dictionary<Pillar, double> Weights;
        /// <summary>Share of project work per craft discipline (sum to 1).</summary>
        public Dictionary<Skill, double> WorkShare;
    }

    public sealed class ArtStyleDef
    {
        public ArtStyleId Id;
        public string Name;
        public double ArtCost;
        /// <summary>Art skill needed for the style to look good; below it Graphics is capped ("cheap-looking").</summary>
        public double SkillFloor;
        public double Ceiling;
        public double ScreenshotPop;
        public bool Is3D;
        public string Note;
    }

    /// <summary>
    /// Built-in content for the M1 slice (8 genres, 6 art styles). Kept in code so the simulation
    /// runs without Unity; it can move to data assets later without changing the rules.
    /// </summary>
    public static class GameContent
    {
        public static readonly IReadOnlyList<GenreDef> Genres = new[]
        {
            Genre(GenreId.Platformer, "Platformer", 3, 2, W(40, 20, 10, 0, 15, 15), S(35, 25, 28, 8, 4)),
            Genre(GenreId.Puzzle, "Puzzle", 2, 1, W(35, 10, 10, 5, 10, 30), S(30, 40, 18, 7, 5)),
            Genre(GenreId.Horror, "Horror", 3, 5, W(25, 25, 25, 15, 5, 5), S(25, 20, 30, 15, 10)),
            Genre(GenreId.Roguelike, "Roguelike", 4, 3, W(45, 10, 5, 0, 25, 15), S(40, 30, 20, 6, 4)),
            Genre(GenreId.Simulation, "Simulation", 4, 3, W(40, 10, 5, 0, 25, 20), S(40, 30, 18, 6, 6)),
            Genre(GenreId.Narrative, "Narrative Adventure", 2, 2, W(5, 20, 15, 45, 5, 10), S(15, 15, 30, 10, 30)),
            Genre(GenreId.Cozy, "Cozy Life Sim", 4, 3, W(25, 25, 10, 5, 30, 5), S(28, 25, 32, 8, 7)),
            Genre(GenreId.Party, "Party (Local Multiplayer)", 3, 5, W(40, 10, 5, 0, 15, 30), S(40, 30, 20, 7, 3)),
        };

        public static readonly IReadOnlyList<ArtStyleDef> ArtStyles = new[]
        {
            new ArtStyleDef { Id = ArtStyleId.PixelArt, Name = "Pixel Art", ArtCost = 0.8, SkillFloor = 3, Ceiling = 8, ScreenshotPop = 3, Note = "Cheap to start, slow to animate. Ages well." },
            new ArtStyleDef { Id = ArtStyleId.Minimalist, Name = "Minimalist", ArtCost = 0.4, SkillFloor = 2, Ceiling = 6, ScreenshotPop = 2, Note = "Very cheap. Needs a strong hook to stand out." },
            new ArtStyleDef { Id = ArtStyleId.Cartoon, Name = "Cartoon", ArtCost = 1.0, SkillFloor = 3, Ceiling = 8, ScreenshotPop = 4, Note = "Friendly and readable. Great for party and cozy games." },
            new ArtStyleDef { Id = ArtStyleId.HandPainted, Name = "Hand-Painted", ArtCost = 1.4, SkillFloor = 5, Ceiling = 10, ScreenshotPop = 5, Note = "Gorgeous screenshots — if your art skill can carry it." },
            new ArtStyleDef { Id = ArtStyleId.LowPoly, Name = "Low Poly 3D", ArtCost = 0.7, SkillFloor = 2, Ceiling = 7, ScreenshotPop = 2, Is3D = true, Note = "Cheap 3D. Slow builds on a laptop. Asset-flip risk." },
            new ArtStyleDef { Id = ArtStyleId.RetroPS1, Name = "Retro PS1", ArtCost = 0.7, SkillFloor = 3, Ceiling = 7, ScreenshotPop = 4, Is3D = true, Note = "Nostalgic 3D. Perfect for horror. Slow builds on a laptop." },
        };

        public static GenreDef Genre(GenreId id) => Genres[(int)id];
        public static ArtStyleDef ArtStyle(ArtStyleId id) => ArtStyles[(int)id];

        /// <summary>Hidden genre × art-style fit (−1 … +1). Discovered through post-mortems.</summary>
        public static double StyleFit(GenreId genre, ArtStyleId style)
        {
            switch (style)
            {
                case ArtStyleId.RetroPS1: return genre == GenreId.Horror ? 1.0 : genre == GenreId.Cozy ? -1.0 : 0;
                case ArtStyleId.PixelArt: return genre == GenreId.Platformer || genre == GenreId.Roguelike ? 0.5 : 0;
                case ArtStyleId.Cartoon: return genre == GenreId.Party || genre == GenreId.Cozy ? 0.5 : genre == GenreId.Horror ? -0.5 : 0;
                case ArtStyleId.HandPainted: return genre == GenreId.Narrative || genre == GenreId.Cozy ? 0.5 : 0;
                case ArtStyleId.Minimalist: return genre == GenreId.Puzzle ? 0.5 : genre == GenreId.Cozy ? -0.5 : 0;
                case ArtStyleId.LowPoly: return genre == GenreId.Simulation ? 0.3 : 0;
                default: return 0;
            }
        }

        public static readonly string[] Themes =
        {
            "Fantasy", "Sci-Fi", "Space", "Cyberpunk", "Post-Apocalyptic", "Cosmic Horror", "Folk Horror", "Pirates",
            "Western", "Modern City", "Office Life", "Farming", "Ocean", "Animals", "Mythology", "Dreams", "Cooking", "Detective",
        };

        public static readonly string[] Hooks =
        {
            "The world changes when you look away",
            "Every death rewinds the whole town",
            "You are the dungeon, not the hero",
            "Gravity flips every ten seconds",
            "Your tools are also your enemies",
            "Time only moves when you move",
            "The map is drawn by other players' mistakes",
            "You can only save by sleeping",
            "Every level is a single screen that folds",
            "The narrator lies and you have to prove it",
            "Customers remember how you treated them",
            "Your shadow plays the level one turn behind you",
            "Weather is the main weapon",
            "Nothing is explained; the game is a puzzle box",
            "Cooking spells from ingredients you grow",
            "A friendship meter replaces health",
        };

        public static readonly string[] TitleFirst =
        {
            "Hollow", "Paper", "Tiny", "Lantern", "Neon", "Salt", "Moth", "Ember", "Quiet", "Velvet", "Iron", "Drift",
            "Pocket", "Glass", "Last", "Wild", "Little", "Crooked", "Sunken", "Honey",
        };

        public static readonly string[] TitleSecond =
        {
            "Tide", "Moths", "Kingdom", "Fields", "Signal", "Harbor", "Garden", "Engine", "Echoes", "Spire", "Orchard",
            "Circuit", "Burrow", "Parade", "Lighthouse", "Market", "Hollow", "Dreamers", "Station", "Pantry",
        };

        public static readonly string[] ClientFirstNames =
        {
            "Pixel", "Bright", "North", "Blue", "Copper", "Rapid", "Kind", "Brick", "Moon", "Lucky", "Orbit", "Plum",
        };

        public static readonly string[] ClientSecondNames =
        {
            "Forge", "Labs", "Works", "Digital", "Studio", "Media", "Collective", "Interactive", "Apps", "Games",
        };

        static GenreDef Genre(GenreId id, string name, double market, double streamer, Dictionary<Pillar, double> weights, Dictionary<Skill, double> share) =>
            new GenreDef { Id = id, Name = name, Market = market, StreamerAppeal = streamer, Weights = weights, WorkShare = share };

        static Dictionary<Pillar, double> W(double gameplay, double graphics, double audio, double story, double content, double innovation) =>
            new Dictionary<Pillar, double>
            {
                { Pillar.Gameplay, gameplay / 100 }, { Pillar.Graphics, graphics / 100 }, { Pillar.Audio, audio / 100 },
                { Pillar.Story, story / 100 }, { Pillar.Content, content / 100 }, { Pillar.Innovation, innovation / 100 },
            };

        static Dictionary<Skill, double> S(double programming, double design, double art, double audio, double writing) =>
            new Dictionary<Skill, double>
            {
                { Skill.Programming, programming / 100 }, { Skill.Design, design / 100 }, { Skill.Art, art / 100 },
                { Skill.Audio, audio / 100 }, { Skill.Writing, writing / 100 },
            };
    }
}
