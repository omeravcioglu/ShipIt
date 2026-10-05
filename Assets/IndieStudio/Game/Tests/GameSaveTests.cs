using System;
using System.Collections;
using System.IO;
using System.Linq;
using IndieStudio.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IndieStudio.Game.Tests
{
    /// <summary>
    /// The real save path: the game autosaves into a folder, a new session continues from it with a "welcome
    /// back", and a damaged save is set aside instead of crashing the game. Uses a temporary folder, never the
    /// player's own save.
    /// </summary>
    public class GameSaveTests
    {
        GameController _game;
        RenderTexture _texture;
        string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Application.temporaryCachePath, "IndieStudioSaveTest");
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
            _texture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.Destroy(_game.gameObject);
            if (_texture != null) _texture.Release();
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        IEnumerator Restart()
        {
            UnityEngine.Object.Destroy(_game.gameObject);
            yield return null;
            _game = GameBootstrap.Create(0, _texture, _folder);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Game_ContinuesWhereYouLeftOff()
        {
            _game = GameBootstrap.Create(5, _texture, _folder);
            yield return null;
            yield return null;
            Assert.IsFalse(_game.ResumedFromSave, "A first session starts a new studio");
            Assert.IsFalse(_game.Ui.HasBlockingModal);

            var sim = _game.Sim;
            var friend = sim.State.Freelance.Offers.First(g => g.Client == ClientType.Friend);
            Assert.IsTrue(sim.AcceptGig(friend.Id, out _));
            _game.Ui.Play(PlanBlock.Research);
            _game.Ui.Coach.Next();
            _game.Ui.Coach.Next();
            yield return new WaitForSecondsRealtime(1.3f); // the autosave writes a moment after the last change
            Assert.IsTrue(File.Exists(Path.Combine(_folder, "studio.isav")), "The game saved itself");
            Assert.IsNotNull(_game.LastSaved);

            int day = sim.Day, slot = sim.Slot, step = sim.State.TutorialStep;
            double cash = sim.Finance.Cash;
            ulong seed = sim.State.Seed;
            yield return Restart();

            Assert.IsTrue(_game.ResumedFromSave, "The next session continues the save");
            var loaded = _game.Sim;
            Assert.AreEqual(seed, loaded.State.Seed);
            Assert.AreEqual(day, loaded.Day);
            Assert.AreEqual(slot, loaded.Slot, "…in the same part of the day");
            Assert.AreEqual(cash, loaded.Finance.Cash, 1e-9);
            Assert.AreEqual(step, loaded.State.TutorialStep, "…and the same tutorial step");
            Assert.IsTrue(loaded.State.Freelance.Active.Any(g => g.Client == ClientType.Friend));
            Assert.IsTrue(_game.Ui.HasBlockingModal, "A returning player is welcomed back");
            Capture("save_welcome_back");
            _game.Ui.ConfirmNewStudio();
            yield return null;
            yield return null;
            Capture("save_confirm_new_studio");

            // A new studio replaces the save on the next autosave.
            _game.NewGame(77);
            Assert.IsFalse(_game.Ui.HasBlockingModal);
            _game.SaveNow();
            yield return Restart();
            Assert.AreEqual(77UL, _game.Sim.State.Seed);
            Assert.AreEqual(0, _game.Sim.Day);
        }

        [UnityTest]
        public IEnumerator Game_SetsADamagedSaveAsideAndStartsFresh()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllBytes(Path.Combine(_folder, "studio.isav"), new byte[] { 0x49, 0x53, 0x41, 0x56, 1, 0, 0, 0, 42 });
            _game = GameBootstrap.Create(9, _texture, _folder);
            yield return null;
            yield return null;

            Assert.IsFalse(_game.ResumedFromSave);
            Assert.AreEqual(9UL, _game.Sim.State.Seed, "A new studio was started");
            Assert.IsTrue(_game.Ui.HasBlockingModal, "The player is told what happened");
            Assert.IsTrue(File.Exists(Path.Combine(_folder, "studio.isav.broken")), "The damaged save is kept for inspection");
            Capture("save_damaged");
        }

        /// <summary>With INDIE_SCREENSHOTS=1, saves the current frame to Logs/UiScreenshots (prefixed "save_").</summary>
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
