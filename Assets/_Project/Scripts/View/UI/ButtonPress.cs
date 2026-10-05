using DG.Tweening;
using Match3.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Match3.View.UI
{
    /// <summary>
    /// Makes a button shrink a little while it is held down and spring back when it is released, so a press feels physical.
    /// It only changes the button's size; the click itself is still handled by the Button component.
    /// </summary>
    public sealed class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private FeelSettings _feel;
        private Tween _tween;
        private bool _pressed;

        public void Init(FeelSettings feel)
        {
            _feel = feel;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            ScaleTo(_feel.ButtonPressedScale, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        // A finger that slides off the button cancels the press (and the click), so the button grows back too.
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!_pressed) return;

            _pressed = false;
            ScaleTo(1f, Ease.OutBack);
        }

        private void ScaleTo(float scale, Ease ease)
        {
            _tween?.Kill();
            _tween = transform.DOScale(scale, _feel.ButtonPressSeconds).SetEase(ease).SetLink(gameObject);
        }

        // A hidden button must not stay small, and no tween may keep running on it.
        private void OnDisable()
        {
            _tween?.Kill();
            _pressed = false;
            transform.localScale = Vector3.one;
        }
    }
}
