using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Programming gigs and polishing your own game: bugs crawl out of the code, click them before they escape.
    /// Green sparkles are features. Don't squash those.
    /// </summary>
    public sealed class BugSquash : Minigame
    {
        static readonly string[] CodeLines =
        {
            "void Update() {", "  if (player.isGrounded && input.jump)", "    velocity.y = jumpForce;", "  velocity.y -= gravity * dt;",
            "  position += velocity * dt;", "}", "for (int i = 0; i <= enemies.Length; i++)", "  enemies[i].Think(player);",
            "if (health = 0) GameOver();", "score += combo * 10;", "SaveGame(slot, progress);", "var path = FindPath(start, goal);",
            "while (!level.IsLoaded) Wait();", "inventory.Add(item, count);", "audio.Play(\"jump.wav\");", "camera.Follow(player, 0.1f);",
            "if (timer > 3f) SpawnWave();", "return items.Where(i => i.rare);", "door.isOpen = !door.isLocked;", "particles.Emit(32);",
            "string name = save.ReadLine();", "menu.Show(Screen.Title);", "boss.health -= damage * crit;", "Debug.Log(\"why??\");",
        };

        sealed class Critter
        {
            public CritterView View;
            public bool IsBug;
            public float Age;
            public float Life;
            public Vector2 Position;
            public Vector2 Velocity;
            public bool Gone;
        }

        readonly List<Critter> _critters = new List<Critter>();
        VisualElement _field;
        float _spawnTimer = 0.4f;
        float _elapsed;
        int _squashed;
        int _escaped;
        int _featureHits;
        int _bugsSpawned;

        public BugSquash(MinigameSetup setup) : base(setup) { }

        public override string Name => "Bug Squash";
        public override string HowTo => Relaxed
            ? $"Bugs crawl around the {(Setup.OwnGame ? "game's" : "client's")} code. Click all {RelaxedBugs} of them. Green sparkles are features: leave them alone."
            : Setup.OwnGame
                ? "Bugs crawl out of your game's code. Click them before they escape! Green sparkles are features: leave them alone."
                : "Bugs crawl out of the client's code. Click them before they escape! Green sparkles are features: leave them alone.";
        public override float Duration => 25f + 0.5f * SkillTime;

        /// <summary>Relaxed games have a fixed number of bugs that never escape.</summary>
        int RelaxedBugs => 10 + 2 * Mathf.Clamp(Setup.Tier, 0, 2);

        public override float Score
        {
            get
            {
                if (Relaxed) return 0.85f * Mathf.Clamp01((float)_squashed / RelaxedBugs - 0.08f * _featureHits);
                int resolved = _squashed + _escaped;
                if (resolved == 0) return 0f;
                return Mathf.Clamp01((float)_squashed / resolved - 0.08f * _featureHits);
            }
        }
        public override string Status => (Relaxed ? $"{_squashed}/{RelaxedBugs} squashed" : $"{_squashed} squashed · {_escaped} escaped")
                                          + (_featureHits > 0 ? $" · {_featureHits} features hit" : "");

        public int Squashed => _squashed;

        float BugLife => Mathf.Max(1.2f, 2.3f - 0.3f * Setup.Tier + 0.08f * Mathf.Max(0f, (float)Setup.SkillLevel - 2f));

        protected override void Build()
        {
            var editor = Ui.El("bugs");
            Root.Add(editor);
            var tabs = Ui.Row("bugs__tabs");
            tabs.Add(Ui.Text(Setup.OwnGame ? "game.cs" : "client_project.cs", "bugs__tab bugs__tab--on"));
            tabs.Add(Ui.Text("utils.cs", "bugs__tab"));
            editor.Add(tabs);
            var code = Ui.El("bugs__code");
            editor.Add(code);
            int start = Rng.Next(CodeLines.Length);
            for (int i = 0; i < 12; i++)
            {
                var line = Ui.Row("bugs__line");
                line.Add(Ui.Text((i + 1).ToString(), "bugs__number"));
                line.Add(Ui.Text(CodeLines[(start + i * 5) % CodeLines.Length], "bugs__text"));
                code.Add(line);
            }
            _field = Ui.El("bugs__field");
            _field.pickingMode = PickingMode.Ignore;
            editor.Add(_field);
        }

        protected override void Tick(float dt)
        {
            _elapsed += dt;
            float width = _field.resolvedStyle.width, height = _field.resolvedStyle.height;
            if (float.IsNaN(width) || width < 50f) return;

            for (int i = _critters.Count - 1; i >= 0; i--)
            {
                var c = _critters[i];
                c.Age += dt;
                if (c.Gone)
                {
                    if (c.Age > 0.35f)
                    {
                        c.View.RemoveFromHierarchy();
                        _critters.RemoveAt(i);
                    }
                    continue;
                }
                c.Position += c.Velocity * dt;
                if (c.Position.x < 0 || c.Position.x > width - 48f) c.Velocity.x = -c.Velocity.x;
                if (c.Position.y < 0 || c.Position.y > height - 48f) c.Velocity.y = -c.Velocity.y;
                c.Position = new Vector2(Mathf.Clamp(c.Position.x, 0, width - 48f), Mathf.Clamp(c.Position.y, 0, height - 48f));
                c.View.style.left = c.Position.x;
                c.View.style.top = c.Position.y;
                c.View.Fade = Mathf.Clamp01((c.Life - c.Age) / 0.5f);
                if (c.Age >= c.Life)
                {
                    if (c.IsBug) _escaped++;
                    c.View.RemoveFromHierarchy();
                    _critters.RemoveAt(i);
                }
            }

            _spawnTimer -= dt;
            int alive = 0;
            foreach (var c in _critters) if (!c.Gone) alive++;
            if (_spawnTimer <= 0f && alive < 3 + Setup.Tier)
            {
                Spawn(width, height);
                float progress = Relaxed ? (float)_bugsSpawned / RelaxedBugs : _elapsed / Duration;
                float pace = Mathf.Lerp(0.95f, 0.6f, progress) - 0.08f * Setup.Tier;
                _spawnTimer = pace * (0.7f + 0.6f * (float)Rng.NextDouble());
            }
        }

        void Spawn(float width, float height)
        {
            bool bug = Rng.NextDouble() > 0.22;
            if (Relaxed && _bugsSpawned >= RelaxedBugs)
            {
                if (bug) return; // every bug is out: only the odd feature still wanders in
            }
            else if (bug) _bugsSpawned++;
            float angle = (float)(Rng.NextDouble() * Mathf.PI * 2);
            float speed = bug ? 25f + 25f * Setup.Tier + (float)Rng.NextDouble() * 20f : 10f;
            var critter = new Critter
            {
                IsBug = bug,
                Life = bug ? (Relaxed ? float.MaxValue : BugLife) : 1.7f,
                Position = new Vector2((float)Rng.NextDouble() * (width - 48f), (float)Rng.NextDouble() * (height - 48f)),
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
            };
            critter.View = new CritterView(bug);
            critter.View.style.left = critter.Position.x;
            critter.View.style.top = critter.Position.y;
            critter.View.RegisterCallback<PointerDownEvent>(e => Hit(critter));
            _field.Add(critter.View);
            _critters.Add(critter);
        }

        void Hit(Critter critter)
        {
            if (critter.Gone || IsOver) return;
            critter.Gone = true;
            critter.Age = 0f;
            if (critter.IsBug)
            {
                _squashed++;
                if (Relaxed && _squashed >= RelaxedBugs) Solved = true;
            }
            else _featureHits++;
            critter.View.Squash();
        }

        /// <summary>Test helper: squash every bug on screen.</summary>
        public void SquashAllForTest()
        {
            foreach (var c in _critters.ToArray())
                if (c.IsBug) Hit(c);
        }

        /// <summary>Test helper (recordings): squash the bug that has been out the longest, like a player would.</summary>
        public bool SquashOneForTest()
        {
            Critter oldest = null;
            foreach (var c in _critters)
                if (c.IsBug && !c.Gone && (oldest == null || c.Age > oldest.Age)) oldest = c;
            if (oldest == null) return false;
            Hit(oldest);
            return true;
        }
    }

    /// <summary>A bug (red beetle) or a feature (green sparkle), drawn with Painter2D.</summary>
    public sealed class CritterView : VisualElement
    {
        static readonly Color Shell = Palette.Hex("#E0643C");
        static readonly Color Feature = Palette.Hex("#8FC77A");
        static readonly Color Splat = Palette.Hex("#F4B63F", 0.55f);
        readonly bool _bug;
        bool _squashed;
        float _fade = 1f;

        public CritterView(bool bug)
        {
            _bug = bug;
            AddToClassList("critter");
            generateVisualContent += Draw;
        }

        public float Fade
        {
            set
            {
                if (Mathf.Abs(_fade - value) < 0.05f) return;
                _fade = value;
                style.opacity = 0.35f + 0.65f * value;
            }
        }

        public void Squash()
        {
            _squashed = true;
            style.opacity = 1f;
            AddToClassList(_bug ? "critter--squashed" : "critter--oops");
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 4f) return;
            var p = context.painter2D;
            var c = rect.center;
            float s = Mathf.Min(rect.width, rect.height);
            if (_squashed && _bug)
            {
                Blob(p, c, s * 0.42f, Splat);
                return;
            }
            if (!_bug)
            {
                float scale = s * 0.9f / 24f;
                p.BeginPath();
                SvgPath.Trace(p, Icons.Star, scale, c - new Vector2(12f, 12f) * scale);
                p.fillColor = _squashed ? Palette.Red : Feature;
                p.Fill();
                p.strokeColor = Palette.Ink;
                p.lineWidth = 3f;
                p.Stroke();
                return;
            }
            p.strokeColor = Palette.Ink;
            p.lineWidth = 3f;
            p.lineCap = LineCap.Round;
            p.BeginPath();
            for (int i = -1; i <= 1; i++)
            {
                float y = c.y + i * s * 0.16f;
                p.MoveTo(new Vector2(c.x - s * 0.18f, y));
                p.LineTo(new Vector2(c.x - s * 0.42f, y + i * s * 0.08f));
                p.MoveTo(new Vector2(c.x + s * 0.18f, y));
                p.LineTo(new Vector2(c.x + s * 0.42f, y + i * s * 0.08f));
            }
            p.MoveTo(new Vector2(c.x - s * 0.06f, c.y - s * 0.3f));
            p.LineTo(new Vector2(c.x - s * 0.16f, c.y - s * 0.46f));
            p.MoveTo(new Vector2(c.x + s * 0.06f, c.y - s * 0.3f));
            p.LineTo(new Vector2(c.x + s * 0.16f, c.y - s * 0.46f));
            p.Stroke();
            Blob(p, new Vector2(c.x, c.y - s * 0.24f), s * 0.11f, Palette.Ink);
            Blob(p, new Vector2(c.x, c.y + s * 0.04f), s * 0.25f, Shell);
            p.BeginPath();
            p.Arc(new Vector2(c.x, c.y + s * 0.04f), s * 0.25f, Angle.Degrees(0), Angle.Degrees(360));
            p.ClosePath();
            p.strokeColor = Palette.Ink;
            p.lineWidth = 3f;
            p.Stroke();
            p.BeginPath();
            p.MoveTo(new Vector2(c.x, c.y - s * 0.2f));
            p.LineTo(new Vector2(c.x, c.y + s * 0.28f));
            p.Stroke();
        }

        static void Blob(Painter2D p, Vector2 center, float radius, Color fill)
        {
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
            p.ClosePath();
            p.fillColor = fill;
            p.Fill();
        }
    }
}
