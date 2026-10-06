namespace Match3.Core
{
    public enum DifficultyRating
    {
        Easy,
        Medium,
        Hard,
        VeryHard
    }

    /// <summary>
    /// Turns the win rate of the GREEDY bot (the better of the two bots, a rough skilled player) into a label:
    /// Easy above 80%, Medium 50 to 80%, Hard 20 to 50%, Very Hard below 20%.
    /// </summary>
    public static class DifficultyRater
    {
        public const double EasyAbove = 0.80;
        public const double MediumFrom = 0.50;
        public const double HardFrom = 0.20;

        public static DifficultyRating FromGreedyWinRate(double winRate)
        {
            if (winRate > EasyAbove) return DifficultyRating.Easy;
            if (winRate >= MediumFrom) return DifficultyRating.Medium;
            if (winRate >= HardFrom) return DifficultyRating.Hard;
            return DifficultyRating.VeryHard;
        }

        public static string GetLabel(DifficultyRating rating)
        {
            switch (rating)
            {
                case DifficultyRating.Easy: return "Easy";
                case DifficultyRating.Medium: return "Medium";
                case DifficultyRating.Hard: return "Hard";
                default: return "Very Hard";
            }
        }
    }
}
