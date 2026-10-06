using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Edits the obstacle text of a level: one string per board row, the TOP row first, one letter per cell.
    ///   '.' nothing   'C' crate 1 HP   'D' crate 2 HP   'I' ice 1 HP   'J' ice 2 HP   'L' chained tile
    /// An empty or missing list means "this level has no obstacles".
    /// Every method is a pure function: it never changes the list it is given and returns a new array.
    /// The Level Editor's grid painter uses these, and they live in Core (no Unity types) so they can be unit tested.
    /// </summary>
    public static class ObstacleRows
    {
        public const char Empty = '.';
        public const char Crate1 = 'C';
        public const char Crate2 = 'D';
        public const char Ice1 = 'I';
        public const char Ice2 = 'J';
        public const char Chain = 'L';

        public static bool IsValidLetter(char letter)
        {
            return letter == Empty || letter == Crate1 || letter == Crate2 || letter == Ice1 || letter == Ice2 || letter == Chain;
        }

        /// <summary>Row 0 of the text is the TOP of the board, but GridPos.Y = 0 is the bottom: this converts a Y to the text row.</summary>
        public static int RowFromTop(int y, int boardHeight) => boardHeight - 1 - y;

        /// <summary>A full grid with nothing in it (height rows of width dots).</summary>
        public static string[] CreateEmpty(int width, int height)
        {
            string[] rows = new string[height];
            string emptyRow = new string(Empty, width);
            for (int i = 0; i < rows.Length; i++) rows[i] = emptyRow;
            return rows;
        }

        /// <summary>True if there is nothing to show: no rows at all, or only dots.</summary>
        public static bool IsEmpty(IReadOnlyList<string> rows)
        {
            if (rows == null) return true;

            for (int row = 0; row < rows.Count; row++)
            {
                string text = rows[row] ?? "";
                for (int column = 0; column < text.Length; column++)
                {
                    if (text[column] != Empty) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The same obstacles on a board of another size. The top-left corner stays where it is: cells that no longer fit are
        /// dropped, new cells are empty. Rows that are missing or too short count as empty, so a level without obstacles works too.
        /// </summary>
        public static string[] Resize(IReadOnlyList<string> rows, int newWidth, int newHeight)
        {
            string[] result = new string[newHeight];
            for (int row = 0; row < newHeight; row++)
            {
                string old = rows != null && row < rows.Count && rows[row] != null ? rows[row] : "";
                if (old.Length >= newWidth) result[row] = old.Substring(0, newWidth);
                else result[row] = old + new string(Empty, newWidth - old.Length);
            }

            return result;
        }

        /// <summary>The letter at a cell (column from the left, row from the TOP). Anything outside the text counts as empty.</summary>
        public static char GetCell(IReadOnlyList<string> rows, int column, int row)
        {
            if (rows == null || row < 0 || row >= rows.Count) return Empty;

            string text = rows[row];
            if (text == null || column < 0 || column >= text.Length) return Empty;

            return text[column];
        }

        /// <summary>
        /// A copy of the grid with one cell changed. The result always has exactly height rows of width letters
        /// (a short or missing grid is filled up with dots first).
        /// </summary>
        public static string[] SetCell(IReadOnlyList<string> rows, int width, int height, int column, int row, char letter)
        {
            if (!IsValidLetter(letter)) throw new ArgumentException("'" + letter + "' is not an obstacle letter.", nameof(letter));
            if (column < 0 || column >= width) throw new ArgumentOutOfRangeException(nameof(column), "Column " + column + " is outside a board " + width + " wide.");
            if (row < 0 || row >= height) throw new ArgumentOutOfRangeException(nameof(row), "Row " + row + " is outside a board " + height + " high.");

            string[] result = Resize(rows, width, height);
            char[] letters = result[row].ToCharArray();
            letters[column] = letter;
            result[row] = new string(letters);
            return result;
        }

        /// <summary>The way a level stores "no obstacles": an empty list instead of a grid of dots. A grid with obstacles is returned unchanged.</summary>
        public static string[] Normalize(string[] rows)
        {
            return IsEmpty(rows) ? new string[0] : rows;
        }
    }
}
