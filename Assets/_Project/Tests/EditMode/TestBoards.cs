using System;
using Match3.Core;

namespace Match3.Tests
{
    /// <summary>
    /// Builds boards from text so tests are readable.
    /// Rows are written TOP row first, exactly as you would see them on screen.
    /// Letters: R=Red G=Green B=Blue Y=Yellow P=Purple O=Orange, '*' = ColorBomb (it has no color), '.' = empty cell.
    ///
    ///   TestBoards.FromRows(
    ///       "RGB",     // top row    (y = 2)
    ///       "RBG",     // middle row (y = 1)
    ///       "RGB");    // bottom row (y = 0)
    /// </summary>
    public static class TestBoards
    {
        public static readonly TileColor[] AllColors =
        {
            TileColor.Red, TileColor.Green, TileColor.Blue,
            TileColor.Yellow, TileColor.Purple, TileColor.Orange
        };

        public static Board FromRows(params string[] rowsTopFirst)
        {
            int height = rowsTopFirst.Length;
            int width = rowsTopFirst[0].Length;
            Board board = new Board(width, height);

            for (int row = 0; row < height; row++)
            {
                if (rowsTopFirst[row].Length != width)
                {
                    throw new ArgumentException("All rows must have the same length.");
                }

                int y = height - 1 - row; // first string is the top row
                for (int x = 0; x < width; x++)
                {
                    char letter = rowsTopFirst[row][x];
                    if (letter == '.') continue;
                    if (letter == '*')
                    {
                        board.Set(x, y, board.NewTile(TileColor.None, SpecialType.ColorBomb));
                        continue;
                    }

                    board.Set(x, y, board.NewTile(ParseColor(letter)));
                }
            }

            return board;
        }

        /// <summary>
        /// A board with a diagonal color pattern: the tile at (x, y) has color number (x + y) % colorCount.
        /// With a colorCount of 3 or more, no two neighbors in a row or column share a color, so the board has no match.
        /// Tests use it as a quiet background for the one situation they want to set up.
        /// </summary>
        public static Board Diagonal(int width, int height, int colorCount)
        {
            Board board = new Board(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    board.Set(x, y, board.NewTile(AllColors[(x + y) % colorCount]));
                }
            }

            return board;
        }

        /// <summary>
        /// Puts obstacles on a board. Rows are written TOP row first, like FromRows, one letter per cell:
        ///   '.' nothing   'C' crate 1 HP   'D' crate 2 HP   'I' ice 1 HP   'J' ice 2 HP   'L' chained tile
        /// A crate takes the cell, so its tile is removed (write '.' in that cell of the tile rows to keep the test readable).
        /// Ice and chains go on the tile that is already in the cell. This reads the letters by itself, without
        /// ObstacleLayout, so a bug in the layout parser cannot hide a bug in the rules under test.
        /// </summary>
        public static Board WithObstacles(this Board board, params string[] rowsTopFirst)
        {
            if (rowsTopFirst.Length != board.Height) throw new ArgumentException("Need one obstacle row per board row.");

            for (int row = 0; row < rowsTopFirst.Length; row++)
            {
                if (rowsTopFirst[row].Length != board.Width) throw new ArgumentException("Every obstacle row needs one letter per column.");

                int y = board.Height - 1 - row;
                for (int x = 0; x < board.Width; x++)
                {
                    switch (rowsTopFirst[row][x])
                    {
                        case '.':
                            break;
                        case 'C':
                        case 'D':
                            board.Set(x, y, null);
                            board.SetObstacle(x, y, new Obstacle(ObstacleType.Crate, rowsTopFirst[row][x] == 'C' ? 1 : 2));
                            break;
                        case 'I':
                        case 'J':
                            RequireTile(board, x, y);
                            board.SetObstacle(x, y, new Obstacle(ObstacleType.Ice, rowsTopFirst[row][x] == 'I' ? 1 : 2));
                            break;
                        case 'L':
                            RequireTile(board, x, y);
                            board.SetObstacle(x, y, new Obstacle(ObstacleType.Chain, 1));
                            break;
                        default:
                            throw new ArgumentException("Unknown obstacle letter '" + rowsTopFirst[row][x] + "'.");
                    }
                }
            }

            return board;
        }

        private static void RequireTile(Board board, int x, int y)
        {
            if (board.Get(x, y) == null) throw new ArgumentException("Ice and chains need a tile in the cell (" + x + "," + y + ").");
        }

        /// <summary>Turns the tile at the cell into a special tile of the same color (a new tile with a new id). Returns the board for chaining.</summary>
        public static Board WithSpecial(this Board board, int x, int y, SpecialType special)
        {
            board.Set(x, y, board.NewTile(board.Get(x, y).Color, special));
            return board;
        }

        /// <summary>Turns the tile at the cell into a ColorBomb. A ColorBomb has no color, so unlike WithSpecial the old color is lost.</summary>
        public static Board WithColorBomb(this Board board, int x, int y)
        {
            board.Set(x, y, board.NewTile(TileColor.None, SpecialType.ColorBomb));
            return board;
        }

        private static TileColor ParseColor(char letter)
        {
            switch (letter)
            {
                case 'R': return TileColor.Red;
                case 'G': return TileColor.Green;
                case 'B': return TileColor.Blue;
                case 'Y': return TileColor.Yellow;
                case 'P': return TileColor.Purple;
                case 'O': return TileColor.Orange;
                default: throw new ArgumentException("Unknown tile letter '" + letter + "'.");
            }
        }
    }
}
