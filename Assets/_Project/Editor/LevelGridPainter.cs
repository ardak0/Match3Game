using System;
using System.Collections.Generic;
using Match3.Core;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>What the left mouse button paints. Each brush is one letter of the obstacle text.</summary>
    public enum LevelBrush
    {
        Eraser,
        Crate1,
        Crate2,
        Ice1,
        Ice2,
        Chain
    }

    /// <summary>
    /// Draws the obstacle grid of a level and lets the designer paint on it with the mouse.
    ///   left mouse button: paint with the chosen brush (click, or hold and drag)
    ///   right mouse button: erase
    /// Row 1 is the TOP row of the board, the same order as the text in the LevelData asset.
    /// A whole mouse stroke is collected in a copy of the rows; only when the button goes up does Draw hand the result back,
    /// so one stroke is one undo step and the asset is written once.
    /// </summary>
    public sealed class LevelGridPainter
    {
        private const float MaxCellSize = 38f;
        private const float MinCellSize = 18f;
        private const float LabelSize = 18f;

        private readonly Action _repaint;
        private LevelBrush _brush = LevelBrush.Crate1;

        private string[] _strokeRows;   // the rows being painted (null when no stroke is going on)
        private char _strokeLetter;
        private bool _strokeChanged;
        private int _lastColumn = -1;
        private int _lastRow = -1;

        private GUIStyle _letterStyle;

        public LevelGridPainter(Action repaint)
        {
            _repaint = repaint;
        }

        // ---------- the look of each letter (colors are the editor's own, not the game art) ----------

        private static char GetLetter(LevelBrush brush)
        {
            switch (brush)
            {
                case LevelBrush.Crate1: return ObstacleRows.Crate1;
                case LevelBrush.Crate2: return ObstacleRows.Crate2;
                case LevelBrush.Ice1: return ObstacleRows.Ice1;
                case LevelBrush.Ice2: return ObstacleRows.Ice2;
                case LevelBrush.Chain: return ObstacleRows.Chain;
                default: return ObstacleRows.Empty;
            }
        }

        private static string GetName(LevelBrush brush)
        {
            switch (brush)
            {
                case LevelBrush.Crate1: return "Crate 1hp";
                case LevelBrush.Crate2: return "Crate 2hp";
                case LevelBrush.Ice1: return "Ice 1hp";
                case LevelBrush.Ice2: return "Ice 2hp";
                case LevelBrush.Chain: return "Chain";
                default: return "Empty / Eraser";
            }
        }

        private static Color GetColor(char letter)
        {
            switch (letter)
            {
                case ObstacleRows.Crate1: return new Color(0.80f, 0.60f, 0.35f);
                case ObstacleRows.Crate2: return new Color(0.55f, 0.35f, 0.15f);
                case ObstacleRows.Ice1: return new Color(0.65f, 0.88f, 0.98f);
                case ObstacleRows.Ice2: return new Color(0.30f, 0.60f, 0.90f);
                case ObstacleRows.Chain: return new Color(0.60f, 0.60f, 0.65f);
                default: return EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.80f, 0.80f, 0.80f);
            }
        }

        // ---------- the brush palette ----------

        public void DrawPalette()
        {
            EditorGUILayout.BeginHorizontal();
            foreach (LevelBrush brush in Enum.GetValues(typeof(LevelBrush)))
            {
                Rect rect = GUILayoutUtility.GetRect(60f, 42f, GUILayout.ExpandWidth(true));
                bool selected = brush == _brush;

                EditorGUI.DrawRect(rect, selected ? new Color(0.25f, 0.55f, 1f, 0.6f) : new Color(0f, 0f, 0f, 0.15f));

                Rect swatch = new Rect(rect.x + (rect.width - 20f) / 2f, rect.y + 3f, 20f, 20f);
                DrawCell(swatch, GetLetter(brush));

                GUI.Label(new Rect(rect.x, rect.y + 24f, rect.width, 16f), GetName(brush), SmallCenteredLabel());

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) _brush = brush;
            }

            EditorGUILayout.EndHorizontal();
        }

        private static GUIStyle SmallCenteredLabel()
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            return style;
        }

        // ---------- the grid ----------

        /// <summary>
        /// Draws the grid and handles the mouse. Returns true in the one call where a stroke has just ended and changed something:
        /// changedRows then holds the new rows (a full width x height grid) for the caller to store. Otherwise changedRows is null.
        /// </summary>
        /// <param name="unfillableCell">A cell nothing could ever fill (drawn with a red frame), or null.</param>
        public bool Draw(IReadOnlyList<string> rows, int width, int height, GridPos? unfillableCell, float availableWidth, out string[] changedRows)
        {
            changedRows = null;

            float cellSize = Mathf.Clamp((availableWidth - LabelSize - 12f) / width, MinCellSize, MaxCellSize);
            Rect area = GUILayoutUtility.GetRect(LabelSize + width * cellSize, LabelSize + height * cellSize);
            area.width = LabelSize + width * cellSize;
            Rect cells = new Rect(area.x + LabelSize, area.y + LabelSize, width * cellSize, height * cellSize);

            IReadOnlyList<string> shown = _strokeRows ?? rows; // while painting, show the stroke so far

            if (Event.current.type == EventType.Repaint)
            {
                DrawNumbers(cells, width, height, cellSize);

                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        Rect cell = new Rect(cells.x + column * cellSize, cells.y + row * cellSize, cellSize - 2f, cellSize - 2f);
                        DrawCell(cell, ObstacleRows.GetCell(shown, column, row));
                    }
                }

                if (unfillableCell.HasValue) DrawUnfillableFrame(cells, cellSize, unfillableCell.Value, height);
            }

            HandleMouse(cells, rows, width, height, cellSize, out changedRows);
            return changedRows != null;
        }

        private void HandleMouse(Rect cells, IReadOnlyList<string> rows, int width, int height, float cellSize, out string[] changedRows)
        {
            changedRows = null;

            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button > 1 || !cells.Contains(e.mousePosition)) break;

                    GUIUtility.hotControl = id; // from now on this control gets the mouse events, even outside the grid
                    _strokeRows = ObstacleRows.Resize(rows, width, height);
                    _strokeLetter = e.button == 1 ? ObstacleRows.Empty : GetLetter(_brush);
                    _strokeChanged = false;
                    _lastColumn = -1;
                    _lastRow = -1;
                    PaintAt(e.mousePosition, cells, width, height, cellSize);
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id) break;

                    PaintAt(e.mousePosition, cells, width, height, cellSize);
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id) break;

                    GUIUtility.hotControl = 0;
                    if (_strokeChanged) changedRows = _strokeRows;
                    _strokeRows = null;
                    e.Use();
                    _repaint();
                    break;
            }
        }

        private void PaintAt(Vector2 mouse, Rect cells, int width, int height, float cellSize)
        {
            int column = Mathf.FloorToInt((mouse.x - cells.x) / cellSize);
            int row = Mathf.FloorToInt((mouse.y - cells.y) / cellSize);

            if (column < 0 || column >= width || row < 0 || row >= height) return;
            if (column == _lastColumn && row == _lastRow) return; // still on the cell we just painted

            _lastColumn = column;
            _lastRow = row;

            if (ObstacleRows.GetCell(_strokeRows, column, row) == _strokeLetter) return;

            _strokeRows = ObstacleRows.SetCell(_strokeRows, width, height, column, row, _strokeLetter);
            _strokeChanged = true;
            _repaint();
        }

        // ---------- drawing helpers ----------

        private void DrawCell(Rect rect, char letter)
        {
            EditorGUI.DrawRect(rect, GetColor(letter));
            if (letter == ObstacleRows.Empty) return;

            if (_letterStyle == null)
            {
                _letterStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            }

            _letterStyle.fontSize = Mathf.Max(8, (int)(rect.height * 0.5f));
            _letterStyle.normal.textColor = (letter == ObstacleRows.Ice1) ? Color.black : Color.white;
            GUI.Label(rect, letter.ToString(), _letterStyle);
        }

        private static void DrawNumbers(Rect cells, int width, int height, float cellSize)
        {
            GUIStyle style = SmallCenteredLabel();

            for (int column = 0; column < width; column++)
            {
                GUI.Label(new Rect(cells.x + column * cellSize, cells.y - LabelSize, cellSize, LabelSize), (column + 1).ToString(), style);
            }

            for (int row = 0; row < height; row++)
            {
                GUI.Label(new Rect(cells.x - LabelSize, cells.y + row * cellSize, LabelSize, cellSize), (row + 1).ToString(), style);
            }
        }

        // The cell is stored with Y = 0 at the BOTTOM; the grid is drawn with the top row first.
        private static void DrawUnfillableFrame(Rect cells, float cellSize, GridPos cell, int height)
        {
            int row = ObstacleRows.RowFromTop(cell.Y, height);
            Rect rect = new Rect(cells.x + cell.X * cellSize, cells.y + row * cellSize, cellSize - 2f, cellSize - 2f);

            Color red = new Color(1f, 0.15f, 0.15f);
            const float thickness = 3f;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), red);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), red);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), red);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), red);
        }
    }
}
