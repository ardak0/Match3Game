using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Game;
using NUnit.Framework;
using static Match3.Tests.ObstacleTestHelpers;

namespace Match3.Tests
{
    public class ObstacleGoalTests
    {
        private static readonly int ColorCount = Enum.GetValues(typeof(TileColor)).Length;

        // A result made by hand from steps: the goals only look at the color counts and at the obstacle steps.
        private static ResolveResult ResultOf(int redCleared, params ResolveStep[] steps)
        {
            int[] clearedByColor = new int[ColorCount];
            clearedByColor[(int)TileColor.Red] = redCleared;
            return new ResolveResult(steps, clearedByColor, 1);
        }

        private static ObstacleDestroyedStep Destroyed(ObstacleType type) => new ObstacleDestroyedStep(1, type, new GridPos(0, 0));

        [Test]
        public void ColorGoal_IsTheDefault_AndKeepsItsOldConstructor()
        {
            GoalDefinition old = new GoalDefinition(TileColor.Blue, 5);
            GoalDefinition untouched = default;

            Assert.That(old.kind, Is.EqualTo(GoalKind.CollectColor));
            Assert.That(old.color, Is.EqualTo(TileColor.Blue));
            Assert.That(old.count, Is.EqualTo(5));
            Assert.That(untouched.kind, Is.EqualTo(GoalKind.CollectColor), "A saved level without the new field must stay a color goal.");
        }

        [Test]
        public void ObstacleGoal_HasItsKindAndType()
        {
            GoalDefinition goal = GoalDefinition.ForObstacle(ObstacleType.Crate, 4);

            Assert.That(goal.kind, Is.EqualTo(GoalKind.ClearObstacle));
            Assert.That(goal.obstacle, Is.EqualTo(ObstacleType.Crate));
            Assert.That(goal.count, Is.EqualTo(4));
        }

        [Test]
        public void ResolveResult_CountsDestroyedObstaclesByType()
        {
            ResolveResult result = ResultOf(0,
                Destroyed(ObstacleType.Crate), Destroyed(ObstacleType.Crate), Destroyed(ObstacleType.Ice), Destroyed(ObstacleType.Chain));

            Assert.That(result.GetDestroyedCount(ObstacleType.Crate), Is.EqualTo(2));
            Assert.That(result.GetDestroyedCount(ObstacleType.Ice), Is.EqualTo(1));
            Assert.That(result.GetDestroyedCount(ObstacleType.Chain), Is.EqualTo(1));
            Assert.That(ResolveResult.Invalid.GetDestroyedCount(ObstacleType.Crate), Is.EqualTo(0));
        }

        [Test]
        public void Tracker_CountsOnlyDestroyedObstacles_NotDamagedOnes()
        {
            GoalTracker goals = new GoalTracker(new[] { GoalDefinition.ForObstacle(ObstacleType.Crate, 3) });

            goals.Register(ResultOf(0,
                new ObstacleDamagedStep(1, ObstacleType.Crate, new GridPos(0, 0), 1),
                Destroyed(ObstacleType.Crate),
                Destroyed(ObstacleType.Ice)));

            Assert.That(goals.GetRemaining(0), Is.EqualTo(2));
            Assert.That(goals.GetKind(0), Is.EqualTo(GoalKind.ClearObstacle));
            Assert.That(goals.GetObstacle(0), Is.EqualTo(ObstacleType.Crate));
        }

        [Test]
        public void Tracker_KeepsColorAndObstacleGoalsApart()
        {
            GoalTracker goals = new GoalTracker(new[]
            {
                new GoalDefinition(TileColor.Red, 5),
                GoalDefinition.ForObstacle(ObstacleType.Ice, 2),
                GoalDefinition.ForObstacle(ObstacleType.Chain, 1)
            });

            goals.Register(ResultOf(3, Destroyed(ObstacleType.Ice), Destroyed(ObstacleType.Crate)));

            Assert.That(goals.GetRemaining(0), Is.EqualTo(2));
            Assert.That(goals.GetRemaining(1), Is.EqualTo(1));
            Assert.That(goals.GetRemaining(2), Is.EqualTo(1));
        }

        [Test]
        public void Tracker_RaisesGoalChanged_ForAnObstacleGoal()
        {
            GoalTracker goals = new GoalTracker(new[] { GoalDefinition.ForObstacle(ObstacleType.Chain, 2) });
            List<string> events = new List<string>();
            goals.GoalChanged += (index, remaining) => events.Add(index + ":" + remaining);

            goals.Register(ResultOf(0, Destroyed(ObstacleType.Chain)));
            goals.Register(ResultOf(0)); // nothing for this goal: no event

            Assert.That(events, Is.EqualTo(new[] { "0:1" }));
        }

        [Test]
        public void Level_IsWon_OnlyWhenTheColorGoalAndTheObstacleGoalAreBothDone()
        {
            GoalTracker goals = new GoalTracker(new[]
            {
                new GoalDefinition(TileColor.Red, 3),
                GoalDefinition.ForObstacle(ObstacleType.Crate, 1)
            });
            MoveCounter moves = new MoveCounter(10);

            goals.Register(ResultOf(3));
            Assert.That(LevelRules.GetOutcome(goals, moves), Is.EqualTo(LevelOutcome.Continue), "The crate is still standing.");

            goals.Register(ResultOf(0, Destroyed(ObstacleType.Crate)));
            Assert.That(LevelRules.GetOutcome(goals, moves), Is.EqualTo(LevelOutcome.Won));
        }

