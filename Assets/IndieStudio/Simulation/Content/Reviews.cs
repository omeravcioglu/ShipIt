using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IndieStudio.Simulation
{
    public sealed class PlayerReview
    {
        public bool Positive;
        public string Author;
        public double Hours;
        public string Text;
    }

    /// <summary>
    /// What players say about a released game (Design/11 §2). Each review reacts to a pillar the game was really
    /// judged on, and names a feature you actually built ("The Jump physics feel perfect"), so launch day
    /// reflects your work. The share of thumbs-up matches the game's positive share. Deterministic per game.
    /// </summary>
    public static class ReviewWriter
    {
        enum Topic { Gameplay, Graphics, Audio, Story, Content, Innovation, Stability }

        static readonly string[] Authors =
        {
            "pixelpenguin", "LateNightGamer", "moss_and_mud", "Grandma Joy", "speedrun_sara", "the_real_kevin", "cozyfox",
            "dad_plays", "Rune", "nightshift_nurse", "achievement_hunter", "Mx Pebble", "retro_rita", "couchcoop", "lena.plays",
            "Tom (age 9)", "bugcatcher", "quietgamer", "Professor Waffles", "indie_ida",
        };

        static readonly Dictionary<Topic, string[]> Praise = new Dictionary<Topic, string[]>
        {
            { Topic.Gameplay, new[] { "Can we talk about the {design}? Genius. I lost a whole evening.", "Love the {programming}. Tight and responsive.",
                "Just one more go… it's 3am. Send help.", "The {design} alone? Worth the price." } },
            { Topic.Graphics, new[] { "Gorgeous {art}. Every screenshot could be a wallpaper.", "So much charm in the {art}.",
                "I keep stopping just to look at the {art}." } },
            { Topic.Audio, new[] { "That {audio}! Stuck in my head for days. Soundtrack when?", "Headphones on. Wonderful {audio}." } },
            { Topic.Story, new[] { "Didn't expect the {writing} to make me tear up.", "I read every line of the {writing}. Lovely writing." } },
            { Topic.Content, new[] { "So much game for {price}.", "{hours} hours in and I'm still finding new things." } },
            { Topic.Innovation, new[] { "\"{hook}\"? Why has nobody done this before?", "Never played anything quite like it." } },
            { Topic.Stability, new[] { "Runs perfectly. Not a single bug.", "Smooth as butter on my old laptop." } },
        };

        static readonly Dictionary<Topic, string[]> Complaints = new Dictionary<Topic, string[]>
        {
            { Topic.Gameplay, new[] { "Got bored of the {design} fast.", "Something about the {programming} feels off.",
                "Couldn't get into it. Not sure what I'm supposed to do." } },
            { Topic.Graphics, new[] { "Did anyone finish the {art}? Looks like placeholders.", "The art feels unfinished. Hope it gets an update." } },
            { Topic.Audio, new[] { "Muted the {audio} after ten minutes.", "The sound is… there. That's about it." } },
            { Topic.Story, new[] { "Skipped every line of the {writing}.", "The story doesn't go anywhere." } },
            { Topic.Content, new[] { "Finished it in an hour. For {price}?", "Over too quickly. I wanted more." } },
            { Topic.Innovation, new[] { "Feels like a dozen games I've already played.", "Nice, but nothing new." } },
            { Topic.Stability, new[] { "Crashed twice in the {design}. Patch please!", "Fell through the floor in the {design}. Twice.",
                "Great game when it doesn't crash." } },
        };

        /// <summary>Up to <paramref name="count"/> reviews for a released game (none before launch).</summary>
        public static List<PlayerReview> For(Project project, int count)
        {
            var reviews = new List<PlayerReview>();
            var result = project?.Result;
            if (result == null) return reviews;
            var rng = new SimRandom(StableHash.Fnv1a64(project.Title) ^ ((ulong)project.Id * 0x9E3779B97F4A7C15UL), 31);
            var seen = new HashSet<string>();
            for (int attempt = 0; attempt < count * 4 && reviews.Count < count; attempt++)
            {
                bool positive = rng.NextDouble() < result.PositiveShare;
                var topic = PickTopic(project, result, positive, rng);
                var pool = positive ? Praise[topic] : Complaints[topic];
                double hours = positive ? rng.LogNormal(6, 0.7) : rng.LogNormal(1.2, 0.6);
                string text = Fill(rng.Pick(pool), project, hours, rng);
                if (!seen.Add(text)) continue;
                reviews.Add(new PlayerReview { Positive = positive, Author = rng.Pick(Authors), Hours = Math.Round(hours, 1), Text = text });
            }
            return reviews;
        }

        static Topic PickTopic(Project project, ReleaseResult result, bool positive, SimRandom rng)
        {
            var weights = GameContent.Genre(project.Genre).Weights;
            var topics = new List<(Topic topic, double weight)>();
            foreach (var pair in result.Pillars)
            {
                double score = pair.Value;
                double genreWeight = weights.TryGetValue(pair.Key, out double w) ? Math.Max(0.05, w) : 0.05;
                double lean = positive ? Math.Max(0.1, score - 4) : Math.Max(0.1, 8 - score);
                topics.Add(((Topic)(int)pair.Key, genreWeight * lean));
            }
            // Bugs get talked about: stability is praised when it's spotless and complained about when it isn't.
            topics.Add((Topic.Stability, positive ? Math.Max(0, result.Stability - 8.5) * 0.3 : Math.Max(0, 9 - result.Stability) * 0.15));
            return rng.PickWeighted(topics, t => t.weight).topic;
        }

        static string Fill(string template, Project project, double hours, SimRandom rng)
        {
            string text = template;
            text = Insert(text, "{design}", Feature(project, Skill.Design, "levels", rng));
            text = Insert(text, "{programming}", Feature(project, Skill.Programming, "controls", rng));
            text = Insert(text, "{art}", Feature(project, Skill.Art, "art", rng));
            text = Insert(text, "{audio}", Feature(project, Skill.Audio, "music", rng));
            text = Insert(text, "{writing}", Feature(project, Skill.Writing, "story", rng));
            return text
                .Replace("{hook}", (project.Hook ?? "").TrimEnd('.'))
                .Replace("{price}", "$" + project.Price.ToString("0.00", CultureInfo.InvariantCulture))
                .Replace("{hours}", Math.Max(2, Math.Round(hours)).ToString("0", CultureInfo.InvariantCulture));
        }

        /// <summary>Numbered names read without an article: "every line of Chapter 2", not "of the Chapter 2".</summary>
        static string Insert(string text, string token, string name)
        {
            if (name.Length > 0 && char.IsDigit(name[name.Length - 1])) text = text.Replace("the " + token, token);
            return text.Replace(token, name);
        }

        /// <summary>A feature of that discipline, as players would call it ("Hero sprite" → "Hero sprite").</summary>
        internal static string Feature(Project project, Skill discipline, string fallback, SimRandom rng)
        {
            var stream = project.Stream(discipline);
            if (stream == null || stream.Features.Count == 0) return fallback;
            return stream.Features[rng.Range(0, Math.Min(stream.Features.Count, stream.FeaturesDone + 1))];
        }
    }

    /// <summary>
    /// Critic reviews for ScoreHub (Design/11 §1). A few outlets review the launch build and publish over the
    /// first days of launch week. Vapor games get more critics the more wishlists they have, a scratch.io game
    /// gets one blogger. Scores follow the game's quality and stability, with each outlet's taste and some
    /// noise. They're decided at launch (later hotfixes don't change them) and are deterministic per game.
    /// </summary>
    public static class CriticWriter
    {
        static readonly (string name, int bias)[] Outlets =
        {
            ("Pixel Press", 2), ("GameVault", -2), ("Indie Lantern", 4), ("Save Point Weekly", 0),
            ("The Joystick Herald", -4), ("Couch Co-op Club", 1), ("Nostalgia Bit", 3), ("Respawn Review", -1),
        };

        static readonly string[] Acclaim = { "A small masterpiece.", "Instantly one of the year's best indies.", "Pure joy from start to finish." };
        static readonly string[] Good = { "Charming, confident and easy to recommend.", "Small, sharp and full of heart.", "Worth it for the {feature} alone." };
        static readonly string[] Middling = { "Good ideas, rough edges.", "Worth a look for fans of the genre.", "Promising, if a little thin." };
        static readonly string[] Weak = { "It never quite comes together.", "Fun in bursts, frustrating in between.", "Needs another month in the oven." };
        static readonly string[] Poor = { "Hard to recommend.", "Buggy and unfinished.", "A frustrating mess." };

        public static List<CriticReview> For(Project project, double quality, double stability)
        {
            var rng = new SimRandom(StableHash.Fnv1a64(project.Title) ^ ((ulong)project.Id * 0xC2B2AE3D27D4EB4FUL), 59);
            int count = project.Platform == StorePlatform.Vapor
                ? Math.Min(Outlets.Length - 2, 2 + (int)(project.Wishlists / 500) + (project.Scope == ProjectScope.Small ? 1 : 0))
                : 1;
            var outlets = Outlets.OrderBy(o => rng.NextDouble()).Take(count).ToList();
            var critics = new List<CriticReview>();
            for (int i = 0; i < outlets.Count; i++)
            {
                double raw = 100 * (0.28 + 0.72 * quality) - 1.5 * (10 - stability) + outlets[i].bias + rng.Gaussian() * 6;
                int score = (int)Math.Round(Rules.Clamp(raw, 15, 97));
                var pool = score >= 85 ? Acclaim : score >= 72 ? Good : score >= 58 ? Middling : score >= 45 ? Weak : Poor;
                string quote = rng.Pick(pool).Replace("{feature}", ReviewWriter.Feature(project, Skill.Design, "core idea", rng));
                int day = count == 1 ? 2 : 1 + i * 4 / (count - 1);
                critics.Add(new CriticReview { Outlet = outlets[i].name, Score = score, Day = day, Quote = quote });
            }
            return critics;
        }
    }
}
