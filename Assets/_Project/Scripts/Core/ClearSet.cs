using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// The cells that one wave is going to clear, collected before anything is removed from the board.
    /// Matches add their cells, then special tiles add the cells they hit, which may include more specials (a chain).
    /// Each cell is in the set at most once, so no tile can be cleared twice.
    /// The set is reused every wave (Reset), so it does not allocate per wave once its arrays have the right size.
    /// </summary>
    public sealed class ClearSet
    {
        private readonly List<GridPos> _positions = new List<GridPos>();
        private bool[] _marked = new bool[0];
        private bool[] _activated = new bool[0];
        private int[] _depth = new int[0];
        private Board _board;

        /// <summary>Starts an empty set for this board.</summary>
        public void Reset(Board board)
        {
            _board = board;
            _positions.Clear();

            int cellCount = board.Width * board.Height;
            if (_marked.Length != cellCount)
            {
                _marked = new bool[cellCount];
                _activated = new bool[cellCount];
                _depth = new int[cellCount];
            }
            else
            {
                for (int i = 0; i < cellCount; i++)
                {
                    _marked[i] = false;
                    _activated[i] = false;
                }
            }
        }

        /// <summary>How many cells are in the set. It can grow while you loop over it (chain reactions).</summary>
        public int Count => _positions.Count;

        /// <summary>The cells in the order they were added: match cells first, then the cells hit by specials.</summary>
        public GridPos PositionAt(int index) => _positions[index];

        public bool IsMarked(GridPos pos) => _board.IsInside(pos) && _marked[IndexOf(pos)];

        public int DepthAt(GridPos pos) => _depth[IndexOf(pos)];

        public bool IsActivated(GridPos pos) => _activated[IndexOf(pos)];

        /// <summary>Remembers that the special tile in this cell already went off, so it never fires twice.</summary>
        public void MarkActivated(GridPos pos) => _activated[IndexOf(pos)] = true;

        /// <summary>
        /// Adds a cell. Cells outside the board, empty cells and cells already in the set are ignored.
        /// Returns true if the cell was added.
        /// </summary>
        public bool Mark(GridPos pos, int chainDepth)
        {
            if (!_board.IsInside(pos)) return false;
            if (_board.Get(pos) == null) return false;

            int index = IndexOf(pos);
            if (_marked[index]) return false;

            _marked[index] = true;
            _depth[index] = chainDepth;
            _positions.Add(pos);
            return true;
        }

        private int IndexOf(GridPos pos) => pos.Y * _board.Width + pos.X;
    }
}