        [Test]
        public void Level_IsWon_WhenTheObstacleIsTheLastGoalAndTheLastMoveWasUsed()
        {
            GoalTracker goals = new GoalTracker(new[]
            {
                new GoalDefinition(TileColor.Red, 1),
                GoalDefinition.ForObstacle(ObstacleType.Ice, 1)
            });
            MoveCounter moves = new MoveCounter(1);
            moves.UseMove();

            goals.Register(ResultOf(1, Destroyed(ObstacleType.Ice)));

            Assert.That(LevelRules.GetOutcome(goals, moves), Is.EqualTo(LevelOutcome.Won), "Goals are checked before the moves.");
        }

        [Test]
        public void RealMove_ThatBreaksACrate_CountsForTheGoal()
        {
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "B.PRG",
                "RRGRP").WithObstacles(
                ".....",
                ".....",
                ".C...",
                ".....");
            GoalTracker goals = new GoalTracker(new[]
            {
                new GoalDefinition(TileColor.Red, 3),
                GoalDefinition.ForObstacle(ObstacleType.Crate, 1)
            });

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4).ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0));
            goals.Register(result);

            Assert.That(goals.GetRemaining(1), Is.EqualTo(0));
            Assert.That(goals.GetRemaining(0), Is.EqualTo(0));
            Assert.That(goals.AllGoalsMet, Is.True);
        }

        // ---------- checking a level with obstacles ----------

        [Test]
        public void LevelProblem_AnObstacleGoalNeedsAnObstacleType()
        {
            GoalDefinition broken = new GoalDefinition { kind = GoalKind.ClearObstacle, obstacle = ObstacleType.None, count = 3 };

            string problem = LevelRules.GetLevelProblem(8, 8, 5, 20, new[] { broken });

            Assert.That(problem, Does.Contain("Goal 1"));
            Assert.That(problem, Does.Contain("obstacle"));
        }

        [Test]
        public void LevelProblem_AColorGoalIsCheckedAsBefore()
        {
            Assert.That(LevelRules.GetLevelProblem(8, 8, 4, 20, new[] { new GoalDefinition(TileColor.Orange, 5) }), Does.Contain("Orange"));
            Assert.That(LevelRules.GetLevelProblem(8, 8, 4, 20, new[] { new GoalDefinition(TileColor.Red, 5) }), Is.Null);
            Assert.That(LevelRules.GetLevelProblem(8, 8, 4, 20, new[] { GoalDefinition.ForObstacle(ObstacleType.Crate, 2) }), Is.Null,
                "An obstacle goal does not depend on the color count.");
        }

        [Test]
        public void ObstacleProblem_NoRows_IsFine_WhenThereIsNoObstacleGoal()
        {
            Assert.That(LevelRules.GetObstacleProblem(8, 8, null, new[] { new GoalDefinition(TileColor.Red, 5) }), Is.Null);
            Assert.That(LevelRules.GetObstacleProblem(8, 8, new string[0], new[] { new GoalDefinition(TileColor.Red, 5) }), Is.Null);
        }

        [Test]
        public void ObstacleProblem_WrongSize_SaysWhatIsWrong()
        {
            string problem = LevelRules.GetObstacleProblem(4, 3, new[] { "....", "...." }, new[] { new GoalDefinition(TileColor.Red, 5) });

            Assert.That(problem, Does.Contain("Obstacle layout"));
            Assert.That(problem, Does.Contain("3 rows"));
        }

        [Test]
        public void ObstacleProblem_AGoalForMoreObstaclesThanTheLayoutHas_IsReported()
        {
            string[] rows = { "....", ".C..", "...." };

            string problem = LevelRules.GetObstacleProblem(4, 3, rows, new[] { GoalDefinition.ForObstacle(ObstacleType.Crate, 2) });

            Assert.That(problem, Does.Contain("Crate"));
            Assert.That(problem, Does.Contain("1"));
            Assert.That(LevelRules.GetObstacleProblem(4, 3, rows, new[] { GoalDefinition.ForObstacle(ObstacleType.Crate, 1) }), Is.Null);
        }

        [Test]
        public void ObstacleProblem_AGoalForAnObstacleTheLayoutLacks_IsReported()
        {
            string problem = LevelRules.GetObstacleProblem(4, 3, new[] { "....", "....", "...." },
                new[] { GoalDefinition.ForObstacle(ObstacleType.Ice, 1) });

            Assert.That(problem, Does.Contain("Ice"));
        }

        [Test]
        public void ObstacleProblem_ACellNothingCanFill_IsReported()
        {
            // The cell under the middle crate of ".CCC." is the one nothing can reach: row 2, column 3 counting from the top left.
            string problem = LevelRules.GetObstacleProblem(5, 3, new[] { ".CCC.", ".....", "....." }, new[] { new GoalDefinition(TileColor.Red, 5) });

            Assert.That(problem, Does.Contain("row 2, column 3"));
        }
    }
}
