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
    /// The free demo through the real UI: the welcome and how to play, the apps it keeps closed, Doug's guide from the
    /// first day to the launch, the launch-day show and the end card (wishlist, play again). Also the demo running out
    /// of days, and a demo save resuming at its end without touching the full game's save. With INDIE_SCREENSHOTS=1 it
    /// saves "demo_*" screenshots to Logs/UiScreenshots.
    /// </summary>
    public class DemoUiTests
    {
        GameController _game;
        RenderTexture _texture;
        string _folder;

        [SetUp]
        public void SetUp()
        {
            _texture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
            _folder = Path.Combine(Application.temporaryCachePath, "IndieStudioDemoTest");
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_texture != null) _texture.Release();
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        [UnityTest]
        public IEnumerator Demo_FromTheWelcomeToTheEnd()
        {
            _game = GameBootstrap.Create(11, _texture, null, null, Edition.Demo);
            yield return Frames(3);
            var ui = _game.Ui;
            var sim = _game.Sim;
            Assert.IsTrue(sim.IsDemo);

            // --- the welcome, and how to play
            Assert.IsNotNull(ui.Root.Q("demo-intro"), "A new demo opens with its welcome");
            Assert.IsTrue(ui.HasBlockingModal);
            Assert.AreEqual(3, ui.Root.Query(className: "demo-step").ToList().Count);
            Capture("demo_01_welcome");
            Click(ui.Root.Q<Button>("choice-1"));
            yield return Frames(3);
            Assert.IsNull(ui.Root.Q("demo-intro"));
            Assert.IsNotNull(ui.Root.Q("how-to-play"));
            Assert.AreEqual(6, ui.Root.Query(className: "howto__card").ToList().Count);
            Capture("demo_02_how_to_play");
            Click(ui.Root.Q<Button>("choice-0"));
            yield return Frames(3);
            Assert.IsNull(ui.Root.Q("how-to-play"));
            Assert.IsFalse(ui.HasBlockingModal);
            Assert.IsTrue(ui.Coach.IsActive, "Doug's tutorial is waiting once the welcome is closed");
            Assert.IsFalse(ui.Root.Q("coach").ClassListContains("hidden"));

            // --- a simpler game: closed apps, a shorter day panel, the demo's clock
            foreach (var locked in new[] { "dock-dashboard", "dock-ideas", "dock-bank", "dock-you", "dock-shop" })
                Assert.IsTrue(ui.Root.Q(locked).ClassListContains("dock-btn--locked"), locked + " is closed in the demo");
            foreach (var open in new[] { "dock-home", "dock-mail", "dock-project", "dock-gigs" })
                Assert.IsFalse(ui.Root.Q(open).ClassListContains("dock-btn--locked"), open + " is open in the demo");
            ui.Show<FinancePage>();
            Assert.IsInstanceOf<HomePage>(ui.Current, "A closed app doesn't open");
            foreach (var hidden in new[] { "act-market", "act-research", "act-learn" }) Assert.IsNull(ui.Root.Q(hidden), hidden);
            Assert.IsNotNull(ui.Root.Q("today-locked"));
            Assert.AreEqual("DEMO · 14 DAYS LEFT", ui.Root.Q<Label>(className: "hud-goal__chapter").text);
            ui.Hud.ToggleMenu();
            yield return Frames(2);
            Assert.IsNotNull(ui.Root.Q("menu-how-to-play"));
            Assert.IsNotNull(ui.Root.Q("menu-wishlist"));
            Assert.IsNull(ui.Root.Q("menu-achievements"), "Achievements belong to the full game");
            Capture("demo_03_menu");
            ui.Hud.ToggleMenu();
            yield return Frames(2);

            // --- Doug's first day, as in the full game
            ui.Root.Q<StudioNameField>().Value = "Moth Lab";
            for (int i = 0; i < 4; i++) ui.Coach.Next(); // the name, the goal, the runway, your day
            Assert.AreEqual("Moth Lab", sim.State.StudioName);
            Assert.AreEqual("dock-gigs", ui.Coach.TargetName);
            var sam = sim.State.Freelance.Gigs.First(g => g.Client == ClientType.Friend);
            Assert.IsTrue(sim.AcceptGig(sam.Id, out string error), error);
            Assert.IsNotNull(sim.PerformSlot(PlanBlock.Gig(sam.Id)));
            _game.NotifyChanged();
            Assert.AreEqual("dock-project", ui.Coach.TargetName);
            ui.Show<ProjectPage>();
            yield return Frames(2);
            var small = ui.Root.Query<Button>(className: "choice-chip").ToList().First(b => b.text.StartsWith("Small"));
            Assert.AreEqual("Small (full game)", small.text);
            Assert.IsFalse(small.enabledSelf, "Small games come with the full game");
            ui.Root.Q<ScrollView>(className: "window__body").ScrollTo(small);
            yield return Frames(3);
            Capture("demo_04_new_project");
            var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out error);
            Assert.IsNotNull(project, error);
            Assert.IsNotNull(sim.PerformSlot(PlanBlock.Develop));
            ui.ShowHome();
            Assert.AreEqual("today-week", ui.Coach.TargetName);
            ui.Coach.Next(); // 45 hours a week
            Assert.IsNotNull(sim.PerformSlot(PlanBlock.Develop));
            Assert.IsTrue(sim.IsNight);
            Settle(sim);
            ui.Sleep();
            Assert.AreEqual(1, sim.Day);
            ui.Coach.Next(); // "That's the basics!"
            yield return Frames(2);

            // --- then Doug's guide points at what's next, all the way to the launch
            Assert.AreEqual("What's next · demo, 13 days left", ui.Root.Q<Label>(className: "coach__counter").text);
            Assert.That(new[] { "act-gig", "gig-friend" }, Has.Member(ui.Coach.TargetName), "Doug points at Sam's gig first");
            StringAssert.Contains("Finish Sam's gig", ui.Root.Q<Label>(className: "coach__text").text);
            Settle(sim);
            yield return Frames(2);
            Capture("demo_05_guide");

            for (int i = 0; sam.Status == GigStatus.Active; i++)
            {
                Assert.Less(i, 20, "Sam's gig takes a few sessions");
                PlaySlot(sim, PlanBlock.Gig(sam.Id));
            }
            Assert.AreEqual(GigStatus.Completed, sam.Status);
            Settle(sim);
            if (!sim.IsNight)
            {
                Assert.That(new[] { "act-build", "build-first" }, Has.Member(ui.Coach.TargetName), "Then the next feature");
                StringAssert.Contains("Keep building", ui.Root.Q<Label>(className: "coach__text").text);
            }
            for (int i = 0; project.Status == ProjectStatus.InDevelopment; i++)
            {
                Assert.Less(i, 120, "A tiny game gets built within the demo");
                PlaySlot(sim, PlanBlock.Develop);
            }
            Assert.AreEqual(ProjectStatus.ReadyToRelease, project.Status);
            Assert.Less(sim.Day, DemoRules.Days);
            Settle(sim);
            if (sim.IsNight) ui.Sleep();
            Settle(sim);
            Assert.AreEqual("dock-project", ui.Coach.TargetName, "Every feature built: the Project app is next");

            // --- a scratch.io page (there's no Vapor in the demo) and the launch
            ui.Show<ProjectPage>();
            yield return Frames(2);
            Assert.AreEqual("store-scratch", ui.Coach.TargetName);
            Assert.IsFalse(ui.Root.Query<Button>().ToList().Any(b => b.text.Contains("Vapor")), "No Vapor page in the demo");
            Click(ui.Root.Q<Button>("store-scratch"));
            yield return Frames(2);
            Assert.IsTrue(project.HasStorePage);
            Assert.AreEqual(StorePlatform.ScratchIo, project.Platform);
            Assert.AreEqual("project-launch", ui.Coach.TargetName);
            Settle(sim);
            yield return Frames(2); // laid out, so it can be scrolled to
            var launch = ui.Root.Q<Button>("project-launch");
            Assert.IsTrue(launch.enabledSelf, "A scratch.io game can launch right away");
            ui.Root.Q<ScrollView>(className: "window__body").ScrollTo(launch);
            yield return new WaitForSecondsRealtime(0.2f); // Doug's box moves off the button on its next tick
            Capture("demo_06_ready_to_launch");
            Click(launch);
            yield return Frames(2);
            Assert.IsNotNull(ui.LaunchShow, "Launching puts on the show");
            Assert.AreEqual(DemoEnd.Shipped, sim.State.DemoEnd);
            Assert.IsNull(ui.Root.Q("demo-end"), "The end card waits for the show");
            Assert.AreEqual("Continue", Label(ui.Root.Q<Button>("launch-done")));
            ui.LaunchShow.SkipToEnd();
            yield return new WaitForSecondsRealtime(0.6f);
            Capture("demo_07_launch_day");
            ui.LaunchShow.Close();
            yield return Frames(3);

            // --- the end of the demo
            Assert.IsNotNull(ui.Root.Q("demo-end"), "After the show, the demo's end card");
            Assert.IsTrue(ui.HasBlockingModal);
            Assert.AreEqual("That's the end of the demo!", ui.Root.Q<Label>(className: "modal__title").text);
            StringAssert.Contains("I hope you liked it", ui.Root.Q<Label>(className: "modal__body").text);
            Assert.AreEqual(5, ui.Root.Query(className: "demo-end__stat").ToList().Count, "The game, its launch day, features, gigs and days");
            Assert.AreEqual("Wishlist Ship It! on Steam", ui.Root.Q<Button>("choice-0").Q<Label>(className: "choice__label").text);
            Assert.AreEqual("Play the demo again", ui.Root.Q<Button>("choice-1").Q<Label>(className: "choice__label").text);
            Assert.IsFalse(ui.Coach.IsActive, "Doug's guide is done once the game is out");
            Assert.AreEqual("DEMO · COMPLETE", ui.Root.Q<Label>(className: "hud-goal__chapter").text);
            Assert.AreEqual("5/5", ui.Root.Q<Label>(className: "hud-goal__count").text, "The goal stays on chapter 1, all done");
            Assert.IsInstanceOf<HomePage>(ui.Current, "The room is behind the end card");
            string copies = ui.Root.Query<Label>(className: "demo-end__value").ToList()[1].text;
            Assert.AreEqual(Fmt.Num(Math.Floor(GameSimulation.ExpectedLaunchDayUnits(project))), copies, "The launch show's count");
            Assert.IsNull(sim.PerformSlot(PlanBlock.Develop), "Time has stopped");
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("demo_08_end");

            // --- and again
            var before = sim;
            Click(ui.Root.Q<Button>("choice-1"));
            yield return Frames(3);
            Assert.AreNotSame(before, _game.Sim);
            Assert.IsTrue(_game.Sim.IsDemo);
            Assert.AreEqual(DemoEnd.None, _game.Sim.State.DemoEnd);
            Assert.AreEqual(0, _game.Sim.Day);
            Assert.IsNotNull(ui.Root.Q("demo-intro"), "Playing again starts with the welcome");
            Assert.IsNull(ui.Root.Q("demo-end"));
            Assert.IsTrue(ui.Coach.IsActive, "...and Doug's tutorial");
        }

        [UnityTest]
        public IEnumerator Demo_EndsWhenTheTwoWeeksAreUp()
        {
            _game = GameBootstrap.Create(12, _texture, null, null, Edition.Demo);
            yield return Frames(2);
            var ui = _game.Ui;
            var sim = _game.Sim;
            ui.DismissDialog();
            ui.Coach.Finish();
            for (int i = 0; sim.Day < DemoRules.Days - 1; i++)
            {
                Assert.Less(i, 30);
                Settle(sim);
                ui.CallItADay();
                Settle(sim);
                ui.Sleep();
            }
            Assert.IsTrue(sim.State.Inbox.Messages.Any(m => m.EventId == "demo_days_left"), "Doug warned three days before");
            Assert.AreEqual("DEMO · 1 DAY LEFT", ui.Root.Q<Label>(className: "hud-goal__chapter").text);
            Settle(sim);
            ui.CallItADay();
            Settle(sim);
            yield return Frames(2);
            Assert.AreEqual("Sleep (ends the demo)", Label(ui.Root.Q<Button>("today-sleep")));
            Capture("demo_09_last_night");
            ui.Sleep();
            yield return Frames(3);
            Assert.AreEqual(DemoEnd.OutOfTime, sim.State.DemoEnd);
            Assert.IsNotNull(ui.Root.Q("demo-end"));
            Assert.AreEqual("Your two weeks are up!", ui.Root.Q<Label>(className: "modal__title").text);
            Assert.AreEqual(4, ui.Root.Query(className: "demo-end__stat").ToList().Count);
            Assert.AreEqual("DEMO · OVER", ui.Root.Q<Label>(className: "hud-goal__chapter").text);
            Assert.IsNull(sim.PerformSlot(PlanBlock.Rest), "Time has stopped");
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("demo_10_out_of_time");
        }

        [UnityTest]
        public IEnumerator Demo_KeepsItsOwnSave_AndResumesAtTheEnd()
        {
            _game = GameBootstrap.Create(13, _texture, _folder, null, Edition.Demo);
            yield return Frames(2);
            var sim = _game.Sim;
            _game.Ui.DismissDialog();
            _game.Ui.Coach.Finish();
            var sam = sim.State.Freelance.Gigs.First(g => g.Client == ClientType.Friend);
            Assert.IsTrue(sim.AcceptGig(sam.Id, out string error), error);
            for (int i = 0; sam.Status == GigStatus.Active && i < 20; i++) PlaySlot(sim, PlanBlock.Gig(sam.Id));
            var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.Minimalist, out error);
            Assert.IsNotNull(project, error);
            for (int i = 0; project.Status == ProjectStatus.InDevelopment && i < 120; i++) PlaySlot(sim, PlanBlock.Develop);
            if (sim.IsNight) sim.Sleep();
            Assert.IsTrue(sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out error), error);
            Settle(sim);
            _game.Ui.Launch(project, 2.99);
            Assert.IsNotNull(_game.Ui.LaunchShow, "Launched");
            _game.Ui.LaunchShow.Close();
            Assert.IsNotNull(_game.Ui.Root.Q("demo-end"));
            _game.SaveNow();
            Assert.IsTrue(File.Exists(Path.Combine(_folder, SaveStore.DemoFileName)), "The demo saves as demo.isav");
            Assert.IsFalse(File.Exists(Path.Combine(_folder, SaveStore.FileName)), "...never as the full game's studio.isav");
            UnityEngine.Object.DestroyImmediate(_game.gameObject);

            _game = GameBootstrap.Create(14, _texture, _folder, null, Edition.Demo);
            yield return Frames(3);
            Assert.IsTrue(_game.ResumedFromSave);
            Assert.AreEqual(DemoEnd.Shipped, _game.Sim.State.DemoEnd);
            Assert.IsNotNull(_game.Ui.Root.Q("demo-end"), "A finished demo opens at its end card");
            UnityEngine.Object.DestroyImmediate(_game.gameObject);

            _game = GameBootstrap.Create(15, _texture, _folder);
            yield return Frames(2);
            Assert.IsFalse(_game.ResumedFromSave, "The full game doesn't pick up the demo's studio");
            Assert.IsFalse(_game.Sim.IsDemo);
            Assert.IsNull(_game.Ui.Root.Q("demo-end"));
            Assert.IsNull(_game.Ui.Root.Q("demo-intro"));
            Assert.IsTrue(File.Exists(Path.Combine(_folder, SaveStore.DemoFileName)), "...and leaves it alone");
        }

        /// <summary>Answers the mail that waits for an answer and reads the rest, then refreshes the UI (so no dialog is open).</summary>
        void Settle(GameSimulation sim)
        {
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            foreach (var m in sim.State.Inbox.Messages.Where(m => !m.IsRead).ToList()) sim.MarkRead(m.Id);
            _game.NotifyChanged();
        }

        /// <summary>Plays one slot of work (an ordinary session), sleeping first if the day is used up.</summary>
        static void PlaySlot(GameSimulation sim, PlanBlock activity)
        {
            if (sim.IsNight) sim.Sleep();
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            Assert.IsNotNull(sim.PerformSlot(activity), "The slot was played");
        }

        static string Label(Button button) => button.Q<Label>(className: "btn__label").text;

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        static void Click(Button button)
        {
            Assert.IsNotNull(button);
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
