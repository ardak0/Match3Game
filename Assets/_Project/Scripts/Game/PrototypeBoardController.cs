using System;
using Match3.Core;
using Match3.View;
using UnityEngine;

namespace Match3.Game
{
    /// <summary>
    /// TEMPORARY wiring for M3, so the board can be played in the editor.
    /// M5 replaces it with LevelController + the game state machine (and then moves, goals, win and lose).
    ///
    /// Flow: swipe -> model resolves the swap instantly -> StepPlayer animates the steps -> input is allowed again.
    /// While animations play, input is switched off. The model is always ahead of the view.
    /// </summary>
    [RequireComponent(typeof(BoardView), typeof(StepPlayer), typeof(SwipeInput))]
    public sealed class PrototypeBoardController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private StepPlayer stepPlayer;
        [SerializeField] private SwipeInput swipeInput;

        [Header("Board")]
        [SerializeField, Range(4, 10)] private int width = 8;
        [SerializeField, Range(4, 12)] private int height = 8;
        [SerializeField, Range(3, 6)] private int colorCount = 5;

        [Header("Randomness")]
        [Tooltip("Tick this to get a different board every time you press Play.")]
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 12345;

        private Board _board;
        private BoardResolver _resolver;
        private MoveFinder _moveFinder;
        private Action _onPlaybackFinished;

        private void Reset()
        {
            // Editor-only convenience when the component is added.
            boardView = GetComponent<BoardView>();
            stepPlayer = GetComponent<StepPlayer>();
            swipeInput = GetComponent<SwipeInput>();
        }

        private void Awake()
        {
            _onPlaybackFinished = OnPlaybackFinished; // created once, reused for every move
        }

        private void Start()
        {
            swipeInput.SwipeDetected += OnSwipe;
            StartNewGame();
        }

        private void OnDestroy()
        {
            if (swipeInput != null) swipeInput.SwipeDetected -= OnSwipe;
        }

        /// <summary>Right-click the component header in the Inspector (while playing) and choose "Restart".</summary>
        [ContextMenu("Restart")]
        public void StartNewGame()
        {
            if (stepPlayer.IsPlaying) return;

            int usedSeed = randomSeed ? Environment.TickCount : seed;

            TileColor[] colors = new TileColor[colorCount];
            Array.Copy((TileColor[])Enum.GetValues(typeof(TileColor)), colors, colorCount);

            SystemRandom random = new SystemRandom(usedSeed);
            _board = new BoardGenerator(random).Generate(width, height, colors);
            _resolver = new BoardResolver(random, colors);
            _moveFinder = new MoveFinder(new MatchFinder());

            boardView.Build(_board);
            swipeInput.InputEnabled = true;
        }

        private void OnSwipe(GridPos a, GridPos b)
        {
            swipeInput.InputEnabled = false;

            // Read the ids first: if the swap is rejected, the view needs them to animate the bounce back.
            Tile tileA = _board.Get(a);
            Tile tileB = _board.Get(b);

            ResolveResult result = _resolver.ResolveSwap(_board, a, b); // the model decides, instantly

            if (result.IsValid)
            {
                stepPlayer.Play(result.Steps, _onPlaybackFinished);
            }
            else
            {
                stepPlayer.PlayRejectedSwap(tileA.Id, tileB.Id, a, b, _onPlaybackFinished);
            }
        }

        private void OnPlaybackFinished()
        {
            if (!_moveFinder.HasPossibleMove(_board))
            {
                Debug.LogWarning("No possible moves left. Shuffling arrives in M8; for now right-click this component and choose Restart.");
            }

            swipeInput.InputEnabled = true;
        }
    }
}
