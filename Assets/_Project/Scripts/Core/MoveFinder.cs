using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Answers "would swapping these two tiles make a match?" and "is there any such swap on the board?".
    /// It tries the swap on the real board and undoes it, so the board is unchanged afterwards.
    /// A swap is a valid move when it makes a color match, or when one of the two tiles is a special tile
    /// (swapping a special with any neighbor sets it off).
    /// Locked cells never count: a crate has no tile to swap, and a chained tile cannot be swapped.
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

        /// <summary>Finds the first valid swap (scanning bottom-left first). Used by tests and later by hints/shuffle.</summary>
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
                        if (IsValidMove(board, pos, right))
                        {
                            a = pos;
                            b = right;
                            return true;
                        }
                    }

                    if (y + 1 < board.Height)
                    {
                        GridPos up = new GridPos(x, y + 1);
                        if (IsValidMove(board, pos, up))
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

        /// <summary>
        /// Fills results (after clearing it) with every valid swap on the board, each adjacent pair once.
        /// The order is fixed (bottom row first, left to right; the swap with the right neighbor before the one with the upper neighbor),
        /// so the same board always gives the same list. Bots pick one entry of it.
        /// </summary>
        public void GetAllMoves(Board board, List<SwapMove> results)
        {
            results.Clear();

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    GridPos pos = new GridPos(x, y);

                    if (x + 1 < board.Width)
                    {
                        GridPos right = new GridPos(x + 1, y);
                        if (IsValidMove(board, pos, right)) results.Add(new SwapMove(pos, right));
                    }

                    if (y + 1 < board.Height)
                    {
                        GridPos up = new GridPos(x, y + 1);
                        if (IsValidMove(board, pos, up)) results.Add(new SwapMove(pos, up));
                    }
                }
            }
        }

        /// <summary>True if swapping the neighbors a and b is allowed: it makes a match, or one of the tiles is special.</summary>
        public bool IsValidMove(Board board, GridPos a, GridPos b)
        {
            Tile tileA = board.Get(a);
            Tile tileB = board.Get(b);
            if (tileA == null || tileB == null) return false;
            if (board.IsChained(a) || board.IsChained(b)) return false;

            if (tileA.Special != SpecialType.None || tileB.Special != SpecialType.None) return true;

            return WouldMatchAfterSwap(board, a, b);
        }

        /// <summary>True if swapping a and b would produce at least one color match. Swapping with an empty cell or a chained tile is never a move.</summary>
        public bool WouldMatchAfterSwap(Board board, GridPos a, GridPos b)
        {
            if (board.Get(a) == null || board.Get(b) == null) return false;
            if (board.IsChained(a) || board.IsChained(b)) return false;

            board.Swap(a, b);
            _matchFinder.FindMatches(board, _scratch);
            bool foundMatch = _scratch.Count > 0;
            board.Swap(a, b); // undo: the board must look exactly as before

            return foundMatch;
        }
    }
}
