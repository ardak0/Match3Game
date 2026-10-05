using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// A ColorBomb goes off. It comes first in its wave (before any ConvertStep and the ClearStep), so the view can
    /// draw a beam from the bomb to every target before they clear. Depth says how many chain links in the bomb is,
    /// so a bomb caught in a blast fires later than one the player swapped.
    /// </summary>
    public sealed class ColorBombFireStep : ResolveStep
    {
        public int TileId { get; }
        public GridPos Position { get; }
        public int Depth { get; }
        public IReadOnlyList<GridPos> Targets { get; }

        public ColorBombFireStep(int wave, int tileId, GridPos position, int depth, IReadOnlyList<GridPos> targets) : base(wave)
        {
            TileId = tileId;
            Position = position;
            Depth = depth;
            Targets = targets;
        }
    }
}
