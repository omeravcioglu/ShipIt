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
    /// Next Fest through the real UI: sign up from the Project page, make the demo from your day (Bug Squash), and the
    /// fest week's card. With INDIE_SCREENSHOTS=1 it saves "fest_*" screenshots.
    /// </summary>
    public class NextFestUiTests
    {
        GameController _game;
        RenderTexture _texture;

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_texture != null) _texture.Release();
        }

        [UnityTest]
        public IEnumerator NextFest_SignUpMakeTheDemoAndShowIt()
        {
            _texture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
            _game = GameBootstrap.Create(51, _texture);
            yield return null;
            yield return null;
            var ui = _game.Ui;
            var sim = _game.Sim;
            ui.Coach.Finish();

            // A Small game, 40% built, with a Vapor page.
            var game = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Small, ArtStyleId.PixelArt, out _);
            foreach (var w in game.Workstreams)
            {
                w.Done = w.Required * 0.4;
                w.QualitySum = w.Done * 5.5;
            }
            Assert.IsTrue(sim.OpenStorePage(game.Id, StorePlatform.Vapor, out string error), error);
            _game.NotifyChanged();

            ui.Show<ProjectPage>();
            yield return null;
            var signUp = ui.Root.Q<Button>("fest-signup");
            Assert.IsNotNull(signUp, "A Vapor game can sign up for Next Fest");
            ui.Root.Q<ScrollView>(className: "window__body").ScrollTo(signUp);
            yield return null;
            yield return null;
            Capture("fest_project_signup");
            Click(signUp);
            yield return null;
            Assert.AreEqual(GameSimulation.UpcomingFest(0), game.FestDay, "Signed up for the next fest");

            ui.ShowHome();
            if (ui.Today.OpenGroup != "build") ui.Today.Toggle("build");
            yield return null;
            Assert.IsNotNull(ui.Root.Q("build-demo"), "The demo is made from your day");
            Capture("fest_today_demo");
            ui.Play(PlanBlock.Demo(game.Id));
            Assert.IsTrue(ui.Minigames.IsOpen, "A demo session is played as Bug Squash");
            ui.Minigames.StartPlaying();
            yield return null;
            ((BugSquash)ui.Minigames.Current).SquashAllForTest();
            ui.Minigames.FinishNow();
            yield return null;
            yield return null;
            Capture("fest_demo_result");
            ui.Minigames.Close();
            Assert.Greater(game.DemoProgress, 0);

            // Fest week.
            for (int i = 0; i < 6; i++)
            {
                if (sim.IsNight) sim.Sleep();
                sim.PerformSlot(PlanBlock.Demo(game.Id));
            }
            while (sim.Day < game.FestDay + 2) sim.Sleep();
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            foreach (var m in sim.State.Inbox.Messages.Where(m => !m.IsRead).ToList()) sim.MarkRead(m.Id);
            _game.NotifyChanged();
            ui.ShowHome();
            yield return null;
            yield return null;
            Assert.IsNotNull(ui.Root.Q("today-fest"), "The fest week has its own card");
            Assert.Greater(game.FestWishlists, 0);
            Capture("fest_week");
        }

        static void Click(Button button)
        {
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = button;
                button.SendEvent(e);
            }
        }

        void Capture(string name)
        {
            if (Environment.GetEnvironmentVariable("INDIE_SCREENSHOTS") != "1") return;
            string folder = Path.Combine(Application.dataPath, "..", "Logs", "UiScreenshots");
            Directory.CreateDirectory(folder);
            var previous = RenderTexture.active;
            RenderTexture.active = _texture;
            var image = new Texture2D(_texture.width, _texture.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, _texture.width, _texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }
    }
}
