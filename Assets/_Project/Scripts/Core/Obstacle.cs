namespace Match3.Core
{
    /// <summary>
    /// The obstacle in one cell: its type and how many hits it still takes (HP).
    /// Immutable, like Tile: taking a hit makes a new Obstacle with one HP less (see Damaged).
    /// A chain always has 1 HP: the first time its tile would be cleared, the chain breaks.
    /// </summary>
    public readonly struct Obstacle
    {
        public static readonly Obstacle None = default;

        public readonly ObstacleType Type;
        public readonly int Hp;

        public Obstacle(ObstacleType type, int hp)
        {
            Type = type;
            Hp = hp;
        }

        public bool IsNone => Type == ObstacleType.None;

        /// <summary>The same obstacle after one hit. At 0 HP it is destroyed, and the caller removes it from the board.</summary>
        public Obstacle Damaged() => new Obstacle(Type, Hp - 1);

        public override string ToString() => IsNone ? "None" : Type + " (" + Hp + " HP)";
    }
}
