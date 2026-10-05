using System;
using System.Collections;
using System.IO;
using IndieStudio.Game.UI;
using IndieStudio.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace IndieStudio.Game.Tests
{
    /// <summary>
    /// Draws every achievement's Steam icons into Steam/AchievementIcons: achieved and unachieved, at 64 px (JPG,
    /// for the Steamworks upload) and 256 px (PNG masters). It only runs with INDIE_ACHIEVEMENT_ICONS=1; otherwise
    /// it passes without doing anything.
    /// </summary>
    public class AchievementIconExport
    {
        [UnityTest]
        public IEnumerator Export_SteamAchievementIcons()
        {
            if (Environment.GetEnvironmentVariable("INDIE_ACHIEVEMENT_ICONS") != "1") yield break;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Steam", "AchievementIcons"));
            Directory.CreateDirectory(Path.Combine(folder, "256"));

            foreach (int size in new[] { 64, 256 })
            {
                var texture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
                texture.Create();
                var go = new GameObject("Achievement icon export");
                go.SetActive(false);
                var document = go.AddComponent<UIDocument>();
                bool logging = Debug.unityLogger.logEnabled;
                Debug.unityLogger.logEnabled = false; // PanelSettings warns about its theme before one can be set
                var settings = ScriptableObject.CreateInstance<PanelSettings>();
                Debug.unityLogger.logEnabled = logging;
                settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("IndieStudio/Theme");
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.scale = size / 64f;
                settings.clearColor = true;
                settings.colorClearValue = Color.white;
                settings.targetTexture = texture;
                document.panelSettings = settings;
                go.SetActive(true);
                yield return null;
                var root = document.rootVisualElement;
                root.styleSheets.Add(Resources.Load<StyleSheet>("IndieStudio/Styles"));

                foreach (var achievement in AchievementCatalog.All)
                    foreach (bool achieved in new[] { true, false })
                    {
                        root.Clear();
                        root.Add(AchievementView.Art(achievement, achieved));
                        yield return null;
                        yield return null;
                        string name = achievement.Id + (achieved ? "" : "_locked");
                        var image = Read(texture);
                        if (size == 64) File.WriteAllBytes(Path.Combine(folder, name + ".jpg"), image.EncodeToJPG(95));
                        else File.WriteAllBytes(Path.Combine(folder, "256", name + ".png"), image.EncodeToPNG());
                        UnityEngine.Object.Destroy(image);
                    }

                UnityEngine.Object.Destroy(go);
                UnityEngine.Object.Destroy(settings);
                texture.Release();
            }
            Assert.AreEqual(AchievementCatalog.All.Count * 2, Directory.GetFiles(folder, "*.jpg").Length);
        }

        static Texture2D Read(RenderTexture texture)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            return image;
        }
    }
}
