namespace Match3.Core
{
    /// <summary>
    /// The two tiles at A and B traded places (the player's move).
    /// TileIdA is the id of the tile that WAS at A before the swap (it ends up at B),
    /// and TileIdB is the id of the tile that was at B (it ends up at A).
    /// The ids let the view find the right tile objects to move.
    /// </summary>
    public sealed class SwapStep : ResolveStep
    {
        public GridPos A { get; }
        public GridPos B { get; }
        public int TileIdA { get; }
        public int TileIdB { get; }

        public SwapStep(int wave, GridPos a, GridPos b, int tileIdA, int tileIdB) : base(wave)
        {
            A = a;
            B = b;
            TileIdA = tileIdA;
            TileIdB = tileIdB;
        }
    }
}
