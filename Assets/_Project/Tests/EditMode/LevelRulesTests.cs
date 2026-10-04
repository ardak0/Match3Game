using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Game;
using NUnit.Framework;

namespace Match3.Tests
{
    public class LevelRulesTests
    {
        private static ResolveResult ClearedRed(int count)
        {
            int[] clearedByColor = new int[Enum.GetValues(typeof(TileColor)).Length];
            clearedByColor[(int)TileColor.Red] = count;
            return new ResolveResult(new ResolveStep[0], clearedByColor, 1);
        }

        private static GoalTracker RedGoal(int count) => new GoalTracker(new[] { new GoalDefinition(TileColor.Red, count) });

        [Test]
        public void GoalsLeftAndMovesLeft_LevelContinues()
        {
            Assert.That(LevelRules.GetOutcome(RedGoal(5), new MoveCounter(3)), Is.EqualTo(LevelOutcome.Continue));
        }

        [Test]
        public void AllGoalsMet_IsAWin()
        {
            GoalTracker goals = RedGoal(5);
            goals.Register(ClearedRed(5));

            Assert.That(LevelRules.GetOutcome(goals, new MoveCounter(3)), Is.EqualTo(LevelOutcome.Won));
        }

        [Test]
        public void GoalsLeftAndNoMovesLeft_IsALoss()
        {
            MoveCounter moves = new MoveCounter(1);
            moves.UseMove();

            Assert.That(LevelRules.GetOutcome(RedGoal(5), moves), Is.EqualTo(LevelOutcome.Lost));
        }

        [Test]
        public void FinishingTheGoalsWithTheLastMove_IsAWin_NotALoss()
        {
            GoalTracker goals = RedGoal(5);
            MoveCounter moves = new MoveCounter(1);
            moves.UseMove();
            goals.Register(ClearedRed(6));

            Assert.That(LevelRules.GetOutcome(goals, moves), Is.EqualTo(LevelOutcome.Won));
        }

        // ---------- is a level playable? ----------

        private static GoalDefinition[] RedGoals(int count) => new[] { new GoalDefinition(TileColor.Red, count) };

        [Test]
        public void AValidLevel_HasNoProblem()
        {
            Assert.That(LevelRules.GetLevelProblem(8, 8, 5, 20, RedGoals(10)), Is.Null);
        }

        [Test]
        public void TheSmallestAndLargestAllowedLevels_AreValid()
        {
            Assert.That(LevelRules.GetLevelProblem(LevelRules.MinBoardSize, LevelRules.MinBoardSize, LevelRules.MinColors, 1, RedGoals(1)), Is.Null);
            Assert.That(LevelRules.GetLevelProblem(LevelRules.MaxWidth, LevelRules.MaxHeight, LevelRules.MaxColors, 99, RedGoals(99)), Is.Null);
        }

        [Test]
        public void ABoardThatIsTooSmallOrTooBig_IsAProblem()
        {
            Assert.That(LevelRules.GetLevelProblem(3, 8, 5, 20, RedGoals(10)), Does.Contain("Width"));
            Assert.That(LevelRules.GetLevelProblem(11, 8, 5, 20, RedGoals(10)), Does.Contain("Width"));
            Assert.That(LevelRules.GetLevelProblem(8, 3, 5, 20, RedGoals(10)), Does.Contain("Height"));
            Assert.That(LevelRules.GetLevelProblem(8, 13, 5, 20, RedGoals(10)), Does.Contain("Height"));
        }

        [Test]
        public void ATooSmallOrTooLargeColorCount_IsAProblem()
        {
            Assert.That(LevelRules.GetLevelProblem(8, 8, 3, 20, RedGoals(10)), Does.Contain("Color count"));
            Assert.That(LevelRules.GetLevelProblem(8, 8, 7, 20, RedGoals(10)), Does.Contain("Color count"));
        }

        [Test]
        public void ANoMoveLevel_IsAProblem()
        {
            Assert.That(LevelRules.GetLevelProblem(8, 8, 5, 0, RedGoals(10)), Does.Contain("move limit"));
        }

        [Test]
        public void ALevelWithoutGoals_IsAProblem()
        {
            Assert.That(LevelRules.GetLevelProblem(8, 8, 5, 20, new GoalDefinition[0]), Does.Contain("at least one goal"));
            Assert.That(LevelRules.GetLevelProblem(8, 8, 5, 20, null), Does.Contain("at least one goal"));
        }

        [Test]
        public void AGoalForNothing_IsAProblem()
        {
            Assert.That(LevelRules.GetLevelProblem(8, 8, 5, 20, RedGoals(0)), Does.Contain("Goal 1"));
        }

        [Test]
        public void AGoalForAColorThatIsNotOnTheBoard_IsAProblem()
        {
            // Five colors means Red..Purple. Orange is the sixth, so it can never be cleared.
            List<GoalDefinition> goals = new List<GoalDefinition>
            {
                new GoalDefinition(TileColor.Red, 5),
                new GoalDefinition(TileColor.Orange, 5)
            };

            string problem = LevelRules.GetLevelProblem(8, 8, 5, 20, goals);

            Assert.That(problem, Does.Contain("Goal 2"));
            Assert.That(problem, Does.Contain("Orange"));
        }

        [Test]
        public void TheSameGoalColor_BecomesValidWhenTheLevelUsesMoreColors()
        {
            GoalDefinition[] goals = { new GoalDefinition(TileColor.Orange, 5) };

            Assert.That(LevelRules.GetLevelProblem(8, 8, 5, 20, goals), Is.Not.Null);
            Assert.That(LevelRules.GetLevelProblem(8, 8, 6, 20, goals), Is.Null);
        }
    }
}
