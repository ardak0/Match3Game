namespace Match3.Core
{
    /// <summary>The outcome of one simulated play-through of a level.</summary>
    public readonly struct RunResult
    {
        private RunResult(bool isWin, int movesLeft, double goalProgress, bool shuffleFailed)
        {
            IsWin = isWin;
            MovesLeft = movesLeft;
            GoalProgress = goalProgress;
            ShuffleFailed = shuffleFailed;
        }

        public bool IsWin { get; }

        /// <summary>Moves that were still unused when the last goal was met. 0 for a loss.</summary>
        public int MovesLeft { get; }

        /// <summary>How much of the goals was done at the end, from 0 to 1 (1 for a win).</summary>
        public double GoalProgress { get; }

        /// <summary>True if the run ended because the board had no move and could not be shuffled either.</summary>
        public bool ShuffleFailed { get; }

        public static RunResult Won(int movesLeft) => new RunResult(true, movesLeft, 1.0, false);

        public static RunResult Lost(double goalProgress, bool shuffleFailed = false) => new RunResult(false, 0, goalProgress, shuffleFailed);
    }
}
