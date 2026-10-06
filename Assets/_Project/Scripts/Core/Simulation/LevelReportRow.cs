using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>One line of the difficulty report: a level's setup, both bots' reports, and its current and suggested stars.</summary>
    public sealed class LevelReportRow
    {
        public int LevelNumber { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int ColorCount { get; private set; }
        public int MoveLimit { get; private set; }

        /// <summary>For example "Red 12, Blue 12, Crate 6".</summary>
        public string GoalsText { get; private set; }

        /// <summary>For example "6 crate, 10 ice, 2 chain", or "-" for a level without obstacles.</summary>
        public string ObstaclesText { get; private set; }

        public SimulationReport Random { get; private set; }
        public SimulationReport Greedy { get; private set; }
        public DifficultyRating Rating { get; private set; }

        public int CurrentTwoStar { get; private set; }
        public int CurrentThreeStar { get; private set; }

        /// <summary>False if the greedy bot never won, so no stars can be suggested.</summary>
        public bool HasSuggestion { get; private set; }

        public int SuggestedTwoStar { get; private set; }
        public int SuggestedThreeStar { get; private set; }

        /// <summary>levelNumber counts from 1. The suggestion and the rating both come from the GREEDY report.</summary>
        public static LevelReportRow Create(int levelNumber, LevelConfig level, int currentTwoStar, int currentThreeStar,
            SimulationReport random, SimulationReport greedy)
        {
            LevelReportRow row = new LevelReportRow
            {
                LevelNumber = levelNumber,
                Width = level.Width,
                Height = level.Height,
                ColorCount = level.ColorCount,
                MoveLimit = level.MoveLimit,
                GoalsText = DescribeGoals(level.Goals),
                ObstaclesText = DescribeObstacles(level),
                Random = random,
                Greedy = greedy,
                Rating = DifficultyRater.FromGreedyWinRate(greedy.WinRate),
                CurrentTwoStar = currentTwoStar,
                CurrentThreeStar = currentThreeStar
            };

            row.HasSuggestion = StarSuggester.TrySuggest(greedy, level.MoveLimit, out int two, out int three);
            row.SuggestedTwoStar = two;
            row.SuggestedThreeStar = three;
            return row;
        }

        public static string DescribeGoals(IReadOnlyList<GoalDefinition> goals)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < goals.Count; i++)
            {
                string name = goals[i].kind == GoalKind.ClearObstacle ? goals[i].obstacle.ToString() : goals[i].color.ToString();
                parts.Add(name + " " + goals[i].count);
            }

            return string.Join(", ", parts);
        }

        public static string DescribeObstacles(LevelConfig level)
        {
            if (!ObstacleLayout.TryParse(level.ObstacleRows, level.Width, level.Height, out ObstacleLayout layout, out _)) return "invalid layout";

            List<string> parts = new List<string>();
            AddCount(parts, layout, ObstacleType.Crate, "crate");
            AddCount(parts, layout, ObstacleType.Ice, "ice");
            AddCount(parts, layout, ObstacleType.Chain, "chain");

            return parts.Count == 0 ? "-" : string.Join(", ", parts);
        }

        private static void AddCount(List<string> parts, ObstacleLayout layout, ObstacleType type, string name)
        {
            int count = layout.Count(type);
            if (count > 0) parts.Add(count + " " + name);
        }
    }
}
