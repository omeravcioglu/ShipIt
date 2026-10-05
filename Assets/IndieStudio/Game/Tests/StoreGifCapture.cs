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
    /// Records the gameplay clips for the animated GIFs in the Steam store description. The scenes are played like a person
    /// would: one click at a time on Wire It Up, bugs crawling before they're squashed, a lit board before the result.
    /// Only runs with INDIE_GIFS=1 (ffmpeg from INDIE_FFMPEG or PATH). Clips land in Logs/StoreGifs/clips, and
    /// Marketing/Steam/gifs/make_gifs.sh cuts them into GIFs.
    /// </summary>
    public class StoreGifCapture
    {
        const int Width = 1920, Height = 1080;

        GameController _game;
        RenderTexture _texture;
        Recorder _rec;

        [TearDown]
        public void TearDown()
        {
            _rec?.End();
            if (_game != null) UnityEngine.Object.Destroy(_game.gameObject);
            if (_texture != null) _texture.Release();
        }

        [UnityTest]
        public IEnumerator RecordStoreGifClips()
        {
            if (Environment.GetEnvironmentVariable("INDIE_GIFS") != "1") Assert.Ignore("Set INDIE_GIFS=1 to record the store-page GIF clips.");
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "StoreGifs", "clips"));
            Directory.CreateDirectory(folder);
            foreach (var old in Directory.GetFiles(folder, "*.mp4")) File.Delete(old);

            _texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
            _game = GameBootstrap.Create(11, _texture);
            _rec = new Recorder(_texture, folder);
            yield return Frames(10);
            var ui = _game.Ui;
            var sim = _game.Sim;

            // A named studio, Sam's gig and a tiny game in progress.
            ui.Root.Q<StudioNameField>().Value = "McHunter Studio";
            for (int i = 0; i < 3; i++) ui.Coach.Next();
            ui.Coach.Finish();
            var friend = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
            sim.AcceptGig(friend.Id, out _);
            Assert.IsNotNull(sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out string error), error);
            ui.ShowHome();
            ui.Today.Toggle("build");
            _game.NotifyChanged();
            yield return Frames(5);

            // --- wire_code: building a feature, one click at a time, until the board lights up (the morning)
            ui.Play(PlanBlock.Build(Skill.Programming));
            ui.Minigames.StartPlaying();
            var wire = (WireItUp)ui.Minigames.Current;
            yield return Frames(3);
            _rec.Begin("wire_code");
            yield return _rec.Hold(0.6f);
            while (ui.Minigames.IsPlaying && !wire.IsOver && wire.StepTowardSolutionForTest()) yield return _rec.Hold(0.22f);
            yield return _rec.Hold(2.2f);
            _rec.End();
            ui.Minigames.Close();

            // --- looks_*: every discipline's board (played for show; no slot is spent)
            int n = 0;
            foreach (var skill in new[] { Skill.Art, Skill.Audio, Skill.Design, Skill.Writing })
            {
                var board = Activities.For(sim, PlanBlock.Build(skill)) as WireItUp;
                Assert.IsNotNull(board);
                board.Setup.Seed += 97 * ++n;
                ui.Minigames.Open(board, score => Report(board, score, 0.2, 0.2 + 0.3 * score));
                ui.Minigames.StartPlaying();
                yield return Frames(3);
                _rec.Begin("looks_" + skill.ToString().ToLowerInvariant());
                yield return _rec.Hold(0.3f);
                while (ui.Minigames.IsPlaying && !board.IsOver && board.StepTowardSolutionForTest()) yield return _rec.Hold(0.15f);
                yield return _rec.Hold(0.6f);
                _rec.End();
                ui.Minigames.Close();
            }

            // --- gig_bugs: Sam's gig is Bug Squash; the bugs crawl before they're squashed (the afternoon)
            ui.Play(PlanBlock.Gig(friend.Id));
            ui.Minigames.StartPlaying();
            var bugs = (BugSquash)ui.Minigames.Current;
            yield return Frames(3);
            _rec.Begin("gig_bugs");
            yield return _rec.Hold(1.4f);
            for (int i = 0; i < 9; i++)
            {
                bugs.SquashOneForTest();
                yield return _rec.Hold(0.42f);
            }
            _rec.End();
            ui.Minigames.FinishNow();
            ui.Minigames.Close();

            // --- gig_*: the other clients' minigames (played for show)
            yield return Show(ui, "gig_pixel", new PixelMatch(Gig(Skill.Art, "Animate a character sprite sheet", "Copper Labs")), 3.2f,
                (g, t) => ((PixelMatch)g).PaintForTest(Mathf.Clamp01(t / 2.8f) * 0.95f));
            int typos = 0;
            yield return Show(ui, "gig_typo", new TypoHunt(Gig(Skill.Writing, "Proofread a quest log", "Bright Games")), 3.2f,
                (g, t) => { if (t > 0.6f + 0.55f * typos) { ((TypoHunt)g).FixForTest(1); typos++; } });
            bool tuned = false;
            yield return Show(ui, "gig_synth", new SynthTuner(Gig(Skill.Audio, "Tune menu sound effects", "Copper Labs")), 3.0f,
                (g, t) => { if (!tuned && t > 1.3f) { ((SynthTuner)g).MatchForTest(); tuned = true; } });
            int sorted = 0;
            yield return Show(ui, "gig_feedback", new FeedbackSort(Gig(Skill.Design, "Sort playtest feedback", "Pixel Interactive")), 3.2f,
                (g, t) => { if (t > 0.6f + 0.45f * sorted) { ((FeedbackSort)g).SortForTest(1); sorted++; } });

            // --- room: the evening: a coffee, and the cat when the week gets rough
            var home = ui.Page<HomePage>();
            ui.ShowHome();
            if (ui.Today.OpenGroup != "build") ui.Today.Toggle("build");
            _game.NotifyChanged();
            yield return Frames(5);
            _rec.Begin("room");
            yield return _rec.Hold(1.0f);
            home.BrewCoffee();
            _game.NotifyChanged();
            yield return _rec.Hold(1.7f);
            sim.Founder.Morale = 50;
            home.PetCat();
            _game.NotifyChanged();
            yield return _rec.Hold(2.6f);
            _rec.End();

            // --- feature_complete: the last part of a feature clicks into place
            var finale = Activities.For(sim, PlanBlock.Build(Skill.Design)) as WireItUp;
            Assert.IsNotNull(finale);
            finale.Setup.Seed += 1234;
            ui.Minigames.Open(finale, score =>
            {
                // The feature really is finished, so the banner counts it ("1 of 8 built").
                var design = sim.State.ActiveProject.Workstreams.First(w => w.Discipline == Skill.Design);
                design.Done = design.FeatureSize;
                design.QualitySum = design.Done * 6;
                var report = Report(finale, score, 0.6, 1.0);
                report.Lines.Add($"Finished: {finale.Setup.Title} (Design)");
                report.Lines.Add("Flow: session 3 in a row, ×1.10.");
                return report;
            });
            ui.Minigames.StartPlaying();
            yield return Frames(3);
            _rec.Begin("feature_complete");
            yield return _rec.Hold(0.3f);
            while (ui.Minigames.IsPlaying && !finale.IsOver && finale.StepTowardSolutionForTest()) yield return _rec.Hold(0.15f);
            yield return _rec.Hold(3.6f);
            _rec.End();
            ui.Minigames.Close();

            // --- day: the evening ends, night falls, you sleep, it's morning
            ui.ShowHome();
            _game.NotifyChanged();
            yield return Frames(5);
            _rec.Begin("day");
            yield return _rec.Hold(0.8f);
            ui.Play(PlanBlock.Rest);
            _game.NotifyChanged();
            yield return _rec.Hold(1.9f);
            ui.Sleep();
            _game.NotifyChanged();
            yield return _rec.Hold(2.1f);
            _rec.End();

            // --- launch_day: play on (unrecorded) until the game ships, then the launch show
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
                if (project == null || project.Status != ProjectStatus.ReadyToRelease || sim.Day - project.StorePageDay < Tuning.VaporMinStorePageDays) continue;
                // The wishlists of a page that was marketed and went to Next Fest (the bot doesn't market much), so the counters have something to count.
                project.Wishlists = Math.Max(project.Wishlists, 1480);
                if (!sim.Release(project.Id, 2.99, out _)) continue;
                launched = true;
                sim.StartDay();
                foreach (var m in sim.State.Inbox.Messages.Where(m => !m.IsRead).ToList()) sim.MarkRead(m.Id);
                ui.ShowHome();
                _game.NotifyChanged();
                yield return Frames(5);
                _rec.Begin("launch_day");
                ui.ShowLaunchDay(project);
                yield return _rec.Hold(7.2f);
                _rec.End();
                ui.LaunchShow?.Close();
            }
            Assert.IsTrue(launched, "The scripted run should ship a game");
        }

        IEnumerator Show(StudioUI ui, string clip, Minigame game, float seconds, Action<Minigame, float> play)
        {
            ui.Minigames.Open(game, score => Report(game, score, 0.25, 0.25 + 0.4 * score));
            ui.Minigames.StartPlaying();
            yield return Frames(3);
            _rec.Begin(clip);
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < seconds)
            {
                play(game, Time.realtimeSinceStartup - start);
                yield return _rec.Hold(0.05f);
            }
            _rec.End();
            ui.Minigames.Close();
        }

        static SlotReport Report(Minigame game, double score, double before, double after) => new SlotReport
        {
            Slot = 2, Activity = PlanBlock.Build(game.Setup.Skill), Title = game.Setup.Title, Subject = game.Setup.Title,
            HasSkill = true, Skill = game.Setup.Skill, ProgressBefore = before, ProgressAfter = after, Performance = Rules.MinigameMultiplier(score),
        };

        static MinigameSetup Gig(Skill skill, string title, string client) => new MinigameSetup
        {
            Kicker = "AFTERNOON · GIG", Title = title, Subtitle = client + " · " + skill,
            Icon = Fmt.SkillIcon(skill), TileClass = Fmt.SkillClass(skill), Skill = skill, SkillLevel = 2, Tier = 1, Seed = 2026 + (int)skill,
        };

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        /// <summary>
        /// Streams the render texture into ffmpeg at a steady 30 frames per real second (a frame that takes longer to
        /// render repeats the last image, so animations keep their real speed).
        /// </summary>
        sealed class Recorder
        {
            const int Fps = 30;
            readonly RenderTexture _source;
            readonly string _folder;
            readonly Texture2D _frame;
            System.Diagnostics.Process _ffmpeg;
            Stream _pipe;
            double _start;
            int _written;

            public Recorder(RenderTexture source, string folder)
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
                _ffmpeg = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = $"-y -loglevel error -f rawvideo -pix_fmt rgba -s {_source.width}x{_source.height} -r {Fps} -i - " +
                                    $"-vf vflip -c:v libx264 -preset veryfast -crf 10 -pix_fmt yuv444p \"{Path.Combine(_folder, name + ".mp4")}\"",
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
