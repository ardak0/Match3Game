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
    /// The small panel opened by the pause button: the star requirements of the level, Resume and Home.
    /// It only shows buttons and raises events; LevelController stops and restarts the game.
    /// The game is paused with Time.timeScale = 0, so this panel animates with unscaled time.
    /// </summary>
    public sealed class PausePanelView : MonoBehaviour
    {
        private static readonly Color DimColor = new Color(0.03f, 0.04f, 0.08f, 0.8f);
        private static readonly Color CardColor = new Color(0.17f, 0.22f, 0.4f, 1f);

        private GameObject _panel;
        private CanvasGroup _group;
        private RectTransform _card;
        private TMP_Text _rules;
        private FeelSettings _feel;
        private Tween _tween;

        public event Action ResumeClicked;
        public event Action HomeClicked;

        public bool IsOpen => _panel.activeSelf;

        public static PausePanelView Create(UiStyle style, FeelSettings feel)
        {
            PausePanelView view = new GameObject("Pause Panel").AddComponent<PausePanelView>();
            view.Build(style, feel);
            return view;
        }

        private void Build(UiStyle style, FeelSettings feel)
        {
            _feel = feel;
            UiFactory.EnsureEventSystem();
            Canvas canvas = UiFactory.CreateCanvas(transform, "Pause Canvas", 30); // above the HUD (10) and the end screen (20)

            Image dim = UiFactory.CreateImage(canvas.transform, "Panel", DimColor);
            UiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true; // nothing under the panel can be touched
            _panel = dim.gameObject;
            _group = _panel.AddComponent<CanvasGroup>();

            Image card = UiFactory.CreateImage(_panel.transform, "Card", CardColor, style.PanelSprite);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 900f));
            _card = card.rectTransform;

            // The title and buttons are children of the card, so they pop in together with it.
            TMP_Text title = UiFactory.CreateText(_card, "Title", "Paused", 100f, Color.white, TextAlignmentOptions.Center, style.Font);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 310f), new Vector2(700f, 140f));

            // The star rules of the current level; the text is filled in by Show.
            _rules = UiFactory.CreateText(_card, "Star Rules", "", 52f, Color.white, TextAlignmentOptions.Center, style.Font);
            UiFactory.Place(_rules.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(700f, 270f));

            Vector2 buttonSize = new Vector2(560f, 140f);
            Button resume = UiFactory.CreateButton(_card, "Resume", "Resume", buttonSize, style.PrimaryButtonSprite, style.Font, feel);
            UiFactory.Place((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), buttonSize);
            Button home = UiFactory.CreateButton(_card, "Home", "Home", buttonSize, style.SecondaryButtonSprite, style.Font, feel);
            UiFactory.Place((RectTransform)home.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), buttonSize);

            resume.onClick.AddListener(() => ResumeClicked?.Invoke());
            home.onClick.AddListener(() => HomeClicked?.Invoke());

            _panel.SetActive(false);
        }

        /// <summary>Opens the panel. The thresholds and the moves left tell the player what each star needs and where they stand now.</summary>
        public void Show(StarThresholds thresholds, int movesLeft)
        {
            _rules.text = DescribeRules(thresholds, movesLeft);

            _tween?.Kill();
            _panel.SetActive(true);
            _group.alpha = 0f;
            _card.localScale = Vector3.one * 0.8f;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOVirtual.Float(0f, 1f, _feel.ScreenSwitchSeconds, value => _group.alpha = value));
            sequence.Join(_card.DOScale(1f, _feel.ScreenSwitchSeconds).SetEase(Ease.OutBack));
            sequence.SetUpdate(true).SetLink(gameObject);
            _tween = sequence;
        }

        private static string DescribeRules(StarThresholds thresholds, int movesLeft)
        {
            string now = movesLeft == 1 ? "1 move left" : movesLeft + " moves left";
            return "1 star: win the level\n"
                + "2 stars: " + thresholds.TwoStarMovesLeft + "+ moves left\n"
                + "3 stars: " + thresholds.ThreeStarMovesLeft + "+ moves left\n"
                + "<color=#FFD84A>Now: " + now + "</color>";
        }

        /// <summary>Closes at once (a panel that fades out slowly would feel like a delay after tapping Resume).</summary>
        public void Hide()
        {
            _tween?.Kill();
            _panel.SetActive(false);
        }
    }
}
