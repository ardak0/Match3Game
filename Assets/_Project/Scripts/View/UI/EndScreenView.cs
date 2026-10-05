using System;
using DG.Tweening;
using Match3.Data;
using TMPro;
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
        private static readonly Color CardColor = new Color(0.17f, 0.22f, 0.4f, 1f);
        private static readonly Color WinTitleColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color LoseTitleColor = new Color(1f, 0.5f, 0.45f);

        private GameObject _panel;
        private CanvasGroup _group;
        private UiStyle _style;
        private FeelSettings _feel;
        private RectTransform _card;
        private TMP_Text _title;
        private Button _nextButton;
        private TMP_Text _nextLabel;
        private Button _retryButton;
        private Tween _intro;

        /// <summary>The player clicked Retry.</summary>
        public event Action RetryClicked;

        /// <summary>The player clicked Next Level (or Play Again after the last level).</summary>
        public event Action NextClicked;

        /// <summary>Builds the end screen (hidden) as a new object, and makes sure the scene has an EventSystem for the buttons.</summary>
        public static EndScreenView Create(UiStyle style, FeelSettings feel)
        {
            EndScreenView screen = new GameObject("End Screen").AddComponent<EndScreenView>();
            screen.Build(style, feel);
            return screen;
        }

        private void Build(UiStyle style, FeelSettings feel)
        {
            _style = style;
            _feel = feel;
            UiFactory.EnsureEventSystem();
            Canvas canvas = UiFactory.CreateCanvas(transform, "End Screen Canvas", 20);

            // The dimmed full-screen panel also swallows clicks, so nothing underneath can be touched.
            Image dim = UiFactory.CreateImage(canvas.transform, "Panel", DimColor);
            UiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            _panel = dim.gameObject;
            _group = _panel.AddComponent<CanvasGroup>();

            // A card behind the title and the buttons: it pops in first, the rest follows.
            Image card = UiFactory.CreateImage(_panel.transform, "Card", CardColor, _style.PanelSprite);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(880f, 900f));
            _card = card.rectTransform;

            _title = UiFactory.CreateText(_panel.transform, "Title", "", 100f, Color.white, TextAlignmentOptions.Center, _style.Font);
            UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000f, 200f));

            Vector2 buttonSize = new Vector2(620f, 150f);
            _nextButton = UiFactory.CreateButton(_panel.transform, "Next", "Next Level", buttonSize, _style.PrimaryButtonSprite, _style.Font, _feel);
            _nextLabel = _nextButton.GetComponentInChildren<TMP_Text>();
            _retryButton = UiFactory.CreateButton(_panel.transform, "Retry", "Retry", buttonSize, _style.SecondaryButtonSprite, _style.Font, _feel);

            _nextButton.onClick.AddListener(() => NextClicked?.Invoke());
            _retryButton.onClick.AddListener(() => RetryClicked?.Invoke());

            _panel.SetActive(false);
        }

        /// <summary>Level won. hasNextLevel false (the last level) turns the Next button into "Play Again".</summary>
        public void ShowWin(bool hasNextLevel)
        {
            _title.text = hasNextLevel ? "Level Complete!" : "All Levels Complete!";
            _title.color = WinTitleColor;
            _nextLabel.text = hasNextLevel ? "Next Level" : "Play Again";

            _nextButton.gameObject.SetActive(true);
            UiFactory.Place((RectTransform)_nextButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 150f));
            UiFactory.Place((RectTransform)_retryButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(620f, 150f));
            PlayIntro();
        }

        /// <summary>Out of moves: only Retry.</summary>
        public void ShowLose()
        {
            _title.text = "Out of Moves";
            _title.color = LoseTitleColor;

            _nextButton.gameObject.SetActive(false);
            UiFactory.Place((RectTransform)_retryButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 150f));
            PlayIntro();
        }

        public void Hide()
        {
            _intro?.Kill();
            _panel.SetActive(false);
        }

        // The panel fades in while the card pops up, then the title and the buttons pop in one after another.
        // The scales are set to zero here and tweened to 1, so every showing starts the same way, even after an earlier one was cut short.
        private void PlayIntro()
        {
            _intro?.Kill();
            _group.alpha = 0f;
            _panel.SetActive(true);

            float seconds = _feel.EndPanelSeconds;
            float stagger = _feel.EndStaggerSeconds;

            _card.localScale = Vector3.one * 0.6f;
            _title.transform.localScale = Vector3.zero;
            _nextButton.transform.localScale = Vector3.zero;
            _retryButton.transform.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOVirtual.Float(0f, 1f, _feel.EndFadeSeconds, value => _group.alpha = value));
            sequence.Join(_card.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.Insert(stagger, _title.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.Insert(2f * stagger, _nextButton.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.Insert(3f * stagger, _retryButton.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.SetLink(gameObject);
            _intro = sequence;
        }
    }
}
