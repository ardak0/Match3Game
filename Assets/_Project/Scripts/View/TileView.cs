using System.Collections.Generic;
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
        private FeelSettings _feel;
        private TileVisuals _visuals;
        private SpecialType _pendingSpecial; // what ApplyPendingSpecial turns this tile into (set by CreateConvertTween)
        private TweenCallback _onLanded; // created once (Awake), so a landing does not allocate a delegate
        private TweenCallback _applyPendingSpecial;

        // One material per sprite, shared by every tile that shows it.
        // Why: tiles are drawn through the board's SpriteMask, and Unity 6 (2D renderer) was batching masked sprites that share
        // one material into a single draw with ONE texture, so every tile came out as the first tile's picture (all red).
        // Different material = different batch, so each picture keeps its own texture. The cache is filled while the board is
        // built (a handful of entries), so nothing is created during play.
        private static Material _baseMaterial;
        private static readonly Dictionary<Sprite, Material> MaterialsBySprite = new Dictionary<Sprite, Material>();

        private static Material MaterialFor(Sprite sprite, Material current)
        {
            if (_baseMaterial == null) _baseMaterial = current;

            if (!MaterialsBySprite.TryGetValue(sprite, out Material material) || material == null)
            {
                material = new Material(_baseMaterial);
                MaterialsBySprite[sprite] = material;
            }

            return material;
        }

        public int TileId => tileId;

        /// <summary>Plays the landing squash. StepPlayer hangs it on the end of a fall tween (OnComplete).</summary>
        public TweenCallback Landed => _onLanded;

        private void Reset()
        {
            // Runs in the editor when the component is added: fills the Inspector slot for you.
            body = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            _onLanded = PlayLandingSquash;
            _applyPendingSpecial = ApplyPendingSpecial;
            EnsureIcon();
        }

        /// <summary>
        /// Makes this view show the given model tile, sized to fit one cell.
        /// A pooled tile is reused, so this sets EVERYTHING that can differ between uses (sprite, color, scale, icon).
        /// </summary>
        public void Setup(int newTileId, TileColor color, SpecialType special, TileVisuals visuals, FeelSettings feel, float cellSize)
        {
            tileId = newTileId;
            _feel = feel;
            _visuals = visuals;

            Sprite sprite = TileArt.GetSprite(visuals, color);
            body.sprite = sprite;
            body.color = TileArt.GetTint(visuals, color);
            body.sortingOrder = special == SpecialType.None ? 0 : SpecialBodySortingOrder;

            // Tiles are only visible inside the board's mask, so tiles waiting above the board stay hidden.
            body.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            body.sharedMaterial = MaterialFor(sprite, body.sharedMaterial);

            float spriteSize = TileArt.SizeOf(sprite);
            float scale = cellSize * visuals.TileFill * TileArt.GetScale(visuals, color) / spriteSize;
            transform.localScale = new Vector3(scale, scale, 1f);

            SetupIcon(special, visuals, spriteSize);
        }

        /// <summary>
        /// The localScale that makes the tile's body this big in the board's local space (world units at board scale 1).
        /// StepPlayer uses it to stretch a rocket into a beam or to blow a bomb up to its blast size.
        /// </summary>
        public Vector3 ScaleForSize(float width, float height)
        {
            float spriteSize = TileArt.SizeOf(body.sprite);
            return new Vector3(width / spriteSize, height / spriteSize, 1f);
        }

        /// <summary>
        /// The clear animation: the tile swells a little (the "pop", skipped for specials, which swell on their own),
        /// then shrinks to nothing. StepPlayer puts it in the wave's Sequence at the moment the tile should clear.
        /// </summary>
        public Tween CreateClearTween(float shrinkSeconds, bool pop)
        {
            DOTween.Kill(this, true); // finish a landing squash that may still be running, so the scale below is the normal one

            Sequence sequence = DOTween.Sequence();
            if (pop)
            {
                sequence.Append(transform.DOScale(transform.localScale * _feel.ClearPopScale, _feel.ClearPopSeconds).SetEase(Ease.OutQuad));
            }

            sequence.Append(transform.DOScale(0f, shrinkSeconds).SetEase(Ease.InBack));
            return sequence;
        }

        /// <summary>
        /// A ColorBomb turns this plain tile into a rocket or a bomb. Same piece, same id, new power.
        /// The tween changes the picture at its start (so the change happens when StepPlayer wants it, not when the wave
        /// is built) and then pops the tile.
        /// </summary>
        public Tween CreateConvertTween(SpecialType special, float punchStrength, float seconds)
        {
            DOTween.Kill(this, true); // finish a landing squash, so the punch below starts from the normal size

            _pendingSpecial = special;
            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(_applyPendingSpecial);
            sequence.Append(transform.DOPunchScale(transform.localScale * punchStrength, seconds, 1, 0f).SetEase(Ease.OutQuad));
            sequence.SetTarget(this);
            return sequence;
        }

        // Shows the special's icon on top of the body. A special draws over plain tiles, so the sorting order changes too.
        private void ApplyPendingSpecial()
        {
            body.sortingOrder = SpecialBodySortingOrder;
            SetupIcon(_pendingSpecial, _visuals, TileArt.SizeOf(body.sprite));
        }

        // Squashed flat for a moment (wider and shorter), then it springs back. DOPunchScale returns to the starting size by itself.
        private void PlayLandingSquash()
        {
            if (_feel.SquashStrength <= 0f) return;

            DOTween.Kill(this, true); // a squash still running from an earlier landing is finished first

            Vector3 scale = transform.localScale;
            Vector3 punch = new Vector3(scale.x * _feel.SquashStrength, -scale.y * _feel.SquashStrength, 0f);
            transform.DOPunchScale(punch, _feel.SquashSeconds, _feel.SquashVibrato, _feel.SquashElasticity)
                .SetTarget(this) // the target is this component, so ResetForPool and the calls above can find it
                .SetLink(gameObject);
        }

        /// <summary>
        /// Called when the tile goes back to the pool: stop every tween still aimed at it
        /// (otherwise an old tween could keep moving the tile after it was reused) and hide it.
        /// </summary>
        public void ResetForPool()
        {
            transform.DOKill();
            DOTween.Kill(this); // the squash tween has this component as its target
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

        private void SetupIcon(SpecialType special, TileVisuals visuals, float bodySpriteSize)
        {
            EnsureIcon();

            // A ColorBomb is a picture of its own (the whole body), so it has no icon on top.
            if (special == SpecialType.None || special == SpecialType.ColorBomb)
            {
                _icon.enabled = false;
                return;
            }

            Sprite iconSprite = GetIconSprite(special, visuals);
            _icon.sprite = iconSprite;
            _icon.sharedMaterial = MaterialFor(iconSprite, _icon.sharedMaterial);
            _icon.color = visuals.SpecialIconColor;

            // The icon is a child of the body, so it is already scaled with it. Correct for sprites that are not 1 unit wide.
            float iconScale = IconFill * bodySpriteSize / TileArt.SizeOf(iconSprite);
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
