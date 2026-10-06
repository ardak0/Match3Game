using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    /// <summary>The difficulty report text: its table rows, number formats, and the cases with no wins.</summary>
    public class DifficultyReportBuilderTests
    {
        private static SimulationReport Report(string name, int wins, int losses, int movesLeftOfEveryWin = 4)
        {
            List<RunResult> runs = new List<RunResult>();
            for (int i = 0; i < wins; i++) runs.Add(RunResult.Won(movesLeftOfEveryWin));
            for (int i = 0; i < losses; i++) runs.Add(RunResult.Lost(0.5));
            return SimulationReport.FromRuns(name, runs);
        }

        private static LevelConfig PlainLevel()
        {
            return new LevelConfig(8, 9, 5, 20,
                new[] { new GoalDefinition(TileColor.Red, 12), new GoalDefinition(TileColor.Blue, 10) }, null);
        }

        [Test]
        public void ARow_DescribesTheLevelAndRatesItFromTheGreedyWinRate()
        {
            LevelReportRow row = LevelReportRow.Create(3, PlainLevel(), 8, 12, Report("Random", 3, 7), Report("Greedy", 9, 1));

            Assert.That(row.LevelNumber, Is.EqualTo(3));
            Assert.That(row.GoalsText, Is.EqualTo("Red 12, Blue 10"));
            Assert.That(row.ObstaclesText, Is.EqualTo("-"));
            Assert.That(row.Rating, Is.EqualTo(DifficultyRating.Easy)); // greedy 90%
        }

        [Test]
        public void Goals_IncludeObstacleGoals()
        {
            GoalDefinition[] goals = { new GoalDefinition(TileColor.Green, 5), GoalDefinition.ForObstacle(ObstacleType.Ice, 7) };

            Assert.That(LevelReportRow.DescribeGoals(goals), Is.EqualTo("Green 5, Ice 7"));
        }

        [Test]
        public void Obstacles_AreCountedPerType_WhateverTheirHp()
        {
            string[] rows = { "C...", "D.I.", "J..L", "...." };
            LevelConfig level = new LevelConfig(4, 4, 5, 10, new[] { new GoalDefinition(TileColor.Red, 5) }, rows);

            Assert.That(LevelReportRow.DescribeObstacles(level), Is.EqualTo("2 crate, 2 ice, 1 chain"));
        }

        [Test]
        public void ARow_SuggestsStarsFromTheGreedyWins()
        {
            LevelReportRow row = LevelReportRow.Create(1, PlainLevel(), 8, 12, Report("Random", 1, 1), Report("Greedy", 5, 0, 6));

            Assert.That(row.HasSuggestion, Is.True);
            Assert.That(row.SuggestedTwoStar, Is.EqualTo(6));
            Assert.That(row.SuggestedThreeStar, Is.EqualTo(7)); // all wins left 6, but 3 stars must be more than 2 stars
        }

        [Test]
        public void ALevelTheGreedyBotNeverWins_HasNoSuggestion()
        {
            LevelReportRow row = LevelReportRow.Create(1, PlainLevel(), 8, 12, Report("Random", 0, 5), Report("Greedy", 0, 5));

            Assert.That(row.HasSuggestion, Is.False);
            Assert.That(row.Rating, Is.EqualTo(DifficultyRating.VeryHard));
        }

        [Test]
        public void Percent_IsRoundedToWholeNumbers()
        {
            Assert.That(DifficultyReportBuilder.Percent(0.294), Is.EqualTo("29%"));
            Assert.That(DifficultyReportBuilder.Percent(0.0), Is.EqualTo("0%"));
            Assert.That(DifficultyReportBuilder.Percent(1.0), Is.EqualTo("100%"));
        }

        [Test]
        public void TheMarkdown_HasOneTableRowPerLevel()
        {
            List<LevelReportRow> rows = new List<LevelReportRow>
            {
                LevelReportRow.Create(1, PlainLevel(), 8, 12, Report("Random", 4, 6), Report("Greedy", 8, 2, 5)),
                LevelReportRow.Create(2, PlainLevel(), 8, 12, Report("Random", 0, 10), Report("Greedy", 0, 10))
            };

            string markdown = DifficultyReportBuilder.BuildMarkdown(rows, 10, 99);
            string[] lines = markdown.Split('\n');

            List<string> tableRows = new List<string>();
            foreach (string line in lines)
            {
                if (line.StartsWith("| 1 |") || line.StartsWith("| 2 |")) tableRows.Add(line);
            }

            Assert.That(markdown, Does.StartWith("# Difficulty report"));
            Assert.That(markdown, Does.Contain("| Level | Size | Colors | Moves | Goals | Obstacles | Random win | Greedy win | Avg moves left | Rating | Stars now | Stars suggested |"));
            Assert.That(tableRows.Count, Is.EqualTo(2));
            Assert.That(tableRows[0], Is.EqualTo("| 1 | 8x9 | 5 | 20 | Red 12, Blue 10 | - | 40% | 80% | 5.0 | Medium | 8/12 | 5/6 |"));
            Assert.That(tableRows[1], Is.EqualTo("| 2 | 8x9 | 5 | 20 | Red 12, Blue 10 | - | 0% | 0% | n/a | Very Hard | 8/12 | n/a |"));
        }

        [Test]
        public void TheMarkdown_MentionsTheRunsAndTheSeed_ButNoDate()
        {
            string markdown = DifficultyReportBuilder.BuildMarkdown(new List<LevelReportRow>(), 400, 1234);

            Assert.That(markdown, Does.Contain("400 times"));
            Assert.That(markdown, Does.Contain("base seed 1234"));
            Assert.That(markdown, Does.Not.Contain("202")); // no year: the same input always gives the same file
        }

        [Test]
        public void TheMarkdown_IsTheSameEveryTime()
        {
            List<LevelReportRow> rows = new List<LevelReportRow>
            {
                LevelReportRow.Create(1, PlainLevel(), 8, 12, Report("Random", 4, 6), Report("Greedy", 8, 2))
            };

            Assert.That(DifficultyReportBuilder.BuildMarkdown(rows, 10, 1), Is.EqualTo(DifficultyReportBuilder.BuildMarkdown(rows, 10, 1)));
        }
    }
}
