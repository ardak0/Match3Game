using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Everything the view and the game flow need to know about one swap.
    /// Invalid: the swap was not allowed, nothing changed on the board, Steps is empty.
    /// Valid: Steps lists, in order, what to animate. The board is already in its final state.
    /// </summary>
    public sealed class ResolveResult
    {
        private static readonly int ColorCount = Enum.GetValues(typeof(TileColor)).Length;
        private const int ObstacleTypeCount = 4; // the values of ObstacleType (None, Crate, Ice, Chain)

        public static readonly ResolveResult Invalid =
            new ResolveResult(false, new ResolveStep[0], new int[ColorCount], 0);

        private readonly int[] _clearedByColor;
        private readonly int[] _destroyedByType = new int[ObstacleTypeCount];
        private readonly int[] _damagedByType = new int[ObstacleTypeCount];

        public bool IsValid { get; }
        public IReadOnlyList<ResolveStep> Steps { get; }

        /// <summary>How many clear-fall-refill rounds ran. 1 means no cascade; 2 means one cascade; and so on.</summary>
        public int WaveCount { get; }

        public int TotalCleared { get; }

        public ResolveResult(IReadOnlyList<ResolveStep> steps, int[] clearedByColor, int waveCount)
            : this(true, steps, clearedByColor, waveCount)
        {
        }

        private ResolveResult(bool isValid, IReadOnlyList<ResolveStep> steps, int[] clearedByColor, int waveCount)
        {
            IsValid = isValid;
            Steps = steps;
            _clearedByColor = clearedByColor;
            WaveCount = waveCount;

            int total = 0;
            for (int i = 0; i < clearedByColor.Length; i++)
            {
                total += clearedByColor[i];
            }

            TotalCleared = total;

            // Obstacles that were destroyed, and obstacles that were hit but are still there, are counted once here,
            // so goals (and the simulator's bots) do not have to scan the steps.
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] is ObstacleDestroyedStep destroyed) _destroyedByType[(int)destroyed.Type]++;
                else if (steps[i] is ObstacleDamagedStep damaged) _damagedByType[(int)damaged.Type]++;
            }
        }

        /// <summary>How many tiles of this color were cleared during the whole resolve (used by level goals).</summary>
        public int GetClearedCount(TileColor color) => _clearedByColor[(int)color];

        /// <summary>How many obstacles of this type were destroyed during the whole resolve (used by obstacle goals).</summary>
        public int GetDestroyedCount(ObstacleType type) => _destroyedByType[(int)type];

        /// <summary>How many hits on obstacles of this type left the obstacle standing (a 2 HP crate or ice layer that now has 1 HP). Goals do not count these.</summary>
        public int GetDamagedCount(ObstacleType type) => _damagedByType[(int)type];
    }
}
