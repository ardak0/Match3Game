using DG.Tweening;
using Match3.Core;
using Match3.Data;
using Match3.Infrastructure;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// The effects that happen on the board: the particle burst of a cleared tile, the streak of a rocket, the
    /// camera shake of a bomb, the beams of a ColorBomb and the ring of a new one. StepPlayer asks for them while it builds a wave of animation.
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

        // A few more than the bursts and beams need, for rocket streaks and rings.
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

            // Worst case, every tile of the board clears in the same wave: each one throws its particles, and a
            // ColorBomb that clears the whole board also shoots one beam at each tile.
            // Obstacles that break in the same wave throw their own pieces, at most one obstacle per cell.
            _pool.Prewarm(cellCount * (feel.BurstParticleCount + 1 + feel.ObstacleShardCount) + StreakReserve);
        }

        /// <summary>The particles of one cleared tile fly out from its center, starting after delaySeconds.</summary>
        public void PlayBurst(Vector3 center, TileColor color, float delaySeconds)
        {
            PlayParticles(center, _visuals.GetColor(color), _feel.BurstParticleCount, _feel.BurstDistanceInCells,
                _feel.BurstParticleSizeInCells, _feel.BurstSeconds, delaySeconds);
        }

        /// <summary>The pieces of a destroyed crate, ice or chain fly out from the center of its cell, starting after delaySeconds.</summary>
        public void PlayShards(Vector3 center, ObstacleType type, float delaySeconds)
        {
            PlayParticles(center, ObstacleArt.GetShardColor(_visuals, type), _feel.ObstacleShardCount, _feel.ObstacleShardDistanceInCells,
                _feel.ObstacleShardSizeInCells, _feel.ObstacleShardSeconds, delaySeconds);
        }

        // Evenly spaced particles that fly out of a point while they shrink and fade. Skipped ones (empty pool) are simply missing.
        private void PlayParticles(Vector3 center, Color tint, int count, float distanceInCells, float sizeInCells, float seconds, float delaySeconds)
        {
            if (count <= 0) return;

            Sprite sprite = _visuals.BurstSprite != null ? _visuals.BurstSprite : PlaceholderSprite.Solid;
            float scale = sizeInCells * _cellSize / TileArt.SizeOf(sprite);
            float angleStep = 2f * Mathf.PI / count;

            for (int i = 0; i < count; i++)
            {
                if (!TryTake(out FxSprite fx)) return;

                // Evenly spaced directions, each nudged a little so the burst does not look like a perfect wheel.
                float angle = (i + Random.Range(-0.3f, 0.3f)) * angleStep;
                float distance = distanceInCells * _cellSize * Random.Range(0.6f, 1.2f);
                Vector3 target = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;

                fx.PlayParticle(sprite, tint, center, target, scale, seconds, delaySeconds);
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

        /// <summary>A beam from the ColorBomb's cell to one tile's cell. It starts after delaySeconds and takes the BeamSeconds of the feel settings to arrive.</summary>
        public void PlayBeam(Vector3 from, Vector3 to, float delaySeconds)
        {
            if (!TryTake(out FxSprite fx)) return;

            Sprite sprite = _visuals.StreakSprite != null ? _visuals.StreakSprite : PlaceholderSprite.Solid;
            fx.PlayBeam(sprite, _feel.ColorBombBeamColor, from, to, _feel.ColorBombBeamThicknessInCells * _cellSize, _feel.ColorBombBeamSeconds, delaySeconds);
        }

        /// <summary>A ring that spreads out from a point, started after delaySeconds (a new ColorBomb appearing).</summary>
        public void PlayRing(Vector3 center, float delaySeconds)
        {
            if (!TryTake(out FxSprite fx)) return;

            fx.PlayRing(PlaceholderSprite.Bomb, _feel.ColorBombBeamColor, center, _cellSize * 0.6f,
                _feel.ColorBombRingSizeInCells * _cellSize, _feel.ColorBombPulseSeconds + _feel.PopInDuration, delaySeconds);
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
