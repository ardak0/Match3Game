using System;

namespace Match3.Core
{
    /// <summary>
    /// A width x height grid of tiles. An empty cell is null.
    /// Y = 0 is the bottom row (see GridPos).
    ///
    /// Every cell also has an obstacle layer (crate, ice or chain, see ObstacleType) that is separate from the tile layer:
    ///   a crate takes the cell, so a crate cell never holds a tile;
    ///   ice sits under whatever tile is in the cell, and stays in its cell when the tile moves away;
    ///   a chain holds the tile that is in the cell.
    /// Swap only exchanges tiles, so obstacles never move.
    ///
    /// The board only stores things. It knows nothing about matches or rules.
    /// </summary>
    public sealed class Board
    {
        private readonly Tile[] _cells;
        private readonly Obstacle[] _obstacles;
        private int _nextTileId;

        public int Width { get; }
        public int Height { get; }

        public Board(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

            Width = width;
            Height = height;
            _cells = new Tile[width * height];
            _obstacles = new Obstacle[width * height];
        }

        public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsInside(GridPos pos) => IsInside(pos.X, pos.Y);

        /// <summary>Returns the tile at the cell, or null if the cell is empty. Throws if outside the board.</summary>
        public Tile Get(int x, int y) => _cells[IndexOf(x, y)];

        public Tile Get(GridPos pos) => Get(pos.X, pos.Y);

        /// <summary>Puts a tile (or null to empty the cell) at the cell. Throws if outside the board.</summary>
        public void Set(int x, int y, Tile tile) => _cells[IndexOf(x, y)] = tile;

        public void Set(GridPos pos, Tile tile) => Set(pos.X, pos.Y, tile);

        /// <summary>The obstacle in the cell (Obstacle.None if there is none). Throws if outside the board.</summary>
        public Obstacle GetObstacle(int x, int y) => _obstacles[IndexOf(x, y)];

        public Obstacle GetObstacle(GridPos pos) => GetObstacle(pos.X, pos.Y);

        /// <summary>
        /// Puts an obstacle in the cell (Obstacle.None removes it). The board does not check that it makes sense:
        /// whoever sets a crate must leave the cell without a tile.
        /// </summary>
        public void SetObstacle(int x, int y, Obstacle obstacle) => _obstacles[IndexOf(x, y)] = obstacle;

        public void SetObstacle(GridPos pos, Obstacle obstacle) => SetObstacle(pos.X, pos.Y, obstacle);

        /// <summary>True if the cell holds a crate (so it has no tile and takes no part in the game). False outside the board.</summary>
        public bool HasCrate(int x, int y) => IsInside(x, y) && _obstacles[y * Width + x].Type == ObstacleType.Crate;

        public bool HasCrate(GridPos pos) => HasCrate(pos.X, pos.Y);

        /// <summary>True if the cell's tile is held by a chain: it cannot be swapped and does not fall. False outside the board.</summary>
        public bool IsChained(int x, int y) => IsInside(x, y) && _obstacles[y * Width + x].Type == ObstacleType.Chain;

        public bool IsChained(GridPos pos) => IsChained(pos.X, pos.Y);

        /// <summary>
        /// True if tiles cannot fall through this cell: it holds a crate, or a chained tile that stays where it is.
        /// Gravity treats a blocked cell as a wall.
        /// </summary>
        public bool IsBlocked(int x, int y) => HasCrate(x, y) || IsChained(x, y);

        public bool IsBlocked(GridPos pos) => IsBlocked(pos.X, pos.Y);

        /// <summary>True if any cell above this one, in the same column, is blocked (see IsBlocked).</summary>
        public bool HasBlockerAbove(int x, int y)
        {
            for (int above = y + 1; above < Height; above++)
            {
                if (IsBlocked(x, above)) return true;
            }

            return false;
        }

        /// <summary>
        /// A copy of the board: the same tiles, obstacles and tile-id counter, but changing the copy never changes this board.
        /// Tiles and obstacles are immutable, so the copy can share them. A bot uses it to try a move without playing it.
        /// </summary>
        public Board Clone()
        {
            Board copy = new Board(Width, Height);
            Array.Copy(_cells, copy._cells, _cells.Length);
            Array.Copy(_obstacles, copy._obstacles, _obstacles.Length);
            copy._nextTileId = _nextTileId;
            return copy;
        }

        /// <summary>Exchanges the tiles of two cells (obstacles stay where they are). Does not check adjacency: that is a rule, not storage.</summary>
        public void Swap(GridPos a, GridPos b)
        {
            int indexA = IndexOf(a.X, a.Y);
            int indexB = IndexOf(b.X, b.Y);
            Tile temp = _cells[indexA];
            _cells[indexA] = _cells[indexB];
            _cells[indexB] = temp;
        }

        /// <summary>
        /// Creates a tile with a fresh unique Id. It is NOT placed on the board; call Set for that.
        /// Board owns the Id counter so every tile of one game has a different Id.
        /// </summary>
        public Tile NewTile(TileColor color, SpecialType special = SpecialType.None)
        {
            return new Tile(_nextTileId++, color, special);
        }

        private int IndexOf(int x, int y)
        {
            if (!IsInside(x, y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x), "Cell (" + x + "," + y + ") is outside the " + Width + "x" + Height + " board.");
            }

            return y * Width + x;
        }
    }
}
