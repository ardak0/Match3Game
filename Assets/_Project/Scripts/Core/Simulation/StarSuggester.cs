using System;

namespace Match3.Core
{
    /// <summary>
    /// Suggests star thresholds from a simulation (the method used by hand in M12): 2 stars at the 40th percentile of the moves
    /// left on a win, 3 stars at the 75th. So about 60% of wins reach 2 stars and about 25% reach 3.
    /// The numbers are then forced to obey the game's star rule (LevelRules.GetStarProblem):
    /// 2 stars needs at least 1 move left, 3 stars needs more than 2 stars, and 3 stars stays below the move limit.
    /// </summary>
    public static class StarSuggester
    {
        /// <summary>False if the report has no wins, or the move limit is too small for three different star levels (it must be at least 3).</summary>
        public static bool TrySuggest(SimulationReport report, int moveLimit, out int twoStarMovesLeft, out int threeStarMovesLeft)
        {
            twoStarMovesLeft = 0;
            threeStarMovesLeft = 0;

            if (!report.HasWins) return false;

            int two = Math.Max(1, report.MovesLeftP40);
            int three = Math.Max(two + 1, report.MovesLeftP75);

            three = Math.Min(three, moveLimit - 1); // a win uses at least one move
            two = Math.Min(two, three - 1);
            if (two < 1) return false;

            twoStarMovesLeft = two;
            threeStarMovesLeft = three;
            return true;
        }
    }
}
