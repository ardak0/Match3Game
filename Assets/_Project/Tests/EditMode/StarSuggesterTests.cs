using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    /// <summary>The difficulty label (from the greedy win rate) and the suggested star thresholds (from the moves-left percentiles).</summary>
    public class StarSuggesterTests
    {
        // A report whose wins have exactly these moves left (so the percentiles are easy to predict).
        private static SimulationReport ReportWithWins(params int[] movesLeft)
        {
            List<RunResult> runs = new List<RunResult>();
            foreach (int left in movesLeft) runs.Add(RunResult.Won(left));
            return SimulationReport.FromRuns("Greedy", runs);
        }

        // ---------- rating ----------

        [TestCase(1.00, DifficultyRating.Easy)]
        [TestCase(0.95, DifficultyRating.Easy)]
        [TestCase(0.81, DifficultyRating.Easy)]
        [TestCase(0.80, DifficultyRating.Medium)] // "Easy" is MORE than 80%
        [TestCase(0.65, DifficultyRating.Medium)]
        [TestCase(0.50, DifficultyRating.Medium)]
        [TestCase(0.49, DifficultyRating.Hard)]
        [TestCase(0.30, DifficultyRating.Hard)]
        [TestCase(0.20, DifficultyRating.Hard)]
        [TestCase(0.19, DifficultyRating.VeryHard)]
        [TestCase(0.00, DifficultyRating.VeryHard)]
        public void Rating_FollowsTheGreedyWinRate(double winRate, DifficultyRating expected)
        {
            Assert.That(DifficultyRater.FromGreedyWinRate(winRate), Is.EqualTo(expected));
        }

        [Test]
        public void Labels_AreReadableText()
        {
            Assert.That(DifficultyRater.GetLabel(DifficultyRating.Easy), Is.EqualTo("Easy"));
            Assert.That(DifficultyRater.GetLabel(DifficultyRating.Medium), Is.EqualTo("Medium"));
            Assert.That(DifficultyRater.GetLabel(DifficultyRating.Hard), Is.EqualTo("Hard"));
            Assert.That(DifficultyRater.GetLabel(DifficultyRating.VeryHard), Is.EqualTo("Very Hard"));
        }

        // ---------- suggested stars ----------

        [Test]
        public void Suggestion_IsThe40thAnd75thPercentileOfMovesLeft()
        {
            // Sorted 2 4 6 8 10: 40th percentile = 6, 75th = 8.
            bool ok = StarSuggester.TrySuggest(ReportWithWins(10, 2, 6, 4, 8), 24, out int two, out int three);

            Assert.That(ok, Is.True);
            Assert.That(two, Is.EqualTo(6));
            Assert.That(three, Is.EqualTo(8));
        }

        [Test]
        public void Suggestion_WithoutWins_IsNotPossible()
        {
            SimulationReport noWins = SimulationReport.FromRuns("Greedy", new List<RunResult> { RunResult.Lost(0.2) });

            Assert.That(StarSuggester.TrySuggest(noWins, 24, out _, out _), Is.False);
        }

        [Test]
        public void Suggestion_ThreeStarsIsAlwaysMoreThanTwoStars()
        {
            // Every win left exactly 5 moves: both percentiles are 5, but the rule needs three > two.
            bool ok = StarSuggester.TrySuggest(ReportWithWins(5, 5, 5, 5), 24, out int two, out int three);

            Assert.That(ok, Is.True);
            Assert.That(two, Is.EqualTo(5));
            Assert.That(three, Is.EqualTo(6));
        }

        [Test]
        public void Suggestion_TwoStarsNeedsAtLeastOneMoveLeft()
        {
            // The wins finished on the last move (0 moves left).
            bool ok = StarSuggester.TrySuggest(ReportWithWins(0, 0, 0, 0), 24, out int two, out int three);

            Assert.That(ok, Is.True);
            Assert.That(two, Is.EqualTo(1));
            Assert.That(three, Is.EqualTo(2));
        }

        [Test]
        public void Suggestion_StaysBelowTheMoveLimit()
        {
            // 10 moves in total: 3 stars may need at most 9 moves left, and 2 stars one less.
            bool ok = StarSuggester.TrySuggest(ReportWithWins(9, 9, 9, 9), 10, out int two, out int three);

            Assert.That(ok, Is.True);
            Assert.That(three, Is.EqualTo(9));
            Assert.That(two, Is.EqualTo(8));
        }

        [Test]
        public void Suggestion_AlwaysPassesTheGameStarRule()
        {
            // The rule LevelRules.GetStarProblem enforces: two >= 1, three > two, three < move limit.
            int[][] winSets = { new[] { 0 }, new[] { 1, 1 }, new[] { 3, 9, 4 }, new[] { 20, 21, 22 }, new[] { 2, 2, 2, 30 } };
            foreach (int[] wins in winSets)
            {
                foreach (int moveLimit in new[] { 3, 4, 10, 24 })
                {
                    bool ok = StarSuggester.TrySuggest(ReportWithWins(wins), moveLimit, out int two, out int three);

                    Assert.That(ok, Is.True, "move limit " + moveLimit);
                    Assert.That(two, Is.GreaterThanOrEqualTo(1));
                    Assert.That(three, Is.GreaterThan(two));
                    Assert.That(three, Is.LessThan(moveLimit));
                }
            }
        }

        [Test]
        public void Suggestion_IsNotPossibleWhenTheMoveLimitIsTooSmallForThreeDifferentStars()
        {
            // Needs two >= 1, three >= 2 and three < limit, so a limit of 2 can never work.
            Assert.That(StarSuggester.TrySuggest(ReportWithWins(1, 1), 2, out _, out _), Is.False);
        }
    }
}
