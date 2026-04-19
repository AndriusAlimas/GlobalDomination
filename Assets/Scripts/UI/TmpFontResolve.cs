using TMPro;
using UnityEngine;

namespace GlobalDomination.UI
{
    /// <summary>
    /// Resolves a TextMeshPro font for runtime-built UI. Missing font (or assigning <see cref="TMP_Text.text"/>
    /// before a font) causes TMP/material paths to throw <c>ArgumentNullException</c> (<c>source</c>).
    /// </summary>
    public static class TmpFontResolve
    {
        private static readonly string[] s_resourceFontPaths =
        {
            "Fonts & Materials/LiberationSans SDF",
            "Fonts & Materials/LiberationSans SDF - Fallback",
            "LiberationSans SDF",
            "LiberationSans SDF - Fallback",
        };

        public static TMP_FontAsset GetDefault()
        {
            TMP_FontAsset fromSettings = TMP_Settings.defaultFontAsset;
            if (fromSettings != null)
            {
                return fromSettings;
            }

            for (int i = 0; i < s_resourceFontPaths.Length; i++)
            {
                TMP_FontAsset loaded = Resources.Load<TMP_FontAsset>(s_resourceFontPaths[i]);
                if (loaded != null)
                {
                    return loaded;
                }
            }

            TMP_FontAsset[] inMemory = Object.FindObjectsByType<TMP_FontAsset>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < inMemory.Length; i++)
            {
                if (inMemory[i] != null)
                {
                    return inMemory[i];
                }
            }

            return null;
        }

        public static void AssignIfNeeded(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            TMP_FontAsset font = GetDefault();
            if (font != null)
            {
                text.font = font;
            }
        }
    }
}
