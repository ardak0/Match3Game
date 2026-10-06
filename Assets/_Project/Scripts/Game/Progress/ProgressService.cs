using System;
using System.Collections.Generic;

namespace Match3.Game
{
    /// <summary>
    /// The player's progress: which levels are unlocked and how many stars each has. Plain C# with no Unity types.
    /// It is created with a store (where progress is saved) and one StarThresholds per level (in level order).
    /// </summary>
    public sealed class ProgressService
    {
        private readonly IProgressStore _store;
        private readonly StarThresholds[] _thresholds;
        private readonly int[] _stars; // best stars per level, 0 = not won yet

        public ProgressService(IProgressStore store, IReadOnlyList<StarThresholds> thresholds)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (thresholds == null) throw new ArgumentNullException(nameof(thresholds));

            _store = store;

            _thresholds = new StarThresholds[thresholds.Count];
            for (int i = 0; i < _thresholds.Length; i++) _thresholds[i] = thresholds[i];

            _stars = new int[_thresholds.Length];
            LoadFromStore();
        }

        public int LevelCount => _thresholds.Length;

        public int MaxStars => LevelCount * StarThresholds.MaxStars;

        public int TotalStars
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _stars.Length; i++) total += _stars[i];
                return total;
            }
        }

        /// <summary>
        /// The first level that has not been won yet. If every level is won it is the last level, so Play still does something.
        /// A level is only unlocked after the one before it was won, so this level is always unlocked.
        /// </summary>
        public int NextLevelToPlay
        {
            get
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] == 0) return i;
                }

                return Math.Max(0, _stars.Length - 1);
            }
        }

        /// <summary>The first level is always open. Any other level opens when the one before it has been won.</summary>
        public bool IsUnlocked(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= LevelCount) return false;
            if (levelIndex == 0) return true;
            return _stars[levelIndex - 1] > 0;
        }

        /// <summary>Best stars for the level: 0 if it was never won, otherwise 1 to 3.</summary>
        public int GetStars(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= LevelCount) return 0;
            return _stars[levelIndex];
        }

        /// <summary>
        /// Call when the player wins a level. Turns movesLeft into stars with that level's thresholds,
        /// keeps the best stars, unlocks the next level, and saves if anything improved.
        /// </summary>
        public WinResult RecordWin(int levelIndex, int movesLeft)
        {
            if (levelIndex < 0 || levelIndex >= LevelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex), "There is no level " + levelIndex + ".");
            }

            int stars = _thresholds[levelIndex].GetStars(movesLeft);
            int previousBest = _stars[levelIndex];

            bool improved = stars > previousBest;
            if (improved)
            {
                _stars[levelIndex] = stars;
                Save();
            }

            // The next level opens on the very first win of this one.
            bool unlockedNext = previousBest == 0 && levelIndex + 1 < LevelCount;
            return new WinResult(stars, previousBest, unlockedNext);
        }

        /// <summary>Forgets all progress, in memory and in the store. Only the first level is open afterwards.</summary>
        public void Reset()
        {
            Array.Clear(_stars, 0, _stars.Length);
            _store.Clear();
        }

        private void LoadFromStore()
        {
            ProgressData data = _store.Load();
            if (data == null || data.stars == null) return;

            // The save may hold more or fewer levels than the game has now (levels were added or removed),
            // and a hand-edited file could hold nonsense, so every value is clamped.
            int count = Math.Min(_stars.Length, data.stars.Length);
            for (int i = 0; i < count; i++)
            {
                _stars[i] = Math.Max(0, Math.Min(StarThresholds.MaxStars, data.stars[i]));
            }
        }

        private void Save()
        {
            _store.Save(new ProgressData { stars = (int[])_stars.Clone() });
        }
    }
}
