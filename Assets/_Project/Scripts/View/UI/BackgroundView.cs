using DG.Tweening;
using Match3.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.View.UI
{
    /// <summary>
    /// The scene background: a vertical gradient with a few faint shapes that slowly drift upwards.
    /// It is a canvas that renders THROUGH the camera and sorts behind everything (sorting order -100),
    /// so it sits behind the board's sprites in the Game scene; an overlay canvas would cover the board.
    /// The gradient is one thin generated texture stretched over the screen, and the shapes are ordinary UI images
    /// moved by looping DOTween tweens, so there is no Update and nothing is allocated while playing.
    /// </summary>
    public sealed class BackgroundView : MonoBehaviour
    {
        private const int GradientHeight = 128;
        private const float ShapeMinSize = 70f;
        private const float ShapeMaxSize = 240f;
        private const float ShapeMinAlpha = 0.05f;
        private const float ShapeMaxAlpha = 0.13f;

        // Top to bottom: deep indigo, the old flat blue in the middle (the board still reads well on it), a lighter teal.
        private static readonly Color TopColor = new Color(0.10f, 0.13f, 0.32f, 1f);
        private static readonly Color MiddleColor = new Color(0.19f, 0.30f, 0.47f, 1f);
        private static readonly Color BottomColor = new Color(0.26f, 0.46f, 0.60f, 1f);

        private Texture2D _gradientTexture;
        private Sprite _gradientSprite;

        /// <param name="camera">The camera the canvas is drawn through.</param>
        public static BackgroundView Create(Camera camera, UiStyle style, FeelSettings feel)
        {
            BackgroundView view = new GameObject("Background").AddComponent<BackgroundView>();
            view.Build(camera, style, feel);
            return view;
        }

        private void Build(Camera camera, UiStyle style, FeelSettings feel)
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Background Canvas", -100);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.farClipPlane * 0.5f;
            Destroy(canvas.GetComponent<GraphicRaycaster>()); // a background must never block a tap

            _gradientTexture = CreateGradientTexture();
            _gradientSprite = Sprite.Create(_gradientTexture, new Rect(0f, 0f, 1f, GradientHeight), new Vector2(0.5f, 0.5f), 100f);
            Image gradient = UiFactory.CreateImage(canvas.transform, "Gradient", Color.white, _gradientSprite);
            gradient.raycastTarget = false;
            UiFactory.Stretch(gradient.rectTransform);

            Sprite[] shapes = { style.DotSprite, style.StarSprite, PlaceholderSprite.RoundedSquare };
            for (int i = 0; i < feel.BackgroundShapeCount; i++)
            {
                CreateShape(canvas.transform, shapes[i % shapes.Length], i % shapes.Length == 2, feel);
            }
        }

        private void OnDestroy()
        {
            if (_gradientSprite != null) Destroy(_gradientSprite);
            if (_gradientTexture != null) Destroy(_gradientTexture);
        }

        private static Texture2D CreateGradientTexture()
        {
            Texture2D texture = new Texture2D(1, GradientHeight, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < GradientHeight; y++)
            {
                float fromTop = 1f - y / (GradientHeight - 1f); // pixel row 0 is the bottom of the texture
                Color color = fromTop < 0.5f
                    ? Color.Lerp(TopColor, MiddleColor, fromTop * 2f)
                    : Color.Lerp(MiddleColor, BottomColor, (fromTop - 0.5f) * 2f);
                texture.SetPixel(0, y, color);
            }

            texture.Apply();
            return texture;
        }

        private static void CreateShape(Transform parent, Sprite sprite, bool diamond, FeelSettings feel)
        {
            float size = Random.Range(ShapeMinSize, ShapeMaxSize);
            Color color = new Color(1f, 1f, 1f, Random.Range(ShapeMinAlpha, ShapeMaxAlpha));
            Image image = UiFactory.CreateImage(parent, "Shape", color, sprite);
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            // A bit more than half the reference size, because a tall phone screen shows more canvas than 1080x1920.
            float halfWidth = UiFactory.ReferenceWidth * 0.6f;
            float halfHeight = UiFactory.ReferenceHeight * 0.6f;
            float x = Random.Range(-halfWidth, halfWidth);
            float bottom = -halfHeight - size;
            float top = halfHeight + size;
            UiFactory.Place(rect, new Vector2(0.5f, 0.5f), new Vector2(x, bottom), new Vector2(size, size));
            rect.localRotation = Quaternion.Euler(0f, 0f, diamond ? 45f : Random.Range(0f, 360f));

            float seconds = Random.Range(feel.BackgroundDriftMinSeconds, feel.BackgroundDriftMaxSeconds);

            // Looping tweens that start at a random point of their path, so the screen is filled from the first frame.
            Tween move = rect.DOAnchorPosY(top, seconds).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetLink(image.gameObject);
            move.Goto(Random.value * seconds, true);

            float spin = Random.value < 0.5f ? 360f : -360f;
            rect.DOLocalRotate(new Vector3(0f, 0f, rect.localEulerAngles.z + spin), seconds * 2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetLink(image.gameObject);
        }
    }
}
