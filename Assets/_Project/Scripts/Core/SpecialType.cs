namespace Match3.Core
{
    /// <summary>
    /// What a tile does when it is cleared. Everything is None until M6 adds special tiles,
    /// but Tile already carries the field so we don't have to reshape it later.
    /// </summary>
    public enum SpecialType
    {
        None,
        RocketHorizontal,
        RocketVertical,
        Bomb
    }
}
