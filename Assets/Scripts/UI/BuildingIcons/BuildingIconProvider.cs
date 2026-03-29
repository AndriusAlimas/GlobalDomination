using System.Collections.Generic;
using UnityEngine;
using GlobalDomination.GameData;

namespace GlobalDomination.UI.BuildingIcons
{
    /// <summary>
    /// Supplies sprites per <see cref="BuildingType"/>: loads
    /// <c>Resources/BuildingIcons/{BuildingType}</c> if present, otherwise builds a procedural icon.
    /// </summary>
    public static class BuildingIconProvider
    {
        private const int IconSize = 96;
        private static readonly Dictionary<BuildingType, Sprite> Cache = new Dictionary<BuildingType, Sprite>();

        public static Sprite GetIcon(BuildingType type)
        {
            if (type == BuildingType.None)
            {
                return null;
            }

            if (Cache.TryGetValue(type, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite loaded = Resources.Load<Sprite>($"BuildingIcons/{type}");
            if (loaded != null)
            {
                Cache[type] = loaded;
                return loaded;
            }

            Sprite generated = CreateProceduralIcon(type);
            Cache[type] = generated;
            return generated;
        }

        private static Sprite CreateProceduralIcon(BuildingType type)
        {
            Texture2D tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                }
            }

            Color bg = BaseColor(type);
            FillRectSolid(tex, IconSize, margin: 4, bg);
            DrawGlyph(tex, type, Brighten(bg));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, IconSize, IconSize), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Color Brighten(Color c)
        {
            return Color.Lerp(c, Color.white, 0.55f);
        }

        private static Color BaseColor(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.SpecForce: return new Color(0.75f, 0.2f, 0.22f);
                case BuildingType.PowerBase: return new Color(0.95f, 0.75f, 0.15f);
                case BuildingType.Barraka: return new Color(0.55f, 0.38f, 0.24f);
                case BuildingType.LowTech: return new Color(0.45f, 0.48f, 0.52f);
                case BuildingType.DroneFactory: return new Color(0.55f, 0.35f, 0.78f);
                case BuildingType.MutantLab: return new Color(0.35f, 0.78f, 0.42f);
                case BuildingType.MoneyBase: return new Color(0.92f, 0.72f, 0.2f);
                case BuildingType.MidTech: return new Color(0.25f, 0.72f, 0.82f);
                case BuildingType.AirShipBase: return new Color(0.35f, 0.55f, 0.95f);
                case BuildingType.HighTech: return new Color(0.85f, 0.35f, 0.78f);
                case BuildingType.SpecialWarBase: return new Color(0.45f, 0.18f, 0.2f);
                case BuildingType.ShipBase: return new Color(0.22f, 0.45f, 0.82f);
                case BuildingType.MainBase: return new Color(0.92f, 0.5f, 0.22f);
                case BuildingType.NuclearWeapon: return new Color(0.55f, 0.85f, 0.28f);
                default: return new Color(0.4f, 0.42f, 0.46f);
            }
        }

        private static void FillRectSolid(Texture2D tex, int size, int margin, Color fill)
        {
            for (int y = margin; y < size - margin; y++)
            {
                for (int x = margin; x < size - margin; x++)
                {
                    tex.SetPixel(x, y, fill);
                }
            }
        }

