namespace Match3.Core
{
    /// <summary>The two tiles at A and B traded places (the player's move).</summary>
    public sealed class SwapStep : ResolveStep
    {
        public GridPos A { get; }
        public GridPos B { get; }

        public SwapStep(int wave, GridPos a, GridPos b) : base(wave)
        {
            A = a;
            B = b;
        }
    }
}
