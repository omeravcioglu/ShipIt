using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    public enum Mood { Happy, Ecstatic, Focus, Tired, Panic }

    public enum HairStyle { Mess, Bun, Bob, Beanie, Bald }

    public enum Accessory { None, Glasses, Headphones }

    /// <summary>How a character looks. For <see cref="HairStyle.Beanie"/>, <see cref="Hair"/> is the hat color.</summary>
    public sealed class AvatarLook
    {
        public Color Skin;
        public Color Hair;
        public Color Shirt;
        public HairStyle Style;
        public Accessory Accessory;

        public AvatarLook(string skin, string hair, string shirt, HairStyle style, Accessory accessory = Accessory.None)
        {
            Skin = Palette.Hex(skin);
            Hair = Palette.Hex(hair);
            Shirt = Palette.Hex(shirt);
            Style = style;
            Accessory = accessory;
        }

        public static readonly AvatarLook Founder = new AvatarLook("#F4C197", "#6B3A22", "#E2603A", HairStyle.Mess);
        public static readonly AvatarLook FounderWorking = new AvatarLook("#F4C197", "#6B3A22", "#E2603A", HairStyle.Mess, Accessory.Headphones);
        public static readonly AvatarLook Mentor = new AvatarLook("#D4955F", "#CFC3B4", "#2E7F7A", HairStyle.Bald, Accessory.Glasses);
        public static readonly AvatarLook Landlord = new AvatarLook("#F4C197", "#CFC3B4", "#8A4A2B", HairStyle.Bald);
        public static readonly AvatarLook Parent = new AvatarLook("#F4C197", "#8C7A6B", "#A98BD8", HairStyle.Bob, Accessory.Glasses);
        public static readonly AvatarLook Friend = new AvatarLook("#D4955F", "#2B1A12", "#4FB3A9", HairStyle.Bun);
        public static readonly AvatarLook Colleague = new AvatarLook("#FBD9B8", "#C73A2E", "#3A2317", HairStyle.Bob, Accessory.Glasses);
        public static readonly AvatarLook CafeOwner = new AvatarLook("#9A5F3A", "#E0643C", "#F4B63F", HairStyle.Beanie);
        public static readonly AvatarLook Streamer = new AvatarLook("#F4C197", "#2B1A12", "#E36FA0", HairStyle.Bob, Accessory.Headphones);

        public static AvatarLook Client(ClientType client)
        {
            switch (client)
            {
                case ClientType.Friend: return Friend;
                case ClientType.SmallStudio: return new AvatarLook("#FBD9B8", "#7C4B9E", "#F4B63F", HairStyle.Beanie);
                case ClientType.Agency: return new AvatarLook("#D4955F", "#2B1A12", "#6F9BD1", HairStyle.Mess, Accessory.Glasses);
                case ClientType.Startup: return new AvatarLook("#F4C197", "#E0B04F", "#8FC77A", HairStyle.Bun, Accessory.Headphones);
                default: return new AvatarLook("#9A5F3A", "#1F120B", "#3A2317", HairStyle.Bald, Accessory.Glasses);
            }
        }
    }

    /// <summary>
    /// The cartoon character used for the founder and everyone who writes to you. Drawn with Painter2D on a
    /// 120×140 grid, from the same paths as the design mockups. Mood changes the eyes, mouth and sweat.
    /// </summary>
    public sealed class Avatar : VisualElement
    {
        const float GridW = 120f, GridH = 140f;

        static readonly Color Ink = Palette.Ink;
        static readonly Color White = Color.white;
        static readonly Color Cheek = Palette.Hex("#EE7F5E", 0.55f);
        static readonly Color MouthDark = Palette.Hex("#8E2F22");
        static readonly Color Tongue = Palette.Hex("#EE7F5E");
        static readonly Color Sweat = Palette.Hex("#8FD0E6");
        static readonly Color EyeBags = Palette.Hex("#9B6BA8");
        static readonly Color Pompom = Palette.Hex("#FFF4E2");

        const string Body = "M20 140 C20 106 38 95 60 95 C82 95 100 106 100 140 Z";
        const string Collar = "M50 96 L60 110 L70 96";
        const string BobBack = "M22 76 C14 38 36 16 60 16 C84 16 106 38 98 76 Z";
        const string MessFront = "M25 58 C18 28 40 14 60 17 C84 13 104 30 95 58 C91 46 84 40 77 43 C74 33 65 34 61 41 C55 31 45 34 43 43 C36 38 28 45 25 58 Z";
        const string MessTuft = "M58 18 C60 8 70 4 74 6 C68 10 66 14 66 19";
        const string BunFront = "M26 60 C23 34 40 22 60 22 C80 22 97 34 94 60 C89 45 76 36 60 36 C44 36 31 45 26 60 Z";
        const string BobFront = "M25 62 C22 34 40 21 60 21 C80 21 98 34 95 62 C88 50 80 40 66 38 C58 46 44 50 30 50 C28 54 26 58 25 62 Z";
        const string BeanieDome = "M25 52 C25 27 42 17 60 17 C78 17 95 27 95 52 Z";
        const string BaldShine = "M44 32 Q52 27 60 28";
        const string BaldSides = "M24 50 C22 44 26 38 30 38 M96 50 C98 44 94 38 90 38";
        const string EyesHappyArcs = "M41 62 Q47 53 53 62 M67 62 Q73 53 79 62";
        const string EyesTiredLids = "M41 59 L53 59 M67 59 L79 59";
        const string EyesTiredPupils = "M42 59 A5 5 0 0 0 52 59 Z M68 59 A5 5 0 0 0 78 59 Z";
        const string EyesTiredBags = "M41 67 Q47 70 53 67 M67 67 Q73 70 79 67";
        const string EyesFocusBrows = "M41 58 L53 61 M79 58 L67 61";
        const string MouthSmile = "M50 74 Q60 85 70 74";
        const string MouthBig = "M46 72 Q60 94 74 72 Z";
        const string MouthBigTongue = "M52 83 Q60 78 68 83 Q60 90 52 83 Z";
        const string MouthFlat = "M52 79 Q60 76 68 79";
        const string MouthFocus = "M51 78 Q60 81 69 76";
        const string MouthFocusTongue = "M63 79 Q66 86 70 80";
        const string SweatDrop = "M96 30 C96 30 89 40 89 45 A7 7 0 0 0 103 45 C103 40 96 30 96 30 Z";
        const string GlassesBridge = "M57 60 L63 60";
        const string HeadphoneBand = "M20 62 C18 16 102 16 100 62";

        static readonly string EarLeft = SvgPath.Circle(25, 62, 8);
        static readonly string EarRight = SvgPath.Circle(95, 62, 8);
        static readonly string Head = SvgPath.Circle(60, 58, 35);
        static readonly string CheekLeft = SvgPath.Ellipse(40, 72, 6.5f, 4);
        static readonly string CheekRight = SvgPath.Ellipse(80, 72, 6.5f, 4);
        static readonly string Bun = SvgPath.Circle(60, 16, 12);
        static readonly string BeanieBand = SvgPath.RoundRect(22, 44, 76, 14, 7);
        static readonly string BeaniePompom = SvgPath.Circle(60, 13, 8);
        static readonly string EyesOpen = SvgPath.Circle(47, 60, 4.8f) + " " + SvgPath.Circle(73, 60, 4.8f);
        static readonly string EyesOpenShine = SvgPath.Circle(48.6f, 58.4f, 1.6f) + " " + SvgPath.Circle(74.6f, 58.4f, 1.6f);
        static readonly string EyesPanicWhites = SvgPath.Circle(47, 59, 8) + " " + SvgPath.Circle(73, 59, 8);
        static readonly string EyesPanicPupils = SvgPath.Circle(47, 60, 2.8f) + " " + SvgPath.Circle(73, 60, 2.8f);
        static readonly string EyesFocusPupils = SvgPath.Circle(47, 63, 3.2f) + " " + SvgPath.Circle(73, 63, 3.2f);
        static readonly string MouthO = SvgPath.Ellipse(60, 80, 5.5f, 7);
        static readonly string Lenses = SvgPath.Circle(47, 60, 10) + " " + SvgPath.Circle(73, 60, 10);
        static readonly string CupLeft = SvgPath.RoundRect(12, 50, 16, 26, 7);
        static readonly string CupRight = SvgPath.RoundRect(92, 50, 16, 26, 7);

        AvatarLook _look;
        Mood _mood;
        float _scale;
        Vector2 _offset;

        public Avatar(AvatarLook look, Mood mood, float size)
        {
            _look = look;
            _mood = mood;
            AddToClassList("avatar");
            pickingMode = PickingMode.Ignore;
            style.width = size;
            style.height = Mathf.Round(size * GridH / GridW);
            generateVisualContent += Draw;
        }

        public Mood Mood
        {
            get => _mood;
            set
            {
                if (_mood == value) return;
                _mood = value;
                MarkDirtyRepaint();
            }
        }

        public AvatarLook Look
        {
            get => _look;
            set
            {
                if (_look == value) return;
                _look = value;
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f || _look == null) return;
            _scale = Mathf.Min(rect.width / GridW, rect.height / GridH);
            _offset = new Vector2((rect.width - GridW * _scale) / 2f, rect.height - GridH * _scale);
            var p = context.painter2D;
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;
            var look = _look;

            Shape(p, Body, look.Shirt, 3.5f);
            Stroke(p, Collar, Ink, 3f);
            if (look.Style == HairStyle.Bob) Shape(p, BobBack, look.Hair, 3.5f);
            Shape(p, EarLeft, look.Skin, 3.5f);
            Shape(p, EarRight, look.Skin, 3.5f);
            Shape(p, Head, look.Skin, 3.5f);
            Fill(p, CheekLeft, Cheek);
            Fill(p, CheekRight, Cheek);
            DrawHair(p, look);
            DrawFace(p);
            if (_mood == Mood.Tired || _mood == Mood.Panic) Shape(p, SweatDrop, Sweat, 2.5f);
            if (look.Accessory == Accessory.Glasses)
            {
                Shape(p, Lenses, new Color(1f, 1f, 1f, 0.25f), 3f);
                Stroke(p, GlassesBridge, Ink, 3f);
            }
            else if (look.Accessory == Accessory.Headphones)
            {
                Stroke(p, HeadphoneBand, Ink, 7f);
                Stroke(p, HeadphoneBand, look.Shirt, 3f);
                Shape(p, CupLeft, look.Shirt, 3.5f);
                Shape(p, CupRight, look.Shirt, 3.5f);
            }
        }

        void DrawHair(Painter2D p, AvatarLook look)
        {
            switch (look.Style)
            {
                case HairStyle.Mess:
                    Shape(p, MessFront, look.Hair, 3.5f);
                    Shape(p, MessTuft, look.Hair, 3.5f);
                    break;
                case HairStyle.Bun:
                    Shape(p, Bun, look.Hair, 3.5f);
                    Shape(p, BunFront, look.Hair, 3.5f);
                    break;
                case HairStyle.Bob:
                    Shape(p, BobFront, look.Hair, 3.5f);
                    break;
                case HairStyle.Beanie:
                    Shape(p, BeanieDome, look.Hair, 3.5f);
                    Shape(p, BeanieBand, look.Hair, 3.5f);
                    Shape(p, BeaniePompom, Pompom, 3.5f);
                    break;
                case HairStyle.Bald:
                    Stroke(p, BaldShine, new Color(1f, 1f, 1f, 0.7f), 4f);
                    Stroke(p, BaldSides, look.Hair, 6f);
                    break;
            }
        }

        void DrawFace(Painter2D p)
        {
            switch (_mood)
            {
                case Mood.Happy:
                    Fill(p, EyesOpen, Ink);
                    Fill(p, EyesOpenShine, White);
                    Stroke(p, MouthSmile, Ink, 3.5f);
                    break;
                case Mood.Ecstatic:
                    Stroke(p, EyesHappyArcs, Ink, 3.5f);
                    Shape(p, MouthBig, MouthDark, 3.5f);
                    Fill(p, MouthBigTongue, Tongue);
                    break;
                case Mood.Focus:
                    Stroke(p, EyesFocusBrows, Ink, 3.5f);
                    Fill(p, EyesFocusPupils, Ink);
                    Stroke(p, MouthFocus, Ink, 3.5f);
                    Shape(p, MouthFocusTongue, Tongue, 2.5f);
                    break;
                case Mood.Tired:
                    Stroke(p, EyesTiredLids, Ink, 3f);
                    Fill(p, EyesTiredPupils, Ink);
                    Stroke(p, EyesTiredBags, EyeBags, 2.5f);
                    Stroke(p, MouthFlat, Ink, 3.5f);
                    break;
                case Mood.Panic:
                    Shape(p, EyesPanicWhites, White, 3f);
                    Fill(p, EyesPanicPupils, Ink);
                    Shape(p, MouthO, MouthDark, 3f);
                    break;
            }
        }

        void Shape(Painter2D p, string path, Color fill, float stroke)
        {
            p.BeginPath();
            SvgPath.Trace(p, path, _scale, _offset);
            p.fillColor = fill;
            p.Fill();
            p.strokeColor = Ink;
            p.lineWidth = stroke * _scale;
            p.Stroke();
        }

        void Fill(Painter2D p, string path, Color fill)
        {
            p.BeginPath();
            SvgPath.Trace(p, path, _scale, _offset);
            p.fillColor = fill;
            p.Fill();
        }

        void Stroke(Painter2D p, string path, Color color, float width)
        {
            p.BeginPath();
            SvgPath.Trace(p, path, _scale, _offset);
            p.strokeColor = color;
            p.lineWidth = width * _scale;
            p.Stroke();
        }

        /// <summary>The founder's face follows the simulation: rest, burnout, money and morale.</summary>
        public static Mood FounderMood(GameState state)
        {
            var f = state.Founder;
            if (state.IsGameOver) return Mood.Panic;
            if (f.IsResting(state.Day)) return Mood.Tired;
            if (f.Burnout >= Tuning.CollapseBurnout || state.Finance.Cash < 0) return Mood.Panic;
            if (f.Burnout >= 55 || state.Finance.RunwayDays() < 30) return Mood.Tired;
            if (f.Morale >= 85) return Mood.Ecstatic;
            if (f.Morale >= 60) return Mood.Happy;
            return Mood.Focus;
        }
    }
}
