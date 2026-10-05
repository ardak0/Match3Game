using System;
using DG.Tweening;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// One pooled picture used by the effects: a particle that flies out of a cleared tile, a rocket's streak,
    /// a ColorBomb's beam or the ring of a new ColorBomb.
    /// BoardEffects takes it from its pool, calls PlayParticle, PlayStreak, PlayBeam or PlayRing, and the effect gives itself back to
    /// the pool when it is over. Everything is tweened with DOTween, and every tween is killed when the object returns
    /// to the pool, so an old tween can never move a reused object.
    ///
    /// The effect is started in advance with a delay (the view builds a whole wave of animation at once), so the
    /// picture stays hidden until its delay is over.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FxSprite : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Action<FxSprite> _returnToPool;

        // Created once, so starting an effect does not allocate delegates.
        private TweenCallback _show;
        private TweenCallback _finish;

        public void Init(Action<FxSprite> returnToPool, int sortingOrder)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sortingOrder = sortingOrder;
            _returnToPool = returnToPool;
            _show = Show;
            _finish = Finish;
        }

        /// <summary>A particle: appears after delaySeconds at "from", flies to "to" while it shrinks and fades.</summary>
        public void PlayParticle(Sprite sprite, Color color, Vector3 from, Vector3 to, float scale, float seconds, float delaySeconds)
        {
            Prepare(sprite, color, from, Vector3.one * scale, Quaternion.identity);

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delaySeconds);
            sequence.Append(transform.DOLocalMove(to, seconds).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(0f, seconds).SetEase(Ease.InQuad));
            sequence.Join(_renderer.DOFade(0f, seconds));
            Launch(sequence);
        }

        /// <summary>
        /// A streak: a long thin glow across the board that thins out and fades. "scale" is its size in world units
        /// (x = across, y = along); the picture points up and down, so a horizontal streak is turned a quarter circle.
        /// </summary>
        public void PlayStreak(Sprite sprite, Color color, Vector3 center, bool horizontal, Vector3 scale, float seconds, float delaySeconds)
        {
            Quaternion rotation = horizontal ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
            Prepare(sprite, color, center, scale, rotation);

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delaySeconds);
            sequence.Append(transform.DOScale(new Vector3(scale.x * 0.2f, scale.y, 1f), seconds).SetEase(Ease.OutQuad));
            sequence.Join(_renderer.DOFade(0f, seconds).SetEase(Ease.InQuad));
            Launch(sequence);
        }

        /// <summary>
        /// A beam from one point to another (a ColorBomb reaching for a tile). It shoots out of "from" until it touches "to",
        /// then fades. The picture points up and down, so it is turned to point along the beam.
        /// thickness is in world units; the beam is as long as the distance between the two points.
        /// </summary>
        public void PlayBeam(Sprite sprite, Color color, Vector3 from, Vector3 to, float thickness, float seconds, float delaySeconds)
        {
            Vector3 direction = to - from;
            float length = direction.magnitude;
            float size = TileArt.SizeOf(sprite);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // 90 degrees: the picture points up, not right

            // It starts as a dot at "from" and grows towards "to": the middle of the beam moves from "from" to the halfway point.
            Prepare(sprite, color, from, new Vector3(thickness / size, 0f, 1f), Quaternion.Euler(0f, 0f, angle));

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delaySeconds);
            sequence.Append(transform.DOLocalMove(from + direction * 0.5f, seconds).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScaleY(length / size, seconds).SetEase(Ease.OutQuad));
            sequence.Append(_renderer.DOFade(0f, seconds * 0.6f).SetEase(Ease.InQuad));
            Launch(sequence);
        }

        /// <summary>A ring that spreads out from a point while it fades: it grows from fromSize to toSize (both in world units).</summary>
        public void PlayRing(Sprite sprite, Color color, Vector3 center, float fromSize, float toSize, float seconds, float delaySeconds)
        {
            float size = TileArt.SizeOf(sprite);
            Prepare(sprite, color, center, Vector3.one * (fromSize / size), Quaternion.identity);

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delaySeconds);
            sequence.Append(transform.DOScale(toSize / size, seconds).SetEase(Ease.OutQuad));
            sequence.Join(_renderer.DOFade(0f, seconds).SetEase(Ease.InQuad));
            Launch(sequence);
        }

        /// <summary>Called when the object goes back to the pool: stop every tween aimed at it and hide it.</summary>
        public void ResetForPool()
        {
            DOTween.Kill(this);
            if (_renderer != null) _renderer.enabled = false;
            transform.localRotation = Quaternion.identity;
            gameObject.SetActive(false);
        }

        private void Prepare(Sprite sprite, Color color, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            _renderer.sprite = sprite;
            _renderer.color = color;
            _renderer.enabled = false; // shown by the sequence when its delay is over
            transform.localPosition = position;
            transform.localRotation = rotation;
            transform.localScale = scale;
        }

        private void Launch(Sequence sequence)
        {
            sequence.OnStart(_show);
            sequence.OnComplete(_finish);
            sequence.SetTarget(this); // ResetForPool kills by this target
            sequence.SetLink(gameObject);
        }

        private void Show() => _renderer.enabled = true;

        private void Finish() => _returnToPool(this);
    }
}
