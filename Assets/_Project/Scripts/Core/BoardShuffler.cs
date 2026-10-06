using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Rearranges the tiles of a full board that has no possible move, so the player can keep playing.
    ///
    /// Idea: take all tiles off the board, then put them back cell by cell (bottom row first, left to right).
    /// For every cell pick a random remaining tile that would not complete a run of 3 with the two cells
    /// to its left or the two cells below it. That way the new board has no ready-made match.
    /// A random fill can paint itself into a corner, or end up with no possible move again,
    /// so we simply try again, up to MaxAttempts times. If nothing works, the board is put back
    /// exactly as it was and TryShuffle returns false (the game then ends the level).
    ///
    /// The tiles themselves (ids, colors, specials) are reused: a shuffle only moves them.
    ///
    /// Locked cells are left alone: crates have no tile, and a chained tile keeps its cell (and its chain).
    /// Only the free tiles are shuffled, among the cells that are not locked. Ice stays where it is.
    /// </summary>
    public sealed class BoardShuffler
    {
        public const int MaxAttempts = 200;

        private readonly IRandom _random;
        private readonly MoveFinder _moveFinder;
        private readonly MatchFinder _matchFinder = new MatchFinder();
        private readonly List<Tile> _pool = new List<Tile>();
        private readonly List<int> _candidates = new List<int>();

        public BoardShuffler(IRandom random, MoveFinder moveFinder)
        {
            _random = random;
            _moveFinder = moveFinder;
        }

        /// <summary>
        /// Shuffles the board. On success the board holds the new arrangement and
        /// <paramref name="step"/> describes it for the view. On failure the board is unchanged and step is null.
        /// The board must be full, which is always true between two moves. The only empty cells allowed are crate cells and
        /// cells under a crate or chained tile that nothing could fill (they stay empty).
        /// </summary>
        public bool TryShuffle(Board board, out ShuffleStep step)
        {
            int width = board.Width;
            int height = board.Height;

            Tile[] original = new Tile[width * height]; // index = y * width + x
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Tile tile = board.Get(x, y);
                    if (tile == null && !board.HasCrate(x, y) && !board.HasBlockerAbove(x, y))
                    {
                        throw new InvalidOperationException("Cannot shuffle: cell (" + x + "," + y + ") is empty.");
                    }

                    original[y * width + x] = tile; // null for a crate or for a gap that cannot be filled
                }
            }

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                if (TryFill(board, original) && !_matchFinder.HasMatch(board) && _moveFinder.HasPossibleMove(board))
                {
                    step = new ShuffleStep(BuildMoves(board, original));
                    return true;
                }
            }

            PutBack(board, original);
            step = null;
            return false;
        }

        // Fills the whole board with the tiles of "tiles" in a new random order. False if it got stuck.
        private bool TryFill(Board board, Tile[] tiles)
        {
            // Only the free tiles are shuffled: not the tiles of empty cells, and not chained tiles.
            _pool.Clear();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Tile tile = tiles[y * board.Width + x];
                    if (tile != null && !board.IsChained(x, y)) _pool.Add(tile);
                }
            }

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (tiles[y * board.Width + x] == null || board.IsChained(x, y)) continue; // locked: it keeps what it has

                    // Which of the remaining tiles may go here without making 3 in a row?
                    _candidates.Clear();
                    for (int i = 0; i < _pool.Count; i++)
                    {
                        if (!MakesRun(board, x, y, _pool[i].Color))
                        {
                            _candidates.Add(i);
                        }
                    }

                    if (_candidates.Count == 0) return false;

                    int chosen = _candidates[_random.Next(0, _candidates.Count)];
                    board.Set(x, y, _pool[chosen]);

                    // Remove it from the pool: move the last tile into its place (order does not matter).
                    int last = _pool.Count - 1;
                    _pool[chosen] = _pool[last];
                    _pool.RemoveAt(last);
                }
            }

            return true;
        }

        // Would a tile of this color at (x, y) make 3 in a row with the two cells to the left or below?
        // Only left and below are looked at, because the cells right and above are not filled yet.
        private static bool MakesRun(Board board, int x, int y, TileColor color)
        {
            if (color == TileColor.None) return false; // a ColorBomb never matches, so it can go anywhere
            if (x >= 2 && HasColor(board, x - 1, y, color) && HasColor(board, x - 2, y, color)) return true;
            if (y >= 2 && HasColor(board, x, y - 1, color) && HasColor(board, x, y - 2, color)) return true;
            return false;
        }

        // A cell without a tile (crate, or a gap that cannot be filled) never helps to make a run.
        private static bool HasColor(Board board, int x, int y, TileColor color)
        {
            Tile tile = board.Get(x, y);
            return tile != null && tile.Color == color;
        }

        private static List<TileMove> BuildMoves(Board board, Tile[] original)
        {
            // Where was each tile before? Tile ids are unique, so id -> old cell index works.
            Dictionary<int, int> oldIndexOfTile = new Dictionary<int, int>(original.Length);
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] != null) oldIndexOfTile[original[i].Id] = i;
            }

            List<TileMove> moves = new List<TileMove>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Tile tile = board.Get(x, y);
                    if (tile == null) continue;

                    int oldIndex = oldIndexOfTile[tile.Id];
                    GridPos from = new GridPos(oldIndex % board.Width, oldIndex / board.Width);
                    GridPos to = new GridPos(x, y);
                    if (from != to)
                    {
                        moves.Add(new TileMove(tile.Id, from, to));
                    }
                }
            }

            return moves;
        }

        private static void PutBack(Board board, Tile[] original)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    board.Set(x, y, original[y * board.Width + x]);
                }
            }
        }
    }
}
