using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>Art gigs: copy a little sprite pixel by pixel. Pick a color, click or drag over the canvas.</summary>
    public sealed class PixelMatch : Minigame
    {
        sealed class Sprite
        {
            public string Name;
            public string[] Rows;
            public string[] Colors;
        }

        static readonly Sprite[] Sprites =
        {
            new Sprite { Name = "Heart", Colors = new[] { "#E0643C", "#FFFFFF" }, Rows = new[] { "aa..aa", "abaaaa", "aaaaaa", ".aaaa.", "..aa..", "......" } },
            new Sprite { Name = "Potion", Colors = new[] { "#3A2317", "#6CC6D9", "#C9502D" }, Rows = new[] { "..cc..", "..aa..", ".abba.", "abbbba", "abbbba", ".aaaa." } },
            new Sprite { Name = "Slime", Colors = new[] { "#8FC77A", "#3A2317" }, Rows = new[] { "......", "..aa..", ".aaaa.", "abaaba", "aaaaaa", ".aaaa." } },
            new Sprite { Name = "Mushroom", Colors = new[] { "#C73A2E", "#FFFFFF", "#F2D3A2" }, Rows = new[] { ".aaaa.", "abaaba", "aaaaaa", "..cc..", "..cc..", ".cccc." } },
            new Sprite { Name = "Coin", Colors = new[] { "#F4B63F", "#FFFFFF" }, Rows = new[] { ".aaaa.", "aabaaa", "abaaaa", "aaaaaa", "aaaaaa", ".aaaa." } },
            new Sprite { Name = "Ghost", Colors = new[] { "#FFFFFF", "#3A2317" }, Rows = new[] { ".aaaa.", "aaaaaa", "abaaba", "aaaaaa", "aaaaaa", "a.aa.a" } },
            new Sprite { Name = "Tree", Colors = new[] { "#5E8C4A", "#7A3E26" }, Rows = new[] { "..aa..", ".aaaa.", "aaaaaa", ".aaaa.", "..bb..", "..bb.." } },
            new Sprite { Name = "Sword", Colors = new[] { "#CFEAE6", "#7A3E26", "#F4B63F" }, Rows = new[] { "..a...", "..a...", "..a...", ".ccc..", "..b...", "..b..." } },
            new Sprite { Name = "Cherries", Colors = new[] { "#C73A2E", "#5E8C4A" }, Rows = new[] { "....b.", "...b..", "..b.b.", ".b...b", "aa..aa", "aa..aa" } },
            new Sprite { Name = "Key", Colors = new[] { "#F4B63F" }, Rows = new[] { ".aa...", "a..a..", ".aa...", "..a...", "..aa..", "..a..." } },
        };

        const int Size = 6;
        Sprite _sprite;
        int[] _target;
        int[] _canvas;
        Color[] _colors;
        VisualElement[] _cells;
        VisualElement _palette;
        int _brush = 1;
        bool _painting;
        int _filled;

        public PixelMatch(MinigameSetup setup) : base(setup) { }

        public override string Name => "Pixel Match";
        public override string HowTo => "Copy the sprite on the left onto your canvas. Pick a color, then click or drag over the pixels. It's done the moment it matches.";
        public override float Duration => 38f + 4f * Setup.Tier + SkillTime;
        public override string Status => $"{Correct()}/{_filled} pixels right";

        public override float Score
        {
            get
            {
                if (_filled == 0) return 0f;
                float accuracy = Mathf.Clamp01((Correct() - 0.5f * Wrong()) / _filled);
                return Solved ? 0.85f + 0.15f * TimeBonus : 0.85f * accuracy;
            }
        }

        protected override void Build()
        {
            _sprite = Sprites[Rng.Next(Sprites.Length)];
            bool mirror = Rng.NextDouble() < 0.5;
            int colorCount = _sprite.Colors.Length;
            _colors = new Color[colorCount + 1];
            for (int i = 0; i < colorCount; i++) _colors[i + 1] = Palette.Hex(_sprite.Colors[i]);
            _target = new int[Size * Size];
            _canvas = new int[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    char ch = _sprite.Rows[y][mirror ? Size - 1 - x : x];
                    int color = ch == '.' ? 0 : ch - 'a' + 1;
                    // Beginners copy fewer colors: tier 0 merges the accent colors into the main one.
                    if (Setup.Tier == 0 && color > 2) color = 1;
                    _target[y * Size + x] = color;
                    if (color > 0) _filled++;
                }

            var layout = Ui.Row("pixel");
            Root.Add(layout);

            var reference = Ui.El("pixel__panel");
            reference.Add(Ui.Text("REFERENCE: " + _sprite.Name.ToUpperInvariant(), "pixel__label"));
            reference.Add(Grid(_target, 34f, false));
            layout.Add(reference);

            var canvasPanel = Ui.El("pixel__panel pixel__panel--canvas");
            canvasPanel.Add(Ui.Text("YOUR CANVAS", "pixel__label"));
            var canvas = Grid(_canvas, 58f, true);
            canvasPanel.Add(canvas);
            layout.Add(canvasPanel);

            var tools = Ui.El("pixel__tools");
            tools.Add(Ui.Text("COLORS", "pixel__label"));
            _palette = Ui.El("pixel__palette");
            int used = 0;
            foreach (int c in _target) used = Mathf.Max(used, c);
            for (int i = 1; i <= used; i++) _palette.Add(Swatch(i));
            _palette.Add(Swatch(0));
            tools.Add(_palette);
            tools.Add(Ui.Text("Right after the last pixel matches, you're done.", "pixel__hint"));
            layout.Add(tools);

            Root.RegisterCallback<PointerUpEvent>(e => _painting = false);
            Root.RegisterCallback<PointerLeaveEvent>(e => _painting = false);
            SelectBrush(1);
        }

        VisualElement Grid(int[] pixels, float cell, bool editable)
        {
            var grid = Ui.El("pixel__grid" + (editable ? " pixel__grid--canvas" : ""));
            if (editable) _cells = new VisualElement[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                var row = Ui.Row("pixel__row");
                for (int x = 0; x < Size; x++)
                {
                    int index = y * Size + x;
                    var element = Ui.El("pixel__cell");
                    element.style.width = cell;
                    element.style.height = cell;
                    Paint(element, pixels[index]);
                    if (editable)
                    {
                        _cells[index] = element;
                        element.RegisterCallback<PointerDownEvent>(e =>
                        {
                            _painting = true;
                            Set(index);
                        });
                        element.RegisterCallback<PointerEnterEvent>(e =>
                        {
                            if (_painting) Set(index);
                        });
                    }
                    row.Add(element);
                }
                grid.Add(row);
            }
            return grid;
        }

        VisualElement Swatch(int color)
        {
            var swatch = Ui.Clickable(() => SelectBrush(color), "pixel__swatch" + (color == 0 ? " pixel__swatch--eraser" : ""));
            if (color == 0) swatch.Add(new Icon(Icons.Close, 22f));
            else swatch.style.backgroundColor = _colors[color];
            swatch.userData = color;
            return swatch;
        }

        void SelectBrush(int color)
        {
            _brush = color;
            foreach (var child in _palette.Children())
                child.EnableInClassList("pixel__swatch--on", child.userData is int c && c == color);
        }

        void Set(int index)
        {
            if (IsOver) return;
            _canvas[index] = _brush;
            Paint(_cells[index], _brush);
            if (Wrong() == 0 && Correct() == _filled) Solved = true;
        }

        void Paint(VisualElement cell, int color)
        {
            cell.EnableInClassList("pixel__cell--empty", color == 0);
            cell.style.backgroundColor = color == 0 ? new StyleColor(StyleKeyword.Null) : new StyleColor(_colors[color]);
        }

        int Correct()
        {
            int count = 0;
            for (int i = 0; i < _target.Length; i++) if (_target[i] > 0 && _canvas[i] == _target[i]) count++;
            return count;
        }

        int Wrong()
        {
            int count = 0;
            for (int i = 0; i < _target.Length; i++) if (_canvas[i] > 0 && _canvas[i] != _target[i]) count++;
            return count;
        }

        /// <summary>Test helper: paints a fraction of the sprite correctly.</summary>
        public void PaintForTest(float fraction)
        {
            int goal = Mathf.RoundToInt(_filled * fraction), done = 0;
            for (int i = 0; i < _target.Length && done < goal; i++)
            {
                if (_target[i] == 0) continue;
                _brush = _target[i];
                Set(i);
                done++;
            }
        }
    }
}
