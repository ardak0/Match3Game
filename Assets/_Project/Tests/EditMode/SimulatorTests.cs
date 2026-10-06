using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    /// <summary>The simulator: it is deterministic, both bots play legal games, and the greedy bot is the better player.</summary>
    public class SimulatorTests
    {
        // A plain level without obstacles. Goals and moves decide how hard it is.
        private static LevelConfig Level(int moves, params GoalDefinition[] goals)
        {
            return new LevelConfig(8, 8, 5, moves, goals, null);
        }

        // 8x8, 5 colors, 14 moves, 20 red + 20 blue: the random bot often fails it, the greedy bot usually wins.
        private static LevelConfig MediumLevel()
        {
            return Level(14, new GoalDefinition(TileColor.Red, 20), new GoalDefinition(TileColor.Blue, 20));
        }

        // A bot that always plays the first valid move and records what it was asked. Lets a test look inside a run.
        private sealed class SpyBot : IBotPolicy
        {
            public int StartRunCalls;
            public int ChooseCalls;
            public int MoveCount;

            public string Name => "Spy";

            public void StartRun(IReadOnlyList<TileColor> colors, IRandom random)
            {
                StartRunCalls++;
            }

            public SwapMove ChooseMove(Board board, IReadOnlyList<SwapMove> validMoves, GoalTracker goals)
            {
                ChooseCalls++;
                MoveCount = validMoves.Count;
                return validMoves[0];
            }
        }

        private static void AssertSameReport(SimulationReport a, SimulationReport b)
        {
            Assert.That(b.Runs, Is.EqualTo(a.Runs));
            Assert.That(b.Wins, Is.EqualTo(a.Wins));
            Assert.That(b.ShuffleFailures, Is.EqualTo(a.ShuffleFailures));
            Assert.That(b.AverageMovesLeftOnWin, Is.EqualTo(a.AverageMovesLeftOnWin));
            Assert.That(b.MovesLeftP40, Is.EqualTo(a.MovesLeftP40));
            Assert.That(b.MovesLeftP75, Is.EqualTo(a.MovesLeftP75));
            Assert.That(b.AverageGoalProgressOnLoss, Is.EqualTo(a.AverageGoalProgressOnLoss));
        }

        // ---------- determinism ----------

        [Test]
        public void SameSeed_GivesTheSameReport_ForBothBots()
        {
            LevelConfig level = MediumLevel();

            AssertSameReport(
                LevelSimulator.Simulate(level, new RandomBot(), 40, 7),
                LevelSimulator.Simulate(level, new RandomBot(), 40, 7));

            AssertSameReport(
                LevelSimulator.Simulate(level, new GreedyBot(), 40, 7),
                LevelSimulator.Simulate(level, new GreedyBot(), 40, 7));
        }

        [Test]
        public void ReusingOneBotObject_DoesNotChangeTheResult()
        {
            // StartRun resets the bot, so nothing is carried over from an earlier simulation.
            LevelConfig level = MediumLevel();
            GreedyBot bot = new GreedyBot();

            SimulationReport first = LevelSimulator.Simulate(level, bot, 30, 11);
            SimulationReport second = LevelSimulator.Simulate(level, bot, 30, 11);

            AssertSameReport(first, second);
        }

        [Test]
        public void EachRunDependsOnlyOnTheSeedAndItsIndex()
        {
            // Run 5 gives the same result whether runs 0-4 were played before it or not.
            LevelConfig level = MediumLevel();

            for (int i = 0; i < 5; i++) LevelSimulator.SimulateRun(level, new GreedyBot(), 3, i);
            RunResult afterOthers = LevelSimulator.SimulateRun(level, new GreedyBot(), 3, 5);
            RunResult alone = LevelSimulator.SimulateRun(level, new GreedyBot(), 3, 5);

            Assert.That(afterOthers.IsWin, Is.EqualTo(alone.IsWin));
            Assert.That(afterOthers.MovesLeft, Is.EqualTo(alone.MovesLeft));
            Assert.That(afterOthers.GoalProgress, Is.EqualTo(alone.GoalProgress));
        }

        [Test]
        public void DifferentSeeds_PlayDifferentGames()
        {
            LevelConfig level = MediumLevel();

            SimulationReport a = LevelSimulator.Simulate(level, new RandomBot(), 60, 1);
            SimulationReport b = LevelSimulator.Simulate(level, new RandomBot(), 60, 2);

            bool same = a.Wins == b.Wins && a.AverageGoalProgressOnLoss == b.AverageGoalProgressOnLoss;
            Assert.That(same, Is.False);
        }

        [Test]
        public void SimulateIsTheSameAsPlayingTheRunsOneByOne()
        {
            // The Level Editor plays run by run (for its progress bar); the numbers must match the all-at-once call.
            LevelConfig level = MediumLevel();
            List<RunResult> runs = new List<RunResult>();
            for (int i = 0; i < 25; i++) runs.Add(LevelSimulator.SimulateRun(level, new RandomBot(), 9, i));

            AssertSameReport(SimulationReport.FromRuns("Random", runs), LevelSimulator.Simulate(level, new RandomBot(), 25, 9));
        }

        // ---------- what a run does ----------

        [Test]
        public void ARunNeverUsesMoreMovesThanTheLimit_AndAskedThePolicyOncePerMove()
        {
            // 500 reds can not be cleared in 3 moves, so every run is lost after exactly 3 moves.
            LevelConfig level = Level(3, new GoalDefinition(TileColor.Red, 500));
            SpyBot spy = new SpyBot();

            SimulationReport report = LevelSimulator.Simulate(level, spy, 5, 21);

            Assert.That(report.Runs, Is.EqualTo(5));
            Assert.That(report.Wins, Is.EqualTo(0));
            Assert.That(spy.StartRunCalls, Is.EqualTo(5));
            Assert.That(spy.ChooseCalls, Is.EqualTo(15)); // 5 runs x 3 moves
            Assert.That(spy.MoveCount, Is.GreaterThan(0));
        }

        [Test]
        public void ALostRun_ReportsProgressBetweenZeroAndOne()
        {
            LevelConfig level = Level(3, new GoalDefinition(TileColor.Red, 500));

            for (int i = 0; i < 10; i++)
            {
                RunResult run = LevelSimulator.SimulateRun(level, new GreedyBot(), 5, i);

                Assert.That(run.IsWin, Is.False);
                Assert.That(run.GoalProgress, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(run.GoalProgress, Is.LessThan(1.0));
            }
        }

        [Test]
        public void AWin_LeavesBetweenZeroAndLimitMinusOneMovesLeft()
        {
            // One red tile is cleared by nearly any move, so this level is won in the first moves.
            LevelConfig level = Level(10, new GoalDefinition(TileColor.Red, 1), new GoalDefinition(TileColor.Green, 1));

            for (int i = 0; i < 20; i++)
            {
                RunResult run = LevelSimulator.SimulateRun(level, new GreedyBot(), 5, i);

                Assert.That(run.IsWin, Is.True);
                Assert.That(run.MovesLeft, Is.InRange(0, 9)); // at least one move was used
            }
        }

        [Test]
        public void AnInvalidObstacleLayout_ThrowsInsteadOfPlayingARandomBoard()
        {
            LevelConfig level = new LevelConfig(8, 8, 5, 10, new[] { new GoalDefinition(TileColor.Red, 5) }, new[] { "C" });

            Assert.Throws<System.ArgumentException>(() => LevelSimulator.SimulateRun(level, new RandomBot(), 1, 0));
        }

        [Test]
        public void ObstacleLevels_ArePlayedWithoutProblems()
        {
            string[] rows = { "........", "........", "........", "...CC...", "...II...", "........", "..L..L..", "........" };
            LevelConfig level = new LevelConfig(8, 8, 5, 12,
                new[] { GoalDefinition.ForObstacle(ObstacleType.Crate, 2), GoalDefinition.ForObstacle(ObstacleType.Chain, 2) }, rows);

            SimulationReport random = LevelSimulator.Simulate(level, new RandomBot(), 20, 4);
            SimulationReport greedy = LevelSimulator.Simulate(level, new GreedyBot(), 20, 4);

            Assert.That(random.ShuffleFailures, Is.EqualTo(0));
            Assert.That(greedy.ShuffleFailures, Is.EqualTo(0));
        }

        // ---------- the two bots ----------

        [Test]
        public void Greedy_BeatsRandom_OnAnEasyLevel()
        {
            LevelConfig level = MediumLevel();

            SimulationReport random = LevelSimulator.Simulate(level, new RandomBot(), 100, 2024);
            SimulationReport greedy = LevelSimulator.Simulate(level, new GreedyBot(), 100, 2024);

            Assert.That(greedy.WinRate, Is.GreaterThan(random.WinRate + 0.2),
                "greedy " + greedy.WinRate + " vs random " + random.WinRate);
        }

        [Test]
        public void Greedy_AlsoWorksTowardsObstacleGoals()
        {
            // Only crates count here. A bot that ignores the goals (random) should clear fewer of them than one that aims for them.
            string[] rows = { "........", "........", "........", "..C..C..", "........", "..C..C..", "........", "........" };
            LevelConfig level = new LevelConfig(8, 8, 5, 8, new[] { GoalDefinition.ForObstacle(ObstacleType.Crate, 4) }, rows);

            SimulationReport random = LevelSimulator.Simulate(level, new RandomBot(), 100, 5);
            SimulationReport greedy = LevelSimulator.Simulate(level, new GreedyBot(), 100, 5);

            Assert.That(greedy.WinRate, Is.GreaterThan(random.WinRate),
                "greedy " + greedy.WinRate + " vs random " + random.WinRate);
        }

        [Test]
        public void TheBotsHaveTheirNamesInTheReport()
        {
            LevelConfig level = Level(3, new GoalDefinition(TileColor.Red, 500));

            Assert.That(LevelSimulator.Simulate(level, new RandomBot(), 2, 1).PolicyName, Is.EqualTo("Random"));
            Assert.That(LevelSimulator.Simulate(level, new GreedyBot(), 2, 1).PolicyName, Is.EqualTo("Greedy"));
        }

        [Test]
        public void ZeroRuns_GiveAnEmptyReport()
        {
            SimulationReport report = LevelSimulator.Simulate(MediumLevel(), new RandomBot(), 0, 1);

            Assert.That(report.Runs, Is.EqualTo(0));
        }
    }
}
