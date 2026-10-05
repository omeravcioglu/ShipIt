using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndieStudio.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace IndieStudio.Game.Tests
{
    /// <summary>
    /// The Steam integration, without Steam: a fake platform signed in as "Tester", with a folder standing in for
    /// Steam Cloud. Your studio follows you to a second computer, a real conflict asks which studio to keep,
    /// achievements unlock with a toast (and quietly when they were earned before), and the menu shows who's
    /// signed in. With INDIE_SCREENSHOTS=1 it saves "steam_*" screenshots to Logs/UiScreenshots.
    /// </summary>
    public class PlatformTests
    {
        sealed class FakeSteam : IPlatform
        {
            public readonly HashSet<string> Achievements = new HashSet<string>();
            public readonly List<string> Unlocks = new List<string>();

            public FakeSteam(ICloudStorage cloud)
            {
                Cloud = cloud;
            }

            public string Name => "Steam";
            public bool IsSignedIn => true;
            public PlayerIdentity Player => new PlayerIdentity { Id = "76561197960287930", DisplayName = "Tester" };
            public string Problem => null;
            public ICloudStorage Cloud { get; }
            public bool IsAchievementUnlocked(string id) => Achievements.Contains(id);

            public bool UnlockAchievement(string id)
            {
                Unlocks.Add(id);
                return Achievements.Add(id);
            }

            public void RequestAuthTicket(string audience, Action<string> onTicket) => onTicket?.Invoke("14000000");
            public void Tick() { }
            public void Dispose() { }
        }

        GameController _game;
        RenderTexture _texture;
        string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "IndieStudioPlatformTest");
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            _texture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_texture != null) _texture.Release();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        /// <summary>Starts the game on one of the "computers" (its own save folder), sharing the fake Steam's cloud.</summary>
        IEnumerator Boot(string computer, IPlatform platform, ulong seed = 0)
        {
            if (_game != null)
            {
                UnityEngine.Object.DestroyImmediate(_game.gameObject);
                yield return null;
            }
            _game = GameBootstrap.Create(seed, _texture, computer == null ? null : Path.Combine(_root, computer), platform);
            yield return null;
            yield return null;
        }

        FolderCloudStorage NewCloud() => new FolderCloudStorage(Path.Combine(_root, "cloud"));

        [UnityTest]
        public IEnumerator Cloud_YourStudioFollowsYouToAnotherComputer()
        {
            var cloud = NewCloud();
            yield return Boot("pc_a", new FakeSteam(cloud), 41);
            _game.Sim.NameStudio("Moth Lab");
            _game.Ui.Play(PlanBlock.Research);
            _game.SaveNow();
            Assert.IsTrue(cloud.Exists(SaveStore.FileName), "Saving uploads the studio to the cloud");
            int slot = _game.Sim.Slot;

            // A second computer with nothing saved on it: the studio comes down from the cloud.
            yield return Boot("pc_b", new FakeSteam(cloud));
            Assert.IsTrue(_game.ResumedFromSave);
            Assert.IsTrue(_game.LoadedFromCloud);
            Assert.AreEqual("Moth Lab", _game.Sim.State.StudioName);
            Assert.AreEqual(slot, _game.Sim.Slot, "…exactly where you left it");
            Assert.IsTrue(_game.Ui.HasBlockingModal, "Welcome back (from the cloud)");
            yield return null;
            Capture("steam_cloud_welcome_back");
            _game.Ui.DismissDialog();
            _game.Ui.Play(PlanBlock.Research);
            _game.SaveNow();
            int slotOnSecond = _game.Sim.Slot;
            Assert.AreEqual(slot + 1, slotOnSecond);

            // Back on the first computer: its own copy is older, so the cloud's wins without a question.
            yield return Boot("pc_a", new FakeSteam(cloud));
            Assert.IsNull(_game.PendingConflict);
            Assert.IsTrue(_game.LoadedFromCloud);
            Assert.AreEqual(slotOnSecond, _game.Sim.Slot);
        }

        [UnityTest]
        public IEnumerator Cloud_WhenBothComputersPlayedOn_ThePlayerChooses()
        {
            var cloud = NewCloud();
            yield return Boot("pc_a", new FakeSteam(cloud), 43);
            _game.Ui.Play(PlanBlock.Research);
            _game.SaveNow();

            // The first computer loses its connection and plays on…
            cloud.IsEnabled = false;
            _game.Ui.Play(PlanBlock.Research);
            _game.SaveNow();
            cloud.IsEnabled = true;

            // …while the second one plays the copy in the cloud.
            yield return Boot("pc_b", new FakeSteam(cloud));
            _game.Ui.DismissDialog();
            _game.Sim.NameStudio("Night Owl Games");
            _game.NotifyChanged();
            _game.SaveNow();

            // The first computer is back online: both copies moved on since they last matched, so it asks.
            yield return Boot("pc_a", new FakeSteam(cloud));
            Assert.IsNotNull(_game.PendingConflict);
            Assert.IsTrue(_game.Ui.HasBlockingModal);
            yield return null;
            Capture("steam_cloud_conflict");
            var localSave = Path.Combine(_root, "pc_a", SaveStore.FileName);
            var before = File.ReadAllBytes(localSave);
            _game.NotifyChanged();
            _game.SaveNow();
            CollectionAssert.AreEqual(before, File.ReadAllBytes(localSave), "Nothing is saved until the player has chosen");

            Click(_game.Ui.Root.Q<Button>("choice-0")); // keep the cloud save
            yield return null;
            Assert.IsNull(_game.PendingConflict);
            Assert.AreEqual("Night Owl Games", _game.Sim.State.StudioName, "The other computer's studio");
            Assert.IsTrue(File.Exists(Path.Combine(_root, "pc_a", SaveStore.FileName + ".local-copy")), "This computer's copy is kept");
            Assert.IsTrue(_game.Ui.HasBlockingModal, "…and then the welcome back");
            _game.Ui.DismissDialog();
            _game.SaveNow();
            CollectionAssert.AreEqual(cloud.Read(SaveStore.FileName), File.ReadAllBytes(localSave), "Both copies match again");
        }

        [UnityTest]
        public IEnumerator Achievements_UnlockOnSteamWithAToast()
        {
            var steam = new FakeSteam(null);
            yield return Boot(null, steam, 45);
            CollectionAssert.IsEmpty(steam.Unlocks, "A new studio has earned nothing");
            StringAssert.StartsWith("Hey Tester, I'm Doug.", _game.Ui.Root.Q<Label>(className: "coach__text").text, "Doug greets you by your Steam name");
            Capture("steam_doug_says_hi");

            _game.Sim.NameStudio("Moth Lab");
            _game.NotifyChanged();
            CollectionAssert.Contains(steam.Achievements, "ACH_ITS_OFFICIAL");
            yield return null;
            Assert.IsNotNull(_game.Ui.Root.Q("toast-achievement"), "A new achievement gets a toast");
            Capture("steam_achievement_toast");
            _game.NotifyChanged();
            Assert.AreEqual(1, steam.Unlocks.Count(id => id == "ACH_ITS_OFFICIAL"), "Each achievement is sent once");

            _game.Ui.Hud.ToggleMenu();
            yield return null;
            var account = _game.Ui.Root.Q("menu-account");
            Assert.IsTrue(account.Query<Label>().ToList().Any(l => l.text.Contains("Signed in to Steam as Tester")));
            Capture("steam_menu");
            _game.Ui.Hud.CloseMenus();

            _game.Ui.ShowAchievements();
            yield return null;
            Assert.AreEqual(AchievementCatalog.All.Count, _game.Ui.Root.Q("achievements").Query(className: "ach-row").ToList().Count);
            Capture("steam_achievements");
            _game.Ui.DismissDialog();

            string ticket = null;
            _game.Platform.RequestAuthTicket("indie-studio-server", t => ticket = t);
            Assert.AreEqual("14000000", ticket, "Signing in to a game server: a ticket for it to check");
        }

        [UnityTest]
        public IEnumerator Achievements_EarnedBefore_AreSentQuietly()
        {
            var sim = new GameSimulation(47);
            sim.NameStudio("Moth Lab");
            new SaveStore(Path.Combine(_root, "pc_a")).Write(sim);
            var steam = new FakeSteam(null);
            yield return Boot("pc_a", steam);
            CollectionAssert.Contains(steam.Achievements, "ACH_ITS_OFFICIAL", "Sent to Steam when the studio loads");
            Assert.IsNull(_game.Ui.Root.Q("toast-achievement"), "…without a toast");
        }

        [UnityTest]
        public IEnumerator Offline_SaysSoAndKeepsSavesLocal()
        {
            yield return Boot("pc_a", new OfflinePlatform(false, "Steam isn't running, or you aren't signed in to it."), 49);
            Assert.IsFalse(_game.Saves.HasCloud);
            _game.Ui.Hud.ToggleMenu();
            yield return null;
            var account = _game.Ui.Root.Q("menu-account");
            Assert.IsTrue(account.Query<Label>().ToList().Any(l => l.text == "Playing offline"));
            Assert.IsTrue(account.Query<Label>().ToList().Any(l => l.text.Contains("Steam isn't running")));
            Capture("steam_menu_offline");
        }

        static void Click(Button button)
        {
            Assert.IsNotNull(button, "The button should be on screen");
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
