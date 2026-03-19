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

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private Image currentPlayerFlagImage;
        [SerializeField] private TextMeshProUGUI instructionsText;

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

        private GameManager gameManager;
        private CurrentTurnHeaderUI currentTurnHeaderUI;
        private CitiesDisplayManager citiesDisplayManager;

        private Image instructionsCardBackground;
        

        private readonly System.Collections.Generic.Dictionary<CountryType, Sprite> generatedFlags =
            new System.Collections.Generic.Dictionary<CountryType, Sprite>();

        private CurrentTurnHeaderSettings appliedHeaderSettings;
        private bool headerSettingsInitialized;
        private bool isHelpVisible;
        private int turnIteration = 1;

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
            BuildCurrentTurnHeaderPresenterIfNeeded(true);
            SetupInstructions();
            isHelpVisible = showHelpOnStart;
            ApplyHelpVisibility();
            BuildCurrentTurnHeaderPresenterIfNeeded(false);
            UpdateCardLayouts();

            if (autoInitializeGame)
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
Use the buttons below to test game functions

<b>Goal:</b>
Test the dice rolling system and game initialization";
        }

        public void InitializeGame()
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
            UpdateDisplay();

            Debug.Log("Game initialized! Use buttons or keyboard to interact.");
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
                return;
            }

            Player currentPlayer = gameManager.GetCurrentPlayer();
            currentTurnHeaderUI?.UpdatePlayer(currentPlayer, turnIteration);

            // Update city icons display
            if (citiesDisplayManager != null && currentPlayer != null && currentPlayer.ownedCities != null)
            {
                citiesDisplayManager.DisplayCities(currentPlayer.ownedCities);
            }
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
            BuildCurrentTurnHeaderPresenterIfNeeded(false);
            UpdateCardLayouts();

            if (BuildCityRollSceneScope.IsRollInProgress)
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


