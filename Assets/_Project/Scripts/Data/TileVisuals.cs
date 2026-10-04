using Match3.Core;
using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// How tiles look: one color per TileColor, (optionally) one sprite shared by all tiles,
    /// and (optionally) an icon sprite for each kind of special tile.
    /// Swap the sprite or the colors here to restyle the game without touching code.
    /// If no sprite is assigned, a generated placeholder is used, so art never blocks progress.
    /// Create one with: right-click in the Project window, Create > Match3 > Tile Visuals.
    /// </summary>
    [CreateAssetMenu(fileName = "TileVisuals", menuName = "Match3/Tile Visuals")]
    public sealed class TileVisuals : ScriptableObject
    {
        [Tooltip("Optional. Leave empty to use the generated rounded square.")]
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

        [Header("Colors")]
        // Order matches the TileColor enum: Red, Green, Blue, Yellow, Purple, Orange.
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
        public float TileFill => tileFill;
        public Sprite RocketHorizontalSprite => rocketHorizontalSprite;
        public Sprite RocketVerticalSprite => rocketVerticalSprite;
        public Sprite BombSprite => bombSprite;
        public Color SpecialIconColor => specialIconColor;

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
