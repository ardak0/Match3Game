using System.Collections.Generic;
using Match3.Core;

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
    ///   GetObstacleProblem - is the obstacle layout of a level valid, and does it fit the goals?
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
                if (goals[i].count < 1) return "Goal " + (i + 1) + " must ask for at least one.";

                if (goals[i].kind == GoalKind.ClearObstacle)
                {
                    if (goals[i].obstacle == ObstacleType.None) return "Goal " + (i + 1) + " is an obstacle goal, so it needs an obstacle type (crate, ice or chain).";
                    continue;
                }

                if ((int)goals[i].color >= colorCount)
                {
                    return "Goal " + (i + 1) + " asks for " + goals[i].color + ", but only the first " + colorCount
                        + " colors are on the board.";
                }
            }

            return null;
        }

        /// <summary>
        /// Star thresholds are "moves left when the level is won". Returns null if they make sense,
        /// otherwise a sentence that says what is wrong.
        /// </summary>
        public static string GetStarProblem(int moveLimit, int twoStarMovesLeft, int threeStarMovesLeft)
        {
            if (twoStarMovesLeft < 1) return "2 stars needs at least 1 move left (it is " + twoStarMovesLeft + ").";

            if (threeStarMovesLeft <= twoStarMovesLeft)
            {
                return "3 stars must need more moves left than 2 stars (" + threeStarMovesLeft + " is not more than " + twoStarMovesLeft + ").";
            }

            if (threeStarMovesLeft >= moveLimit)
            {
                return "3 stars needs " + threeStarMovesLeft + " moves left, but a win uses at least one move, so it must stay below the move limit (" + moveLimit + ").";
            }

            return null;
        }

        /// <summary>
        /// Checks the obstacle layout (rows of letters, top row first). Null or no rows means "no obstacles" and is fine,
        /// unless a goal asks for obstacles. Returns null if the layout is valid, otherwise a sentence that says what is wrong:
        /// a wrong size or letter, a cell that nothing could ever fill, or an obstacle goal that asks for more
        /// obstacles than the layout has.
        /// </summary>
        public static string GetObstacleProblem(int width, int height, IReadOnlyList<string> rows, IReadOnlyList<GoalDefinition> goals)
        {
            if (!ObstacleLayout.TryParse(rows, width, height, out ObstacleLayout layout, out string error))
            {
                return "Obstacle layout " + error;
            }

            if (layout.TryFindUnfillableCell(out GridPos cell))
            {
                int row = height - cell.Y; // rows are written top first, and people count from 1
                int column = cell.X + 1;
                return "Obstacle layout: the cell at row " + row + ", column " + column
                    + " has a crate above it and crates (or the wall) on both diagonals above it, so nothing could ever fill it.";
            }

            if (goals == null) return null;

            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i].kind != GoalKind.ClearObstacle || goals[i].obstacle == ObstacleType.None) continue;

                int available = layout.Count(goals[i].obstacle);
                if (available < goals[i].count)
                {
                    return "Goal " + (i + 1) + " asks for " + goals[i].count + " " + goals[i].obstacle
                        + " but the obstacle layout has only " + available + ".";
                }
            }

            return null;
        }
    }
}
