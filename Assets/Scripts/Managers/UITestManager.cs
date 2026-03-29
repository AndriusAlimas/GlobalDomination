using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GlobalDomination.GameData;
using GlobalDomination.UI;

namespace GlobalDomination.Managers
{
    /// <summary>
    /// Test harness for game actions plus lightweight UI orchestration.
    /// </summary>
    public class UITestManager : MonoBehaviour
    {
        private const float DefaultTopOffset = 24f;
        private const float DefaultHeaderFontSize = 36f;
        private const float DefaultCountryFontSize = 24f;
        private const float DefaultHudRightMargin = 28f;
        private const float DefaultHudFlagWidth = 103.1f;
        private const float DefaultHudFlagHeight = 66.6f;
        private const float DefaultHudFlagGap = 10f;
        private const float DefaultHudTextWidth = 190f;
        private const float DefaultHudBlockHeight = 72f;
        private static readonly Color EndTurnReadyColor = new Color(0.18f, 0.52f, 0.23f, 0.97f);
        private static readonly Color EndTurnPendingColor = new Color(0.72f, 0.4f, 0.08f, 0.97f);
        private static readonly Color EndTurnDisabledColor = new Color(0.28f, 0.3f, 0.34f, 0.94f);

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private Image currentPlayerFlagImage;
        [SerializeField] private TextMeshProUGUI instructionsText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TextMeshProUGUI endTurnButtonText;

        [Header("Top Header Style")]
        [SerializeField] private bool useTopHeaderCard = false;

        [Header("Player HUD Layout")]
        [SerializeField] private float hudRightMargin = 28f;
        [SerializeField] private float hudFlagGap = 10f;
        [SerializeField] private float hudTextWidth = 190f;
        [SerializeField] private float hudBlockHeight = 72f;
        [SerializeField] private Vector2 hudPlayerNameOffset = Vector2.zero;
        [SerializeField] private Vector2 hudCountryOffset = Vector2.zero;

        [Header("Card Visual Style")]
        [SerializeField] private bool useCardStyle = true;
        [SerializeField] private Sprite topCardSprite;
        [SerializeField] private Sprite sideCardSprite;
        [SerializeField] private Color topCardColor = new Color(0.07f, 0.12f, 0.2f, 0.8f);
        [SerializeField] private Color sideCardColor = new Color(0.05f, 0.09f, 0.16f, 0.76f);
        [SerializeField] private Color cardBorderColor = new Color(0.95f, 0.8f, 0.35f, 0.45f);
        [SerializeField] private Color cardShadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Vector2 topCardPadding = new Vector2(24f, 12f);
        [SerializeField] private Vector2 sideCardPadding = new Vector2(16f, 14f);

        [Header("Settings")]
        [SerializeField] private bool autoInitializeGame = true;
        [SerializeField] private bool showHelpOnStart = false;
        [SerializeField] private KeyCode toggleHelpKey = KeyCode.H;
        [SerializeField] private bool showStartupStatRollReveal = true;
        [SerializeField] private bool devShowSkipStartupButton = false;
        [SerializeField] private float startupStatSpinDuration = 1.7f;
        [SerializeField] private float startupAutoNextSeconds = 5f;

        private GameManager gameManager;
        private CurrentTurnHeaderUI currentTurnHeaderUI;
        private CitiesDisplayManager citiesDisplayManager;

        private Image instructionsCardBackground;

        private readonly System.Collections.Generic.Dictionary<CountryType, Sprite> generatedFlags =
            new System.Collections.Generic.Dictionary<CountryType, Sprite>();
        private Sprite[] startupDiceFaceSprites;
        private Sprite startupTransferBadgeSprite;

        private CurrentTurnHeaderSettings appliedHeaderSettings;
        private bool headerSettingsInitialized;
        private bool isHelpVisible;
        private int turnIteration = 1;
        private bool startupRevealInProgress;
        private Coroutine initializeGameCoroutine;
        private Coroutine startupRevealCoroutine;
        private Button devSkipStartupButton;
        private GameObject startupRevealOverlayObj;

        private void Reset()
        {
            ApplyPlayerHudDefaults();
        }

        [ContextMenu("Apply Player HUD Defaults")]
        private void ApplyPlayerHudDefaults()
        {
            hudRightMargin = DefaultHudRightMargin;
            hudFlagGap = DefaultHudFlagGap;
            hudTextWidth = DefaultHudTextWidth;
            hudBlockHeight = DefaultHudBlockHeight;
            hudPlayerNameOffset = Vector2.zero;
            hudCountryOffset = Vector2.zero;

            headerSettingsInitialized = false;

            if (Application.isPlaying)
            {
                BuildCurrentTurnHeaderPresenterIfNeeded(true);
                UpdateDisplay();
            }
        }

        private void Start()
        {
            EnsureUIReferences();
            EnsureEndTurnButton();
            EnsureDevSkipButton();
            BuildCurrentTurnHeaderPresenterIfNeeded(true);
            SetupInstructions();
            isHelpVisible = showHelpOnStart;
            ApplyHelpVisibility();
            BuildCurrentTurnHeaderPresenterIfNeeded(false);
            UpdateCardLayouts();
            RefreshEndTurnButtonState();

            if (autoInitializeGame || devShowSkipStartupButton)
            {
                InitializeGame();
            }
        }

        private void BuildCurrentTurnHeaderPresenterIfNeeded(bool forceRebuild)
        {
            CurrentTurnHeaderSettings currentSettings = GetCurrentHeaderSettings();
            if (!forceRebuild && headerSettingsInitialized && HeaderSettingsEqual(currentSettings, appliedHeaderSettings))
            {
                return;
            }

            currentTurnHeaderUI = new CurrentTurnHeaderUI(
                currentPlayerText,
                currentPlayerFlagImage,
                ResolveFlagForCountry,
                currentSettings);

            currentTurnHeaderUI.ConfigureStyle();
            currentTurnHeaderUI.ApplyVisuals();

            // Re-apply player text so the display stays populated after a live settings change.
            if (gameManager != null && gameManager.players.Count > 0)
            {
                currentTurnHeaderUI.UpdatePlayer(gameManager.GetCurrentPlayer(), turnIteration);
            }

            appliedHeaderSettings = currentSettings;
            headerSettingsInitialized = true;
        }

        private CurrentTurnHeaderSettings GetCurrentHeaderSettings()
        {
            return new CurrentTurnHeaderSettings
            {
                useCardStyle = useCardStyle,
                useTopHeaderCard = useTopHeaderCard,
                topOffset = DefaultTopOffset,
                headerFontSize = DefaultHeaderFontSize,
                countryFontSize = DefaultCountryFontSize,
                topCardSprite = topCardSprite,
                topCardColor = topCardColor,
                cardBorderColor = cardBorderColor,
                cardShadowColor = cardShadowColor,
                topCardPadding = topCardPadding,
                hudRightMargin = hudRightMargin,
                hudFlagWidth = DefaultHudFlagWidth,
                hudFlagHeight = DefaultHudFlagHeight,
                hudFlagGap = hudFlagGap,
                hudTextWidth = hudTextWidth,
                hudBlockHeight = hudBlockHeight
                ,hudPlayerNameOffset = hudPlayerNameOffset,
                hudCountryOffset = hudCountryOffset
            };
        }

