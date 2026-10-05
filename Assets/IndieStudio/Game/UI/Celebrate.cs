using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Little bursts of joy for the moments that deserve them (a finished feature, launch day, a purring cat):
    /// confetti and floating hearts made of plain elements and animated by USS transitions.
    /// </summary>
    public static class Celebrate
    {
        static readonly string[] Colors = { "bg-mustard", "bg-pink", "bg-cyan", "bg-green", "bg-orange", "bg-lavender", "bg-blue" };

        /// <summary>Throws confetti from a point of <paramref name="host"/> (in its local coordinates).</summary>
        public static void Confetti(VisualElement host, Vector2 origin, int count = 28, int seed = 1)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var piece = Ui.El("confetti " + Colors[i % Colors.Length] + (i % 3 == 0 ? " confetti--round" : ""));
                piece.pickingMode = PickingMode.Ignore;
                piece.style.left = origin.x;
                piece.style.top = origin.y;
                piece.style.rotate = new Rotate(Angle.Degrees(rng.Next(0, 360)));
                host.Add(piece);

                float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                float distance = 140f + (float)rng.NextDouble() * 280f;
                var target = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance * 0.6f + 120f);
                float spin = rng.Next(-720, 720);
                piece.schedule.Execute(() =>
                {
                    piece.AddToClassList("confetti--flying");
                    piece.style.translate = new Translate(target.x, target.y);
                    piece.style.rotate = new Rotate(Angle.Degrees(spin));
                }).StartingIn(30);
                piece.schedule.Execute(() => piece.RemoveFromHierarchy()).StartingIn(1800);
            }
        }

        /// <summary>A few hearts that float up from a point of <paramref name="host"/> and fade away.</summary>
        public static void Hearts(VisualElement host, Vector2 origin, int count = 3)
        {
            for (int i = 0; i < count; i++)
            {
                var heart = new Icon(Icons.Heart, 26f - 4f * (i % 2), 2.2f) { Filled = true };
                heart.AddToClassList("float-heart");
                heart.style.left = origin.x + (i - (count - 1) / 2f) * 26f;
                heart.style.top = origin.y + 8f * (i % 2);
                host.Add(heart);
                int delay = 40 + 160 * i;
                heart.schedule.Execute(() => heart.AddToClassList("float-heart--up")).StartingIn(delay);
                heart.schedule.Execute(() => heart.RemoveFromHierarchy()).StartingIn(delay + 1500);
            }
        }
    }
}
