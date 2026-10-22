using System.IO;
using System.Linq;
using IndieStudio.Game;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IndieStudio.EditorTools
{
    /// <summary>
    /// Windows builds of the full game and of the free demo, and a switch to play the demo in the editor.
    /// The demo build is the same project with the scripting define SHIPIT_DEMO and the product name "Ship It! Demo"
    /// (so its saves and settings live apart from the full game's). Builds go to Builds/ next to Assets/, ready for
    /// SteamPipe (Steam/README.md, "The demo"). From the command line, with the editor closed:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod IndieStudio.EditorTools.GameBuilds.BuildWindowsDemoFromCommandLine
    /// </summary>
    public static class GameBuilds
    {
        const string Scene = "Assets/Scenes/SampleScene.unity";
        const string PlayAsDemoMenu = "Indie Studio/Play as the demo";
        public const string DemoProductName = "Ship It! Demo";
        public const string DemoFolder = "Builds/Demo/Windows";
        public const string GameFolder = "Builds/Game/Windows";

        [MenuItem(PlayAsDemoMenu, false, 100)]
        static void TogglePlayAsDemo()
        {
            bool on = !EditorPrefs.GetBool(GameEdition.PlayAsDemoPref, false);
            EditorPrefs.SetBool(GameEdition.PlayAsDemoPref, on);
            Menu.SetChecked(PlayAsDemoMenu, on);
            Debug.Log(on ? "Play mode now starts the free demo (its own save: demo.isav)." : "Play mode now starts the full game.");
        }

        [MenuItem(PlayAsDemoMenu, true)]
        static bool TogglePlayAsDemoCheck()
        {
            Menu.SetChecked(PlayAsDemoMenu, EditorPrefs.GetBool(GameEdition.PlayAsDemoPref, false));
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Indie Studio/Build the Windows demo", false, 120)]
        public static void BuildWindowsDemo() => Report(Build(true));

        [MenuItem("Indie Studio/Build the Windows game", false, 121)]
        public static void BuildWindowsGame() => Report(Build(false));

        /// <summary>Builds the demo and quits the editor with 0 when it worked (for -batchmode -executeMethod).</summary>
        public static void BuildWindowsDemoFromCommandLine() => EditorApplication.Exit(Build(true).summary.result == BuildResult.Succeeded ? 0 : 1);

        /// <summary>Builds the full game and quits the editor with 0 when it worked (for -batchmode -executeMethod).</summary>
        public static void BuildWindowsGameFromCommandLine() => EditorApplication.Exit(Build(false).summary.result == BuildResult.Succeeded ? 0 : 1);

        /// <summary>A 64-bit Windows build of the demo or the full game. The project's own settings are left as they were.</summary>
        public static BuildReport Build(bool demo)
        {
            string folder = demo ? DemoFolder : GameFolder;
            string exe = demo ? "Ship It Demo.exe" : "Ship It.exe";
            string productName = PlayerSettings.productName;
            try
            {
                if (demo) PlayerSettings.productName = DemoProductName;
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = Path.Combine(folder, exe),
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    extraScriptingDefines = demo ? new[] { "SHIPIT_DEMO" } : new string[0],
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result == BuildResult.Succeeded)
                {
                    // Steam builds never ship a steam_appid.txt, and Unity's debug symbols don't belong in the upload.
                    foreach (var extra in new[] { "steam_appid.txt" }.Select(name => Path.Combine(folder, name)).Where(File.Exists)) File.Delete(extra);
                    string symbols = Path.Combine(folder, Path.GetFileNameWithoutExtension(exe) + "_BurstDebugInformation_DoNotShip");
                    if (Directory.Exists(symbols)) Directory.Delete(symbols, true);
                }
                return report;
            }
            finally
            {
                PlayerSettings.productName = productName;
                AssetDatabase.SaveAssets();
            }
        }

        static void Report(BuildReport report)
        {
            var summary = report.summary;
            string where = Path.GetFullPath(Path.GetDirectoryName(summary.outputPath) ?? ".");
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build finished: {where} ({summary.totalSize / 1048576f:0.0} MB, {summary.totalTime.TotalSeconds:0} s).");
                EditorUtility.RevealInFinder(summary.outputPath);
            }
            else Debug.LogError($"Build {summary.result}: {summary.totalErrors} error(s). See the console.");
        }
    }
}
