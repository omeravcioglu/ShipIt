using System;
using System.Collections.Generic;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>What a minigame stands for: which session of which work, and how hard it should be.</summary>
    public sealed class MinigameSetup
    {
        /// <summary>Small caps line above the title, e.g. "MORNING · BUILD".</summary>
        public string Kicker;
        public string Title;
        public string Subtitle;
        public string Icon;
        /// <summary>Background class for the header tile, e.g. "skill-programming".</summary>
        public string TileClass;
        public Skill Skill;
        public double SkillLevel;
        /// <summary>0 easy … 2 hard: the project's scope or how demanding the client is.</summary>
        public int Tier;
        public int Seed;
        /// <summary>Names of the parts being put together (Wire It Up modules).</summary>
        public string[] Parts = new string[0];
        /// <summary>True for polishing your own game (Bug Squash on your code rather than a client's).</summary>
        public bool OwnGame;
        /// <summary>
        /// Relaxed minigames (Design/18 §9): no timer, the game ends when it's solved or the player is done, and
        /// there is no speed bonus, so a perfect session scores 0.85 (×1.1) instead of up to 1.0 (×1.2).
        /// </summary>
        public bool Relaxed;
    }

    /// <summary>
    /// A short skill game that stands for one 4-hour session of work (Design/02 §4). Its score (0–1) becomes
    /// the session's work multiplier through <see cref="Rules.MinigameMultiplier"/>: playing well makes the
    /// session count for more, playing badly never makes it worthless.
    /// </summary>
    public abstract class Minigame
    {
        protected Minigame(MinigameSetup setup)
        {
            Setup = setup;
            Rng = new System.Random(setup.Seed);
            Root = Ui.El("mg");
        }

        public MinigameSetup Setup { get; }
        protected System.Random Rng { get; }
        public VisualElement Root { get; }

        public abstract string Name { get; }
        public abstract string HowTo { get; }
        public abstract float Duration { get; }
        /// <summary>The session's quality so far, 0–1.</summary>
        public abstract float Score { get; }
        /// <summary>A short live status for the timer bar, e.g. "4/6 modules online".</summary>
        public abstract string Status { get; }

        public float TimeLeft { get; private set; }
        /// <summary>Seconds played so far (relaxed games count up instead of down).</summary>
        public float Elapsed { get; private set; }
        public bool IsOver { get; private set; }
        public bool Relaxed => Setup.Relaxed;
        /// <summary>Set when the player solved everything before the buzzer.</summary>
        protected bool Solved { get; set; }
        /// <summary>The game ended because the player solved it (the host lets the finished board shine for a moment).</summary>
        public bool IsSolved => Solved;

        /// <summary>1 with 40% or more of the time left, falling to 0 at the buzzer; always 0 when relaxed.</summary>
        protected float TimeBonus => Relaxed ? 0f : Mathf.Clamp01(TimeLeft / (0.4f * Duration));

        /// <summary>A little extra time for skilled players: up to +10 s at skill 6.</summary>
        protected float SkillTime => 2f * Mathf.Clamp((float)Setup.SkillLevel - 1f, 0f, 5f);

        public void Begin()
        {
            TimeLeft = Duration;
            Build();
        }

        public void Advance(float deltaTime)
        {
            if (IsOver) return;
            Elapsed += deltaTime;
            if (!Relaxed) TimeLeft = Mathf.Max(0f, TimeLeft - deltaTime);
            Tick(deltaTime);
            if (Solved || (!Relaxed && TimeLeft <= 0f)) IsOver = true;
        }

        /// <summary>Ends the game now ("I'm done", tests).</summary>
        public void Stop() => IsOver = true;

        protected abstract void Build();
        protected virtual void Tick(float deltaTime) { }

        protected T Pick<T>(T[] items) => items[Rng.Next(items.Length)];

        protected static Button Clicker(VisualElement content, Action onPress, string classes)
        {
            var button = Ui.Clickable(onPress, classes);
            if (content != null) button.Add(content);
            return button;
        }
    }

    /// <summary>
    /// Runs a minigame in a full-screen overlay: intro (what to do), play (with a timer), and a result card
    /// that shows what the session actually achieved in the simulation. Building shows your game as it grows,
    /// and finishing a feature (or the whole game) gets a little celebration.
    /// </summary>
    public sealed class MinigameHost
    {
        readonly StudioUI _studio;
        readonly VisualElement _layer;
        Minigame _game;
        Func<double, SlotReport> _apply;
        VisualElement _panel;
        VisualElement _body;
        VisualElement _timerFill;
        Label _timeLabel;
        Label _status;
        IVisualElementScheduledItem _ticker;
        bool _playing;
        /// <summary>When the running game ended (real time), for the pause on a solved board.</summary>
        float _overAt;
        /// <summary>Seconds a solved game stays on screen (the lit board, the last bug squashed) before the result card.</summary>
        const float SolvedHold = 0.7f;

        public MinigameHost(StudioUI studio, VisualElement layer)
        {
            _studio = studio;
            _layer = layer;
        }

        public bool IsOpen => _game != null;
        public Minigame Current => _game;
        public bool IsPlaying => _playing;
        /// <summary>The last result, kept for tests and the day log.</summary>
        public SlotReport LastReport { get; private set; }

        public void Open(Minigame game, Func<double, SlotReport> apply)
        {
            Close(false);
            _game = game;
            _apply = apply;
            var setup = game.Setup;

            var backdrop = Ui.El("modal-backdrop mg-backdrop");
            _panel = Ui.El("mg-panel");
            backdrop.Add(_panel);

            var head = Ui.Row("mg-head");
            head.Add(Ui.Tile(setup.Icon, "tile--big mg-head__tile " + setup.TileClass, 30f));
            var words = Ui.El("grow");
            words.Add(Ui.Text(setup.Kicker, "mg-head__kicker"));
            words.Add(Ui.Display(setup.Title, "mg-head__title"));
            if (!string.IsNullOrEmpty(setup.Subtitle)) words.Add(Ui.Text(setup.Subtitle, "mg-head__sub"));
            head.Add(words);
            head.Add(Ui.Display(game.Name, "mg-head__name"));
            _panel.Add(head);

            _body = Ui.El("mg-body");
            _panel.Add(_body);
            ShowIntro();

            _layer.Add(backdrop);
            _layer.pickingMode = PickingMode.Position;
            _studio.Coach?.Refresh();
        }

        void ShowIntro()
        {
            _body.Clear();
            var intro = Ui.El("mg-intro");
            intro.Add(Ui.Display(_game.Name, "mg-intro__name"));
            intro.Add(Ui.Text(_game.HowTo, "mg-intro__howto"));
            intro.Add(Ui.Text(_game.Relaxed
                ? "No time limit (relaxed minigames are on): take as long as you like, then press Finish. Accuracy is what counts."
                : $"You have {_game.Duration:0} seconds. The better you play, the more this session gets done.", "mg-intro__time"));
            var start = Ui.IconBtn(Icons.Play, "Start", StartPlaying, "btn btn--primary btn--big mg-intro__start", 20f);
            start.name = "mg-start";
            intro.Add(start);
            _body.Add(intro);
        }

        public void StartPlaying()
        {
            if (_game == null || _playing) return;
            _playing = true;
            _body.Clear();
            var bar = Ui.Row("mg-timer" + (_game.Relaxed ? " mg-timer--relaxed" : ""));
            bar.Add(new Icon(Icons.Clock, 20f));
            var track = Ui.El("bar mg-timer__track");
            _timerFill = Ui.El("bar__fill fill-good");
            track.Add(_timerFill);
            bar.Add(track);
            if (_game.Relaxed) bar.Add(Ui.Text("No time limit", "mg-timer__relaxed"));
            _timeLabel = Ui.Display("", "mg-timer__time");
            bar.Add(_timeLabel);
            _status = Ui.Text("", "mg-timer__status");
            bar.Add(_status);
            if (_game.Relaxed) bar.Add(Ui.Grow());
            var done = Ui.Btn(_game.Relaxed ? "Finish" : "I'm done", () => _game?.Stop(),
                _game.Relaxed ? "btn btn--primary btn--small mg-timer__done" : "btn btn--small mg-timer__done");
            done.name = "mg-done";
            bar.Add(done);
            _body.Add(bar);

            _game.Begin();
            _body.Add(_game.Root);
            UpdateTimer();
            _ticker = _panel.schedule.Execute(Tick).Every(16);
        }

        void Tick(TimerState timer)
        {
            if (_game == null || !_playing) return;
            if (_game.IsOver)
            {
                if (!_game.IsSolved || timer.now / 1000f - _overAt >= SolvedHold) Finish();
                return;
            }
            _game.Advance(Mathf.Min(0.1f, timer.deltaTime / 1000f));
            UpdateTimer();
            if (!_game.IsOver) return;
            // A solved game shows what the player made for a moment; a game that ran out of time goes straight to the result.
            _overAt = timer.now / 1000f; // panel time, like every other UI animation
            if (!_game.IsSolved) Finish();
        }

        void UpdateTimer()
        {
            if (_game.Relaxed)
            {
                _timeLabel.text = "";
                _status.text = _game.Status;
                return;
            }
            float fraction = _game.Duration > 0 ? _game.TimeLeft / _game.Duration : 0;
            Ui.SetFill(_timerFill, fraction);
            _timerFill.EnableInClassList("fill-warn", fraction < 0.5f && fraction >= 0.2f);
            _timerFill.EnableInClassList("fill-bad", fraction < 0.2f);
            _timeLabel.text = Mathf.CeilToInt(_game.TimeLeft).ToString();
            _status.text = _game.Status;
        }

        /// <summary>Ends the running game and shows the result (also used by tests).</summary>
        public void FinishNow()
        {
            if (_game == null || (_game.IsOver && !_playing)) return; // already finished on its own
            if (!_playing) StartPlaying();
            _game.Stop();
            Finish();
        }

        void Finish()
        {
            if (!_playing) return;
            _playing = false;
            _ticker?.Pause();
            _ticker = null;
            double score = Mathf.Clamp01(_game.Score);
            var report = _apply(score);
            LastReport = report;
            ShowResult(score, report);
            _studio.Game.NotifyChanged();
        }

        void ShowResult(double score, SlotReport report)
        {
            _body.Clear();
            var project = _studio.Sim.State.ActiveProject;
            bool building = report != null && report.Activity.Kind == ActivityKind.Develop && project != null;
            var finished = building ? FinishedFeatures(report) : new List<string>();
            if (finished.Count > 0) _body.Add(Cheer(finished, project));
            var result = Ui.Row("mg-result");
            _body.Add(result);

            var verdict = Ui.El("mg-result__verdict");
            int stars = Stars(score);
            var starRow = Ui.Row("mg-stars");
            for (int i = 0; i < 3; i++)
            {
                var star = new Icon(Icons.Star, 46f, 2.6f) { Filled = i < stars };
                star.AddToClassList("mg-star");
                star.EnableInClassList("mg-star--on", i < stars);
                starRow.Add(star);
            }
            verdict.Add(starRow);
            verdict.Add(Ui.Display(Verdict(stars), "mg-result__title"));
            verdict.Add(Ui.Text($"{_game.Name}: {Mathf.RoundToInt((float)score * 100)}%", "mg-result__score"));
            double multiplier = Rules.MinigameMultiplier(score);
            var chip = Ui.Chip($"Work ×{multiplier:0.00}", multiplier >= 1 ? "chip--good mg-result__chip" : "chip--warn mg-result__chip");
            verdict.Add(chip);
            verdict.Add(Ui.Text(multiplier >= 1.1 ? "Careful work: more progress and better quality."
                : multiplier >= 1.05 && _game.Relaxed ? "Careful work. (Relaxed sessions top out at ×1.10.)"
                : multiplier >= 0.95 ? "An ordinary session."
                : "A rough session: less got done.", "mg-result__note"));
            if (building)
            {
                var look = Ui.El("mg-result__game");
                look.Add(Ui.Text("YOUR GAME, SO FAR", "mg-result__game-title"));
                var preview = new GamePreview { name = "mg-preview" };
                preview.AddToClassList("mg-result__preview");
                preview.Show(project);
                look.Add(preview);
                verdict.Add(look);
            }
            result.Add(verdict);

            var effect = Ui.El("mg-result__effect");
            if (report != null) effect.Add(SlotReportView.Build(report, true));
            else effect.Add(Ui.Text("Nothing happened.", "muted"));
            result.Add(effect);

            var buttons = Ui.Row("mg-result__buttons");
            var next = Ui.IconBtn(Icons.ArrowRight, "Continue", () => Close(true), "btn btn--primary btn--big", 20f);
            next.name = "mg-continue";
            buttons.Add(next);
            _body.Add(buttons);
        }

        /// <summary>The features a session finished, from its report ("Finished: Jump physics (Programming)").</summary>
        public static List<string> FinishedFeatures(SlotReport report)
        {
            var names = new List<string>();
            if (report == null) return names;
            foreach (var line in report.Lines.Where(l => l.StartsWith(SlotReport.FinishedPrefix)))
            {
                string name = line.Substring(SlotReport.FinishedPrefix.Length);
                int paren = name.LastIndexOf(" (", StringComparison.Ordinal);
                names.Add(paren > 0 && name.EndsWith(")") ? name.Substring(0, paren) : name);
            }
            return names;
        }

        VisualElement Cheer(List<string> finished, Project project)
        {
            bool complete = project != null && project.Status == ProjectStatus.ReadyToRelease;
            var banner = Ui.Row("mg-cheer" + (complete ? " mg-cheer--complete" : ""));
            banner.name = "mg-cheer";
            banner.Add(Ui.Tile(complete ? Icons.Trophy : Icons.Star, "mg-cheer__tile", 28f));
            var words = Ui.El("grow");
            words.Add(Ui.Display(complete ? "Content complete!" : finished.Count == 1 ? "Feature complete!" : $"{finished.Count} features complete!", "mg-cheer__title"));
            words.Add(Ui.Text(complete
                ? $"Every feature of {project.Title} is built. Polish it, give it a store page, and launch it."
                : string.Join("  ·  ", finished) + (project != null ? $"  ({project.FeaturesDone} of {project.FeatureCount} built)" : ""), "mg-cheer__text"));
            banner.Add(words);

            int seed = finished.Count * 97 + (project?.FeaturesDone ?? 0);
            _layer.schedule.Execute(() =>
            {
                if (_panel == null || _panel.panel == null) return;
                var bounds = _panel.worldBound;
                var origin = _layer.WorldToLocal(new Vector2(bounds.center.x, bounds.yMin + 160f));
                Celebrate.Confetti(_layer, origin, complete ? 64 : 30, seed);
            }).StartingIn(60);
            return banner;
        }

        public static int Stars(double score) => score >= Tuning.ThreeStarScore - 1e-6 ? 3 : score >= 0.6 ? 2 : score >= 0.3 ? 1 : 0;

        static string Verdict(int stars)
        {
            switch (stars)
            {
                case 3: return "Brilliant!";
                case 2: return "Solid work";
                case 1: return "Rough session";
                default: return "Ouch";
            }
        }

        public void Close(bool refresh = true)
        {
            _ticker?.Pause();
            _ticker = null;
            _playing = false;
            _game = null;
            _apply = null;
            _layer.Clear();
            _layer.pickingMode = PickingMode.Ignore;
            if (refresh) _studio.Game.NotifyChanged();
        }
    }

    /// <summary>A slot's results as a card body: the headline, a before→after bar and the details.</summary>
    public static class SlotReportView
    {
        public static VisualElement Build(SlotReport report, bool large)
        {
            var box = Ui.El("slot-report" + (large ? " slot-report--large" : ""));
            var head = Ui.Row("slot-report__head");
            head.Add(Ui.Tile(Fmt.ActivityIcon(report.Activity.Kind), "slot-report__tile " + (report.HasSkill ? Fmt.SkillClass(report.Skill) : Fmt.ActivityClass(report.Activity.Kind)), large ? 22f : 16f));
            var words = Ui.El("grow");
            words.Add(Ui.Text(GameCalendar.BlockNames[Mathf.Clamp(report.Slot, 0, 2)].ToUpperInvariant(), "slot-report__when"));
            words.Add(Ui.Display(report.Title, "slot-report__title"));
            head.Add(words);
            box.Add(head);

            if (report.HasProgress)
            {
                var line = Ui.Row("slot-report__progress");
                var bar = Ui.El("bar slot-report__bar");
                var before = Ui.El("bar__fill slot-report__before");
                var gain = Ui.El("bar__fill slot-report__gain " + (report.HasSkill ? Fmt.SkillClass(report.Skill) : "fill-good"));
                Ui.SetFill(before, report.ProgressBefore);
                gain.style.position = Position.Absolute;
                gain.style.left = Length.Percent((float)(report.ProgressBefore * 100));
                Ui.SetFill(gain, Math.Max(0.01, report.ProgressAfter - report.ProgressBefore));
                bar.Add(before);
                bar.Add(gain);
                line.Add(bar);
                line.Add(Ui.Text($"{Fmt.Pct(report.ProgressBefore)} → {Fmt.Pct(report.ProgressAfter)}", "slot-report__pct"));
                box.Add(line);
            }
            foreach (var text in report.Lines)
            {
                var row = Ui.Row("slot-report__line");
                row.Add(Ui.El("slot-report__dot"));
                row.Add(Ui.Text(text, "slot-report__text"));
                box.Add(row);
            }
            return box;
        }
    }

    /// <summary>A chunky horizontal slider drawn with plain elements (Synth Tuner knobs).</summary>
    public sealed class KnobSlider : VisualElement
    {
        readonly VisualElement _fill;
        readonly VisualElement _knob;
        readonly float _min, _max;
        float _value;

        public KnobSlider(float min, float max, float value)
        {
            _min = min;
            _max = max;
            AddToClassList("knob-slider");
            var track = Ui.El("knob-slider__track");
            track.pickingMode = PickingMode.Ignore;
            _fill = Ui.El("knob-slider__fill");
            _fill.pickingMode = PickingMode.Ignore;
            track.Add(_fill);
            Add(track);
            _knob = Ui.El("knob-slider__knob");
            _knob.pickingMode = PickingMode.Ignore;
            Add(_knob);
            RegisterCallback<PointerDownEvent>(e =>
            {
                this.CapturePointer(e.pointerId);
                SetFromPanel(e.position);
            });
            RegisterCallback<PointerMoveEvent>(e =>
            {
                if (this.HasPointerCapture(e.pointerId)) SetFromPanel(e.position);
            });
            RegisterCallback<PointerUpEvent>(e => this.ReleasePointer(e.pointerId));
            RegisterCallback<GeometryChangedEvent>(e => Layout());
            Value = value;
        }

        public event Action<float> Changed;

        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp(value, _min, _max);
                Layout();
            }
        }

        void SetFromPanel(Vector2 position)
        {
            float width = contentRect.width;
            if (width < 1f) return;
            float x = this.WorldToLocal(position).x;
            Value = _min + Mathf.Clamp01(x / width) * (_max - _min);
            Changed?.Invoke(_value);
        }

        void Layout()
        {
            float t = (_value - _min) / (_max - _min);
            _fill.style.width = Length.Percent(t * 100f);
            _knob.style.left = Length.Percent(t * 100f);
        }
    }
}
