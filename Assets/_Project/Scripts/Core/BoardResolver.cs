using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Plays a player's swap on the board, all at once, and reports what happened.
    /// The model decides everything here; the view only animates the returned steps.
    ///
    /// ResolveSwap:
    ///   1. Check the swap is allowed: both cells on the board, next to each other, both holding a tile,
    ///      and the swap must create a match. If not, the board stays as it was and we return Invalid.
    ///   2. Repeat until no match is left: clear the matched tiles, let tiles fall, refill from above.
    ///   3. Return the list of steps plus how many tiles of each color were cleared.
    /// </summary>
    public sealed class BoardResolver
    {
        private readonly MatchFinder _matchFinder = new MatchFinder();
        private readonly GravityResolver _gravity = new GravityResolver();
        private readonly Refiller _refiller;
        private readonly List<Match> _matches = new List<Match>();

        public BoardResolver(IRandom random, IReadOnlyList<TileColor> colors)
        {
            _refiller = new Refiller(random, colors);
        }

        public ResolveResult ResolveSwap(Board board, GridPos a, GridPos b)
        {
            if (!IsLegalSwapTarget(board, a, b)) return ResolveResult.Invalid;

            // Remember which tile is where BEFORE swapping: the SwapStep needs these ids.
            int tileIdA = board.Get(a).Id;
            int tileIdB = board.Get(b).Id;

            board.Swap(a, b);
            _matchFinder.FindMatches(board, _matches);
            if (_matches.Count == 0)
            {
                board.Swap(a, b); // put everything back: an invalid move must not change the board
                return ResolveResult.Invalid;
            }

            List<ResolveStep> steps = new List<ResolveStep>();
            int[] clearedByColor = new int[Enum.GetValues(typeof(TileColor)).Length];
            steps.Add(new SwapStep(0, a, b, tileIdA, tileIdB));

            int wave = 1;
            while (_matches.Count > 0)
            {
                steps.Add(ClearMatches(board, wave, clearedByColor));

                FallStep fall = _gravity.Apply(board, wave);
                if (fall != null) steps.Add(fall);

                SpawnStep spawn = _refiller.Refill(board, wave);
                if (spawn != null) steps.Add(spawn);

                // New tiles or tiles that landed next to each other may have made a new match.
                _matchFinder.FindMatches(board, _matches);
                wave++;
            }

            return new ResolveResult(steps, clearedByColor, wave - 1);
        }

        private static bool IsLegalSwapTarget(Board board, GridPos a, GridPos b)
        {
            if (!board.IsInside(a) || !board.IsInside(b)) return false;

            bool nextToEachOther = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1; // no diagonals, not the same cell
            if (!nextToEachOther) return false;

            return board.Get(a) != null && board.Get(b) != null;
        }

        // Removes every tile of every current match from the board and records it.
        // Two matches never share a cell (MatchFinder merged overlapping runs), so no tile is cleared twice.
        private ClearStep ClearMatches(Board board, int wave, int[] clearedByColor)
        {
            int tileCount = 0;
            for (int i = 0; i < _matches.Count; i++)
            {
                tileCount += _matches[i].Positions.Count;
            }

            ClearedTile[] cleared = new ClearedTile[tileCount];
            int next = 0;
            for (int i = 0; i < _matches.Count; i++)
            {
                List<GridPos> positions = _matches[i].Positions;
                for (int j = 0; j < positions.Count; j++)
                {
                    Tile tile = board.Get(positions[j]);
                    cleared[next++] = new ClearedTile(tile.Id, tile.Color, positions[j]);
                    clearedByColor[(int)tile.Color]++;
                    board.Set(positions[j], null);
                }
            }

            return new ClearStep(wave, cleared);
        }
    }
}
