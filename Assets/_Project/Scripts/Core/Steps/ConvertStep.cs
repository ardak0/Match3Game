using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>A plain tile that turned into a special tile. It keeps its id: it is the same piece with a new power.</summary>
    public readonly struct ConvertedTile
    {
        public readonly int TileId;
        public readonly TileColor Color;
        public readonly GridPos Position;
        public readonly SpecialType Special;

        public ConvertedTile(int tileId, TileColor color, GridPos position, SpecialType special)
        {
            TileId = tileId;
            Color = color;
            Position = position;
            Special = special;
        }
    }

    /// <summary>
    /// ColorBomb + rocket or bomb: every plain tile of that color turned into a rocket or bomb. It comes after the
    /// ColorBombFireStep and before the ClearStep of the same wave, in which those new specials go off.
    /// The view plays it as "tiles turning into rockets" before they fire.
    /// </summary>
    public sealed class ConvertStep : ResolveStep
    {
        public IReadOnlyList<ConvertedTile> Tiles { get; }

        public ConvertStep(int wave, IReadOnlyList<ConvertedTile> tiles) : base(wave)
        {
            Tiles = tiles;
        }
    }
}