        private static bool HeaderSettingsEqual(CurrentTurnHeaderSettings a, CurrentTurnHeaderSettings b)
        {
            return a.useCardStyle == b.useCardStyle
                && a.useTopHeaderCard == b.useTopHeaderCard
                && a.topCardSprite == b.topCardSprite
                && a.topCardColor == b.topCardColor
                && a.cardBorderColor == b.cardBorderColor
                && a.cardShadowColor == b.cardShadowColor
                && a.topCardPadding == b.topCardPadding
                && Mathf.Approximately(a.hudRightMargin, b.hudRightMargin)
                && Mathf.Approximately(a.hudFlagGap, b.hudFlagGap)
                && Mathf.Approximately(a.hudTextWidth, b.hudTextWidth)
                && Mathf.Approximately(a.hudBlockHeight, b.hudBlockHeight)
                && a.hudPlayerNameOffset == b.hudPlayerNameOffset
                && a.hudCountryOffset == b.hudCountryOffset;
        }

        private void EnsureUIReferences()
        {
            EnsureEventSystem();

            bool missingAnyReference = currentPlayerText == null || instructionsText == null;
            if (!missingAnyReference)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                canvas = RuntimeUiCanvasHelper.CreateScreenSpaceOverlayCanvas("RuntimeGameUICanvas");
            }

            if (currentPlayerText == null)
            {
                currentPlayerText = CreateTextElement(
                    "CurrentPlayerText",
                    canvas.transform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -DefaultTopOffset),
                    new Vector2(560f, 120f),
                    32f,
                    TextAlignmentOptions.Center,
                    Color.white);
            }

            if (currentPlayerFlagImage == null)
            {
                GameObject flagObject = new GameObject("CurrentPlayerFlag");
                flagObject.transform.SetParent(canvas.transform, false);

                RectTransform flagRect = flagObject.AddComponent<RectTransform>();
                flagRect.anchorMin = new Vector2(0.5f, 1f);
                flagRect.anchorMax = new Vector2(0.5f, 1f);
                flagRect.pivot = new Vector2(0.5f, 1f);
                flagRect.anchoredPosition = new Vector2(-220f, -DefaultTopOffset);
                flagRect.sizeDelta = new Vector2(56f, 36f);

                currentPlayerFlagImage = flagObject.AddComponent<Image>();
                currentPlayerFlagImage.enabled = false;
            }
            
            // Create Cities Display Manager
            if (citiesDisplayManager == null)
            {
                citiesDisplayManager = CitiesDisplayManager.CreateCitiesDisplay(canvas);
            }

