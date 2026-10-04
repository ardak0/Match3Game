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

        public static readonly ResolveResult Invalid =
            new ResolveResult(false, new ResolveStep[0], new int[ColorCount], 0);

        private readonly int[] _clearedByColor;

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
        }

        /// <summary>How many tiles of this color were cleared during the whole resolve (used by level goals).</summary>
        public int GetClearedCount(TileColor color) => _clearedByColor[(int)color];
    }
}
