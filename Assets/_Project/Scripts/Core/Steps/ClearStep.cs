using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>A tile that was removed from the board, and where it was.</summary>
    public readonly struct ClearedTile
    {
        public readonly int TileId;
        public readonly TileColor Color;
        public readonly GridPos Position;

        public ClearedTile(int tileId, TileColor color, GridPos position)
        {
            TileId = tileId;
            Color = color;
            Position = position;
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
