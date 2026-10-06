using System;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// Sprites generated in code, so the prototype has visuals without any art files.
    /// RoundedSquare is for tiles; Solid is a plain white square for the board background and mask.
    /// The three special-tile icons (two arrows and a ring) are drawn from simple shapes with a little anti-aliasing.
    /// The obstacle pictures (crate, cracked crate, ice, cracked ice, chain) are drawn the same way, but in color.
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
        private static Sprite _crate;
        private static Sprite _crateCracked;
        private static Sprite _ice;
        private static Sprite _iceCracked;
        private static Sprite _chain;

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

        /// <summary>A wooden crate (2 HP, whole). It takes the whole cell, so it is a full picture, not an icon.</summary>
        public static Sprite Crate
        {
            get
            {
                if (_crate == null) _crate = CreateColored("PlaceholderCrate", (x, y) => CrateColor(x, y, false));
                return _crate;
            }
        }

        /// <summary>The same crate with a crack across it (1 HP left).</summary>
        public static Sprite CrateCracked
        {
            get
            {
                if (_crateCracked == null) _crateCracked = CreateColored("PlaceholderCrateCracked", (x, y) => CrateColor(x, y, true));
                return _crateCracked;
            }
        }

        /// <summary>A pale blue translucent square that lies over a tile (2 HP: thick and whole).</summary>
        public static Sprite Ice
        {
            get
            {
                if (_ice == null) _ice = CreateColored("PlaceholderIce", (x, y) => IceColor(x, y, false));
                return _ice;
            }
        }

        /// <summary>The ice with cracks across it, a little thinner (1 HP left).</summary>
        public static Sprite IceCracked
        {
            get
            {
                if (_iceCracked == null) _iceCracked = CreateColored("PlaceholderIceCracked", (x, y) => IceColor(x, y, true));
                return _iceCracked;
            }
        }

        /// <summary>Two crossed chains, drawn over a tile that cannot be moved. Everything around the chains is transparent.</summary>
        public static Sprite Chain
        {
            get
            {
                if (_chain == null) _chain = CreateColored("PlaceholderChain", ChainColor);
                return _chain;
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


        // ---------- colored shapes (the obstacles) ----------

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        // Signed distance from (x, y) to a rounded square that reaches +-half from the center (negative = inside).
        private static float RoundedBoxDistance(float x, float y, float half, float radius)
        {
            float dx = Mathf.Max(Mathf.Abs(x) - (half - radius), 0f);
            float dy = Mathf.Max(Mathf.Abs(y) - (half - radius), 0f);
            return Mathf.Sqrt(dx * dx + dy * dy) - radius;
        }

        // Distance from the point to the line segment a-b.
        private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax;
            float aby = by - ay;
            float t = Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / (abx * abx + aby * aby));
            float cx = ax + abx * t - px;
            float cy = ay + aby * t - py;
            return Mathf.Sqrt(cx * cx + cy * cy);
        }

        // The main crack (top to bottom) and a short branch. Used by the crate and, thinner, by the ice.
        private static float DistanceToCrack(float x, float y)
        {
            float d = DistanceToSegment(x, y, -0.05f, 0.5f, 0.06f, 0.22f);
            d = Mathf.Min(d, DistanceToSegment(x, y, 0.06f, 0.22f, -0.08f, 0.06f));
            d = Mathf.Min(d, DistanceToSegment(x, y, -0.08f, 0.06f, 0.10f, -0.14f));
            d = Mathf.Min(d, DistanceToSegment(x, y, 0.10f, -0.14f, -0.02f, -0.30f));
            d = Mathf.Min(d, DistanceToSegment(x, y, -0.02f, -0.30f, 0.08f, -0.5f));
            d = Mathf.Min(d, DistanceToSegment(x, y, 0.06f, 0.22f, 0.30f, 0.12f));
            return d;
        }

        private static Color CrateColor(float x, float y, bool cracked)
        {
            float edge = RoundedBoxDistance(x, y, 0.47f, 0.09f);
            if (edge > 0f) return Clear;

            Color outline = new Color(0.25f, 0.15f, 0.07f);
            Color frame = new Color(0.45f, 0.28f, 0.13f);
            Color wood = new Color(0.76f, 0.54f, 0.28f);

            if (edge > -0.03f) return outline;

            float ax = Mathf.Abs(x);
            float ay = Mathf.Abs(y);
            Color color = wood;
            if (ax > 0.36f || ay > 0.36f) color = frame;                      // the wooden frame
            else if (Mathf.Abs(ax - ay) < 0.045f) color = frame;               // the two braces across the middle
            else if (Mathf.Abs(Mathf.Repeat(y + 0.5f, 0.2f) - 0.1f) > 0.095f) color = new Color(0.66f, 0.45f, 0.22f); // thin plank lines

            if (cracked && DistanceToCrack(x, y) < 0.024f) color = new Color(0.10f, 0.06f, 0.03f);
            return color;
        }

        private static Color IceColor(float x, float y, bool cracked)
        {
            float edge = RoundedBoxDistance(x, y, 0.47f, 0.12f);
            if (edge > 0f) return Clear;

            float alpha = cracked ? 0.42f : 0.60f; // 1 HP ice is thinner, so more of the tile shows through
            Color color = new Color(0.62f, 0.88f, 1f, alpha);

            if (edge > -0.04f) color = new Color(0.88f, 0.97f, 1f, alpha + 0.3f); // a bright rim

            // Two diagonal shiny streaks, like light on glass.
            float diagonal = (x - y) * 0.7071f;
            float across = (x + y) * 0.7071f;
            bool streak = Mathf.Abs(diagonal + 0.20f) < 0.035f && Mathf.Abs(across) < 0.34f;
            streak |= Mathf.Abs(diagonal + 0.30f) < 0.015f && Mathf.Abs(across) < 0.2f;
            if (streak) color = new Color(1f, 1f, 1f, alpha + 0.3f);

            if (cracked && DistanceToCrack(x, y) < 0.016f) color = new Color(0.95f, 1f, 1f, 0.95f);
            return color;
        }

        // Two chains crossing in an X. A chain is a row of links: a ring seen from the front, then a link seen edge-on, and so on.
        private static Color ChainColor(float x, float y)
        {
            Color best = Clear;
            for (int diagonal = 0; diagonal < 2; diagonal++)
            {
                // The coordinates along and across this chain. The second chain runs the other way.
                float along = (diagonal == 0 ? x + y : x - y) * 0.7071f;
                float across = (diagonal == 0 ? x - y : x + y) * 0.7071f;

                for (int link = 0; link < 5; link++)
                {
                    float center = -0.4f + link * 0.2f;
                    float u = along - center;

                    Color color;
                    if (link % 2 == 0)
                    {
                        // A ring: filled between a big and a small ellipse. The outer edge is dark.
                        float radius = Mathf.Sqrt((u * u) / (0.14f * 0.14f) + (across * across) / (0.075f * 0.075f));
                        if (radius > 1f || radius < 0.5f) continue;
                        color = radius > 0.85f ? new Color(0.22f, 0.24f, 0.30f) : new Color(0.82f, 0.85f, 0.90f);
                    }
                    else
                    {
                        // A link seen from the side: a short thick bar.
                        float bar = RoundedBoxDistance(u, across, 0.1f, 0.035f);
                        if (bar > 0f) continue;
                        color = bar > -0.012f ? new Color(0.22f, 0.24f, 0.30f) : new Color(0.68f, 0.71f, 0.77f);
                    }

                    best = color; // later links and the second chain draw over the earlier ones
                }
            }

            return best;
        }

        // Draws a sprite pixel by pixel from a color function. Every pixel averages 3x3 samples, so the edges are smooth.
        // The colors are averaged with their alpha as weight, so a half-transparent edge does not turn dark.
        private static Sprite CreateColored(string name, Func<float, float, Color> colorAt)
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
                    float red = 0f, green = 0f, blue = 0f, alpha = 0f;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            float u = (x + (sx + 0.5f) / samples) / size - 0.5f;
                            float v = (y + (sy + 0.5f) / samples) / size - 0.5f;
                            Color sample = colorAt(u, v);
                            red += sample.r * sample.a;
                            green += sample.g * sample.a;
                            blue += sample.b * sample.a;
                            alpha += sample.a;
                        }
                    }

                    Color32 pixel = new Color32(0, 0, 0, 0);
                    if (alpha > 0f)
                    {
                        pixel = new Color32(
                            (byte)Mathf.RoundToInt(255f * red / alpha),
                            (byte)Mathf.RoundToInt(255f * green / alpha),
                            (byte)Mathf.RoundToInt(255f * blue / alpha),
                            (byte)Mathf.RoundToInt(255f * alpha / (samples * samples)));
                    }

                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
