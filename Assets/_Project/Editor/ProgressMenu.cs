using Match3.Game;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>
    /// Editor menu items for the saved progress, so a tester can start from a clean slate without hunting for the file.
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

        [MenuItem("Tools/Match3/Show Progress Folder")]
        private static void ShowProgressFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