            if (instructionsText == null)
            {
                instructionsText = CreateTextElement(
                    "InstructionsText",
                    canvas.transform,
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(-20f, 20f),
                    new Vector2(420f, 240f),
                    16f,
                    TextAlignmentOptions.BottomRight,
                    new Color(0.85f, 0.9f, 1f, 1f));
            }
        }

        private void EnsureEventSystem()
        {
            EventSystem existingEventSystem = FindFirstObjectByType<EventSystem>();
            if (existingEventSystem != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

            // Prefer Input System UI module when package is present, otherwise use legacy module.
            System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                eventSystemObject.AddComponent(inputSystemModuleType);
            }
            else
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
        }

        private static TextMeshProUGUI CreateTextElement(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.raycastTarget = false;
            textComponent.richText = true;

            if (TMP_Settings.defaultFontAsset != null)
            {
                textComponent.font = TMP_Settings.defaultFontAsset;
            }

            return textComponent;
        }

        private void EnsureDevSkipButton()
        {
            if (!devShowSkipStartupButton)
            {
                return;
            }

            if (devSkipStartupButton != null)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            GameObject buttonObject = new GameObject("DevSkipStartupButton");
            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(12f, -12f);
            buttonRect.sizeDelta = new Vector2(190f, 50f);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.55f, 0.12f, 0.12f, 0.92f);

            devSkipStartupButton = buttonObject.AddComponent<Button>();
            ColorBlock colors = devSkipStartupButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.85f, 0.85f, 1f);
            colors.pressedColor = new Color(0.8f, 0.6f, 0.6f, 1f);
            devSkipStartupButton.colors = colors;
            devSkipStartupButton.onClick.AddListener(OnDevSkipStartupPressed);

            TextMeshProUGUI label = CreateTextElement(
                "DevSkipStartupButtonText",
                buttonObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                buttonRect.sizeDelta,
                18f,
                TextAlignmentOptions.Center,
                Color.white);
            label.text = "[DEV] Skip Startup";
        }

        private void OnDevSkipStartupPressed()
        {
            if (!startupRevealInProgress)
            {
                return;
            }

            // StopAllCoroutines kills every nested coroutine (including the 3D dice roll
            // started inside PlayStartupStatRollReveal) so nothing re-disables canvases
            // or the camera after we restore them.
            StopAllCoroutines();
            startupRevealCoroutine = null;
            initializeGameCoroutine = null;

            if (startupRevealOverlayObj != null)
            {
                Destroy(startupRevealOverlayObj);
                startupRevealOverlayObj = null;
            }

            startupRevealInProgress = false;
            BuildCityRollSceneScope.ForceReset();
            RevealVisibleCityBadgeNumbers();
            UpdateDisplay();
        }

        private void EnsureEndTurnButton()
        {
            if (endTurnButton == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    canvas = RuntimeUiCanvasHelper.CreateScreenSpaceOverlayCanvas("RuntimeGameUICanvas");
                }

                GameObject buttonObject = new GameObject("EndTurnButton");
                buttonObject.transform.SetParent(canvas.transform, false);

                RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(1f, 0f);
                buttonRect.anchorMax = new Vector2(1f, 0f);
                buttonRect.pivot = new Vector2(1f, 0f);
                buttonRect.anchoredPosition = new Vector2(-22f, 22f);
                buttonRect.sizeDelta = new Vector2(270f, 68f);

                Image buttonImage = buttonObject.AddComponent<Image>();
                buttonImage.color = EndTurnPendingColor;

                Outline border = buttonObject.AddComponent<Outline>();
                border.effectColor = new Color(0f, 0f, 0f, 0.45f);
                border.effectDistance = new Vector2(2f, -2f);
                border.useGraphicAlpha = true;

                endTurnButton = buttonObject.AddComponent<Button>();
                ColorBlock colors = endTurnButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.94f, 0.97f, 1f, 1f);
                colors.pressedColor = new Color(0.82f, 0.9f, 1f, 1f);
                colors.disabledColor = new Color(0.68f, 0.68f, 0.68f, 0.7f);
                endTurnButton.colors = colors;

                endTurnButtonText = CreateTextElement(
                    "EndTurnButtonText",
                    buttonObject.transform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    buttonRect.sizeDelta,
                    24f,
                    TextAlignmentOptions.Center,
                    Color.white);
            }

            if (endTurnButtonText == null && endTurnButton != null)
            {
                endTurnButtonText = endTurnButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(OnEndTurnButtonPressed);
                endTurnButton.onClick.AddListener(OnEndTurnButtonPressed);
            }
        }

        private void OnEndTurnButtonPressed()
        {
            NextTurn();
        }

        private void RefreshEndTurnButtonState()
        {
            if (endTurnButton == null)
            {
                return;
            }

            if (BuildCityRollSceneScope.IsRollInProgress || startupRevealInProgress)
            {
                SetEndTurnButtonVisual("Rolling...", false, EndTurnDisabledColor, new Color(0.9f, 0.9f, 0.9f, 1f));
                return;
            }

            if (gameManager == null || gameManager.players == null || gameManager.players.Count == 0)
            {
                SetEndTurnButtonVisual("End Turn", false, EndTurnDisabledColor, new Color(0.86f, 0.9f, 0.98f, 1f));
                return;
            }

            Player currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.ownedCities == null || currentPlayer.ownedCities.Count == 0)
            {
                SetEndTurnButtonVisual("End Turn", true, EndTurnReadyColor, Color.white);
                return;
            }

            int remainingCities = 0;
            for (int i = 0; i < currentPlayer.ownedCities.Count; i++)
            {
                City city = currentPlayer.ownedCities[i];
                if (city != null && !city.hasTakenTurn)
                {
                    remainingCities++;
                }
            }

            if (remainingCities <= 0)
            {
                SetEndTurnButtonVisual("End Turn - Ready", true, EndTurnReadyColor, Color.white);
            }
            else
            {
                string label = remainingCities == 1
                    ? "End Turn (1 city left)"
                    : $"End Turn ({remainingCities} cities left)";

                SetEndTurnButtonVisual(label, true, EndTurnPendingColor, new Color(1f, 0.96f, 0.9f, 1f));
            }
        }

        private void SetEndTurnButtonVisual(string label, bool interactable, Color backgroundColor, Color textColor)
        {
            endTurnButton.interactable = interactable;

            Image background = endTurnButton.GetComponent<Image>();
            if (background != null)
            {
                background.color = backgroundColor;
            }

            if (endTurnButtonText != null)
            {
                endTurnButtonText.text = label;
                endTurnButtonText.color = textColor;
            }
        }

        private void SetupInstructions()
        {
            if (instructionsText == null)
            {
                return;
            }

            instructionsText.text = @"<b>GAME TEST CONTROLS</b>

<b>Keyboard:</b>
T - Initialize New Game
B - Roll for Building
N - Next Turn
P - Print to Console
R - Refresh Display
H - Toggle Help Panel

<b>UI Buttons:</b>
Use city actions, then press End Turn (bottom-right)

<b>Goal:</b>
Test the dice rolling system and game initialization";
        }

        public void InitializeGame()
        {
            if (initializeGameCoroutine != null)
            {
                StopCoroutine(initializeGameCoroutine);
                initializeGameCoroutine = null;
            }

            initializeGameCoroutine = StartCoroutine(InitializeGameRoutine());
        }

        private IEnumerator InitializeGameRoutine()
        {
            Debug.Log("=== Initializing Game ===");

            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                GameObject gmObject = new GameObject("GameManager");
                gameManager = gmObject.AddComponent<GameManager>();
            }

            gameManager.InitializeTestGame();
            turnIteration = 1;

            if (showStartupStatRollReveal || devShowSkipStartupButton)
            {
                // Show world/cities, but hide badge values until each roll reveals them.
                UpdateDisplay();
                HideVisibleCityBadges();
                startupRevealCoroutine = StartCoroutine(PlayStartupStatRollReveal());
                yield return startupRevealCoroutine;
                startupRevealCoroutine = null;
            }
            else
            {
                UpdateDisplay();
            }

            Debug.Log("Game initialized! Use buttons or keyboard to interact.");
            initializeGameCoroutine = null;
        }

        private IEnumerator PlayStartupStatRollReveal()
        {
            if (gameManager == null || gameManager.players == null || gameManager.players.Count == 0)
            {
                yield break;
            }

            Canvas canvas = currentPlayerText != null ? currentPlayerText.canvas : FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                yield break;
            }

            startupRevealInProgress = true;

            GameObject overlayObj = new GameObject("StartupRollRevealOverlay");
            startupRevealOverlayObj = overlayObj;
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.transform.SetAsLastSibling();

            // Keep the dev skip button always on top of the overlay.
            if (devSkipStartupButton != null)
            {
                devSkipStartupButton.transform.SetAsLastSibling();
            }

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(0.02f, 0.06f, 0.12f, 0.86f);
            overlayImage.raycastTarget = true;

            GameObject panelObj = new GameObject("StartupRollPanel");
            panelObj.transform.SetParent(overlayObj.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(760f, 560f);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.09f, 0.16f, 0.25f, 0.94f);

            TextMeshProUGUI titleText = CreateTextElement(
                "StartupRollTitle",
                panelObj.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -36f),
                new Vector2(700f, 60f),
                28f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.93f, 0.57f, 1f));

            TextMeshProUGUI subtitleText = CreateTextElement(
                "StartupRollSubtitle",
                panelObj.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -92f),
                new Vector2(700f, 72f),
                52f,
                TextAlignmentOptions.Center,
                new Color(0.82f, 0.91f, 1f, 1f));
            subtitleText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI statNameText = CreateTextElement(
                "StatName",
                panelObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 106f),
                new Vector2(660f, 52f),
                28f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.93f, 0.57f, 1f));
            statNameText.outlineWidth = 0.2f;
            statNameText.outlineColor = new Color(1f, 1f, 1f, 0.95f);
            statNameText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI rollHeaderText = CreateTextElement(
                "RollHeader",
                panelObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f),
                new Vector2(660f, 38f),
                19f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.93f, 0.57f, 1f));
            rollHeaderText.outlineWidth = 0.2f;
            rollHeaderText.outlineColor = new Color(1f, 1f, 1f, 0.9f);

            Image die1Image = CreateDiceSlotImage("Die1Image", panelObj.transform, new Vector2(-110f, -34f), new Vector2(80f, 80f));
            Image die2Image = CreateDiceSlotImage("Die2Image", panelObj.transform, new Vector2(0f, -34f), new Vector2(80f, 80f));
            Image die3Image = CreateDiceSlotImage("Die3Image", panelObj.transform, new Vector2(110f, -34f), new Vector2(80f, 80f));

            TextMeshProUGUI formulaText = CreateTextElement(
                "FormulaText",
                panelObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -190f),
                new Vector2(660f, 58f),
                26f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.93f, 0.57f, 1f));
            formulaText.outlineWidth = 0.2f;
            formulaText.outlineColor = new Color(1f, 1f, 1f, 0.92f);

            GameObject transferBadgeObj = new GameObject("TransferBadge");
            transferBadgeObj.transform.SetParent(panelObj.transform, false);
            Image transferBadgeImage = transferBadgeObj.AddComponent<Image>();
            transferBadgeImage.sprite = GetOrCreateStartupTransferBadgeSprite();
            transferBadgeImage.color = new Color(1f, 1f, 1f, 0f);

            RectTransform transferBadgeRect = transferBadgeObj.GetComponent<RectTransform>();
            transferBadgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            transferBadgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            transferBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            transferBadgeRect.anchoredPosition = new Vector2(0f, -178f);
            transferBadgeRect.sizeDelta = new Vector2(140f, 76f);

            TextMeshProUGUI transferBadgeText = CreateTextElement(
                "TransferBadgeText",
                transferBadgeObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(132f, 66f),
                30f,
                TextAlignmentOptions.Center,
                new Color(0.08f, 0.1f, 0.16f, 1f));
            transferBadgeText.outlineWidth = 0.2f;
            transferBadgeText.outlineColor = new Color(1f, 1f, 1f, 0.85f);
            transferBadgeText.text = string.Empty;

            Button nextButton = CreateOverlayButton(
                "NextRollButton",
                panelObj.transform,
                new Vector2(0f, -246f),
                new Vector2(220f, 54f),
                "Next Roll",
                out TextMeshProUGUI nextButtonText);
            nextButton.gameObject.SetActive(false);

            TextMeshProUGUI countdownText = CreateTextElement(
                "CountdownText",
                panelObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -332f),
                new Vector2(760f, 44f),
                28f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.96f, 0.72f, 1f));
            countdownText.outlineWidth = 0.25f;
            countdownText.outlineColor = new Color(0f, 0f, 0f, 0.9f);

            GameObject countdownBgObj = new GameObject("CountdownBg");
            countdownBgObj.transform.SetParent(panelObj.transform, false);
            RectTransform countdownBgRect = countdownBgObj.AddComponent<RectTransform>();
            countdownBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            countdownBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            countdownBgRect.pivot = new Vector2(0.5f, 0.5f);
            countdownBgRect.anchoredPosition = new Vector2(0f, -332f);
            countdownBgRect.sizeDelta = new Vector2(820f, 56f);

            Image countdownBgImage = countdownBgObj.AddComponent<Image>();
            countdownBgImage.color = new Color(0f, 0f, 0f, 0.35f);
            countdownBgImage.raycastTarget = false;
            countdownBgObj.SetActive(false);
            countdownText.gameObject.SetActive(false);

            countdownBgObj.transform.SetSiblingIndex(Mathf.Max(0, countdownText.transform.GetSiblingIndex() - 1));

            rollHeaderText.text = "Each die roll";
            Image[] dieImages = { die1Image, die2Image, die3Image };

            for (int playerIndex = 0; playerIndex < gameManager.players.Count; playerIndex++)
            {
                Player player = gameManager.players[playerIndex];
                City capital = player != null ? player.GetCapitalCity() : null;
                if (capital == null)
                {
                    continue;
                }

                // Show the active player's city while their startup roll is being revealed.
                currentTurnHeaderUI?.UpdatePlayer(player, turnIteration);
                if (citiesDisplayManager != null && player.ownedCities != null)
                {
                    citiesDisplayManager.DisplayCities(player.ownedCities);
                }
                yield return null;

                titleText.text = $"{player.playerName} Startup Roll";
                subtitleText.text = capital.cityName;

                CityIconUI targetIcon = FindCityIconForCity(capital);
                targetIcon?.HideAllStatBadges();

                List<int> healthRolls = GetStartupRolls(capital.startingHealthRolls, 3, capital.healthPoints);
                List<int> moneyRolls = GetStartupRolls(capital.startingMoneyRolls, 2, capital.money);
                List<int> powerRolls = GetStartupRolls(capital.startingPowerRolls, 1, capital.cityPower);

                yield return StartCoroutine(AnimateStatBreakdown(statNameText, dieImages, formulaText, "HEALTH", healthRolls, capital.healthPoints, new Color(1f, 0.93f, 0.57f, 1f)));
                yield return StartCoroutine(AnimateRollBadgeTransferToCity(
                    overlayRect,
                    transferBadgeRect,
                    transferBadgeImage,
                    transferBadgeText,
                    targetIcon,
                    "HEALTH",
                    capital.healthPoints,
                    new Color(1f, 0.93f, 0.57f, 1f)));
                targetIcon?.RevealHealthBadgeNumber();
                yield return StartCoroutine(WaitForNextOrTimeout(nextButton, nextButtonText, countdownText, countdownBgObj, "Next Roll", startupAutoNextSeconds));

                yield return StartCoroutine(AnimateStatBreakdown(statNameText, dieImages, formulaText, "MONEY", moneyRolls, capital.money, new Color(0.2f, 0.95f, 0.35f, 1f)));
                yield return StartCoroutine(AnimateRollBadgeTransferToCity(
                    overlayRect,
                    transferBadgeRect,
                    transferBadgeImage,
                    transferBadgeText,
                    targetIcon,
                    "MONEY",
                    capital.money,
                    new Color(0.2f, 0.95f, 0.35f, 1f)));
                targetIcon?.RevealMoneyBadgeNumber();
                yield return StartCoroutine(WaitForNextOrTimeout(nextButton, nextButtonText, countdownText, countdownBgObj, "Next Roll", startupAutoNextSeconds));

                yield return StartCoroutine(AnimateStatBreakdown(statNameText, dieImages, formulaText, "POWER", powerRolls, capital.cityPower, new Color(1f, 0.2f, 0.2f, 1f)));
                yield return StartCoroutine(AnimateRollBadgeTransferToCity(
                    overlayRect,
                    transferBadgeRect,
                    transferBadgeImage,
                    transferBadgeText,
                    targetIcon,
                    "POWER",
                    capital.cityPower,
                    new Color(1f, 0.2f, 0.2f, 1f)));
                targetIcon?.RevealPowerBadgeNumber();
                yield return StartCoroutine(WaitForNextOrTimeout(nextButton, nextButtonText, countdownText, countdownBgObj, "Next Roll", startupAutoNextSeconds));

                // ── Roll for Building with 3D Dice ────────────────────────
                Building rolledBuilding = null;
                int buildingRollOne = 0;
                int buildingRollTwo = 0;
                yield return StartCoroutine(BuildCityRollSceneScope.Run(this,
                    (rollCanvas, rollCamera) => CityIconUI.PlayStartupBuildingRoll(
                        capital,
                        rollCanvas,
                        rollCamera,
                        (building, roll1, roll2) =>
                        {
                            rolledBuilding = building;
                            buildingRollOne = roll1;
                            buildingRollTwo = roll2;
                        })));

                string rolledBuildingName = rolledBuilding != null ? rolledBuilding.displayName : "No Building";
                rollHeaderText.text = "Starting Building Rolled";
                rollHeaderText.color = new Color(1f, 0.95f, 0.55f, 1f);
                statNameText.text = rolledBuildingName;
                statNameText.color = new Color(1f, 0.95f, 0.55f, 1f);
                formulaText.text = $"Rolls: {buildingRollOne}, {buildingRollTwo}";
                formulaText.color = new Color(1f, 0.95f, 0.55f, 1f);

                bool isLastPlayer = playerIndex == gameManager.players.Count - 1;
                string endStepLabel = isLastPlayer ? "Start Game" : "Next Player";
                yield return StartCoroutine(WaitForNextOrTimeout(nextButton, nextButtonText, countdownText, countdownBgObj, endStepLabel, startupAutoNextSeconds));
            }

            Destroy(overlayObj);
            startupRevealOverlayObj = null;
            startupRevealInProgress = false;
            RevealVisibleCityBadgeNumbers();
            UpdateDisplay();
        }

        private void HideVisibleCityBadges()
        {
            CityIconUI[] icons = FindObjectsByType<CityIconUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i]?.HideAllStatBadges();
            }
        }

        private void RevealVisibleCityBadgeNumbers()
        {
            CityIconUI[] icons = FindObjectsByType<CityIconUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i]?.RevealAllBadgeNumbers();
            }
        }

        private static CityIconUI FindCityIconForCity(City city)
        {
            if (city == null)
            {
                return null;
            }

            CityIconUI[] icons = FindObjectsByType<CityIconUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] != null && icons[i].LinkedCity == city)
                {
                    return icons[i];
                }
            }

            return null;
        }

        private IEnumerator AnimateStatBreakdown(TextMeshProUGUI statNameText, Image[] dieImages, TextMeshProUGUI formulaText, string statName, List<int> rolls, int total, Color statColor)
        {
            if (statNameText == null || dieImages == null || dieImages.Length < 3 || formulaText == null)
            {
                yield break;
            }

            statNameText.text = statName;
            statNameText.color = statColor;
            formulaText.color = statColor;
            formulaText.text = string.Empty;

            Sprite[] diceFaces = GetOrCreateStartupDiceFaceSprites();
            int visibleCount = Mathf.Clamp(rolls != null ? rolls.Count : 0, 1, 3);
            const float diceRowY = -34f;
            const float diceGapX = 112f;
            float startX = -((visibleCount - 1) * diceGapX) * 0.5f;

            for (int i = 0; i < dieImages.Length; i++)
            {
                if (dieImages[i] == null)
                {
                    continue;
                }

                bool active = i < visibleCount;
                dieImages[i].gameObject.SetActive(active);
                if (active)
                {
                    RectTransform dieRect = dieImages[i].rectTransform;
                    dieRect.anchoredPosition = new Vector2(startX + (i * diceGapX), diceRowY);
                    dieImages[i].sprite = diceFaces[1];
                }
            }

            for (int i = 0; i < visibleCount; i++)
            {
                int target = rolls[i];
                yield return StartCoroutine(AnimateSingleDieValue(dieImages[i], target, diceFaces));
            }

            formulaText.text = FormatRollFormula(rolls, total);
            yield return new WaitForSeconds(0.28f);
        }

        private IEnumerator AnimateSingleDieValue(Image dieImage, int finalValue, Sprite[] diceFaces)
        {
            if (dieImage == null || diceFaces == null || diceFaces.Length < 7)
            {
                yield break;
            }

            float elapsed = 0f;
            const float stepDuration = 0.07f;
            float singleDieSpinDuration = Mathf.Max(0.35f, startupStatSpinDuration * 0.65f);
            RectTransform dieRect = dieImage.rectTransform;
            Vector3 baseScale = dieRect.localScale;

            while (elapsed < singleDieSpinDuration)
            {
                dieImage.sprite = diceFaces[Random.Range(1, 7)];
                SetDiceSpinVisual(dieRect, elapsed, singleDieSpinDuration, baseScale);
                elapsed += stepDuration;
                yield return new WaitForSeconds(stepDuration);
            }

            for (int i = 0; i < 3; i++)
            {
                dieImage.sprite = diceFaces[Random.Range(1, 7)];
                SetDiceSpinVisual(dieRect, singleDieSpinDuration + (i * stepDuration), singleDieSpinDuration + (3f * stepDuration), baseScale);
                yield return new WaitForSeconds(0.04f);
            }

            dieImage.sprite = diceFaces[Mathf.Clamp(finalValue, 1, 6)];
            dieRect.localRotation = Quaternion.identity;
            dieRect.localScale = baseScale;

            float bounceTime = 0f;
            const float bounceDuration = 0.14f;
            while (bounceTime < bounceDuration)
            {
                bounceTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(bounceTime / bounceDuration);
                float bump = Mathf.Sin(t * Mathf.PI) * 0.14f;
                dieRect.localScale = baseScale * (1f + bump);
                yield return null;
            }

            dieRect.localScale = baseScale;
            yield return new WaitForSeconds(0.12f);
        }

        private IEnumerator AnimateRollBadgeTransferToCity(
            RectTransform overlayRect,
            RectTransform transferBadgeRect,
            Image transferBadgeImage,
            TextMeshProUGUI transferBadgeText,
            CityIconUI targetIcon,
            string statName,
            int value,
            Color statColor)
        {
            if (overlayRect == null || transferBadgeRect == null || transferBadgeImage == null || transferBadgeText == null)
            {
                yield break;
            }

            Vector2 startPos = new Vector2(0f, -178f);
            Vector2 targetLocalPos = startPos;
            bool hasTarget = TryResolveBadgeTargetLocalPosition(overlayRect, targetIcon, statName, out targetLocalPos);

            transferBadgeRect.anchoredPosition = startPos;
            transferBadgeRect.localScale = Vector3.one;
            transferBadgeImage.color = new Color(1f, 1f, 1f, 0.96f);
            transferBadgeText.text = value.ToString();
            transferBadgeText.color = statColor;

            if (!hasTarget)
            {
                yield return new WaitForSeconds(0.3f);
                transferBadgeImage.color = new Color(1f, 1f, 1f, 0f);
                transferBadgeText.text = string.Empty;
                yield break;
            }

            float elapsed = 0f;
            const float travelDuration = 0.6f;
            Vector2 controlPos = startPos + new Vector2(0f, 120f);

            while (elapsed < travelDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / travelDuration);

                // Quadratic Bezier curve for a nice arc.
                Vector2 p0 = startPos;
                Vector2 p1 = controlPos;
                Vector2 p2 = targetLocalPos;
                Vector2 curvePos = ((1 - t) * (1 - t) * p0) + (2 * (1 - t) * t * p1) + (t * t * p2);
                transferBadgeRect.anchoredPosition = curvePos;

                float scale = Mathf.Lerp(1f, 0.72f, t);
                transferBadgeRect.localScale = new Vector3(scale, scale, 1f);
                transferBadgeImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.96f, 0.65f, t));

                yield return null;
            }

            transferBadgeRect.anchoredPosition = targetLocalPos;
            transferBadgeRect.localScale = new Vector3(0.72f, 0.72f, 1f);
            yield return new WaitForSeconds(0.08f);

            transferBadgeImage.color = new Color(1f, 1f, 1f, 0f);
            transferBadgeText.text = string.Empty;
        }

        private static bool TryResolveBadgeTargetLocalPosition(
            RectTransform overlayRect,
            CityIconUI targetIcon,
            string statName,
            out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            if (overlayRect == null || targetIcon == null)
            {
                return false;
            }

            bool found = false;
            Vector3 targetWorld = Vector3.zero;

            if (statName == "HEALTH")
            {
                found = targetIcon.TryGetHealthBadgeWorldPosition(out targetWorld);
            }
            else if (statName == "MONEY")
            {
                found = targetIcon.TryGetMoneyBadgeWorldPosition(out targetWorld);
            }
            else if (statName == "POWER")
            {
                found = targetIcon.TryGetPowerBadgeWorldPosition(out targetWorld);
            }

            if (!found)
            {
                return false;
            }

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetWorld);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, screenPos, null, out localPoint);
        }

        private IEnumerator WaitForNextOrTimeout(Button nextButton, TextMeshProUGUI nextButtonText, TextMeshProUGUI countdownText, GameObject countdownBgObj, string buttonLabel, float autoSeconds)
        {
            if (nextButton == null)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, autoSeconds));
                yield break;
            }

            nextButton.gameObject.SetActive(true);
            if (countdownBgObj != null) countdownBgObj.SetActive(true);
            if (countdownText != null) countdownText.gameObject.SetActive(true);

            bool pressed = false;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => pressed = true);
            nextButton.interactable = true;

            if (nextButtonText != null)
            {
                nextButtonText.text = buttonLabel;
            }

            float timeout = Mathf.Max(0.5f, autoSeconds);
            float remaining = timeout;

            while (!pressed && remaining > 0f)
            {
                if (countdownText != null)
                {
                    countdownText.text = $"Auto continue in {Mathf.CeilToInt(remaining)}s";
                }

                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (countdownText != null)
            {
                countdownText.text = string.Empty;
                countdownText.gameObject.SetActive(false);
            }

            if (countdownBgObj != null) countdownBgObj.SetActive(false);
            nextButton.gameObject.SetActive(false);
        }

        private static Image CreateDiceSlotImage(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject imageObject = new GameObject(objectName);
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static Button CreateOverlayButton(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta, string label, out TextMeshProUGUI buttonText)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = sizeDelta;

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.17f, 0.34f, 0.52f, 0.96f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.9f, 1f, 1f);
            button.colors = colors;

            buttonText = CreateTextElement(
                objectName + "Text",
                buttonObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                sizeDelta,
                22f,
                TextAlignmentOptions.Center,
                Color.white);
            buttonText.text = label;

            return button;
        }

        private Sprite GetOrCreateStartupTransferBadgeSprite()
        {
            if (startupTransferBadgeSprite != null)
            {
                return startupTransferBadgeSprite;
            }

            const int width = 180;
            const int height = 96;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color body = new Color(0.92f, 0.96f, 1f, 0.96f);
            Color edge = new Color(0.36f, 0.45f, 0.58f, 0.98f);

            float rx = width * 0.48f;
            float ry = height * 0.43f;
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - center.x) / rx;
                    float ny = (y - center.y) / ry;
                    float dist = (nx * nx) + (ny * ny);

                    if (dist > 1f)
                    {
                        texture.SetPixel(x, y, clear);
                    }
                    else if (dist > 0.9f)
                    {
                        texture.SetPixel(x, y, edge);
                    }
                    else
                    {
                        texture.SetPixel(x, y, body);
                    }
                }
            }

            texture.Apply();
            startupTransferBadgeSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            return startupTransferBadgeSprite;
        }

        private Sprite[] GetOrCreateStartupDiceFaceSprites()
        {
            if (startupDiceFaceSprites != null && startupDiceFaceSprites.Length == 7)
            {
                return startupDiceFaceSprites;
            }

            Sprite[] faces = new Sprite[7];
            for (int i = 1; i <= 6; i++)
            {
                faces[i] = CreateDieFaceSprite(i);
            }

            startupDiceFaceSprites = faces;
            return startupDiceFaceSprites;
        }

        private static Sprite CreateDieFaceSprite(int value)
        {
            const int size = 96;
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

        private static void SetDiceSpinVisual(RectTransform dieRect, float elapsed, float duration, Vector3 baseScale)
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

        private static string FormatRollFormula(List<int> rolls, int total)
        {
            if (rolls == null || rolls.Count == 0)
            {
                return "No rolls";
            }

            string formula = string.Join(" + ", rolls);
            return $"{formula} = {total}";
        }

        private static List<int> GetStartupRolls(List<int> source, int expectedCount, int total)
        {
            List<int> result = new List<int>();
            if (source != null)
            {
                foreach (int roll in source)
                {
                    if (roll >= 1 && roll <= 6)
                    {
                        result.Add(roll);
                    }
                }
            }

            int sum = 0;
            for (int i = 0; i < result.Count; i++)
            {
                sum += result[i];
            }

            if (result.Count == expectedCount && sum == total)
            {
                return result;
            }

            return BuildFallbackRolls(expectedCount, total);
        }

        private static List<int> BuildFallbackRolls(int diceCount, int total)
        {
            List<int> rolls = new List<int>();
            int remainingTotal = total;

            for (int i = 0; i < diceCount; i++)
            {
                int remainingDiceAfterThis = diceCount - i - 1;
                int minRoll = Mathf.Max(1, remainingTotal - (remainingDiceAfterThis * 6));
                int maxRoll = Mathf.Min(6, remainingTotal - remainingDiceAfterThis);
                int roll = i == diceCount - 1 ? remainingTotal : Random.Range(minRoll, maxRoll + 1);

                rolls.Add(Mathf.Clamp(roll, 1, 6));
                remainingTotal -= roll;
            }

            return rolls;
        }

        public void RollForBuilding()
        {
            if (gameManager == null)
            {
                Debug.LogWarning("Game not initialized! Press T or click Initialize Game.");
                return;
            }

            Player currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.ownedCities.Count == 0)
            {
                Debug.LogWarning("No player or cities available!");
                return;
            }

            City capital = currentPlayer.GetCapitalCity();
            Building newBuilding = BuildingRollTable.RollForBuilding();

            if (newBuilding != null)
            {
                capital.AddBuilding(newBuilding);
                Debug.Log($"<color=green>{currentPlayer.playerName} rolled: {newBuilding.displayName}</color>");
            }
            else
            {
                Debug.Log($"<color=yellow>{currentPlayer.playerName} rolled: Nothing (empty slot)</color>");
            }

            UpdateDisplay();
        }

        public void NextTurn()
        {
            if (BuildCityRollSceneScope.IsRollInProgress)
            {
                Debug.LogWarning("Cannot advance turn while a dice roll is in progress.");
                return;
            }

            if (gameManager == null)
            {
                Debug.LogWarning("Game not initialized!");
                return;
            }

            CityIconUI.CloseActionMenu();
            int previousPlayerIndex = gameManager.currentPlayerIndex;
            gameManager.NextTurn();

            if (gameManager.players != null
                && gameManager.players.Count > 0
                && gameManager.currentPlayerIndex <= previousPlayerIndex)
            {
                turnIteration++;
            }

            UpdateDisplay();

            Player currentPlayer = gameManager.GetCurrentPlayer();
            Debug.Log($"<color=cyan>=== {currentPlayer.playerName}'s Turn ===</color>");
        }

        public void PrintGameState()
        {
            if (gameManager == null)
            {
                Debug.LogWarning("Game not initialized!");
                return;
            }

            gameManager.PrintGameState();
        }

        public void TestMultipleBuildingRolls()
        {
            Debug.Log("\n=== Testing 10 Building Rolls ===");

            var buildingCounts = new System.Collections.Generic.Dictionary<BuildingType, int>();
            int noneCount = 0;

            for (int i = 0; i < 10; i++)
            {
                int roll1 = DiceRoller.RollD6();
                int roll2 = DiceRoller.RollD6();
                Building building = BuildingRollTable.GetBuildingFromRoll(roll1, roll2);

                if (building != null)
                {
                    if (!buildingCounts.ContainsKey(building.type))
                    {
                        buildingCounts[building.type] = 0;
                    }

                    buildingCounts[building.type]++;
                    Debug.Log($"  Roll {i + 1}: [{roll1},{roll2}] = {building.displayName}");
                }
                else
                {
                    noneCount++;
                    Debug.Log($"  Roll {i + 1}: [{roll1},{roll2}] = <color=grey>Nothing</color>");
                }
            }

            Debug.Log($"\n<b>Summary:</b> {10 - noneCount} buildings, {noneCount} empty slots");
        }

        private void UpdateDisplay()
        {
            if (gameManager == null || gameManager.players.Count == 0)
            {
                
                if (citiesDisplayManager != null)
                {
                    citiesDisplayManager.ClearCityIcons();
                }

                currentTurnHeaderUI?.Clear();
                RefreshEndTurnButtonState();
                return;
            }

            Player currentPlayer = gameManager.GetCurrentPlayer();
            currentTurnHeaderUI?.UpdatePlayer(currentPlayer, turnIteration);

            // Update city icons display
            if (citiesDisplayManager != null && currentPlayer != null && currentPlayer.ownedCities != null)
            {
                citiesDisplayManager.DisplayCities(currentPlayer.ownedCities);
            }

            RefreshEndTurnButtonState();
        }

        public void RefreshCurrentTurnDisplay()
        {
            UpdateDisplay();
        }

        public void PlayFoundedCityStartupReveal(Player player, City foundedCity)
        {
            if (player == null || foundedCity == null)
            {
                UpdateDisplay();
                return;
            }

            StartCoroutine(PlayFoundedCityStartupRevealRoutine(player, foundedCity));
        }

        private IEnumerator PlayFoundedCityStartupRevealRoutine(Player player, City foundedCity)
        {
            while (BuildCityRollSceneScope.IsRollInProgress || startupRevealInProgress)
            {
                yield return null;
            }

            Canvas canvas = currentPlayerText != null ? currentPlayerText.canvas : FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                UpdateDisplay();
                yield break;
            }

            startupRevealInProgress = true;

            GameObject overlayObj = new GameObject("FoundedCityStartupRevealOverlay");
            startupRevealOverlayObj = overlayObj;
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.transform.SetAsLastSibling();

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(0.02f, 0.06f, 0.12f, 0.86f);
            overlayImage.raycastTarget = true;

            GameObject panelObj = new GameObject("FoundedCityStartupRollPanel");
            panelObj.transform.SetParent(overlayObj.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(760f, 560f);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.09f, 0.16f, 0.25f, 0.94f);

            TextMeshProUGUI titleText = CreateTextElement(
                "FoundedCityRollTitle",
                panelObj.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -36f),
                new Vector2(700f, 60f),
                28f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.93f, 0.57f, 1f));

            TextMeshProUGUI subtitleText = CreateTextElement(
                "FoundedCityRollSubtitle",
                panelObj.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -92f),
                new Vector2(700f, 72f),
                52f,
                TextAlignmentOptions.Center,
                new Color(0.82f, 0.91f, 1f, 1f));
            subtitleText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI statNameText = CreateTextElement(
                "FoundedCityStatName",
                panelObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 106f),
                new Vector2(660f, 52f),
                28f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.93f, 0.57f, 1f));
            statNameText.outlineWidth = 0.2f;
            statNameText.outlineColor = new Color(1f, 1f, 1f, 0.95f);
            statNameText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI rollHeaderText = CreateTextElement(
                "FoundedCityRollHeader",
                panelObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f),
                new Vector2(660f, 38f),
                19f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.93f, 0.57f, 1f));
            rollHeaderText.outlineWidth = 0.2f;
            rollHeaderText.outlineColor = new Color(1f, 1f, 1f, 0.9f);

            Image die1Image = CreateDiceSlotImage("FoundedCityDie1Image", panelObj.transform, new Vector2(-110f, -34f), new Vector2(80f, 80f));
            Image die2Image = CreateDiceSlotImage("FoundedCityDie2Image", panelObj.transform, new Vector2(0f, -34f), new Vector2(80f, 80f));
            Image die3Image = CreateDiceSlotImage("FoundedCityDie3Image", panelObj.transform, new Vector2(110f, -34f), new Vector2(80f, 80f));

            TextMeshProUGUI formulaText = CreateTextElement(
                "FoundedCityFormulaText",
                panelObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -190f),
                new Vector2(660f, 58f),
                26f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.93f, 0.57f, 1f));
            formulaText.outlineWidth = 0.2f;
            formulaText.outlineColor = new Color(1f, 1f, 1f, 0.92f);

            GameObject transferBadgeObj = new GameObject("FoundedCityTransferBadge");
            transferBadgeObj.transform.SetParent(panelObj.transform, false);
            Image transferBadgeImage = transferBadgeObj.AddComponent<Image>();
            transferBadgeImage.sprite = GetOrCreateStartupTransferBadgeSprite();
            transferBadgeImage.color = new Color(1f, 1f, 1f, 0f);

            RectTransform transferBadgeRect = transferBadgeObj.GetComponent<RectTransform>();
            transferBadgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            transferBadgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            transferBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            transferBadgeRect.anchoredPosition = new Vector2(0f, -178f);
            transferBadgeRect.sizeDelta = new Vector2(140f, 76f);

            TextMeshProUGUI transferBadgeText = CreateTextElement(
                "FoundedCityTransferBadgeText",
                transferBadgeObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(132f, 66f),
                30f,
                TextAlignmentOptions.Center,
                new Color(0.08f, 0.1f, 0.16f, 1f));
            transferBadgeText.outlineWidth = 0.2f;
            transferBadgeText.outlineColor = new Color(1f, 1f, 1f, 0.85f);
            transferBadgeText.text = string.Empty;

            Button nextButton = CreateOverlayButton(
                "FoundedCityNextRollButton",
                panelObj.transform,
                new Vector2(0f, -246f),
                new Vector2(220f, 54f),
                "Next Roll",
                out TextMeshProUGUI nextButtonText);
            nextButton.gameObject.SetActive(false);

            TextMeshProUGUI countdownText = CreateTextElement(
                "FoundedCityCountdownText",
                panelObj.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -332f),
                new Vector2(760f, 44f),
                28f,
                TextAlignmentOptions.Center,
                new Color(1f, 0.96f, 0.72f, 1f));
            countdownText.outlineWidth = 0.25f;
            countdownText.outlineColor = new Color(0f, 0f, 0f, 0.9f);

            GameObject countdownBgObj = new GameObject("FoundedCityCountdownBg");
            countdownBgObj.transform.SetParent(panelObj.transform, false);
            RectTransform countdownBgRect = countdownBgObj.AddComponent<RectTransform>();
            countdownBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            countdownBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            countdownBgRect.pivot = new Vector2(0.5f, 0.5f);
            countdownBgRect.anchoredPosition = new Vector2(0f, -332f);
            countdownBgRect.sizeDelta = new Vector2(820f, 56f);

            Image countdownBgImage = countdownBgObj.AddComponent<Image>();
            countdownBgImage.color = new Color(0f, 0f, 0f, 0.35f);
            countdownBgImage.raycastTarget = false;
            countdownBgObj.SetActive(false);
            countdownText.gameObject.SetActive(false);
            countdownBgObj.transform.SetSiblingIndex(Mathf.Max(0, countdownText.transform.GetSiblingIndex() - 1));

            rollHeaderText.text = "Each die roll";
            titleText.text = $"{player.playerName} Founded New City";
            subtitleText.text = foundedCity.cityName;

            currentTurnHeaderUI?.UpdatePlayer(player, turnIteration);
            if (citiesDisplayManager != null && player.ownedCities != null)
            {
                citiesDisplayManager.DisplayCities(player.ownedCities);
            }
            yield return null;

            CityIconUI targetIcon = FindCityIconForCity(foundedCity);
            targetIcon?.HideAllStatBadges();

            List<int> healthRolls = GetStartupRolls(foundedCity.startingHealthRolls, 3, foundedCity.healthPoints);
            List<int> moneyRolls = GetStartupRolls(foundedCity.startingMoneyRolls, 2, foundedCity.money);
            List<int> powerRolls = GetStartupRolls(foundedCity.startingPowerRolls, 1, foundedCity.cityPower);
            Image[] dieImages = { die1Image, die2Image, die3Image };

            yield return StartCoroutine(AnimateStatBreakdown(statNameText, dieImages, formulaText, "HEALTH", healthRolls, foundedCity.healthPoints, new Color(1f, 0.93f, 0.57f, 1f)));
            yield return StartCoroutine(AnimateRollBadgeTransferToCity(
                overlayRect,
                transferBadgeRect,
                transferBadgeImage,
                transferBadgeText,
                targetIcon,
                "HEALTH",
                foundedCity.healthPoints,
                new Color(1f, 0.93f, 0.57f, 1f)));
            targetIcon?.RevealHealthBadgeNumber();
            yield return StartCoroutine(WaitForNextOrTimeout(nextButton, nextButtonText, countdownText, countdownBgObj, "Next Roll", startupAutoNextSeconds));

            yield return StartCoroutine(AnimateStatBreakdown(statNameText, dieImages, formulaText, "MONEY", moneyRolls, foundedCity.money, new Color(0.2f, 0.95f, 0.35f, 1f)));
            yield return StartCoroutine(AnimateRollBadgeTransferToCity(
                overlayRect,
                transferBadgeRect,
                transferBadgeImage,
                transferBadgeText,
                targetIcon,
                "MONEY",
                foundedCity.money,
                new Color(0.2f, 0.95f, 0.35f, 1f)));
            targetIcon?.RevealMoneyBadgeNumber();
            yield return StartCoroutine(WaitForNextOrTimeout(nextButton, nextButtonText, countdownText, countdownBgObj, "Next Roll", startupAutoNextSeconds));

            yield return StartCoroutine(AnimateStatBreakdown(statNameText, dieImages, formulaText, "POWER", powerRolls, foundedCity.cityPower, new Color(1f, 0.2f, 0.2f, 1f)));
            yield return StartCoroutine(AnimateRollBadgeTransferToCity(
                overlayRect,
                transferBadgeRect,
                transferBadgeImage,
                transferBadgeText,
                targetIcon,
                "POWER",
                foundedCity.cityPower,
                new Color(1f, 0.2f, 0.2f, 1f)));
            targetIcon?.RevealPowerBadgeNumber();
            yield return StartCoroutine(WaitForNextOrTimeout(nextButton, nextButtonText, countdownText, countdownBgObj, "Next Roll", startupAutoNextSeconds));

            Building rolledBuilding = null;
            int buildingRollOne = 0;
            int buildingRollTwo = 0;
            yield return StartCoroutine(BuildCityRollSceneScope.Run(this,
                (rollCanvas, rollCamera) => CityIconUI.PlayStartupBuildingRoll(
                    foundedCity,
                    rollCanvas,
                    rollCamera,
                    (building, roll1, roll2) =>
                    {
                        rolledBuilding = building;
                        buildingRollOne = roll1;
                        buildingRollTwo = roll2;
                    })));

            string rolledBuildingName = rolledBuilding != null ? rolledBuilding.displayName : "No Building";
            rollHeaderText.text = "Starting Building Rolled";
            rollHeaderText.color = new Color(1f, 0.95f, 0.55f, 1f);
            statNameText.text = rolledBuildingName;
            statNameText.color = new Color(1f, 0.95f, 0.55f, 1f);
            formulaText.text = $"Rolls: {buildingRollOne}, {buildingRollTwo}";
            formulaText.color = new Color(1f, 0.95f, 0.55f, 1f);

            yield return StartCoroutine(WaitForNextOrTimeout(nextButton, nextButtonText, countdownText, countdownBgObj, "Continue", startupAutoNextSeconds));

            Destroy(overlayObj);
            startupRevealOverlayObj = null;
            startupRevealInProgress = false;
            RevealVisibleCityBadgeNumbers();
            UpdateDisplay();
        }

        private void UpdateCardLayouts()
        {
            currentTurnHeaderUI?.ApplyVisuals();
            EnsureSideCards();
        }

        private void EnsureSideCards()
        {
            if (!useCardStyle)
            {
                if (instructionsCardBackground != null)
                {
                    instructionsCardBackground.gameObject.SetActive(false);
                }

                return;
            }

            // Note: We're not adding a card background for city icons display
            // since they display as individual icons now

            instructionsCardBackground = EnsureCardBackground(
                instructionsText,
                instructionsCardBackground,
                "InstructionsCard",
                sideCardSprite,
                sideCardColor,
                sideCardPadding);

            if (instructionsCardBackground != null)
            {
                instructionsCardBackground.gameObject.SetActive(isHelpVisible);
            }
        }

        private void ToggleHelpVisibility()
        {
            isHelpVisible = !isHelpVisible;
            ApplyHelpVisibility();
        }

        private void ApplyHelpVisibility()
        {
            if (instructionsText != null)
            {
                instructionsText.gameObject.SetActive(isHelpVisible);
            }

            if (instructionsCardBackground != null)
            {
                instructionsCardBackground.gameObject.SetActive(useCardStyle && isHelpVisible);
            }
        }

        private Image EnsureCardBackground(
            TextMeshProUGUI text,
            Image existingCard,
            string cardObjectName,
            Sprite cardSprite,
            Color cardColor,
            Vector2 padding)
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
                border.effectColor = cardBorderColor;
                border.effectDistance = new Vector2(1f, -1f);
                border.useGraphicAlpha = true;
            }

            Shadow shadow = card.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = cardShadowColor;
                shadow.effectDistance = new Vector2(3f, -3f);
                shadow.useGraphicAlpha = true;
            }

            int textSibling = text.transform.GetSiblingIndex();
            card.transform.SetSiblingIndex(Mathf.Max(0, textSibling - 1));
            return card;
        }

        private Sprite ResolveFlagForCountry(CountryType country)
        {
            if (generatedFlags.TryGetValue(country, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            Sprite generated = CountryFlagFactory.CreateFallbackFlag(country, 96, 64);
            generatedFlags[country] = generated;
            return generated;
        }

        private void Update()
        {
            if (endTurnButton == null)
            {
                EnsureEndTurnButton();
            }

            RefreshEndTurnButtonState();
            BuildCurrentTurnHeaderPresenterIfNeeded(false);
            UpdateCardLayouts();

            if (BuildCityRollSceneScope.IsRollInProgress || startupRevealInProgress)
            {
                return;
            }

            if (Input.GetKeyDown(toggleHelpKey))
            {
                ToggleHelpVisibility();
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                InitializeGame();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                RollForBuilding();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                NextTurn();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                PrintGameState();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                UpdateDisplay();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                TestMultipleBuildingRolls();
            }
        }
    }
}


