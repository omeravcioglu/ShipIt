using System.Collections.Generic;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Which minigame plays out an activity (Design/02 §4): building a feature is a circuit puzzle, each kind of
    /// gig has its own game, polishing is bug squashing. Planning-type activities (marketing, research, learning,
    /// rest) are decisions, not dexterity, so they resolve instantly.
    /// </summary>
    public static class Activities
    {
        static readonly Dictionary<Skill, string[]> Parts = new Dictionary<Skill, string[]>
        {
            { Skill.Programming, new[] { "Input", "Physics", "Collision", "State machine", "Camera", "Save data", "Events", "Timers", "Spawner", "Debug view" } },
            { Skill.Art, new[] { "Sketch", "Outline", "Base colors", "Shading", "Highlights", "Animation", "Palette", "Export" } },
            { Skill.Design, new[] { "Layout", "Pacing", "Difficulty", "Rewards", "Secrets", "Checkpoints", "Tuning", "Playtest notes" } },
            { Skill.Audio, new[] { "Melody", "Bass line", "Drums", "Mix", "Sound effects", "Loop points", "Mastering" } },
            { Skill.Writing, new[] { "Outline", "First draft", "Dialogue", "Jokes", "Edit", "Names", "Lore" } },
        };

        static readonly string[] JamParts = { "Core idea", "Player", "Goal", "Juice", "Sound", "Title screen", "Last-minute fixes" };

        /// <summary>The minigame for an activity in the current slot, or null when the activity resolves instantly.</summary>
        public static Minigame For(GameSimulation sim, PlanBlock activity, bool relaxed = false)
        {
            var game = Create(sim, activity);
            if (game != null) game.Setup.Relaxed = relaxed;
            return game;
        }

        static Minigame Create(GameSimulation sim, PlanBlock activity)
        {
            var s = sim.State;
            string when = GameCalendar.BlockNames[Mathf.Clamp(s.Slot, 0, 2)].ToUpperInvariant();
            int seed = unchecked((int)(s.Seed % 1000003) * 7919 + s.Day * 31 + s.Slot * 7 + (int)activity.Kind);
            switch (activity.Kind)
            {
                case ActivityKind.Develop:
                {
                    var project = s.ActiveProject;
                    if (project == null) return null;
                    int tier = project.Scope == ProjectScope.Small ? 1 : 0;
                    if (project.Status == ProjectStatus.ReadyToRelease)
                        return new BugSquash(new MinigameSetup
                        {
                            Kicker = when + " · POLISH", Title = "Polish " + project.Title, Subtitle = $"About {project.BugsRemaining:0} bugs left to find",
                            Icon = Icons.Bug, TileClass = "skill-programming", Skill = Skill.Programming,
                            SkillLevel = s.Founder.Get(Skill.Programming), Tier = tier, Seed = seed, OwnGame = true,
                        });
                    var stream = activity.HasFocus ? project.Stream(activity.Focus) : null;
                    if (stream == null || stream.IsDone) stream = GameSimulation.Bottleneck(project);
                    if (stream == null) return null;
                    return new WireItUp(new MinigameSetup
                    {
                        Kicker = when + " · BUILD", Title = stream.FeatureName(stream.CurrentFeature),
                        Subtitle = $"{project.Title} · {stream.Discipline} · feature {project.FeaturesDone + 1} of {project.FeatureCount}",
                        Icon = Icons.Plug, TileClass = Fmt.SkillClass(stream.Discipline), Skill = stream.Discipline,
                        SkillLevel = s.Founder.Get(stream.Discipline), Tier = tier, Seed = seed, Parts = Shuffled(Parts[stream.Discipline], seed),
                    });
                }
                case ActivityKind.Hotfix:
                {
                    var game = s.ReleasedProjects.FirstOrDefault(p => p.Id == activity.Option) ?? sim.PatchableGame;
                    if (game == null || sim.DaysSinceLaunch(game) >= Tuning.PatchWindowDays) return null;
                    int launchDay = sim.DaysSinceLaunch(game) + 1;
                    return new BugSquash(new MinigameSetup
                    {
                        Kicker = when + " · HOTFIX", Title = "Hotfix " + game.Title,
                        Subtitle = (launchDay <= Tuning.LaunchWeekDays ? $"Launch day {launchDay} · " : "") + $"about {game.BugsRemaining:0} bugs left",
                        Icon = Icons.Bug, TileClass = "act-hotfix", Skill = Skill.Programming, SkillLevel = s.Founder.Get(Skill.Programming),
                        Tier = game.Scope == ProjectScope.Small ? 1 : 0, Seed = seed, OwnGame = true,
                    });
                }
                case ActivityKind.Community:
                {
                    var game = s.ReleasedProjects.FirstOrDefault(p => p.Id == activity.Option) ?? sim.PatchableGame;
                    if (game == null || sim.DaysSinceLaunch(game) >= Tuning.PatchWindowDays) return null;
                    var r = game.Result;
                    return new FeedbackSort(new MinigameSetup
                    {
                        Kicker = when + " · PLAYERS", Title = "Answer players: " + game.Title,
                        Subtitle = r.ReviewCount > 0 ? $"{r.ReviewCount} reviews · {Fmt.Pct(r.PositiveShare)} positive" : $"Players' mood: {Fmt.Pct(r.Mood)} positive",
                        Icon = Icons.Chat, TileClass = "act-community", Skill = Skill.Design, SkillLevel = s.Founder.Get(Skill.Design),
                        Tier = 0, Seed = seed, OwnGame = true,
                    });
                }
                case ActivityKind.Demo:
                {
                    var game = s.Projects.FirstOrDefault(p => p.Id == activity.Option) ?? s.ActiveProject;
                    if (!sim.CanWorkOnDemo(game) || game.Progress < Tuning.DemoMinProgress) return null;
                    int days = game.FestDay - s.Day;
                    return new BugSquash(new MinigameSetup
                    {
                        Kicker = when + " · DEMO", Title = "Make the demo: " + game.Title,
                        Subtitle = $"Demo {Fmt.Pct(game.DemoProgress)} done · Next Fest in {days} day{(days == 1 ? "" : "s")}",
                        Icon = Icons.Play, TileClass = "act-demo", Skill = Skill.Programming, SkillLevel = s.Founder.Get(Skill.Programming),
                        Tier = 1, Seed = seed, OwnGame = true,
                    });
                }
                case ActivityKind.Jam:
                    return new WireItUp(new MinigameSetup
                    {
                        Kicker = when + " · GAME JAM", Title = "The jam prototype", Subtitle = "72 hours, one game, thousands of developers",
                        Icon = Icons.Bolt, TileClass = "act-jam", Skill = Skill.Programming, SkillLevel = s.Founder.Get(Skill.Programming),
                        Tier = 0, Seed = seed, Parts = JamParts,
                    });
                case ActivityKind.Freelance:
                {
                    if (activity.Option < 0) return null; // prospecting is instant
                    var gig = (activity.Option > 0 ? s.Freelance.Active.FirstOrDefault(g => g.Id == activity.Option) : null)
                              ?? s.Freelance.Active.OrderBy(g => g.DeadlineDay).FirstOrDefault();
                    if (gig == null) return null;
                    var setup = new MinigameSetup
                    {
                        Kicker = when + " · GIG", Title = gig.Title, Subtitle = $"{gig.ClientName} · {gig.Discipline}",
                        Icon = Fmt.SkillIcon(gig.Discipline), TileClass = Fmt.SkillClass(gig.Discipline), Skill = gig.Discipline,
                        SkillLevel = s.Founder.Get(gig.Discipline), Tier = gig.RequiredSkill <= 2.5 ? 0 : gig.RequiredSkill <= 4 ? 1 : 2, Seed = seed,
                    };
                    switch (gig.Discipline)
                    {
                        case Skill.Art: return new PixelMatch(setup);
                        case Skill.Writing: return new TypoHunt(setup);
                        case Skill.Audio: return new SynthTuner(setup);
                        case Skill.Design: return new FeedbackSort(setup);
                        default: return new BugSquash(setup);
                    }
                }
                default:
                    return null;
            }
        }

        static string[] Shuffled(string[] items, int seed)
        {
            var rng = new System.Random(seed);
            return items.OrderBy(i => rng.Next()).ToArray();
        }

        /// <summary>The minigame's name for an activity (shown before you pick it), or null for instant ones.</summary>
        public static string GameName(GameState s, PlanBlock activity)
        {
            switch (activity.Kind)
            {
                case ActivityKind.Develop:
                    var project = s.ActiveProject;
                    if (project == null) return null;
                    return project.Status == ProjectStatus.ReadyToRelease ? "Bug Squash" : "Wire It Up";
                case ActivityKind.Jam: return "Wire It Up";
                case ActivityKind.Hotfix: return "Bug Squash";
                case ActivityKind.Community: return "Feedback Sort";
                case ActivityKind.Demo: return "Bug Squash";
                case ActivityKind.Freelance:
                    var gig = s.Freelance.Active.FirstOrDefault(g => g.Id == activity.Option);
                    if (gig == null) return null;
                    return GigGameName(gig.Discipline);
                default: return null;
            }
        }

        public static string GigGameName(Skill discipline)
        {
            switch (discipline)
            {
                case Skill.Art: return "Pixel Match";
                case Skill.Writing: return "Typo Hunt";
                case Skill.Audio: return "Synth Tuner";
                case Skill.Design: return "Feedback Sort";
                default: return "Bug Squash";
            }
        }
    }
}
