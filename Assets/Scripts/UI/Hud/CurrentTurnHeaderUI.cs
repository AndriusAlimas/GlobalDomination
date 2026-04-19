using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GlobalDomination.GameData;
using GlobalDomination.UI;

namespace GlobalDomination.UI.Hud
{
    public struct CurrentTurnHeaderSettings
    {
        public float topOffset;
        public float headerFontSize;
        public float countryFontSize;

        // Player HUD right-corner layout — all tweakable live in the Inspector
        public float hudRightMargin;
        public float hudFlagWidth;
        public float hudFlagHeight;
        public float hudFlagGap;
        public float hudTextWidth;
        public float hudBlockHeight;
        // Independent per-line offsets (X = left/right, Y = up/down)
        public Vector2 hudPlayerNameOffset;
        public Vector2 hudCountryOffset;
    }

    /// <summary>
    /// Handles layout and rendering for the top current-turn header.
    /// </summary>
    public sealed class CurrentTurnHeaderUI
    {
        private readonly TextMeshProUGUI currentPlayerText;
        private readonly Image currentPlayerFlagImage;
        private readonly Func<CountryType, Sprite> resolveFlag;
        private readonly CurrentTurnHeaderSettings settings;

        private TextMeshProUGUI currentPlayerDetailsText;
        private TextMeshProUGUI currentPlayerCountryText;

        public CurrentTurnHeaderUI(
            TextMeshProUGUI currentPlayerText,
            Image currentPlayerFlagImage,
            Func<CountryType, Sprite> resolveFlag,
            CurrentTurnHeaderSettings settings)
        {
            this.currentPlayerText = currentPlayerText;
            this.currentPlayerFlagImage = currentPlayerFlagImage;
            this.resolveFlag = resolveFlag;
            this.settings = settings;
        }

        public void ConfigureStyle()
        {
            if (currentPlayerText == null)
            {
                return;
            }

            TmpFontResolve.AssignIfNeeded(currentPlayerText);
            currentPlayerText.richText = true;
            currentPlayerText.textWrappingMode = TextWrappingModes.NoWrap;
            currentPlayerText.alignment = TextAlignmentOptions.Center;

            RectTransform rect = currentPlayerText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -settings.topOffset);

            EnsurePlayerDetailsText();
            ConfigurePlayerDetailsPlacement();
        }

        public void ApplyVisuals()
        {
            ConfigurePlayerDetailsPlacement();
            ConfigureFlagPlacement();
        }

        public void UpdatePlayer(Player currentPlayer, int turnIteration)
        {
            if (currentPlayerText == null || currentPlayer == null)
            {
                return;
            }

            TmpFontResolve.AssignIfNeeded(currentPlayerText);
            CountryData countryData = CountryDatabase.GetCountryData(currentPlayer.selectedCountry);
            string countryName = countryData != null ? countryData.countryName : currentPlayer.selectedCountry.ToString();

            EnsurePlayerDetailsText();

            float playerNameSize = Mathf.Clamp(settings.headerFontSize * 0.72f, 20f, 28f);
            float playerCountrySize = Mathf.Clamp(settings.countryFontSize * 0.78f, 15f, 22f);
            int safeTurn = Mathf.Max(1, turnIteration);

            currentPlayerText.text =
                $"<size=16><b><color=#8FB6D8>TURN</color></b></size> " +
                $"<size=30><b><color=#F4D35E>{safeTurn}</color></b></size>";

            if (currentPlayerDetailsText != null)
            {
                currentPlayerDetailsText.text =
                    $"<size={playerNameSize}><b><color=#F4D35E>{currentPlayer.playerName}</color></b></size>";
            }

            if (currentPlayerCountryText != null)
            {
                currentPlayerCountryText.text =
                    $"<size={playerCountrySize}><color=#8ECAE6>{countryName}</color></size>";
            }

            SetFlagImage(resolveFlag != null ? resolveFlag(currentPlayer.selectedCountry) : null);
        }

        public void Clear()
        {
            if (currentPlayerText != null)
            {
                currentPlayerText.text = "No game active";
            }

            if (currentPlayerDetailsText != null)
            {
                currentPlayerDetailsText.text = string.Empty;
            }

            if (currentPlayerCountryText != null)
            {
                currentPlayerCountryText.text = string.Empty;
            }

            SetFlagImage(null);
        }

