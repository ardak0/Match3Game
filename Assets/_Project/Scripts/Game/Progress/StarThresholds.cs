namespace Match3.Game
{
    /// <summary>
    /// How many moves must be left when a level is won to earn 2 and 3 stars. Winning at all is 1 star.
    /// Plain C#: the LevelData asset holds the two numbers, this struct turns them into stars.
    /// </summary>
    public readonly struct StarThresholds
    {
        public const int MaxStars = 3;

        public StarThresholds(int twoStarMovesLeft, int threeStarMovesLeft)
        {
            TwoStarMovesLeft = twoStarMovesLeft;
            ThreeStarMovesLeft = threeStarMovesLeft;
        }

        public int TwoStarMovesLeft { get; }
        public int ThreeStarMovesLeft { get; }

        /// <summary>Stars for a win with this many moves left: 1, 2 or 3.</summary>
        public int GetStars(int movesLeft)
        {
            if (movesLeft >= ThreeStarMovesLeft) return 3;
            if (movesLeft >= TwoStarMovesLeft) return 2;
            return 1;
        }
    }
}
