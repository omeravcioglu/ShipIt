using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>
    /// Seeing how far you've come: the studio's story, the week's visible progress (for Monday's look back), decor that
    /// makes the room yours, and the "seen it" memory behind the UI's one-time notes.
    /// </summary>
    public class ProgressTests
    {
        static void Play(GameSimulation sim, PlanBlock activity)
        {
            if (sim.IsNight) sim.Sleep();
            foreach (var m in sim.State.Inbox.Pending.ToList()) sim.ChooseOption(m.Id, m.DefaultOption);
            Assert.IsNotNull(sim.PerformSlot(activity));
        }

        static bool Told(GameSimulation sim, StoryKind kind, string text) => sim.State.Story.Any(e => e.Kind == kind && e.Text.Contains(text));

        [Test]
        public void Story_RemembersTheMilestones()
        {
            var sim = new GameSimulation(21);
            Assert.AreEqual(StoryKind.Start, sim.State.Story.First().Kind, "Day one starts the story");
            StringAssert.Contains("Quit your job to make games", sim.State.Story.First().Text);

            var sam = sim.State.Freelance.Gigs.First(g => g.Client == ClientType.Friend);
            Assert.IsTrue(sim.AcceptGig(sam.Id, out _));
            for (int i = 0; sam.Status == GigStatus.Active && i < 20; i++) Play(sim, PlanBlock.Gig(sam.Id));
            Assert.IsTrue(Told(sim, StoryKind.Gig, "Delivered your first gig"));

            var project = sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            Assert.IsTrue(Told(sim, StoryKind.Project, $"Started {project.Title}"));
            StringAssert.EndsWith("your first.", sim.State.Story.Last().Text);
            for (int i = 0; project.Status == ProjectStatus.InDevelopment && i < 150; i++) Play(sim, PlanBlock.Develop);
            Assert.IsTrue(Told(sim, StoryKind.Feature, "Built your first feature"));
            Assert.IsTrue(Told(sim, StoryKind.Feature, $"Finished building {project.Title}"));
            Assert.AreEqual(1, sim.State.Story.Count(e => e.Text.StartsWith("Built your first feature")), "Only the first feature is a milestone");

            Assert.IsTrue(sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out _));
            Assert.IsTrue(Told(sim, StoryKind.Store, "scratch.io page"));
            Assert.IsTrue(sim.Release(project.Id, 2.99, out _));
            Assert.IsTrue(Told(sim, StoryKind.Launch, "your first game is out in the world"));
            Assert.IsTrue(Told(sim, StoryKind.Chapter, "Chapter 1 complete"));

            sim.State.Audience.Followers = 120;
            sim.CheckProgress();
            sim.CheckProgress();
            Assert.AreEqual(1, sim.State.Story.Count(e => e.Kind == StoryKind.Fans && e.Text == "Reached 100 followers."), "Each milestone is told once");
            Assert.AreEqual(3, sim.FanMilestonesReached, "10, 50 and 100 followers: three notes on the corkboard");
            Assert.IsTrue(sim.State.Story.Zip(sim.State.Story.Skip(1), (a, b) => a.Day <= b.Day).All(x => x), "Oldest first");
        }

        [Test]
        public void Week_CountsWhatYouGotDone_ForMondaysLookBack()
        {
            var sim = new GameSimulation(22);
            sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            sim.Founder.Skills[Skill.Programming] = 2.999;
            Play(sim, PlanBlock.Build(Skill.Programming));
            Assert.AreEqual(3, sim.Founder.Level(Skill.Programming));
            Assert.AreEqual(1, sim.State.CurrentWeek.LevelsGained, "Crossing a level counts");
            Assert.IsTrue(Told(sim, StoryKind.Level, "Programming reached level 3"));

            while (GameCalendar.DayOfWeek(sim.Day) != 6 || !sim.IsNight) Play(sim, PlanBlock.Develop);
            int built = sim.State.ActiveProject.FeaturesDone;
            Assert.Greater(built, 0);
            Assert.AreEqual(built, sim.State.CurrentWeek.FeaturesBuilt, "Every feature finished this week");
            sim.Sleep();
            Assert.IsTrue(GameCalendar.IsMonday(sim.Day));
            Assert.AreEqual(built, sim.State.LastWeek.FeaturesBuilt, "Monday looks back at last week");
            Assert.AreEqual(1, sim.State.LastWeek.LevelsGained);
            Assert.AreEqual(0, sim.State.CurrentWeek.FeaturesBuilt, "…and the new week starts at zero");
        }

        [Test]
        public void Decor_MakesTheRoomYoursAndKeepsYourSpiritsUp()
        {
            var plain = new GameSimulation(23);
            var cozy = new GameSimulation(23);
            double morale = cozy.Founder.Morale;
            Assert.IsTrue(cozy.Buy("string_lights", out _));
            Assert.IsTrue(cozy.Buy("desk_lamp", out _));
            Assert.AreEqual(System.Math.Min(100, morale + 2 * Tuning.DecorMorale), cozy.Founder.Morale, 1e-9, "Each piece lifts morale right away");
            Assert.AreEqual(6, ShopCatalog.Comfort(cozy.State), 1e-9);
            Assert.AreEqual(0, ShopCatalog.Comfort(plain.State), 1e-9);
            Assert.IsTrue(Told(cozy, StoryKind.Shop, "Bought string lights for the room."));
            Assert.IsTrue(Told(cozy, StoryKind.Shop, "Bought a desk lamp for the room."));
            Assert.IsFalse(cozy.Buy("desk_lamp", out _), "Once is enough");

            // The same twenty weeks for both studios: the cozy one settles higher (and gets more done for it).
            cozy.Founder.Morale = plain.Founder.Morale = 40;
            for (int day = 0; day < 140; day++)
            {
                foreach (var sim in new[] { plain, cozy })
                {
                    sim.RestForTheDay();
                    sim.Sleep();
                }
            }
            Assert.Greater(cozy.Founder.Morale, plain.Founder.Morale + 4, "Decor raises the point morale settles at");
            Assert.Greater(Rules.Productivity(cozy.State).Value, Rules.Productivity(plain.State).Value, "…and a happier you gets more done");
            Assert.IsTrue(ShopCatalog.Items.Where(i => i.Decor).All(i => i.Price > 0 && i.Monthly == 0 && i.Comfort > 0), "Decor is a one-time buy with a comfort value");
        }

        [Test]
        public void MarkSeen_IsTrueOnlyTheFirstTime_AndIsSaved()
        {
            var sim = new GameSimulation(24);
            Assert.IsTrue(sim.MarkSeen("more_activities"));
            Assert.IsFalse(sim.MarkSeen("more_activities"));
            using (var stream = new MemoryStream())
            {
                SaveGame.Write(sim.State, stream);
                stream.Position = 0;
                Assert.IsFalse(GameSimulation.Load(stream).MarkSeen("more_activities"), "A loaded game remembers");
            }
        }

        [Test]
        public void Story_AndTheWeek_SurviveASave_AndOlderSavesStartWithout()
        {
            var sim = new GameSimulation(25);
            sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            for (int i = 0; i < 6; i++) Play(sim, PlanBlock.Develop);
            using (var stream = new MemoryStream())
            {
                SaveGame.Write(sim.State, stream);
                stream.Position = 0;
                var loaded = GameSimulation.Load(stream).State;
                Assert.AreEqual(sim.State.Story.Select(e => e.Text), loaded.Story.Select(e => e.Text));
                Assert.AreEqual(sim.State.CurrentWeek.FeaturesBuilt, loaded.CurrentWeek.FeaturesBuilt);
            }
            using (var stream = new MemoryStream())
            {
                SaveGame.Write(sim.State, stream, 6);
                stream.Position = 0;
                var old = GameSimulation.Load(stream).State;
                Assert.AreEqual(0, old.Story.Count, "A version 6 save has no story yet");
                Assert.AreEqual(0, old.CurrentWeek.FeaturesBuilt);
            }
        }
    }
}
