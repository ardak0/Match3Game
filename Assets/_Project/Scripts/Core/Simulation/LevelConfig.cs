using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Everything the simulator needs to know about a level, as plain values (no Unity objects).
    /// LevelData.ToSimulationConfig() makes one from a level asset; tests make them by hand.
    /// A level uses the first ColorCount colors of TileColor, exactly like the real game does.
    /// </summary>
    public sealed class LevelConfig
    {
        public LevelConfig(int width, int height, int colorCount, int moveLimit,
            IReadOnlyList<GoalDefinition> goals, IReadOnlyList<string> obstacleRows)
        {
            if (goals == null || goals.Count == 0) throw new ArgumentException("A level needs at least one goal.", nameof(goals));
            if (moveLimit < 1) throw new ArgumentException("The move limit must be at least 1.", nameof(moveLimit));

            Width = width;
            Height = height;
            ColorCount = colorCount;
            MoveLimit = moveLimit;
            Goals = goals;
            ObstacleRows = obstacleRows;
        }

        public int Width { get; }
        public int Height { get; }
        public int ColorCount { get; }
        public int MoveLimit { get; }
        public IReadOnlyList<GoalDefinition> Goals { get; }

        /// <summary>One text per row, top row first (see ObstacleLayout). Null or empty means no obstacles.</summary>
        public IReadOnlyList<string> ObstacleRows { get; }

        /// <summary>The first ColorCount colors of TileColor: the colors that can be on this level's board.</summary>
        public TileColor[] GetColors()
        {
            TileColor[] colors = new TileColor[ColorCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = (TileColor)i;
            return colors;
        }
    }
}
