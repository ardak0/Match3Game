using UnityEngine;

namespace Match3.View.UI
{
    /// <summary>
    /// Keeps a UI rect inside the "safe area" of the screen: the part not covered by a notch,
    /// a camera hole, rounded corners or the system gesture bar.
    /// Put it on a RectTransform that fills its canvas; everything placed inside that rect is then safe.
    ///
    /// How: Screen.safeArea is a rectangle in pixels. Dividing it by the screen size gives anchors (0..1),
    /// and a rect with those anchors and zero offsets covers exactly the safe area, at any canvas scale.
    ///
    /// No Update: the anchors are set when the object is enabled and whenever its size changes
    /// (rotation or a new resolution), which is when the safe area can change.
    /// In the Editor Game view the safe area is the whole screen; use the Device Simulator to see a notch.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void Apply()
        {
            if (_rect == null) _rect = (RectTransform)transform;

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            // Nothing changed since the last time: skip (this callback can fire often).
            if (safeArea == _lastSafeArea && screenSize == _lastScreenSize) return;
            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            _rect.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            _rect.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
