using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Makes tiles fall: in every column, tiles slide down to close the gaps (empty cells).
    /// The order of the tiles inside a column never changes, and tiles never move sideways.
    /// </summary>
    public sealed class GravityResolver
    {
        // Reused between calls so we don't allocate a new list for every column of every wave.
        private readonly List<TileMove> _moves = new List<TileMove>();

        /// <summary>
        /// Applies gravity to the board and returns what moved,
        /// or null if every tile was already resting (nothing to animate).
        /// </summary>
        public FallStep Apply(Board board, int wave)
        {
            _moves.Clear();

            // Per column, walk up from y = 0. "writeY" is the lowest slot a tile can fall to.
            // Every tile we meet moves down to writeY (if it isn't there already), then writeY goes up by one.
            for (int x = 0; x < board.Width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < board.Height; y++)
                {
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
            return new FallStep(wave, _moves.ToArray());
        }
    }
}
