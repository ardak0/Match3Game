namespace Match3.Core
{
    /// <summary>
    /// A ColorBomb went off: which tile it was, where, how deep in a chain reaction (0 = swapped by the player),
    /// and the cells it reaches. The view draws a beam from the bomb to each target.
    /// </summary>
    public readonly struct ColorBombFire
    {
        public readonly int TileId;
        public readonly GridPos Position;
        public readonly int Depth;
        public readonly GridPos[] Targets;

        public ColorBombFire(int tileId, GridPos position, int depth, GridPos[] targets)
        {
            TileId = tileId;
            Position = position;
            Depth = depth;
            Targets = targets;
        }
    }
}
