namespace Match3.Core
{
    /// <summary>One possible swap: the player drags the tile at A onto its neighbor at B.</summary>
    public readonly struct SwapMove
    {
        public readonly GridPos A;
        public readonly GridPos B;

        public SwapMove(GridPos a, GridPos b)
        {
            A = a;
            B = b;
        }

        public override string ToString() => A + " <-> " + B;
    }
}
