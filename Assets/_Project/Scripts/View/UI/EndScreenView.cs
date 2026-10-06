using System;
using DG.Tweening;
using Match3.Data;
using Match3.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// The screen shown when a level ends.
    /// Win: "Level Complete!", the stars earned (animated in), a "New Best!" tag if this run beat the saved stars,
    /// and Next / Retry / Home. Lose: "Out of Moves" with Retry / Home.
    /// It only shows things and raises events when buttons are clicked; LevelController decides what happens next.
    /// </summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        private static readonly Color DimColor = new Color(0.03f, 0.04f, 0.08f, 0.82f);
        private static readonly Color CardColor = new Color(0.17f, 0.22f, 0.4f, 1f);
        private static readonly Color WinTitleColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color LoseTitleColor = new Color(1f, 0.5f, 0.45f);
        private static readonly Color NewBestColor = new Color(0.55f, 1f, 0.6f);

        private const float CardCenterY = 20f;
        private const float ButtonHeight = 140f;
        private const float ButtonStep = 170f; // button height plus the gap under it
        private static readonly Vector2 ButtonSize = new Vector2(620f, ButtonHeight);

        private GameObject _panel;
        private CanvasGroup _group;
        private UiStyle _style;
        private FeelSettings _feel;
        private RectTransform _card;
        private TMP_Text _title;
        private StarRow _stars;
        private TMP_Text _newBest;
        private TMP_Text _starCaption;
        private Button _nextButton;
        private TMP_Text _nextLabel;
        private Button _retryButton;
        private Button _homeButton;
        private Tween _intro;
        private Tween _newBestPulse;

        /// <summary>The player clicked Retry.</summary>
        public event Action RetryClicked;

        /// <summary>The player clicked Next Level.</summary>
        public event Action NextClicked;

        /// <summary>The player clicked Home.</summary>
        public event Action HomeClicked;

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

            // A card behind everything else: it pops in first, the rest follows. Its height is set per showing.
            Image card = UiFactory.CreateImage(_panel.transform, "Card", CardColor, _style.PanelSprite);
            _card = card.rectTransform;

            _title = UiFactory.CreateText(_panel.transform, "Title", "", 100f, Color.white, TextAlignmentOptions.Center, _style.Font);
            _title.rectTransform.sizeDelta = new Vector2(1000f, 140f);

            _stars = StarRow.Create(_panel.transform, "Stars", _style, 180f, 215f, 50f);

            _starCaption = UiFactory.CreateText(_panel.transform, "Star Caption", "", 44f, new Color(1f, 1f, 1f, 0.75f), TextAlignmentOptions.Center, _style.Font);
            _starCaption.rectTransform.sizeDelta = new Vector2(800f, 120f);

            _newBest = UiFactory.CreateText(_panel.transform, "New Best", "New Best!", 64f, NewBestColor, TextAlignmentOptions.Center, _style.Font);
            _newBest.rectTransform.sizeDelta = new Vector2(600f, 80f);

            _nextButton = UiFactory.CreateButton(_panel.transform, "Next", "Next Level", ButtonSize, _style.PrimaryButtonSprite, _style.Font, _feel);
            _nextLabel = _nextButton.GetComponentInChildren<TMP_Text>();
            _retryButton = UiFactory.CreateButton(_panel.transform, "Retry", "Retry", ButtonSize, _style.SecondaryButtonSprite, _style.Font, _feel);
            _homeButton = UiFactory.CreateButton(_panel.transform, "Home", "Home", ButtonSize, _style.SecondaryButtonSprite, _style.Font, _feel);

            _nextButton.onClick.AddListener(() => NextClicked?.Invoke());
            _retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
            _homeButton.onClick.AddListener(() => HomeClicked?.Invoke());

            _panel.SetActive(false);
        }

        /// <summary>
        /// Level won. The result says how many stars this run earned and whether that beats the saved best.
        /// The caption under the stars says what they are for: the moves left, and what the next star needs.
        /// hasNextLevel false (the last level) hides the Next button and says all levels are complete.
        /// </summary>
        public void ShowWin(WinResult result, int movesLeft, StarThresholds thresholds, bool hasNextLevel)
        {
            _title.text = hasNextLevel ? "Level Complete!" : "All Levels Complete!";
            _title.color = WinTitleColor;
            _stars.SetEarned(result.Stars);
            _starCaption.text = DescribeStars(result.Stars, movesLeft, thresholds);
            _newBest.gameObject.SetActive(result.IsNewBest);
            _nextLabel.text = "Next Level";

            Layout(true, hasNextLevel);
            PlayIntro(result.Stars, result.IsNewBest);
        }

        /// <summary>Out of moves: Retry and Home.</summary>
        public void ShowLose()
        {
            _title.text = "Out of Moves";
            _title.color = LoseTitleColor;
            _stars.Root.gameObject.SetActive(false);
            _starCaption.gameObject.SetActive(false);
            _newBest.gameObject.SetActive(false);

            Layout(false, false);
            PlayIntro(0, false);
        }

        public void Hide()
        {
            _intro?.Kill();
            _newBestPulse?.Kill();
            _panel.SetActive(false);
        }

        // "7 moves left" on the first line; on the second, what the next star needs (or that there is none).
        private static string DescribeStars(int stars, int movesLeft, StarThresholds thresholds)
        {
            string movesLine = movesLeft == 1 ? "1 move left" : movesLeft + " moves left";

            if (stars >= StarThresholds.MaxStars) return movesLine + "\nBest possible!";

            int needed = stars == 1 ? thresholds.TwoStarMovesLeft : thresholds.ThreeStarMovesLeft;
            return movesLine + "\nWin with " + needed + "+ moves left for " + (stars + 1) + " stars";
        }

        // Stacks the parts from the top of the card downwards and sizes the card to fit,
        // so a win with three buttons and a loss with two buttons both look balanced.
        private void Layout(bool showStars, bool showNext)
        {
            int buttonCount = showNext ? 3 : 2;
            float starsBlock = showStars ? 380f : 0f; // stars (about 230), the caption (about 80) and the "New Best!" line (about 70)
            float cardHeight = 40f + 150f + starsBlock + buttonCount * ButtonStep + 20f;
            float y = CardCenterY + cardHeight / 2f - 40f; // the running position, moving down as parts are placed

            Vector2 center = new Vector2(0.5f, 0.5f);
            UiFactory.Place(_card, center, new Vector2(0f, CardCenterY), new Vector2(880f, cardHeight));

            UiFactory.Place(_title.rectTransform, center, new Vector2(0f, y - 70f), _title.rectTransform.sizeDelta);
            y -= 150f;

            _stars.Root.gameObject.SetActive(showStars);
            _starCaption.gameObject.SetActive(showStars);
            if (showStars)
            {
                UiFactory.Place(_stars.Root, center, new Vector2(0f, y - 115f), _stars.Root.sizeDelta);
                UiFactory.Place(_starCaption.rectTransform, center, new Vector2(0f, y - 270f), _starCaption.rectTransform.sizeDelta);
                UiFactory.Place(_newBest.rectTransform, center, new Vector2(0f, y - 345f), _newBest.rectTransform.sizeDelta);
                y -= starsBlock;
            }

            _nextButton.gameObject.SetActive(showNext);
            if (showNext)
            {
                UiFactory.Place((RectTransform)_nextButton.transform, center, new Vector2(0f, y - ButtonHeight / 2f), ButtonSize);
                y -= ButtonStep;
            }

            UiFactory.Place((RectTransform)_retryButton.transform, center, new Vector2(0f, y - ButtonHeight / 2f), ButtonSize);
            y -= ButtonStep;
            UiFactory.Place((RectTransform)_homeButton.transform, center, new Vector2(0f, y - ButtonHeight / 2f), ButtonSize);
        }

        // The panel fades in while the card pops up, then the title, the stars and the buttons pop in one after another.
        // The scales are set to zero here and tweened to 1, so every showing starts the same way, even after an earlier one was cut short.
        private void PlayIntro(int earnedStars, bool isNewBest)
        {
            _intro?.Kill();
            _newBestPulse?.Kill();
            _group.alpha = 0f;
            _panel.SetActive(true);

            float seconds = _feel.EndPanelSeconds;
            float stagger = _feel.EndStaggerSeconds;

            _card.localScale = Vector3.one * 0.6f;
            _title.transform.localScale = Vector3.zero;
            _stars.Root.localScale = Vector3.zero;
            _newBest.transform.localScale = Vector3.zero;
            _starCaption.transform.localScale = Vector3.zero;
            _nextButton.transform.localScale = Vector3.zero;
            _retryButton.transform.localScale = Vector3.zero;
            _homeButton.transform.localScale = Vector3.zero;
            for (int i = 0; i < earnedStars; i++) _stars.GetEarnedStar(i).localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOVirtual.Float(0f, 1f, _feel.EndFadeSeconds, value => _group.alpha = value));
            sequence.Join(_card.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.Insert(stagger, _title.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.Insert(2f * stagger, _stars.Root.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.Insert(2f * stagger, _starCaption.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));

            // The gold stars pop in one by one on top of the dark slots.
            float starsStart = 3f * stagger;
            for (int i = 0; i < earnedStars; i++)
            {
                sequence.Insert(starsStart + i * _feel.StarStaggerSeconds,
                    _stars.GetEarnedStar(i).DOScale(1f, _feel.StarPopSeconds).SetEase(Ease.OutBack));
            }

            if (isNewBest)
            {
                float newBestStart = starsStart + earnedStars * _feel.StarStaggerSeconds;
                sequence.Insert(newBestStart, _newBest.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));
                sequence.InsertCallback(newBestStart + seconds, StartNewBestPulse);
            }

            // The buttons do not wait for the stars: the player can already tap Next while the stars are still popping.
            sequence.Insert(2f * stagger, _nextButton.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.Insert(3f * stagger, _retryButton.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.Insert(4f * stagger, _homeButton.transform.DOScale(1f, seconds).SetEase(Ease.OutBack));
            sequence.SetLink(gameObject);
            _intro = sequence;
        }

        // "New Best!" breathes slowly until the screen is closed.
        private void StartNewBestPulse()
        {
            _newBestPulse = _newBest.transform.DOScale(_feel.NewBestPulseScale, _feel.NewBestPulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }
    }
}
