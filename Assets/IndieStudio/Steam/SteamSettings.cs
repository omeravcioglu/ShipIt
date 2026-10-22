namespace IndieStudio.Steam
{
    /// <summary>The game's Steam settings (Steam/README.md). Replace the App IDs with yours from the Steamworks partner site.</summary>
    public static class SteamSettings
    {
        /// <summary>
        /// The full game's Steam App ID. 480 is Valve's "Spacewar" test app: good for trying sign-in and Steam Cloud,
        /// but it doesn't have this game's achievements (Steam refuses them, and the game logs a warning).
        /// The same number goes in steam_appid.txt for running in the editor.
        /// Ship It!'s own App ID is 5379550 (set it here once its Steamworks settings are published).
        /// </summary>
        public const uint FullGameAppId = 480;

        /// <summary>
        /// The demo's Steam App ID. On Steam a demo is an app of its own: create it from the full game's
        /// "All Associated Packages, DLC, Demos and Tools" page in Steamworks (Steam/README.md, "The demo"),
        /// then put its App ID here. Until then the demo uses the Spacewar test app too.
        /// </summary>
        public const uint DemoAppId = 480;

        /// <summary>The App ID of this build: a demo build (scripting define SHIPIT_DEMO) is the demo app.</summary>
#if SHIPIT_DEMO
        public const uint AppId = DemoAppId;
#else
        public const uint AppId = FullGameAppId;
#endif

        /// <summary>
        /// When a release build is started outside Steam, ask Steam to start it instead (so the overlay and
        /// sign-in work). Keep it off while testing with the Spacewar App ID: Steam would start Spacewar.
        /// </summary>
        public static readonly bool RestartThroughSteam = false;
    }
}
