using UnityEngine;
using GlobalDomination.GameData;

namespace GlobalDomination.UI.Hud
{
    /// <summary>
    /// Creates simple fallback flag sprites when no flag asset is assigned.
    /// </summary>
    public static class CountryFlagFactory
    {
        public static Sprite CreateFallbackFlag(CountryType country, int width = 96, int height = 64)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color white = new Color(0.95f, 0.95f, 0.95f, 1f);
            Color red = new Color(0.78f, 0.16f, 0.2f, 1f);
            Color blue = new Color(0.08f, 0.2f, 0.55f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, white);
                }
            }

            if (country == CountryType.England)
            {
                DrawEngland(texture, width, height, red);
            }
            else if (country == CountryType.France)
            {
                DrawFrance(texture, width, height, blue, red);
            }
            else if (country == CountryType.Russia)
            {
                DrawRussia(texture, width, height, white, blue, red);
            }
            else
            {
                DrawAmerica(texture, width, height, white, blue, red);
            }

            texture.Apply();

            Rect rect = new Rect(0f, 0f, width, height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            sprite.name = country + "_FallbackFlag";
            return sprite;
        }

        private static void DrawEngland(Texture2D texture, int width, int height, Color red)
        {
            int verticalBar = width / 6;
            int horizontalBar = height / 5;
            int centerX = width / 2;
            int centerY = height / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = centerX - verticalBar / 2; x <= centerX + verticalBar / 2; x++)
                {
                    if (x >= 0 && x < width)
                    {
                        texture.SetPixel(x, y, red);
                    }
                }
            }

            for (int y = centerY - horizontalBar / 2; y <= centerY + horizontalBar / 2; y++)
            {
                if (y < 0 || y >= height)
                {
                    continue;
                }

                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, red);
                }
            }
        }

        private static void DrawFrance(Texture2D texture, int width, int height, Color blue, Color red)
        {
            int stripe = width / 3;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < stripe)
                    {
                        texture.SetPixel(x, y, blue);
                    }
                    else if (x >= stripe * 2)
                    {
                        texture.SetPixel(x, y, red);
                    }
                }
            }
        }

        private static void DrawRussia(Texture2D texture, int width, int height, Color white, Color blue, Color red)
        {
            int stripe = height / 3;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y < stripe)
                    {
                        texture.SetPixel(x, y, white);
                    }
                    else if (y < stripe * 2)
                    {
                        texture.SetPixel(x, y, blue);
                    }
                    else
                    {
                        texture.SetPixel(x, y, red);
                    }
                }
            }
        }

        private static void DrawAmerica(Texture2D texture, int width, int height, Color white, Color blue, Color red)
        {
            int stripe = height / 7;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool redStripe = (y / stripe) % 2 == 0;
                    texture.SetPixel(x, y, redStripe ? red : white);
                }
            }

            int cantonWidth = width * 2 / 5;
            int cantonHeight = height * 4 / 7;
            for (int y = height - cantonHeight; y < height; y++)
            {
                for (int x = 0; x < cantonWidth; x++)
                {
                    texture.SetPixel(x, y, blue);
                }
            }
        }
    }
}
