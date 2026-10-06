namespace Match3.Core
{
    /// <summary>
    /// An obstacle is gone: a crate broke (its cell is empty now and gravity fills it), the ice cracked away,
    /// or a chain snapped (the tile it held stays and is free from now on).
    /// Level goals of the kind "clear N crates / ice / chains" count these steps.
    /// </summary>
    public sealed class ObstacleDestroyedStep : ResolveStep
    {
        public ObstacleType Type { get; }
        public GridPos Position { get; }

        public ObstacleDestroyedStep(int wave, ObstacleType type, GridPos position) : base(wave)
        {
            Type = type;
            Position = position;
        }
    }
}
