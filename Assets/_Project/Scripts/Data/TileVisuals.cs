using System;
using Match3.Core;
using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// How the board and its tiles look: one sprite (with tint and size) per TileColor, an icon sprite for each
    /// kind of special tile, and the board frame and cell slot sprites.
    /// Swap sprites or colors here to restyle the game without touching code.
    /// Anything left empty falls back to a generated placeholder, so art never blocks progress.
    /// Create one with: right-click in the Project window, Create > Match3 > Tile Visuals.
    /// </summary>
    [CreateAssetMenu(fileName = "TileVisuals", menuName = "Match3/Tile Visuals")]
    public sealed class TileVisuals : ScriptableObject
    {
        /// <summary>The look of the tiles of one color. Public fields so Unity can serialize it (same idea as GoalDefinition).</summary>
        [Serializable]
        public struct TileStyle
        {
            [Tooltip("The picture of this color's tile. Leave empty to use the shared sprite below, tinted with the Tile Colors entry.")]
            public Sprite sprite;

            [Tooltip("Multiplied with the sprite's own colors. White = unchanged. Use it to recolor a grey sprite.")]
            public Color tint;

            [Tooltip("Size relative to the other tiles. Sprites that fill their whole square look bigger than gems with empty corners, so shrink those a little.")]
            [Range(0.5f, 1.3f)] public float scale;
        }

        [Header("Tiles")]
        // Order matches the TileColor enum: Red, Green, Blue, Yellow, Purple, Orange.
        [SerializeField] private TileStyle[] tileStyles;

        [Tooltip("Fallback for a color that has no sprite of its own. Leave empty to use the generated rounded square.")]
        [SerializeField] private Sprite tileSprite;

        [Tooltip("How much of its cell a tile fills. 0.9 leaves a small gap between tiles.")]
        [SerializeField, Range(0.5f, 1f)] private float tileFill = 0.9f;

        [Header("Special tile icons (drawn on top of the colored tile)")]
        [Tooltip("Optional. Leave empty to use a generated left-right arrow.")]
        [SerializeField] private Sprite rocketHorizontalSprite;

        [Tooltip("Optional. Leave empty to use a generated up-down arrow.")]
        [SerializeField] private Sprite rocketVerticalSprite;

        [Tooltip("Optional. Leave empty to use a generated ring.")]
        [SerializeField] private Sprite bombSprite;

        [SerializeField] private Color specialIconColor = new Color(0.1f, 0.1f, 0.14f, 0.9f);

        [Header("Board look")]
        [Tooltip("Optional. The frame around the board (a 9-slice sprite). Leave empty for a plain dark rectangle.")]
        [SerializeField] private Sprite boardFrameSprite;

        [SerializeField] private Color frameColor = new Color(0.17f, 0.22f, 0.40f, 1f);

        [Tooltip("How far the frame reaches beyond the outer cells, in cells.")]
        [SerializeField, Range(0f, 1f)] private float framePaddingInCells = 0.3f;

        [Tooltip("Optional. The background drawn behind every cell (a 9-slice sprite). Leave empty for no slots.")]
        [SerializeField] private Sprite cellSlotSprite;

        [SerializeField] private Color slotColor = new Color(0.10f, 0.14f, 0.28f, 0.65f);

        [Tooltip("How much of its cell a slot fills. Below 1 leaves a thin gap between slots.")]
        [SerializeField, Range(0.5f, 1f)] private float slotFill = 0.96f;

        [Header("Effects")]
        [Tooltip("Flies out of a tile when it clears (tinted with the tile's color). Leave empty for a small generated square.")]
        [SerializeField] private Sprite burstSprite;

        [Tooltip("The glowing streak of a rocket. It should point up and down; the game turns it for a horizontal rocket. Leave empty for a plain bar.")]
        [SerializeField] private Sprite streakSprite;

        [Header("Colors")]
        [Tooltip("The identity color of each tile color: used to tint the shared fallback sprite, and by effects (particles) that should match the tile.")]
        [SerializeField] private Color[] tileColors =
        {
            new Color(0.92f, 0.26f, 0.26f),
            new Color(0.30f, 0.80f, 0.35f),
            new Color(0.30f, 0.52f, 0.95f),
            new Color(0.97f, 0.85f, 0.25f),
            new Color(0.66f, 0.40f, 0.90f),
            new Color(0.97f, 0.60f, 0.20f)
        };

        public Sprite TileSprite => tileSprite;
        public Sprite BoardFrameSprite => boardFrameSprite;
        public Color FrameColor => frameColor;
        public float FramePaddingInCells => framePaddingInCells;
        public Sprite CellSlotSprite => cellSlotSprite;
        public Color SlotColor => slotColor;
        public float SlotFill => slotFill;
        public float TileFill => tileFill;
        public Sprite RocketHorizontalSprite => rocketHorizontalSprite;
        public Sprite RocketVerticalSprite => rocketVerticalSprite;
        public Sprite BombSprite => bombSprite;
        public Color SpecialIconColor => specialIconColor;
        public Sprite BurstSprite => burstSprite;
        public Sprite StreakSprite => streakSprite;

        private void OnValidate()
        {
            // Editor-only reminder: a style list that is too short silently leaves the last colors on the fallback sprite.
            int colorCount = Enum.GetValues(typeof(TileColor)).Length;
            if (tileStyles != null && tileStyles.Length > 0 && tileStyles.Length < colorCount)
            {
                Debug.LogWarning("TileVisuals: Tile Styles has " + tileStyles.Length + " entries but there are " + colorCount + " tile colors.", this);
            }
        }

        /// <summary>
        /// The style of a color's own tile, if one is set up (a sprite is assigned). False means "use the shared fallback".
        /// </summary>
        public bool TryGetTileStyle(TileColor color, out TileStyle style)
        {
            int index = (int)color;
            if (tileStyles != null && index < tileStyles.Length && tileStyles[index].sprite != null)
            {
                style = tileStyles[index];
                return true;
            }

            style = default;
            return false;
        }

        /// <summary>The identity color of a tile color (for effects and as the tint of the fallback sprite).</summary>
        public Color GetColor(TileColor color)
        {
            int index = (int)color;
            if (index >= tileColors.Length)
            {
                Debug.LogError("TileVisuals has no color for " + color + ". Add it to the Tile Colors list.", this);
                return Color.magenta; // magenta = "something is missing", impossible to overlook
            }

            return tileColors[index];
        }
    }
}
