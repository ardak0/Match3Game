using System.IO;
using Match3.Data;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>
    /// Creates, duplicates and deletes level assets, and keeps the LevelCatalog's list in step with them.
    /// The Level Editor window calls these; they hold no window state, so each one is easy to read on its own.
    /// The catalog's list is edited through a SerializedObject, which is what makes the change undoable and saved.
    /// </summary>
    public static class LevelCatalogTools
    {
        private const string FallbackFolder = "Assets/_Project/Levels";
        private const string CatalogListField = "levels";

        /// <summary>The first LevelCatalog asset in the project, or null if there is none.</summary>
        public static LevelCatalog FindCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelCatalog");
            if (guids.Length == 0) return null;

            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>Makes a new level with the default values (see LevelData), saves it next to the other levels and adds it to the end of the catalog.</summary>
        public static LevelData CreateLevel(LevelCatalog catalog)
        {
            LevelData level = ScriptableObject.CreateInstance<LevelData>();

            string path = AssetDatabase.GenerateUniqueAssetPath(GetLevelFolder(catalog) + "/Level_" + (catalog.Count + 1).ToString("00") + ".asset");
            AssetDatabase.CreateAsset(level, path);

            AppendToCatalog(catalog, level);
            AssetDatabase.SaveAssets();
            return level;
        }

        /// <summary>Copies a level asset (all its values) and adds the copy to the end of the catalog.</summary>
        public static LevelData DuplicateLevel(LevelCatalog catalog, LevelData source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string copyPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            AssetDatabase.CopyAsset(sourcePath, copyPath);

            LevelData copy = AssetDatabase.LoadAssetAtPath<LevelData>(copyPath);
            AppendToCatalog(catalog, copy);
            AssetDatabase.SaveAssets();
            return copy;
        }

        /// <summary>
        /// Removes the level from the catalog and moves its asset to the trash (so the operating system can still bring it back).
        /// Progress is saved per level NUMBER, so deleting a level in the middle shifts the numbers of the later levels.
        /// </summary>
        public static void DeleteLevel(LevelCatalog catalog, int index)
        {
            LevelData level = catalog.Get(index);

            SerializedObject catalogObject = new SerializedObject(catalog);
            SerializedProperty list = catalogObject.FindProperty(CatalogListField);

            // For an object reference, the first Delete only clears the entry and the second one removes it.
            list.GetArrayElementAtIndex(index).objectReferenceValue = null;
            list.DeleteArrayElementAtIndex(index);
            catalogObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);

            if (level != null) AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(level));
            AssetDatabase.SaveAssets();
        }

        private static void AppendToCatalog(LevelCatalog catalog, LevelData level)
        {
            SerializedObject catalogObject = new SerializedObject(catalog);
            SerializedProperty list = catalogObject.FindProperty(CatalogListField);

            int last = list.arraySize;
            list.InsertArrayElementAtIndex(last);
            list.GetArrayElementAtIndex(last).objectReferenceValue = level;

            catalogObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        // New levels go where the existing ones are.
        private static string GetLevelFolder(LevelCatalog catalog)
        {
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData level = catalog.Get(i);
                if (level == null) continue;

                return Path.GetDirectoryName(AssetDatabase.GetAssetPath(level)).Replace('\\', '/');
            }

            if (!AssetDatabase.IsValidFolder(FallbackFolder)) AssetDatabase.CreateFolder("Assets/_Project", "Levels");
            return FallbackFolder;
        }
    }
}
