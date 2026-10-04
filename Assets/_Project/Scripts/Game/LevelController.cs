using System;
using Match3.Core;
using Match3.Data;
using Match3.View;
using Match3.View.UI;
using UnityEngine;

namespace Match3.Game
{
    /// <summary>
    /// Composition root of the scene: loads a level from its LevelData, creates the model and the game state machine,
    /// and wires them to the view and the UI. It holds no game rules.
    ///
    /// Flow of information (all through C# events):
    ///   MoveCounter.MovesChanged  -> HudView.SetMoves
    ///   GoalTracker.GoalChanged   -> HudView.SetGoalRemaining
    ///   GameStateMachine.StateChanged (WinState / LoseState) -> EndScreenView
    ///   EndScreenView.RetryClicked / NextClicked -> this class starts a level
    /// </summary>
    [RequireComponent(typeof(BoardView), typeof(StepPlayer), typeof(SwipeInput))]
    public sealed class LevelController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private StepPlayer stepPlayer;
        [SerializeField] private SwipeInput swipeInput;

        [Header("Levels")]
        [Tooltip("Played in this order. Drag the Level assets here.")]
        [SerializeField] private LevelData[] levels;

        [Tooltip("Which level to start with (0 = the first). Handy for testing a later level.")]
        [SerializeField, Min(0)] private int startLevelIndex;

        [Header("Debug")]
        [Tooltip("Write moves, goals and state changes to the Console. Off by default: the log strings allocate, which hides real garbage in the Profiler.")]
        [SerializeField] private bool logToConsole;

        private GameStateMachine _machine;
        private GoalTracker _goalTracker;
        private Board _board;
        private HudView _hud;
        private EndScreenView _endScreen;
        private int _levelIndex;

        private bool HasNextLevel => _levelIndex + 1 < levels.Length;

        private void Reset()
        {
            // Editor-only convenience when the component is added.
            boardView = GetComponent<BoardView>();
            stepPlayer = GetComponent<StepPlayer>();
            swipeInput = GetComponent<SwipeInput>();
        }

        private void Awake()
        {
            _hud = HudView.Create(boardView.Visuals);
            _endScreen = EndScreenView.Create();
            _endScreen.RetryClicked += RetryLevel;
            _endScreen.NextClicked += GoToNextLevel;
        }

        private void Start()
        {
            StartLevel(startLevelIndex);
        }

        private void OnDestroy()
        {
            if (_endScreen != null)
            {
                _endScreen.RetryClicked -= RetryLevel;
                _endScreen.NextClicked -= GoToNextLevel;
            }

            _machine?.Stop(); // lets the current state unsubscribe from the swipe event
        }

        /// <summary>Starts the level again from a new board. Also available from the Inspector (right-click the header, "Restart").</summary>
        [ContextMenu("Restart")]
        public void RetryLevel()
        {
            StartLevel(_levelIndex);
        }

        /// <summary>The next level, or the first one again after the last.</summary>
        public void GoToNextLevel()
        {
            StartLevel(HasNextLevel ? _levelIndex + 1 : 0);
        }

        public void StartLevel(int index)
        {
            if (stepPlayer.IsPlaying) return;

            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("LevelController has no levels. Drag the Level assets into its Levels list.", this);
                return;
            }

            if (index < 0 || index >= levels.Length)
            {
                Debug.LogError("There is no level " + index + ". Levels has " + levels.Length + " entries (0 is the first).", this);
                return;
            }

            LevelData level = levels[index];
            if (level == null)
            {
                Debug.LogError("Levels entry " + index + " is empty.", this);
                return;
            }

            string problem = level.GetProblem();
            if (problem != null)
            {
                Debug.LogError(level.name + " cannot be played: " + problem, level);
                return;
            }

            _machine?.Stop(); // leave the old level's state before building the new one
            _levelIndex = index;

            int usedSeed = level.UseRandomSeed ? Environment.TickCount : level.Seed;
            TileColor[] colors = new TileColor[level.ColorCount];
            Array.Copy((TileColor[])Enum.GetValues(typeof(TileColor)), colors, level.ColorCount);

            SystemRandom random = new SystemRandom(usedSeed);
            _board = new BoardGenerator(random).Generate(level.Width, level.Height, colors);
            BoardResolver resolver = new BoardResolver(random, colors);
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());

            MoveCounter moves = new MoveCounter(level.MoveLimit);
            _goalTracker = new GoalTracker(level.Goals);

            boardView.Build(_board);

            // Build the states from the end of the flow to the start, so each one can be given what it hands over to.
            _machine = new GameStateMachine();
            ResolvingState resolving = new ResolvingState(_machine, _board, moveFinder, moves, _goalTracker);
            SwappingState swapping = new SwappingState(_machine, _board, resolver, stepPlayer, moves, resolving);
            IdleState idle = new IdleState(_machine, swipeInput, swapping);

            _machine.Register(idle);
            _machine.Register(swapping);
            _machine.Register(resolving);
            _machine.Register(new WinState());
            _machine.Register(new LoseState());

            _hud.Show(index + 1, moves.MoveLimit, level.Goals);
            _endScreen.Hide();

            moves.MovesChanged += _hud.SetMoves;
            _goalTracker.GoalChanged += _hud.SetGoalRemaining;
            moves.MovesChanged += OnMovesChanged;
            _goalTracker.GoalChanged += OnGoalChanged;
            _machine.StateChanged += OnStateChanged;
            resolving.NoPossibleMoves += OnNoPossibleMoves;

            if (logToConsole) Debug.Log("Level " + (index + 1) + ": " + level.MoveLimit + " moves, " + level.Goals.Count + " goal(s).");

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

        private void OnStateChanged(IGameState previous, IGameState next)
        {
            if (next is WinState) _endScreen.ShowWin(HasNextLevel);
            else if (next is LoseState) _endScreen.ShowLose();

            if (!logToConsole) return;
            Debug.Log("State: " + (previous == null ? "(none)" : previous.GetType().Name) + " -> " + next.GetType().Name);
        }

        private void OnMovesChanged(int movesLeft)
        {
            if (logToConsole) Debug.Log("Moves left: " + movesLeft);
        }

        private void OnGoalChanged(int goalIndex, int remaining)
        {
            if (logToConsole) Debug.Log("Goal " + _goalTracker.GetColor(goalIndex) + ": " + remaining + " left");
        }

        private void OnNoPossibleMoves()
        {
            Debug.LogWarning("No possible moves left. Shuffling arrives in M8; for now right-click this component and choose Restart.");
        }
    }
}
