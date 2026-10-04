using System;
using Match3.Core;
using Match3.View;
using UnityEngine;

namespace Match3.Game
{
    /// <summary>
    /// Composition root of the scene: creates the model and the game state machine and wires them together.
    /// It holds no game logic. Moves, goals and states report changes through C# events;
    /// until the HUD exists (M7) this class just writes those events to the Console.
    /// Level settings are Inspector fields for now; M7 moves them into a LevelData asset.
    /// </summary>
    [RequireComponent(typeof(BoardView), typeof(StepPlayer), typeof(SwipeInput))]
    public sealed class LevelController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private StepPlayer stepPlayer;
        [SerializeField] private SwipeInput swipeInput;

        [Header("Board")]
        [SerializeField, Range(4, 10)] private int width = 8;
        [SerializeField, Range(4, 12)] private int height = 8;
        [SerializeField, Range(3, 6)] private int colorCount = 5;

        [Header("Rules")]
        [SerializeField, Min(1)] private int moveLimit = 20;
        [SerializeField] private GoalDefinition[] goals =
        {
            new GoalDefinition(TileColor.Red, 12),
            new GoalDefinition(TileColor.Blue, 12)
        };

        [Header("Randomness")]
        [Tooltip("Tick this to get a different board every time you press Play.")]
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 12345;

        [Header("Debug")]
        [Tooltip("Write moves, goals and state changes to the Console (temporary, until the HUD exists).")]
        [SerializeField] private bool logToConsole = true;

        private GameStateMachine _machine;
        private GoalTracker _goalTracker;
        private Board _board;

        private void Reset()
        {
            // Editor-only convenience when the component is added.
            boardView = GetComponent<BoardView>();
            stepPlayer = GetComponent<StepPlayer>();
            swipeInput = GetComponent<SwipeInput>();
        }

        private void Start()
        {
            StartNewGame();
        }

        private void OnDestroy()
        {
            _machine?.Stop(); // lets the current state unsubscribe from the swipe event
        }

        /// <summary>Right-click the component header in the Inspector (while playing) and choose "Restart".</summary>
        [ContextMenu("Restart")]
        public void StartNewGame()
        {
            if (stepPlayer.IsPlaying) return;

            if (!GoalsAreReachable()) return;

            _machine?.Stop(); // leave the old game's state before building the new one

            int usedSeed = randomSeed ? Environment.TickCount : seed;
            TileColor[] colors = new TileColor[colorCount];
            Array.Copy((TileColor[])Enum.GetValues(typeof(TileColor)), colors, colorCount);

            SystemRandom random = new SystemRandom(usedSeed);
            Board board = new BoardGenerator(random).Generate(width, height, colors);
            _board = board;
            BoardResolver resolver = new BoardResolver(random, colors);
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());

            MoveCounter moves = new MoveCounter(moveLimit);
            _goalTracker = new GoalTracker(goals);

            boardView.Build(board);

            // Build the states from the end of the flow to the start, so each one can be given what it hands over to.
            _machine = new GameStateMachine();
            ResolvingState resolving = new ResolvingState(_machine, board, moveFinder, moves, _goalTracker);
            SwappingState swapping = new SwappingState(_machine, board, resolver, stepPlayer, moves, resolving);
            IdleState idle = new IdleState(_machine, swipeInput, swapping);

            _machine.Register(idle);
            _machine.Register(swapping);
            _machine.Register(resolving);
            _machine.Register(new WinState());
            _machine.Register(new LoseState());

            moves.MovesChanged += OnMovesChanged;
            _goalTracker.GoalChanged += OnGoalChanged;
            _machine.StateChanged += OnStateChanged;
            resolving.NoPossibleMoves += OnNoPossibleMoves;

            if (logToConsole) Debug.Log("New level: " + moveLimit + " moves, " + goals.Length + " goal(s).");

            _machine.ChangeTo<IdleState>();
        }

        /// <summary>
        /// Testing aid: turns four tiles into special tiles so every special and combo can be tried without waiting for luck.
        /// Bomb at (2,2) and RocketHorizontal at (3,2) are neighbors (swap them for a combo); RocketVertical at (5,5) is on its own.
        /// A tile keeps its color, so this never creates a match. Only works while the game waits for a swipe.
        /// </summary>
        [ContextMenu("Debug: Place Specials")]
        private void DebugPlaceSpecials()
        {
            if (_board == null || stepPlayer.IsPlaying || !_machine.IsIn<IdleState>()) return;

            MakeSpecial(2, 2, SpecialType.Bomb);
            MakeSpecial(3, 2, SpecialType.RocketHorizontal);
            MakeSpecial(5, 5, SpecialType.RocketVertical);
            MakeSpecial(6, 1, SpecialType.Bomb);

            boardView.Build(_board); // shows the changed board
        }

        private void MakeSpecial(int x, int y, SpecialType special)
        {
            if (!_board.IsInside(x, y)) return;

            _board.Set(x, y, _board.NewTile(_board.Get(x, y).Color, special));
        }

        // A goal for a color that is not on the board could never be reached.
        private bool GoalsAreReachable()
        {
            for (int i = 0; i < goals.Length; i++)
            {
                if ((int)goals[i].color >= colorCount)
                {
                    Debug.LogError("Goal " + i + " asks for " + goals[i].color + ", but only the first " + colorCount
                        + " colors are on the board. Raise Color Count or change the goal.", this);
                    return false;
                }
            }

            return true;
        }

        private void OnMovesChanged(int movesLeft)
        {
            if (logToConsole) Debug.Log("Moves left: " + movesLeft);
        }

        private void OnGoalChanged(int goalIndex, int remaining)
        {
            if (logToConsole) Debug.Log("Goal " + _goalTracker.GetColor(goalIndex) + ": " + remaining + " left");
        }

        private void OnStateChanged(IGameState previous, IGameState next)
        {
            if (!logToConsole) return;

            if (next is WinState) Debug.Log("YOU WIN! Right-click this component and choose Restart.");
            else if (next is LoseState) Debug.Log("OUT OF MOVES. Right-click this component and choose Restart.");
            else Debug.Log("State: " + (previous == null ? "(none)" : previous.GetType().Name) + " -> " + next.GetType().Name);
        }

        private void OnNoPossibleMoves()
        {
            Debug.LogWarning("No possible moves left. Shuffling arrives in M8; for now right-click this component and choose Restart.");
        }
    }
}
