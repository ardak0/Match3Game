namespace Match3.Core
{
    /// <summary>
    /// A match of 4 in a row or an L/T shape left a special tile behind.
    /// It is a NEW tile (new Id) placed in a cell that this wave just cleared. It appears after the ClearStep
    /// of its wave and before gravity, so tiles above it fall onto it like onto any other tile.
    /// </summary>
    public sealed class SpecialCreatedStep : ResolveStep
    {
        public int TileId { get; }
        public TileColor Color { get; }
        public SpecialType Special { get; }
        public GridPos Position { get; }

        public SpecialCreatedStep(int wave, int tileId, TileColor color, SpecialType special, GridPos position) : base(wave)
        {
            TileId = tileId;
            Color = color;
            Special = special;
            Position = position;
        }
    }
}
