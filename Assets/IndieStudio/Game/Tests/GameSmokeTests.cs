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
    /// Boots the real game with its UI, names the studio and follows the tutorial through the first day, plays
    /// every minigame (and every look of Wire It Up), brews a coffee and pets the cat, then plays on until the
    /// first game has launched (with its launch-day show) and opens every screen. Unity fails the test on any
    /// logged exception. With INDIE_SCREENSHOTS=1 each step is also saved to Logs/UiScreenshots.
    /// </summary>
    public class GameSmokeTests
    {
        GameController _game;
        RenderTexture _texture;
        string _folder;
        bool _screenshots;
        int _shot;

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.Destroy(_game.gameObject);
            if (_texture != null) _texture.Release();
        }

        [UnityTest]
        public IEnumerator Game_TutorialMinigamesAndFirstRelease()
        {
            _screenshots = Environment.GetEnvironmentVariable("INDIE_SCREENSHOTS") == "1";
            _folder = Path.Combine(Application.dataPath, "..", "Logs", "UiScreenshots");
            if (_screenshots)
            {
                Directory.CreateDirectory(_folder);
                foreach (var old in Directory.GetFiles(_folder, "*.png"))
                {
                    string file = Path.GetFileName(old);
                    if (!file.StartsWith("save_") && !file.StartsWith("steam_") && !file.StartsWith("fest_")) File.Delete(old);
                }
            }

            _texture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
            _game = GameBootstrap.Create(11, _texture);
            yield return Frames(2);
            var ui = _game.Ui;
            var sim = _game.Sim;
            Assert.IsNotNull(ui, "The UI should be built in Start()");
            Assert.IsTrue(ui.Coach.IsActive, "A new game starts with the tutorial");
            Capture("start_tutorial");

            // There is no clock: nothing moves until the player acts.
            for (int i = 0; i < 30; i++) yield return null;
            Assert.AreEqual(0, sim.Day);
            Assert.AreEqual(0, sim.Slot);

            // --- Doug asks for the studio's name first; the next steps only need "Next"
            var nameField = ui.Root.Q<StudioNameField>();
            Assert.IsNotNull(nameField, "The tutorial starts by naming the studio");
            nameField.Value = "Moth Lab";
            for (int i = 0; i < 3; i++) ui.Coach.Next();
            Assert.AreEqual("Moth Lab", sim.State.StudioName);
            yield return Frames(3);
            Assert.AreEqual("today", ui.Coach.TargetName);
            Capture("tutorial_your_day");
            ui.Coach.Next();
            yield return Frames(3);
            Assert.AreEqual("dock-gigs", ui.Coach.TargetName);
            Capture("tutorial_open_giggrid");

            ui.Show<GigsPage>();
            yield return Frames(3);
            Assert.AreEqual("accept-friend", ui.Coach.TargetName);
            Capture("tutorial_accept_gig");

            var friend = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
            Assert.IsTrue(sim.AcceptGig(friend.Id, out _));
            ui.ShowHome();
            ui.Today.Toggle("gig");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.AreEqual("gig-friend", ui.Coach.TargetName);
            Capture("today_gig_options");

            // --- a gig session is a minigame (Bug Squash for Sam's programming gig)
            ui.Play(PlanBlock.Gig(friend.Id));
            Assert.IsTrue(ui.Minigames.IsOpen, "Gig work opens a minigame");
            Assert.AreEqual(0, sim.Slot, "The slot is only spent when the minigame ends");
            yield return Frames(2);
            Capture("minigame_intro");
            ui.Minigames.StartPlaying();
            yield return new WaitForSecondsRealtime(2f);
            Capture("minigame_bug_squash");
            var bugs = (BugSquash)ui.Minigames.Current;
            bugs.SquashAllForTest();
            Assert.Greater(bugs.Squashed, 0, "Bugs crawl out within two seconds");
            yield return Frames(2);
            ui.Minigames.FinishNow();
            yield return Frames(2);
            Capture("minigame_result");
            Assert.AreEqual(1, sim.Slot, "The session used the morning");
            Assert.Greater(friend.HoursDone, 0);
            ui.Minigames.Close();

            // --- starting a game and building a feature (Wire It Up)
            ui.Show<ProjectPage>();
            yield return Frames(3);
            Assert.AreEqual("start-project", ui.Coach.TargetName);
            Capture("project_new");
            var idea = sim.State.Ideas.First();
            Assert.IsNotNull(sim.StartProject(idea.Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out string error), error);
            ui.ShowHome();
            ui.Today.Toggle("build");
            yield return Frames(3);
            Capture("today_build_options");

            ui.Play(PlanBlock.Build(Skill.Programming));
            ui.Minigames.StartPlaying();
            yield return Frames(3);
            Capture("minigame_wire_it_up");
            var wire = (WireItUp)ui.Minigames.Current;
            Assert.Greater(wire.ModuleCount, 1);
            Assert.AreEqual(WireLook.Circuit, wire.Theme.Look, "Code is a circuit");
            Assert.AreEqual(2, wire.LockedCount, "A tiny project bolts two tiles in place");
            wire.SolveForTest();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(ui.Minigames.IsPlaying, "The lit board shines for a moment…");
            yield return new WaitForSecondsRealtime(0.9f);
            Assert.IsFalse(ui.Minigames.IsPlaying, "…then lighting every module ends the session early");
            Capture("minigame_wire_it_up_result");
            Assert.IsNotNull(ui.Root.Q<GamePreview>("mg-preview"), "A build session shows your game so far");
            Assert.AreEqual(2, sim.Slot);
            Assert.Greater(sim.State.ActiveProject.Progress, 0);
            Assert.AreEqual(1, sim.State.FlowStreak, "One build session starts a streak");
            ui.Minigames.Close();

            // --- the evening: a coffee, the cat, and the flow of the streak
            var home = ui.Page<HomePage>();
            ui.ShowHome();
            home.BrewCoffee();
            Assert.IsTrue(sim.State.CoffeeReady, "The mug brews a coffee for the next session");
            sim.Founder.Morale = 50;
            home.PetCat();
            Assert.AreEqual(50 + Tuning.CatMorale, sim.Founder.Morale, 1e-9, "The cat cheers you up when you're low");
            if (ui.Today.OpenGroup != "build") ui.Today.Toggle("build");
            _game.NotifyChanged();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsNotNull(ui.Root.Q("today-coffee"), "Your day shows the coffee waiting");
            Capture("room_coffee_cat_flow");

            ui.Show<ProjectPage>();
            yield return Frames(3);
            Assert.IsNotNull(ui.Root.Q<GamePreview>("project-preview"), "The project shows your game so far");
            Capture("project_active");
            ui.ShowHome();

            // --- the other gig minigames (played for screenshots; no slot is spent)
            yield return PlayForShow(ui, new PixelMatch(Setup(Skill.Art)), g => ((PixelMatch)g).PaintForTest(0.6f), "minigame_pixel_match");
            yield return PlayForShow(ui, new TypoHunt(Setup(Skill.Writing)), g => ((TypoHunt)g).FixForTest(2), "minigame_typo_hunt");
            yield return PlayForShow(ui, new SynthTuner(Setup(Skill.Audio)), g => ((SynthTuner)g).MatchForTest(), "minigame_synth_tuner");
            yield return PlayForShow(ui, new FeedbackSort(Setup(Skill.Design)), g => ((FeedbackSort)g).SortForTest(3), "minigame_feedback_sort");

            // --- Wire It Up dresses up as the work: paint pipes, patch cables, level paths, red string
            var looks = new[] { (Skill.Art, WireLook.Paint), (Skill.Audio, WireLook.Patch), (Skill.Design, WireLook.Path), (Skill.Writing, WireLook.String) };
            foreach (var (skill, look) in looks)
            {
                var board = new WireItUp(BuildSetup(skill));
                Assert.AreEqual(look, board.Theme.Look);
                yield return PlayForShow(ui, board, g =>
                {
                    Assert.AreEqual(2, ((WireItUp)g).WobblyCount, "Bigger projects have wobbly tiles");
                    ((WireItUp)g).SolveForTest(0.55f);
                }, "wire_" + look.ToString().ToLowerInvariant(), settle: 0.35f);
            }

            // --- finishing a feature is a little party
            yield return PlayForShow(ui, new WireItUp(BuildSetup(Skill.Programming)), g => ((WireItUp)g).SolveForTest(), "minigame_feature_complete",
                score =>
                {
                    var report = new SlotReport
                    {
                        Slot = 2, Activity = PlanBlock.Build(Skill.Programming), Title = "Built Jump physics", Subject = "Jump physics",
                        HasSkill = true, Skill = Skill.Programming, ProgressBefore = 0.55, ProgressAfter = 1, Performance = Rules.MinigameMultiplier(score),
                    };
                    report.Lines.Add("Finished: Jump physics (Programming)");
                    report.Lines.Add("Flow: session 2 in a row, ×1.05.");
                    return report;
                },
                () => Assert.IsNotNull(ui.Root.Q("mg-cheer"), "A finished feature gets a celebration"));
            Assert.AreEqual(2, sim.Slot, "Show games don't spend slots");

            // --- the menu, and relaxed minigames: no timer and no speed bonus
            ui.Hud.ToggleMenu();
            yield return Frames(2);
            Capture("menu");
            ui.Hud.ToggleMenu();
            _game.Settings.RelaxedMinigames = true;
            var relaxed = Activities.For(sim, PlanBlock.Build(Skill.Programming), _game.Settings.RelaxedMinigames);
            Assert.IsTrue(relaxed.Relaxed);
            yield return PlayForShow(ui, relaxed, g =>
            {
                g.Advance(600f);
                Assert.IsFalse(g.IsOver, "Relaxed games never run out of time");
                ((WireItUp)g).SolveForTest();
            }, "minigame_relaxed_wire_it_up");
            Assert.AreEqual(0.85f, relaxed.Score, 1e-4f, "A perfect relaxed session scores 0.85 (×1.1)");
            _game.Settings.RelaxedMinigames = false;

            // --- the evening, the night and sleep
            ui.Play(PlanBlock.Rest);
            yield return Frames(3);
            Assert.IsTrue(sim.IsNight);
            Capture("night");
            ui.Sleep();
            yield return Frames(3);
            Assert.AreEqual(1, sim.Day);
            Assert.AreEqual(0, sim.Slot);
            Capture("next_morning");
            ui.Coach.Finish();

            // --- play on (scripted) until the first game has launched and its post-mortem arrived
            sim.ApplyPlan(PlanTemplates.Balanced());
            bool capturedDecision = false, capturedPostMortem = false;
            for (int day = 0; day < 200 && !capturedPostMortem; day++)
            {
                sim.AdvanceDay();
                foreach (var offer in sim.State.Freelance.Offers.OrderByDescending(g => g.EffectiveHourlyRate).ToList())
                    if (sim.State.Freelance.ActiveCount < 2) sim.AcceptGig(offer.Id, out _);

                var project = sim.State.ActiveProject;
                if (project != null && !project.HasStorePage && project.Progress >= 0.3) sim.OpenStorePage(project.Id, StorePlatform.Vapor, out _);
                if (project != null && project.Status == ProjectStatus.ReadyToRelease && sim.Day - project.StorePageDay >= Tuning.VaporMinStorePageDays
                    && sim.Release(project.Id, 2.99, out _))
                {
                    _game.NotifyChanged();
                    ui.ShowLaunchDay(project);
                    Assert.IsNotNull(ui.LaunchShow, "Launching puts on a show");
                    Assert.IsTrue(ui.HasBlockingModal, "News waits until the show is over");
                    yield return Frames(2);
                    ui.LaunchShow.SkipToEnd();
                    yield return new WaitForSecondsRealtime(0.6f); // the last reviews fade in
                    Capture("launch_day");
                    Assert.AreEqual(4, ui.LaunchShow.ReviewsShown, "The first player reviews pop in");
                    ui.LaunchShow.Close();
                    Assert.IsNull(ui.LaunchShow);

                    // Launch week: the morning numbers, and a hotfix played as Bug Squash.
                    foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
                    _game.NotifyChanged();
                    ui.ShowHome();
                    if (ui.Today.OpenGroup != "launch") ui.Today.Toggle("launch");
                    yield return Frames(2);
                    Assert.IsNotNull(ui.Root.Q("today-launch"), "Launch week shows the morning numbers");
                    Assert.IsNotNull(ui.Root.Q("launch-hotfix"));
                    Capture("launch_week_today");
                    ui.Play(PlanBlock.Hotfix(project.Id));
                    Assert.IsTrue(ui.Minigames.IsOpen, "A hotfix is played as Bug Squash");
                    ui.Minigames.StartPlaying();
                    yield return Frames(2);
                    ((BugSquash)ui.Minigames.Current).SquashAllForTest();
                    ui.Minigames.FinishNow();
                    yield return Frames(2);
                    Capture("launch_week_hotfix");
                    Assert.AreEqual(1, project.Result.Hotfixes);
                    ui.Minigames.Close();
                }

                if (!sim.State.Inbox.Pending.Any()) continue;
                bool postMortem = sim.State.Inbox.Pending.Any(m => m.EventId == "post_mortem");
                _game.NotifyChanged();
                if (!capturedDecision || postMortem)
                {
                    yield return Frames(2);
                    Capture(postMortem ? "post_mortem_modal" : "decision_modal");
                }
                capturedDecision = true;
                capturedPostMortem |= postMortem;
                foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            }
            Assert.IsTrue(capturedPostMortem, "The first game should launch and get a post-mortem within 200 days");
            sim.StartDay();
            _game.NotifyChanged();
            yield return Frames(2);

            foreach (var page in ui.Pages.ToList())
            {
                ui.Show(page);
                _game.NotifyChanged();
                yield return Frames(2);
                Capture("page_" + page.Title.Replace(' ', '_').ToLowerInvariant());
            }

            // Released games say what players think.
            ui.Show<ProjectPage>();
            yield return Frames(2);
            ui.Root.Q<ScrollView>(className: "window__body").scrollOffset = new Vector2(0f, 5000f);
            yield return Frames(3);
            Assert.That(ui.Root.Query(className: "review").ToList().Count, Is.GreaterThanOrEqualTo(1), "A released game shows player reviews");
            Capture("project_released_reviews");

            // The room reacts to the founder: time off puts you in bed.
            sim.TakeVacation(7);
            ui.ShowHome();
            _game.NotifyChanged();
            yield return Frames(2);
            Capture("room_resting");

            // The studio can be renamed from the menu (or the sign above the window).
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            foreach (var m in sim.State.Inbox.Messages.Where(m => !m.IsRead).ToList()) sim.MarkRead(m.Id);
            _game.NotifyChanged();
            ui.ShowNameStudio();
            yield return Frames(2);
            Assert.IsTrue(ui.HasBlockingModal);
            Capture("rename_studio");
            ui.DismissDialog();
            Assert.AreEqual("Moth Lab", sim.State.StudioName, "Cancelling keeps the name");

            Assert.IsFalse(sim.State.IsGameOver);
            Assert.Greater(sim.State.Freelance.Profile.CompletedGigs, 0);
            Assert.AreEqual(1, sim.State.ShippedGames);
            Assert.IsTrue(sim.State.ChaptersCompleted.ContainsKey("bedroom"), "Shipping the first game completes chapter 1");
            Assert.AreEqual(1, sim.State.Lessons.Count, "Answering the post-mortem teaches a lesson");
        }

        static MinigameSetup Setup(Skill skill) => new MinigameSetup
        {
            Kicker = "AFTERNOON · GIG", Title = "A test gig", Subtitle = "Test client · " + skill,
            Icon = Fmt.SkillIcon(skill), TileClass = Fmt.SkillClass(skill), Skill = skill, SkillLevel = 2, Tier = 1, Seed = 1234 + (int)skill,
        };

        static readonly string[] TestParts = { "Sketch", "Layout", "Melody", "First draft", "Input", "Polish" };

        static MinigameSetup BuildSetup(Skill skill) => new MinigameSetup
        {
            Kicker = "EVENING · BUILD", Title = skill + " feature", Subtitle = "Test game · " + skill + " · Small",
            Icon = Icons.Plug, TileClass = Fmt.SkillClass(skill), Skill = skill, SkillLevel = 2, Tier = 1, Seed = 4321 + (int)skill, Parts = TestParts,
        };

        /// <summary>
        /// Plays a minigame without spending a slot. Captures the board (after <paramref name="settle"/> seconds, for turning
        /// tiles to finish their animation) or, with <paramref name="check"/>, the result card.
        /// </summary>
        IEnumerator PlayForShow(StudioUI ui, Minigame game, Action<Minigame> play, string name, Func<double, SlotReport> apply = null, Action check = null,
            float settle = 0f)
        {
            ui.Minigames.Open(game, apply ?? (score => new SlotReport
            {
                Slot = 1, Activity = PlanBlock.Freelance, Title = "Worked on \"A test gig\"", Subject = "A test gig",
                HasSkill = true, Skill = game.Setup.Skill, ProgressBefore = 0.25, ProgressAfter = 0.25 + 0.4 * score,
            }));
            ui.Minigames.StartPlaying();
            yield return Frames(3);
            play(game);
            yield return Frames(2);
            if (settle > 0f) yield return new WaitForSecondsRealtime(settle);
            if (check == null) Capture(name);
            ui.Minigames.FinishNow();
            yield return Frames(2);
            if (check != null)
            {
                yield return new WaitForSecondsRealtime(0.2f);
                check();
                Capture(name);
            }
            ui.Minigames.Close();
            yield return Frames(1);
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        void Capture(string name)
        {
            if (!_screenshots) return;
            var previous = RenderTexture.active;
            RenderTexture.active = _texture;
            var image = new Texture2D(_texture.width, _texture.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, _texture.width, _texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(_folder, $"{_shot++:00}_{name}.png"), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }
    }
}
