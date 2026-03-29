using UnityEngine;

namespace GlobalDomination
{
    /// <summary>
    /// Shared procedural D6 face sprites and spin visuals for HUD / city roll UIs.
    /// </summary>
    public static class DiceFaceSpriteUtility
    {
        public const int DefaultFaceSize = 96;

        /// <summary>Returns a 7-length array; indices 1–6 are sprites.</summary>
        public static Sprite[] CreateIndexedDiceFaceSprites(int size = DefaultFaceSize)
        {
            Sprite[] faces = new Sprite[7];
            for (int i = 1; i <= 6; i++)
            {
                faces[i] = CreateDieFaceSprite(i, size);
            }

            return faces;
        }

        public static Sprite CreateDieFaceSprite(int value, int size = DefaultFaceSize)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color faceColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            Color edgeColor = new Color(0.18f, 0.18f, 0.18f, 1f);
            Color pipColor = new Color(0.03f, 0.03f, 0.03f, 1f);

            const int border = 4;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x < border || x >= size - border || y < border || y >= size - border;
                    texture.SetPixel(x, y, isBorder ? edgeColor : faceColor);
                }
            }

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float offset = size * 0.23f;
            Vector2 topLeft = new Vector2(center.x - offset, center.y + offset);
            Vector2 topRight = new Vector2(center.x + offset, center.y + offset);
            Vector2 midLeft = new Vector2(center.x - offset, center.y);
            Vector2 midRight = new Vector2(center.x + offset, center.y);
            Vector2 botLeft = new Vector2(center.x - offset, center.y - offset);
            Vector2 botRight = new Vector2(center.x + offset, center.y - offset);

            const int pipRadius = 6;
            if (value == 1 || value == 3 || value == 5)
            {
                DrawPip(texture, center, pipRadius, pipColor);
            }

            if (value >= 2)
            {
                DrawPip(texture, topLeft, pipRadius, pipColor);
                DrawPip(texture, botRight, pipRadius, pipColor);
            }

            if (value >= 4)
            {
                DrawPip(texture, topRight, pipRadius, pipColor);
                DrawPip(texture, botLeft, pipRadius, pipColor);
            }

            if (value == 6)
            {
                DrawPip(texture, midLeft, pipRadius, pipColor);
                DrawPip(texture, midRight, pipRadius, pipColor);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static void ApplyDiceSpinVisual(RectTransform dieRect, float elapsed, float duration, Vector3 baseScale)
        {
            if (dieRect == null)
            {
                return;
            }

            float t = duration > 0.001f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float fastSpin = elapsed * 980f;
            float settleSpin = Mathf.Lerp(1f, 0.25f, t);
            dieRect.localRotation = Quaternion.Euler(0f, 0f, fastSpin * settleSpin);

            float pulse = Mathf.Abs(Mathf.Sin(elapsed * 18f));
            float flatten = Mathf.Lerp(0.72f, 0.96f, t) + (pulse * 0.08f * (1f - t));
            dieRect.localScale = new Vector3(baseScale.x * flatten, baseScale.y, baseScale.z);
        }

        private static void DrawPip(Texture2D texture, Vector2 center, int radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 1));
            int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + radius + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 1));
            int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + radius + 1));

            float maxDist = radius + 0.5f;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= maxDist)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }
}
