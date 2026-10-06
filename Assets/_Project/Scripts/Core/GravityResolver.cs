using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Makes tiles fall. There are two kinds of movement:
    ///
    ///   Apply (straight pass): in every column, tiles slide down to close the gaps (empty cells).
    ///   The order of the tiles inside a column never changes. Crates and chained tiles stay where they are
    ///   and act as walls: a tile above a blocker lands on top of it and never passes it.
    ///
    ///   ApplyDiagonal (diagonal pass): a cell directly under a blocker can never be filled from straight above,
    ///   so it takes a tile from its upper-left cell, or else from its upper-right cell (always in that order,
    ///   so the result is the same every time). Only tiles that can move are taken: not chained tiles.
    ///
    /// BoardResolver alternates the two passes (with a refill in between) until the board has settled.
    /// </summary>
    public sealed class GravityResolver
    {
        // Reused between calls so we don't allocate a new list for every column of every wave.
        private readonly List<TileMove> _moves = new List<TileMove>();

        /// <summary>
        /// Applies straight gravity to the board and returns what moved,
        /// or null if every tile was already resting (nothing to animate).
        /// </summary>
        public FallStep Apply(Board board, int wave, int round = 0)
        {
            _moves.Clear();

            // Per column, walk up from y = 0. "writeY" is the lowest slot a tile can fall to.
            // Every tile we meet moves down to writeY (if it isn't there already), then writeY goes up by one.
            // A blocked cell (crate, chained tile) does not move: the slots above it start right on top of it.
            for (int x = 0; x < board.Width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.IsBlocked(x, y))
                    {
                        writeY = y + 1;
                        continue;
                    }

                    Tile tile = board.Get(x, y);
                    if (tile == null) continue;

                    if (y != writeY)
                    {
                        board.Set(x, writeY, tile);
                        board.Set(x, y, null);
                        _moves.Add(new TileMove(tile.Id, new GridPos(x, y), new GridPos(x, writeY)));
                    }

                    writeY++;
                }
            }

            if (_moves.Count == 0) return null;

            // The step keeps its own copy, because _moves is reused by the next call.
            return new FallStep(wave, _moves.ToArray(), round);
        }

        /// <summary>
        /// Fills the empty cells that sit directly under a blocker by sliding a tile in from the upper-left or upper-right.
        /// Cells are visited bottom row first, left to right. Each tile moves at most once per pass.
        /// Returns what moved, or null if nothing could slide.
        /// </summary>
        public FallStep ApplyDiagonal(Board board, int wave, int round)
        {
            _moves.Clear();

            for (int y = 0; y < board.Height - 1; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.HasCrate(x, y) || board.Get(x, y) != null) continue;
                    if (!board.IsBlocked(x, y + 1)) continue; // only the cell right under a blocker: other gaps are for the straight pass

                    if (TrySlideInto(board, x, y, x - 1) || TrySlideInto(board, x, y, x + 1)) continue;
                }
            }

            if (_moves.Count == 0) return null;

            return new FallStep(wave, _moves.ToArray(), round);
        }

        // Moves the tile at (fromX, y + 1) down into the empty cell (x, y), if there is a tile there that is free to move.
        private bool TrySlideInto(Board board, int x, int y, int fromX)
        {
            if (!board.IsInside(fromX, y + 1)) return false;

            Tile tile = board.Get(fromX, y + 1);
            if (tile == null || board.IsChained(fromX, y + 1)) return false;

            board.Set(x, y, tile);
            board.Set(fromX, y + 1, null);
            _moves.Add(new TileMove(tile.Id, new GridPos(fromX, y + 1), new GridPos(x, y)));
            return true;
        }
    }
}
