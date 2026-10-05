using UnityEditor;
using UnityEngine;

namespace IndieStudio.EditorTools
{
    /// <summary>
    /// Applies the game's store identity to the player settings: "Ship It!" by McHunter Studio, with the
    /// founder-face icon from Branding/AppIcon.png (the same art as the Steam app icon). Run from the menu or with
    /// -executeMethod IndieStudio.EditorTools.Branding.Apply.
    /// </summary>
    public static class Branding
    {
        const string IconPath = "Assets/IndieStudio/Branding/AppIcon.png";

        [MenuItem("Indie Studio/Apply Branding")]
        public static void Apply()
        {
            PlayerSettings.productName = "Ship It!";
            PlayerSettings.companyName = "McHunter Studio";

            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(IconPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log("Branding applied: Ship It! by McHunter Studio, icon " + (icon != null ? "set" : "MISSING"));
        }
    }
}
