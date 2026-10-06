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
    /// The first page of the Home scene: the title, the total stars, Play (continues at the next level to play) and Levels.
    /// It only shows what it is told (Refresh) and raises events when buttons are clicked.
    /// </summary>
    public sealed class HomeScreenView : MonoBehaviour
    {
        private static readonly Color TitleColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color SoftTextColor = new Color(1f, 1f, 1f, 0.6f);
        private static readonly Color StarColor = new Color(1f, 0.82f, 0.25f);

        private ScreenPanel _panel;
        private TMP_Text _starsText;
        private TMP_Text _levelCaption;

        public event Action PlayClicked;
        public event Action LevelsClicked;

        public static HomeScreenView Create(Transform canvas, UiStyle style, FeelSettings feel)
        {
            HomeScreenView view = new GameObject("Home Screen View").AddComponent<HomeScreenView>();
            view.transform.SetParent(canvas, false);
            view.Build(canvas, style, feel);
            return view;
        }

        private void Build(Transform canvas, UiStyle style, FeelSettings feel)
        {
            _panel = ScreenPanel.Create(canvas, "Home Screen", feel);
            RectTransform content = _panel.Content;
            Vector2 center = new Vector2(0.5f, 0.5f);

            TMP_Text title = UiFactory.CreateText(content, "Title", "Match 3", 170f, TitleColor, TextAlignmentOptions.Center, style.Font);
            UiFactory.Place(title.rectTransform, center, new Vector2(0f, 520f), new Vector2(1000f, 220f));
            title.rectTransform.DOAnchorPosY(520f + 18f, 1.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(title.gameObject); // a slow bob

            // Total stars: a star icon and "earned / possible".
            RectTransform starsRow = UiFactory.CreateRect(content, "Total Stars");
            UiFactory.Place(starsRow, center, new Vector2(0f, 330f), new Vector2(600f, 120f));
            Image starIcon = UiFactory.CreateImage(starsRow, "Star", StarColor, style.StarSprite);
            starIcon.preserveAspect = true;
            UiFactory.Place(starIcon.rectTransform, center, new Vector2(-150f, 0f), new Vector2(110f, 110f));
            _starsText = UiFactory.CreateText(starsRow, "Stars Text", "0 / 0", 90f, Color.white, TextAlignmentOptions.Left, style.Font);
            UiFactory.Place(_starsText.rectTransform, center, new Vector2(110f, 0f), new Vector2(420f, 120f));

            // The Play button sits in a holder so the holder can pulse without fighting the button's own press animation.
            Vector2 playSize = new Vector2(700f, 200f);
            RectTransform playHolder = UiFactory.CreateRect(content, "Play Holder");
            UiFactory.Place(playHolder, center, new Vector2(0f, -20f), playSize);
            Button play = UiFactory.CreateButton(playHolder, "Play", "Play", playSize, style.PrimaryButtonSprite, style.Font, feel);
            UiFactory.Place((RectTransform)play.transform, center, Vector2.zero, playSize);
            play.GetComponentInChildren<TMP_Text>().fontSize = 100f;
            playHolder.DOScale(1.04f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(playHolder.gameObject);

            _levelCaption = UiFactory.CreateText(content, "Level Caption", "Level 1", 52f, SoftTextColor, TextAlignmentOptions.Center, style.Font);
            UiFactory.Place(_levelCaption.rectTransform, center, new Vector2(0f, -180f), new Vector2(700f, 70f));

            Vector2 levelsSize = new Vector2(700f, 170f);
            Button levels = UiFactory.CreateButton(content, "Levels", "Levels", levelsSize, style.SecondaryButtonSprite, style.Font, feel);
            UiFactory.Place((RectTransform)levels.transform, center, new Vector2(0f, -350f), levelsSize);

            play.onClick.AddListener(() => PlayClicked?.Invoke());
            levels.onClick.AddListener(() => LevelsClicked?.Invoke());
        }

        /// <summary>Updates the total stars and the "Level N" under the Play button from the saved progress.</summary>
        public void Refresh(ProgressService progress)
        {
            _starsText.text = progress.TotalStars + " / " + progress.MaxStars;

            int next = progress.NextLevelToPlay;
            bool replay = progress.GetStars(next) > 0; // every level is won, so Play replays the last one
            _levelCaption.text = (replay ? "Replay Level " : "Level ") + (next + 1);
        }

        public void Show(float fromDirection) => _panel.Show(fromDirection);

        public void Hide(float toDirection) => _panel.Hide(toDirection);

        public void ShowImmediately() => _panel.ShowImmediately();
    }
}
