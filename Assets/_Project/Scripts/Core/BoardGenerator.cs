using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Builds a starting board that has NO matches yet but at least one possible move.
    ///
    /// Trick: fill cells bottom row first, left to right. When we pick the color for a cell,
    /// only the two cells to its left and the two cells below it exist already.
    /// So we simply forbid any color that would make 3 in a row with those.
    /// </summary>
    public sealed class BoardGenerator
    {
        private const int MaxAttempts = 100;

        private readonly IRandom _random;
        private readonly MoveFinder _moveFinder = new MoveFinder(new MatchFinder());
        private readonly List<TileColor> _allowedColors = new List<TileColor>();

        public BoardGenerator(IRandom random)
        {
            _random = random;
        }

        /// <param name="colors">Colors to use. Needs at least 3 different ones, otherwise a match-free board can be impossible.</param>
        public Board Generate(int width, int height, IReadOnlyList<TileColor> colors)
        {
            if (colors == null || colors.Count < 3)
            {
                throw new ArgumentException("Need at least 3 colors to build a board without matches.", nameof(colors));
            }

            // A random fill can rarely leave a board with no possible move. Just try again.
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                Board board = Fill(width, height, colors);
                if (_moveFinder.HasPossibleMove(board))
                {
                    return board;
                }
            }

            throw new InvalidOperationException(
                "Could not generate a " + width + "x" + height + " board with a possible move in " + MaxAttempts + " attempts.");
        }

        private Board Fill(int width, int height, IReadOnlyList<TileColor> colors)
        {
            Board board = new Board(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    _allowedColors.Clear();
                    for (int i = 0; i < colors.Count; i++)
                    {
                        if (!WouldCompleteRun(board, x, y, colors[i]))
                        {
                            _allowedColors.Add(colors[i]);
                        }
                    }

                    if (_allowedColors.Count == 0)
                    {
                        throw new InvalidOperationException("No allowed color for cell (" + x + "," + y + "). Are the colors distinct?");
                    }

                    TileColor chosen = _allowedColors[_random.Next(0, _allowedColors.Count)];
                    board.Set(x, y, board.NewTile(chosen));
                }
            }

            return board;
        }

        // Cells fill left-to-right, bottom-to-top, so only the cells to the LEFT and BELOW exist yet.
        // A color is forbidden if the two cells on one of those sides already have it.
        private static bool WouldCompleteRun(Board board, int x, int y, TileColor color)
        {
            bool twoToTheLeft =
                x >= 2 &&
                board.Get(x - 1, y).Color == color &&
                board.Get(x - 2, y).Color == color;

            bool twoBelow =
                y >= 2 &&
                board.Get(x, y - 1).Color == color &&
                board.Get(x, y - 2).Color == color;

            return twoToTheLeft || twoBelow;
        }
    }
}
