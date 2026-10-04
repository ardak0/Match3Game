namespace Match3.Core
{
    /// <summary>A special tile that a match leaves behind: which one, which color, and in which cell.</summary>
    public readonly struct SpecialCreation
    {
        public readonly GridPos Position;
        public readonly TileColor Color;
        public readonly SpecialType Special;

        public SpecialCreation(GridPos position, TileColor color, SpecialType special)
        {
            Position = position;
            Color = color;
            Special = special;
        }
    }

    /// <summary>
    /// The rules for special tiles, in three parts:
    ///   creation    - which special a match leaves behind, and where (TryGetCreation)
    ///   activation  - what a special clears when it is cleared itself, including chain reactions (ActivateSpecials)
    ///   combos      - what two specials swapped together clear (MarkCombo)
    /// It only fills a ClearSet. BoardResolver does the actual clearing.
    /// </summary>
    public sealed class SpecialResolver
    {
        /// <summary>A run of this length or more (and nothing crossing it) makes a rocket.</summary>
        public const int RocketRunLength = 4;

        /// <summary>
        /// Decides whether a match creates a special tile.
        ///   L, T or + shape (a horizontal and a vertical run cross) -> Bomb
        ///   a straight run of 4 or more horizontally -> RocketVertical (clears a column)
        ///   a straight run of 4 or more vertically   -> RocketHorizontal (clears a row)
        /// Where it appears: in the swapped cell if that cell is part of the match (the one the player moved
        /// the tile to is tried first), otherwise at the junction of a bomb shape, otherwise in the middle of the run.
        /// Pass hasSwap = false for cascade matches, which have no swapped cells.
        /// </summary>
        public bool TryGetCreation(Match match, bool hasSwap, GridPos swapA, GridPos swapB, out SpecialCreation creation)
        {
            SpecialType special;
            if (match.HasCrossing)
            {
                special = SpecialType.Bomb;
            }
            else if (match.LongestHorizontalRun >= RocketRunLength)
            {
                special = SpecialType.RocketVertical;
            }
            else if (match.LongestVerticalRun >= RocketRunLength)
            {
                special = SpecialType.RocketHorizontal;
            }
            else
            {
                creation = default;
                return false;
            }

            GridPos position;
            if (hasSwap && match.Positions.Contains(swapB)) position = swapB;
            else if (hasSwap && match.Positions.Contains(swapA)) position = swapA;
            else if (match.HasCrossing) position = match.Crossing;
            else position = match.Positions[match.Positions.Count / 2];

            creation = new SpecialCreation(position, match.Color, special);
            return true;
        }

        /// <summary>
        /// Goes through the set and sets off every special tile in it, once each. The cells a special hits are
        /// added to the set, and if one of them is a special too it is set off in turn (chain reaction).
        /// Because new cells are appended at the end while we walk the set, each link of the chain is one step
        /// deeper than the one before.
        /// </summary>
        public void ActivateSpecials(Board board, ClearSet set)
        {
            for (int i = 0; i < set.Count; i++)
            {
                GridPos pos = set.PositionAt(i);
                Tile tile = board.Get(pos);
                if (tile.Special == SpecialType.None || set.IsActivated(pos)) continue;

                set.MarkActivated(pos);
                int depth = set.DepthAt(pos) + 1;

                switch (tile.Special)
                {
                    case SpecialType.RocketHorizontal:
                        MarkRow(board, set, pos.Y, depth);
                        break;
                    case SpecialType.RocketVertical:
                        MarkColumn(board, set, pos.X, depth);
                        break;
                    case SpecialType.Bomb:
                        MarkSquare(board, set, pos, 1, depth);
                        break;
                }
            }
        }

        /// <summary>
        /// Two specials were swapped together. Both are used up (they go into the set at depth 0 and count as
        /// already activated), and the combo clears an area centered on the cell the player swiped to:
        ///   Rocket + Rocket -> its row and its column (a cross)
        ///   Rocket + Bomb   -> 3 rows and 3 columns
        ///   Bomb + Bomb     -> a 5x5 square
        /// The area is clipped at the edges. Other specials inside it are set off by ActivateSpecials afterwards.
        /// </summary>
        public void MarkCombo(Board board, ClearSet set, GridPos swappedFrom, GridPos center, SpecialType first, SpecialType second)
        {
            set.Mark(swappedFrom, 0);
            set.Mark(center, 0);
            set.MarkActivated(swappedFrom);
            set.MarkActivated(center);

            bool firstIsBomb = first == SpecialType.Bomb;
            bool secondIsBomb = second == SpecialType.Bomb;

            if (firstIsBomb && secondIsBomb)
            {
                MarkSquare(board, set, center, 2, 1);
            }
            else if (firstIsBomb || secondIsBomb)
            {
                for (int offset = -1; offset <= 1; offset++)
                {
                    MarkRow(board, set, center.Y + offset, 1);
                    MarkColumn(board, set, center.X + offset, 1);
                }
            }
            else
            {
                MarkRow(board, set, center.Y, 1);
                MarkColumn(board, set, center.X, 1);
            }
        }

        private static void MarkRow(Board board, ClearSet set, int y, int depth)
        {
            for (int x = 0; x < board.Width; x++)
            {
                set.Mark(new GridPos(x, y), depth); // a row outside the board adds nothing: Mark ignores it
            }
        }

        private static void MarkColumn(Board board, ClearSet set, int x, int depth)
        {
            for (int y = 0; y < board.Height; y++)
            {
                set.Mark(new GridPos(x, y), depth);
            }
        }

        // Every cell within 'radius' of the center, in both directions (radius 1 = 3x3, radius 2 = 5x5).
        private static void MarkSquare(Board board, ClearSet set, GridPos center, int radius, int depth)
        {
            for (int y = center.Y - radius; y <= center.Y + radius; y++)
            {
                for (int x = center.X - radius; x <= center.X + radius; x++)
                {
                    set.Mark(new GridPos(x, y), depth);
                }
            }
        }
    }
}
