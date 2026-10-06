using System;
using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// Carries one number from the Home scene to the Game scene: which level the player picked.
    /// It is an asset that both scenes point at, so no object has to survive the scene change and nothing is static.
    /// The value is [NonSerialized], so it is never saved into the asset file: a fresh start always has no selection.
    /// Create one with: right-click in the Project window, Create > Match3 > Game Session.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSession", menuName = "Match3/Game Session")]
    public sealed class GameSession : ScriptableObject
    {
        [NonSerialized] private int _selectedLevelIndex = -1;

        /// <summary>False when the Game scene was opened directly (for example with Play in the editor).</summary>
        public bool HasSelection => _selectedLevelIndex >= 0;

        public int SelectedLevelIndex => _selectedLevelIndex;

        public void Select(int levelIndex)
        {
            _selectedLevelIndex = levelIndex;
        }

        public void Clear()
        {
            _selectedLevelIndex = -1;
        }
    }
}
