using System;
using System.Collections;
using System.IO;
using System.Linq;
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
    /// pipes each scene at 1920×1080, 30 fps into ffmpeg. Only runs with INDIE_TRAILER=1 (ffmpeg from
    /// INDIE_FFMPEG or PATH); clips land in Logs/Trailer/clips. Marketing/Steam/trailer/build_trailer.sh cuts them.
    /// </summary>
    public class TrailerCapture
    {
        const int Width = 1920, Height = 1080;

        GameController _game;
        RenderTexture _texture;
        ClipRecorder _rec;

        [TearDown]
        public void TearDown()
        {
            _rec?.End();
            if (_game != null) UnityEngine.Object.Destroy(_game.gameObject);
            if (_texture != null) _texture.Release();
        }

        [UnityTest]
        public IEnumerator RecordTrailerClips()
        {
            if (Environment.GetEnvironmentVariable("INDIE_TRAILER") != "1") Assert.Ignore("Set INDIE_TRAILER=1 to record trailer clips.");

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Trailer", "clips"));
            Directory.CreateDirectory(folder);
            foreach (var old in Directory.GetFiles(folder, "*.mp4")) File.Delete(old);

            _texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
            _game = GameBootstrap.Create(11, _texture);
            _rec = new ClipRecorder(_texture, folder);
            yield return Frames(10);
            var ui = _game.Ui;
            var sim = _game.Sim;

            // --- 01: Doug asks for the studio's name; it goes up on the sign
            var nameField = ui.Root.Q<StudioNameField>();
            nameField.Value = "";
            _rec.Begin("01_name");
            yield return _rec.Hold(1.2f);
            foreach (var prefix in Prefixes("McHunter Studio"))
            {
                nameField.Value = prefix;
                yield return _rec.Hold(0.07f);
            }
            yield return _rec.Hold(1.0f);
            for (int i = 0; i < 3; i++) ui.Coach.Next();
            ui.Coach.Finish();
            _game.NotifyChanged();
            yield return _rec.Hold(2.4f);
            _rec.End();

            // --- 02: GigGrid pays the rent
            _rec.Begin("02_giggrid");
            ui.Show<GigsPage>();
            _game.NotifyChanged();
            yield return _rec.Hold(2.6f);
            var friend = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
            sim.AcceptGig(friend.Id, out _);
            ui.ShowHome();
            ui.Today.Toggle("gig");
            _game.NotifyChanged();
            yield return _rec.Hold(1.8f);
            _rec.End();

            // --- 03: a gig is a minigame: Bug Squash
            _rec.Begin("03_bug_squash");
            ui.Play(PlanBlock.Gig(friend.Id));
            yield return _rec.Hold(1.4f);
            ui.Minigames.StartPlaying();
            var bugs = (BugSquash)ui.Minigames.Current;
            for (float t = 0f; t < 5.5f; t += 0.75f)
            {
                yield return _rec.Hold(0.75f);
                bugs.SquashAllForTest();
            }
            ui.Minigames.FinishNow();
            yield return _rec.Hold(2.4f);
            ui.Minigames.Close();
            _rec.End();

            // --- 04: pick an idea and start your game
            _rec.Begin("04_new_project");
            ui.Show<ProjectPage>();
            _game.NotifyChanged();
            yield return _rec.Hold(2.4f);
            var idea = sim.State.Ideas.First();
            Assert.IsNotNull(sim.StartProject(idea.Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out string error), error);
            _game.NotifyChanged();
            yield return _rec.Hold(2.2f);
            ui.ShowHome();
            _rec.End();

            // --- 05: building a feature is Wire It Up (code is a circuit)
            _rec.Begin("05_wire_code");
            ui.Today.Toggle("build");
            ui.Play(PlanBlock.Build(Skill.Programming));
            yield return _rec.Hold(1.0f);
            ui.Minigames.StartPlaying();
            var wire = (WireItUp)ui.Minigames.Current;
            yield return _rec.Hold(0.8f);
            for (int step = 1; step <= 10 && ui.Minigames.IsPlaying; step++)
            {
                wire.SolveForTest(step / 10f);
                yield return _rec.Hold(0.4f);
            }
            yield return _rec.Hold(2.8f);
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
                yield return _rec.Hold(0.6f);
                for (int step = 1; step <= 8; step++)
                {
                    board.SolveForTest(step / 10f);
                    yield return _rec.Hold(0.28f);
                }
                yield return _rec.Hold(0.5f);
                ui.Minigames.Close();
                _rec.End();
            }

            // --- 07: the other gig minigames
            yield return PlayShow(ui, "07_pixel_match", new PixelMatch(GigSetup(Skill.Art, "Animate a character sprite sheet", "Copper Labs")), 4.2f,
                (g, t) => ((PixelMatch)g).PaintForTest(Mathf.Clamp01(t / 3.6f) * 0.92f));
            int fixedTypos = 0;
            yield return PlayShow(ui, "08_typo_hunt", new TypoHunt(GigSetup(Skill.Writing, "Proofread a quest log", "Bright Games")), 4.0f,
                (g, t) => { if (t > 0.8f + 0.7f * fixedTypos) { ((TypoHunt)g).FixForTest(1); fixedTypos++; } });
            int sorted = 0;
            yield return PlayShow(ui, "09_feedback_sort", new FeedbackSort(GigSetup(Skill.Design, "Sort playtest feedback", "Pixel Interactive")), 4.0f,
                (g, t) => { if (t > 0.8f + 0.6f * sorted) { ((FeedbackSort)g).SortForTest(1); sorted++; } });
            bool matched = false;
            yield return PlayShow(ui, "10_synth_tuner", new SynthTuner(GigSetup(Skill.Audio, "Tune menu sound effects", "Copper Labs")), 3.6f,
                (g, t) => { if (!matched && t > 1.8f) { ((SynthTuner)g).MatchForTest(); matched = true; } });

            // --- 11: the evening: a coffee and the cat
            _rec.Begin("11_coffee_cat");
            var home = ui.Page<HomePage>();
            ui.ShowHome();
            if (ui.Today.OpenGroup != "build") ui.Today.Toggle("build");
            _game.NotifyChanged();
            yield return _rec.Hold(1.4f);
            home.BrewCoffee();
            _game.NotifyChanged();
            yield return _rec.Hold(1.8f);
            sim.Founder.Morale = 50;
            home.PetCat();
            _game.NotifyChanged();
            yield return _rec.Hold(2.8f);
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
            yield return _rec.Hold(0.6f);
            for (int step = 1; step <= 10 && ui.Minigames.IsPlaying; step++)
            {
                finale.SolveForTest(step / 10f);
                yield return _rec.Hold(0.25f);
            }
            if (ui.Minigames.IsPlaying) ui.Minigames.FinishNow();
            yield return _rec.Hold(3.4f);
            ui.Minigames.Close();
            _rec.End();

            // --- 13: night falls; sleep
            _rec.Begin("13_night");
            ui.ShowHome();
            ui.Play(PlanBlock.Rest);
            _game.NotifyChanged();
            yield return _rec.Hold(2.2f);
            ui.Sleep();
            _game.NotifyChanged();
            yield return _rec.Hold(2.2f);
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
                    yield return Frames(5);
                    _rec.Begin("14_launch_day");
                    ui.ShowLaunchDay(project);
                    yield return _rec.Hold(7.5f);
                    ui.LaunchShow?.Close();
                    _rec.End();
                }
            }
            Assert.IsTrue(launched, "The scripted run should ship a game");

            // --- 15: the studio's pages after launch
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            _game.NotifyChanged();
            yield return Frames(3);
            int pageClip = 0;
            foreach (var title in new[] { "Dashboard", "Bank" })
            {
                var page = ui.Pages.FirstOrDefault(p => p.Title == title);
                if (page == null) continue;
                _rec.Begin($"15_page_{++pageClip}_{title.ToLowerInvariant()}");
                ui.Show(page);
                _game.NotifyChanged();
                yield return _rec.Hold(2.6f);
                _rec.End();
            }
            _rec.Begin("16_reviews");
            ui.Show<ProjectPage>();
            _game.NotifyChanged();
            yield return Frames(2);
            var body = ui.Root.Q<ScrollView>(className: "window__body");
            yield return _rec.Hold(1.0f);
            if (body != null)
                for (int i = 1; i <= 20; i++)
                {
                    body.scrollOffset = new Vector2(0f, 60f * i);
                    yield return _rec.Hold(0.05f);
                }
            yield return _rec.Hold(2.2f);
            _rec.End();
        }

        IEnumerator PlayShow(StudioUI ui, string clip, Minigame game, float seconds, Action<Minigame, float> play)
        {
            _rec.Begin(clip);
            ui.Minigames.Open(game, score => ShowReport(game, score, 0.25, 0.25 + 0.4 * score));
            ui.Minigames.StartPlaying();
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < seconds)
            {
                play(game, Time.realtimeSinceStartup - start);
                yield return _rec.Hold(0.05f);
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

        static System.Collections.Generic.IEnumerable<string> Prefixes(string text)
        {
            for (int i = 1; i <= text.Length; i++) yield return text.Substring(0, i);
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        /// <summary>
        /// Streams the render texture into ffmpeg at a constant 30 fps of real time: when a frame takes longer than
        /// 1/30 s to render and read back, the last image is repeated, so animations keep their real speed.
        /// </summary>
        sealed class ClipRecorder
        {
            const int Fps = 30;
            readonly RenderTexture _source;
            readonly string _folder;
            readonly Texture2D _frame;
            System.Diagnostics.Process _ffmpeg;
            Stream _pipe;
            double _start;
            int _written;

            public ClipRecorder(RenderTexture source, string folder)
            {
                _source = source;
                _folder = folder;
                _frame = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            }

            public void Begin(string name)
            {
                End();
                string exe = Environment.GetEnvironmentVariable("INDIE_FFMPEG");
                if (string.IsNullOrEmpty(exe)) exe = "ffmpeg";
                string output = Path.Combine(_folder, name + ".mp4");
                _ffmpeg = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = $"-y -loglevel error -f rawvideo -pix_fmt rgba -s {_source.width}x{_source.height} -r {Fps} -i - " +
                                    $"-vf vflip -c:v libx264 -preset veryfast -crf 12 -pix_fmt yuv420p \"{output}\"",
                        UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true,
                    },
                };
                _ffmpeg.Start();
                _pipe = _ffmpeg.StandardInput.BaseStream;
                _start = Time.realtimeSinceStartupAsDouble;
                _written = 0;
                Pump();
            }

            public IEnumerator Hold(float seconds)
            {
                double end = Time.realtimeSinceStartupAsDouble + seconds;
                do
                {
                    yield return null;
                    Pump();
                } while (Time.realtimeSinceStartupAsDouble < end);
            }

            void Pump()
            {
                if (_pipe == null) return;
                int due = (int)((Time.realtimeSinceStartupAsDouble - _start) * Fps) + 1;
                if (due <= _written) return;
                var previous = RenderTexture.active;
                RenderTexture.active = _source;
                _frame.ReadPixels(new Rect(0, 0, _source.width, _source.height), 0, 0);
                RenderTexture.active = previous;
                var bytes = _frame.GetRawTextureData();
                for (; _written < due; _written++) _pipe.Write(bytes, 0, bytes.Length);
            }

            public void End()
            {
                if (_ffmpeg == null) return;
                _pipe.Flush();
                _pipe.Close();
                _ffmpeg.WaitForExit(120000);
                _ffmpeg.Dispose();
                _ffmpeg = null;
                _pipe = null;
            }
        }
    }
}
