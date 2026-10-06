using System.Collections.Generic;
using Match3.Core;
using Match3.Game;
using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// Everything that makes one level different from another: board size, how many colors, how many moves,
    /// what the goals are, where the obstacles are, and the random seed. A level designer edits these in the Inspector, no code needed.
    /// Create one with: right-click in the Project window, Create > Match3 > Level Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_01", menuName = "Match3/Level Data")]
    public sealed class LevelData : ScriptableObject
    {
        [Header("Board")]
        [SerializeField, Range(LevelRules.MinBoardSize, LevelRules.MaxWidth)] private int width = 8;
        [SerializeField, Range(LevelRules.MinBoardSize, LevelRules.MaxHeight)] private int height = 8;
        [Tooltip("The level uses the first N colors: Red, Green, Blue, Yellow, Purple, Orange.")]
        [SerializeField, Range(LevelRules.MinColors, LevelRules.MaxColors)] private int colorCount = 5;

        [Header("Rules")]
        [SerializeField, Min(1)] private int moveLimit = 20;
        [SerializeField] private GoalDefinition[] goals =
        {
            new GoalDefinition(TileColor.Red, 12),
            new GoalDefinition(TileColor.Blue, 12)
        };

        [Header("Obstacles")]
        [Tooltip("One text per board row, the TOP row first, one character per cell (the text must be as long as the board is wide):\n. nothing   C crate 1 HP   D crate 2 HP   I ice 1 HP   J ice 2 HP   L chained tile\nLeave the list empty for a level without obstacles.")]
        [SerializeField] private string[] obstacleRows;

        [Header("Stars (moves left when the level is won; winning at all is 1 star)")]
        [Tooltip("Win with at least this many moves left for 2 stars.")]
        [SerializeField, Min(1)] private int twoStarMovesLeft = 8;
        [Tooltip("Win with at least this many moves left for 3 stars. Must be more than the 2-star number and less than the move limit.")]
        [SerializeField, Min(2)] private int threeStarMovesLeft = 12;

        [Header("Randomness")]
        [Tooltip("Tick this to get a different board every time the level starts. Untick it and set Seed to replay the same board.")]
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 12345;

        public int Width => width;
        public int Height => height;
        public int ColorCount => colorCount;
        public int MoveLimit => moveLimit;
        public IReadOnlyList<GoalDefinition> Goals => goals;
        public IReadOnlyList<string> ObstacleRows => obstacleRows;
        public StarThresholds Stars => new StarThresholds(twoStarMovesLeft, threeStarMovesLeft);
        public bool UseRandomSeed => randomSeed;
        public int Seed => seed;

        /// <summary>The same level as plain values, for the simulator (which lives in Core and knows nothing about Unity assets).</summary>
        public LevelConfig ToSimulationConfig()
        {
            return new LevelConfig(width, height, colorCount, moveLimit, goals, obstacleRows);
        }

        /// <summary>Null if the level is playable, otherwise a sentence that says what is wrong.</summary>
        public string GetProblem()
        {
            return LevelRules.GetLevelProblem(width, height, colorCount, moveLimit, goals)
                ?? LevelRules.GetObstacleProblem(width, height, obstacleRows, goals)
                ?? LevelRules.GetStarProblem(moveLimit, twoStarMovesLeft, threeStarMovesLeft);
        }

        // Runs in the editor whenever a value changes: a broken level is reported as soon as you make it.
        private void OnValidate()
        {
            string problem = GetProblem();
            if (problem != null) Debug.LogWarning(name + ": " + problem, this);
        }
    }
}
