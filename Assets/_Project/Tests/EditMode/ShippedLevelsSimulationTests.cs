using Match3.Core;
using Match3.Data;
using NUnit.Framework;
using UnityEditor;

namespace Match3.Tests
{
    /// <summary>
    /// Plays every level of the real LevelCatalog with both bots. It catches a level that cannot be generated or shuffled,
    /// or that makes the game logic throw, before a player does. Needs the Unity asset database, so it is not part of the dotnet run.
    /// </summary>
    public class ShippedLevelsSimulationTests
    {
        private const int RunsPerLevel = 15;

        private static LevelCatalog LoadCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelCatalog");
            Assert.That(guids.Length, Is.GreaterThan(0), "No LevelCatalog asset found in the project.");

            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        [Test]
        public void EveryShippedLevel_SimulatesWithoutExceptionsOrFailedShuffles()
        {
            LevelCatalog catalog = LoadCatalog();
            Assert.That(catalog.Count, Is.GreaterThan(0));

            for (int i = 0; i < catalog.Count; i++)
            {
                LevelConfig config = catalog.Get(i).ToSimulationConfig();
                string name = "Level " + (i + 1) + " (" + catalog.Get(i).name + ")";

                SimulationReport random = null;
                SimulationReport greedy = null;
                Assert.DoesNotThrow(() => random = LevelSimulator.Simulate(config, new RandomBot(), RunsPerLevel, 1), name + " with the random bot");
                Assert.DoesNotThrow(() => greedy = LevelSimulator.Simulate(config, new GreedyBot(), RunsPerLevel, 1), name + " with the greedy bot");

                Assert.That(random.ShuffleFailures, Is.EqualTo(0), name + ": the random bot hit a failed shuffle");
                Assert.That(greedy.ShuffleFailures, Is.EqualTo(0), name + ": the greedy bot hit a failed shuffle");
            }
        }
    }
}
