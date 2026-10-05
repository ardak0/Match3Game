using System;
using System.Collections.Generic;

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
    ///   combos      - what two specials swapped together clear (MarkCombo, MarkColorBombSwap)
    /// It only fills a ClearSet. BoardResolver does the actual clearing.
    ///
    /// The ColorBomb also needs two things a ClearSet cannot say, so the resolver reports them in lists that
    /// BoardResolver reads after each call and empties at the start of every wave (BeginWave):
    ///   Conversions    - plain tiles that must turn into rockets or bombs before they fire
    ///   ColorBombFires - which ColorBombs went off and which cells they reach (for the view's beams)
    /// The IRandom is only used to pick the direction of a rocket made from a tile.
    /// </summary>
    public sealed class SpecialResolver
    {
        /// <summary>A straight run of this length or more makes a ColorBomb. It beats every other shape.</summary>
        public const int ColorBombRunLength = 5;

        /// <summary>A run of this length or more (and nothing crossing it) makes a rocket.</summary>
        public const int RocketRunLength = 4;

        private readonly IRandom _random;
        private readonly List<SpecialConversion> _conversions = new List<SpecialConversion>();
        private readonly List<ColorBombFire> _colorBombFires = new List<ColorBombFire>();
        private readonly List<GridPos> _targets = new List<GridPos>(); // scratch: the cells of the ColorBomb being resolved
        private readonly int[] _colorCounts = new int[Enum.GetValues(typeof(TileColor)).Length];

        public SpecialResolver(IRandom random)
        {
            _random = random;
        }

        /// <summary>Tiles a ColorBomb combo turned into rockets or bombs since BeginWave. The board is NOT changed here.</summary>
        public IReadOnlyList<SpecialConversion> Conversions => _conversions;

        /// <summary>The ColorBombs that went off since BeginWave, in the order they fired.</summary>
        public IReadOnlyList<ColorBombFire> ColorBombFires => _colorBombFires;

        /// <summary>Forgets the conversions and fires of the previous wave. BoardResolver calls it at the start of every wave.</summary>
        public void BeginWave()
        {
            _conversions.Clear();
            _colorBombFires.Clear();
        }

        /// <summary>
        /// Decides whether a match creates a special tile. The first rule that fits wins:
        ///   a straight run of 5 or more, either way (even with another run crossing it) -> ColorBomb, which has no color
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
            TileColor color = match.Color;
            if (match.LongestHorizontalRun >= ColorBombRunLength || match.LongestVerticalRun >= ColorBombRunLength)
            {
                special = SpecialType.ColorBomb;
                color = TileColor.None;
            }
            else if (match.HasCrossing)
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

            creation = new SpecialCreation(position, color, special);
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
                    case SpecialType.ColorBomb:
                        // Only a ColorBomb caught in a blast gets here (a swapped one is already marked as activated).
                        if (TryFindMostCommonColor(board, out TileColor mostCommon))
                        {
                            MarkAllOfColor(board, set, mostCommon, depth, pos, SpecialType.None);
                        }

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

        /// <summary>
        /// A ColorBomb was swapped with a neighbor (the board already holds the swapped result, so the bomb is at A or B).
        /// The bomb is used up (depth 0, already activated), and what the partner is decides the rest:
        ///   normal tile   -> every tile of its color is cleared
        ///   rocket / bomb -> every PLAIN tile of its color turns into a rocket (random direction) or a bomb, and then
        ///                    all of them go off. The swapped special is not used up: it goes off like the others.
        ///                    Specials of that color that already exist keep their own power.
        ///   ColorBomb     -> the whole board is cleared
        /// The tiles it reaches are one chain link deep (depth 1). Specials among them are set off by ActivateSpecials.
        /// </summary>
        public void MarkColorBombSwap(Board board, ClearSet set, GridPos a, GridPos b)
        {
            bool aIsColorBomb = board.Get(a).Special == SpecialType.ColorBomb;
            bool bIsColorBomb = board.Get(b).Special == SpecialType.ColorBomb;

            GridPos source = bIsColorBomb ? b : a; // with two ColorBombs, the one at B fires
            GridPos partner = bIsColorBomb ? a : b;

            set.Mark(source, 0);
            set.MarkActivated(source);

            if (aIsColorBomb && bIsColorBomb)
            {
                set.Mark(partner, 0);
                set.MarkActivated(partner);
                MarkWholeBoard(board, set, source);
                return;
            }

            Tile partnerTile = board.Get(partner);
            MarkAllOfColor(board, set, partnerTile.Color, 1, source, partnerTile.Special);
        }

        // Marks every cell except the ones already in the set (the two ColorBombs). One beam target per cell.
        private void MarkWholeBoard(Board board, ClearSet set, GridPos source)
        {
            _targets.Clear();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    GridPos pos = new GridPos(x, y);
                    if (set.Mark(pos, 1)) _targets.Add(pos);
                }
            }

            RecordFire(board, source, 0);
        }

        // Marks every tile of the color at the given depth and records the ColorBomb's fire.
        // If partnerSpecial is a rocket or a bomb, plain tiles of the color are also queued for conversion.
        // Cells are scanned row by row, bottom row first, so the random numbers are used in a predictable order.
        private void MarkAllOfColor(Board board, ClearSet set, TileColor color, int depth, GridPos source, SpecialType partnerSpecial)
        {
            _targets.Clear();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    GridPos pos = new GridPos(x, y);
                    Tile tile = board.Get(pos);
                    if (tile == null || tile.Color != color) continue;

                    set.Mark(pos, depth);
                    _targets.Add(pos);

                    if (partnerSpecial != SpecialType.None && tile.Special == SpecialType.None)
                    {
                        _conversions.Add(new SpecialConversion(pos, ChooseConvertedSpecial(partnerSpecial)));
                    }
                }
            }

            RecordFire(board, source, depth - 1);
        }

        private SpecialType ChooseConvertedSpecial(SpecialType partnerSpecial)
        {
            if (partnerSpecial == SpecialType.Bomb) return SpecialType.Bomb;

            return _random.Next(0, 2) == 0 ? SpecialType.RocketHorizontal : SpecialType.RocketVertical;
        }

        private void RecordFire(Board board, GridPos source, int depth)
        {
            _colorBombFires.Add(new ColorBombFire(board.Get(source).Id, source, depth, _targets.ToArray()));
        }

        // The color with the most tiles on the board. On a tie the lowest color number (the one first in the enum) wins,
        // so the same board always gives the same answer. ColorBombs have no color and are not counted.
        // False if no tile has a color.
        private bool TryFindMostCommonColor(Board board, out TileColor color)
        {
            Array.Clear(_colorCounts, 0, _colorCounts.Length);
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Tile tile = board.Get(x, y);
                    if (tile != null && tile.Color != TileColor.None) _colorCounts[(int)tile.Color]++;
                }
            }

            int bestCount = 0;
            color = TileColor.None;
            for (int i = 0; i < _colorCounts.Length; i++)
            {
                if (_colorCounts[i] > bestCount) // strictly greater: an equal count never replaces the earlier color
                {
                    bestCount = _colorCounts[i];
                    color = (TileColor)i;
                }
            }

            return bestCount > 0;
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
