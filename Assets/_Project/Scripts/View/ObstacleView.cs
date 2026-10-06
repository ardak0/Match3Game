using System;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// The picture of one obstacle in one cell: a crate, a layer of ice over a tile, or a chain on a tile.
    /// Like TileView it only looks right and never decides anything. BoardView takes it from a pool, StepPlayer plays
    /// its hit and destroy animations, and when a destroy animation ends the view gives itself back to the pool.
    ///
    /// Obstacles never move, so a view stays in its cell until it is destroyed.
    /// The picture follows the HP that is left (whole with 2 HP, cracked with 1 HP), see ObstacleArt.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ObstacleView : MonoBehaviour
    {
        // Draw order: a crate takes the place of a tile (tiles use 0..2); ice and chains are drawn over the tile, so above its icon.
        private const int CrateSortingOrder = 0;
        private const int OverlaySortingOrder = 3;

        // How much of the cell each kind fills.
        private const float CrateFill = 0.96f;
        private const float IceFill = 0.96f;
        private const float ChainFill = 0.9f;

        private SpriteRenderer _renderer;
        private Action<ObstacleView> _returnToPool;
        private TileVisuals _visuals;
        private FeelSettings _feel;
        private float _cellSize;
        private Vector3 _baseScale;
        private int _pendingHp;

        // Created once (Init), so starting an animation does not allocate delegates.
        private TweenCallback _showPendingHp;
        private TweenCallback _finish;

        public ObstacleType Type { get; private set; }
        public int Hp { get; private set; }
        public GridPos Cell { get; private set; }

        /// <summary>Called once, when the pool creates the object. returnToPool is called when a destroy animation is over.</summary>
        public void Init(Action<ObstacleView> returnToPool)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _returnToPool = returnToPool;
            _showPendingHp = ShowPendingHp;
            _finish = Finish;
        }

        /// <summary>Makes this view show an obstacle in a cell. A pooled view is reused, so everything that can differ is set here.</summary>
        public void Setup(ObstacleType type, int hp, GridPos cell, Vector3 localPosition, TileVisuals visuals, FeelSettings feel, float cellSize)
        {
            Type = type;
            Cell = cell;
            _visuals = visuals;
            _feel = feel;
            _cellSize = cellSize;

            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;
            _renderer.sortingOrder = type == ObstacleType.Crate ? CrateSortingOrder : OverlaySortingOrder;
            _renderer.color = ObstacleArt.GetTint(visuals, type);
            _renderer.enabled = true;

            ShowHp(hp);
        }

        /// <summary>
        /// A hit that the obstacle survives: a crate shakes, ice gets its crack and a small bounce.
        /// The picture changes at the start of the animation, not when the wave is built, so it cracks when the tiles clear.
        /// </summary>
        public Tween CreateHitTween(int hpLeft)
        {
            _pendingHp = hpLeft;

            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(_showPendingHp);

            if (Type == ObstacleType.Ice)
            {
                sequence.Append(transform.DOPunchScale(_baseScale * 0.15f, _feel.IceCrackSeconds, 1, 0.5f));
            }
            else
            {
                sequence.Append(CreateShakeTween());
            }

            sequence.SetTarget(this);
            return sequence;
        }

        /// <summary>A short sideways shake. Also used when the player tries to swipe a crate or a chained tile.</summary>
        public Tween CreateShakeTween()
        {
            float strength = _feel.CrateShakeStrengthInCells * _cellSize;
            return transform.DOPunchPosition(new Vector3(strength, 0f, 0f), _feel.CrateShakeSeconds, 10, 0.5f).SetTarget(this);
        }

        /// <summary>
        /// The obstacle is gone. A crate swells a little and shrinks away, ice swells and fades, a chain snaps (swells fast) and fades.
        /// When it is over the view returns itself to the pool.
        /// </summary>
        public Tween CreateDestroyTween()
        {
            Sequence sequence = DOTween.Sequence();

            switch (Type)
            {
                case ObstacleType.Crate:
                    sequence.Append(transform.DOScale(_baseScale * _feel.ClearPopScale, _feel.ClearPopSeconds).SetEase(Ease.OutQuad));
                    sequence.Append(transform.DOScale(0f, _feel.CrateBreakSeconds).SetEase(Ease.InBack));
                    break;

                case ObstacleType.Ice:
                    sequence.Append(transform.DOScale(_baseScale * 1.15f, _feel.IceShatterSeconds).SetEase(Ease.OutQuad));
                    sequence.Join(_renderer.DOFade(0f, _feel.IceShatterSeconds).SetEase(Ease.InQuad));
                    break;

                default: // chain
                    float snapSeconds = _feel.ChainSnapSeconds * 0.35f;
                    float fadeSeconds = _feel.ChainSnapSeconds - snapSeconds;
                    sequence.Append(transform.DOScale(_baseScale * 1.3f, snapSeconds).SetEase(Ease.OutBack));
                    sequence.Append(transform.DOScale(_baseScale * 1.5f, fadeSeconds).SetEase(Ease.OutQuad));
                    sequence.Join(_renderer.DOFade(0f, fadeSeconds).SetEase(Ease.InQuad));
                    sequence.Join(transform.DOLocalRotate(new Vector3(0f, 0f, 15f), fadeSeconds));
                    break;
            }

            sequence.OnComplete(_finish);
            sequence.SetTarget(this); // ResetForPool kills by this target
            return sequence;
        }

        /// <summary>Called when the view goes back to the pool: stop every tween aimed at it and hide it.</summary>
        public void ResetForPool()
        {
            DOTween.Kill(this);
            transform.DOKill();
            if (_renderer != null) _renderer.color = Color.white;
            transform.localRotation = Quaternion.identity;
            gameObject.SetActive(false);
        }

        private void ShowPendingHp() => ShowHp(_pendingHp);

        // Picks the picture for this HP and sizes it to fit the cell.
        private void ShowHp(int hp)
        {
            Hp = hp;

            Sprite sprite = ObstacleArt.GetSprite(_visuals, Type, hp);
            _renderer.sprite = sprite;

            float fill = Type == ObstacleType.Crate ? CrateFill : Type == ObstacleType.Ice ? IceFill : ChainFill;
            float scale = _cellSize * fill / TileArt.SizeOf(sprite);
            _baseScale = new Vector3(scale, scale, 1f);
            transform.localScale = _baseScale;
        }

        private void Finish() => _returnToPool(this);
    }
}
