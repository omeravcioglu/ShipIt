using System.Collections.Generic;
using UnityEngine;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Colors for things drawn in code (Painter2D and generated textures). Everything else is colored in
    /// Styles.uss; keep the two in step.
    /// </summary>
    public static class Palette
    {
        // Declared first: static fields initialize in order, and the colors below call Hex().
        static readonly Dictionary<string, Color> Cache = new Dictionary<string, Color>();

        public static readonly Color Ink = Hex("#3A2317");
        public static readonly Color Paper = Hex("#FFFAF1");
        public static readonly Color Cream = Hex("#FFF4E2");
        public static readonly Color Mustard = Hex("#F4B63F");
        public static readonly Color Tomato = Hex("#E0643C");
        public static readonly Color Red = Hex("#C73A2E");
        public static readonly Color Sage = Hex("#5E8C4A");
        public static readonly Color Teal = Hex("#2E7F7A");
        public static readonly Color Desk = Hex("#7A3E26");
        public static readonly Color DeskDots = Hex("#8E4C30");

        public static Color Hex(string hex, float alpha = 1f)
        {
            if (!Cache.TryGetValue(hex, out var color))
            {
                if (!ColorUtility.TryParseHtmlString(hex, out color)) color = Color.magenta;
                Cache[hex] = color;
            }
            color.a = alpha;
            return color;
        }
    }

    /// <summary>Small tiling textures generated at startup (USS has no gradients or patterns).</summary>
    public static class Textures
    {
        static Texture2D _desktopDots, _wallpaper, _floorboards, _stripes, _dangerStripes;

        /// <summary>The warm brown desktop behind app windows: a soft dot every 22 px.</summary>
        public static Texture2D DesktopDots => _desktopDots ? _desktopDots : _desktopDots = Dots(22, 2.2f, Palette.Desk, Palette.DeskDots);

        /// <summary>Bedroom wallpaper: two warm vertical stripes.</summary>
        public static Texture2D Wallpaper => _wallpaper ? _wallpaper : _wallpaper = Bands(46, 46, Hex("#F3C58C"), Hex("#EDB97C"));

        /// <summary>Floor planks with a thin dark seam.</summary>
        public static Texture2D Floorboards => _floorboards ? _floorboards : _floorboards = Bands(178, 2, Hex("#C98A55"), Hex("#B97A47"));

        /// <summary>Candy stripes for progress bars.</summary>
        public static Texture2D Stripes => _stripes ? _stripes : _stripes = Diagonal(16, Hex("#E0643C"), Hex("#EC7E55"));

        /// <summary>Hatching for anything below zero.</summary>
        public static Texture2D DangerStripes => _dangerStripes ? _dangerStripes : _dangerStripes = Diagonal(16, Hex("#C73A2E", 0.22f), Hex("#C73A2E", 0.08f));

        static Color Hex(string hex, float alpha = 1f) => Palette.Hex(hex, alpha);

        static Texture2D Create(int width, int height, FilterMode filter)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "IndieStudio generated",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = filter,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        static Texture2D Dots(int size, float radius, Color background, Color dot)
        {
            var texture = Create(size, size, FilterMode.Bilinear);
            var pixels = new Color[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    pixels[y * size + x] = Color.Lerp(dot, background, Mathf.Clamp01(d - radius + 0.5f));
                }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Texture2D Bands(int first, int second, Color a, Color b)
        {
            int width = first + second;
            var texture = Create(width, 2, FilterMode.Point);
            var pixels = new Color[width * 2];
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = x < first ? a : b;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Texture2D Diagonal(int size, Color a, Color b)
        {
            var texture = Create(size, size, FilterMode.Bilinear);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = (x + y) % size < size / 2 ? a : b;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
