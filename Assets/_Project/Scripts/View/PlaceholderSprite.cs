using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// Sprites generated in code, so the prototype has visuals without any art files.
    /// RoundedSquare is for tiles; Solid is a plain white square for the board background and mask.
    /// Both are exactly 1 x 1 world units at scale 1.
    /// </summary>
    public static class PlaceholderSprite
    {
        private const int RoundedSize = 64;
        private const float CornerRadius = 14f;

        private static Sprite _roundedSquare;
        private static Sprite _solid;

        public static Sprite RoundedSquare
        {
            get
            {
                // "== null" on a Unity object also catches an object Unity already destroyed.
                if (_roundedSquare == null) _roundedSquare = CreateRoundedSquare();
                return _roundedSquare;
            }
        }

        public static Sprite Solid
        {
            get
            {
                if (_solid == null) _solid = CreateSolid();
                return _solid;
            }
        }

        private static Sprite CreateRoundedSquare()
        {
            Texture2D texture = new Texture2D(RoundedSize, RoundedSize, TextureFormat.RGBA32, false);
            texture.name = "PlaceholderRoundedSquare";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            float half = RoundedSize / 2f;
            Color32[] pixels = new Color32[RoundedSize * RoundedSize];
            for (int y = 0; y < RoundedSize; y++)
            {
                for (int x = 0; x < RoundedSize; x++)
                {
                    // Distance from this pixel to the edge of a rounded rectangle (negative = inside).
                    float px = Mathf.Abs(x + 0.5f - half);
                    float py = Mathf.Abs(y + 0.5f - half);
                    float dx = Mathf.Max(px - (half - CornerRadius), 0f);
                    float dy = Mathf.Max(py - (half - CornerRadius), 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) - CornerRadius;

                    float alpha = Mathf.Clamp01(0.5f - distance); // one pixel of soft edge
                    pixels[y * RoundedSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture, new Rect(0, 0, RoundedSize, RoundedSize), new Vector2(0.5f, 0.5f), RoundedSize);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateSolid()
        {
            const int size = 4;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "PlaceholderSolid";
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color32[] pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
