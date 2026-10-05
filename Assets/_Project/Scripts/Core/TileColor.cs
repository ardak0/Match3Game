namespace Match3.Core
{
    /// <summary>
    /// The six tile colors, plus None. A level uses the first 4-6 of them (see LevelData in M7).
    /// None is only for the ColorBomb, which has no color. It is LAST on purpose: Red..Orange keep the numbers 0..5,
    /// so everything that counts or indexes by color keeps working, and the board never spawns None tiles.
    /// </summary>
    public enum TileColor
    {
        Red,
        Green,
        Blue,
        Yellow,
        Purple,
        Orange,
        None
    }
}
