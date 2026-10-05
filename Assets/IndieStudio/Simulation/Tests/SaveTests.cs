using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace IndieStudio.Simulation.Tests
{
    /// <summary>Save games: nothing is lost, and a loaded game plays on exactly as if it had never been saved.</summary>
    public class SaveTests
    {
        static byte[] Save(GameSimulation sim)
        {
            using (var stream = new MemoryStream())
            {
                sim.Save(stream);
                return stream.ToArray();
            }
        }

        static GameSimulation Load(byte[] bytes) => GameSimulation.Load(new MemoryStream(bytes));

        /// <summary>A game with a bit of everything: gigs, a shipped game and its post-mortem, mail, reports, a half-played day.</summary>
        static GameSimulation PlayedGame(ulong seed, int days)
        {
            var sim = new GameSimulation(seed);
            BotPlayer.Balanced().Play(sim, days);
            sim.StartDay();
            sim.PerformSlot(PlanBlock.Research);
            sim.State.TutorialStep = 7;
            sim.State.TutorialStepDay = sim.Day - 1;
            sim.State.Stats.CatPets = 4;
            sim.State.Stats.BrilliantSessions = 9;
            return sim;
        }

        [Test]
        public void Save_RoundTripKeepsEveryField()
        {
            var sim = PlayedGame(3, 250);
            Assert.That(sim.State.ShippedGames, Is.GreaterThan(0), "The test game should have shipped something");
            Assert.That(sim.State.Today.Count, Is.EqualTo(1), "…and be in the middle of a day");

            var bytes = Save(sim);
            var loaded = Load(bytes);
            AssertDeepEqual(sim.State, loaded.State, "State");
            CollectionAssert.AreEqual(bytes, Save(loaded), "Saving a loaded game gives the same bytes");
            TestContext.WriteLine($"Save size after {sim.Day} days: {bytes.Length / 1024.0:0.0} KB");
        }

        [Test]
        public void Save_LoadedGameContinuesIdentically()
        {
            var original = new GameSimulation(17);
            var bot = BotPlayer.Balanced();
            bot.Play(original, 120);
            var copy = Load(Save(original));

            bot.Play(original, 200);
            BotPlayer.Balanced().Play(copy, 200);
            AssertDeepEqual(original.State, copy.State, "State");
        }

        [Test]
        public void Save_RejectsOtherFilesAndCutOffSaves()
        {
            Assert.Throws<InvalidDataException>(() => Load(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            var bytes = Save(new GameSimulation(1));
            Assert.Throws<InvalidDataException>(() => Load(bytes.Take(bytes.Length / 2).ToArray()));
            Assert.Throws<InvalidDataException>(() => Load(bytes.Take(bytes.Length - 1).ToArray()));

            var future = (byte[])bytes.Clone();
            future[4] = 99; // version
            Assert.Throws<InvalidDataException>(() => Load(future));
        }

        /// <summary>Compares two object graphs field by field (public and private), including collection order.</summary>
        static void AssertDeepEqual(object expected, object actual, string path)
        {
            if (expected == null || actual == null)
            {
                Assert.AreEqual(expected == null, actual == null, path + " (null)");
                return;
            }
            var type = expected.GetType();
            Assert.AreEqual(type, actual.GetType(), path + " (type)");
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            {
                Assert.AreEqual(expected, actual, path);
                return;
            }
            if (typeof(Delegate).IsAssignableFrom(type)) return;
            if (expected is IDictionary expectedMap)
            {
                var actualMap = (IDictionary)actual;
                var expectedKeys = expectedMap.Keys.Cast<object>().ToList();
                var actualKeys = actualMap.Keys.Cast<object>().ToList();
                Assert.AreEqual(expectedKeys.Count, actualKeys.Count, path + ".Count");
                for (int i = 0; i < expectedKeys.Count; i++)
                {
                    Assert.AreEqual(expectedKeys[i], actualKeys[i], $"{path} key #{i}");
                    AssertDeepEqual(expectedMap[expectedKeys[i]], actualMap[actualKeys[i]], $"{path}[{expectedKeys[i]}]");
                }
                return;
            }
            if (expected is IEnumerable expectedItems)
            {
                var a = expectedItems.Cast<object>().ToList();
                var b = ((IEnumerable)actual).Cast<object>().ToList();
                Assert.AreEqual(a.Count, b.Count, path + ".Count");
                for (int i = 0; i < a.Count; i++) AssertDeepEqual(a[i], b[i], $"{path}[{i}]");
                return;
            }
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    AssertDeepEqual(field.GetValue(expected), field.GetValue(actual), path + "." + field.Name);
        }
    }
}
