using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// The cells that one wave is going to clear, collected before anything is removed from the board.
    /// Matches add their cells, then special tiles add the cells they hit, which may include more specials (a chain).
    /// Each cell is in the set at most once, so no tile can be cleared twice.
    ///
    /// Obstacles change what "marking a cell" means:
    ///   a crate cell has no tile, so it is never added to the set; marking it (a blast covers it) records one hit on the crate;
    ///   a chained tile is not added either: marking it records that its chain breaks, and the tile stays;
    ///   MarkCratesNextTo records a hit on every crate that touches a cleared tile (used for matches).
    /// A crate is hit at most once per wave, however many blasts and neighbors reach it.
    /// The set is reused every wave (Reset), so it does not allocate per wave once its arrays have the right size.
    /// </summary>
    public sealed class ClearSet
    {
        private readonly List<GridPos> _positions = new List<GridPos>();
        private bool[] _marked = new bool[0];
        private bool[] _activated = new bool[0];
        private int[] _depth = new int[0];
        private bool[] _crateHit = new bool[0];
        private bool[] _chainBroken = new bool[0];
        private readonly List<GridPos> _crateHits = new List<GridPos>();
        private readonly List<GridPos> _chainBreaks = new List<GridPos>();
        private Board _board;

        /// <summary>Starts an empty set for this board.</summary>
        public void Reset(Board board)
        {
            _board = board;
            _positions.Clear();
            _crateHits.Clear();
            _chainBreaks.Clear();

            int cellCount = board.Width * board.Height;
            if (_marked.Length != cellCount)
            {
                _marked = new bool[cellCount];
                _activated = new bool[cellCount];
                _depth = new int[cellCount];
                _crateHit = new bool[cellCount];
                _chainBroken = new bool[cellCount];
            }
            else
            {
                for (int i = 0; i < cellCount; i++)
                {
                    _marked[i] = false;
                    _activated[i] = false;
                    _crateHit[i] = false;
                    _chainBroken[i] = false;
                }
            }
        }

        /// <summary>How many cells are in the set. It can grow while you loop over it (chain reactions).</summary>
        public int Count => _positions.Count;

        /// <summary>The cells in the order they were added: match cells first, then the cells hit by specials.</summary>
        public GridPos PositionAt(int index) => _positions[index];

        /// <summary>How many crates were hit in this wave (each crate once). Read by BoardResolver after the set is complete.</summary>
        public int CrateHitCount => _crateHits.Count;

        public GridPos CrateHitAt(int index) => _crateHits[index];

        /// <summary>How many chained tiles were reached in this wave: their chains break, and the tiles stay on the board.</summary>
        public int ChainBreakCount => _chainBreaks.Count;

        public GridPos ChainBreakAt(int index) => _chainBreaks[index];

        public bool IsMarked(GridPos pos) => _board.IsInside(pos) && _marked[IndexOf(pos)];

        public int DepthAt(GridPos pos) => _depth[IndexOf(pos)];

        public bool IsActivated(GridPos pos) => _activated[IndexOf(pos)];

        /// <summary>Remembers that the special tile in this cell already went off, so it never fires twice.</summary>
        public void MarkActivated(GridPos pos) => _activated[IndexOf(pos)] = true;

        /// <summary>
        /// Adds a cell. Cells outside the board, empty cells and cells already in the set are ignored.
        /// A crate cell records a hit on the crate, and a chained tile records a chain break; neither is added.
        /// Returns true if the cell was added, which means its tile will be cleared.
        /// </summary>
        public bool Mark(GridPos pos, int chainDepth)
        {
            if (!_board.IsInside(pos)) return false;

            int index = IndexOf(pos);

            if (_board.HasCrate(pos))
            {
                RecordCrateHit(pos, index);
                return false;
            }

            if (_board.Get(pos) == null) return false;

            if (_board.IsChained(pos))
            {
                if (!_chainBroken[index])
                {
                    _chainBroken[index] = true;
                    _chainBreaks.Add(pos);
                }

                return false;
            }

            if (_marked[index]) return false;

            _marked[index] = true;
            _depth[index] = chainDepth;
            _positions.Add(pos);
            return true;
        }

        /// <summary>Records a hit on every crate that touches this cell (up, down, left, right). Call it for a tile that is cleared.</summary>
        public void MarkCratesNextTo(GridPos pos)
        {
            RecordCrateHitAt(pos.X + 1, pos.Y);
            RecordCrateHitAt(pos.X - 1, pos.Y);
            RecordCrateHitAt(pos.X, pos.Y + 1);
            RecordCrateHitAt(pos.X, pos.Y - 1);
        }

        private void RecordCrateHitAt(int x, int y)
        {
            if (_board.HasCrate(x, y)) RecordCrateHit(new GridPos(x, y), y * _board.Width + x);
        }

        private void RecordCrateHit(GridPos pos, int index)
        {
            if (_crateHit[index]) return;

            _crateHit[index] = true;
            _crateHits.Add(pos);
        }

        private int IndexOf(GridPos pos) => pos.Y * _board.Width + pos.X;
    }
}
