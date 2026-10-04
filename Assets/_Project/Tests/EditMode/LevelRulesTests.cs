using System;
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
    }
}
