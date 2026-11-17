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
    /// The game opening up and showing your progress, through the real UI: a first day with three choices that grows
    /// into more, one mail button, a level bar after each session, Monday's look back, "Your story so far", and a
    /// room that grows with the studio (the plant, your games on the wall, fan notes, decor from the shop).
    /// With INDIE_SCREENSHOTS=1 it saves "progress_*" screenshots to Logs/UiScreenshots.
    /// </summary>
    public class ProgressUiTests
    {
        GameController _game;
        RenderTexture _texture;

        [SetUp]
        public void SetUp()
        {
            _texture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_texture != null) _texture.Release();
        }

        [UnityTest]
        public IEnumerator FirstDay_IsSimple_ThenTheDayOpensUp()
        {
            _game = GameBootstrap.Create(31, _texture);
            yield return Frames(2);
            var ui = _game.Ui;
            var sim = _game.Sim;

            foreach (var name in new[] { "act-build", "act-gig", "act-rest" }) Assert.IsNotNull(ui.Root.Q(name), name);
            foreach (var name in new[] { "act-market", "act-research", "act-learn" }) Assert.IsNull(ui.Root.Q(name), name + " waits for later");
            Assert.AreEqual(7, ui.Root.Query(className: "dock-btn").ToList().Count, "Room, Project, GigGrid, Mail, Bank, You, Shop");
            Assert.IsNotNull(ui.Root.Q("hud-mail"));
            Assert.AreEqual(2, ui.Root.Query(className: "hud-alert").ToList().Count, "One mail button, and the menu");

            ui.Show<ProjectPage>();
            yield return Frames(3);
            var stars = ui.Root.Q(className: "idea-card").Query(className: "star").ToList();
            Assert.AreEqual(5, stars.Count, "An idea shows how fun it could be in stars, not a percentage range");
            Assert.IsFalse(ui.Root.Query<Label>().ToList().Any(l => l.text.Contains("Fun potential") || l.text.Contains("person-weeks")));
            Capture("progress_00_ideas");
            ui.ShowHome();

            var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out string error);
            Assert.IsNotNull(project, error);
            _game.NotifyChanged();
            Assert.IsNull(ui.Root.Q("act-market"), "Not on day one, while Doug shows you around");
            Assert.AreEqual(0, sim.State.Inbox.Messages.Count(m => m.EventId == "project_started" && !m.IsRead), "News you saw pop up isn't unread mail");

            sim.RestForTheDay();
            Settle();
            ui.Sleep();
            yield return Frames(2);
            foreach (var name in new[] { "act-market", "act-research", "act-learn" }) Assert.IsNotNull(ui.Root.Q(name), name + " joins your day");
            Assert.IsTrue(ui.Root.Query<Label>(className: "toast__title").ToList().Any(l => l.text == "New things to do"), "…with a note saying so");
            Assert.AreEqual("Get inspired", ui.Root.Q("act-research").Q<Label>(className: "act__title").text);
            Capture("progress_01_day_opens_up");

            ui.Show<InboxPage>();
            yield return Frames(2);
            Assert.IsNull(ui.Root.Q(className: "inbox__rules"), "No pausing rules: there's no clock to pause");
            Assert.AreEqual(2, ui.Root.Query(className: "folder").ToList().Count, "All mail, and the mail to answer");
            Capture("progress_02_mail");
        }

        [UnityTest]
        public IEnumerator Sessions_FillYourSkills_AndMondayLooksBack()
        {
            _game = GameBootstrap.Create(32, _texture);
            yield return Frames(2);
            var ui = _game.Ui;
            var sim = _game.Sim;
            ui.Coach.Finish();
            sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            Settle();

            // An ordinary session fills the bar; this one tips Programming over into level 3.
            sim.Founder.Skills[Skill.Programming] = 2.995;
            ui.Play(PlanBlock.Build(Skill.Programming));
            Assert.IsTrue(ui.Minigames.IsOpen);
            ui.Minigames.StartPlaying();
            yield return Frames(3);
            ((WireItUp)ui.Minigames.Current).SolveForTest();
            ui.Minigames.FinishNow();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsNotNull(ui.Root.Q("mg-level-up"), "A new level gets its own moment");
            StringAssert.StartsWith("Level up! Programming 3", ui.Root.Q("mg-level-up").Q<Label>(className: "mg-skill__title").text);
            Assert.IsFalse(ui.Minigames.LastReport.Lines.Any(l => l.StartsWith("Programming 2.")), "No skill decimals in the report");
            Capture("progress_03_level_up");
            ui.Minigames.Close();

            ui.Play(PlanBlock.Build(Skill.Programming));
            ui.Minigames.StartPlaying();
            yield return Frames(3);
            ((WireItUp)ui.Minigames.Current).SolveForTest();
            ui.Minigames.FinishNow();
            yield return Frames(3);
            Assert.IsNotNull(ui.Root.Q("mg-skill"), "Every session shows how far the skill came");
            StringAssert.StartsWith("+", ui.Root.Q("mg-skill").Q<Label>(className: "mg-skill__text").text, "…as XP earned");
            Capture("progress_03b_skill_xp");
            ui.Minigames.Close();

            // Play on to Sunday night: Monday morning opens with last week.
            for (int i = 0; i < 40 && !(GameCalendar.DayOfWeek(sim.Day) == 6 && sim.IsNight); i++)
            {
                if (sim.IsNight) sim.Sleep();
                foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
                sim.PerformSlot(PlanBlock.Develop);
            }
            Settle();
            ui.Sleep();
            yield return Frames(2);
            Assert.IsTrue(GameCalendar.IsMonday(sim.Day));
            var week = ui.Root.Q("today-last-week");
            Assert.IsNotNull(week, "Monday looks back at last week");
            Assert.That(week.Query(className: "week-card__stat").ToList().Count, Is.GreaterThanOrEqualTo(2), "Features built and the level gained, at least");
            Capture("progress_04_week_in_review");
            ui.CallItADay();
            yield return Frames(1);
            Assert.IsNull(ui.Root.Q("today-last-week"), "It's a morning thing");

            ui.Show<SkillsPage>();
            yield return Frames(2);
            var story = ui.Root.Q("you-story");
            Assert.IsNotNull(story);
            var rows = story.Query(className: "story__row").ToList();
            Assert.That(rows.Count, Is.GreaterThanOrEqualTo(3));
            StringAssert.Contains("Quit your job", rows.Last().Q<Label>(className: "story__text").text, "Oldest at the bottom");
            Assert.AreEqual(6, ui.Root.Query(className: "skill-row").ToList().Count, "The skills that matter");
            ui.Root.Q<ScrollView>(className: "window__body").ScrollTo(story);
            yield return Frames(3);
            Capture("progress_05_your_story");
        }

        [UnityTest]
        public IEnumerator Room_GrowsWithTheStudio()
        {
            _game = GameBootstrap.Create(33, _texture);
            yield return Frames(2);
            var ui = _game.Ui;
            var sim = _game.Sim;
            ui.Coach.Finish();
            Settle();
            var plant = ui.Root.Q(className: "dio-plant");
            Assert.IsTrue(plant.ClassListContains("dio-plant--sprout"), "Day one: a sprout");
            Assert.IsTrue(ui.Root.Q("room-cork").ClassListContains("hidden"), "No fans yet");
            Assert.AreEqual(0, ui.Root.Query("room-frame").ToList().Count);

            // A first game, shipped; then a few months of studio life, and some fans.
            var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            for (int i = 0; project.Status == ProjectStatus.InDevelopment && i < 150; i++)
            {
                if (sim.IsNight) sim.Sleep();
                foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
                sim.PerformSlot(PlanBlock.Develop);
            }
            if (sim.IsNight) sim.Sleep();
            Assert.IsTrue(sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out _));
            Assert.IsTrue(sim.Release(project.Id, 2.99, out _));
            while (sim.Day < 85)
            {
                foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
                sim.RestForTheDay();
                sim.Sleep();
            }
            sim.State.Audience.Followers = 300;
            sim.Founder.Morale = 70;
            Settle();

            ui.Show<ShopPage>();
            yield return Frames(2);
            foreach (var id in new[] { "string_lights", "desk_lamp", "fluffy_rug", "neon_sign" })
            {
                var buy = ui.Root.Q<Button>("buy-" + id);
                Assert.IsNotNull(buy, id);
                Click(buy);
                yield return Frames(1);
                Assert.IsTrue(sim.State.Owned.Contains(id), id);
            }
            ui.Root.Q<ScrollView>(className: "window__body").scrollOffset = new Vector2(0, 520);
            yield return Frames(3);
            Capture("progress_06_shop_decor");

            Settle();
            ui.ShowHome();
            yield return Frames(3);
            Assert.IsTrue(plant.ClassListContains("dio-plant--bloom"), "Months later, the plant flowers");
            Assert.AreEqual(1, ui.Root.Query("room-frame").ToList().Count, "Your released game hangs on the wall");
            Assert.IsFalse(ui.Root.Q("room-cork").ClassListContains("hidden"));
            Assert.AreEqual(4, ui.Root.Q("room-cork").Query(className: "dio-note").ToList().Count, "10, 50, 100 and 250 followers");
            Assert.IsFalse(ui.Root.Q(className: "dio-lights").ClassListContains("hidden"));
            Assert.IsFalse(ui.Root.Q(className: "dio-lamp").ClassListContains("hidden"));
            Assert.IsTrue(ui.Root.Q(className: "dio-rug").ClassListContains("dio-rug--fluffy"));
            Assert.IsTrue(ui.Root.Q(className: "dio-sign").ClassListContains("dio-sign--neon"));
            Capture("progress_07_room_grown");
        }

        /// <summary>Answers the mail that waits for an answer and reads the rest, then refreshes (no dialog open).</summary>
        void Settle()
        {
            var sim = _game.Sim;
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            foreach (var m in sim.State.Inbox.Messages.Where(m => !m.IsRead).ToList()) sim.MarkRead(m.Id);
            _game.NotifyChanged();
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
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
