using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GlobalDomination.GameData;

namespace GlobalDomination.UI
{
    public struct CurrentTurnHeaderSettings
    {
        public bool forceTopCenterForCurrentPlayer;
        public bool useCardStyle;
        public bool useTopHeaderCard;

        public float topOffset;
        public float headerFontSize;
        public float countryFontSize;

        public float standaloneFlagXOffset;
        public float standaloneFlagYOffset;
        public float standaloneHeaderTextXOffset;
        public Vector2 standaloneFlagSize;

        public Sprite topCardSprite;
        public Color topCardColor;
        public Color cardBorderColor;
        public Color cardShadowColor;
        public Vector2 topCardPadding;
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
            currentPlayerText.alignment = TextAlignmentOptions.Center;
            currentPlayerText.textWrappingMode = TextWrappingModes.NoWrap;

            if (!settings.forceTopCenterForCurrentPlayer)
            {
                return;
            }

            RectTransform rect = currentPlayerText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            if (!settings.useTopHeaderCard)
            {
                rect.anchoredPosition = new Vector2(settings.standaloneHeaderTextXOffset, -settings.topOffset);
                currentPlayerText.alignment = TextAlignmentOptions.Left;
                return;
            }

            rect.anchoredPosition = new Vector2(0f, -settings.topOffset);
            currentPlayerText.alignment = TextAlignmentOptions.Center;
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

            ConfigureFlagPlacement();
        }

        public void UpdatePlayer(Player currentPlayer)
        {
            if (currentPlayerText == null || currentPlayer == null)
            {
                return;
            }

            CountryData countryData = CountryDatabase.GetCountryData(currentPlayer.selectedCountry);
            string countryName = countryData != null ? countryData.countryName : currentPlayer.selectedCountry.ToString();

            currentPlayerText.text =
                $"<size=18><b><color=#9DC5E8>CURRENT TURN</color></b></size>\n" +
                $"<size={settings.headerFontSize}><b><color=#F4D35E>{currentPlayer.playerName}</color></b></size>\n" +
                $"<size={settings.countryFontSize}><color=#8ECAE6>{countryName}</color></size>";

            SetFlagImage(resolveFlag != null ? resolveFlag(currentPlayer.selectedCountry) : null);
        }

        public void Clear()
        {
            if (currentPlayerText != null)
            {
                currentPlayerText.text = "No game active";
            }

            SetFlagImage(null);
        }

        private void ConfigureFlagPlacement()
        {
            if (currentPlayerFlagImage == null)
            {
                return;
            }

            RectTransform flagRect = currentPlayerFlagImage.rectTransform;
            if (settings.useCardStyle && settings.useTopHeaderCard && currentPlayerCardBackground != null)
            {
                flagRect.SetParent(currentPlayerCardBackground.transform, false);
                flagRect.anchorMin = new Vector2(0f, 0.5f);
                flagRect.anchorMax = new Vector2(0f, 0.5f);
                flagRect.pivot = new Vector2(0f, 0.5f);
                flagRect.anchoredPosition = new Vector2(14f, 0f);
                flagRect.sizeDelta = new Vector2(56f, 36f);
                currentPlayerFlagImage.preserveAspect = true;
                return;
            }

            RectTransform headerRect = currentPlayerText != null ? currentPlayerText.rectTransform : null;
            Transform parent = headerRect != null ? headerRect.parent : null;
            if (parent != null)
            {
                flagRect.SetParent(parent, false);
            }

            flagRect.anchorMin = new Vector2(0.5f, 1f);
            flagRect.anchorMax = new Vector2(0.5f, 1f);
            flagRect.pivot = new Vector2(0.5f, 0.5f);
            flagRect.anchoredPosition = new Vector2(settings.standaloneFlagXOffset, settings.standaloneFlagYOffset);
            flagRect.sizeDelta = settings.standaloneFlagSize;
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
