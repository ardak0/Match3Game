using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// Small helpers that build uGUI objects in code, so the HUD and the end screen need no hand-made UI in the editor.
    /// Everything is laid out for a 1080 x 1920 portrait reference screen; the canvas scales it to the real screen.
    /// Text uses Unity's built-in font (legacy Text), which needs no asset import.
    /// </summary>
    public static class UiFactory
    {
        private static Font _font;

        private static Font DefaultFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        /// <summary>A full-screen canvas drawn on top of the game. sortingOrder decides which canvas covers which.</summary>
        public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        /// <summary>
        /// Buttons need an EventSystem in the scene. If there is none, make one that reads the new Input System
        /// (the project does not use the old Input class, so the default input module would not work).
        /// </summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        /// <summary>Places a rect relative to a point on its parent: anchor (0..1, 0..1) is the point, offset moves it from there.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        /// <summary>Makes a rect cover its whole parent.</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            RectTransform rect = CreateRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(parent, name);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A rounded rectangle button with a centered label. Returns the button; the label is its child "Label".</summary>
        public static Button CreateButton(Transform parent, string name, string label, Vector2 size, Color background)
        {
            Image image = CreateImage(parent, name, background, PlaceholderSprite.RoundedSquare);
            image.raycastTarget = true; // the button's own picture is what receives the click
            image.rectTransform.sizeDelta = size;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText(image.transform, "Label", label, 64, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }
    }
}
