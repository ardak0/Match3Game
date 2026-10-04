using System.Collections.Generic;
using Match3.Data;
using Match3.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// The heads-up display: which level, how many moves are left, and one icon with a counter per goal.
    /// It only shows what it is told. LevelController feeds it from the MoveCounter and GoalTracker events,
    /// so the HUD never reads (or changes) game state itself.
    /// The goal icons use the same colors as the tiles (TileVisuals), so "12 next to a red square" means 12 red tiles.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.14f, 0.85f);
        private static readonly Color NormalTextColor = Color.white;
        private static readonly Color LowMovesColor = new Color(1f, 0.45f, 0.4f);
        private static readonly Color DoneColor = new Color(1f, 1f, 1f, 0.45f);

        private const int LowMovesThreshold = 3;

        private TileVisuals _visuals;
        private Text _levelText;
        private Text _movesText;
        private RectTransform _goalRow;
        private readonly List<Text> _goalTexts = new List<Text>();
        private readonly List<Image> _goalIcons = new List<Image>();

        /// <summary>Builds the HUD (a canvas with all its parts) as a new object. The visuals give the goal icons their colors.</summary>
        public static HudView Create(TileVisuals visuals)
        {
            HudView hud = new GameObject("HUD").AddComponent<HudView>();
            hud.Build(visuals);
            return hud;
        }

        private void Build(TileVisuals visuals)
        {
            _visuals = visuals;
            Canvas canvas = UiFactory.CreateCanvas(transform, "HUD Canvas", 10);

            // A dark strip along the top of the screen.
            Image panel = UiFactory.CreateImage(canvas.transform, "Panel", PanelColor);
            panel.rectTransform.anchorMin = new Vector2(0f, 1f);
            panel.rectTransform.anchorMax = new Vector2(1f, 1f);
            panel.rectTransform.pivot = new Vector2(0.5f, 1f);
            panel.rectTransform.sizeDelta = new Vector2(0f, 380f);
            panel.rectTransform.anchoredPosition = Vector2.zero;

            _levelText = UiFactory.CreateText(panel.transform, "Level", "Level 1", 56, NormalTextColor, TextAnchor.MiddleLeft);
            UiFactory.Place(_levelText.rectTransform, new Vector2(0f, 1f), new Vector2(190f, -60f), new Vector2(300f, 80f)); // top row: level on the left, moves on the right

            Text movesLabel = UiFactory.CreateText(panel.transform, "MovesLabel", "MOVES", 36, DoneColor, TextAnchor.MiddleRight);
            UiFactory.Place(movesLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-190f, -40f), new Vector2(300f, 50f));

            _movesText = UiFactory.CreateText(panel.transform, "Moves", "0", 100, NormalTextColor, TextAnchor.MiddleRight);
            UiFactory.Place(_movesText.rectTransform, new Vector2(1f, 1f), new Vector2(-190f, -125f), new Vector2(300f, 120f));

            // The goals sit in a centered row at the bottom of the strip, with a clear gap under the moves counter (the top row ends about 190 px from the top, this row starts about 230 px from the top). Their positions are worked out in Show().
            _goalRow = UiFactory.CreateRect(panel.transform, "Goals");
            UiFactory.Place(_goalRow, new Vector2(0.5f, 0f), new Vector2(0f, 95f), new Vector2(1000f, 110f));
        }

        /// <summary>Starts showing a level: its number, the move limit and one counter per goal.</summary>
        public void Show(int levelNumber, int moveLimit, IReadOnlyList<GoalDefinition> goals)
        {
            _levelText.text = "Level " + levelNumber;
            SetMoves(moveLimit);

            // Remove the previous level's goal items, then make one per goal.
            for (int i = _goalRow.childCount - 1; i >= 0; i--)
            {
                Destroy(_goalRow.GetChild(i).gameObject);
            }

            _goalTexts.Clear();
            _goalIcons.Clear();

            const float itemWidth = 240f; // icon (80) + gap + number, with room for 4 goals across 1080
            float firstX = -(goals.Count - 1) * itemWidth / 2f;
            Sprite iconSprite = _visuals.TileSprite != null ? _visuals.TileSprite : PlaceholderSprite.RoundedSquare;

            for (int i = 0; i < goals.Count; i++)
            {
                RectTransform item = UiFactory.CreateRect(_goalRow, "Goal " + (i + 1));
                UiFactory.Place(item, new Vector2(0.5f, 0.5f), new Vector2(firstX + i * itemWidth, 0f), new Vector2(itemWidth, 100f));

                Image icon = UiFactory.CreateImage(item, "Icon", _visuals.GetColor(goals[i].color), iconSprite);
                UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(45f, 0f), new Vector2(80f, 80f));

                Text count = UiFactory.CreateText(item, "Count", goals[i].count.ToString(), 60, NormalTextColor, TextAnchor.MiddleLeft);
                UiFactory.Place(count.rectTransform, new Vector2(0f, 0.5f), new Vector2(180f, 0f), new Vector2(140f, 90f)); // the text is left-aligned, so it starts at x = 110: right of the icon (which ends at x = 85)

                _goalIcons.Add(icon);
                _goalTexts.Add(count);
            }
        }

        public void SetMoves(int movesLeft)
        {
            _movesText.text = movesLeft.ToString();
            _movesText.color = movesLeft <= LowMovesThreshold ? LowMovesColor : NormalTextColor;
        }

        /// <summary>Shows how many tiles a goal still needs. A finished goal reads "Done" and fades.</summary>
        public void SetGoalRemaining(int goalIndex, int remaining)
        {
            bool done = remaining <= 0;
            _goalTexts[goalIndex].text = done ? "Done" : remaining.ToString();
            _goalTexts[goalIndex].fontSize = done ? 44 : 60;
            _goalTexts[goalIndex].color = done ? DoneColor : NormalTextColor;

            Color iconColor = _goalIcons[goalIndex].color;
            iconColor.a = done ? 0.45f : 1f;
            _goalIcons[goalIndex].color = iconColor;
        }
    }
}
