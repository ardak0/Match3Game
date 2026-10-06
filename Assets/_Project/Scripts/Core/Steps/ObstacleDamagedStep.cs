namespace Match3.Core
{
    /// <summary>
    /// An obstacle took a hit and is still there: a 2 HP crate or a 2 HP ice layer that now has 1 HP left.
    /// (A hit that takes the last HP is an ObstacleDestroyedStep instead.)
    /// It comes right after the wave's ClearStep, so the view can show the crack as the tiles disappear.
    /// </summary>
    public sealed class ObstacleDamagedStep : ResolveStep
    {
        public ObstacleType Type { get; }
        public GridPos Position { get; }

        /// <summary>The HP the obstacle has left, always 1 or more.</summary>
        public int HpLeft { get; }

        public ObstacleDamagedStep(int wave, ObstacleType type, GridPos position, int hpLeft) : base(wave)
        {
            Type = type;
            Position = position;
            HpLeft = hpLeft;
        }
    }
}
