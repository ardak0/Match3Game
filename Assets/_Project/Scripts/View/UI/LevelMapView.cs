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
    /// The level map: a scrollable vertical path with one node per level, level 1 at the bottom.
    /// A node is locked (gray, with a lock), open (green, no stars yet) or completed (blue, with 1 to 3 stars).
    /// The next level to play pulses. Tapping an open node raises LevelSelected; tapping a locked one only wobbles it.
    /// It only shows the progress it is given (Open); it never changes progress.
    /// </summary>
    public sealed class LevelMapView : MonoBehaviour
    {
        private static readonly Color LockedColor = new Color(0.55f, 0.58f, 0.66f, 1f);
        private static readonly Color PathDoneColor = new Color(1f, 0.85f, 0.3f, 0.9f);
        private static readonly Color PathTodoColor = new Color(1f, 1f, 1f, 0.25f);

        private const float HeaderHeight = 190f;
        private const float NodeSize = 150f;
        private const float NodeSpacing = 250f;   // vertical distance between two nodes
        private const float ZigZag = 250f;        // how far the path swings left and right
        private const float BottomPadding = 230f; // room under the first node for its stars
        private const float TopPadding = 200f;
        private const int DotsPerSegment = 4;
        private const float DotSize = 26f;

        private UiStyle _style;
        private FeelSettings _feel;
        private ScreenPanel _panel;
        private ScrollRect _scroll;
        private RectTransform _viewport;
        private RectTransform _content;
        private Tween _introTween;
        private Tween _pulseTween;
        private Tween _scrollTween;
        private Tween _wobbleTween;

        /// <summary>The player tapped an open level node (0 = the first level).</summary>
        public event Action<int> LevelSelected;

        public event Action BackClicked;

        public static LevelMapView Create(Transform canvas, UiStyle style, FeelSettings feel)
        {
            LevelMapView view = new GameObject("Level Map View").AddComponent<LevelMapView>();
            view.transform.SetParent(canvas, false);
            view.Build(canvas, style, feel);
            return view;
        }

        private void Build(Transform canvas, UiStyle style, FeelSettings feel)
        {
            _style = style;
            _feel = feel;
            _panel = ScreenPanel.Create(canvas, "Level Map", feel);
            RectTransform safe = _panel.Content;

            // Header: a Back button on the left and the title in the middle.
            Vector2 backSize = new Vector2(250f, 110f);
            Button back = UiFactory.CreateButton(safe, "Back", "Back", backSize, _style.SecondaryButtonSprite, _style.Font, _feel);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(160f, -95f), backSize);
            back.GetComponentInChildren<TMP_Text>().fontSize = 56f;
            back.onClick.AddListener(() => BackClicked?.Invoke());

            TMP_Text title = UiFactory.CreateText(safe, "Title", "Levels", 100f, Color.white, TextAlignmentOptions.Center, _style.Font);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(500f, 140f));

            // The scroll area fills everything under the header. The transparent image lets the empty parts of it catch drags.
            RectTransform scrollRect = UiFactory.CreateRect(safe, "Scroll");
            UiFactory.Stretch(scrollRect);
            scrollRect.offsetMax = new Vector2(0f, -HeaderHeight);
            Image catcher = scrollRect.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;

            _viewport = UiFactory.CreateRect(scrollRect, "Viewport");
            UiFactory.Stretch(_viewport);
            _viewport.gameObject.AddComponent<RectMask2D>();

            // The content is as wide as the viewport, hangs from its top edge, and is as tall as the whole path.
            _content = UiFactory.CreateRect(_viewport, "Content");
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;

            _scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = _viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.scrollSensitivity = 40f;
        }

        /// <summary>Slides the map in, builds the nodes from the progress, and scrolls to the level to play next.</summary>
        public void Open(ProgressService progress, float fromDirection)
        {
            _panel.Show(fromDirection);
            Rebuild(progress);
        }

        public void Close(float toDirection)
        {
            KillTweens();
            _panel.Hide(toDirection);
        }

        private void Rebuild(ProgressService progress)
        {
            KillTweens();

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject old = _content.GetChild(i).gameObject;
                old.SetActive(false); // Destroy only happens at the end of the frame, and the old node must not show until then
                Destroy(old);
            }

            int count = progress.LevelCount;
            float contentHeight = BottomPadding + (count - 1) * NodeSpacing + TopPadding;
            _content.sizeDelta = new Vector2(0f, contentHeight);

            // The dotted path goes first, so the nodes are drawn on top of it.
            for (int i = 0; i < count - 1; i++)
            {
                Color color = progress.GetStars(i) > 0 ? PathDoneColor : PathTodoColor; // gold once the way to the next level is open
                for (int d = 1; d <= DotsPerSegment; d++)
                {
                    float t = d / (float)(DotsPerSegment + 1);
                    Image dot = UiFactory.CreateImage(_content, "Dot", color, _style.DotSprite);
                    UiFactory.Place(dot.rectTransform, new Vector2(0.5f, 0f), Vector2.Lerp(NodePosition(i), NodePosition(i + 1), t), new Vector2(DotSize, DotSize));
                }
            }

            int current = progress.NextLevelToPlay;
            bool hasCurrent = progress.IsUnlocked(current) && progress.GetStars(current) == 0;
            RectTransform currentHolder = null;
            Sequence intro = DOTween.Sequence();

            for (int i = 0; i < count; i++)
            {
                RectTransform holder = CreateNode(i, progress);
                if (hasCurrent && i == current) currentHolder = holder;

                holder.localScale = Vector3.zero;
                intro.Insert(i * _feel.NodeIntroStaggerSeconds, holder.DOScale(1f, _feel.NodeIntroSeconds).SetEase(Ease.OutBack));
            }

            intro.SetLink(gameObject);
            if (currentHolder != null) intro.OnComplete(() => StartPulse(currentHolder)); // the pulse starts after the pop-in: both change the scale
            _introTween = intro;

            ScrollToLevel(hasCurrent ? current : Mathf.Max(0, current), contentHeight);
        }

        private RectTransform CreateNode(int index, ProgressService progress)
        {
            bool unlocked = progress.IsUnlocked(index);
            int stars = progress.GetStars(index);

            RectTransform holder = UiFactory.CreateRect(_content, "Node " + (index + 1));
            Vector2 size = new Vector2(NodeSize, NodeSize);
            UiFactory.Place(holder, new Vector2(0.5f, 0f), NodePosition(index), size);

            Sprite sprite = !unlocked ? _style.PanelSprite : stars > 0 ? _style.PrimaryButtonSprite : _style.SecondaryButtonSprite;
            Button button = UiFactory.CreateButton(holder, "Button", unlocked ? (index + 1).ToString() : "", size, sprite, _style.Font, _feel);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            ((Image)button.targetGraphic).color = unlocked ? Color.white : LockedColor;
            button.GetComponentInChildren<TMP_Text>().fontSize = 76f;

            if (!unlocked && _style.LockIconSprite != null)
            {
                Image lockIcon = UiFactory.CreateImage(button.transform, "Lock", Color.white, _style.LockIconSprite);
                lockIcon.preserveAspect = true;
                UiFactory.Place(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
            }

            if (unlocked)
            {
                StarRow row = StarRow.Create(holder, "Stars", _style, 46f, 52f, 10f);
                UiFactory.Place(row.Root, new Vector2(0.5f, 0.5f), new Vector2(0f, -(NodeSize / 2f + 38f)), row.Root.sizeDelta);
                row.SetEarned(stars);
            }

            button.onClick.AddListener(() => OnNodeClicked(index, unlocked, holder));
            return holder;
        }

        // The path swings left and right along a sine curve, bottom to top: level 1 is at the bottom.
        private static Vector2 NodePosition(int index)
        {
            return new Vector2(Mathf.Sin(index * 0.9f) * ZigZag, BottomPadding + index * NodeSpacing);
        }

        private void OnNodeClicked(int index, bool unlocked, RectTransform holder)
        {
            if (unlocked)
            {
                LevelSelected?.Invoke(index);
                return;
            }

            // Locked: a short wobble says "not yet".
            _wobbleTween?.Complete(); // finish the previous wobble so the node is straight again
            _wobbleTween = holder.DOPunchRotation(new Vector3(0f, 0f, _feel.LockedShakeStrength), _feel.LockedShakeSeconds, 10, 1f)
                .SetRecyclable(false) // a stored tween that is completed later must not be a recycled one
                .SetLink(holder.gameObject);
        }

        private void StartPulse(RectTransform holder)
        {
            _pulseTween = holder.DOScale(_feel.NodePulseScale, _feel.NodePulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(holder.gameObject);
        }

        // Shows the map from the bottom and scrolls up so the given level ends up in the middle of the screen.
        private void ScrollToLevel(int index, float contentHeight)
        {
            Canvas.ForceUpdateCanvases(); // the viewport's size is needed now, and a just-activated page has not been laid out yet
            float viewportHeight = _viewport.rect.height;
            float maxY = Mathf.Max(0f, contentHeight - viewportHeight);

            // The content hangs from its top edge: anchoredPosition.y = 0 shows the top of the path, maxY shows the bottom.
            float nodeFromTop = contentHeight - NodePosition(index).y;
            float targetY = Mathf.Clamp(nodeFromTop - viewportHeight / 2f, 0f, maxY);

            _scroll.StopMovement();
            _content.anchoredPosition = new Vector2(0f, maxY);
            if (Mathf.Approximately(maxY, targetY)) return;

            _scroll.enabled = false; // the user cannot fight the scroll animation
            _scrollTween = DOVirtual.Float(maxY, targetY, _feel.MapScrollSeconds, y => _content.anchoredPosition = new Vector2(0f, y))
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject)
                .OnKill(() => { if (_scroll != null) _scroll.enabled = true; });
        }

        private void KillTweens()
        {
            _introTween?.Kill();
            _pulseTween?.Kill();
            _scrollTween?.Kill();
            _wobbleTween?.Kill();
        }
    }
}
