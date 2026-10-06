using System;
using System.Collections.Generic;
using Match3.Core;

namespace Match3.Game
{
    /// <summary>
    /// Tracks how much of each goal is still to do: tiles of a color to clear, or obstacles of a type to destroy.
    /// The game flow feeds it the ResolveResult of every valid move; listeners react to GoalChanged.
    /// Two goals with the same color (or obstacle type) are counted separately.
    /// An obstacle only counts when it is destroyed, not when it is damaged.
    /// </summary>
    public sealed class GoalTracker
    {
        private readonly GoalDefinition[] _goals;
        private readonly int[] _remaining;

        public GoalTracker(IReadOnlyList<GoalDefinition> goals)
        {
            if (goals == null || goals.Count == 0) throw new ArgumentException("A level needs at least one goal.", nameof(goals));

            _goals = new GoalDefinition[goals.Count];
            _remaining = new int[goals.Count];

            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i].count < 1) throw new ArgumentException("Goal " + i + " must ask for at least one.", nameof(goals));

                _goals[i] = goals[i];
                _remaining[i] = goals[i].count;
            }
        }

        public int GoalCount => _goals.Length;

        public GoalKind GetKind(int goalIndex) => _goals[goalIndex].kind;

        /// <summary>The color of a color goal (ignore it for an obstacle goal).</summary>
        public TileColor GetColor(int goalIndex) => _goals[goalIndex].color;

        /// <summary>The obstacle type of an obstacle goal (ignore it for a color goal).</summary>
        public ObstacleType GetObstacle(int goalIndex) => _goals[goalIndex].obstacle;

        public int GetRemaining(int goalIndex) => _remaining[goalIndex];

        public bool AllGoalsMet
        {
            get
            {
                for (int i = 0; i < _remaining.Length; i++)
                {
                    if (_remaining[i] > 0) return false;
                }

                return true;
            }
        }

        /// <summary>Raised with (goal index, tiles still needed) each time a goal makes progress.</summary>
        public event Action<int, int> GoalChanged;

        /// <summary>Counts what one valid move cleared or destroyed (all waves of the cascade together).</summary>
        public void Register(ResolveResult result)
        {
            for (int i = 0; i < _goals.Length; i++)
            {
                if (_remaining[i] == 0) continue; // already done, nothing to report

                int done = _goals[i].kind == GoalKind.ClearObstacle
                    ? result.GetDestroyedCount(_goals[i].obstacle)
                    : result.GetClearedCount(_goals[i].color);
                if (done == 0) continue;

                _remaining[i] = Math.Max(0, _remaining[i] - done);
                GoalChanged?.Invoke(i, _remaining[i]);
            }
        }
    }
}
