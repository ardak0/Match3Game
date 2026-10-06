using System;
using Match3.Core;
using Match3.Data;
using Match3.Infrastructure;
using Match3.View;
using Match3.View.UI;
using UnityEngine;

namespace Match3.Game
{
    /// <summary>
    /// Composition root of the Game scene: loads a level from the LevelCatalog, creates the model and the game state machine,
    /// and wires them to the view, the UI and the saved progress. It holds no game rules.
    ///
    /// Flow of information (all through C# events):
    ///   MoveCounter.MovesChanged  -> HudView.SetMoves
    ///   GoalTracker.GoalChanged   -> HudView.SetGoalRemaining
    ///   GameStateMachine.StateChanged (WinState) -> ProgressService.RecordWin -> EndScreenView.ShowWin (with the stars)
    ///   GameStateMachine.StateChanged (LoseState) -> EndScreenView.ShowLose
    ///   HudView.PauseClicked -> pause (Time.timeScale = 0, swiping off) -> PausePanelView
    ///   EndScreenView / PausePanelView buttons -> this class starts a level, resumes, or goes Home through the ScreenFader
    /// </summary>
    [RequireComponent(typeof(BoardView), typeof(StepPlayer), typeof(SwipeInput))]
    public sealed class LevelController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private StepPlayer stepPlayer;
        [SerializeField] private SwipeInput swipeInput;

        [Tooltip("Font, panel, button and icon sprites for the HUD and the end screen.")]
        [SerializeField] private UiStyle uiStyle;

        [Header("Levels")]
        [Tooltip("All levels in order. The same asset the Home scene uses (Assets/_Project/Data/LevelCatalog).")]
        [SerializeField] private LevelCatalog levelCatalog;

        [Tooltip("Which level the player picked on the level map. Optional: leave it empty to test this scene on its own.")]
        [SerializeField] private GameSession gameSession;

        [Tooltip("The level to play when the scene is opened directly (no level picked on the map). 0 = the first. Handy for testing a later level.")]
        [SerializeField, Min(0)] private int startLevelIndex;

        [Header("Debug")]
        [Tooltip("Write moves, goals and state changes to the Console. Off by default: the log strings allocate, which hides real garbage in the Profiler.")]
        [SerializeField] private bool logToConsole;

        private GameStateMachine _machine;
        private GoalTracker _goalTracker;
        private MoveCounter _moves;
        private StarThresholds _starThresholds;
        private Board _board;
        private HudView _hud;
        private EndScreenView _endScreen;
        private PausePanelView _pausePanel;
        private ScreenFader _fader;
        private ProgressService _progress;
        private int _levelIndex;
        private bool _inputWasEnabled;
        private bool _isLeaving;

        private bool HasNextLevel => _levelIndex + 1 < levelCatalog.Count;

        private void Reset()
        {
            // Editor-only convenience when the component is added.
            boardView = GetComponent<BoardView>();
            stepPlayer = GetComponent<StepPlayer>();
            swipeInput = GetComponent<SwipeInput>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60; // phones default to 30; the tile animations look better at 60

            if (uiStyle == null)
            {
                throw new InvalidOperationException("LevelController needs its Ui Style assigned in the Inspector (Assets/_Project/Data/UiStyle).");
            }

            if (boardView.Feel == null)
            {
                throw new InvalidOperationException("BoardView needs its Feel Settings assigned in the Inspector (Assets/_Project/Data/FeelSettings).");
            }

            if (levelCatalog == null)
            {
                throw new InvalidOperationException("LevelController needs its Level Catalog assigned in the Inspector (Assets/_Project/Data/LevelCatalog).");
            }

            string catalogProblem = levelCatalog.GetProblem();
            if (catalogProblem != null)
            {
                throw new InvalidOperationException("The Level Catalog has a problem: " + catalogProblem);
            }

            _progress = ProgressSetup.Create(levelCatalog);

            BackgroundView.Create(boardView.BoardCamera, uiStyle, boardView.Feel);

            _hud = HudView.Create(boardView.Visuals, uiStyle, boardView.Feel);
            stepPlayer.ComboReached += _hud.ShowCombo;
            _hud.PauseClicked += OnPauseClicked;

            _endScreen = EndScreenView.Create(uiStyle, boardView.Feel);
            _endScreen.RetryClicked += RetryLevel;
            _endScreen.NextClicked += GoToNextLevel;
            _endScreen.HomeClicked += GoHome;

            _pausePanel = PausePanelView.Create(uiStyle, boardView.Feel);
            _pausePanel.ResumeClicked += OnResumeClicked;
            _pausePanel.HomeClicked += GoHome;

            _fader = ScreenFader.Create(boardView.Feel); // starts black and fades in
        }

