using System;
using Match3.Data;
using Match3.Game;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>
    /// Editor menu items for the saved progress, so a tester can start from a clean slate or jump to a later level
    /// without hunting for the file. This file is in an Editor folder, so Unity never puts it in a build.
    /// </summary>
    public static class ProgressMenu
    {
        [MenuItem("Tools/Match3/Reset Progress")]
        private static void ResetProgress()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Reset Progress",
                "Delete all saved stars and lock every level except the first?\n\nThe file is deleted, so this cannot be undone.",
                "Reset",
                "Cancel");

            if (!confirmed) return;

            ProgressSetup.CreateStore().Clear();
            Debug.Log("Progress reset. Deleted " + ProgressSetup.FilePath);
        }

        [MenuItem("Tools/Match3/Unlock All Levels")]
        private static void UnlockAll()
        {
            LevelCatalog catalog = FindCatalog();
            if (catalog != null) UnlockUpTo(catalog, catalog.Count);
        }

        [MenuItem("Tools/Match3/Unlock Levels...")]
        private static void OpenUnlockWindow()
        {
            EditorWindow.GetWindow<UnlockLevelsWindow>(true, "Unlock Levels");
        }

        // The game reads the progress file once when it starts, so changing it while playing would have no effect (and be overwritten on the next win).
        [MenuItem("Tools/Match3/Unlock All Levels", true)]
        [MenuItem("Tools/Match3/Unlock Levels...", true)]
        [MenuItem("Tools/Match3/Reset Progress", true)]
        private static bool NotWhilePlaying()
        {
            return !EditorApplication.isPlaying;
        }

        [MenuItem("Tools/Match3/Show Progress Folder")]
        private static void ShowProgressFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        /// <summary>
        /// Makes levelNumber (1 = the first level) playable: every level before it gets at least 1 star.
        /// Stars that are already earned are kept, and levels after it are not touched.
        /// </summary>
        private static void UnlockUpTo(LevelCatalog catalog, int levelNumber)
        {
            levelNumber = Mathf.Clamp(levelNumber, 1, catalog.Count);

            IProgressStore store = ProgressSetup.CreateStore();
            ProgressData data = store.Load();

            int[] stars = new int[catalog.Count];
            Array.Copy(data.stars, stars, Math.Min(data.stars.Length, stars.Length));

            for (int i = 0; i < levelNumber - 1; i++)
            {
                if (stars[i] == 0) stars[i] = 1;
            }

            data.stars = stars;
            store.Save(data);
            Debug.Log("Progress saved: levels 1 to " + levelNumber + " are playable.");
        }

        private static LevelCatalog FindCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelCatalog");
            if (guids.Length == 0)
            {
                Debug.LogWarning("No LevelCatalog asset found in the project.");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>A small window with a level number and an Unlock button.</summary>
        private sealed class UnlockLevelsWindow : EditorWindow
        {
            private int _levelNumber = 6;

            private void OnGUI()
            {
                _levelNumber = EditorGUILayout.IntField("Level to play", _levelNumber);

                if (GUILayout.Button("Unlock"))
                {
                    LevelCatalog catalog = FindCatalog();
                    if (catalog != null) UnlockUpTo(catalog, _levelNumber);
                }
            }
        }
    }
}
