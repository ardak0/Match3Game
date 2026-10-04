using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>A tile that was removed from the board, and where it was.</summary>
    public readonly struct ClearedTile
    {
        public readonly int TileId;
        public readonly TileColor Color;
        public readonly GridPos Position;

        /// <summary>What the tile was. A special here means it went off (rocket beam, bomb blast) as it was cleared.</summary>
        public readonly SpecialType Special;

        /// <summary>
        /// How many chain-reaction links away from the player's match this tile is.
        /// 0 = part of a match (or the swapped special itself), 1 = hit by a special that was at depth 0, and so on.
        /// The view uses it to let a chain ripple outwards instead of clearing everything at once.
        /// </summary>
        public readonly int ChainDepth;

        public ClearedTile(int tileId, TileColor color, GridPos position, SpecialType special = SpecialType.None, int chainDepth = 0)
        {
            TileId = tileId;
            Color = color;
            Position = position;
            Special = special;
            ChainDepth = chainDepth;
        }
    }

    /// <summary>These tiles were matched and removed from the board.</summary>
    public sealed class ClearStep : ResolveStep
    {
        public IReadOnlyList<ClearedTile> Tiles { get; }

        public ClearStep(int wave, IReadOnlyList<ClearedTile> tiles) : base(wave)
        {
            Tiles = tiles;
        }
    }
}
