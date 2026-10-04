using System.Collections.Generic;
using Match3.Game;
using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// Everything that makes one level different from another: board size, how many colors, how many moves,
    /// what the goals are, and the random seed. A level designer edits these in the Inspector, no code needed.
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
            new GoalDefinition(Match3.Core.TileColor.Red, 12),
            new GoalDefinition(Match3.Core.TileColor.Blue, 12)
        };

        [Header("Randomness")]
        [Tooltip("Tick this to get a different board every time the level starts. Untick it and set Seed to replay the same board.")]
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 12345;

        public int Width => width;
        public int Height => height;
        public int ColorCount => colorCount;
        public int MoveLimit => moveLimit;
        public IReadOnlyList<GoalDefinition> Goals => goals;
        public bool UseRandomSeed => randomSeed;
        public int Seed => seed;

        /// <summary>Null if the level is playable, otherwise a sentence that says what is wrong.</summary>
        public string GetProblem()
        {
            return LevelRules.GetLevelProblem(width, height, colorCount, moveLimit, goals);
        }

        // Runs in the editor whenever a value changes: a broken level is reported as soon as you make it.
        private void OnValidate()
        {
            string problem = GetProblem();
            if (problem != null) Debug.LogWarning(name + ": " + problem, this);
        }
    }
}
