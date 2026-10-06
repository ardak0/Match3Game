using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    /// <summary>The math of SimulationReport, checked on small hand-made lists of runs (no board involved).</summary>
    public class SimulationReportTests
    {
        private static SimulationReport Report(params RunResult[] runs)
        {
            return SimulationReport.FromRuns("Test", new List<RunResult>(runs));
        }

        [Test]
        public void WinRate_IsWinsDividedByRuns()
        {
            SimulationReport report = Report(
                RunResult.Won(5), RunResult.Won(3), RunResult.Won(1),
                RunResult.Lost(0.5), RunResult.Lost(0.2));

            Assert.That(report.Runs, Is.EqualTo(5));
            Assert.That(report.Wins, Is.EqualTo(3));
            Assert.That(report.Losses, Is.EqualTo(2));
            Assert.That(report.WinRate, Is.EqualTo(0.6).Within(1e-9));
        }

        [Test]
        public void AverageMovesLeft_CountsWinsOnly()
        {
            SimulationReport report = Report(RunResult.Won(10), RunResult.Won(2), RunResult.Lost(0.9));

            Assert.That(report.AverageMovesLeftOnWin, Is.EqualTo(6.0).Within(1e-9));
        }

        [Test]
        public void Percentiles_AreTakenFromTheSortedMovesLeftOfTheWins()
        {
            // Sorted: 2 4 6 8 10. The 40th percentile is the value at index (int)(0.4 * 5) = 2, the 75th at (int)(0.75 * 5) = 3.
            SimulationReport report = Report(
                RunResult.Won(10), RunResult.Won(2), RunResult.Won(6), RunResult.Won(4), RunResult.Won(8),
                RunResult.Lost(0.1), RunResult.Lost(0.1));

            Assert.That(report.MovesLeftP40, Is.EqualTo(6));
            Assert.That(report.MovesLeftP75, Is.EqualTo(8));
        }

        [Test]
        public void Percentiles_OfASingleWin_AreThatWin()
        {
            SimulationReport report = Report(RunResult.Won(7));

            Assert.That(report.MovesLeftP40, Is.EqualTo(7));
            Assert.That(report.MovesLeftP75, Is.EqualTo(7));
        }

        [Test]
        public void GoalProgressOnLoss_IsTheAverageOverTheLostRunsOnly()
        {
            SimulationReport report = Report(RunResult.Won(4), RunResult.Lost(0.5), RunResult.Lost(0.25));

            Assert.That(report.AverageGoalProgressOnLoss, Is.EqualTo(0.375).Within(1e-9));
        }

        [Test]
        public void ShuffleFailures_AreCounted_AndCountAsLosses()
        {
            SimulationReport report = Report(RunResult.Won(3), RunResult.Lost(0.4, shuffleFailed: true), RunResult.Lost(0.4));

            Assert.That(report.ShuffleFailures, Is.EqualTo(1));
            Assert.That(report.Losses, Is.EqualTo(2));
        }

        [Test]
        public void NoWins_GivesZerosInsteadOfFailing()
        {
            SimulationReport report = Report(RunResult.Lost(0.3), RunResult.Lost(0.1));

            Assert.That(report.HasWins, Is.False);
            Assert.That(report.WinRate, Is.EqualTo(0));
            Assert.That(report.AverageMovesLeftOnWin, Is.EqualTo(0));
            Assert.That(report.MovesLeftP40, Is.EqualTo(0));
            Assert.That(report.MovesLeftP75, Is.EqualTo(0));
        }

        [Test]
        public void NoLosses_GivesZeroGoalProgress()
        {
            SimulationReport report = Report(RunResult.Won(3), RunResult.Won(5));

            Assert.That(report.Losses, Is.EqualTo(0));
            Assert.That(report.AverageGoalProgressOnLoss, Is.EqualTo(0));
        }

        [Test]
        public void NoRunsAtAll_IsAnEmptyReport()
        {
            SimulationReport report = SimulationReport.FromRuns("Test", new List<RunResult>());

            Assert.That(report.Runs, Is.EqualTo(0));
            Assert.That(report.WinRate, Is.EqualTo(0));
        }

        [Test]
        public void TheReportRemembersThePolicyAndWhetherItWasCancelled()
        {
            SimulationReport report = SimulationReport.FromRuns("Greedy", new List<RunResult> { RunResult.Won(1) }, cancelled: true);

            Assert.That(report.PolicyName, Is.EqualTo("Greedy"));
            Assert.That(report.Cancelled, Is.True);
        }

        [Test]
        public void ARunResult_KeepsWhatItWasGiven()
        {
            RunResult win = RunResult.Won(9);
            RunResult loss = RunResult.Lost(0.75, shuffleFailed: true);

            Assert.That(win.IsWin, Is.True);
            Assert.That(win.MovesLeft, Is.EqualTo(9));
            Assert.That(win.GoalProgress, Is.EqualTo(1.0));
            Assert.That(loss.IsWin, Is.False);
            Assert.That(loss.MovesLeft, Is.EqualTo(0));
            Assert.That(loss.GoalProgress, Is.EqualTo(0.75));
            Assert.That(loss.ShuffleFailed, Is.True);
        }
    }
}