        private void Start()
        {
            StartLevel(GetFirstLevelIndex());
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f; // never leave the game paused behind us: the next scene would start frozen

            if (stepPlayer != null && _hud != null) stepPlayer.ComboReached -= _hud.ShowCombo;
            if (_hud != null) _hud.PauseClicked -= OnPauseClicked;

            if (_endScreen != null)
            {
                _endScreen.RetryClicked -= RetryLevel;
                _endScreen.NextClicked -= GoToNextLevel;
                _endScreen.HomeClicked -= GoHome;
            }

            if (_pausePanel != null)
            {
                _pausePanel.ResumeClicked -= OnResumeClicked;
                _pausePanel.HomeClicked -= GoHome;
            }

            _machine?.Stop(); // lets the current state unsubscribe from the swipe event
        }

        // The level picked on the map, or startLevelIndex when this scene was opened directly (for example with Play in the editor).
        private int GetFirstLevelIndex()
        {
            int index = gameSession != null && gameSession.HasSelection ? gameSession.SelectedLevelIndex : startLevelIndex;
            return Mathf.Clamp(index, 0, levelCatalog.Count - 1);
        }

        /// <summary>Starts the level again from a new board. Also available from the Inspector (right-click the header, "Restart").</summary>
        [ContextMenu("Restart")]
        public void RetryLevel()
        {
            StartLevel(_levelIndex);
        }

        /// <summary>The next level. Does nothing after the last one (the end screen has no Next button then).</summary>
        public void GoToNextLevel()
        {
            if (HasNextLevel) StartLevel(_levelIndex + 1);
        }

        public void StartLevel(int index)
        {
            if (stepPlayer.IsPlaying) return;

            if (index < 0 || index >= levelCatalog.Count)
            {
                Debug.LogError("There is no level " + index + ". The catalog has " + levelCatalog.Count + " levels (0 is the first).", this);
                return;
            }

            LevelData level = levelCatalog.Get(index);
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

            // GetProblem (above) already checked the rows, so the parse cannot fail here. No rows = an empty layout.
            ObstacleLayout.TryParse(level.ObstacleRows, level.Width, level.Height, out ObstacleLayout layout, out _);

            SystemRandom random = new SystemRandom(usedSeed);
            _board = new BoardGenerator(random).Generate(level.Width, level.Height, colors, layout);
            BoardResolver resolver = new BoardResolver(random, colors);
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            BoardShuffler shuffler = new BoardShuffler(random, moveFinder);

            _moves = new MoveCounter(level.MoveLimit);
            _starThresholds = level.Stars;
            _goalTracker = new GoalTracker(level.Goals);

            boardView.Build(_board);

            // Build the states from the end of the flow to the start, so each one can be given what it hands over to.
            _machine = new GameStateMachine();
            ResolvingState resolving = new ResolvingState(_machine, _board, moveFinder, _moves, _goalTracker);
            SwappingState swapping = new SwappingState(_machine, _board, resolver, stepPlayer, _moves, resolving);
            ShufflingState shuffling = new ShufflingState(_machine, _board, shuffler, stepPlayer);
            IdleState idle = new IdleState(_machine, swipeInput, swapping);

            _machine.Register(idle);
            _machine.Register(swapping);
            _machine.Register(resolving);
            _machine.Register(shuffling);
            _machine.Register(new WinState());
            _machine.Register(new LoseState());

            _hud.Show(index + 1, _moves.MoveLimit, level.Goals, _starThresholds);
            _endScreen.Hide();

            _moves.MovesChanged += _hud.SetMoves;
            _goalTracker.GoalChanged += _hud.SetGoalRemaining;
            _moves.MovesChanged += OnMovesChanged;
            _goalTracker.GoalChanged += OnGoalChanged;
            _machine.StateChanged += OnStateChanged;

            if (logToConsole) Debug.Log("Level " + (index + 1) + ": " + level.MoveLimit + " moves, " + level.Goals.Count + " goal(s).");

            _machine.ChangeTo<IdleState>();
        }

