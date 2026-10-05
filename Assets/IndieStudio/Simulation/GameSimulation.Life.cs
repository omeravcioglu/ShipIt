using System;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// The small rhythms of a working day (Design/02 §4): flow from staying on one thing, coffee that buys
    /// output with burnout, the cat, and the studio's name.
    /// </summary>
    public sealed partial class GameSimulation
    {
        public const int StudioNameMaxLength = 28;

        // ------------------------------------------------------------------ the studio

        /// <summary>Names the studio (trimmed; an empty name keeps the old one).</summary>
        public void NameStudio(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return;
            if (name.Length > StudioNameMaxLength) name = name.Substring(0, StudioNameMaxLength).TrimEnd();
            State.StudioName = name;
        }

        static readonly string[] NameFirst = { "Lantern", "Pocket", "Moth", "Tiny", "Copper", "Honey", "Velvet", "Paper", "Salt", "Quiet", "Pixel", "Little" };
        static readonly string[] NameSecond = { "Bytes", "Games", "Harbor", "Garden", "Works", "Moon", "Fox", "Owl", "Lab", "Kite", "Engine", "Room" };

        /// <summary>A suggestion for the studio-name box (deterministic per seed and attempt).</summary>
        public string SuggestStudioName(int attempt)
        {
            var rng = new SimRandom(State.Seed ^ 0x5EED5EEDUL, (ulong)(attempt + 1));
            return $"{rng.Pick(NameFirst)} {rng.Pick(NameSecond)}";
        }

        // ------------------------------------------------------------------ flow

        /// <summary>
        /// What a session of this activity would count as for flow: the discipline of your game it would build,
        /// the gig it would work on, the skill it would study. Null for activities that don't flow.
        /// </summary>
        public string FlowKeyFor(PlanBlock activity)
        {
            switch (activity.Kind)
            {
                case ActivityKind.Develop:
                {
                    var project = State.ActiveProject;
                    if (project == null) return null;
                    if (project.Status == ProjectStatus.ReadyToRelease) return "polish";
                    var stream = PickStream(project, activity);
                    return stream == null ? null : "build:" + stream.Discipline;
                }
                case ActivityKind.Freelance:
                {
                    if (activity.Option < 0) return null;
                    var gig = (activity.Option > 0 ? State.Freelance.Active.FirstOrDefault(g => g.Id == activity.Option) : null)
                              ?? State.Freelance.Active.OrderBy(g => g.DeadlineDay).FirstOrDefault();
                    return gig == null ? null : "gig:" + gig.Id;
                }
                case ActivityKind.Learn:
                    return "learn:" + (activity.HasFocus ? activity.Focus.ToString() : "weakest");
                case ActivityKind.Demo:
                {
                    var game = State.Projects.FirstOrDefault(p => p.Id == activity.Option) ?? State.ActiveProject;
                    return CanWorkOnDemo(game) ? "demo:" + game.Id : null;
                }
                case ActivityKind.Hotfix:
                case ActivityKind.Community:
                {
                    var game = ReleasedGame(activity);
                    return game == null ? null : (activity.Kind == ActivityKind.Hotfix ? "hotfix:" : "community:") + game.Id;
                }
                default:
                    return null;
            }
        }

        /// <summary>The output bonus the next session of this activity would get from flow (0 when it would start fresh).</summary>
        public double FlowBonusFor(PlanBlock activity)
        {
            string key = FlowKeyFor(activity);
            if (key == null || key != State.FlowKey) return 0;
            return Math.Min(Tuning.FlowMaxBonus, Tuning.FlowPerSession * State.FlowStreak);
        }

        /// <summary>Advances or breaks the streak for a session that is about to run; returns its output multiplier.</summary>
        double ApplyFlow(PlanBlock block, bool forced)
        {
            if (!block.IsWork) return 1.0; // rest keeps the streak: you pick up where you left off
            string key = forced ? null : FlowKeyFor(block);
            if (key == null)
            {
                State.FlowKey = null;
                State.FlowStreak = 0;
                return 1.0;
            }
            if (key == State.FlowKey) State.FlowStreak++;
            else
            {
                State.FlowKey = key;
                State.FlowStreak = 1;
            }
            return 1 + Math.Min(Tuning.FlowMaxBonus, Tuning.FlowPerSession * (State.FlowStreak - 1));
        }

        // ------------------------------------------------------------------ coffee and the cat

        /// <summary>Brews a coffee: the next work session today counts ×1.15, for +1 burnout now. Two a day at most.</summary>
        public bool DrinkCoffee(out string error)
        {
            error = null;
            if (State.IsGameOver || !State.DayStarted || IsNight) { error = "It's late. Coffee now would only ruin your sleep."; return false; }
            if (State.CoffeeReady) { error = "There's still a full mug on your desk."; return false; }
            if (State.CoffeesToday >= Tuning.CoffeesPerDay) { error = "Your hands are shaking. No more coffee today."; return false; }
            State.CoffeeReady = true;
            State.CoffeesToday++;
            Founder.Burnout = Rules.Clamp(Founder.Burnout + Tuning.CoffeeBurnout, 0, 100);
            return true;
        }

        /// <summary>Pets the cat. When you're feeling low it helps a little, once a day. Returns what happened.</summary>
        public string PetCat()
        {
            if (State.CatPettedDay == State.Day) return "The cat has had quite enough attention today.";
            State.CatPettedDay = State.Day;
            State.Stats.CatPets++;
            if (Founder.Morale >= Tuning.CatMoraleBelow) return "The cat purrs and goes back to sleep.";
            Founder.Morale = Rules.Clamp(Founder.Morale + Tuning.CatMorale, 0, 100);
            return $"The cat climbs onto your lap and purrs. You feel a little better. Morale +{Tuning.CatMorale:0}.";
        }
    }
}