        private void EnsurePlayerDetailsText()
        {
            if (currentPlayerText == null || currentPlayerDetailsText != null)
            {
                return;
            }

            Transform parent = currentPlayerText.transform.parent;
            if (parent == null)
            {
                return;
            }

            Transform existing = parent.Find("CurrentPlayerDetailsText");
            if (existing != null)
            {
                currentPlayerDetailsText = existing.GetComponent<TextMeshProUGUI>();
            }

            if (currentPlayerDetailsText == null)
            {
                GameObject detailsObject = new GameObject("CurrentPlayerDetailsText");
                detailsObject.transform.SetParent(parent, false);
                currentPlayerDetailsText = detailsObject.AddComponent<TextMeshProUGUI>();
            }

            TmpFontResolve.AssignIfNeeded(currentPlayerDetailsText);
            currentPlayerDetailsText.richText = true;
            currentPlayerDetailsText.textWrappingMode = TextWrappingModes.NoWrap;
            currentPlayerDetailsText.alignment = TextAlignmentOptions.MidlineRight;
            currentPlayerDetailsText.raycastTarget = false;

            // Country text element
            Transform existingCountry = parent.Find("CurrentPlayerCountryText");
            if (existingCountry != null)
            {
                currentPlayerCountryText = existingCountry.GetComponent<TextMeshProUGUI>();
            }

            if (currentPlayerCountryText == null)
            {
                GameObject countryObject = new GameObject("CurrentPlayerCountryText");
                countryObject.transform.SetParent(parent, false);
                currentPlayerCountryText = countryObject.AddComponent<TextMeshProUGUI>();
            }

            TmpFontResolve.AssignIfNeeded(currentPlayerCountryText);
            currentPlayerCountryText.richText = true;
            currentPlayerCountryText.textWrappingMode = TextWrappingModes.NoWrap;
            currentPlayerCountryText.alignment = TextAlignmentOptions.MidlineRight;
            currentPlayerCountryText.raycastTarget = false;
        }

        private void ConfigurePlayerDetailsPlacement()
        {
            if (currentPlayerDetailsText == null)
            {
                return;
            }

            float centerY = -settings.topOffset - settings.hudBlockHeight * 0.5f;
            float rightEdgeX = -(settings.hudRightMargin + settings.hudFlagWidth + settings.hudFlagGap);
            float leftEdgeX = rightEdgeX - settings.hudTextWidth;

            RectTransform detailsRect = currentPlayerDetailsText.rectTransform;
            detailsRect.anchorMin = new Vector2(1f, 1f);
            detailsRect.anchorMax = new Vector2(1f, 1f);
            detailsRect.pivot     = new Vector2(0f, 0.5f);
            detailsRect.anchoredPosition = new Vector2(leftEdgeX + settings.hudPlayerNameOffset.x, centerY + settings.hudBlockHeight * 0.25f + settings.hudPlayerNameOffset.y);
            detailsRect.sizeDelta = new Vector2(settings.hudTextWidth, settings.hudBlockHeight);
            currentPlayerDetailsText.alignment = TextAlignmentOptions.MidlineLeft;

            if (currentPlayerCountryText != null)
            {
                RectTransform countryRect = currentPlayerCountryText.rectTransform;
                countryRect.anchorMin = new Vector2(1f, 1f);
                countryRect.anchorMax = new Vector2(1f, 1f);
                countryRect.pivot     = new Vector2(0f, 0.5f);
                countryRect.anchoredPosition = new Vector2(leftEdgeX + settings.hudCountryOffset.x, centerY - settings.hudBlockHeight * 0.25f + settings.hudCountryOffset.y);
                countryRect.sizeDelta = new Vector2(settings.hudTextWidth, settings.hudBlockHeight * 0.5f);
                currentPlayerCountryText.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }

        private void ConfigureFlagPlacement()
        {
            if (currentPlayerFlagImage == null)
            {
                return;
            }

            RectTransform flagRect = currentPlayerFlagImage.rectTransform;
            RectTransform headerRect = currentPlayerText != null ? currentPlayerText.rectTransform : null;
            Transform parent = headerRect != null ? headerRect.parent : null;
            if (parent != null)
            {
                flagRect.SetParent(parent, false);
            }

            float centerY = -settings.topOffset - settings.hudBlockHeight * 0.5f;

            flagRect.anchorMin = new Vector2(1f, 1f);
            flagRect.anchorMax = new Vector2(1f, 1f);
            flagRect.pivot     = new Vector2(1f, 0.5f);
            flagRect.anchoredPosition = new Vector2(-settings.hudRightMargin, centerY + 4f);
            flagRect.sizeDelta = new Vector2(settings.hudFlagWidth, settings.hudFlagHeight);
            currentPlayerFlagImage.preserveAspect = true;
        }

        private void SetFlagImage(Sprite flag)
        {
            if (currentPlayerFlagImage == null)
            {
                return;
            }

            currentPlayerFlagImage.sprite = flag;
            currentPlayerFlagImage.enabled = flag != null;
        }
    }
}
