using System;
using System.Collections.Generic;
using DG.Tweening;
using Match3.Core;
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
    ///            ->  a special tile pops in, if the match created one
    ///            ->  fall + spawn (together)
    ///
    /// Each wave is one DOTween Sequence. When it finishes, the next wave is built and played.
    /// StepPlayer never changes game state. It only moves TileViews through BoardView.
    /// </summary>
    public sealed class StepPlayer : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        [Header("Timing (seconds)")]
        [SerializeField] private float swapDuration = 0.18f;
        [SerializeField] private float clearDuration = 0.22f;
        [Tooltip("Fall time = base + perSqrtCell * sqrt(cells fallen), so long falls feel faster per cell.")]
        [SerializeField] private float fallBaseSeconds = 0.12f;
        [SerializeField] private float fallPerSqrtCell = 0.09f;

        [Tooltip("How far (in cells) a falling tile dips past its cell before settling. Small = slight bounce.")]
        [SerializeField] private float landingBounceInCells = 0.08f;

        [Header("Special tiles (seconds)")]
        [Tooltip("How long a rocket takes to stretch into a beam, or a bomb to swell to its blast size.")]
        [SerializeField] private float activationDuration = 0.18f;

        [Tooltip("Delay per chain-reaction link: tiles hit by a special clear this much later than the special itself. Keep it at least as long as the activation.")]
        [SerializeField] private float chainDelay = 0.2f;

        [Tooltip("How long a newly created special tile takes to pop in.")]
        [SerializeField] private float popInDuration = 0.18f;

        [Tooltip("Thickness of a rocket's beam, in cells.")]
        [SerializeField] private float beamThicknessInCells = 0.35f;

        private IReadOnlyList<ResolveStep> _steps;
        private int _nextStep;
        private Action _onComplete;
        private ClearStep _clearInCurrentWave;

        // Created once so playing a wave does not allocate new delegates.
        private TweenCallback _playNextWave;
        private TweenCallback _removeClearedTiles;
        private TweenCallback _finish;

        public bool IsPlaying { get; private set; }

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
            sequence.Append(tileA.DOLocalMove(positionB, swapDuration).SetEase(Ease.OutQuad));
            sequence.Join(tileB.DOLocalMove(positionA, swapDuration).SetEase(Ease.OutQuad));
            sequence.Append(tileA.DOLocalMove(positionA, swapDuration).SetEase(Ease.OutQuad));
            sequence.Join(tileB.DOLocalMove(positionB, swapDuration).SetEase(Ease.OutQuad));
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

            while (_nextStep < _steps.Count && _steps[_nextStep].Wave == wave)
            {
                ResolveStep step = _steps[_nextStep];
                _nextStep++;

                if (step is SwapStep swap)
                {
                    AddSwap(sequence, swap);
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

            sequence.OnComplete(_playNextWave);
            sequence.SetLink(gameObject);
        }

        private void AddSwap(Sequence sequence, SwapStep swap)
        {
            Transform tileA = boardView.GetTile(swap.TileIdA).transform;
            Transform tileB = boardView.GetTile(swap.TileIdB).transform;

            sequence.Append(tileA.DOLocalMove(boardView.CellToLocal(swap.B.X, swap.B.Y), swapDuration).SetEase(Ease.OutQuad));
            sequence.Join(tileB.DOLocalMove(boardView.CellToLocal(swap.A.X, swap.A.Y), swapDuration).SetEase(Ease.OutQuad));
        }

        private void AddClear(Sequence sequence, ClearStep clear)
        {
            for (int i = 0; i < clear.Tiles.Count; i++)
            {
                ClearedTile cleared = clear.Tiles[i];
                TileView view = boardView.GetTile(cleared.TileId);

                // Tiles hit by a chain reaction wait for the special that hit them to finish its effect.
                float start = cleared.ChainDepth * chainDelay;

                if (cleared.Special != SpecialType.None)
                {
                    sequence.Insert(start, CreateActivationEffect(view, cleared.Special));
                    start += activationDuration;
                }

                sequence.Insert(start, view.transform.DOScale(0f, clearDuration).SetEase(Ease.InBack));
            }

            // When every tile has shrunk away, delete their views. Everything after this in the wave starts after that.
            _clearInCurrentWave = clear;
            sequence.AppendCallback(_removeClearedTiles);
        }

        // The effect of a special tile going off. It is the tile's own picture that changes size, so no extra objects are needed.
        // A beam is longer than the board on purpose: the board's mask hides the part that sticks out.
        private Tween CreateActivationEffect(TileView view, SpecialType special)
        {
            float cell = boardView.CellSize;
            float beamLength = 2f * Mathf.Max(boardView.Width, boardView.Height) * cell;
            float beamThickness = beamThicknessInCells * cell;

            switch (special)
            {
                case SpecialType.RocketHorizontal:
                    return view.transform.DOScale(view.ScaleForSize(beamLength, beamThickness), activationDuration).SetEase(Ease.OutQuad);
                case SpecialType.RocketVertical:
                    return view.transform.DOScale(view.ScaleForSize(beamThickness, beamLength), activationDuration).SetEase(Ease.OutQuad);
                default: // bomb: swell to the size of its 3x3 blast
                    return view.transform.DOScale(view.ScaleForSize(3f * cell, 3f * cell), activationDuration).SetEase(Ease.OutBack);
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

            Tween pop = view.transform.DOScale(fullScale, popInDuration).SetEase(Ease.OutBack);
            if (popInStarted)
            {
                sequence.Join(pop);
            }
            else
            {
                sequence.Append(pop);
                popInStarted = true;
            }
        }

        private void AddFall(Sequence sequence, FallStep fall, ref bool movementStarted)
        {
            for (int i = 0; i < fall.Moves.Count; i++)
            {
                TileMove move = fall.Moves[i];
                Transform tile = boardView.GetTile(move.TileId).transform;
                Tween drop = CreateDrop(tile, move.To, move.From.Y - move.To.Y);
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

                Tween drop = CreateDrop(view.transform, tileSpawn.To, tileSpawn.FromY - tileSpawn.To.Y);
                AppendOrJoin(sequence, drop, ref movementStarted);
            }
        }

        // A tile falls to its cell, dips a little past it and settles (Ease.OutBack gives that overshoot).
        private Tween CreateDrop(Transform tile, GridPos target, int cellsFallen)
        {
            float duration = fallBaseSeconds + fallPerSqrtCell * Mathf.Sqrt(cellsFallen);

            // OutBack overshoots by roughly 0.0588 * overshoot * distance. Solve for a constant dip in cells.
            float overshoot = Mathf.Clamp(landingBounceInCells / (0.0588f * cellsFallen), 0.1f, 1.7f);

            return tile.DOLocalMove(boardView.CellToLocal(target.X, target.Y), duration)
                .SetEase(Ease.OutBack, overshoot);
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
