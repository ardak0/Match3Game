using Match3.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// Small helpers that build uGUI objects in code, so the HUD and the end screen need no hand-made UI in the editor.
    /// Everything is laid out for a 1080 x 1920 portrait reference screen; the canvas scales it to the real screen.
    /// Text is TextMeshPro with the font from the UiStyle asset; sprites for panels and buttons come from there too.
    /// </summary>
    public static class UiFactory
    {
        // The one place that defines how every canvas scales: a 1080 x 1920 portrait layout,
        // width and height counted equally (0.5), so tall and short phones both look right.
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const float MatchWidthOrHeight = 0.5f;

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
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;

            return canvas;
        }

        /// <summary>A full-canvas rect that follows the screen's safe area (see SafeArea). Put HUD content inside it.</summary>
        public static RectTransform CreateSafeArea(Transform canvas)
        {
            RectTransform rect = CreateRect(canvas, "Safe Area");
            Stretch(rect);
            rect.gameObject.AddComponent<SafeArea>();
            return rect;
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

        /// <summary>An image. A sprite with 9-slice borders is drawn sliced, so it can be any size without stretching its corners.</summary>
        public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            RectTransform rect = CreateRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent, string name, string content, float fontSize, Color color, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            RectTransform rect = CreateRect(parent, name);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A button drawn with a 9-slice sprite and a centered label. Returns the button; the label is its child "Label".</summary>
        public static Button CreateButton(Transform parent, string name, string label, Vector2 size, Sprite sprite, TMP_FontAsset font, FeelSettings feel)
        {
            Image image = CreateImage(parent, name, Color.white, sprite);
            image.raycastTarget = true; // the button's own picture is what receives the click
            image.rectTransform.sizeDelta = size;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            image.gameObject.AddComponent<ButtonPress>().Init(feel); // shrinks while held down

            TextMeshProUGUI text = CreateText(image.transform, "Label", label, 64f, Color.white, TextAlignmentOptions.Center, font);
            Stretch(text.rectTransform);
            return button;
        }
    }
}
