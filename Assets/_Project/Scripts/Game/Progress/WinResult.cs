namespace Match3.Game
{
    /// <summary>What recording a win changed: the stars of this run, and what the level had before.</summary>
    public readonly struct WinResult
    {
        public WinResult(int stars, int previousBestStars, bool unlockedNextLevel)
        {
            Stars = stars;
            PreviousBestStars = previousBestStars;
            UnlockedNextLevel = unlockedNextLevel;
        }

        /// <summary>Stars earned by this run (1 to 3). It can be lower than the saved best.</summary>
        public int Stars { get; }

        /// <summary>The best stars before this run. 0 means the level had never been won.</summary>
        public int PreviousBestStars { get; }

        /// <summary>True if this win unlocked a level that was locked before.</summary>
        public bool UnlockedNextLevel { get; }

        public bool IsNewBest => Stars > PreviousBestStars;
    }
}
