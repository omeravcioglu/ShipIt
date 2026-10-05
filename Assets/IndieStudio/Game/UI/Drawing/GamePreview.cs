using System;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// "Your game, so far": a little screenshot of the game you're building, drawn from its genre and art style,
    /// that fills in as features are built. Gray placeholder boxes and a missing-texture hero become real art;
    /// level layout, enemies, motion, a HUD, music and dialogue appear with their disciplines; remaining bugs
    /// show up as glitches until you polish them away. It turns progress bars into something you can see.
    /// </summary>
    public sealed class GamePreview : VisualElement
    {
        const float W = 320f, H = 180f; // the virtual canvas everything is drawn on

        enum Scene { Side, Grid, Story }

        sealed class Look
        {
            public Color SkyTop, SkyBottom, Far, Ground, GroundTop, Platform, Hero, Enemy, Accent, Detail;
            public Color[] Party;
        }

        static readonly Color Ink = Palette.Ink;
        static readonly Color Placeholder = Palette.Hex("#A8A29A");
        static readonly Color PlaceholderDark = Palette.Hex("#86807A");
        static readonly Color Missing = Palette.Hex("#E040C8");
        static readonly Color GlitchA = Palette.Hex("#E040C8");
        static readonly Color GlitchB = Palette.Hex("#40E0D8");

        readonly Label _title;
        Project _project;
        float _art, _design, _code, _audio, _writing;
        int _glitches;
        Vector2[] _platforms = new Vector2[0];
        Vector2[] _props = new Vector2[0];
        Vector2[] _glitchSpots = new Vector2[0];

        public GamePreview()
        {
            AddToClassList("game-preview");
            pickingMode = PickingMode.Ignore;
            _title = Ui.Display("", "game-preview__title");
            _title.pickingMode = PickingMode.Ignore;
            Add(_title);
            generateVisualContent += Draw;
        }

        /// <summary>Shows a project as it is right now (null shows an empty screen).</summary>
        public void Show(Project project)
        {
            _project = project;
            if (project != null)
            {
                _art = Progress(project, Skill.Art, 1f);
                _design = Progress(project, Skill.Design, 1f);
                _code = Progress(project, Skill.Programming, 1f);
                _audio = Progress(project, Skill.Audio, 0f);
                _writing = Progress(project, Skill.Writing, 0f);
                _glitches = project.BugsRemaining < 0.5 ? 0 : Mathf.Clamp(Mathf.CeilToInt((float)project.BugsRemaining / 12f), 1, 10);
                var rng = new System.Random(project.Id * 7919 + (int)project.Genre);
                _platforms = Enumerable.Range(0, 7).Select(i => new Vector2(30 + i * 40 + rng.Next(-8, 9), 70 + rng.Next(0, 60))).ToArray();
                _props = Enumerable.Range(0, 8).Select(i => new Vector2(18 + rng.Next(0, 284), 40 + rng.Next(0, 100))).ToArray();
                _glitchSpots = Enumerable.Range(0, 10).Select(i => new Vector2(rng.Next(6, 300), rng.Next(6, 168))).ToArray();
            }
            bool titled = project != null && (_art >= 0.85f || project.Status != ProjectStatus.InDevelopment);
            _title.text = project?.Title ?? "";
            _title.EnableInClassList("hidden", !titled);
            MarkDirtyRepaint();
        }

        /// <summary>How much of a discipline is built; a game without that kind of work counts as <paramref name="missing"/>.</summary>
        static float Progress(Project project, Skill discipline, float missing)
        {
            var stream = project.Stream(discipline);
            if (stream == null || stream.Required <= 0) return missing;
            return Mathf.Clamp01((float)(stream.Done / stream.Required));
        }

        // ------------------------------------------------------------------ drawing

        Vector2 _offset;
        float _scale;
        bool _pixel, _outline, _lowPoly, _soft;

        Vector2 P(float x, float y)
        {
            if (_pixel)
            {
                x = Mathf.Round(x / 4f) * 4f;
                y = Mathf.Round(y / 4f) * 4f;
            }
            return _offset + new Vector2(x, y) * _scale;
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 8f || rect.height < 8f) return;
            _scale = Mathf.Min(rect.width / W, rect.height / H);
            _offset = new Vector2((rect.width - W * _scale) / 2f, (rect.height - H * _scale) / 2f);
            var p = context.painter2D;
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;

            if (_project == null)
            {
                Box(p, 0, 0, W, H, Palette.Hex("#2B1A12"), 0);
                return;
            }

            var style = _project.ArtStyle;
            _pixel = style == ArtStyleId.PixelArt;
            _outline = style == ArtStyleId.Cartoon;
            _lowPoly = style == ArtStyleId.LowPoly || style == ArtStyleId.RetroPS1;
            _soft = style == ArtStyleId.HandPainted;
            var look = LookFor(_project.Genre, style);
            bool placeholder = _art < 0.25f;

            switch (SceneFor(_project.Genre))
            {
                case Scene.Grid: DrawGrid(p, look, placeholder); break;
                case Scene.Story: DrawStory(p, look, placeholder); break;
                default: DrawSide(p, look, placeholder); break;
            }

            if (_audio >= 0.25f) DrawNotes(p, look);
            if (_code >= 0.55f) DrawHud(p, look);
            DrawGlitches(p);
        }

        static Scene SceneFor(GenreId genre) =>
            genre == GenreId.Puzzle ? Scene.Grid : genre == GenreId.Narrative ? Scene.Story : Scene.Side;

        // A side view: platformers, roguelikes, horror, party games, cozy and simulation games.
        void DrawSide(Painter2D p, Look look, bool placeholder)
        {
            Sky(p, look, placeholder);
            var genre = _project.Genre;

            if (!placeholder)
            {
                float alpha = Mathf.Clamp01((_art - 0.25f) / 0.3f);
                var far = WithAlpha(look.Far, alpha);
                if (genre == GenreId.Roguelike) Bricks(p, far);
                else if (genre == GenreId.Simulation) Skyline(p, far);
                else Hills(p, far, genre == GenreId.Horror);
            }

            // Ground and level layout.
            Box(p, 0, 148, W, 32, placeholder ? PlaceholderDark : look.Ground, 0);
            Box(p, 0, 144, W, 6, placeholder ? Placeholder : look.GroundTop, 0);
            int platforms = Mathf.RoundToInt(2 + 5 * _design);
            for (int i = 0; i < platforms && i < _platforms.Length; i++)
            {
                var spot = _platforms[i];
                Box(p, spot.x - 16, spot.y, 34, 8, placeholder ? Placeholder : look.Platform, 2);
            }

            if (genre == GenreId.Cozy && !placeholder) House(p, look, 230, 96);
            if (genre == GenreId.Horror && !placeholder) Flashlight(p, look);

            // Things to collect and things to avoid.
            int props = _design >= 0.2f ? Mathf.RoundToInt(6 * _design) : 0;
            for (int i = 0; i < props && i < _props.Length; i++)
            {
                var spot = _platforms[i % Mathf.Max(1, platforms)];
                Coin(p, spot.x + 2, spot.y - 10, placeholder ? Placeholder : look.Accent);
            }
            int enemies = _design >= 0.35f ? 1 + Mathf.RoundToInt(2 * _design) : 0;
            for (int i = 0; i < enemies; i++)
                Critter(p, 150 + i * 46, 134, _art >= 0.5f ? look.Enemy : Placeholder, _art >= 0.6f);

            // The hero (four of them in a party game).
            if (genre == GenreId.Party)
                for (int i = 0; i < 4; i++) Hero(p, 40 + i * 28, 122, placeholder ? Placeholder : look.Party[i], placeholder);
            else
            {
                Hero(p, 56, 122, look.Hero, placeholder);
                if (_code >= 0.35f) JumpArc(p, look);
            }

            if (_writing >= 0.3f) Bubble(p, 96, 74, look);
        }

        // A puzzle board.
        void DrawGrid(Painter2D p, Look look, bool placeholder)
        {
            Sky(p, look, placeholder);
            int size = 4 + Mathf.RoundToInt(3 * _design);
            float cell = 120f / size;
            float left = 100, top = 24;
            Box(p, left - 6, top - 6, cell * size + 12, cell * size + 12, placeholder ? PlaceholderDark : look.Ground, 6);
            var colors = new[] { look.Hero, look.Enemy, look.Accent, look.Platform };
            var rng = new System.Random(_project.Id);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int pick = rng.Next(0, 5);
                    if (pick == 4) continue;
                    var color = placeholder ? (pick % 2 == 0 ? Placeholder : PlaceholderDark) : colors[pick];
                    Box(p, left + x * cell + 1.5f, top + y * cell + 1.5f, cell - 3, cell - 3, color, 3);
                }
            if (_code >= 0.35f)
            {
                // The cursor and a cleared line: the rules are working.
                Outline(p, left + cell, top + cell, cell * 2, cell, Palette.Paper, 2.5f);
            }
            if (_writing >= 0.3f) Bubble(p, 30, 40, look);
        }

        // A story scene: characters, a backdrop and the dialogue box.
        void DrawStory(Painter2D p, Look look, bool placeholder)
        {
            Sky(p, look, placeholder);
            if (!placeholder) Hills(p, WithAlpha(look.Far, Mathf.Clamp01((_art - 0.25f) / 0.3f)), false);
            Box(p, 0, 132, W, 48, placeholder ? PlaceholderDark : look.Ground, 0);
            Person(p, 90, 70, placeholder ? Placeholder : look.Hero, placeholder);
            if (_art >= 0.4f || _design >= 0.4f) Person(p, 220, 74, placeholder ? PlaceholderDark : look.Enemy, false);

            // The dialogue box grows with the writing.
            if (_writing > 0.05f)
            {
                Box(p, 16, 128, W - 32, 44, WithAlpha(Palette.Cream, 0.95f), 6);
                if (_outline || _lowPoly) Outline(p, 16, 128, W - 32, 44, Ink, 1.5f);
                int lines = 1 + Mathf.RoundToInt(2 * _writing);
                for (int i = 0; i < lines; i++)
                    Box(p, 28, 138 + i * 10, (W - 64) * (i == lines - 1 ? 0.55f : 1f), 4, WithAlpha(Ink, 0.6f), 2);
            }
            if (_design >= 0.5f)
            {
                // Choices.
                Box(p, 200, 100, 96, 12, look.Accent, 4);
                Box(p, 200, 114, 96, 12, WithAlpha(look.Accent, 0.6f), 4);
            }
        }

        // ------------------------------------------------------------------ pieces

        void Sky(Painter2D p, Look look, bool placeholder)
        {
            if (placeholder)
            {
                Box(p, 0, 0, W, H, Palette.Hex("#C9C3BC"), 0);
                return;
            }
            Box(p, 0, 0, W, H, look.SkyBottom, 0);
            Box(p, 0, 0, W, H * 0.45f, look.SkyTop, 0);
            if (_project.ArtStyle == ArtStyleId.RetroPS1)
                for (int i = 0; i < 40; i++) Box(p, (i * 37) % 320, (i * 23) % 80, 2, 2, WithAlpha(Palette.Paper, 0.25f), 0);
        }

        void Hills(Painter2D p, Color color, bool spiky)
        {
            p.fillColor = color;
            p.BeginPath();
            p.MoveTo(P(0, 150));
            if (_lowPoly || spiky)
            {
                for (int i = 0; i <= 8; i++) p.LineTo(P(i * 40, i % 2 == 0 ? 96 + (i * 13) % 20 : 70 + (i * 17) % 24));
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    float x0 = i * 80, x1 = x0 + 80;
                    p.BezierCurveTo(P(x0 + 20, 70 + (i % 2) * 14), P(x1 - 20, 70 + (i % 2) * 14), P(x1, 104));
                }
            }
            p.LineTo(P(W, 150));
            p.ClosePath();
            p.Fill();
            if (_soft)
            {
                p.fillColor = WithAlpha(Palette.Paper, 0.12f);
                p.BeginPath();
                p.Arc(P(80, 110), 40 * _scale, Angle.Degrees(0), Angle.Degrees(360));
                p.Fill();
            }
        }

        void Bricks(Painter2D p, Color color)
        {
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 9; x++)
                    Box(p, x * 38 + (y % 2) * 19 - 10, 20 + y * 20, 34, 16, color, 2);
        }

        void Skyline(Painter2D p, Color color)
        {
            for (int i = 0; i < 8; i++) Box(p, 10 + i * 40, 60 + (i * 29) % 50, 30, 100, color, 2);
        }

        void House(Painter2D p, Look look, float x, float y)
        {
            Box(p, x, y, 50, 48, look.Platform, 2);
            p.fillColor = look.Enemy;
            p.BeginPath();
            p.MoveTo(P(x - 6, y));
            p.LineTo(P(x + 25, y - 26));
            p.LineTo(P(x + 56, y));
            p.ClosePath();
            p.Fill();
            Box(p, x + 18, y + 22, 14, 26, look.Ground, 2);
            Box(p, x + 6, y + 10, 10, 10, look.Accent, 1);
        }

        void Flashlight(Painter2D p, Look look)
        {
            p.fillColor = WithAlpha(look.Accent, 0.22f);
            p.BeginPath();
            p.MoveTo(P(64, 118));
            p.LineTo(P(220, 90));
            p.LineTo(P(220, 150));
            p.ClosePath();
            p.Fill();
        }

        void Hero(Painter2D p, float x, float y, Color color, bool placeholder)
        {
            if (placeholder)
            {
                // The classic missing texture.
                for (int iy = 0; iy < 3; iy++)
                    for (int ix = 0; ix < 2; ix++)
                        Box(p, x + ix * 8, y + iy * 8, 8, 8, (ix + iy) % 2 == 0 ? Missing : Palette.Hex("#1A1A1A"), 0);
                return;
            }
            bool detailed = _art >= 0.6f;
            Box(p, x, y, 16, 22, color, detailed ? 6 : 2);
            if (_outline) Outline(p, x, y, 16, 22, Ink, 1.5f);
            if (detailed)
            {
                Box(p, x + 4, y + 5, 3, 4, Ink, 1);
                Box(p, x + 10, y + 5, 3, 4, Ink, 1);
                Box(p, x - 2, y - 3, 20, 5, Palette.Hex("#3A2317"), 2); // hair or hat
            }
        }

        void Person(Painter2D p, float x, float y, Color color, bool placeholder)
        {
            if (placeholder)
            {
                Box(p, x - 18, y, 36, 60, Missing, 4);
                return;
            }
            Disc(p, x, y + 10, 14, color);
            Box(p, x - 20, y + 26, 40, 40, color, 10);
            if (_art >= 0.6f)
            {
                Disc(p, x - 5, y + 8, 2, Ink);
                Disc(p, x + 5, y + 8, 2, Ink);
            }
            if (_outline) Outline(p, x - 20, y + 26, 40, 40, Ink, 1.5f);
        }

        void Critter(Painter2D p, float x, float y, Color color, bool detailed)
        {
            Box(p, x, y, 18, 12, color, 5);
            if (detailed)
            {
                Disc(p, x + 5, y + 4, 1.8f, Palette.Paper);
                Disc(p, x + 13, y + 4, 1.8f, Palette.Paper);
            }
            if (_outline) Outline(p, x, y, 18, 12, Ink, 1.2f);
        }

        void Coin(Painter2D p, float x, float y, Color color)
        {
            if (_pixel) Box(p, x, y, 8, 8, color, 0);
            else Disc(p, x + 4, y + 4, 4, color);
        }

        void JumpArc(Painter2D p, Look look)
        {
            p.strokeColor = WithAlpha(Palette.Paper, 0.75f);
            p.lineWidth = 1.6f * _scale;
            p.BeginPath();
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                var a = P(72 + t * 60, 120 - Mathf.Sin(t * Mathf.PI) * 46);
                if (i % 2 == 0) p.MoveTo(a);
                else p.LineTo(a);
            }
            p.Stroke();
            if (_code >= 0.75f)
                for (int i = 0; i < 5; i++) Disc(p, 64 + i * 6, 146 - (i % 2) * 4, 1.6f, look.Accent);
        }

        void Bubble(Painter2D p, float x, float y, Look look)
        {
            Box(p, x, y, 54, 28, Palette.Paper, 8);
            if (_outline || _lowPoly) Outline(p, x, y, 54, 28, Ink, 1.2f);
            p.fillColor = Palette.Paper;
            p.BeginPath();
            p.MoveTo(P(x + 10, y + 27));
            p.LineTo(P(x + 4, y + 38));
            p.LineTo(P(x + 20, y + 27));
            p.ClosePath();
            p.Fill();
            int lines = _writing >= 0.7f ? 2 : 1;
            for (int i = 0; i < lines; i++) Box(p, x + 8, y + 8 + i * 8, i == 0 ? 38 : 24, 3, WithAlpha(Ink, 0.55f), 1.5f);
        }

        void DrawNotes(Painter2D p, Look look)
        {
            int notes = 1 + Mathf.RoundToInt(3 * _audio);
            for (int i = 0; i < notes; i++)
            {
                float x = 250 + i * 16, y = 30 + (i % 2) * 14;
                Disc(p, x, y + 10, 3.2f, Palette.Paper);
                Box(p, x + 2, y - 2, 1.6f, 12, Palette.Paper, 0);
            }
        }

        void DrawHud(Painter2D p, Look look)
        {
            for (int i = 0; i < 3; i++) Disc(p, 12 + i * 11, 12, 4, Palette.Hex("#E0643C"));
            Box(p, 248, 8, 62, 8, WithAlpha(Ink, 0.45f), 4);
            Box(p, 250, 10, 40, 4, look.Accent, 2);
        }

        void DrawGlitches(Painter2D p)
        {
            for (int i = 0; i < _glitches && i < _glitchSpots.Length; i++)
            {
                var g = _glitchSpots[i];
                Box(p, g.x, g.y, 10 + (i % 3) * 6, 3, i % 2 == 0 ? GlitchA : GlitchB, 0);
                Box(p, g.x + 4, g.y + 4, 5, 2, i % 2 == 0 ? GlitchB : GlitchA, 0);
            }
        }

        // ------------------------------------------------------------------ primitives

        void Box(Painter2D p, float x, float y, float w, float h, Color fill, float radius)
        {
            if (fill.a <= 0.01f) return;
            if (_pixel || _lowPoly) radius = 0;
            var a = P(x, y);
            var b = P(x + w, y + h);
            p.fillColor = fill;
            p.BeginPath();
            float r = Mathf.Min(radius * _scale, Mathf.Min(b.x - a.x, b.y - a.y) / 2f);
            if (r < 0.5f)
            {
                p.MoveTo(a);
                p.LineTo(new Vector2(b.x, a.y));
                p.LineTo(b);
                p.LineTo(new Vector2(a.x, b.y));
            }
            else SvgPath.Trace(p, SvgPath.RoundRect(a.x, a.y, b.x - a.x, b.y - a.y, r), 1f, Vector2.zero);
            p.ClosePath();
            p.Fill();
        }

        void Outline(Painter2D p, float x, float y, float w, float h, Color color, float width)
        {
            var a = P(x, y);
            var b = P(x + w, y + h);
            p.strokeColor = color;
            p.lineWidth = width * _scale;
            p.BeginPath();
            p.MoveTo(a);
            p.LineTo(new Vector2(b.x, a.y));
            p.LineTo(b);
            p.LineTo(new Vector2(a.x, b.y));
            p.ClosePath();
            p.Stroke();
        }

        void Disc(Painter2D p, float x, float y, float radius, Color fill)
        {
            if (_pixel)
            {
                Box(p, x - radius, y - radius, radius * 2, radius * 2, fill, 0);
                return;
            }
            p.fillColor = fill;
            p.BeginPath();
            p.Arc(P(x, y), radius * _scale, Angle.Degrees(0), Angle.Degrees(360));
            p.ClosePath();
            p.Fill();
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        // ------------------------------------------------------------------ palettes

        static Look LookFor(GenreId genre, ArtStyleId style)
        {
            Look look;
            switch (genre)
            {
                case GenreId.Roguelike:
                    look = L("#2B2235", "#3E3350", "#4A3F5C", "#3A2E44", "#6B5B7A", "#6B5B7A", "#F4B63F", "#C73A2E", "#6CC6D9");
                    break;
                case GenreId.Horror:
                    look = L("#141020", "#2E2840", "#3D3550", "#2A2433", "#4A4258", "#4A4258", "#D9C9F0", "#8FC77A", "#F4CF4F");
                    break;
                case GenreId.Party:
                    look = L("#F7B98B", "#FDE7B8", "#F29A4A", "#E36FA0", "#F08C78", "#6F9BD1", "#E0643C", "#6B3FA0", "#F4B63F");
                    break;
                case GenreId.Cozy:
                    look = L("#FDD9A8", "#FFF4E2", "#9CCB86", "#8FBF6E", "#B9D3A5", "#C98A55", "#F08C78", "#C9502D", "#E36FA0");
                    break;
                case GenreId.Simulation:
                    look = L("#A9D8EC", "#E9F5F3", "#9FB7D6", "#7FA866", "#9CCB86", "#C9A2DB", "#F29A4A", "#6F9BD1", "#F4B63F");
                    break;
                case GenreId.Puzzle:
                    look = L("#A98BD8", "#D7C6F0", "#8E6BB0", "#5A4470", "#8E6BB0", "#4FB3A9", "#E0643C", "#6F9BD1", "#F4CF4F");
                    break;
                case GenreId.Narrative:
                    look = L("#3B2A5C", "#B07AA1", "#5A3F7A", "#2E2240", "#2E2240", "#C9A2DB", "#F7B98B", "#6CC6D9", "#F4B63F");
                    break;
                default: // platformer
                    look = L("#8ECDEB", "#CFEAF5", "#7FB069", "#8A5A35", "#5E8C4A", "#C98A55", "#E0643C", "#6B3FA0", "#F4B63F");
                    break;
            }
            look.Party = new[] { Palette.Hex("#E0643C"), Palette.Hex("#6F9BD1"), Palette.Hex("#8FC77A"), Palette.Hex("#F4CF4F") };
            if (style == ArtStyleId.Minimalist)
            {
                look.Far = Color.Lerp(look.Far, look.SkyBottom, 0.5f);
                look.Platform = look.Ground;
            }
            if (style == ArtStyleId.RetroPS1)
            {
                look.SkyTop = Color.Lerp(look.SkyTop, Palette.Hex("#1A1626"), 0.4f);
                look.SkyBottom = Color.Lerp(look.SkyBottom, Palette.Hex("#1A1626"), 0.4f);
            }
            return look;
        }

        static Look L(string skyTop, string skyBottom, string far, string ground, string groundTop, string platform, string hero, string enemy, string accent) =>
            new Look
            {
                SkyTop = Palette.Hex(skyTop), SkyBottom = Palette.Hex(skyBottom), Far = Palette.Hex(far), Ground = Palette.Hex(ground),
                GroundTop = Palette.Hex(groundTop), Platform = Palette.Hex(platform), Hero = Palette.Hex(hero), Enemy = Palette.Hex(enemy),
                Accent = Palette.Hex(accent), Detail = Palette.Hex(accent),
            };
    }
}
