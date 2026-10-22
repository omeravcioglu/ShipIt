using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace IndieStudio.Game
{
    /// <summary>
    /// Starts the game when the main scene enters Play mode, so no scene setup is required.
    /// The UI is built in code with UI Toolkit (Design/20 §3).
    /// </summary>
    public static class GameBootstrap
    {
        const string MainSceneName = "SampleScene";

        /// <summary>Where the real game keeps its save (tests pass their own folder, or none).</summary>
        public static string DefaultSaveFolder => System.IO.Path.Combine(Application.persistentDataPath, "Saves");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (SceneManager.GetActiveScene().name != MainSceneName) return;
            if (Object.FindAnyObjectByType<GameController>() != null) return;
            var edition = GameEdition.Current;
            if (edition == Simulation.Edition.Full) CarryOverOldSave(); // the demo has its own folder and save, and no old saves
            Create(0, null, DefaultSaveFolder, Platforms.Shared, edition);
        }

        /// <summary>
        /// The game was renamed (company "McHunter Studio", product "Ship It!"), which moves persistentDataPath.
        /// Copies a save from the old "DefaultCompany/IndieStudio" folder once, so playtests continue.
        /// </summary>
        static void CarryOverOldSave()
        {
            try
            {
                var company = System.IO.Directory.GetParent(Application.persistentDataPath)?.Parent;
                if (company == null || System.IO.Directory.Exists(DefaultSaveFolder)) return;
                string old = System.IO.Path.Combine(company.FullName, "DefaultCompany", "IndieStudio", "Saves");
                if (!System.IO.Directory.Exists(old)) return;
                System.IO.Directory.CreateDirectory(DefaultSaveFolder);
                foreach (var file in System.IO.Directory.GetFiles(old))
                    System.IO.File.Copy(file, System.IO.Path.Combine(DefaultSaveFolder, System.IO.Path.GetFileName(file)));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not carry over the old save: {e.Message}");
            }
        }

        /// <summary>
        /// Creates the game. With a <paramref name="targetTexture"/> the UI renders into it (screenshots). With a
        /// <paramref name="saveFolder"/> it continues the save found there and autosaves; without one nothing is
        /// written to disk. The real game passes <see cref="Platforms.Shared"/> (Steam, or offline); tests pass a fake
        /// or nothing (a forgetful offline platform, so they never touch the player's Steam account or settings).
        /// <paramref name="edition"/> picks the full game or the free demo.
        /// </summary>
        public static GameController Create(ulong seed, RenderTexture targetTexture, string saveFolder = null, IPlatform platform = null,
            Simulation.Edition edition = Simulation.Edition.Full)
        {
            var go = new GameObject(edition == Simulation.Edition.Demo ? "Indie Studio (demo)" : "Indie Studio");
            go.SetActive(false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = CreatePanelSettings(targetTexture);
            var controller = go.AddComponent<GameController>();
            controller.Seed = seed;
            controller.Edition = edition;
            controller.SaveFolder = saveFolder;
            controller.PlatformOverride = platform;
            go.SetActive(true);
            return controller;
        }

        static PanelSettings CreatePanelSettings(RenderTexture targetTexture)
        {
            // PanelSettings warns about a missing theme as it is created, before one can be assigned (two lines down).
            bool logging = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            Debug.unityLogger.logEnabled = logging;
            settings.name = "Indie Studio Panel";
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("IndieStudio/Theme");
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1600, 900);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.clearColor = true;
            settings.colorClearValue = new Color(0.169f, 0.102f, 0.071f, 1f);
            if (targetTexture != null) settings.targetTexture = targetTexture;
            return settings;
        }
    }
}
