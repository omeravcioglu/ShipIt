using System;
using System.Collections.Generic;
using IndieStudio.Simulation;

namespace IndieStudio.Game
{
    /// <summary>
    /// Checks the achievements after every change and unlocks new ones on the platform (Steam, or this computer).
    /// Some are moments rather than states (two coffees today, full flow right now), so they're unlocked as they
    /// happen. When a studio is loaded its earlier achievements are sent quietly; only new ones get a toast.
    /// </summary>
    public sealed class AchievementTracker
    {
        readonly IPlatform _platform;
        readonly HashSet<string> _sent = new HashSet<string>();

        public AchievementTracker(IPlatform platform)
        {
            _platform = platform;
        }

        /// <summary>An achievement the player didn't have before (for the toast).</summary>
        public event Action<AchievementDef> Unlocked;

        /// <summary>Forgets what was sent for the previous studio (a new or different studio was loaded).</summary>
        public void Reset() => _sent.Clear();

        /// <summary>False in the demo: achievements belong to the full game (a Steam demo is its own app, without them).</summary>
        public bool Enabled { get; set; } = true;

        public void Check(GameState state, bool quietly)
        {
            if (state == null || !Enabled) return;
            foreach (var achievement in AchievementCatalog.All)
            {
                if (_sent.Contains(achievement.Id) || !achievement.IsEarned(state)) continue;
                _sent.Add(achievement.Id);
                if (_platform.UnlockAchievement(achievement.Id) && !quietly) Unlocked?.Invoke(achievement);
            }
        }

        public int UnlockedCount
        {
            get
            {
                int count = 0;
                foreach (var achievement in AchievementCatalog.All)
                    if (_platform.IsAchievementUnlocked(achievement.Id)) count++;
                return count;
            }
        }
    }
}
