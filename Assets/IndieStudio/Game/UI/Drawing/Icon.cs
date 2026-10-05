using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// A stroked line icon drawn with Painter2D from 24×24 SVG path data (see <see cref="Icons"/>).
    /// It takes its color from the USS <c>color</c> property, so icons follow text color and hover states.
    /// </summary>
    public sealed class Icon : VisualElement
    {
        string _path;
        readonly float _stroke;

        public Icon(string path, float size = 20f, float stroke = 2.4f)
        {
            _path = path;
            _stroke = stroke;
            AddToClassList("icon");
            pickingMode = PickingMode.Ignore;
            style.width = size;
            style.height = size;
            generateVisualContent += Draw;
        }

        public string Path
        {
            get => _path;
            set
            {
                if (_path == value) return;
                _path = value;
                MarkDirtyRepaint();
            }
        }

        bool _filled;

        /// <summary>Fills the shape with the text color and outlines it in ink (stars, badges).</summary>
        public bool Filled
        {
            get => _filled;
            set
            {
                if (_filled == value) return;
                _filled = value;
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f || string.IsNullOrEmpty(_path)) return;
            float scale = Mathf.Min(rect.width, rect.height) / 24f;
            var offset = new Vector2((rect.width - 24f * scale) / 2f, (rect.height - 24f * scale) / 2f);
            var painter = context.painter2D;
            painter.lineWidth = _stroke * scale;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            SvgPath.Trace(painter, _path, scale, offset);
            if (_filled)
            {
                painter.fillColor = resolvedStyle.color;
                painter.Fill();
                painter.strokeColor = Palette.Ink;
            }
            else painter.strokeColor = resolvedStyle.color;
            painter.Stroke();
        }
    }

    /// <summary>Icon path data (24×24 viewBox), shared with the design mockups.</summary>
    public static class Icons
    {
        public const string Home = "M3 11 L12 4 L21 11 M5 10 V20 H19 V10 M10 20 V14 H14 V20";
        public const string Dashboard = "M4 4 H10 V10 H4 Z M14 4 H20 V10 H14 Z M4 14 H10 V20 H4 Z M14 14 H20 V20 H14 Z";
        public const string Mail = "M3 6 H21 V18 H3 Z M3 6 L12 13 L21 6";
        public const string Calendar = "M4 5 H20 V20 H4 Z M4 10 H20 M9 3 V7 M15 3 V7";
        public const string Gamepad = "M6 8 H18 A4.5 4.5 0 0 1 18 17 H6 A4.5 4.5 0 0 1 6 8 Z M8 10.5 V14.5 M6 12.5 H10 M15.5 11.5 V12 M17.5 13.5 V14";
        public const string Briefcase = "M4 8 H20 V19 H4 Z M9 8 V5 H15 V8 M4 13 H20";
        public const string Bulb = "M9 16 C9 13 7 12 7 9 A5 5 0 0 1 17 9 C17 12 15 13 15 16 Z M9 19 H15 M10 22 H14";
        public const string Bank = "M3 10 L12 4 L21 10 Z M5 10 V18 M10 10 V18 M14 10 V18 M19 10 V18 M3 20 H21";
        public const string Person = "M12 12 A4 4 0 1 0 12 4 A4 4 0 1 0 12 12 Z M4 21 C4 16 20 16 20 21";
        public const string Bag = "M5 8 H19 L18 21 H6 Z M9 8 V6 A3 3 0 0 1 15 6 V8";

        public const string Pause = "M8 5 V19 M16 5 V19";
        public const string Play = "M7 5 L18 12 L7 19 Z";
        public const string Fast = "M4 6 L11 12 L4 18 Z M12 6 L19 12 L12 18 Z";
        public const string Fastest = "M2 6 L8 12 L2 18 Z M9 6 L15 12 L9 18 Z M16 6 L22 12 L16 18 Z";
        public const string Skip = "M5 5 L13 12 L5 19 Z M15 5 V19";

        public const string Hourglass = "M6 3 H18 M6 21 H18 M7 3 C7 9 12 10 12 12 C12 14 7 15 7 21 M17 3 C17 9 12 10 12 12 C12 14 17 15 17 21";
        public const string Bell = "M6 17 V11 A6 6 0 0 1 18 11 V17 L20 19 H4 Z M10 21 H14";
        public const string Warning = "M12 3 L22 20 H2 Z M12 10 V14 M12 17 V17.5";
        public const string Info = "M21 12 A9 9 0 1 1 3 12 A9 9 0 1 1 21 12 Z M12 11 V16 M12 7.5 V8";
        public const string Target = "M21 12 A9 9 0 1 1 3 12 A9 9 0 1 1 21 12 Z M17 12 A5 5 0 1 1 7 12 A5 5 0 1 1 17 12 Z M12 12 V12.5";
        public const string Check = "M5 12 L10 17 L19 7";
        public const string Close = "M6 6 L18 18 M18 6 L6 18";
        public const string Scale = "M12 4 V19 M5 7 H19 M5 7 L2.5 13 H7.5 Z M19 7 L16.5 13 H21.5 Z M8 20 H16";
        public const string Star = "M12 3 L14.6 9 L21 9.5 L16 13.6 L17.6 20 L12 16.6 L6.4 20 L8 13.6 L3 9.5 L9.4 9 Z";
        public const string Clock = "M21 12 A9 9 0 1 1 3 12 A9 9 0 1 1 21 12 Z M12 7 V12 L15 14";
        public const string Chart = "M4 18 L9 12 L13 15 L20 6 M15 6 H20 V11";
        public const string ArrowRight = "M5 12 H19 M13 6 L19 12 L13 18";
        public const string ArrowUp = "M12 19 V5 M6 11 L12 5 L18 11";
        public const string ArrowDown = "M12 5 V19 M6 13 L12 19 L18 13";
        public const string Lock = "M6 11 H18 V20 H6 Z M8.5 11 V8 A3.5 3.5 0 0 1 15.5 8 V11";
        public const string Heart = "M12 20 C4 14 3 9 7 6 C9.5 4.5 11.5 6 12 7.5 C12.5 6 14.5 4.5 17 6 C21 9 20 14 12 20 Z";
        public const string Flame = "M12 21 C7 21 5 17 6 13 C7 10 9 9 9 5 C12 7 13 9 13 11 C14 10 15 9 15 7 C18 10 19 13 18 16 C17 19 15 21 12 21 Z";

        public const string Laptop = "M5 6 H19 V15 H5 Z M3 18 H21";
        public const string Monitor = "M3 4 H21 V15 H3 Z M9 19 H15 M12 15 V19";
        public const string Trophy = "M8 4 H16 V9 A4 4 0 0 1 8 9 Z M8 6 H5 C5 9 6 10 8 10 M16 6 H19 C19 9 18 10 16 10 M12 13 V17 M9 20 H15";
        public const string Book = "M4 5 C7 4 10 4 12 6 C14 4 17 4 20 5 V19 C17 18 14 18 12 20 C10 18 7 18 4 19 Z M12 6 V20";
        public const string Cloud = "M7 18 H17 A4 4 0 0 0 16 10 A5 5 0 0 0 6.5 11 A3.5 3.5 0 0 0 7 18 Z";
        public const string Rocket = "M12 3 C16 6 17 11 15 16 H9 C7 11 8 6 12 3 Z M12 9.5 V10 M9 16 L7 20 M15 16 L17 20 M12 16 V20";
        public const string Chair = "M7 3 H17 V12 H7 Z M5 12 H19 M8 12 V21 M16 12 V21";
        public const string Brush = "M4 20 L14 10 M14 10 L18 4 L20 6 L14 10 M4 20 L6 16";
        public const string Snowflake = "M12 3 V21 M4.5 7.5 L19.5 16.5 M19.5 7.5 L4.5 16.5";
        public const string Coffee = "M5 8 H16 V14 A4 4 0 0 1 12 18 H9 A4 4 0 0 1 5 14 Z M16 10 H18 A2 2 0 0 1 18 14 H16 M8 3 V5 M12 3 V5";

        public const string Sunrise = "M2 18 H22 M7 18 A5 5 0 0 1 17 18 M12 3 V8 M9.5 5.5 L12 3 L14.5 5.5 M4.5 11.5 L6 13 M19.5 11.5 L18 13";
        public const string Sun = "M7 12 A5 5 0 1 0 17 12 A5 5 0 1 0 7 12 Z M12 2 V4 M12 20 V22 M2 12 H4 M20 12 H22 M4.9 4.9 L6.3 6.3 M17.7 17.7 L19.1 19.1 M4.9 19.1 L6.3 17.7 M17.7 6.3 L19.1 4.9";
        public const string Sunset = "M2 18 H22 M7 18 A5 5 0 0 1 17 18 M12 3 V8 M9.5 5.5 L12 8 L14.5 5.5 M4.5 11.5 L6 13 M19.5 11.5 L18 13";
        public const string Bed = "M3 19 V6 M3 14 H21 V19 M3 11 H9 V14 M9 11 H18 A3 3 0 0 1 21 14";
        public const string Bug = "M7 14 A5 5 0 1 0 17 14 A5 5 0 1 0 7 14 Z M9.5 7.5 A2.5 2.5 0 1 0 14.5 7.5 A2.5 2.5 0 1 0 9.5 7.5 Z M12 10 V19 M7 12 L3 10 M7 15 H3 M7.5 18 L4 20 M17 12 L21 10 M17 15 H21 M16.5 18 L20 20 M10.5 5.5 L9 3 M13.5 5.5 L15 3";
        public const string Plug = "M9 2 V7 M15 2 V7 M6 7 H18 V11 A6 6 0 0 1 6 11 Z M12 17 V22";
        public const string Pen = "M4 20 L5 15 L16 4 L20 8 L9 19 Z M14 6 L18 10";
        public const string Note = "M9 17 V5 L19 3 V15 M9 17 A3 3 0 1 1 3 17 A3 3 0 1 1 9 17 Z M19 15 A3 3 0 1 1 13 15 A3 3 0 1 1 19 15 Z";
        public const string Chat = "M4 5 H20 V16 H10 L6 20 V16 H4 Z";
        public const string Grid = "M4 4 H20 V20 H4 Z M4 9.3 H20 M4 14.6 H20 M9.3 4 V20 M14.6 4 V20";
        public const string Menu = "M4 7 H20 M4 12 H20 M4 17 H20";

        public const string Code = "M8 7 L3 12 L8 17 M16 7 L21 12 L16 17";
        public const string Megaphone = "M3 10 V14 H7 L14 19 V5 L7 10 Z M18 9 C19.5 10.5 19.5 13.5 18 15";
        public const string Moon = "M20 14 A8 8 0 1 1 10 4 A6 6 0 0 0 20 14 Z";
        public const string Search = "M10.5 17 A6.5 6.5 0 1 0 10.5 4 A6.5 6.5 0 0 0 10.5 17 Z M15.5 15.5 L20 20";
        public const string Graduation = "M2 9 L12 4 L22 9 L12 14 Z M6 11 V16 C8 18 16 18 18 16 V11 M22 9 V14";
        public const string Bolt = "M13 2 L4 14 H11 L10 22 L20 9 H13 Z";
        public const string Dice = "M5 4 H19 A1 1 0 0 1 20 5 V19 A1 1 0 0 1 19 20 H5 A1 1 0 0 1 4 19 V5 A1 1 0 0 1 5 4 Z M8.5 8.5 V8.6 M15.5 8.5 V8.6 M12 12 V12.1 M8.5 15.5 V15.6 M15.5 15.5 V15.6";
        public const string ThumbUp = "M7 11 V20 H4 V11 Z M7 11 L11 4 C12.5 4 13.5 5 13 7 L12 10 H18 C19.5 10 20.5 11.3 20 12.8 L18.3 18.6 C18 19.4 17.3 20 16.4 20 H7";
        public const string ThumbDown = "M7 13 V4 H4 V13 Z M7 13 L11 20 C12.5 20 13.5 19 13 17 L12 14 H18 C19.5 14 20.5 12.7 20 11.2 L18.3 5.4 C18 4.6 17.3 4 16.4 4 H7";
    }
}
