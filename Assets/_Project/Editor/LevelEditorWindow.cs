using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Game;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>
    /// Tools > Match3 > Level Editor. Edits the LevelData assets of the LevelCatalog without touching the Inspector:
    ///   toolbar     pick a level, create / duplicate / delete one
    ///   problems    everything LevelRules finds wrong with the level, in red, updated while you edit
    ///   settings    size, colors, moves, seed, stars
    ///   grid        paint crates, ice and chains with the mouse
    ///   goals       add and remove goals
    ///   simulation  play the level with a bot a few hundred times (LevelSimulationPanel)
    /// Every change goes through a SerializedObject of the level asset. Applying it records an undo step and marks the asset
    /// as changed (we also call EditorUtility.SetDirty to be explicit). The LevelData class and its saved format are untouched.
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const int MaxGoalsInHud = 4; // the HUD row has room for four goals (see M11)

        private static readonly string[] GoalKindNames = { "Collect color", "Clear obstacle" };
        private static readonly string[] ColorNames = { "Red", "Green", "Blue", "Yellow", "Purple", "Orange" }; // TileColor without None
        private static readonly string[] ObstacleNames = { "Crate", "Ice", "Chain" };                          // ObstacleType without None

        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private int levelIndex;

        private SerializedObject _levelObject; // the selected level, edited through this
        private LevelGridPainter _painter;
        private LevelSimulationPanel _simulation;
        private Vector2 _scroll;

        [MenuItem("Tools/Match3/Level Editor")]
        private static void Open()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(430f, 560f);
        }

        private void OnEnable()
        {
            _painter = new LevelGridPainter(Repaint);
            _simulation = new LevelSimulationPanel(Repaint);
            if (catalog == null) catalog = LevelCatalogTools.FindCatalog();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            _simulation.Stop();
        }

        private void OnUndoRedo()
        {
            _levelObject = null;
            Repaint();
        }

        // ---------- the window ----------

        private void OnGUI()
        {
            DrawCatalogField();
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("No LevelCatalog found. Create one with Create > Match3 > Level Catalog, then drag it in above.", MessageType.Info);
                return;
            }

            DrawToolbar();

            if (catalog.Count == 0)
            {
                EditorGUILayout.HelpBox("The catalog has no levels yet. Press New Level.", MessageType.Info);
                return;
            }

            levelIndex = Mathf.Clamp(levelIndex, 0, catalog.Count - 1);
            LevelData level = catalog.Get(levelIndex);
            if (level == null)
            {
                EditorGUILayout.HelpBox("Entry " + (levelIndex + 1) + " of the catalog is empty. Fill it in the catalog's Inspector.", MessageType.Error);
                return;
            }

            if (_levelObject == null || _levelObject.targetObject != level) _levelObject = new SerializedObject(level);
            _levelObject.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int problemCount = DrawProblems(level);
            EditorGUILayout.Space(8);
            DrawSettings(level);
            EditorGUILayout.Space(8);
            DrawGrid(level);
            EditorGUILayout.Space(8);
            DrawGoals(level);
            EditorGUILayout.Space(8);
            _simulation.Draw(level, problemCount > 0, (two, three) => ApplyStars(level, two, three));
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Changes mark the level asset as unsaved. Press Save (or Ctrl+S) to write them to disk.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.EndScrollView();
        }

        private void DrawCatalogField()
        {
            EditorGUI.BeginChangeCheck();
            catalog = (LevelCatalog)EditorGUILayout.ObjectField("Catalog", catalog, typeof(LevelCatalog), false);
            if (EditorGUI.EndChangeCheck()) SelectLevel(0);
        }

        // ---------- toolbar ----------

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string[] names = new string[Mathf.Max(1, catalog.Count)];
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData entry = catalog.Get(i);
                names[i] = (i + 1) + "  " + (entry != null ? entry.name : "(empty)");
            }

            if (catalog.Count == 0) names[0] = "(no levels)";

            using (new EditorGUI.DisabledScope(catalog.Count == 0))
            {
                EditorGUI.BeginChangeCheck();
                int picked = EditorGUILayout.Popup(levelIndex, names, EditorStyles.toolbarPopup, GUILayout.Width(200f));
                if (EditorGUI.EndChangeCheck()) SelectLevel(picked);
            }

            if (GUILayout.Button("New Level", EditorStyles.toolbarButton)) NewLevel();

            using (new EditorGUI.DisabledScope(catalog.Count == 0 || catalog.Get(levelIndex) == null))
            {
                if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton)) DuplicateLevel();
                if (GUILayout.Button("Delete", EditorStyles.toolbarButton)) DeleteLevel();
                if (GUILayout.Button("Select Asset", EditorStyles.toolbarButton)) Selection.activeObject = catalog.Get(levelIndex);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();

            EditorGUILayout.EndHorizontal();
        }

        private void SelectLevel(int index)
        {
            levelIndex = index;
            _levelObject = null;
            _simulation.Reset(); // a report belongs to the level it was made for
            GUI.FocusControl(null);
        }

        private void NewLevel()
        {
            LevelData created = LevelCatalogTools.CreateLevel(catalog);
            SelectLevel(catalog.Count - 1);
            Selection.activeObject = created;
            GUIUtility.ExitGUI(); // the catalog changed under the layout: stop this OnGUI pass
        }

        private void DuplicateLevel()
        {
            LevelData copy = LevelCatalogTools.DuplicateLevel(catalog, catalog.Get(levelIndex));
            SelectLevel(catalog.Count - 1);
            Selection.activeObject = copy;
            GUIUtility.ExitGUI();
        }

        private void DeleteLevel()
        {
            LevelData level = catalog.Get(levelIndex);
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete level",
                "Delete level " + (levelIndex + 1) + " (" + level.name + ")?\n\n"
                + "It is removed from the catalog and its asset goes to the trash.\n"
                + "Saved progress is kept per level number, so the levels after it move up one number.",
                "Delete",
                "Cancel");

            if (confirmed)
            {
                LevelCatalogTools.DeleteLevel(catalog, levelIndex);
                SelectLevel(Mathf.Clamp(levelIndex, 0, Mathf.Max(0, catalog.Count - 1)));
            }

            GUIUtility.ExitGUI();
        }

        // ---------- problems ----------

        // Returns how many problems the level has.
        private int DrawProblems(LevelData level)
        {
            List<string> problems = LevelRules.GetAllProblems(
                level.Width, level.Height, level.ColorCount, level.MoveLimit, level.Goals,
                level.ObstacleRows, level.Stars.TwoStarMovesLeft, level.Stars.ThreeStarMovesLeft);

            GUIStyle red = new GUIStyle(EditorStyles.wordWrappedLabel);
            red.normal.textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.4f, 0.4f) : new Color(0.75f, 0f, 0f);

            if (problems.Count == 0)
            {
                GUIStyle green = new GUIStyle(EditorStyles.boldLabel);
                green.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.85f, 0.45f) : new Color(0f, 0.5f, 0.1f);
                EditorGUILayout.LabelField("This level is valid.", green);
                return 0;
            }

            GUIStyle heading = new GUIStyle(EditorStyles.boldLabel);
            heading.normal.textColor = red.normal.textColor;
            EditorGUILayout.LabelField("Problems (" + problems.Count + ")", heading);

            for (int i = 0; i < problems.Count; i++) EditorGUILayout.LabelField("- " + problems[i], red);

            return problems.Count;
        }

        // ---------- settings ----------

        private void DrawSettings(LevelData level)
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            SerializedProperty width = _levelObject.FindProperty("width");
            SerializedProperty height = _levelObject.FindProperty("height");
            int oldWidth = width.intValue;
            int oldHeight = height.intValue;

            EditorGUILayout.IntSlider(width, LevelRules.MinBoardSize, LevelRules.MaxWidth, "Width");
            EditorGUILayout.IntSlider(height, LevelRules.MinBoardSize, LevelRules.MaxHeight, "Height");
            EditorGUILayout.IntSlider(_levelObject.FindProperty("colorCount"), LevelRules.MinColors, LevelRules.MaxColors, "Colors");
            EditorGUILayout.PropertyField(_levelObject.FindProperty("moveLimit"), new GUIContent("Move limit"));

            SerializedProperty randomSeed = _levelObject.FindProperty("randomSeed");
            EditorGUILayout.PropertyField(randomSeed, new GUIContent("Random board each start"));
            using (new EditorGUI.DisabledScope(randomSeed.boolValue))
            {
                EditorGUILayout.PropertyField(_levelObject.FindProperty("seed"), new GUIContent("Seed"));
            }

            EditorGUILayout.PropertyField(_levelObject.FindProperty("twoStarMovesLeft"), new GUIContent("2 stars: moves left"));
            EditorGUILayout.PropertyField(_levelObject.FindProperty("threeStarMovesLeft"), new GUIContent("3 stars: moves left"));

            // A new board size keeps the obstacles that still fit (the top-left corner stays where it is).
            if (width.intValue != oldWidth || height.intValue != oldHeight)
            {
                string[] resized = ObstacleRows.Normalize(ObstacleRows.Resize(level.ObstacleRows, width.intValue, height.intValue));
                WriteRows(_levelObject.FindProperty("obstacleRows"), resized);
            }

            ApplyChanges(level);
        }

        private void ApplyStars(LevelData level, int twoStar, int threeStar)
        {
            _levelObject.Update();
            _levelObject.FindProperty("twoStarMovesLeft").intValue = twoStar;
            _levelObject.FindProperty("threeStarMovesLeft").intValue = threeStar;
            ApplyChanges(level);
        }

        // ---------- grid ----------

        private void DrawGrid(LevelData level)
        {
            EditorGUILayout.LabelField("Obstacles", EditorStyles.boldLabel);
            _painter.DrawPalette();
            EditorGUILayout.LabelField("Left mouse paints, right mouse erases. Row 1 is the top row of the board.", EditorStyles.wordWrappedMiniLabel);

            GridPos? unfillable = null;
            string counts = "No obstacles.";

            if (ObstacleLayout.TryParse(level.ObstacleRows, level.Width, level.Height, out ObstacleLayout layout, out _))
            {
                if (layout.TryFindUnfillableCell(out GridPos cell)) unfillable = cell;
                counts = "Crates " + layout.Count(ObstacleType.Crate) + "    Ice " + layout.Count(ObstacleType.Ice) + "    Chains " + layout.Count(ObstacleType.Chain);
            }

            if (_painter.Draw(level.ObstacleRows, level.Width, level.Height, unfillable, position.width, out string[] changedRows))
            {
                WriteRows(_levelObject.FindProperty("obstacleRows"), ObstacleRows.Normalize(changedRows));
                ApplyChanges(level);
            }

            EditorGUILayout.LabelField(counts, EditorStyles.miniLabel);
        }

        // ---------- goals ----------

        private void DrawGoals(LevelData level)
        {
            EditorGUILayout.LabelField("Goals", EditorStyles.boldLabel);

            SerializedProperty goals = _levelObject.FindProperty("goals");
            ObstacleLayout.TryParse(level.ObstacleRows, level.Width, level.Height, out ObstacleLayout layout, out _);

            int removeIndex = -1;
            for (int i = 0; i < goals.arraySize; i++)
            {
                SerializedProperty goal = goals.GetArrayElementAtIndex(i);
                if (DrawGoalRow(goal, layout)) removeIndex = i;
            }

            if (removeIndex >= 0) goals.DeleteArrayElementAtIndex(removeIndex);

            if (GUILayout.Button("Add goal", GUILayout.Width(100f)))
            {
                int last = goals.arraySize;
                goals.InsertArrayElementAtIndex(last);
                SerializedProperty added = goals.GetArrayElementAtIndex(last);
                added.FindPropertyRelative("kind").enumValueIndex = (int)GoalKind.CollectColor;
                added.FindPropertyRelative("color").enumValueIndex = (int)TileColor.Red;
                added.FindPropertyRelative("obstacle").enumValueIndex = (int)ObstacleType.None;
                added.FindPropertyRelative("count").intValue = 10;
            }

            if (goals.arraySize > MaxGoalsInHud)
            {
                EditorGUILayout.HelpBox("The HUD has room for " + MaxGoalsInHud + " goals. More will not fit on screen.", MessageType.Warning);
            }

            ApplyChanges(level);
        }

        // Draws one goal. Returns true if its remove button was pressed.
        private static bool DrawGoalRow(SerializedProperty goal, ObstacleLayout layout)
        {
            SerializedProperty kind = goal.FindPropertyRelative("kind");
            SerializedProperty color = goal.FindPropertyRelative("color");
            SerializedProperty obstacle = goal.FindPropertyRelative("obstacle");
            SerializedProperty count = goal.FindPropertyRelative("count");

            bool remove;
            EditorGUILayout.BeginHorizontal();

            int newKind = EditorGUILayout.Popup(kind.enumValueIndex, GoalKindNames, GUILayout.Width(110f));
            if (newKind != kind.enumValueIndex)
            {
                kind.enumValueIndex = newKind;
                obstacle.enumValueIndex = newKind == (int)GoalKind.ClearObstacle ? (int)ObstacleType.Crate : (int)ObstacleType.None;
            }

            if (kind.enumValueIndex == (int)GoalKind.CollectColor)
            {
                int colorIndex = Mathf.Clamp(color.enumValueIndex, 0, ColorNames.Length - 1);
                color.enumValueIndex = EditorGUILayout.Popup(colorIndex, ColorNames, GUILayout.Width(90f));
            }
            else
            {
                int obstacleIndex = Mathf.Clamp(obstacle.enumValueIndex - 1, 0, ObstacleNames.Length - 1); // ObstacleType.None is 0
                obstacle.enumValueIndex = EditorGUILayout.Popup(obstacleIndex, ObstacleNames, GUILayout.Width(90f)) + 1;
            }

            count.intValue = EditorGUILayout.IntField(count.intValue, GUILayout.Width(50f));

            if (kind.enumValueIndex == (int)GoalKind.ClearObstacle && layout != null)
            {
                EditorGUILayout.LabelField("(" + layout.Count((ObstacleType)obstacle.enumValueIndex) + " in the grid)", GUILayout.Width(90f));
            }

            GUILayout.FlexibleSpace();
            remove = GUILayout.Button("Remove", GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();

            return remove;
        }

        // ---------- writing to the asset ----------

        private static void WriteRows(SerializedProperty property, string[] rows)
        {
            property.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++) property.GetArrayElementAtIndex(i).stringValue = rows[i];
        }

        // Stores the edited values in the asset: this is the undo step, and the asset is marked as changed.
        private void ApplyChanges(LevelData level)
        {
            if (_levelObject.ApplyModifiedProperties()) EditorUtility.SetDirty(level);
        }
    }
}
