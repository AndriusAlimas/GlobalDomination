using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalDomination.UI
{
    /// <summary>
    /// Shared UI builders for the build-city dice roll presentation.
    /// </summary>
    public static class BuildCityDiceUiFactory
    {
        private static Sprite diceHandSprite;

        public static TextMeshProUGUI CreateDiceText(Transform parent, string objectName, float fontSize, Vector2 anchoredPosition)
        {
            GameObject textObj = new GameObject(objectName);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(760f, 80f);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        public static Image CreateDiceHandImage(Transform parent)
        {
            if (diceHandSprite == null)
            {
                diceHandSprite = CreateDiceHandSprite();
            }

            if (diceHandSprite == null)
            {
                return null;
            }

            GameObject handObj = new GameObject("DiceHand");
            handObj.transform.SetParent(parent, false);

            RectTransform rect = handObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.08f);
            rect.anchoredPosition = new Vector2(-170f, 92f);
            rect.sizeDelta = new Vector2(240f, 280f);

            Image image = handObj.AddComponent<Image>();
            image.sprite = diceHandSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite CreateDiceHandSprite()
        {
            const int width = 192;
            const int height = 256;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color clear = Color.clear;
            Color fill = new Color(0.83f, 0.58f, 0.34f, 1f);
            Color shadow = new Color(0.56f, 0.31f, 0.17f, 1f);
            Color highlight = new Color(0.92f, 0.72f, 0.47f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool insidePalm = IsInsideRoundedRect(x, y, 54f, 40f, 86f, 120f, 24f);
                    bool insideFinger1 = IsInsideRoundedRect(x, y, 38f, 138f, 26f, 84f, 12f);
                    bool insideFinger2 = IsInsideRoundedRect(x, y, 64f, 156f, 24f, 88f, 12f);
                    bool insideFinger3 = IsInsideRoundedRect(x, y, 90f, 150f, 24f, 86f, 12f);
                    bool insideFinger4 = IsInsideRoundedRect(x, y, 116f, 132f, 24f, 76f, 12f);
                    bool insideThumb = IsInsideEllipse(x, y, 40f, 105f, 28f, 58f, 0.58f);

                    bool inside = insidePalm || insideFinger1 || insideFinger2 || insideFinger3 || insideFinger4 || insideThumb;
                    if (!inside)
                    {
                        continue;
                    }

                    bool edge = !IsInsideRoundedRect(x - 2, y, 54f, 40f, 86f, 120f, 24f) && insidePalm;
                    edge |= !IsInsideRoundedRect(x, y - 2, 38f, 138f, 26f, 84f, 12f) && insideFinger1;
                    edge |= !IsInsideRoundedRect(x, y - 2, 64f, 156f, 24f, 88f, 12f) && insideFinger2;
                    edge |= !IsInsideRoundedRect(x, y - 2, 90f, 150f, 24f, 86f, 12f) && insideFinger3;
                    edge |= !IsInsideRoundedRect(x, y - 2, 116f, 132f, 24f, 76f, 12f) && insideFinger4;

                    float light = Mathf.InverseLerp(0f, height, y);
                    Color color = Color.Lerp(shadow, fill, light);
                    if (x < 72)
                    {
                        color = Color.Lerp(color, shadow, 0.18f);
                    }
                    if (x > 110 && y > 70)
                    {
                        color = Color.Lerp(color, highlight, 0.22f);
                    }
                    if (edge)
                    {
                        color = Color.Lerp(color, shadow, 0.35f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.08f), 100f);
        }

        private static bool IsInsideRoundedRect(float x, float y, float left, float bottom, float width, float height, float radius)
        {
            float right = left + width;
            float top = bottom + height;

            float clampedX = Mathf.Clamp(x, left + radius, right - radius);
            float clampedY = Mathf.Clamp(y, bottom + radius, top - radius);
            float dx = x - clampedX;
            float dy = y - clampedY;
            return (dx * dx) + (dy * dy) <= radius * radius;
        }

        private static bool IsInsideEllipse(float x, float y, float centerX, float centerY, float radiusX, float radiusY, float rotationRadians)
        {
            float sin = Mathf.Sin(rotationRadians);
            float cos = Mathf.Cos(rotationRadians);

            float dx = x - centerX;
            float dy = y - centerY;
            float localX = dx * cos + dy * sin;
            float localY = -dx * sin + dy * cos;

            float value = (localX * localX) / (radiusX * radiusX) + (localY * localY) / (radiusY * radiusY);
            return value <= 1f;
        }
    }
}
