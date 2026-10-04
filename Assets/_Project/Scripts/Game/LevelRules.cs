namespace Match3.Game
{
    public enum LevelOutcome
    {
        Continue,
        Won,
        Lost
    }

    /// <summary>
    /// The win/lose rule in one place, so it can be unit tested without any Unity objects.
    /// Goals are checked first: completing the last goal with the last move is a win, not a loss.
    /// </summary>
    public static class LevelRules
    {
        public static LevelOutcome GetOutcome(GoalTracker goals, MoveCounter moves)
        {
            if (goals.AllGoalsMet) return LevelOutcome.Won;
            if (!moves.HasMovesLeft) return LevelOutcome.Lost;
            return LevelOutcome.Continue;
        }
    }
}
