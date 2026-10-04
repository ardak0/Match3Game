using System;
using Match3.Core;

namespace Match3.Game
{
    /// <summary>
    /// "Clear this many tiles of this color." The fields are public (and lower-case) only so Unity can
    /// show and save them in the Inspector. M7 uses the same type inside the LevelData asset.
    /// </summary>
    [Serializable]
    public struct GoalDefinition
    {
        public TileColor color;
        public int count;

        public GoalDefinition(TileColor color, int count)
        {
            this.color = color;
            this.count = count;
        }
    }
}
