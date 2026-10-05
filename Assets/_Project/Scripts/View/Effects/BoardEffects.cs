using DG.Tweening;
using Match3.Core;
using Match3.Data;
using Match3.Infrastructure;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// The effects that happen on the board: the particle burst of a cleared tile, the streak of a rocket and the
    /// camera shake of a bomb. StepPlayer asks for them while it builds a wave of animation.
    ///
    /// The particles and streaks are FxSprite objects from a pool that is filled once, when a level starts
    /// (BoardView calls Prepare). During play nothing is created: if the pool is ever empty, an effect is simply
    /// skipped, so a busy cascade can never cause a hitch.
    /// BoardEffects is a child of the board, so all positions are in the board's local space, like the tiles.
    /// </summary>
    public sealed class BoardEffects : MonoBehaviour
    {
        // Draw order: tiles use 0..2, the effects draw on top of them.
        private const int FxSortingOrder = 20;

        // A few more than the bursts need, for rocket streaks.
        private const int StreakReserve = 16;

        private ObjectPool<FxSprite> _pool;
        private System.Action<FxSprite> _release;
        private Camera _camera;
        private TileVisuals _visuals;
        private FeelSettings _feel;
        private float _cellSize;
        private Tween _shake;

        public static BoardEffects Create(Transform board)
        {
            GameObject effectsObject = new GameObject("Effects");
            effectsObject.transform.SetParent(board, false);
            return effectsObject.AddComponent<BoardEffects>();
        }

        /// <summary>Called when a level starts: remembers the settings and makes sure the pool is big enough. Never called during play.</summary>
        public void Prepare(Camera camera, TileVisuals visuals, FeelSettings feel, float cellSize, int cellCount)
        {
            _camera = camera;
            _visuals = visuals;
            _feel = feel;
            _cellSize = cellSize;

            if (_pool == null)
            {
                _release = ReleaseFx;
                _pool = new ObjectPool<FxSprite>(CreateFx, OnFxTaken, OnFxReturned);
            }

            // Worst case, every tile of the board clears in the same wave and each one throws its particles.
            _pool.Prewarm(cellCount * feel.BurstParticleCount + StreakReserve);
        }

        /// <summary>The particles of one cleared tile fly out from its center, starting after delaySeconds.</summary>
        public void PlayBurst(Vector3 center, TileColor color, float delaySeconds)
        {
            int count = _feel.BurstParticleCount;
            if (count <= 0) return;

            Sprite sprite = _visuals.BurstSprite != null ? _visuals.BurstSprite : PlaceholderSprite.Solid;
            Color tint = _visuals.GetColor(color);
            float scale = _feel.BurstParticleSizeInCells * _cellSize / TileArt.SizeOf(sprite);
            float angleStep = 2f * Mathf.PI / count;

            for (int i = 0; i < count; i++)
            {
                if (!TryTake(out FxSprite fx)) return;

                // Evenly spaced directions, each nudged a little so the burst does not look like a perfect wheel.
                float angle = (i + Random.Range(-0.3f, 0.3f)) * angleStep;
                float distance = _feel.BurstDistanceInCells * _cellSize * Random.Range(0.6f, 1.2f);
                Vector3 target = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;

                fx.PlayParticle(sprite, tint, center, target, scale, _feel.BurstSeconds, delaySeconds);
            }
        }

        /// <summary>A glowing streak through center along the whole board: across for a horizontal rocket, up and down for a vertical one.</summary>
        public void PlayStreak(Vector3 center, bool horizontal, float length, float delaySeconds)
        {
            if (!TryTake(out FxSprite fx)) return;

            Sprite sprite = _visuals.StreakSprite != null ? _visuals.StreakSprite : PlaceholderSprite.Solid;
            float size = TileArt.SizeOf(sprite);
            float thickness = _feel.StreakThicknessInCells * _cellSize;
            Vector3 scale = new Vector3(thickness / size, length / size, 1f);

            fx.PlayStreak(sprite, _feel.StreakColor, center, horizontal, scale, _feel.StreakSeconds, delaySeconds);
        }

        /// <summary>Throws the camera around for a moment (a bomb going off). A new shake finishes the old one first, so the camera never drifts.</summary>
        public void ShakeCamera()
        {
            if (_feel.ShakeStrengthInCells <= 0f) return;

            _shake?.Kill(true); // completing a shake puts the camera back where it started
            float strength = _feel.ShakeStrengthInCells * _cellSize;
            _shake = _camera.transform
                .DOShakePosition(_feel.ShakeSeconds, new Vector3(strength, strength, 0f), _feel.ShakeVibrato, 90f, false, true)
                .SetLink(_camera.gameObject);
        }

        // Takes an effect from the pool, but only if one is waiting: the pool is never allowed to create one during play.
        private bool TryTake(out FxSprite fx)
        {
            if (_pool.InactiveCount == 0)
            {
                fx = null;
                return false;
            }

            fx = _pool.Get();
            return true;
        }

        // ---- the three methods the pool uses ----

        private FxSprite CreateFx()
        {
            GameObject fxObject = new GameObject("Fx", typeof(SpriteRenderer));
            fxObject.transform.SetParent(transform, false);
            FxSprite fx = fxObject.AddComponent<FxSprite>();
            fx.Init(_release, FxSortingOrder);
            return fx;
        }

        private static void OnFxTaken(FxSprite fx) => fx.gameObject.SetActive(true);

        private static void OnFxReturned(FxSprite fx) => fx.ResetForPool();

        private void ReleaseFx(FxSprite fx) => _pool.Release(fx);
    }
}