        /// <summary>
        /// Testing aid: turns tiles into special tiles so every special and combo can be tried without waiting for luck.
        /// Bomb at (2,2) and RocketHorizontal at (3,2) are neighbors (swap them for a combo); RocketVertical at (5,5) is on its own.
        /// ColorBombs: (5,4) sits under the RocketVertical (swap them: ColorBomb + rocket), and (0,0) and (1,0) are neighbors
        /// (swap them: the whole board clears; swap (0,0) with (0,1): ColorBomb + a normal tile).
        /// A tile keeps its color (a ColorBomb has none), so this never creates a match. Only works while the game waits for a swipe.
        /// </summary>
        [ContextMenu("Debug: Place Specials")]
        private void DebugPlaceSpecials()
        {
            if (_board == null || stepPlayer.IsPlaying || !_machine.IsIn<IdleState>()) return;

            MakeSpecial(2, 2, SpecialType.Bomb);
            MakeSpecial(3, 2, SpecialType.RocketHorizontal);
            MakeSpecial(5, 5, SpecialType.RocketVertical);
            MakeSpecial(6, 1, SpecialType.Bomb);
            MakeColorBomb(5, 4);
            MakeColorBomb(0, 0);
            MakeColorBomb(1, 0);

            boardView.Build(_board); // shows the changed board
        }

        private void MakeColorBomb(int x, int y)
        {
            if (!_board.IsInside(x, y)) return;

            if (_board.HasCrate(x, y)) return; // a crate cell holds no tile

            _board.Set(x, y, _board.NewTile(TileColor.None, SpecialType.ColorBomb)); // no color: it can never make a match
        }

        private void MakeSpecial(int x, int y, SpecialType special)
        {
            if (!_board.IsInside(x, y) || _board.Get(x, y) == null) return; // outside, or a crate cell

            _board.Set(x, y, _board.NewTile(_board.Get(x, y).Color, special));
        }

        // ---------- Pause and leaving ----------

        // Pausing stops the game with Time.timeScale = 0 (every tween and delay of the board freezes in place)
        // and turns swiping off. What swiping was before is remembered: while a move is still animating it was already off,
        // and Resume must not turn it on in the middle of that.
        private void OnPauseClicked()
        {
            if (_pausePanel.IsOpen || _isLeaving) return;
            if (_machine.IsIn<WinState>() || _machine.IsIn<LoseState>()) return; // the end screen is already up

            _inputWasEnabled = swipeInput.InputEnabled;
            swipeInput.InputEnabled = false;
            Time.timeScale = 0f;
            _pausePanel.Show(_starThresholds, _moves.MovesLeft);
        }

        private void OnResumeClicked()
        {
            _pausePanel.Hide();
            Time.timeScale = 1f;
            swipeInput.InputEnabled = _inputWasEnabled;
        }

        // Fades to black, then loads the Home scene. No progress changes: only a win saves anything.
        private void GoHome()
        {
            if (_isLeaving) return;
            _isLeaving = true;

            _pausePanel.Hide();
            Time.timeScale = 1f; // the fade would run on unscaled time anyway; the board animations may finish behind it
            swipeInput.InputEnabled = false;
            _fader.LoadScene(SceneNames.Home);
        }

        // ---------- Model events ----------

        private void OnStateChanged(IGameState previous, IGameState next)
        {
            if (next is WinState)
            {
                // The model has decided "won"; the progress service turns the moves that are left into stars and saves them.
                WinResult result = _progress.RecordWin(_levelIndex, _moves.MovesLeft);
                _endScreen.ShowWin(result, _moves.MovesLeft, _starThresholds, HasNextLevel);
            }
            else if (next is LoseState) _endScreen.ShowLose();
            else if (next is ShufflingState) _hud.ShowMessage("No moves left. Shuffling!", 1.4f);

            if (!logToConsole) return;
            Debug.Log("State: " + (previous == null ? "(none)" : previous.GetType().Name) + " -> " + next.GetType().Name);
        }

        private void OnMovesChanged(int movesLeft)
        {
            if (logToConsole) Debug.Log("Moves left: " + movesLeft);
        }

        private void OnGoalChanged(int goalIndex, int remaining)
        {
            if (!logToConsole) return;

            string what = _goalTracker.GetKind(goalIndex) == GoalKind.ClearObstacle
                ? _goalTracker.GetObstacle(goalIndex).ToString()
                : _goalTracker.GetColor(goalIndex).ToString();
            Debug.Log("Goal " + what + ": " + remaining + " left");
        }
    }
}
