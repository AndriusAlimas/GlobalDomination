using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GlobalDomination.GameData;

namespace GlobalDomination.UI
{
    public struct CurrentTurnHeaderSettings
    {
        public bool useCardStyle;
        public bool useTopHeaderCard;

        public float topOffset;
        public float headerFontSize;
        public float countryFontSize;

        public Sprite topCardSprite;
        public Color topCardColor;
        public Color cardBorderColor;
        public Color cardShadowColor;
        public Vector2 topCardPadding;

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

        private Image currentPlayerCardBackground;
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
            if (settings.useCardStyle && settings.useTopHeaderCard)
            {
                currentPlayerCardBackground = EnsureCardBackground(
                    currentPlayerText,
                    currentPlayerCardBackground,
                    "CurrentTurnCard",
                    settings.topCardSprite,
                    settings.topCardColor,
                    settings.topCardPadding,
                    settings.cardBorderColor,
                    settings.cardShadowColor);
            }

            if (currentPlayerCardBackground != null)
            {
                currentPlayerCardBackground.gameObject.SetActive(settings.useCardStyle && settings.useTopHeaderCard);
            }

            ConfigurePlayerDetailsPlacement();
            ConfigureFlagPlacement();
        }

        public void UpdatePlayer(Player currentPlayer, int turnIteration)
        {
            if (currentPlayerText == null || currentPlayer == null)
            {
                return;
            }

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

            currentPlayerDetailsText.richText = true;
            currentPlayerDetailsText.textWrappingMode = TextWrappingModes.NoWrap;
            currentPlayerDetailsText.alignment = TextAlignmentOptions.MidlineRight;
            currentPlayerDetailsText.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                currentPlayerDetailsText.font = TMP_Settings.defaultFontAsset;
            }

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

            currentPlayerCountryText.richText = true;
            currentPlayerCountryText.textWrappingMode = TextWrappingModes.NoWrap;
            currentPlayerCountryText.alignment = TextAlignmentOptions.MidlineRight;
            currentPlayerCountryText.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                currentPlayerCountryText.font = TMP_Settings.defaultFontAsset;
            }
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

        private static Image EnsureCardBackground(
            TextMeshProUGUI text,
            Image existingCard,
            string cardObjectName,
            Sprite cardSprite,
            Color cardColor,
            Vector2 padding,
            Color borderColor,
            Color shadowColor)
        {
            if (text == null)
            {
                return existingCard;
            }

            Image card = existingCard;
            if (card == null)
            {
                Transform parent = text.transform.parent;
                if (parent == null)
                {
                    return null;
                }

                Transform cardTransform = parent.Find(cardObjectName);
                if (cardTransform != null)
                {
                    card = cardTransform.GetComponent<Image>();
                }

                if (card == null)
                {
                    GameObject cardObject = new GameObject(cardObjectName);
                    cardObject.transform.SetParent(parent, false);
                    card = cardObject.AddComponent<Image>();
                    cardObject.AddComponent<Outline>();
                    cardObject.AddComponent<Shadow>();
                }
            }

            RectTransform cardRect = card.rectTransform;
            RectTransform textRect = text.rectTransform;

            cardRect.anchorMin = textRect.anchorMin;
            cardRect.anchorMax = textRect.anchorMax;
            cardRect.pivot = textRect.pivot;
            cardRect.anchoredPosition = textRect.anchoredPosition;
            cardRect.sizeDelta = textRect.sizeDelta + new Vector2(padding.x * 2f, padding.y * 2f);

            card.sprite = cardSprite;
            card.type = cardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            card.color = cardColor;
            card.raycastTarget = false;

            Outline border = card.GetComponent<Outline>();
            if (border != null)
            {
                border.effectColor = borderColor;
                border.effectDistance = new Vector2(1f, -1f);
                border.useGraphicAlpha = true;
            }

            Shadow shadow = card.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = shadowColor;
                shadow.effectDistance = new Vector2(3f, -3f);
                shadow.useGraphicAlpha = true;
            }

            card.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
            return card;
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
