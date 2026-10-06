using System;
using Match3.Core;

namespace Match3.Game
{
    /// <summary>What a goal asks the player to do.</summary>
    public enum GoalKind
    {
        /// <summary>Clear tiles of one color. This is the default (value 0), so levels saved before obstacles stay color goals.</summary>
        CollectColor = 0,

        /// <summary>Destroy obstacles of one type (crates, ice or chains).</summary>
        ClearObstacle = 1
    }

    /// <summary>
    /// One goal of a level: "collect N tiles of color X" or "clear N obstacles of type Y".
    /// The fields are public (and lower-case) only so Unity can show and save them in the Inspector.
    /// A color goal uses color and count; an obstacle goal uses obstacle and count (the other field is ignored).
    /// </summary>
    [Serializable]
    public struct GoalDefinition
    {
        public GoalKind kind;
        public TileColor color;
        public ObstacleType obstacle;
        public int count;

        /// <summary>A color goal: clear this many tiles of this color.</summary>
        public GoalDefinition(TileColor color, int count)
        {
            kind = GoalKind.CollectColor;
            this.color = color;
            obstacle = ObstacleType.None;
            this.count = count;
        }

        /// <summary>An obstacle goal: destroy this many obstacles of this type.</summary>
        public static GoalDefinition ForObstacle(ObstacleType obstacle, int count)
        {
            return new GoalDefinition { kind = GoalKind.ClearObstacle, obstacle = obstacle, count = count };
        }
    }
}
