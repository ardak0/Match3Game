using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Answers "would swapping these two tiles make a match?" and "is there any such swap on the board?".
    /// It tries the swap on the real board and undoes it, so the board is unchanged afterwards.
    /// It only knows color matches. M6 will extend it for swaps that involve special tiles.
    /// </summary>
    public sealed class MoveFinder
    {
        private readonly MatchFinder _matchFinder;
        private readonly List<Match> _scratch = new List<Match>();

        public MoveFinder(MatchFinder matchFinder)
        {
            _matchFinder = matchFinder;
        }

        /// <summary>True if at least one adjacent swap on the board creates a match.</summary>
        public bool HasPossibleMove(Board board) => TryFindMove(board, out _, out _);

        /// <summary>Finds the first swap (scanning bottom-left first) that creates a match. Used by tests and later by hints/shuffle.</summary>
        public bool TryFindMove(Board board, out GridPos a, out GridPos b)
        {
            // Checking only "swap with the right neighbor" and "swap with the upper neighbor"
            // covers every adjacent pair exactly once.
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    GridPos pos = new GridPos(x, y);

                    if (x + 1 < board.Width)
                    {
                        GridPos right = new GridPos(x + 1, y);
                        if (WouldMatchAfterSwap(board, pos, right))
                        {
                            a = pos;
                            b = right;
                            return true;
                        }
                    }

                    if (y + 1 < board.Height)
                    {
                        GridPos up = new GridPos(x, y + 1);
                        if (WouldMatchAfterSwap(board, pos, up))
                        {
                            a = pos;
                            b = up;
                            return true;
                        }
                    }
                }
            }

            a = default;
            b = default;
            return false;
        }

        /// <summary>True if swapping a and b would produce at least one match. Swapping with an empty cell is never a move.</summary>
        public bool WouldMatchAfterSwap(Board board, GridPos a, GridPos b)
        {
            if (board.Get(a) == null || board.Get(b) == null) return false;

            board.Swap(a, b);
            _matchFinder.FindMatches(board, _scratch);
            bool foundMatch = _scratch.Count > 0;
            board.Swap(a, b); // undo: the board must look exactly as before

            return foundMatch;
        }
    }
}
