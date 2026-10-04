using System;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// Sprites generated in code, so the prototype has visuals without any art files.
    /// RoundedSquare is for tiles; Solid is a plain white square for the board background and mask.
    /// The three special-tile icons (two arrows and a ring) are drawn from simple shapes with a little anti-aliasing.
    /// All sprites are exactly 1 x 1 world units at scale 1.
    /// </summary>
    public static class PlaceholderSprite
    {
        private const int RoundedSize = 64;
        private const float CornerRadius = 14f;

        private static Sprite _roundedSquare;
        private static Sprite _solid;
        private static Sprite _arrowHorizontal;
        private static Sprite _arrowVertical;
        private static Sprite _bomb;

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

        /// <summary>Icon of a rocket that clears its row: a double-headed arrow pointing left and right.</summary>
        public static Sprite ArrowHorizontal
        {
            get
            {
                if (_arrowHorizontal == null) _arrowHorizontal = CreateShape("PlaceholderArrowHorizontal", InsideArrow);
                return _arrowHorizontal;
            }
        }

        /// <summary>Icon of a rocket that clears its column: a double-headed arrow pointing up and down.</summary>
        public static Sprite ArrowVertical
        {
            get
            {
                // The same shape with x and y swapped.
                if (_arrowVertical == null) _arrowVertical = CreateShape("PlaceholderArrowVertical", (x, y) => InsideArrow(y, x));
                return _arrowVertical;
            }
        }

        /// <summary>Icon of a bomb: a ring with a dot in the middle.</summary>
        public static Sprite Bomb
        {
            get
            {
                if (_bomb == null) _bomb = CreateShape("PlaceholderBomb", InsideBomb);
                return _bomb;
            }
        }

        // Shapes are described in sprite space: x and y run from -0.5 to 0.5, (0,0) is the center.
        private static bool InsideArrow(float x, float y)
        {
            float absX = Mathf.Abs(x);
            float absY = Mathf.Abs(y);

            bool shaft = absX <= 0.38f && absY <= 0.06f;

            // Each head is a triangle: its tip is at |x| = 0.5, its flat back at |x| = 0.22 where it is 0.2 high on each side.
            bool head = absX >= 0.22f && absX <= 0.5f && absY <= (0.5f - absX) / 0.28f * 0.2f;

            return shaft || head;
        }

        private static bool InsideBomb(float x, float y)
        {
            float distance = Mathf.Sqrt(x * x + y * y);
            bool ring = distance <= 0.36f && distance >= 0.22f;
            bool dot = distance <= 0.1f;
            return ring || dot;
        }

        // Draws a white sprite wherever "inside" says true. Every pixel samples 3x3 points, so the edges are smooth.
        private static Sprite CreateShape(string name, Func<float, float, bool> inside)
        {
            const int size = 64;
            const int samples = 3;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            float u = (x + (sx + 0.5f) / samples) / size - 0.5f;
                            float v = (y + (sy + 0.5f) / samples) / size - 0.5f;
                            if (inside(u, v)) hits++;
                        }
                    }

                    byte alpha = (byte)Mathf.RoundToInt(255f * hits / (samples * samples));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
