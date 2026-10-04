using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>One existing tile that moved straight down from one cell to another.</summary>
    public readonly struct TileMove
    {
        public readonly int TileId;
        public readonly GridPos From;
        public readonly GridPos To;

        public TileMove(int tileId, GridPos from, GridPos to)
        {
            TileId = tileId;
            From = from;
            To = to;
        }
    }

    /// <summary>Gravity: every tile in Moves fell down to fill gaps left by cleared tiles.</summary>
    public sealed class FallStep : ResolveStep
    {
        public IReadOnlyList<TileMove> Moves { get; }

        public FallStep(int wave, IReadOnlyList<TileMove> moves) : base(wave)
        {
            Moves = moves;
        }
    }
}
