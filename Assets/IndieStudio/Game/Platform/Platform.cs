using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace IndieStudio.Game
{
    /// <summary>Who's playing, as the platform knows them (Steam: the signed-in account).</summary>
    public sealed class PlayerIdentity
    {
        /// <summary>The platform's id for the player (Steam: the 64-bit Steam ID as text).</summary>
        public string Id;
        public string DisplayName;
    }

    /// <summary>Files the platform keeps for the player and syncs between their computers (Steam Cloud).</summary>
    public interface ICloudStorage
    {
        /// <summary>False when the player has turned cloud saves off (for their account or for this game).</summary>
        bool IsEnabled { get; }
        bool Exists(string name);
        /// <summary>The file's bytes, or null if it can't be read.</summary>
        byte[] Read(string name);
        bool Write(string name, byte[] data);
        DateTime? Timestamp(string name);
    }

    /// <summary>
    /// The store the game runs under: who's signed in, achievements, and cloud saves. Steam when the Steam
    /// module is installed and Steam is running (IndieStudio.Steam, see Steam/README.md); offline otherwise.
    /// </summary>
    public interface IPlatform : IDisposable
    {
        /// <summary>"Steam", or "Offline".</summary>
        string Name { get; }
        bool IsSignedIn { get; }
        /// <summary>The signed-in player, or null.</summary>
        PlayerIdentity Player { get; }
        /// <summary>Why the platform isn't available, when it isn't ("Steam isn't running").</summary>
        string Problem { get; }
        /// <summary>Cloud saves, or null when the platform has none.</summary>
        ICloudStorage Cloud { get; }
        bool IsAchievementUnlocked(string id);
        /// <summary>Unlocks an achievement for the player; true if they didn't have it yet.</summary>
        bool UnlockAchievement(string id);
        /// <summary>
        /// Asks for a ticket that proves to a game server who the player is (Steam: a Web API ticket for
        /// <paramref name="audience"/>). The callback gets the ticket as hex, or null if there is none.
        /// </summary>
        void RequestAuthTicket(string audience, Action<string> onTicket);
        /// <summary>Called every frame: platform callbacks, retries.</summary>
        void Tick();
    }

    /// <summary>
    /// Chooses the platform. The Steam module registers <see cref="Steam"/> before the first scene loads; the
    /// real game then gets Steam when it starts, and the offline platform when it doesn't (with the reason).
    /// There is one platform per run of the game: Steam can only be started once.
    /// </summary>
    public static class Platforms
    {
        public delegate IPlatform Factory(out string problem);

        /// <summary>Starts Steam; set by IndieStudio.Steam when Steamworks.NET is installed.</summary>
        public static Factory Steam;

        static IPlatform _shared;

        /// <summary>The platform for the real game (created on first use, closed when the application quits).</summary>
        public static IPlatform Shared
        {
            get
            {
                if (_shared != null) return _shared;
                string problem = "This build doesn't include Steam.";
                IPlatform platform = null;
                if (Steam != null)
                {
                    try
                    {
                        platform = Steam(out problem);
                    }
                    catch (Exception e)
                    {
                        problem = "Steam couldn't start: " + e.Message;
                    }
                }
                _shared = platform ?? new OfflinePlatform(true, problem);
                Application.quitting += CloseShared;
                return _shared;
            }
        }

        static void CloseShared()
        {
            Application.quitting -= CloseShared;
            _shared?.Dispose();
            _shared = null;
        }
    }

    /// <summary>
    /// Playing without a platform: achievements are remembered on this computer (PlayerPrefs), saves stay local.
    /// Tests use a non-persistent one, so they never touch the player's records.
    /// </summary>
    public sealed class OfflinePlatform : IPlatform
    {
        const string AchievementsKey = "IndieStudio.Achievements";
        readonly bool _persistent;
        readonly HashSet<string> _unlocked = new HashSet<string>();

        public OfflinePlatform(bool persistent, string problem = null)
        {
            _persistent = persistent;
            Problem = problem;
            if (!persistent) return;
            foreach (var id in PlayerPrefs.GetString(AchievementsKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                _unlocked.Add(id);
        }

        public string Name => "Offline";
        public bool IsSignedIn => false;
        public PlayerIdentity Player => null;
        public string Problem { get; }
        public ICloudStorage Cloud => null;

        public bool IsAchievementUnlocked(string id) => _unlocked.Contains(id);

        public bool UnlockAchievement(string id)
        {
            if (!_unlocked.Add(id)) return false;
            if (_persistent)
            {
                PlayerPrefs.SetString(AchievementsKey, string.Join(",", _unlocked.OrderBy(x => x)));
                PlayerPrefs.Save();
            }
            return true;
        }

        public void RequestAuthTicket(string audience, Action<string> onTicket) => onTicket?.Invoke(null);
        public void Tick() { }
        public void Dispose() { }
    }

    /// <summary>
    /// A folder that stands in for cloud storage: tests use it to play "two computers" sharing one cloud, and it
    /// is handy for trying the sync logic without Steam.
    /// </summary>
    public sealed class FolderCloudStorage : ICloudStorage
    {
        readonly string _folder;

        public FolderCloudStorage(string folder)
        {
            _folder = folder;
        }

        public bool IsEnabled { get; set; } = true;

        string PathOf(string name) => Path.Combine(_folder, name);
        public bool Exists(string name) => File.Exists(PathOf(name));

        public byte[] Read(string name)
        {
            try
            {
                return File.ReadAllBytes(PathOf(name));
            }
            catch (IOException)
            {
                return null;
            }
        }

        public bool Write(string name, byte[] data)
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllBytes(PathOf(name), data);
            return true;
        }

        public DateTime? Timestamp(string name) => Exists(name) ? File.GetLastWriteTime(PathOf(name)) : (DateTime?)null;
    }
}
