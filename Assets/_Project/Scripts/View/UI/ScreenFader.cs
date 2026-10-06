using DG.Tweening;
using Match3.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// Fades the screen to black, loads the next scene in the background, and (in the new scene) fades back in.
    /// Every scene creates its own fader that starts fully black, so the black of the old scene
    /// continues straight into the black of the new one and the player never sees a flash.
    /// It uses unscaled time, so it also works while Time.timeScale is 0.
    /// </summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        private CanvasGroup _group;
        private FeelSettings _feel;
        private Tween _tween;
        private bool _loading;

        /// <summary>Builds the fader (a black canvas on top of everything) and starts fading in from black.</summary>
        public static ScreenFader Create(FeelSettings feel)
        {
            ScreenFader fader = new GameObject("Screen Fader").AddComponent<ScreenFader>();
            fader.Build(feel);
            return fader;
        }

        private void Build(FeelSettings feel)
        {
            _feel = feel;
            Canvas canvas = UiFactory.CreateCanvas(transform, "Fader Canvas", 100);

            Image black = UiFactory.CreateImage(canvas.transform, "Black", Color.black);
            UiFactory.Stretch(black.rectTransform);
            black.raycastTarget = true; // while it is visible it also swallows clicks

            _group = black.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;

            FadeTo(0f, null);
        }

        /// <summary>Fades to black, then starts loading the scene without freezing the screen. Ignored if a load already started.</summary>
        public void LoadScene(string sceneName)
        {
            if (_loading) return;
            _loading = true;

            FadeTo(1f, () => SceneManager.LoadSceneAsync(sceneName));
        }

        private void FadeTo(float targetAlpha, System.Action onComplete)
        {
            _tween?.Kill();
            _group.blocksRaycasts = true;

            _tween = DOVirtual.Float(_group.alpha, targetAlpha, _feel.SceneFadeSeconds, value => _group.alpha = value)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _group.blocksRaycasts = targetAlpha > 0f;
                    onComplete?.Invoke();
                });
        }
    }
}
