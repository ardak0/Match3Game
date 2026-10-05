using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// Answers "what does a tile of this color look like?" in one place, so the board tiles and the HUD goal icons
    /// always agree. It applies the fallbacks: the color's own sprite, else the shared sprite (tinted with the
    /// color), else a generated rounded square.
    /// </summary>
    public static class TileArt
    {
        public static Sprite GetSprite(TileVisuals visuals, TileColor color)
        {
            if (visuals.TryGetTileStyle(color, out TileVisuals.TileStyle style)) return style.sprite;
            return visuals.TileSprite != null ? visuals.TileSprite : PlaceholderSprite.RoundedSquare;
        }

        /// <summary>The color to draw the sprite with: the style's tint, or the identity color for the fallback sprite.</summary>
        public static Color GetTint(TileVisuals visuals, TileColor color)
        {
            return visuals.TryGetTileStyle(color, out TileVisuals.TileStyle style) ? style.tint : visuals.GetColor(color);
        }

        /// <summary>Size multiplier for this color's tile (1 for the fallback sprite).</summary>
        public static float GetScale(TileVisuals visuals, TileColor color)
        {
            return visuals.TryGetTileStyle(color, out TileVisuals.TileStyle style) ? style.scale : 1f;
        }

        /// <summary>
        /// The sprite's size in world units, measured by its longer side. Sprites of different shapes (a 96x96 diamond,
        /// a 96x92 pentagon, a 64x64 square) are all fitted into a cell using this, so none is stretched.
        /// </summary>
        public static float SizeOf(Sprite sprite)
        {
            Vector3 size = sprite.bounds.size;
            return Mathf.Max(size.x, size.y);
        }
    }
}
