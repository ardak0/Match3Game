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
            return Generate(width, height, colors, null);
        }

        /// <summary>
        /// Same as above, and puts the obstacles of the layout on the board (null = none). Crate cells get no tile;
        /// every other cell gets one, including the cells of ice and chained tiles.
        /// </summary>
        public Board Generate(int width, int height, IReadOnlyList<TileColor> colors, ObstacleLayout layout)
        {
            if (colors == null || colors.Count < 3)
            {
                throw new ArgumentException("Need at least 3 colors to build a board without matches.", nameof(colors));
            }

            // A random fill can rarely leave a board with no possible move. Just try again.
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                Board board = Fill(width, height, colors, layout);
                if (_moveFinder.HasPossibleMove(board))
                {
                    return board;
                }
            }

            throw new InvalidOperationException(
                "Could not generate a " + width + "x" + height + " board with a possible move in " + MaxAttempts + " attempts.");
        }

        private Board Fill(int width, int height, IReadOnlyList<TileColor> colors, ObstacleLayout layout)
        {
            Board board = new Board(width, height);
            if (layout != null) layout.ApplyTo(board);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (board.HasCrate(x, y)) continue; // a crate takes the cell: no tile

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
            bool twoToTheLeft = x >= 2 && HasColor(board, x - 1, y, color) && HasColor(board, x - 2, y, color);
            bool twoBelow = y >= 2 && HasColor(board, x, y - 1, color) && HasColor(board, x, y - 2, color);

            return twoToTheLeft || twoBelow;
        }

        // A crate cell has no tile, so it has no color and never helps to make a run.
        private static bool HasColor(Board board, int x, int y, TileColor color)
        {
            Tile tile = board.Get(x, y);
            return tile != null && tile.Color == color;
        }
    }
}
