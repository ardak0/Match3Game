using System;
using Match3.Data;
using Match3.Infrastructure;
using Match3.View.UI;
using UnityEngine;

namespace Match3.Game
{
    /// <summary>
    /// Composition root of the Home scene (the counterpart of LevelController in the Game scene).
    /// It builds the UI in code, loads the saved progress, and connects the two pages:
    ///   HomeScreenView.PlayClicked    -> load the Game scene at ProgressService.NextLevelToPlay
    ///   HomeScreenView.LevelsClicked  -> open the level map
    ///   LevelMapView.LevelSelected    -> load the Game scene at that level
    ///   LevelMapView.BackClicked      -> back to the home page
    /// "Load the Game scene at level N" means: write N into the GameSession asset, then fade out and load the scene.
    /// The Game scene reads the number from the same asset.
    /// </summary>
    public sealed class HomeController : MonoBehaviour
    {
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private GameSession gameSession;
        [SerializeField] private UiStyle uiStyle;
        [SerializeField] private FeelSettings feelSettings;

        private ProgressService _progress;
        private HomeScreenView _home;
        private LevelMapView _map;
        private ScreenFader _fader;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            if (levelCatalog == null) throw new InvalidOperationException("HomeController needs its Level Catalog assigned in the Inspector (Assets/_Project/Data/LevelCatalog).");
            if (gameSession == null) throw new InvalidOperationException("HomeController needs its Game Session assigned in the Inspector (Assets/_Project/Data/GameSession).");
            if (uiStyle == null) throw new InvalidOperationException("HomeController needs its Ui Style assigned in the Inspector (Assets/_Project/Data/UiStyle).");
            if (feelSettings == null) throw new InvalidOperationException("HomeController needs its Feel Settings assigned in the Inspector (Assets/_Project/Data/FeelSettings).");

            string catalogProblem = levelCatalog.GetProblem();
            if (catalogProblem != null) throw new InvalidOperationException("The Level Catalog has a problem: " + catalogProblem);

            DOTweenSetup.Configure();
            _progress = ProgressSetup.Create(levelCatalog);
            gameSession.Clear(); // arriving at Home means no level is picked yet

            UiFactory.EnsureEventSystem();
            Canvas canvas = UiFactory.CreateCanvas(transform, "Home Canvas", 0);

            // The background canvas is drawn through the scene's only camera (this scene has no BoardView to ask).
            Camera sceneCamera = Camera.main;
            if (sceneCamera == null) throw new InvalidOperationException("The Home scene needs a camera tagged MainCamera.");
            BackgroundView.Create(sceneCamera, uiStyle, feelSettings);

            _home = HomeScreenView.Create(canvas.transform, uiStyle, feelSettings);
            _map = LevelMapView.Create(canvas.transform, uiStyle, feelSettings);
            _fader = ScreenFader.Create(feelSettings); // starts black and fades in

            _home.PlayClicked += OnPlayClicked;
            _home.LevelsClicked += OnLevelsClicked;
            _map.LevelSelected += OnLevelSelected;
            _map.BackClicked += OnMapBackClicked;

            _home.Refresh(_progress);
            _home.ShowImmediately();
        }

        private void OnDestroy()
        {
            if (_home != null)
            {
                _home.PlayClicked -= OnPlayClicked;
                _home.LevelsClicked -= OnLevelsClicked;
            }

            if (_map != null)
            {
                _map.LevelSelected -= OnLevelSelected;
                _map.BackClicked -= OnMapBackClicked;
            }
        }

        private void OnPlayClicked()
        {
            LoadGame(_progress.NextLevelToPlay);
        }

        private void OnLevelsClicked()
        {
            _home.Hide(-1f); // the home page leaves to the left, the map arrives from the right
            _map.Open(_progress, 1f);
        }

        private void OnMapBackClicked()
        {
            _map.Close(1f);
            _home.Refresh(_progress);
            _home.Show(-1f);
        }

        private void OnLevelSelected(int levelIndex)
        {
            if (!_progress.IsUnlocked(levelIndex)) return; // the map never sends a locked level; this is a second lock
            LoadGame(levelIndex);
        }

        private void LoadGame(int levelIndex)
        {
            gameSession.Select(levelIndex);
            _fader.LoadScene(SceneNames.Game);
        }
    }
}
