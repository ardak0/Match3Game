using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// Answers "what does this obstacle look like?" in one place, so the board and the HUD goal icons always agree
    /// (the same job TileArt does for tiles). A picture assigned in TileVisuals wins; an empty slot falls back to the
    /// picture generated in code (PlaceholderSprite).
    ///
    /// The picture depends only on the type and the HP that is left: whole with 2 HP, cracked with 1 HP.
    /// A chain has no cracked picture, because a chain always breaks in one hit.
    /// </summary>
    public static class ObstacleArt
    {
        public static Sprite GetSprite(TileVisuals visuals, ObstacleType type, int hp)
        {
            bool cracked = hp <= 1;

            switch (type)
            {
                case ObstacleType.Crate:
                    if (cracked) return visuals.CrateCrackedSprite != null ? visuals.CrateCrackedSprite : PlaceholderSprite.CrateCracked;
                    return visuals.CrateSprite != null ? visuals.CrateSprite : PlaceholderSprite.Crate;

                case ObstacleType.Ice:
                    if (cracked) return visuals.IceCrackedSprite != null ? visuals.IceCrackedSprite : PlaceholderSprite.IceCracked;
                    return visuals.IceSprite != null ? visuals.IceSprite : PlaceholderSprite.Ice;

                default:
                    return visuals.ChainSprite != null ? visuals.ChainSprite : PlaceholderSprite.Chain;
            }
        }

        /// <summary>The color to draw the picture with (white, except the optional tint of the ice).</summary>
        public static Color GetTint(TileVisuals visuals, ObstacleType type)
        {
            return type == ObstacleType.Ice ? visuals.IceTint : Color.white;
        }

        /// <summary>The picture for a goal icon in the HUD: the whole (2 HP) picture of the obstacle.</summary>
        public static Sprite GetIcon(TileVisuals visuals, ObstacleType type) => GetSprite(visuals, type, 2);

        /// <summary>The color of the pieces that fly out when this obstacle is destroyed.</summary>
        public static Color GetShardColor(TileVisuals visuals, ObstacleType type)
        {
            switch (type)
            {
                case ObstacleType.Crate: return visuals.CrateShardColor;
                case ObstacleType.Ice: return visuals.IceShardColor;
                default: return visuals.ChainShardColor;
            }
        }
    }
}
