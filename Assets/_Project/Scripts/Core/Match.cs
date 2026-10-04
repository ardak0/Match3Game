using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// One match: all the cells of one color that belong together.
    /// Several overlapping runs (an L or T shape) are merged into a single Match.
    /// Positions are listed bottom row first, left to right.
    /// MatchFinder also records the shape, which SpecialResolver uses to decide which special tile (if any) to create.
    /// </summary>
    public sealed class Match
    {
        public TileColor Color { get; }
        public List<GridPos> Positions { get; } = new List<GridPos>();

        /// <summary>Length of the longest horizontal run of 3 or more in this match (0 if it has none).</summary>
        public int LongestHorizontalRun { get; internal set; }

        /// <summary>Length of the longest vertical run of 3 or more in this match (0 if it has none).</summary>
        public int LongestVerticalRun { get; internal set; }

        /// <summary>True when a horizontal and a vertical run cross: an L, T or + shape.</summary>
        public bool HasCrossing { get; internal set; }

        /// <summary>The (lowest, then leftmost) cell where two runs cross. Only meaningful if HasCrossing.</summary>
        public GridPos Crossing { get; internal set; }

        public Match(TileColor color)
        {
            Color = color;
        }
    }
}
