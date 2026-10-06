using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// The summary of many simulated runs of one level with one bot policy: how often it was won,
    /// how many moves were left on a win, and how far the goals got when it was lost.
    /// All the math is here and works on a plain list of RunResults, so it is easy to test by hand.
    /// </summary>
    public sealed class SimulationReport
    {
        private SimulationReport() { }

        public string PolicyName { get; private set; }
        public int Runs { get; private set; }
        public int Wins { get; private set; }
        public int Losses => Runs - Wins;

        /// <summary>Runs that ended because no move existed and the shuffle failed (they count as losses).</summary>
        public int ShuffleFailures { get; private set; }

        /// <summary>True if the simulation was stopped early; the report then covers only the runs that finished.</summary>
        public bool Cancelled { get; private set; }

        public bool HasWins => Wins > 0;

        /// <summary>Wins divided by runs, from 0 to 1. 0 if there were no runs.</summary>
        public double WinRate { get; private set; }

        /// <summary>Average of the moves left over the WON runs only. 0 if nothing was won.</summary>
        public double AverageMovesLeftOnWin { get; private set; }

        /// <summary>Moves left that 40% of the wins stayed under (0 if nothing was won). Used for the 2-star suggestion.</summary>
        public int MovesLeftP40 { get; private set; }

        /// <summary>Moves left that 75% of the wins stayed under (0 if nothing was won). Used for the 3-star suggestion.</summary>
        public int MovesLeftP75 { get; private set; }

        /// <summary>Average goal progress (0 to 1) over the LOST runs only. 0 if nothing was lost.</summary>
        public double AverageGoalProgressOnLoss { get; private set; }

        public static SimulationReport FromRuns(string policyName, IReadOnlyList<RunResult> runs, bool cancelled = false)
        {
            SimulationReport report = new SimulationReport
            {
                PolicyName = policyName,
                Runs = runs.Count,
                Cancelled = cancelled
            };

            List<int> movesLeftOfWins = new List<int>();
            double movesLeftSum = 0;
            double lossProgressSum = 0;

            for (int i = 0; i < runs.Count; i++)
            {
                RunResult run = runs[i];

                if (run.IsWin)
                {
                    movesLeftOfWins.Add(run.MovesLeft);
                    movesLeftSum += run.MovesLeft;
                }
                else
                {
                    lossProgressSum += run.GoalProgress;
                }

                if (run.ShuffleFailed) report.ShuffleFailures++;
            }

            report.Wins = movesLeftOfWins.Count;

            if (report.Runs > 0) report.WinRate = (double)report.Wins / report.Runs;
            if (report.Wins > 0) report.AverageMovesLeftOnWin = movesLeftSum / report.Wins;
            if (report.Losses > 0) report.AverageGoalProgressOnLoss = lossProgressSum / report.Losses;

            movesLeftOfWins.Sort();
            report.MovesLeftP40 = Percentile(movesLeftOfWins, 0.40);
            report.MovesLeftP75 = Percentile(movesLeftOfWins, 0.75);

            return report;
        }

        // The value at index (int)(fraction * count) of the sorted list (the last one at most). 0 for an empty list.
        private static int Percentile(List<int> sorted, double fraction)
        {
            if (sorted.Count == 0) return 0;

            int index = Math.Min(sorted.Count - 1, (int)(fraction * sorted.Count));
            return sorted[index];
        }
    }
}
