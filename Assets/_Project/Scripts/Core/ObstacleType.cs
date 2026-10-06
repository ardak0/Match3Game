namespace Match3.Core
{
    /// <summary>
    /// The three kinds of obstacle a cell can hold (None = nothing).
    ///   Crate - takes the cell instead of a tile. It never moves and cannot be swapped.
    ///   Ice   - a layer UNDER a tile. The tile on it moves and matches normally; the ice stays in its cell.
    ///   Chain - locks the tile in its cell: it cannot be swapped and does not fall, but it still forms matches.
    /// None is first on purpose, so a cell that was never given an obstacle (a zero-filled array) has none.
    /// </summary>
    public enum ObstacleType
    {
        None,
        Crate,
        Ice,
        Chain
    }
}
