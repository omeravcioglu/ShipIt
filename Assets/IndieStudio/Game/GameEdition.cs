using System;
using IndieStudio.Simulation;
using UnityEngine;

namespace IndieStudio.Game
{
    /// <summary>
    /// Which game this is: the full game, or the free demo (<see cref="DemoRules"/>: chapter 1 in 15–20 minutes).
    /// A demo build is made with the scripting define <c>SHIPIT_DEMO</c> (menu: Indie Studio → Build the Windows
    /// demo). Any build also runs as the demo with the command-line argument <c>-demo</c>, and in the editor the menu
    /// item Indie Studio → Play as the demo switches Play mode to it.
    /// </summary>
    public static class GameEdition
    {
        /// <summary>The full game's Steam App ID: the demo's "Wishlist" button opens its store page.</summary>
        public const uint FullGameAppId = 5379550;
        public const string FullGameStoreUrl = "https://store.steampowered.com/app/5379550/";
        /// <summary>The editor preference behind Indie Studio → Play as the demo.</summary>
        public const string PlayAsDemoPref = "IndieStudio.PlayAsDemo";

        /// <summary>The edition the real game starts in (worked out each time, so the editor menu applies to the next Play).</summary>
        public static Edition Current
        {
            get
            {
#if SHIPIT_DEMO
                return Edition.Demo;
#else
                foreach (var arg in Environment.GetCommandLineArgs())
                    if (string.Equals(arg, "-demo", StringComparison.OrdinalIgnoreCase)) return Edition.Demo;
#if UNITY_EDITOR
                if (UnityEditor.EditorPrefs.GetBool(PlayAsDemoPref, false)) return Edition.Demo;
#endif
                return Edition.Full;
#endif
            }
        }

        /// <summary>The save file for an edition: the demo keeps its own, so it never overwrites a full-game studio.</summary>
        public static string SaveFileName(Edition edition) => edition == Edition.Demo ? SaveStore.DemoFileName : SaveStore.FileName;

        /// <summary>
        /// Opens the full game's store page so the player can wishlist it: in the Steam overlay when the game runs on
        /// Steam, otherwise in the browser.
        /// </summary>
        public static void OpenFullGameStorePage(IPlatform platform)
        {
            if (platform is IStoreOverlay overlay && overlay.OpenStorePage(FullGameAppId)) return;
            Application.OpenURL(FullGameStoreUrl);
        }
    }

    /// <summary>A platform that can show a game's store page in its own overlay (Steam).</summary>
    public interface IStoreOverlay
    {
        /// <summary>Shows the store page of <paramref name="appId"/>; false when the overlay isn't available.</summary>
        bool OpenStorePage(uint appId);
    }
}
