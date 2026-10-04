using System;

namespace Match3.Core
{
    /// <summary>
    /// A width x height grid of tiles. An empty cell is null.
    /// Y = 0 is the bottom row (see GridPos).
    /// The board only stores tiles. It knows nothing about matches or rules.
    /// </summary>
    public sealed class Board
    {
        private readonly Tile[] _cells;
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
        }

        public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsInside(GridPos pos) => IsInside(pos.X, pos.Y);

        /// <summary>Returns the tile at the cell, or null if the cell is empty. Throws if outside the board.</summary>
        public Tile Get(int x, int y) => _cells[IndexOf(x, y)];

        public Tile Get(GridPos pos) => Get(pos.X, pos.Y);

        /// <summary>Puts a tile (or null to empty the cell) at the cell. Throws if outside the board.</summary>
        public void Set(int x, int y, Tile tile) => _cells[IndexOf(x, y)] = tile;

        public void Set(GridPos pos, Tile tile) => Set(pos.X, pos.Y, tile);

        /// <summary>Exchanges the contents of two cells. Does not check adjacency: that is a rule, not storage.</summary>
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
