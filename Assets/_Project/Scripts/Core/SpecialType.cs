namespace Match3.Core
{
    /// <summary>
    /// What a tile does when it is cleared.
    /// RocketHorizontal clears its whole ROW, RocketVertical clears its whole COLUMN, Bomb clears the 3x3 around it.
    /// ColorBomb (made by a straight run of 5 or more) has no color and never matches; swapped with a tile it clears
    /// every tile of that tile's color, and caught in another special's blast it clears the most common color.
    /// </summary>
    public enum SpecialType
    {
        None,
        RocketHorizontal,
        RocketVertical,
        Bomb,
        ColorBomb
    }
}
