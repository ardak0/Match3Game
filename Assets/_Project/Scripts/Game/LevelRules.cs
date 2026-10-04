using System.Collections.Generic;

namespace Match3.Game
{
    public enum LevelOutcome
    {
        Continue,
        Won,
        Lost
    }

    /// <summary>
    /// The rules about a level, in one place and free of Unity objects so they can be unit tested:
    ///   GetOutcome       - has the player won, lost, or is the level still going?
    ///   GetLevelProblem  - is a level setup playable at all?
    /// </summary>
    public static class LevelRules
    {
        public const int MinBoardSize = 4;
        public const int MaxWidth = 10;
        public const int MaxHeight = 12;
        public const int MinColors = 4;
        public const int MaxColors = 6; // the TileColor enum has six values

        /// <summary>
        /// Goals are checked first: completing the last goal with the last move is a win, not a loss.
        /// </summary>
        public static LevelOutcome GetOutcome(GoalTracker goals, MoveCounter moves)
        {
            if (goals.AllGoalsMet) return LevelOutcome.Won;
            if (!moves.HasMovesLeft) return LevelOutcome.Lost;
            return LevelOutcome.Continue;
        }

        /// <summary>
        /// Returns null if the level can be played, otherwise a sentence that says what is wrong.
        /// A level uses the first colorCount colors of TileColor, so a goal for a color beyond that could never be reached.
        /// </summary>
        public static string GetLevelProblem(int width, int height, int colorCount, int moveLimit, IReadOnlyList<GoalDefinition> goals)
        {
            if (width < MinBoardSize || width > MaxWidth)
            {
                return "Width must be between " + MinBoardSize + " and " + MaxWidth + " (it is " + width + ").";
            }

            if (height < MinBoardSize || height > MaxHeight)
            {
                return "Height must be between " + MinBoardSize + " and " + MaxHeight + " (it is " + height + ").";
            }

            if (colorCount < MinColors || colorCount > MaxColors)
            {
                return "Color count must be between " + MinColors + " and " + MaxColors + " (it is " + colorCount + ").";
            }

            if (moveLimit < 1) return "The move limit must be at least 1.";

            if (goals == null || goals.Count == 0) return "The level needs at least one goal.";

            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i].count < 1) return "Goal " + (i + 1) + " must ask for at least one tile.";

                if ((int)goals[i].color >= colorCount)
                {
                    return "Goal " + (i + 1) + " asks for " + goals[i].color + ", but only the first " + colorCount
                        + " colors are on the board.";
                }
            }

            return null;
        }
    }
}
