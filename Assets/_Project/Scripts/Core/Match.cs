using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// One match: all the cells of one color that belong together.
    /// Several overlapping runs (an L or T shape) are merged into a single Match.
    /// Positions are listed bottom row first, left to right.
    /// </summary>
    public sealed class Match
    {
        public TileColor Color { get; }
        public List<GridPos> Positions { get; } = new List<GridPos>();

        public Match(TileColor color)
        {
            Color = color;
        }
    }
}
