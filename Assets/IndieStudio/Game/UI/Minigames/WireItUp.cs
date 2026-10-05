using System.Collections.Generic;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Development as a connection puzzle: turn tiles until the flow runs from its source to every module. Each
    /// module that lights up is a part of the feature coming together. The board is a random spanning tree, so
    /// a solution always exists (and any other arrangement that reaches everything counts too).
    /// The puzzle dresses up as the work it stands for (<see cref="WireTheme"/>): a circuit for code, paint
    /// pipes for art, patch cables for audio, paths through a level for design, red string on a corkboard for
    /// writing. Tiny projects bolt a few tiles in place as anchors; bigger ones have wobbly tiles that keep
    /// turning by themselves until the flow reaches them (never in relaxed mode).
    /// </summary>
    public sealed class WireItUp : Minigame
    {
        public const int North = 1, East = 2, South = 4, West = 8;
        static readonly int[] Directions = { North, East, South, West };
        /// <summary>Seconds between turns of a wobbly tile.</summary>
        public const float WobbleEvery = 2.6f;

        readonly WireTheme _theme;
        int _size;
        int _source;
        /// <summary>Each tile's connections at rotation 0 (what its drawing shows).</summary>
        int[] _base;
        /// <summary>Clockwise quarter turns applied; it only grows, so the animation always turns clockwise.</summary>
        int[] _turns;
        bool[] _powered;
        bool[] _locked;
        bool[] _wobbly;
        readonly List<int> _modules = new List<int>();
        WireTile[] _tiles;
        VisualElement _board;
        VisualElement _parts;
        Label _counter;
        VisualElement _counterFill;
        int _lit;
        float _wobbleTimer;

        public WireItUp(MinigameSetup setup) : base(setup)
        {
            _theme = WireTheme.For(setup.Skill, setup.Seed);
        }

        public override string Name => "Wire It Up";
        public override string HowTo => _theme.HowTo + (HasLocked ? " Bolted tiles are already right." : HasWobbly ? " Wobbly tiles keep turning until the flow reaches them." : "");
        public override float Duration => 45f + 15f * Mathf.Clamp(Setup.Tier, 0, 2) + SkillTime;
        public override float Score => _modules.Count == 0 ? 0f : 0.85f * _lit / _modules.Count + (_lit == _modules.Count ? 0.15f * TimeBonus : 0f);
        public override string Status => $"{_lit}/{_modules.Count} {_theme.Unit}";

        public int Size => _size;
        public int ModuleCount => _modules.Count;
        public int LitModules => _lit;
        public WireTheme Theme => _theme;
        public int LockedCount => Count(_locked);
        public int WobblyCount => Count(_wobbly);

        bool HasLocked => Setup.Tier <= 0;
        bool HasWobbly => Setup.Tier >= 1 && !Relaxed;

        protected override void Build()
        {
            _size = 4 + Mathf.Clamp(Setup.Tier, 0, 2);
            Generate();

            var layout = Ui.Row("wire wire--" + _theme.Look.ToString().ToLowerInvariant());
            Root.Add(layout);
            _board = Ui.El("wire__board");
            float tileSize = _size == 4 ? 92f : _size == 5 ? 78f : 66f;
            _tiles = new WireTile[_size * _size];
            for (int y = 0; y < _size; y++)
            {
                var row = Ui.Row("wire__row");
                for (int x = 0; x < _size; x++)
                {
                    int cell = y * _size + x;
                    var kind = cell == _source ? WireTile.Kind.Source : Degree(_base[cell]) == 1 ? WireTile.Kind.Module : WireTile.Kind.Cable;
                    var tile = new WireTile(_base[cell], kind, tileSize, _theme, _theme.TintFor(x, y), _locked[cell], _wobbly[cell]) { Turns = _turns[cell] };
                    if (cell != _source && !_locked[cell]) tile.RegisterCallback<PointerDownEvent>(e => Turn(cell));
                    _tiles[cell] = tile;
                    row.Add(tile);
                }
                _board.Add(row);
            }
            layout.Add(_board);

            var side = Ui.El("wire__side");
            side.Add(Ui.Text(_theme.Kicker, "wire__kicker"));
            var counterRow = Ui.Row("wire__counter");
            _counter = Ui.Display("", "wire__count");
            counterRow.Add(_counter);
            side.Add(counterRow);
            var bar = Ui.El("bar wire__bar");
            _counterFill = Ui.El("bar__fill " + Fmt.SkillClass(Setup.Skill));
            bar.Add(_counterFill);
            side.Add(bar);
            _parts = Ui.El("wire__parts");
            side.Add(_parts);
            side.Add(Ui.Text(_theme.Tip, "wire__tip"));
            layout.Add(side);

            UpdatePower();
        }

        void Generate()
        {
            int count = _size * _size;
            var solution = new int[count];
            var inTree = new bool[count];
            _source = Rng.Next(1, _size - 1) * _size + Rng.Next(1, _size - 1);
            inTree[_source] = true;

            // Randomized Prim: lots of short branches, so lots of modules to light.
            var frontier = new List<(int from, int to, int dir)>();
            void Grow(int cell)
            {
                foreach (int dir in Directions)
                {
                    int next = Neighbor(cell, dir);
                    if (next >= 0 && !inTree[next]) frontier.Add((cell, next, dir));
                }
            }
            Grow(_source);
            while (frontier.Count > 0)
            {
                int k = Rng.Next(frontier.Count);
                var edge = frontier[k];
                frontier[k] = frontier[frontier.Count - 1];
                frontier.RemoveAt(frontier.Count - 1);
                if (inTree[edge.to] || Degree(solution[edge.from]) >= 3) continue; // no four-way crossings: they can't be turned
                Connect(solution, edge.from, edge.to, edge.dir);
                inTree[edge.to] = true;
                Grow(edge.to);
            }
            // Rarely a cell is boxed in by full junctions: attach it anyway.
            for (bool changed = true; changed;)
            {
                changed = false;
                for (int cell = 0; cell < count; cell++)
                {
                    if (inTree[cell]) continue;
                    foreach (int dir in Directions)
                    {
                        int next = Neighbor(cell, dir);
                        if (next < 0 || !inTree[next]) continue;
                        Connect(solution, cell, next, dir);
                        inTree[cell] = changed = true;
                        break;
                    }
                }
            }

            _base = solution;
            _turns = new int[count];
            _powered = new bool[count];
            _locked = new bool[count];
            _wobbly = new bool[count];
            var cables = new List<int>();
            for (int cell = 0; cell < count; cell++)
            {
                if (cell == _source) continue;
                if (Degree(solution[cell]) == 1) _modules.Add(cell);
                else cables.Add(cell);
                if (Rng.NextDouble() < 0.8) _turns[cell] = Rng.Next(1, 4);
            }

            // Bolted tiles sit in their solved position; wobbly ones start anywhere and keep turning.
            int special = HasLocked ? 2 : HasWobbly ? 1 + Mathf.Clamp(Setup.Tier, 1, 2) : 0;
            for (int i = 0; i < special && cables.Count > 0; i++)
            {
                int pick = Rng.Next(cables.Count);
                int cell = cables[pick];
                cables.RemoveAt(pick);
                if (HasLocked)
                {
                    _locked[cell] = true;
                    _turns[cell] = 0;
                }
                else
                {
                    _wobbly[cell] = true;
                    if (_turns[cell] % 4 == 0) _turns[cell] = Rng.Next(1, 4);
                }
            }

            ComputePower();
            for (int guard = 0; CountLit() == _modules.Count && guard < 8; guard++)
            {
                int cell = _modules[Rng.Next(_modules.Count)];
                _turns[cell] += 1;
                ComputePower();
            }
        }

        void Connect(int[] masks, int a, int b, int dir)
        {
            masks[a] |= dir;
            masks[b] |= Opposite(dir);
        }

        int Neighbor(int cell, int dir)
        {
            int x = cell % _size, y = cell / _size;
            switch (dir)
            {
                case North: return y > 0 ? cell - _size : -1;
                case South: return y < _size - 1 ? cell + _size : -1;
                case East: return x < _size - 1 ? cell + 1 : -1;
                default: return x > 0 ? cell - 1 : -1;
            }
        }

        static int Opposite(int dir) => dir == North ? South : dir == South ? North : dir == East ? West : East;

        static int Degree(int mask)
        {
            int degree = 0;
            for (int m = mask; m != 0; m &= m - 1) degree++;
            return degree;
        }

        static int Count(bool[] flags)
        {
            int n = 0;
            if (flags != null) foreach (bool f in flags) if (f) n++;
            return n;
        }

        /// <summary>The connections of a mask after clockwise quarter turns (N → E → S → W).</summary>
        public static int Turned(int mask, int turns)
        {
            for (int i = 0; i < ((turns % 4) + 4) % 4; i++) mask = ((mask << 1) | (mask >> 3)) & 15;
            return mask;
        }

        int Current(int cell) => Turned(_base[cell], _turns[cell]);

        void ComputePower()
        {
            for (int i = 0; i < _powered.Length; i++) _powered[i] = false;
            var queue = new Queue<int>();
            queue.Enqueue(_source);
            _powered[_source] = true;
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                int mask = Current(cell);
                foreach (int dir in Directions)
                {
                    if ((mask & dir) == 0) continue;
                    int next = Neighbor(cell, dir);
                    if (next < 0 || _powered[next] || (Current(next) & Opposite(dir)) == 0) continue;
                    _powered[next] = true;
                    queue.Enqueue(next);
                }
            }
        }

        int CountLit()
        {
            int lit = 0;
            foreach (int cell in _modules) if (_powered[cell]) lit++;
            return lit;
        }

        void Turn(int cell)
        {
            if (IsOver || _tiles == null || _locked[cell]) return;
            _turns[cell]++;
            _tiles[cell].Turns = _turns[cell];
            UpdatePower();
        }

        protected override void Tick(float deltaTime)
        {
            if (!HasWobbly || _tiles == null) return;
            _wobbleTimer += deltaTime;
            if (_wobbleTimer < WobbleEvery) return;
            _wobbleTimer = 0f;
            bool turned = false;
            for (int cell = 0; cell < _wobbly.Length; cell++)
            {
                if (!_wobbly[cell] || _powered[cell]) continue; // the flow holds it still
                _turns[cell]++;
                _tiles[cell].Turns = _turns[cell];
                turned = true;
            }
            if (turned) UpdatePower();
        }

        /// <summary>
        /// Test helper (recordings): one click, like a player's. It turns the first tile (nearest the source) that isn't
        /// in its solved position yet. Returns false when every tile already is.
        /// </summary>
        public bool StepTowardSolutionForTest()
        {
            var order = new List<int> { _source };
            var seen = new bool[_base.Length];
            seen[_source] = true;
            for (int i = 0; i < order.Count; i++)
                foreach (int dir in Directions)
                {
                    int next = Neighbor(order[i], dir);
                    if (next < 0 || seen[next] || (_base[order[i]] & dir) == 0) continue;
                    seen[next] = true;
                    order.Add(next);
                }
            foreach (int cell in order)
            {
                if (_locked[cell] || Current(cell) == _base[cell]) continue;
                Turn(cell);
                return true;
            }
            return false;
        }

        /// <summary>Test helper: turns tiles back into the generated solution, nearest the source first (all of them by default).</summary>
        public void SolveForTest(float fraction = 1f)
        {
            var order = new List<int> { _source };
            var seen = new bool[_base.Length];
            seen[_source] = true;
            for (int i = 0; i < order.Count; i++)
                foreach (int dir in Directions)
                {
                    int next = Neighbor(order[i], dir);
                    if (next < 0 || seen[next] || (_base[order[i]] & dir) == 0) continue;
                    seen[next] = true;
                    order.Add(next);
                }
            int count = Mathf.RoundToInt(Mathf.Clamp01(fraction) * order.Count);
            for (int i = 0; i < count; i++)
            {
                int cell = order[i];
                _turns[cell] += (4 - _turns[cell] % 4) % 4;
                _tiles[cell].Turns = _turns[cell];
            }
            UpdatePower();
        }

        void UpdatePower()
        {
            ComputePower();
            for (int cell = 0; cell < _tiles.Length; cell++) _tiles[cell].Powered = _powered[cell];
            _lit = CountLit();

            _counter.text = $"{_lit}/{_modules.Count} {_theme.Unit}";
            Ui.SetFill(_counterFill, _modules.Count == 0 ? 0 : (double)_lit / _modules.Count);
            _parts.Clear();
            var names = Setup.Parts != null && Setup.Parts.Length > 0 ? Setup.Parts : new[] { "Module" };
            for (int i = 0; i < _modules.Count; i++)
            {
                string name = names[i % names.Length] + (i >= names.Length ? $" {i / names.Length + 1}" : "");
                bool on = i < _lit;
                var row = Ui.Row("wire__part" + (on ? " wire__part--on" : ""));
                var box = Ui.El("wire__part-box");
                if (on) box.Add(new Icon(Icons.Check, 12f, 3.2f));
                row.Add(box);
                row.Add(Ui.Text(name, "wire__part-name"));
                _parts.Add(row);
            }
            _board.EnableInClassList("wire__board--done", _lit == _modules.Count);
            if (_lit == _modules.Count) Solved = true;
        }
    }

    public enum WireLook { Circuit, Paint, Patch, Path, String }

    /// <summary>How a discipline's board looks and talks: the same puzzle, dressed as the work it stands for.</summary>
    public sealed class WireTheme
    {
        static readonly Color[] PaintColors = { Palette.Hex("#E36FA0"), Palette.Hex("#E0643C"), Palette.Hex("#F4B63F"), Palette.Hex("#8FC77A"), Palette.Hex("#A98BD8") };
        static readonly Color[] CableColors = { Palette.Hex("#E0643C"), Palette.Hex("#F4CF4F"), Palette.Hex("#6CC6D9"), Palette.Hex("#8FC77A"), Palette.Hex("#E36FA0") };

        public WireLook Look;
        public string HowTo;
        public string Tip;
        /// <summary>Small caps title over the list of parts, e.g. "MODULES".</summary>
        public string Kicker;
        /// <summary>What a lit module is, e.g. "online" in "3/6 online".</summary>
        public string Unit;
        /// <summary>Line width as a fraction of the tile.</summary>
        public float Width;
        public Color Off;
        public Color On;
        Color _paint;

        public static WireTheme For(Skill discipline, int seed)
        {
            switch (discipline)
            {
                case Skill.Art:
                    return new WireTheme
                    {
                        Look = WireLook.Paint, Kicker = "LAYERS", Unit = "painted", Width = 0.28f,
                        HowTo = "Click a pipe to turn it. Get paint flowing from the bucket to every canvas. Each canvas that fills is a part of the art coming together.",
                        Tip = "Tip: start at the bucket and follow the paint.",
                        Off = Palette.Hex("#DCE1E6"), _paint = PaintColors[((seed % PaintColors.Length) + PaintColors.Length) % PaintColors.Length],
                    };
                case Skill.Audio:
                    return new WireTheme
                    {
                        Look = WireLook.Patch, Kicker = "TRACKS", Unit = "playing", Width = 0.15f,
                        HowTo = "Click a tile to turn it. Patch the oscillator through to every speaker. Each speaker that plays is a part of the sound coming together.",
                        Tip = "Tip: start at the oscillator and follow the signal.",
                        Off = Palette.Hex("#5C6B70"),
                    };
                case Skill.Design:
                    return new WireTheme
                    {
                        Look = WireLook.Path, Kicker = "ROOMS", Unit = "reachable", Width = 0.32f,
                        HowTo = "Click a tile to turn it. Lay paths from the start flag to every star. Each star the player can reach is a part of the level coming together.",
                        Tip = "Tip: walk outward from the flag.",
                        Off = Palette.Hex("#D9C2A5"), On = Palette.Hex("#F4CF4F"),
                    };
                case Skill.Writing:
                    return new WireTheme
                    {
                        Look = WireLook.String, Kicker = "CLUES", Unit = "connected", Width = 0.05f,
                        HowTo = "Click a tile to turn it. Run red string from the photo to every note on the board. Each note that connects is a part of the story coming together.",
                        Tip = "Tip: start at the photo and follow the string.",
                        Off = Palette.Hex("#8E7A66"), On = Palette.Hex("#C73A2E"),
                    };
                default:
                    return new WireTheme
                    {
                        Look = WireLook.Circuit, Kicker = "MODULES", Unit = "online", Width = 0.18f,
                        HowTo = "Click a tile to turn it. Route power from the battery to every module. Each module that lights up is a part of the feature coming together.",
                        Tip = "Tip: start from the battery and follow the glow outward.",
                        Off = Palette.Hex("#A88A72"), On = Palette.Mustard,
                    };
            }
        }

        /// <summary>The lit color of the tile at (x, y): one paint per board, a different cable per tile.</summary>
        public Color TintFor(int x, int y)
        {
            switch (Look)
            {
                case WireLook.Paint: return _paint;
                case WireLook.Patch: return CableColors[(x * 3 + y * 2) % CableColors.Length];
                default: return On;
            }
        }
    }

    /// <summary>One tile of the board, drawn with Painter2D in its theme's look. Turning it animates through USS.</summary>
    public sealed class WireTile : VisualElement
    {
        public enum Kind { Cable, Source, Module }

        static readonly Color ModuleOff = Palette.Hex("#D8C3A8");
        static readonly Color ModuleOn = Palette.Hex("#4FB3A9");
        static readonly Color Glow = Palette.Hex("#F4B63F", 0.35f);
        static readonly Color Metal = Palette.Hex("#B9C2C6");
        static readonly Color Cork = Palette.Hex("#A8743F");

        readonly int _mask;
        readonly Kind _kind;
        readonly WireTheme _theme;
        readonly Color _tint;
        readonly bool _locked;
        readonly bool _wobbly;
        bool _powered;
        int _turns;

        public WireTile(int mask, Kind kind, float size, WireTheme theme, Color tint, bool locked, bool wobbly)
        {
            _mask = mask;
            _kind = kind;
            _theme = theme;
            _tint = tint;
            _locked = locked;
            _wobbly = wobbly;
            AddToClassList("wire-tile");
            if (kind == Kind.Source) AddToClassList("wire-tile--source");
            if (locked) AddToClassList("wire-tile--locked");
            if (wobbly) AddToClassList("wire-tile--wobbly");
            style.width = size;
            style.height = size;
            generateVisualContent += Draw;
        }

        public int Turns
        {
            get => _turns;
            set
            {
                _turns = value;
                style.rotate = new Rotate(Angle.Degrees(90f * value));
            }
        }

        public bool Powered
        {
            get => _powered;
            set
            {
                if (_powered == value) return;
                _powered = value;
                EnableInClassList("wire-tile--powered", value);
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            float s = Mathf.Min(rect.width, rect.height);
            if (s < 4f) return;
            var c = rect.center;
            var p = context.painter2D;
            p.lineJoin = LineJoin.Round;
            switch (_theme.Look)
            {
                case WireLook.Paint: DrawPaint(p, c, s); break;
                case WireLook.Patch: DrawPatch(p, c, s); break;
                case WireLook.Path: DrawPath(p, c, s); break;
                case WireLook.String: DrawString(p, c, s); break;
                default: DrawCircuit(p, c, s); break;
            }
            if (_locked) Bolts(p, c, s);
            if (_wobbly && !_powered) WobbleMarks(p, c, s);
        }

        // ------------------------------------------------------------------ the looks

        void DrawCircuit(Painter2D p, Vector2 c, float s)
        {
            float wire = s * _theme.Width;
            var color = _powered ? _theme.On : _theme.Off;
            p.lineCap = LineCap.Butt;
            if (_powered) Lines(p, c, s, wire + 12f, Glow);
            Lines(p, c, s, wire + 6f, Palette.Ink);
            Lines(p, c, s, wire, color);
            Disc(p, c, wire * 0.5f + 3f, Palette.Ink);
            Disc(p, c, wire * 0.5f, color);

            if (_kind == Kind.Source)
            {
                Disc(p, c, s * 0.3f, Palette.Ink);
                Disc(p, c, s * 0.3f - 3f, Palette.Mustard);
                IconShape(p, Icons.Bolt, c, s * 0.38f, Palette.Ink);
            }
            else if (_kind == Kind.Module)
            {
                float half = s * 0.22f;
                Box(p, c, half, 6f, _powered ? ModuleOn : ModuleOff, Palette.Ink);
                Disc(p, c, half * 0.35f, _powered ? Palette.Cream : _theme.Off);
            }
        }

        void DrawPaint(Painter2D p, Vector2 c, float s)
        {
            float pipe = s * _theme.Width;
            var color = _powered ? _tint : _theme.Off;
            p.lineCap = LineCap.Butt;
            Lines(p, c, s, pipe + 6f, Palette.Ink);
            Lines(p, c, s, pipe, color);
            Lines(p, c, s, pipe * 0.28f, Color.Lerp(color, Color.white, 0.45f)); // the shine on the pipe
            Disc(p, c, pipe * 0.5f + 3f, Palette.Ink);
            Disc(p, c, pipe * 0.5f, color);
            // Metal couplings where pipes meet the next tile.
            Ends(p, c, s, s / 2f - 7f, s / 2f + 3f, pipe + 8f, Palette.Ink);
            Ends(p, c, s, s / 2f - 5f, s / 2f + 3f, pipe + 2f, Palette.Hex("#8A939B"));

            if (_kind == Kind.Source)
            {
                Disc(p, c, s * 0.3f, Palette.Ink);
                Disc(p, c, s * 0.3f - 3f, _tint);
                IconStroke(p, Icons.Brush, c, s * 0.36f, Palette.Ink, 3f);
            }
            else if (_kind == Kind.Module)
            {
                float half = s * 0.23f;
                Box(p, c, half, 3f, Palette.Paper, Palette.Ink);
                if (_powered)
                {
                    Disc(p, c + new Vector2(-half * 0.2f, half * 0.1f), half * 0.5f, _tint);
                    Disc(p, c + new Vector2(half * 0.35f, -half * 0.3f), half * 0.25f, Color.Lerp(_tint, Palette.Mustard, 0.5f));
                }
                else Disc(p, c, half * 0.25f, ModuleOff);
            }
        }

        void DrawPatch(Painter2D p, Vector2 c, float s)
        {
            float cable = s * _theme.Width;
            var color = _powered ? _tint : Color.Lerp(_tint, _theme.Off, 0.65f);
            p.lineCap = LineCap.Round;
            if (_powered) Lines(p, c, s, cable + 10f, new Color(_tint.r, _tint.g, _tint.b, 0.3f));
            Lines(p, c, s, cable + 5f, Palette.Ink);
            Lines(p, c, s, cable, color);
            Disc(p, c, cable * 0.5f + 2.5f, Palette.Ink);
            Disc(p, c, cable * 0.5f, color);
            // Plugs at the jacks.
            p.lineCap = LineCap.Butt;
            Ends(p, c, s, s / 2f - 11f, s / 2f + 3f, cable + 8f, Palette.Ink);
            Ends(p, c, s, s / 2f - 8f, s / 2f + 3f, cable + 3f, Metal);

            if (_kind == Kind.Source)
            {
                Disc(p, c, s * 0.3f, Palette.Ink);
                Disc(p, c, s * 0.3f - 3f, Palette.Teal);
                p.lineCap = LineCap.Round;
                p.strokeColor = Palette.Cream;
                p.lineWidth = 3f;
                p.BeginPath();
                float w = s * 0.18f, h = s * 0.08f;
                for (int i = 0; i <= 16; i++)
                {
                    float t = i / 16f;
                    var point = new Vector2(c.x - w + 2 * w * t, c.y - Mathf.Sin(t * Mathf.PI * 2) * h);
                    if (i == 0) p.MoveTo(point);
                    else p.LineTo(point);
                }
                p.Stroke();
            }
            else if (_kind == Kind.Module)
            {
                Disc(p, c, s * 0.24f, Palette.Ink);
                Disc(p, c, s * 0.24f - 3f, Palette.Hex("#6E4A36"));
                Disc(p, c, s * 0.13f, _powered ? Palette.Mustard : Palette.Hex("#8E7A66"));
                Disc(p, c, s * 0.045f, Palette.Ink);
                if (_powered)
                {
                    p.lineCap = LineCap.Round;
                    p.strokeColor = Palette.Cream;
                    p.lineWidth = 2.5f;
                    for (int i = 1; i <= 2; i++)
                    {
                        p.BeginPath();
                        p.Arc(c, s * (0.26f + 0.07f * i), Angle.Degrees(-35f), Angle.Degrees(35f));
                        p.Stroke();
                    }
                }
            }
        }

        void DrawPath(Painter2D p, Vector2 c, float s)
        {
            float path = s * _theme.Width;
            var color = _powered ? _theme.On : _theme.Off;
            var edge = _powered ? Palette.Hex("#C9902C") : Palette.Hex("#B8997E");
            p.lineCap = LineCap.Butt;
            Lines(p, c, s, path + 6f, edge);
            Lines(p, c, s, path, color);
            Disc(p, c, path * 0.5f + 3f, edge);
            Disc(p, c, path * 0.5f, color);
            if (_powered && _kind == Kind.Cable)
            {
                // Footsteps along the way.
                Ends(p, c, s, s * 0.18f, s * 0.24f, path * 0.22f, Palette.Hex("#C9902C"));
            }

            if (_kind == Kind.Source)
            {
                p.lineCap = LineCap.Round;
                p.strokeColor = Palette.Ink;
                p.lineWidth = 4f;
                p.BeginPath();
                p.MoveTo(c + new Vector2(-s * 0.08f, s * 0.24f));
                p.LineTo(c + new Vector2(-s * 0.08f, -s * 0.26f));
                p.Stroke();
                p.BeginPath();
                p.MoveTo(c + new Vector2(-s * 0.08f, -s * 0.26f));
                p.LineTo(c + new Vector2(s * 0.22f, -s * 0.15f));
                p.LineTo(c + new Vector2(-s * 0.08f, -s * 0.04f));
                p.ClosePath();
                p.fillColor = Palette.Tomato;
                p.Fill();
                p.lineWidth = 3f;
                p.Stroke();
            }
            else if (_kind == Kind.Module)
            {
                float size = s * 0.5f;
                p.BeginPath();
                SvgPath.Trace(p, Icons.Star, size / 24f, c - new Vector2(size / 2f, size / 2f));
                p.fillColor = _powered ? Palette.Mustard : Palette.Hex("#CDBFA9");
                p.Fill();
                p.strokeColor = Palette.Ink;
                p.lineWidth = 3f;
                p.Stroke();
            }
        }

        void DrawString(Painter2D p, Vector2 c, float s)
        {
            // A few specks of cork.
            for (int i = 0; i < 5; i++)
            {
                float a = (i * 73 + _mask * 29) % 360 * Mathf.Deg2Rad;
                float r = s * (0.18f + 0.06f * (i % 3));
                Disc(p, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, 1.6f, Cork);
            }
            float width = Mathf.Max(3f, s * _theme.Width);
            var color = _powered ? _theme.On : _theme.Off;
            p.lineCap = LineCap.Round;
            Lines(p, c + new Vector2(1.5f, 2f), s, width, new Color(0f, 0f, 0f, 0.18f)); // its shadow on the board
            Lines(p, c, s, width, color);

            if (_kind == Kind.Source)
            {
                // A photo of the suspect, pinned up.
                var size = new Vector2(s * 0.42f, s * 0.5f);
                Rect(p, c - size / 2f, size, Palette.Paper, Palette.Ink, 2.5f);
                var photo = new Vector2(size.x - 8f, size.y * 0.66f);
                Rect(p, c - size / 2f + new Vector2(4f, 4f), photo, Palette.Hex("#5A3A28"), Palette.Hex("#5A3A28"), 0f);
                var face = c - size / 2f + new Vector2(4f + photo.x / 2f, 4f + photo.y * 0.42f);
                Disc(p, face, photo.x * 0.17f, Palette.Hex("#C9B49A"));
                Disc(p, face + new Vector2(0f, photo.x * 0.42f), photo.x * 0.3f, Palette.Hex("#C9B49A"));
                Pin(p, c + new Vector2(0f, -size.y / 2f + 2f), s, true);
            }
            else if (_kind == Kind.Module)
            {
                var size = new Vector2(s * 0.46f, s * 0.36f);
                var corner = c - size / 2f;
                Rect(p, corner, size, _powered ? Palette.Hex("#F4CF4F") : Palette.Paper, Palette.Ink, 2.5f);
                p.lineCap = LineCap.Round;
                p.strokeColor = Palette.Hex("#8E7A66");
                p.lineWidth = 2f;
                for (int i = 0; i < 2; i++)
                {
                    p.BeginPath();
                    p.MoveTo(corner + new Vector2(5f, size.y * (0.45f + 0.25f * i)));
                    p.LineTo(corner + new Vector2(size.x - (i == 1 ? 12f : 5f), size.y * (0.45f + 0.25f * i)));
                    p.Stroke();
                }
                Pin(p, c + new Vector2(0f, -size.y / 2f + 1f), s, _powered);
            }
            else Pin(p, c, s, _powered);
        }

        // ------------------------------------------------------------------ markings

        void Bolts(Painter2D p, Vector2 c, float s)
        {
            float d = s / 2f - 9f;
            foreach (var corner in new[] { new Vector2(-d, -d), new Vector2(d, -d), new Vector2(d, d), new Vector2(-d, d) })
            {
                Disc(p, c + corner, 5f, Palette.Ink);
                Disc(p, c + corner, 3.4f, Metal);
                p.lineCap = LineCap.Butt;
                p.strokeColor = Palette.Ink;
                p.lineWidth = 1.5f;
                p.BeginPath();
                p.MoveTo(c + corner + new Vector2(-2.4f, 2.4f));
                p.LineTo(c + corner + new Vector2(2.4f, -2.4f));
                p.Stroke();
            }
        }

        void WobbleMarks(Painter2D p, Vector2 c, float s)
        {
            float radius = s / 2f - 7f;
            p.lineCap = LineCap.Round;
            foreach (float start in new[] { 200f, 20f })
            {
                p.strokeColor = Palette.Tomato;
                p.lineWidth = 3f;
                p.BeginPath();
                p.Arc(c, radius, Angle.Degrees(start), Angle.Degrees(start + 50f));
                p.Stroke();
                // An arrowhead at the end of the arc, pointing clockwise.
                float end = (start + 50f) * Mathf.Deg2Rad;
                var tip = c + new Vector2(Mathf.Cos(end), Mathf.Sin(end)) * radius;
                var along = new Vector2(-Mathf.Sin(end), Mathf.Cos(end));
                var outward = new Vector2(Mathf.Cos(end), Mathf.Sin(end));
                p.BeginPath();
                p.MoveTo(tip + along * 5f);
                p.LineTo(tip - along * 2f + outward * 4.5f);
                p.LineTo(tip - along * 2f - outward * 4.5f);
                p.ClosePath();
                p.fillColor = Palette.Tomato;
                p.Fill();
            }
        }

        // ------------------------------------------------------------------ primitives

        void Lines(Painter2D p, Vector2 c, float s, float width, Color color)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.BeginPath();
            if ((_mask & WireItUp.North) != 0) { p.MoveTo(c); p.LineTo(new Vector2(c.x, c.y - s / 2f - 3f)); }
            if ((_mask & WireItUp.East) != 0) { p.MoveTo(c); p.LineTo(new Vector2(c.x + s / 2f + 3f, c.y)); }
            if ((_mask & WireItUp.South) != 0) { p.MoveTo(c); p.LineTo(new Vector2(c.x, c.y + s / 2f + 3f)); }
            if ((_mask & WireItUp.West) != 0) { p.MoveTo(c); p.LineTo(new Vector2(c.x - s / 2f - 3f, c.y)); }
            p.Stroke();
        }

        /// <summary>A short stretch of each connection, from <paramref name="from"/> to <paramref name="to"/> pixels out from the center.</summary>
        void Ends(Painter2D p, Vector2 c, float s, float from, float to, float width, Color color)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.BeginPath();
            if ((_mask & WireItUp.North) != 0) { p.MoveTo(new Vector2(c.x, c.y - from)); p.LineTo(new Vector2(c.x, c.y - to)); }
            if ((_mask & WireItUp.East) != 0) { p.MoveTo(new Vector2(c.x + from, c.y)); p.LineTo(new Vector2(c.x + to, c.y)); }
            if ((_mask & WireItUp.South) != 0) { p.MoveTo(new Vector2(c.x, c.y + from)); p.LineTo(new Vector2(c.x, c.y + to)); }
            if ((_mask & WireItUp.West) != 0) { p.MoveTo(new Vector2(c.x - from, c.y)); p.LineTo(new Vector2(c.x - to, c.y)); }
            p.Stroke();
        }

        static void Pin(Painter2D p, Vector2 at, float s, bool bright)
        {
            float r = Mathf.Max(4f, s * 0.08f);
            Disc(p, at + new Vector2(1.5f, 2f), r, new Color(0f, 0f, 0f, 0.2f));
            Disc(p, at, r, Palette.Ink);
            Disc(p, at, r - 2f, bright ? Palette.Tomato : Palette.Hex("#C9B49A"));
            Disc(p, at + new Vector2(-r * 0.3f, -r * 0.3f), r * 0.25f, Palette.Paper);
        }

        static void Box(Painter2D p, Vector2 c, float half, float radius, Color fill, Color stroke)
        {
            p.BeginPath();
            SvgPath.Trace(p, SvgPath.RoundRect(c.x - half, c.y - half, half * 2f, half * 2f, radius), 1f, Vector2.zero);
            p.fillColor = fill;
            p.Fill();
            p.strokeColor = stroke;
            p.lineWidth = 3f;
            p.Stroke();
        }

        static void Rect(Painter2D p, Vector2 corner, Vector2 size, Color fill, Color stroke, float width)
        {
            p.BeginPath();
            p.MoveTo(corner);
            p.LineTo(corner + new Vector2(size.x, 0f));
            p.LineTo(corner + size);
            p.LineTo(corner + new Vector2(0f, size.y));
            p.ClosePath();
            p.fillColor = fill;
            p.Fill();
            if (width <= 0f) return;
            p.strokeColor = stroke;
            p.lineWidth = width;
            p.Stroke();
        }

        static void IconShape(Painter2D p, string path, Vector2 c, float size, Color fill)
        {
            float scale = size / 24f;
            p.BeginPath();
            SvgPath.Trace(p, path, scale, c - new Vector2(12f, 12f) * scale);
            p.fillColor = fill;
            p.Fill();
        }

        static void IconStroke(Painter2D p, string path, Vector2 c, float size, Color color, float width)
        {
            float scale = size / 24f;
            p.lineCap = LineCap.Round;
            p.BeginPath();
            SvgPath.Trace(p, path, scale, c - new Vector2(12f, 12f) * scale);
            p.strokeColor = color;
            p.lineWidth = width;
            p.Stroke();
        }

        static void Disc(Painter2D p, Vector2 center, float radius, Color fill)
        {
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
            p.ClosePath();
            p.fillColor = fill;
            p.Fill();
        }
    }
}