        private static void DrawGlyph(Texture2D tex, BuildingType type, Color ink)
        {
            int cx = IconSize / 2;
            int cy = IconSize / 2;
            switch (type)
            {
                case BuildingType.SpecForce:
                    DrawStar(tex, cx, cy, 22, ink);
                    break;
                case BuildingType.PowerBase:
                    DrawZigZag(tex, cx, cy, ink);
                    break;
                case BuildingType.Barraka:
                    FillRect(tex, cx - 18, cy - 14, 36, 28, ink);
                    FillRect(tex, cx - 8, cy + 14, 16, 10, ink);
                    break;
                case BuildingType.LowTech:
                    FillRect(tex, cx - 20, cy - 8, 12, 16, ink);
                    FillRect(tex, cx - 4, cy - 8, 12, 16, ink);
                    FillRect(tex, cx + 12, cy - 8, 12, 16, ink);
                    break;
                case BuildingType.DroneFactory:
                    DrawRectOutline(tex, cx - 20, cy - 20, 40, 40, ink);
                    FillRect(tex, cx - 6, cy - 6, 12, 12, ink);
                    break;
                case BuildingType.MutantLab:
                    FillCircle(tex, cx, cy + 4, 16, ink);
                    FillRect(tex, cx - 4, cy - 18, 8, 22, ink);
                    break;
                case BuildingType.MoneyBase:
                    FillCircle(tex, cx, cy, 20, ink);
                    break;
                case BuildingType.MidTech:
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i * Mathf.PI * 0.25f;
                        int px = cx + Mathf.RoundToInt(Mathf.Cos(ang) * 18f);
                        int py = cy + Mathf.RoundToInt(Mathf.Sin(ang) * 18f);
                        FillRect(tex, px - 3, py - 3, 6, 6, ink);
                    }

                    break;
                case BuildingType.AirShipBase:
                    DrawTriangle(tex, cx, cy + 12, cx - 22, cy - 16, cx + 22, cy - 16, ink);
                    break;
                case BuildingType.HighTech:
                    DrawDiamond(tex, cx, cy, 24, ink);
                    break;
                case BuildingType.SpecialWarBase:
                    FillRect(tex, cx - 18, cy - 20, 36, 36, ink);
                    break;
                case BuildingType.ShipBase:
                    for (int i = -2; i <= 2; i++)
                    {
                        DrawLine(tex, cx - 24 + i * 10, cy + 8 + i * 4, cx - 14 + i * 10, cy - 12 + i * 4, ink);
                    }

                    break;
                case BuildingType.MainBase:
                    FillRect(tex, cx - 24, cy - 18, 48, 36, ink);
                    FillRect(tex, cx - 8, cy + 18, 16, 10, ink);
                    break;
                case BuildingType.NuclearWeapon:
                    for (int b = 0; b < 3; b++)
                    {
                        float a = b * 120f * Mathf.Deg2Rad;
                        int dx = Mathf.RoundToInt(Mathf.Cos(a) * 14f);
                        int dy = Mathf.RoundToInt(Mathf.Sin(a) * 14f);
                        FillRect(tex, cx + dx - 4, cy + dy - 16, 8, 28, ink);
                    }

                    FillCircle(tex, cx, cy, 8, ink);
                    break;
                default:
                    FillRect(tex, cx - 12, cy - 12, 24, 24, ink);
                    break;
            }
        }

        private static void DrawStar(Texture2D tex, int cx, int cy, int outer, Color c)
        {
            for (int i = 0; i < 5; i++)
            {
                float a0 = (i * 144f - 90f) * Mathf.Deg2Rad;
                float a1 = ((i + 1) * 144f - 90f) * Mathf.Deg2Rad;
                int x0 = cx + Mathf.RoundToInt(Mathf.Cos(a0) * outer);
                int y0 = cy + Mathf.RoundToInt(Mathf.Sin(a0) * outer);
                int x1 = cx + Mathf.RoundToInt(Mathf.Cos(a1) * outer);
                int y1 = cy + Mathf.RoundToInt(Mathf.Sin(a1) * outer);
                DrawLine(tex, x0, y0, x1, y1, c);
                DrawLine(tex, cx, cy, x0, y0, c);
            }
        }

        private static void DrawZigZag(Texture2D tex, int cx, int cy, Color c)
        {
            int x0 = cx - 22;
            for (int i = 0; i < 4; i++)
            {
                int yA = cy + (i % 2 == 0 ? 10 : -10);
                int yB = cy + (i % 2 == 0 ? -10 : 10);
                DrawLine(tex, x0 + i * 14, yA, x0 + (i + 1) * 14, yB, c);
            }
        }

        private static void DrawRectOutline(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            DrawLine(tex, x, y, x + w, y, c);
            DrawLine(tex, x + w, y, x + w, y + h, c);
            DrawLine(tex, x + w, y + h, x, y + h, c);
            DrawLine(tex, x, y + h, x, y, c);
        }

        private static void DrawTriangle(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color c)
        {
            DrawLine(tex, x0, y0, x1, y1, c);
            DrawLine(tex, x1, y1, x2, y2, c);
            DrawLine(tex, x2, y2, x0, y0, c);
        }

        private static void DrawDiamond(Texture2D tex, int cx, int cy, int half, Color c)
        {
            DrawLine(tex, cx, cy + half, cx + half, cy, c);
            DrawLine(tex, cx + half, cy, cx, cy - half, c);
            DrawLine(tex, cx, cy - half, cx - half, cy, c);
            DrawLine(tex, cx - half, cy, cx, cy + half, c);
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int j = y; j < y + h; j++)
            {
                for (int i = x; i < x + w; i++)
                {
                    SetPixel(tex, i, j, c);
                }
            }
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            for (int j = cy - r; j <= cy + r; j++)
            {
                for (int i = cx - r; i <= cx + r; i++)
                {
                    if ((i - cx) * (i - cx) + (j - cy) * (j - cy) <= r * r)
                    {
                        SetPixel(tex, i, j, c);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                SetPixel(tex, x0, y0, c);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void SetPixel(Texture2D tex, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= IconSize || y >= IconSize)
            {
                return;
            }

            tex.SetPixel(x, y, c);
        }
    }
}
