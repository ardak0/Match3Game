namespace Match3.Core
{
    /// <summary>
    /// What a tile does when it is cleared.
    /// RocketHorizontal clears its whole ROW, RocketVertical clears its whole COLUMN, Bomb clears the 3x3 around it.
    /// </summary>
    public enum SpecialType
    {
        None,
        RocketHorizontal,
        RocketVertical,
        Bomb
    }
}
