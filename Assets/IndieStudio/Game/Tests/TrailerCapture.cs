using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IndieStudio.Game.UI;
using IndieStudio.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace IndieStudio.Game.Tests
{
    /// <summary>
    /// Records the gameplay clips for the Steam trailer: plays a scripted first week (naming the studio, a gig,
    /// building features in every look of Wire It Up, the other minigames, coffee and the cat, a launch day) and
    /// pipes each scene at 1920×1080, 60 fps into ffmpeg. UI Toolkit runs on a virtual clock that advances exactly
    /// 1/60 s per recorded frame, so every frame is unique and motion is smooth however slow the capture is.
    /// Each clip also gets a list of sound cues (clicks, pops, chimes) that the trailer build mixes in.
    /// Only runs with INDIE_TRAILER=1 (ffmpeg from INDIE_FFMPEG or PATH); clips land in Logs/Trailer/clips.
    /// Marketing/Steam/trailer/build_trailer.py cuts them.
    /// </summary>
    public class TrailerCapture
    {
        const int Width = 1920, Height = 1080, Fps = 60;

        GameController _game;
        RenderTexture _texture;
        ClipRecorder _rec;

        [TearDown]
        public void TearDown()
        {
            _rec?.End();
            VirtualClock.Release();
            Time.captureFramerate = 0;
            if (_game != null) UnityEngine.Object.Destroy(_game.gameObject);
            if (_texture != null) _texture.Release();
        }

        [UnityTest]
        public IEnumerator RecordTrailerClips()
        {
            if (Environment.GetEnvironmentVariable("INDIE_TRAILER") != "1") Assert.Ignore("Set INDIE_TRAILER=1 to record trailer clips.");

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Trailer", "clips"));
            Directory.CreateDirectory(folder);
            foreach (var old in Directory.GetFiles(folder)) File.Delete(old);

            Assert.IsTrue(VirtualClock.Take(), "UI Toolkit's panel clock could not be replaced");
            Time.captureFramerate = Fps;
            _texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
            _game = GameBootstrap.Create(11, _texture);
            _rec = new ClipRecorder(_texture, folder);
            yield return _rec.Idle(0.3f);
            var ui = _game.Ui;
            var sim = _game.Sim;

            // --- 01: Doug asks for the studio's name; it goes up on the sign
            var nameField = ui.Root.Q<StudioNameField>();
            nameField.Value = "";
            yield return _rec.Idle(0.2f);
            _rec.Begin("01_name");
            yield return _rec.Hold(1.0f);
            foreach (var prefix in Prefixes("McHunter Studio"))
            {
                nameField.Value = prefix;
                _rec.Cue("type");
                yield return _rec.Hold(0.075f);
            }
            yield return _rec.Hold(0.9f);
            for (int i = 0; i < 3; i++) ui.Coach.Next();
            ui.Coach.Finish();
            _game.NotifyChanged();
            _rec.Cue("confirm");
            yield return _rec.Hold(3.2f);
            _rec.End();

            // --- 02: GigGrid pays the rent
            _rec.Begin("02_giggrid");
            ui.Show<GigsPage>();
            _game.NotifyChanged();
            _rec.Cue("page");
            yield return _rec.Hold(2.6f);
            var friend = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
            sim.AcceptGig(friend.Id, out _);
            _rec.Cue("click");
            ui.ShowHome();
            ui.Today.Toggle("gig");
            _game.NotifyChanged();
            yield return _rec.Hold(2.6f);
            _rec.End();

            // --- 03: a gig is a minigame: Bug Squash, one bug at a time
            _rec.Begin("03_bug_squash");
            ui.Play(PlanBlock.Gig(friend.Id));
            _rec.Cue("open");
            yield return _rec.Hold(1.2f);
            ui.Minigames.StartPlaying();
            _rec.Cue("click");
            var bugs = (BugSquash)ui.Minigames.Current;
            float squashUntil = _rec.Time + 5.0f;
            while (_rec.Time < squashUntil)
            {
                yield return _rec.Hold(0.32f);
                if (bugs.SquashOneForTest()) _rec.Cue("squash");
            }
            ui.Minigames.FinishNow();
            _rec.Cue("result");
            yield return _rec.Hold(2.8f);
            ui.Minigames.Close();
            _rec.End();

            // --- 04: pick an idea and start your game
            _rec.Begin("04_new_project");
            ui.Show<ProjectPage>();
            _game.NotifyChanged();
            _rec.Cue("page");
            yield return _rec.Hold(2.4f);
            var idea = sim.State.Ideas.First();
            Assert.IsNotNull(sim.StartProject(idea.Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out string error), error);
            _game.NotifyChanged();
            _rec.Cue("confirm");
            yield return _rec.Hold(2.6f);
            ui.ShowHome();
            _rec.End();

            // --- 05: building a feature is Wire It Up (code is a circuit), tile by tile
            _rec.Begin("05_wire_code");
            ui.Today.Toggle("build");
            ui.Play(PlanBlock.Build(Skill.Programming));
            _rec.Cue("open");
            yield return _rec.Hold(1.0f);
            ui.Minigames.StartPlaying();
            var wire = (WireItUp)ui.Minigames.Current;
            yield return _rec.Hold(0.6f);
            while (ui.Minigames.IsPlaying && !wire.IsOver && wire.StepTowardSolutionForTest())
            {
                _rec.Cue("tile");
                yield return _rec.Hold(0.22f);
            }
            _rec.Cue("solved");
            yield return _rec.Hold(3.6f);
            ui.Minigames.Close();
            _rec.End();

            // --- 06: every discipline has its own look (played for show; no slot is spent)
            int n = 0;
            foreach (var skill in new[] { Skill.Art, Skill.Audio, Skill.Design, Skill.Writing })
            {
                var board = Activities.For(sim, PlanBlock.Build(skill)) as WireItUp;
                if (board == null) continue;
                board.Setup.Seed += 97 * (n + 1);
                _rec.Begin($"06_wire_{++n}_{skill.ToString().ToLowerInvariant()}");
                ui.Minigames.Open(board, score => ShowReport(board, score, 0.2, 0.2 + 0.3 * score));
                ui.Minigames.StartPlaying();
                yield return _rec.Hold(0.4f);
                int turns = 0;
                while (turns < 14 && !board.IsOver && board.StepTowardSolutionForTest())
                {
                    _rec.Cue("tile");
                    turns++;
                    yield return _rec.Hold(0.16f);
                }
                yield return _rec.Hold(0.8f);
                ui.Minigames.Close();
                _rec.End();
            }

            // --- 07: the other gig minigames
            yield return PlayShow(ui, "07_pixel_match", new PixelMatch(GigSetup(Skill.Art, "Animate a character sprite sheet", "Copper Labs")), 4.6f,
                (g, t, step) => { if (t > 0.5f + 0.32f * step) { ((PixelMatch)g).PaintForTest(Mathf.Clamp01((step + 1) / 12f) * 0.92f); return "paint"; } return null; });
            yield return PlayShow(ui, "08_typo_hunt", new TypoHunt(GigSetup(Skill.Writing, "Proofread a quest log", "Bright Games")), 4.4f,
                (g, t, step) => { if (t > 0.7f + 0.6f * step) { ((TypoHunt)g).FixForTest(1); return "pop"; } return null; });
            yield return PlayShow(ui, "09_feedback_sort", new FeedbackSort(GigSetup(Skill.Design, "Sort playtest feedback", "Pixel Interactive")), 4.4f,
                (g, t, step) => { if (t > 0.7f + 0.55f * step) { ((FeedbackSort)g).SortForTest(1); return "card"; } return null; });
            yield return PlayShow(ui, "10_synth_tuner", new SynthTuner(GigSetup(Skill.Audio, "Tune menu sound effects", "Copper Labs")), 3.8f,
                (g, t, step) => { if (step == 0 && t > 1.4f) { ((SynthTuner)g).MatchForTest(); return "tune"; } return null; });

            // --- 11: the evening: a coffee and the cat
            _rec.Begin("11_coffee_cat");
            var home = ui.Page<HomePage>();
            ui.ShowHome();
            if (ui.Today.OpenGroup != "build") ui.Today.Toggle("build");
            _game.NotifyChanged();
            yield return _rec.Hold(1.3f);
            home.BrewCoffee();
            _game.NotifyChanged();
            _rec.Cue("coffee");
            yield return _rec.Hold(2.0f);
            sim.Founder.Morale = 50;
            home.PetCat();
            _game.NotifyChanged();
            _rec.Cue("purr");
            yield return _rec.Hold(3.2f);
            _rec.End();

            // --- 12: finishing a feature is a little party
            var finale = Activities.For(sim, PlanBlock.Build(Skill.Design)) as WireItUp;
            Assert.IsNotNull(finale);
            finale.Setup.Seed += 1234;
            _rec.Begin("12_feature_complete");
            ui.Minigames.Open(finale, score =>
            {
                var report = ShowReport(finale, score, 0.6, 1.0);
                report.Lines.Add($"Finished: {finale.Setup.Title} (Design)");
                report.Lines.Add("Flow: session 3 in a row, ×1.10.");
                return report;
            });
            ui.Minigames.StartPlaying();
            yield return _rec.Hold(0.5f);
            while (ui.Minigames.IsPlaying && !finale.IsOver && finale.StepTowardSolutionForTest())
            {
                _rec.Cue("tile");
                yield return _rec.Hold(0.15f);
            }
            if (ui.Minigames.IsPlaying) ui.Minigames.FinishNow();
            _rec.Cue("confetti");
            yield return _rec.Hold(4.2f);
            ui.Minigames.Close();
            _rec.End();

            // --- 13: night falls; sleep
            _rec.Begin("13_night");
            ui.ShowHome();
            ui.Play(PlanBlock.Rest);
            _game.NotifyChanged();
            _rec.Cue("night");
            yield return _rec.Hold(2.4f);
            ui.Sleep();
            _game.NotifyChanged();
            _rec.Cue("morning");
            yield return _rec.Hold(2.4f);
            _rec.End();

            // --- play on (not recorded) until the game ships, then record launch day
            sim.ApplyPlan(PlanTemplates.Balanced());
            bool launched = false;
            for (int day = 0; day < 260 && !launched; day++)
            {
                sim.AdvanceDay();
                foreach (var offer in sim.State.Freelance.Offers.OrderByDescending(g => g.EffectiveHourlyRate).ToList())
                    if (sim.State.Freelance.ActiveCount < 2) sim.AcceptGig(offer.Id, out _);
                foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);

                var project = sim.State.ActiveProject;
                if (project != null && !project.HasStorePage && project.Progress >= 0.3) sim.OpenStorePage(project.Id, StorePlatform.Vapor, out _);
                if (project != null && project.Status == ProjectStatus.ReadyToRelease && sim.Day - project.StorePageDay >= Tuning.VaporMinStorePageDays
                    && sim.Release(project.Id, 2.99, out _))
                {
                    launched = true;
                    sim.StartDay();
                    ui.ShowHome();
                    _game.NotifyChanged();
                    yield return _rec.Idle(0.2f);
                    _rec.Begin("14_launch_day");
                    ui.ShowLaunchDay(project);
                    _rec.Cue("launch");
                    for (int i = 0; i < 4; i++) _rec.CueAt("review", _rec.Time + 1.1f + 0.9f * i);
                    yield return _rec.Hold(9.0f);
                    ui.LaunchShow?.Close();
                    _rec.End();
                }
            }
            Assert.IsTrue(launched, "The scripted run should ship a game");

            // --- 15: the studio's pages after launch
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            _game.NotifyChanged();
            yield return _rec.Idle(0.1f);
            int pageClip = 0;
            foreach (var title in new[] { "Dashboard", "Bank" })
            {
                var page = ui.Pages.FirstOrDefault(p => p.Title == title);
                if (page == null) continue;
                _rec.Begin($"15_page_{++pageClip}_{title.ToLowerInvariant()}");
                ui.Show(page);
                _game.NotifyChanged();
                _rec.Cue("page");
                yield return _rec.Hold(3.0f);
                _rec.End();
            }
            _rec.Begin("16_reviews");
            ui.Show<ProjectPage>();
            _game.NotifyChanged();
            _rec.Cue("page");
            yield return _rec.Hold(0.1f);
            var body = ui.Root.Q<ScrollView>(className: "window__body");
            yield return _rec.Hold(1.0f);
            if (body != null)
                for (int i = 1; i <= 60; i++)
                {
                    body.scrollOffset = new Vector2(0f, 1200f * Mathf.SmoothStep(0f, 1f, i / 60f));
                    yield return _rec.Hold(1f / Fps);
                }
            yield return _rec.Hold(4.5f);
            _rec.End();
        }

        IEnumerator PlayShow(StudioUI ui, string clip, Minigame game, float seconds, Func<Minigame, float, int, string> play)
        {
            _rec.Begin(clip);
            ui.Minigames.Open(game, score => ShowReport(game, score, 0.25, 0.25 + 0.4 * score));
            ui.Minigames.StartPlaying();
            _rec.Cue("open");
            float start = _rec.Time;
            int step = 0;
            while (_rec.Time - start < seconds)
            {
                string cue = play(game, _rec.Time - start, step);
                if (cue != null)
                {
                    _rec.Cue(cue);
                    step++;
                }
                yield return _rec.Hold(1f / Fps);
            }
            ui.Minigames.Close();
            _rec.End();
        }

        static SlotReport ShowReport(Minigame game, double score, double before, double after) => new SlotReport
        {
            Slot = 2, Activity = PlanBlock.Build(game.Setup.Skill), Title = game.Setup.Title, Subject = game.Setup.Title,
            HasSkill = true, Skill = game.Setup.Skill, ProgressBefore = before, ProgressAfter = after, Performance = Rules.MinigameMultiplier(score),
        };

        static MinigameSetup GigSetup(Skill skill, string title, string client) => new MinigameSetup
        {
            Kicker = "AFTERNOON · GIG", Title = title, Subtitle = client + " · " + skill,
            Icon = Fmt.SkillIcon(skill), TileClass = Fmt.SkillClass(skill), Skill = skill, SkillLevel = 2, Tier = 1, Seed = 2026 + (int)skill,
        };

        static IEnumerable<string> Prefixes(string text)
        {
            for (int i = 1; i <= text.Length; i++) yield return text.Substring(0, i);
        }

        /// <summary>
        /// Replaces UI Toolkit's panel clock (Panel.TimeSinceStartup, internal) with one the recorder advances by
        /// exactly one frame per captured frame: schedulers, USS transitions and the minigames all read it.
        /// </summary>
        static class VirtualClock
        {
            static PropertyInfo _property;
            static FieldInfo _field;
            static double _ms;

            public static double Ms => _ms;

            public static bool Take()
            {
                var panel = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.Panel");
                var delegateType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.TimeMsFunction");
                if (panel == null || delegateType == null) return false;
                const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                _property = panel.GetProperty("TimeSinceStartup", flags);
                _field = _property == null ? panel.GetField("TimeSinceStartup", flags) : null;
                if (_property == null && _field == null) return false;
                _ms = Time.realtimeSinceStartupAsDouble * 1000.0;
                var hook = Delegate.CreateDelegate(delegateType, typeof(VirtualClock).GetMethod(nameof(Now), BindingFlags.Static | BindingFlags.NonPublic));
                Set(hook);
                return true;
            }

            public static void Advance(double seconds) => _ms += seconds * 1000.0;

            public static void Release()
            {
                if (_property != null || _field != null) Set(null);
                _property = null;
                _field = null;
            }

            static long Now() => (long)_ms;

            static void Set(object value)
            {
                if (_property != null) _property.SetValue(null, value);
                else _field?.SetValue(null, value);
            }
        }

        /// <summary>
        /// Streams the render texture into ffmpeg at 60 fps of virtual time: each recorded frame advances the clock by
        /// 1/60 s, so the video plays at real speed with no repeated frames. Cues are saved next to the clip as
        /// "seconds\tname" lines for the trailer's sound effects.
        /// </summary>
        sealed class ClipRecorder
        {
            readonly RenderTexture _source;
            readonly string _folder;
            readonly Texture2D _frame;
            readonly List<(double t, string cue)> _cues = new List<(double, string)>();
            System.Diagnostics.Process _ffmpeg;
            Stream _pipe;
            string _name;
            int _frames;

            public ClipRecorder(RenderTexture source, string folder)
            {
                _source = source;
                _folder = folder;
                _frame = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            }

            /// <summary>Seconds into the current clip.</summary>
            public float Time => _frames / (float)Fps;

            public void Begin(string name)
            {
                End();
                string exe = Environment.GetEnvironmentVariable("INDIE_FFMPEG");
                if (string.IsNullOrEmpty(exe)) exe = "ffmpeg";
                _name = name;
                _frames = 0;
                _cues.Clear();
                string output = Path.Combine(_folder, name + ".mp4");
                _ffmpeg = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = $"-y -loglevel error -f rawvideo -pix_fmt rgba -s {_source.width}x{_source.height} -r {Fps} -i - " +
                                    $"-vf vflip -c:v libx264 -preset fast -crf 10 -pix_fmt yuv420p \"{output}\"",
                        UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true,
                    },
                };
                _ffmpeg.Start();
                _pipe = _ffmpeg.StandardInput.BaseStream;
            }

            public void Cue(string cue) => _cues.Add((Time, cue));

            public void CueAt(string cue, float seconds) => _cues.Add((seconds, cue));

            /// <summary>Records frames for <paramref name="seconds"/> of game time (at least one frame).</summary>
            public IEnumerator Hold(float seconds)
            {
                int count = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
                for (int i = 0; i < count; i++)
                {
                    VirtualClock.Advance(1.0 / Fps);
                    yield return null;
                    Grab();
                }
            }

            /// <summary>Lets game time pass without recording (between clips).</summary>
            public IEnumerator Idle(float seconds)
            {
                int count = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
                for (int i = 0; i < count; i++)
                {
                    VirtualClock.Advance(1.0 / Fps);
                    yield return null;
                }
            }

            void Grab()
            {
                if (_pipe == null) return;
                var previous = RenderTexture.active;
                RenderTexture.active = _source;
                _frame.ReadPixels(new Rect(0, 0, _source.width, _source.height), 0, 0);
                RenderTexture.active = previous;
                var bytes = _frame.GetRawTextureData();
                _pipe.Write(bytes, 0, bytes.Length);
                _frames++;
            }

            public void End()
            {
                if (_ffmpeg == null) return;
                _pipe.Flush();
                _pipe.Close();
                _ffmpeg.WaitForExit(300000);
                _ffmpeg.Dispose();
                _ffmpeg = null;
                _pipe = null;
                var text = new StringBuilder();
                foreach (var (t, cue) in _cues.OrderBy(c => c.t))
                    text.Append(t.ToString("0.000", CultureInfo.InvariantCulture)).Append('\t').Append(cue).Append('\n');
                File.WriteAllText(Path.Combine(_folder, _name + ".cues.tsv"), text.ToString());
            }
        }
    }
}
