using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>A brand new tile that entered the board from above.</summary>
    public readonly struct TileSpawn
    {
        public readonly int TileId;
        public readonly TileColor Color;
        public readonly SpecialType Special;

        /// <summary>The cell where the tile ends up.</summary>
        public readonly GridPos To;

        /// <summary>
        /// The row (above the top of the board) the tile starts from. New tiles in one column get
        /// FromY = Height, Height + 1, ... from the lowest one up, so they queue above the board
        /// and the view can drop them in without guessing.
        /// </summary>
        public readonly int FromY;

        public TileSpawn(int tileId, TileColor color, SpecialType special, GridPos to, int fromY)
        {
            TileId = tileId;
            Color = color;
            Special = special;
            To = to;
            FromY = fromY;
        }
    }

    /// <summary>The refill: these new tiles were created to fill the empty cells that nothing blocks from above.</summary>
    public sealed class SpawnStep : ResolveStep
    {
        public IReadOnlyList<TileSpawn> Spawns { get; }

        /// <summary>Same meaning as FallStep.Round: steps of one wave with the same Round play together.</summary>
        public int Round { get; }

        public SpawnStep(int wave, IReadOnlyList<TileSpawn> spawns, int round = 0) : base(wave)
        {
            Spawns = spawns;
            Round = round;
        }
    }
}
