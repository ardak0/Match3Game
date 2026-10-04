using System;
using Match3.Core;

namespace Match3.Tests
{
    /// <summary>
    /// Builds boards from text so tests are readable.
    /// Rows are written TOP row first, exactly as you would see them on screen.
    /// Letters: R=Red G=Green B=Blue Y=Yellow P=Purple O=Orange, '.' = empty cell.
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

        /// <summary>Turns the tile at the cell into a special tile of the same color (a new tile with a new id). Returns the board for chaining.</summary>
        public static Board WithSpecial(this Board board, int x, int y, SpecialType special)
        {
            board.Set(x, y, board.NewTile(board.Get(x, y).Color, special));
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
