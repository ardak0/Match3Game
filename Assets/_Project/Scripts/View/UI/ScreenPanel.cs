using DG.Tweening;
using Match3.Data;
using UnityEngine;

namespace Match3.View.UI
{
    /// <summary>
    /// A full-screen page of the Home scene (the home screen, the level map). It fades and slides in and out,
    /// and its Content rect sits inside the safe area, so everything placed in Content is clear of notches.
    /// While a page is hidden it is inactive, so it costs nothing and cannot be tapped.
    /// </summary>
    public sealed class ScreenPanel : MonoBehaviour
    {
        private CanvasGroup _group;
        private RectTransform _rect;
        private FeelSettings _feel;
        private Tween _tween;

        /// <summary>Put the page's UI inside this rect.</summary>
        public RectTransform Content { get; private set; }

        public static ScreenPanel Create(Transform canvas, string name, FeelSettings feel)
        {
            RectTransform rect = UiFactory.CreateRect(canvas, name);
            UiFactory.Stretch(rect);

            ScreenPanel panel = rect.gameObject.AddComponent<ScreenPanel>();
            panel._rect = rect;
            panel._feel = feel;
            panel._group = rect.gameObject.AddComponent<CanvasGroup>();
            panel.Content = UiFactory.CreateSafeArea(rect);
            panel.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>Fades in while sliding in from a side: -1 = from the left, +1 = from the right, 0 = no slide.</summary>
        public void Show(float fromDirection)
        {
            _tween?.Kill();
            gameObject.SetActive(true);
            _group.interactable = false; // taps are accepted once the page has arrived
            _group.blocksRaycasts = true;

            float slide = fromDirection * _feel.ScreenSlideDistance;
            _tween = DOVirtual.Float(0f, 1f, _feel.ScreenSwitchSeconds, t =>
                {
                    _group.alpha = t;
                    _rect.anchoredPosition = new Vector2((1f - t) * slide, 0f);
                })
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => _group.interactable = true);
        }

        /// <summary>Fades out while sliding out to a side, then deactivates the page.</summary>
        public void Hide(float toDirection)
        {
            if (!gameObject.activeSelf) return;

            _tween?.Kill();
            _group.interactable = false;

            float slide = toDirection * _feel.ScreenSlideDistance;
            _tween = DOVirtual.Float(0f, 1f, _feel.ScreenSwitchSeconds, t =>
                {
                    _group.alpha = 1f - t;
                    _rect.anchoredPosition = new Vector2(t * slide, 0f);
                })
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => gameObject.SetActive(false));
        }

        /// <summary>Visible at once, no animation. Used for the page the scene starts on.</summary>
        public void ShowImmediately()
        {
            _tween?.Kill();
            gameObject.SetActive(true);
            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
            _rect.anchoredPosition = Vector2.zero;
        }
    }
}
