using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>One existing tile that moved down from one cell to another (a shuffle uses it for any direction).</summary>
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

    /// <summary>
    /// Gravity: every tile in Moves moved down to fill a gap. Most moves go straight down. With obstacles in the way
    /// a tile can also slide one cell diagonally down (From and To are then in different columns).
    ///
    /// A wave can have several FallSteps, because a tile may have to wait for another one to move first.
    /// Steps of one wave with the same Round play together; a higher Round plays after the lower ones.
    /// Without obstacles a wave has one FallStep and one SpawnStep, both with Round 0, exactly as before.
    /// </summary>
    public sealed class FallStep : ResolveStep
    {
        public IReadOnlyList<TileMove> Moves { get; }

        /// <summary>Steps of the same wave with the same Round play together; a higher Round plays later.</summary>
        public int Round { get; }

        public FallStep(int wave, IReadOnlyList<TileMove> moves, int round = 0) : base(wave)
        {
            Moves = moves;
            Round = round;
        }
    }
}
