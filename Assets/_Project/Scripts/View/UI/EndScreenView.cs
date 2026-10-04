using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// The screen shown when a level ends: "Level Complete!" with Next and Retry, or "Out of Moves" with Retry.
    /// It only shows buttons and raises events when they are clicked; LevelController decides what happens next.
    /// </summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        private static readonly Color DimColor = new Color(0.03f, 0.04f, 0.08f, 0.82f);
        private static readonly Color PrimaryButtonColor = new Color(0.30f, 0.72f, 0.40f);
        private static readonly Color SecondaryButtonColor = new Color(0.35f, 0.42f, 0.62f);

        private const float FadeSeconds = 0.25f;

        private GameObject _panel;
        private CanvasGroup _group;
        private Text _title;
        private Button _nextButton;
        private Text _nextLabel;
        private Button _retryButton;
        private Tween _fade;

        /// <summary>The player clicked Retry.</summary>
        public event Action RetryClicked;

        /// <summary>The player clicked Next Level (or Play Again after the last level).</summary>
        public event Action NextClicked;

        /// <summary>Builds the end screen (hidden) as a new object, and makes sure the scene has an EventSystem for the buttons.</summary>
        public static EndScreenView Create()
        {
            EndScreenView screen = new GameObject("End Screen").AddComponent<EndScreenView>();
            screen.Build();
            return screen;
        }

        private void Build()
        {
            UiFactory.EnsureEventSystem();
            Canvas canvas = UiFactory.CreateCanvas(transform, "End Screen Canvas", 20);

            // The dimmed full-screen panel also swallows clicks, so nothing underneath can be touched.
            Image dim = UiFactory.CreateImage(canvas.transform, "Panel", DimColor);
            UiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            _panel = dim.gameObject;
            _group = _panel.AddComponent<CanvasGroup>();

            _title = UiFactory.CreateText(_panel.transform, "Title", "", 100, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000f, 200f));

            Vector2 buttonSize = new Vector2(620f, 150f);
            _nextButton = UiFactory.CreateButton(_panel.transform, "Next", "Next Level", buttonSize, PrimaryButtonColor);
            _nextLabel = _nextButton.GetComponentInChildren<Text>();
            _retryButton = UiFactory.CreateButton(_panel.transform, "Retry", "Retry", buttonSize, SecondaryButtonColor);

            _nextButton.onClick.AddListener(() => NextClicked?.Invoke());
            _retryButton.onClick.AddListener(() => RetryClicked?.Invoke());

            _panel.SetActive(false);
        }

        /// <summary>Level won. hasNextLevel false (the last level) turns the Next button into "Play Again".</summary>
        public void ShowWin(bool hasNextLevel)
        {
            _title.text = hasNextLevel ? "Level Complete!" : "All Levels Complete!";
            _nextLabel.text = hasNextLevel ? "Next Level" : "Play Again";

            _nextButton.gameObject.SetActive(true);
            UiFactory.Place((RectTransform)_nextButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 150f));
            UiFactory.Place((RectTransform)_retryButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(620f, 150f));
            FadeIn();
        }

        /// <summary>Out of moves: only Retry.</summary>
        public void ShowLose()
        {
            _title.text = "Out of Moves";

            _nextButton.gameObject.SetActive(false);
            UiFactory.Place((RectTransform)_retryButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 150f));
            FadeIn();
        }

        public void Hide()
        {
            _fade?.Kill();
            _panel.SetActive(false);
        }

        private void FadeIn()
        {
            _fade?.Kill();
            _group.alpha = 0f;
            _panel.SetActive(true);
            _fade = DOVirtual.Float(0f, 1f, FadeSeconds, value => _group.alpha = value).SetLink(gameObject);
        }
    }
}
