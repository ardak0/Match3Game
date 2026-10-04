using DG.Tweening;
using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// The picture of one model tile. It only knows how to look right; it never decides anything.
    /// BoardView takes it from the tile pool, StepPlayer moves it, BoardView gives it back to the pool.
    /// TileId links it to the Tile in the model (it shows in the Inspector while playing, for debugging).
    ///
    /// A special tile also shows an icon (an arrow or a ring) on top of its colored body. The icon is a child
    /// object created once in Awake, while the pool is being prewarmed, so nothing is created during play.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        // How much of the tile the icon covers, and the draw order (body 0, special body 1, icon 2: specials draw over plain tiles).
        private const float IconFill = 0.7f;
        private const int SpecialBodySortingOrder = 1;
        private const int IconSortingOrder = 2;

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private int tileId;

        private SpriteRenderer _icon;

        public int TileId => tileId;

        private void Reset()
        {
            // Runs in the editor when the component is added: fills the Inspector slot for you.
            body = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            EnsureIcon();
        }

        /// <summary>
        /// Makes this view show the given model tile, sized to fit one cell.
        /// A pooled tile is reused, so this sets EVERYTHING that can differ between uses (sprite, color, scale, icon).
        /// </summary>
        public void Setup(int newTileId, TileColor color, SpecialType special, TileVisuals visuals, float cellSize)
        {
            tileId = newTileId;

            Sprite sprite = visuals.TileSprite != null ? visuals.TileSprite : PlaceholderSprite.RoundedSquare;
            body.sprite = sprite;
            body.color = visuals.GetColor(color);
            body.sortingOrder = special == SpecialType.None ? 0 : SpecialBodySortingOrder;

            // Tiles are only visible inside the board's mask, so tiles waiting above the board stay hidden.
            body.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            float spriteWidth = sprite.bounds.size.x;
            float scale = cellSize * visuals.TileFill / spriteWidth;
            transform.localScale = new Vector3(scale, scale, 1f);

            SetupIcon(special, visuals, spriteWidth);
        }

        /// <summary>
        /// The localScale that makes the tile's body this big in the board's local space (world units at board scale 1).
        /// StepPlayer uses it to stretch a rocket into a beam or to blow a bomb up to its blast size.
        /// </summary>
        public Vector3 ScaleForSize(float width, float height)
        {
            float spriteWidth = body.sprite.bounds.size.x;
            return new Vector3(width / spriteWidth, height / spriteWidth, 1f);
        }

        /// <summary>
        /// Called when the tile goes back to the pool: stop every tween still aimed at it
        /// (otherwise an old tween could keep moving the tile after it was reused) and hide it.
        /// </summary>
        public void ResetForPool()
        {
            transform.DOKill();
            gameObject.SetActive(false);
        }

        private void EnsureIcon()
        {
            if (_icon != null) return;

            GameObject iconObject = new GameObject("SpecialIcon");
            iconObject.transform.SetParent(transform, false);
            _icon = iconObject.AddComponent<SpriteRenderer>();
            _icon.sortingOrder = IconSortingOrder;
            _icon.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            _icon.enabled = false;
        }

        private void SetupIcon(SpecialType special, TileVisuals visuals, float bodySpriteWidth)
        {
            EnsureIcon();

            if (special == SpecialType.None)
            {
                _icon.enabled = false;
                return;
            }

            Sprite iconSprite = GetIconSprite(special, visuals);
            _icon.sprite = iconSprite;
            _icon.color = visuals.SpecialIconColor;

            // The icon is a child of the body, so it is already scaled with it. Correct for sprites that are not 1 unit wide.
            float iconScale = IconFill * bodySpriteWidth / iconSprite.bounds.size.x;
            _icon.transform.localScale = new Vector3(iconScale, iconScale, 1f);
            _icon.enabled = true;
        }

        private static Sprite GetIconSprite(SpecialType special, TileVisuals visuals)
        {
            switch (special)
            {
                case SpecialType.RocketHorizontal:
                    return visuals.RocketHorizontalSprite != null ? visuals.RocketHorizontalSprite : PlaceholderSprite.ArrowHorizontal;
                case SpecialType.RocketVertical:
                    return visuals.RocketVerticalSprite != null ? visuals.RocketVerticalSprite : PlaceholderSprite.ArrowVertical;
                default:
                    return visuals.BombSprite != null ? visuals.BombSprite : PlaceholderSprite.Bomb;
            }
        }
    }
}
