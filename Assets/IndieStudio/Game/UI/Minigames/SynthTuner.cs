using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>Audio gigs: match the client's reference sound. Turn the pitch and volume knobs until the waves line up.</summary>
    public sealed class SynthTuner : Minigame
    {
        const int Rounds = 3;
        readonly float[] _targetPitch = new float[Rounds];
        readonly float[] _targetVolume = new float[Rounds];
        readonly float[] _roundScores = new float[Rounds];
        int _round;
        WaveView _wave;
        KnobSlider _pitch;
        KnobSlider _volume;
        Label _roundLabel;
        Label _feedback;
        VisualElement _pips;

        public SynthTuner(MinigameSetup setup) : base(setup) { }

        public override string Name => "Synth Tuner";
        public override string HowTo => "Match the reference sound three times. Drag the pitch and volume knobs until your wave (teal) sits on the reference (dashed), then lock it in.";
        public override float Duration => 30f + 4f * Setup.Tier + SkillTime;
        public override string Status => $"Sound {Mathf.Min(_round + 1, Rounds)} of {Rounds}";

        public override float Score
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < Rounds; i++) sum += i < _round ? _roundScores[i] : i == _round ? 0.5f * Match() : 0f;
                float average = sum / Rounds;
                return 0.85f * average + (Solved ? 0.15f * TimeBonus : 0f);
            }
        }

        protected override void Build()
        {
            for (int i = 0; i < Rounds; i++)
            {
                _targetPitch[i] = 1f + 0.25f * Rng.Next(13);          // 1 … 4 cycles
                _targetVolume[i] = 0.35f + 0.05f * Rng.Next(13);      // 0.35 … 0.95
            }

            var layout = Ui.El("synth");
            Root.Add(layout);
            var top = Ui.Row("synth__top");
            _roundLabel = Ui.Display("", "synth__round");
            top.Add(_roundLabel);
            _pips = Ui.Row("synth__pips");
            for (int i = 0; i < Rounds; i++) _pips.Add(Ui.El("synth__pip"));
            top.Add(_pips);
            top.Add(Ui.Grow());
            _feedback = Ui.Text("", "synth__feedback");
            top.Add(_feedback);
            layout.Add(top);

            _wave = new WaveView();
            layout.Add(_wave);

            var knobs = Ui.Row("synth__knobs");
            knobs.Add(Knob("PITCH", 0.5f, 4.5f, 2.5f, out _pitch));
            knobs.Add(Knob("VOLUME", 0.2f, 1f, 0.6f, out _volume));
            var lockIn = Ui.IconBtn(Icons.Check, "Lock it in", LockIn, "btn btn--primary btn--big synth__lock", 20f);
            lockIn.name = "synth-lock";
            knobs.Add(lockIn);
            layout.Add(knobs);

            _pitch.Changed += v => Redraw();
            _volume.Changed += v => Redraw();
            Redraw();
        }

        VisualElement Knob(string label, float min, float max, float value, out KnobSlider slider)
        {
            var box = Ui.El("synth__knob");
            box.Add(Ui.Text(label, "synth__knob-label"));
            slider = new KnobSlider(min, max, value);
            box.Add(slider);
            return box;
        }

        /// <summary>How close the knobs are to this round's reference, 0–1. Harder clients are pickier.</summary>
        float Match()
        {
            if (_round >= Rounds || _pitch == null) return 0f;
            float pitchTolerance = 1.3f - 0.25f * Setup.Tier;
            float volumeTolerance = 0.5f - 0.08f * Setup.Tier;
            float error = Mathf.Abs(_pitch.Value - _targetPitch[_round]) / pitchTolerance + Mathf.Abs(_volume.Value - _targetVolume[_round]) / volumeTolerance;
            return Mathf.Clamp01(1f - error);
        }

        void Redraw()
        {
            int round = Mathf.Min(_round, Rounds - 1);
            _wave.Set(_targetPitch[round], _targetVolume[round], _pitch.Value, _volume.Value);
            _roundLabel.text = _round < Rounds ? $"Sound {_round + 1} of {Rounds}" : "All sounds matched";
            for (int i = 0; i < Rounds; i++)
            {
                var pip = _pips[i];
                pip.EnableInClassList("synth__pip--now", i == _round);
                pip.EnableInClassList("synth__pip--good", i < _round && _roundScores[i] >= 0.6f);
                pip.EnableInClassList("synth__pip--bad", i < _round && _roundScores[i] < 0.6f);
            }
        }

        public void LockIn()
        {
            if (IsOver || _round >= Rounds) return;
            float match = Match();
            _roundScores[_round] = match;
            _feedback.text = match >= 0.9f ? "Perfect!" : match >= 0.6f ? "Close enough" : "Off-key…";
            _feedback.EnableInClassList("good", match >= 0.6f);
            _feedback.EnableInClassList("bad", match < 0.6f);
            _round++;
            if (_round >= Rounds) Solved = true;
            else
            {
                _pitch.Value = 2.5f;
                _volume.Value = 0.6f;
            }
            Redraw();
        }

        /// <summary>Test helper: sets the knobs onto the reference.</summary>
        public void MatchForTest()
        {
            if (_round >= Rounds) return;
            _pitch.Value = _targetPitch[_round];
            _volume.Value = _targetVolume[_round];
            Redraw();
        }
    }

    /// <summary>The synth screen: the reference wave (dashed) and yours (teal).</summary>
    public sealed class WaveView : VisualElement
    {
        static readonly Color Grid = Palette.Hex("#FFF4E2", 0.12f);
        static readonly Color Reference = Palette.Hex("#F4B63F");
        static readonly Color Yours = Palette.Hex("#4FB3A9");
        float _targetPitch, _targetVolume, _pitch, _volume;

        public WaveView()
        {
            AddToClassList("wave");
            generateVisualContent += Draw;
        }

        public void Set(float targetPitch, float targetVolume, float pitch, float volume)
        {
            _targetPitch = targetPitch;
            _targetVolume = targetVolume;
            _pitch = pitch;
            _volume = volume;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 10f) return;
            var p = context.painter2D;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            p.strokeColor = Grid;
            p.lineWidth = 2f;
            p.BeginPath();
            for (int i = 1; i < 8; i++)
            {
                float x = rect.width * i / 8f;
                p.MoveTo(new Vector2(x, 8f));
                p.LineTo(new Vector2(x, rect.height - 8f));
            }
            p.MoveTo(new Vector2(8f, rect.height / 2f));
            p.LineTo(new Vector2(rect.width - 8f, rect.height / 2f));
            p.Stroke();

            Curve(p, rect, _targetPitch, _targetVolume, Reference, 6f, true);
            Curve(p, rect, _pitch, _volume, Yours, 4f, false);
        }

        static void Curve(Painter2D p, Rect rect, float pitch, float volume, Color color, float width, bool dashed)
        {
            const int steps = 160;
            float mid = rect.height / 2f, amplitude = (rect.height / 2f - 14f) * volume;
            p.strokeColor = color;
            p.lineWidth = width;
            p.BeginPath();
            bool pen = false;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                var point = new Vector2(12f + t * (rect.width - 24f), mid - Mathf.Sin(t * pitch * Mathf.PI * 2f) * amplitude);
                bool draw = !dashed || (i / 4) % 2 == 0;
                if (draw && pen) p.LineTo(point);
                else p.MoveTo(point);
                pen = draw;
            }
            p.Stroke();
        }
    }
}
