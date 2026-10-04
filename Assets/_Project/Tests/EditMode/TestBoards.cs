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
