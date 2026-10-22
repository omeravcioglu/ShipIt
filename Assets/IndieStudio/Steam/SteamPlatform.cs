#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX) && !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.Text;
using IndieStudio.Game;
using Steamworks;
using UnityEngine;

namespace IndieStudio.Steam
{
    /// <summary>
    /// The game on Steam (Steamworks.NET): the signed-in player, achievements, Steam Cloud saves, and tickets
    /// for a future game server. This assembly only compiles when the Steamworks.NET package is installed
    /// (Steam/README.md); without it, or when Steam isn't running, the game plays offline.
    /// </summary>
    public sealed class SteamPlatform : IPlatform, IStoreOverlay
    {
        /// <summary>Seconds between attempts to hand Steam an achievement it didn't take yet.</summary>
        const float RetrySeconds = 5f;
        /// <summary>About two minutes of retries, then a warning: the achievement probably isn't set up in Steamworks.</summary>
        const int MaxAttempts = 24;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() => Platforms.Steam = TryStart;

        readonly Dictionary<string, int> _pending = new Dictionary<string, int>();
        readonly SteamCloud _cloud = new SteamCloud();
        readonly Callback<GetTicketForWebApiResponse_t> _ticketResponse;
        bool _storeStats;
        float _nextAttempt;
        HAuthTicket _ticket = HAuthTicket.Invalid;
        Action<string> _onTicket;
        bool _disposed;

        SteamPlatform()
        {
            _ticketResponse = Callback<GetTicketForWebApiResponse_t>.Create(OnTicket);
        }

        /// <summary>Starts Steam, or says why it can't (the game then runs offline).</summary>
        public static IPlatform TryStart(out string problem)
        {
            problem = null;
            try
            {
                if (!Packsize.Test())
                {
                    problem = "This Steamworks.NET build doesn't match the platform, so Steam is off.";
                    return null;
                }
#if !UNITY_EDITOR
                if (SteamSettings.RestartThroughSteam && SteamAPI.RestartAppIfNecessary(new AppId_t(SteamSettings.AppId)))
                {
                    // Started outside Steam: Steam starts the game again itself.
                    problem = "Restarting through Steam.";
                    Application.Quit();
                    return null;
                }
#endif
                if (!SteamAPI.Init())
                {
#if UNITY_EDITOR
                    problem = "Steam isn't running, you aren't signed in, or steam_appid.txt is missing from the project folder.";
#else
                    problem = "Steam isn't running, or you aren't signed in to it.";
#endif
                    return null;
                }
            }
            catch (DllNotFoundException e)
            {
                problem = "Steam's library is missing (" + e.Message + ").";
                return null;
            }
            return new SteamPlatform();
        }

        public string Name => "Steam";
        public bool IsSignedIn => !_disposed && SteamUser.BLoggedOn();
        public string Problem => null;
        public ICloudStorage Cloud => _cloud;

        public PlayerIdentity Player => _disposed ? null : new PlayerIdentity
        {
            Id = SteamUser.GetSteamID().m_SteamID.ToString(),
            DisplayName = SteamFriends.GetPersonaName(),
        };

        public bool IsAchievementUnlocked(string id) => !_disposed && SteamUserStats.GetAchievement(id, out bool achieved) && achieved;

        /// <summary>Shows a game's store page in the Steam overlay (the demo's "Wishlist the full game").</summary>
        public bool OpenStorePage(uint appId)
        {
            if (_disposed || !SteamUtils.IsOverlayEnabled()) return false;
            SteamFriends.ActivateGameOverlayToStore(new AppId_t(appId), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
            return true;
        }

        public bool UnlockAchievement(string id)
        {
            if (_disposed || _pending.ContainsKey(id) || IsAchievementUnlocked(id)) return false;
            _pending[id] = 0;
            Flush();
            return true;
        }

        // Steam can refuse an achievement in the first moments after start; refused ones are tried again for a while.
        void Flush()
        {
            foreach (var id in new List<string>(_pending.Keys))
            {
                if (SteamUserStats.SetAchievement(id))
                {
                    _pending.Remove(id);
                    _storeStats = true;
                }
                else if (++_pending[id] >= MaxAttempts)
                {
                    _pending.Remove(id);
                    Debug.LogWarning($"Steam didn't accept the achievement \"{id}\". Is it set up and published under Stats & Achievements in Steamworks (Steam/README.md)?");
                }
            }
            if (_storeStats && SteamUserStats.StoreStats()) _storeStats = false; // shows Steam's own "achievement unlocked" pop-up
        }

        /// <summary>
        /// A ticket that proves who the player is to a game server (Steam/README.md, "Signing in"). The server checks it
        /// with the ISteamUserAuth/AuthenticateUserTicket Web API, using the same <paramref name="audience"/>.
        /// </summary>
        public void RequestAuthTicket(string audience, Action<string> onTicket)
        {
            if (_disposed)
            {
                onTicket?.Invoke(null);
                return;
            }
            if (_ticket != HAuthTicket.Invalid) SteamUser.CancelAuthTicket(_ticket);
            _onTicket = onTicket;
            _ticket = SteamUser.GetAuthTicketForWebApi(audience);
        }

        void OnTicket(GetTicketForWebApiResponse_t response)
        {
            if (response.m_hAuthTicket != _ticket) return;
            var callback = _onTicket;
            _onTicket = null;
            if (response.m_eResult != EResult.k_EResultOK || response.m_rgubTicket == null)
            {
                callback?.Invoke(null);
                return;
            }
            var hex = new StringBuilder(response.m_cubTicket * 2);
            for (int i = 0; i < response.m_cubTicket; i++) hex.Append(response.m_rgubTicket[i].ToString("x2"));
            callback?.Invoke(hex.ToString());
        }

        public void Tick()
        {
            if (_disposed) return;
            SteamAPI.RunCallbacks();
            if ((_pending.Count > 0 || _storeStats) && Time.unscaledTime >= _nextAttempt)
            {
                _nextAttempt = Time.unscaledTime + RetrySeconds;
                Flush();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_storeStats) SteamUserStats.StoreStats();
            if (_ticket != HAuthTicket.Invalid) SteamUser.CancelAuthTicket(_ticket);
            _ticketResponse.Dispose();
            _disposed = true;
            SteamAPI.Shutdown();
        }
    }

    /// <summary>
    /// Steam Cloud through ISteamRemoteStorage. Writes go to Steam's local cache at once; Steam uploads them in the
    /// background and when the game quits, and downloads the latest copy before the game starts.
    /// </summary>
    sealed class SteamCloud : ICloudStorage
    {
        public bool IsEnabled => SteamRemoteStorage.IsCloudEnabledForAccount() && SteamRemoteStorage.IsCloudEnabledForApp();

        public bool Exists(string name) => SteamRemoteStorage.FileExists(name);

        public byte[] Read(string name)
        {
            int size = SteamRemoteStorage.GetFileSize(name);
            if (size <= 0) return null;
            var data = new byte[size];
            return SteamRemoteStorage.FileRead(name, data, size) == size ? data : null;
        }

        public bool Write(string name, byte[] data) => SteamRemoteStorage.FileWrite(name, data, data.Length);

        public DateTime? Timestamp(string name) =>
            Exists(name) ? DateTimeOffset.FromUnixTimeSeconds(SteamRemoteStorage.GetFileTimestamp(name)).LocalDateTime : (DateTime?)null;
    }
}
#endif
