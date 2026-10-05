using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>Achievements and cloud-save syncing: the parts of the Steam integration that are pure rules.</summary>
    public class AchievementTests
    {
        static string[] Earned(GameSimulation sim) => AchievementCatalog.Earned(sim.State).Select(a => a.Id).ToArray();

        [Test]
        public void Catalog_IdsAreUniqueAndFitSteam()
        {
            CollectionAssert.AllItemsAreUnique(AchievementCatalog.All.Select(a => a.Id));
            foreach (var a in AchievementCatalog.All)
            {
                StringAssert.IsMatch("^ACH_[A-Z0-9_]+$", a.Id, "Steam API names: capitals, digits and underscores");
                Assert.That(a.Title.Length, Is.InRange(1, 32), a.Id);
                Assert.That(a.Description.Length, Is.InRange(1, 100), a.Id);
                Assert.That(a.Description, Does.EndWith("."), a.Id);
                Assert.AreEqual(a.Progress != null, a.Target > 0, $"{a.Id}: counters need a target");
            }
        }

        [Test]
        public void Catalog_MatchesTheSteamSetupSheet()
        {
            // Steam/achievements.md is what gets typed into Steamworks: every API name, name and description must match.
            var folder = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (folder != null && !File.Exists(Path.Combine(folder.FullName, "Steam", "achievements.md"))) folder = folder.Parent;
            Assert.IsNotNull(folder, "Steam/achievements.md should be in the project");
            var sheet = File.ReadAllLines(Path.Combine(folder.FullName, "Steam", "achievements.md"));
            var rows = sheet.Where(l => l.StartsWith("| ") && l.Contains("`ACH_")).ToList();
            Assert.AreEqual(AchievementCatalog.All.Count, rows.Count, "One row per achievement");
            foreach (var a in AchievementCatalog.All)
                Assert.IsTrue(rows.Any(r => r.Contains($"| `{a.Id}` | {a.Title} | {a.Description} | {(a.Hidden ? "Yes" : "No")} |")),
                    $"{a.Id} isn't in Steam/achievements.md as it is in the game");
        }

        [Test]
        public void ANewStudio_HasEarnedNothing()
        {
            CollectionAssert.IsEmpty(Earned(new GameSimulation(5)));
        }

        [Test]
        public void ShippingTheFirstGame_EarnsTheFirstSteps()
        {
            var sim = new GameSimulation(11);
            BotPlayer.Balanced().Play(sim, 60);
            var earned = Earned(sim);
            CollectionAssert.IsSubsetOf(new[] { "ACH_RENT_MONEY", "ACH_IT_COMPILES", "ACH_SHIP_IT" }, earned);
            CollectionAssert.DoesNotContain(earned, "ACH_A_REAL_INDIE_GAME", "A Small game takes far longer");
            CollectionAssert.DoesNotContain(earned, "ACH_THE_HARD_WAY");
        }

        [Test]
        public void LifeInTheBedroom_EarnsItsOwnAchievements()
        {
            var sim = new GameSimulation(7);
            sim.NameStudio("Moth Lab");
            sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Small, ArtStyleId.PixelArt, out _);
            CollectionAssert.Contains(Earned(sim), "ACH_ITS_OFFICIAL");

            Assert.IsTrue(sim.DrinkCoffee(out _));
            sim.PerformSlot(PlanBlock.Build(Skill.Programming), Rules.MinigameMultiplier(1.0));
            CollectionAssert.Contains(Earned(sim), "ACH_BRILLIANT", "A perfect session is worth three stars");
            CollectionAssert.DoesNotContain(Earned(sim), "ACH_DOUBLE_SHOT");
            Assert.IsTrue(sim.DrinkCoffee(out _));
            CollectionAssert.Contains(Earned(sim), "ACH_DOUBLE_SHOT");

            sim.PerformSlot(PlanBlock.Build(Skill.Programming));
            sim.PerformSlot(PlanBlock.Build(Skill.Programming));
            CollectionAssert.DoesNotContain(Earned(sim), "ACH_IN_THE_ZONE", "Three in a row isn't full flow yet");
            sim.Sleep();
            sim.PerformSlot(PlanBlock.Build(Skill.Programming));
            Assert.AreEqual(4, sim.State.FlowStreak, "The streak survives the night");
            CollectionAssert.Contains(Earned(sim), "ACH_IN_THE_ZONE");

            var cat = AchievementCatalog.Get("ACH_CAT_PERSON");
            for (int day = 0; day < 9; day++)
            {
                sim.PetCat();
                sim.PetCat(); // twice a day still counts once
                sim.Sleep();
            }
            Assert.AreEqual(9, cat.Progress(sim.State), "9 of 10 days");
            Assert.IsFalse(cat.IsEarned(sim.State));
            sim.PetCat();
            Assert.IsTrue(cat.IsEarned(sim.State));
        }

        [Test]
        public void OrdinarySessions_AreNotThreeStars()
        {
            var sim = new GameSimulation(8);
            sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            sim.PerformSlot(PlanBlock.Build(Skill.Art));
            sim.PerformSlot(PlanBlock.Build(Skill.Art), Rules.MinigameMultiplier(0.84));
            sim.PerformSlot(PlanBlock.Rest, Rules.MinigameMultiplier(1.0));
            Assert.AreEqual(0, sim.State.Stats.BrilliantSessions);
        }

        [Test]
        public void HardTimes_AreRemembered()
        {
            var sim = new GameSimulation(9);
            sim.StartProject(sim.State.Ideas.First().Id, ProjectScope.Tiny, ArtStyleId.PixelArt, out _);
            sim.CancelProject(sim.State.Projects[0].Id);
            CollectionAssert.Contains(Earned(sim), "ACH_KILL_YOUR_DARLINGS");
            Assert.IsTrue(AchievementCatalog.Get("ACH_KILL_YOUR_DARLINGS").Hidden);

            var brink = AchievementCatalog.Get("ACH_BACK_FROM_THE_BRINK");
            sim.Finance.Apply(sim.Day, -(sim.Finance.Cash + 300), TxCategory.Hardware, "A very expensive mistake");
            Assert.IsFalse(brink.IsEarned(sim.State), "In debt, not back yet");
            sim.Finance.Apply(sim.Day, 5300, TxCategory.Freelance, "A lucky break");
            Assert.IsTrue(brink.IsEarned(sim.State));
        }

        // ------------------------------------------------------------------ cloud saves

        [Test]
        public void Sync_TheSideThatMovedOnWins()
        {
            Assert.AreEqual(SyncDecision.NoSave, SaveSync.Decide(null, null, null));
            Assert.AreEqual(SyncDecision.UseLocalAndUpload, SaveSync.Decide("a", null, null), "First time with cloud saves");
            Assert.AreEqual(SyncDecision.UseCloud, SaveSync.Decide(null, "a", null), "A new computer");
            Assert.AreEqual(SyncDecision.UseLocal, SaveSync.Decide("a", "a", "old"), "Already the same");
            Assert.AreEqual(SyncDecision.UseCloud, SaveSync.Decide("a", "b", "a"), "Played on another computer");
            Assert.AreEqual(SyncDecision.UseLocalAndUpload, SaveSync.Decide("b", "a", "a"), "Played offline here");
            Assert.AreEqual(SyncDecision.Conflict, SaveSync.Decide("b", "c", "a"), "Both moved on: ask");
            Assert.AreEqual(SyncDecision.Conflict, SaveSync.Decide("b", "c", null), "Never synced and different: ask");
        }

        [Test]
        public void Sync_HashesAreStable()
        {
            var sim = new GameSimulation(12);
            BotPlayer.Balanced().Play(sim, 30);
            byte[] a, b;
            using (var stream = new MemoryStream()) { sim.Save(stream); a = stream.ToArray(); }
            using (var stream = new MemoryStream()) { sim.Save(stream); b = stream.ToArray(); }
            Assert.AreEqual(SaveSync.Hash(a), SaveSync.Hash(b), "The same game saves to the same fingerprint");
            Assert.AreEqual(64, SaveSync.Hash(a).Length);
            sim.PerformSlot(PlanBlock.Rest);
            using (var stream = new MemoryStream()) { sim.Save(stream); b = stream.ToArray(); }
            Assert.AreNotEqual(SaveSync.Hash(a), SaveSync.Hash(b));
        }

        [Test]
        public void Save_TwoYearsFitsEasilyInSteamCloud()
        {
            var sim = new GameSimulation(21);
            BotPlayer.Balanced().Play(sim, 730);
            using (var stream = new MemoryStream())
            {
                sim.Save(stream);
                TestContext.WriteLine($"Save size after two years: {stream.Length / 1024.0:0.0} KB");
                Assert.That(stream.Length, Is.LessThan(1024 * 1024), "Steam/README.md asks for a 4 MB quota: room for the save, its backup and growth");
            }
        }

        [Test]
        public void Save_Version2StillLoads()
        {
            var sim = new GameSimulation(13);
            BotPlayer.Balanced().Play(sim, 20);
            sim.State.Stats.CatPets = 3;
            using (var stream = new MemoryStream())
            {
                SaveGame.Write(sim.State, stream, 2);
                stream.Position = 0;
                var loaded = GameSimulation.Load(stream);
                Assert.AreEqual(sim.Day, loaded.Day);
                Assert.AreEqual(0, loaded.State.Stats.CatPets, "Counters newer than version 2 start at zero");
            }
        }
    }
}
