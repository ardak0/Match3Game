namespace Match3.Core
{
    /// <summary>
    /// One tile on the board. Immutable: a tile never changes color or special type.
    /// The Id is unique per tile for the whole game, so the view can map
    /// "model tile with Id 17" to "the TileView GameObject for tile 17".
    /// Create tiles with Board.NewTile so Ids are never duplicated.
    /// </summary>
    public sealed class Tile
    {
        public int Id { get; }
        public TileColor Color { get; }
        public SpecialType Special { get; }

        public Tile(int id, TileColor color, SpecialType special = SpecialType.None)
        {
            Id = id;
            Color = color;
            Special = special;
        }

        public override string ToString() => "Tile#" + Id + " " + Color + (Special == SpecialType.None ? "" : " " + Special);
    }
}
