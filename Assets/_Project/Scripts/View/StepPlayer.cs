using System;
using System.Collections.Generic;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using Match3.Infrastructure;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// Plays the steps the model produced, using DOTween. The model has already resolved everything;
    /// this class only makes it visible, one wave at a time:
    ///
    ///   wave 0:  swap
    ///   wave n:  clear (tiles shrink away; a special tile that goes off plays its effect first,
    ///            and tiles hit by it clear a moment later, so a chain reaction ripples outwards)
    ///            a ColorBomb that goes off first shoots a beam at every tile it reaches, and if it turns tiles into
    ///            rockets or bombs they change (pop) before they go off; everything it reaches clears after that
    ///            ->  a special tile pops in, if the match created one (a ColorBomb also pulses)
    ///            ->  fall + spawn (together)
    ///
    /// Each wave is one DOTween Sequence. When it finishes, the next wave is built and played.
    /// StepPlayer never changes game state. It only moves TileViews through BoardView.
    /// </summary>
    public sealed class StepPlayer : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        // All timings live in the FeelSettings asset (assigned on the BoardView), so one place tunes the whole feel.
        private FeelSettings Feel => boardView.Feel;
        private float SwapDuration => Feel.SwapDuration;
        private float ClearDuration => Feel.ClearDuration;
        private float ActivationDuration => Feel.ActivationDuration;
        private float ChainDelay => Feel.ChainDelay;

        private IReadOnlyList<ResolveStep> _steps;
        private int _nextStep;
        private Action _onComplete;
        private ClearStep _clearInCurrentWave;

        // Per wave: the ColorBombs that fire in it (they delay what they reach), how long the conversion phase is
        // (0 when the wave converts nothing) and when the first special pops in (the new ColorBomb's ring is timed by it).
        private readonly List<ColorBombFireStep> _firesInWave = new List<ColorBombFireStep>();
        private float _convertSeconds;
        private float _popInStart;

        // How big a ColorBomb swells (in cells) while it charges up, just before it clears.
        private const float ColorBombSwellInCells = 1.5f;

        // Created once so playing a wave does not allocate new delegates.
        private TweenCallback _playNextWave;
        private TweenCallback _removeClearedTiles;
        private TweenCallback _finish;
        private TweenCallback _shakeCamera;

        public bool IsPlaying { get; private set; }

        /// <summary>
        /// A clear wave of the current move has started and it is the given wave number (1 = the player's own match,
        /// 2 = the first cascade, ...). Only raised from the wave set in FeelSettings (Combo Minimum Wave) upwards.
        /// The HUD listens and shows the combo text.
        /// </summary>
        public event Action<int> ComboReached;

        private void Reset()
        {
            boardView = GetComponent<BoardView>(); // editor-only convenience when the component is added
        }

        private void Awake()
        {
            DOTweenSetup.Configure();

            _playNextWave = PlayNextWave;
            _removeClearedTiles = RemoveClearedTiles;
            _finish = Finish;
            _shakeCamera = ShakeCamera;
        }

        /// <summary>Plays all steps of a valid resolve. onComplete is called once everything has finished.</summary>
        public void Play(IReadOnlyList<ResolveStep> steps, Action onComplete)
        {
            if (IsPlaying) throw new InvalidOperationException("StepPlayer is already playing.");

            IsPlaying = true;
            _steps = steps;
            _nextStep = 0;
            _onComplete = onComplete;
            PlayNextWave();
        }

        /// <summary>The model rejected the swap: slide the two tiles over each other, then back.</summary>
        public void PlayRejectedSwap(int tileIdA, int tileIdB, GridPos a, GridPos b, Action onComplete)
        {
            if (IsPlaying) throw new InvalidOperationException("StepPlayer is already playing.");

            IsPlaying = true;
            _onComplete = onComplete;

            Transform tileA = boardView.GetTile(tileIdA).transform;
            Transform tileB = boardView.GetTile(tileIdB).transform;
            Vector3 positionA = boardView.CellToLocal(a.X, a.Y);
            Vector3 positionB = boardView.CellToLocal(b.X, b.Y);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(tileA.DOLocalMove(positionB, SwapDuration).SetEase(Ease.OutQuad));
            sequence.Join(tileB.DOLocalMove(positionA, SwapDuration).SetEase(Ease.OutQuad));
            sequence.Append(tileA.DOLocalMove(positionA, SwapDuration).SetEase(Ease.OutQuad));
            sequence.Join(tileB.DOLocalMove(positionB, SwapDuration).SetEase(Ease.OutQuad));
            sequence.OnComplete(_finish);
            sequence.SetLink(gameObject);
        }

        /// <summary>The board was shuffled: after a short pause, every moved tile glides to its new cell at the same time.</summary>
        public void PlayShuffle(ShuffleStep shuffle, Action onComplete)
        {
            if (IsPlaying) throw new InvalidOperationException("StepPlayer is already playing.");

            IsPlaying = true;
            _onComplete = onComplete;

            if (shuffle.Moves.Count == 0)
            {
                Finish();
                return;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(Feel.ShuffleDelay);

            bool movementStarted = false; // the first move is Appended (after the pause), the others Join it
            for (int i = 0; i < shuffle.Moves.Count; i++)
            {
                TileMove move = shuffle.Moves[i];
                Tween glide = boardView.GetTile(move.TileId).transform
                    .DOLocalMove(boardView.CellToLocal(move.To.X, move.To.Y), Feel.ShuffleDuration)
                    .SetEase(Ease.InOutQuad);
                AppendOrJoin(sequence, glide, ref movementStarted);
            }

            sequence.OnComplete(_finish);
            sequence.SetLink(gameObject);
        }

        // Builds and starts one Sequence for all steps that share the next wave number.
        private void PlayNextWave()
        {
            if (_nextStep >= _steps.Count)
            {
                Finish();
                return;
            }

            int wave = _steps[_nextStep].Wave;
            Sequence sequence = DOTween.Sequence();
            bool movementStarted = false; // the first fall/spawn tween is Appended, later ones Join it so they run together
            bool popInStarted = false;    // same idea for special tiles popping in
            _clearInCurrentWave = null;
            _firesInWave.Clear();
            _convertSeconds = WaveConverts(wave) ? Feel.ColorBombConvertSeconds : 0f;

            while (_nextStep < _steps.Count && _steps[_nextStep].Wave == wave)
            {
                ResolveStep step = _steps[_nextStep];
                _nextStep++;

                if (step is SwapStep swap)
                {
                    AddSwap(sequence, swap);
                }
                else if (step is ColorBombFireStep fire)
                {
                    AddColorBombFire(fire);
                }
                else if (step is ConvertStep convert)
                {
                    AddConvert(sequence, convert);
                }
                else if (step is ClearStep clear)
                {
                    AddClear(sequence, clear);
                }
                else if (step is SpecialCreatedStep created)
                {
                    AddSpecialCreated(sequence, created, ref popInStarted);
                }
                else if (step is FallStep fall)
                {
                    AddFall(sequence, fall, ref movementStarted);
                }
                else if (step is SpawnStep spawn)
                {
                    AddSpawn(sequence, spawn, ref movementStarted);
                }
            }

            if (_clearInCurrentWave != null && wave >= Feel.ComboMinimumWave)
            {
                ComboReached?.Invoke(wave);
            }

            sequence.OnComplete(_playNextWave);
            sequence.SetLink(gameObject);
        }

        // True if this wave has a ConvertStep. It has to be known before the first step is built, because it delays the clear.
        private bool WaveConverts(int wave)
        {
            for (int i = _nextStep; i < _steps.Count && _steps[i].Wave == wave; i++)
            {
                if (_steps[i] is ConvertStep) return true;
            }

            return false;
        }

        // How much later than its normal chain time a tile at this chain depth starts to clear in this wave:
        // one beam time for every ColorBomb that fires at a shallower depth (the tile has to wait for that beam),
        // plus the conversion phase for everything the ColorBomb swap reaches (depth 1 and deeper).
        private float ExtraDelay(int chainDepth)
        {
            float extra = 0f;
            for (int i = 0; i < _firesInWave.Count; i++)
            {
                if (_firesInWave[i].Depth < chainDepth) extra += Feel.ColorBombBeamSeconds;
            }

            if (chainDepth >= 1) extra += _convertSeconds;
            return extra;
        }

        // A ColorBomb goes off: one beam from its cell to every tile it reaches. The beams are pooled effects that
        // start on their own after a delay, so nothing is added to the Sequence.
        private void AddColorBombFire(ColorBombFireStep fire)
        {
            float start = fire.Depth * ChainDelay + ExtraDelay(fire.Depth);
            _firesInWave.Add(fire); // after the line above: a bomb does not delay itself

            Vector3 from = boardView.CellToLocal(fire.Position.X, fire.Position.Y);
            for (int i = 0; i < fire.Targets.Count; i++)
            {
                GridPos target = fire.Targets[i];
                boardView.Effects.PlayBeam(from, boardView.CellToLocal(target.X, target.Y), start);
            }
        }

        // The tiles a ColorBomb turns into rockets or bombs change right after the beams have arrived, all together.
        private void AddConvert(Sequence sequence, ConvertStep convert)
        {
            float start = Feel.ColorBombBeamSeconds;

            for (int i = 0; i < convert.Tiles.Count; i++)
            {
                ConvertedTile tile = convert.Tiles[i];
                TileView view = boardView.GetTile(tile.TileId);
                sequence.Insert(start, view.CreateConvertTween(tile.Special, Feel.ColorBombConvertPunch, _convertSeconds));
            }
        }

        private void AddSwap(Sequence sequence, SwapStep swap)
        {
            Transform tileA = boardView.GetTile(swap.TileIdA).transform;
            Transform tileB = boardView.GetTile(swap.TileIdB).transform;

            sequence.Append(tileA.DOLocalMove(boardView.CellToLocal(swap.B.X, swap.B.Y), SwapDuration).SetEase(Ease.OutQuad));
            sequence.Join(tileB.DOLocalMove(boardView.CellToLocal(swap.A.X, swap.A.Y), SwapDuration).SetEase(Ease.OutQuad));
        }

        private void AddClear(Sequence sequence, ClearStep clear)
        {
            float popSeconds = Feel.ClearPopSeconds;

            for (int i = 0; i < clear.Tiles.Count; i++)
            {
                ClearedTile cleared = clear.Tiles[i];
                TileView view = boardView.GetTile(cleared.TileId);
                Vector3 center = view.transform.localPosition;
                bool isSpecial = cleared.Special != SpecialType.None;

                // Tiles hit by a chain reaction wait for the special that hit them to finish its effect.
                float start = cleared.ChainDepth * ChainDelay + ExtraDelay(cleared.ChainDepth);

                if (isSpecial)
                {
                    sequence.Insert(start, CreateActivationEffect(view, cleared.Special));
                    AddSpecialJuice(sequence, center, cleared.Special, start);
                    start += ActivationDuration;
                }

                // The tile pops and shrinks away (a special already swelled, so it skips the pop), and its particles
                // fly out the moment it starts to shrink.
                sequence.Insert(start, view.CreateClearTween(ClearDuration, !isSpecial));
                boardView.Effects.PlayBurst(center, cleared.Color, isSpecial ? start : start + popSeconds);
            }

            // When every tile has shrunk away, delete their views. Everything after this in the wave starts after that.
            _clearInCurrentWave = clear;
            sequence.AppendCallback(_removeClearedTiles);
        }

        // The extra show of a special tile going off: a rocket gets a glowing streak along its whole line, a bomb shakes the camera.
        private void AddSpecialJuice(Sequence sequence, Vector3 center, SpecialType special, float start)
        {
            float cell = boardView.CellSize;

            switch (special)
            {
                case SpecialType.RocketHorizontal:
                    boardView.Effects.PlayStreak(new Vector3(0f, center.y, 0f), true, boardView.Width * cell, start);
                    break;
                case SpecialType.RocketVertical:
                    boardView.Effects.PlayStreak(new Vector3(center.x, 0f, 0f), false, boardView.Height * cell, start);
                    break;
                case SpecialType.ColorBomb:
                    break; // its show is the beams (AddColorBombFire) and the swell of its activation effect
                default: // bomb
                    sequence.InsertCallback(start, _shakeCamera);
                    break;
            }
        }

        private void ShakeCamera() => boardView.Effects.ShakeCamera();

        // The effect of a special tile going off. It is the tile's own picture that changes size, so no extra objects are needed.
        // A beam is longer than the board on purpose: the board's mask hides the part that sticks out.
        private Tween CreateActivationEffect(TileView view, SpecialType special)
        {
            float cell = boardView.CellSize;
            float beamLength = 2f * Mathf.Max(boardView.Width, boardView.Height) * cell;
            float beamThickness = Feel.BeamThicknessInCells * cell;

            switch (special)
            {
                case SpecialType.RocketHorizontal:
                    return view.transform.DOScale(view.ScaleForSize(beamLength, beamThickness), ActivationDuration).SetEase(Ease.OutQuad);
                case SpecialType.RocketVertical:
                    return view.transform.DOScale(view.ScaleForSize(beamThickness, beamLength), ActivationDuration).SetEase(Ease.OutQuad);
                case SpecialType.ColorBomb: // charges up: swells a little while its beams fly out
                    return view.transform.DOScale(view.ScaleForSize(ColorBombSwellInCells * cell, ColorBombSwellInCells * cell), ActivationDuration).SetEase(Ease.OutBack);
                default: // bomb: swell to the size of its 3x3 blast
                    return view.transform.DOScale(view.ScaleForSize(3f * cell, 3f * cell), ActivationDuration).SetEase(Ease.OutBack);
            }
        }

        // The new special tile appears where the match was, growing from nothing. Its view exists from the start of
        // the wave but is invisible (scale 0) until the clear is over.
        private void AddSpecialCreated(Sequence sequence, SpecialCreatedStep created, ref bool popInStarted)
        {
            TileView view = boardView.CreateTile(
                created.TileId, created.Color, created.Special, boardView.CellToLocal(created.Position.X, created.Position.Y));

            Vector3 fullScale = view.transform.localScale;
            view.transform.localScale = Vector3.zero;

            // Appended tweens start where the sequence is now; joined ones start with the first pop.
            float popStart = popInStarted ? _popInStart : sequence.Duration(false);

            Tween pop = view.transform.DOScale(fullScale, Feel.PopInDuration).SetEase(Ease.OutBack);
            if (created.Special == SpecialType.ColorBomb && Feel.ColorBombPulseStrength > 0f)
            {
                // The pulse: after it has popped in, the ColorBomb bounces once and a ring spreads out from it.
                Sequence pulse = DOTween.Sequence();
                pulse.Append(pop);
                pulse.Append(view.transform.DOPunchScale(fullScale * Feel.ColorBombPulseStrength, Feel.ColorBombPulseSeconds, 2, 0.5f));
                boardView.Effects.PlayRing(view.transform.localPosition, popStart + Feel.PopInDuration * 0.5f);
                pop = pulse;
            }

            if (popInStarted)
            {
                sequence.Join(pop);
            }
            else
            {
                sequence.Append(pop);
                popInStarted = true;
                _popInStart = popStart;
            }
        }

        private void AddFall(Sequence sequence, FallStep fall, ref bool movementStarted)
        {
            for (int i = 0; i < fall.Moves.Count; i++)
            {
                TileMove move = fall.Moves[i];
                TileView view = boardView.GetTile(move.TileId);
                Tween drop = CreateDrop(view, move.To, move.From.Y - move.To.Y);
                AppendOrJoin(sequence, drop, ref movementStarted);
            }
        }

        private void AddSpawn(Sequence sequence, SpawnStep spawn, ref bool movementStarted)
        {
            for (int i = 0; i < spawn.Spawns.Count; i++)
            {
                TileSpawn tileSpawn = spawn.Spawns[i];

                // The new tile starts above the board (hidden by the board mask) and drops in.
                TileView view = boardView.CreateTile(
                    tileSpawn.TileId, tileSpawn.Color, tileSpawn.Special, boardView.CellToLocal(tileSpawn.To.X, tileSpawn.FromY));

                Tween drop = CreateDrop(view, tileSpawn.To, tileSpawn.FromY - tileSpawn.To.Y);
                AppendOrJoin(sequence, drop, ref movementStarted);
            }
        }

        // A tile falls to its cell, dips a little past it and settles (Ease.OutBack gives that overshoot). When it lands it gets squashed for a moment.
        private Tween CreateDrop(TileView view, GridPos target, int cellsFallen)
        {
            float duration = Feel.FallBaseSeconds + Feel.FallPerSqrtCell * Mathf.Sqrt(cellsFallen);

            // OutBack overshoots by roughly 0.0588 * overshoot * distance. Solve for a constant dip in cells.
            float overshoot = Mathf.Clamp(Feel.LandingBounceInCells / (0.0588f * cellsFallen), 0.1f, 1.7f);

            return view.transform.DOLocalMove(boardView.CellToLocal(target.X, target.Y), duration)
                .SetEase(Ease.OutBack, overshoot)
                .OnComplete(view.Landed);
        }

        private static void AppendOrJoin(Sequence sequence, Tween tween, ref bool movementStarted)
        {
            if (movementStarted)
            {
                sequence.Join(tween);
            }
            else
            {
                sequence.Append(tween);
                movementStarted = true;
            }
        }

        private void RemoveClearedTiles()
        {
            for (int i = 0; i < _clearInCurrentWave.Tiles.Count; i++)
            {
                boardView.RemoveTile(_clearInCurrentWave.Tiles[i].TileId);
            }
        }

        private void Finish()
        {
            IsPlaying = false;
            Action done = _onComplete;
            _onComplete = null;
            _steps = null;
            done?.Invoke(); // last, so the callback may safely start the next playback
        }
    }
}
