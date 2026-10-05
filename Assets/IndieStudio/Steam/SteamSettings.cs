namespace IndieStudio.Steam
{
    /// <summary>The game's Steam settings (Steam/README.md). Replace the App ID with yours from the Steamworks partner site.</summary>
    public static class SteamSettings
    {
        /// <summary>
        /// The game's Steam App ID. 480 is Valve's "Spacewar" test app: good for trying sign-in and Steam Cloud,
        /// but it doesn't have this game's achievements (Steam refuses them, and the game logs a warning).
        /// The same number goes in steam_appid.txt for running in the editor.
        /// </summary>
        public const uint AppId = 480;

        /// <summary>
        /// When a release build is started outside Steam, ask Steam to start it instead (so the overlay and
        /// sign-in work). Keep it off while testing with the Spacewar App ID: Steam would start Spacewar.
        /// </summary>
        public static readonly bool RestartThroughSteam = false;
    }
}
