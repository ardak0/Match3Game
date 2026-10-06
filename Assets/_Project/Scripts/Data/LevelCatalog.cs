using System.Collections.Generic;
using Match3.Game;
using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// The ordered list of all levels. Level 1 is the first entry. The Home scene (level map) and the Game scene
    /// both read it, so there is exactly one place that says which levels exist and in which order.
    /// Create one with: right-click in the Project window, Create > Match3 > Level Catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Match3/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [Tooltip("Played in this order. Drag the Level assets here.")]
        [SerializeField] private LevelData[] levels;

        public int Count => levels == null ? 0 : levels.Length;

        public LevelData Get(int index) => levels[index];

        /// <summary>One StarThresholds per level, in level order. This is what ProgressService is built from.</summary>
        public StarThresholds[] GetStarThresholds()
        {
            StarThresholds[] thresholds = new StarThresholds[Count];
            for (int i = 0; i < thresholds.Length; i++) thresholds[i] = levels[i].Stars;
            return thresholds;
        }

        /// <summary>Null if every level can be played, otherwise a sentence that says what is wrong.</summary>
        public string GetProblem()
        {
            if (Count == 0) return "The catalog has no levels.";

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == null) return "Entry " + i + " is empty.";

                string problem = levels[i].GetProblem();
                if (problem != null) return levels[i].name + ": " + problem;
            }

            return null;
        }

        private void OnValidate()
        {
            string problem = GetProblem();
            if (problem != null) Debug.LogWarning(name + ": " + problem, this);
        }
    }
}
