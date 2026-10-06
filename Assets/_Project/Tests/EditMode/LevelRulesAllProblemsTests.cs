using System.Collections.Generic;
using Match3.Core;
using Match3.Game;
using NUnit.Framework;

namespace Match3.Tests
{
    /// <summary>GetAllProblems is what the Level Editor shows: every problem of a level at once, not just the first.</summary>
    public class LevelRulesAllProblemsTests
    {
        private static readonly GoalDefinition[] RedGoal = { new GoalDefinition(TileColor.Red, 10) };

        [Test]
        public void AValidLevel_HasNoProblems()
        {
            List<string> problems = LevelRules.GetAllProblems(8, 8, 5, 20, RedGoal, new string[0], 8, 12);

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void EveryKindOfProblem_IsReportedAtTheSameTime()
        {
            // Too few moves for the star numbers, and a goal color that is not on the board.
            GoalDefinition[] goals = { new GoalDefinition(TileColor.Orange, 10) };

            List<string> problems = LevelRules.GetAllProblems(8, 8, 4, 5, goals, new string[0], 8, 12);

            Assert.That(problems.Count, Is.EqualTo(2));
            Assert.That(problems[0], Does.Contain("Orange"));
            Assert.That(problems[1], Does.Contain("3 stars"));
        }

        [Test]
        public void ABadObstacleLayout_IsReported()
        {
            string[] rows = { "C", "." }; // wrong size for an 8x8 board

            List<string> problems = LevelRules.GetAllProblems(8, 8, 5, 20, RedGoal, rows, 8, 12);

            Assert.That(problems.Count, Is.EqualTo(1));
            Assert.That(problems[0], Does.StartWith("Obstacle layout"));
        }

        [Test]
        public void AnUnfillableCell_IsReported()
        {
            // The cell under the left crate has the wall on its upper-left and the other crate on its upper-right,
            // so nothing could ever fall into it.
            string[] rows = { "CC..", "....", "....", "...." };

            List<string> problems = LevelRules.GetAllProblems(4, 4, 5, 20, RedGoal, rows, 8, 12);

            Assert.That(problems.Count, Is.EqualTo(1));
            Assert.That(problems[0], Does.Contain("nothing could ever fill"));
        }

        [Test]
        public void TheFirstProblemIsTheSameOneLevelDataWouldReport()
        {
            List<string> problems = LevelRules.GetAllProblems(8, 8, 5, 0, RedGoal, new string[0], 8, 12);

            Assert.That(problems[0], Is.EqualTo(LevelRules.GetLevelProblem(8, 8, 5, 0, RedGoal)));
        }
    }
}
