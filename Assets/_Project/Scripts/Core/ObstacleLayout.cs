using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Where the obstacles of a level are, read from text. A level lists one string per board row, TOP row first
    /// (the way the board looks on screen), one character per cell:
    ///   '.' nothing    'C' crate with 1 HP    'D' crate with 2 HP    'I' ice with 1 HP    'J' ice with 2 HP    'L' chained tile
    /// ApplyTo puts the obstacles on a Board; the BoardGenerator then fills every cell that is not a crate with a tile.
    /// Pure data and parsing, so it can be tested without Unity.
    /// </summary>
    public sealed class ObstacleLayout
    {
        private readonly Obstacle[] _obstacles; // index = y * Width + x, with y = 0 the BOTTOM row like everywhere else

        public int Width { get; }
        public int Height { get; }

        private ObstacleLayout(int width, int height, Obstacle[] obstacles)
        {
            Width = width;
            Height = height;
            _obstacles = obstacles;
        }

        /// <summary>
        /// Reads the rows. Null or no rows at all is allowed and means "no obstacles". Otherwise there must be exactly
        /// one row per board row and one letter per board column. On failure layout is null and error says what is wrong.
        /// </summary>
        public static bool TryParse(IReadOnlyList<string> rowsTopFirst, int width, int height, out ObstacleLayout layout, out string error)
        {
            Obstacle[] obstacles = new Obstacle[width * height];

            if (rowsTopFirst != null && rowsTopFirst.Count > 0)
            {
                if (rowsTopFirst.Count != height)
                {
                    layout = null;
                    error = "needs " + height + " rows (one per board row, the top row first), but has " + rowsTopFirst.Count + ".";
                    return false;
                }

                for (int row = 0; row < height; row++)
                {
                    string text = rowsTopFirst[row] ?? "";
                    if (text.Length != width)
                    {
                        layout = null;
                        error = "Row " + (row + 1) + " has " + text.Length + " characters, but the board is " + width + " wide.";
                        return false;
                    }

                    int y = height - 1 - row; // the first row is the top row
                    for (int x = 0; x < width; x++)
                    {
                        if (!TryReadLetter(text[x], out Obstacle obstacle))
                        {
                            layout = null;
                            error = "Unknown character '" + text[x] + "' at row " + (row + 1) + ", column " + (x + 1)
                                + " (use . C D I J L).";
                            return false;
                        }

                        obstacles[y * width + x] = obstacle;
                    }
                }
            }

            layout = new ObstacleLayout(width, height, obstacles);
            error = null;
            return true;
        }

        private static bool TryReadLetter(char letter, out Obstacle obstacle)
        {
            switch (letter)
            {
                case '.': obstacle = Obstacle.None; return true;
                case 'C': obstacle = new Obstacle(ObstacleType.Crate, 1); return true;
                case 'D': obstacle = new Obstacle(ObstacleType.Crate, 2); return true;
                case 'I': obstacle = new Obstacle(ObstacleType.Ice, 1); return true;
                case 'J': obstacle = new Obstacle(ObstacleType.Ice, 2); return true;
                case 'L': obstacle = new Obstacle(ObstacleType.Chain, 1); return true;
                default: obstacle = Obstacle.None; return false;
            }
        }

        /// <summary>The obstacle in a cell (y = 0 is the bottom row).</summary>
        public Obstacle Get(int x, int y) => _obstacles[y * Width + x];

        /// <summary>How many obstacles of this type the layout has. An obstacle counts once, whatever its HP.</summary>
        public int Count(ObstacleType type)
        {
            int count = 0;
            for (int i = 0; i < _obstacles.Length; i++)
            {
                if (_obstacles[i].Type == type) count++;
            }

            return count;
        }

        /// <summary>Puts every obstacle on the board. The board must have the same size. It does not place any tiles.</summary>
        public void ApplyTo(Board board)
        {
            if (board.Width != Width || board.Height != Height)
            {
                throw new System.ArgumentException(
                    "The layout is " + Width + "x" + Height + " but the board is " + board.Width + "x" + board.Height + ".", nameof(board));
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    board.SetObstacle(x, y, Get(x, y));
                }
            }
        }

        /// <summary>
        /// Looks for a cell that nothing could ever fill: it is not a crate, a crate sits directly above it, and
        /// both cells diagonally above it are crates or off the board. Tiles reach such a cell only by sliding in from a
        /// diagonal, so it would stay empty for good. Cells are checked from the bottom row up, left to right.
        /// </summary>
        public bool TryFindUnfillableCell(out GridPos cell)
        {
            for (int y = 0; y < Height - 1; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (IsCrate(x, y) || !IsCrate(x, y + 1)) continue;

                    if (!IsFreeCell(x - 1, y + 1) && !IsFreeCell(x + 1, y + 1))
                    {
                        cell = new GridPos(x, y);
                        return true;
                    }
                }
            }

            cell = default;
            return false;
        }

        private bool IsCrate(int x, int y) => Get(x, y).Type == ObstacleType.Crate;

        private bool IsFreeCell(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height && !IsCrate(x, y);
    }
}
