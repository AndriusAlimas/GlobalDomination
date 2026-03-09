using UnityEngine;
using UnityEngine.UI;
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
        [System.Serializable]
        private class CountryFlagEntry
        {
            public CountryType country;
            public Sprite flagSprite;
        }

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI gameInfoText;
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private Image currentPlayerFlagImage;
        [SerializeField] private TextMeshProUGUI instructionsText;

        [Header("Current Turn HUD")]
        [SerializeField] private bool forceTopCenterForCurrentPlayer = true;
        [SerializeField] private float topOffset = 24f;
        [SerializeField] private float headerFontSize = 36f;
        [SerializeField] private float countryFontSize = 24f;
        [SerializeField] private CountryFlagEntry[] countryFlags;

        [Header("Top Header Style")]
        [SerializeField] private bool useTopHeaderCard = false;
        [SerializeField] private Vector2 standaloneFlagSize = new Vector2(100f, 100f);
        [SerializeField] private float standaloneFlagXOffset = 65.1f;
        [SerializeField] private float standaloneFlagYOffset = -81.3f;
        [SerializeField] private float standaloneHeaderTextXOffset = 80.7f;

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

        private Image gameInfoCardBackground;
        private Image instructionsCardBackground;

        private readonly System.Collections.Generic.Dictionary<CountryType, Sprite> generatedFlags =
            new System.Collections.Generic.Dictionary<CountryType, Sprite>();

        private CurrentTurnHeaderSettings appliedHeaderSettings;
        private bool headerSettingsInitialized;
        private bool isHelpVisible;
        private string lastRenderedCitiesInfo;

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

            appliedHeaderSettings = currentSettings;
            headerSettingsInitialized = true;
        }

        private CurrentTurnHeaderSettings GetCurrentHeaderSettings()
        {
            return new CurrentTurnHeaderSettings
            {
                forceTopCenterForCurrentPlayer = forceTopCenterForCurrentPlayer,
                useCardStyle = useCardStyle,
                useTopHeaderCard = useTopHeaderCard,
                topOffset = topOffset,
                headerFontSize = headerFontSize,
                countryFontSize = countryFontSize,
                standaloneFlagXOffset = standaloneFlagXOffset,
                standaloneFlagYOffset = standaloneFlagYOffset,
                standaloneHeaderTextXOffset = standaloneHeaderTextXOffset,
                standaloneFlagSize = standaloneFlagSize,
                topCardSprite = topCardSprite,
                topCardColor = topCardColor,
                cardBorderColor = cardBorderColor,
                cardShadowColor = cardShadowColor,
                topCardPadding = topCardPadding
            };
        }

        private static bool HeaderSettingsEqual(CurrentTurnHeaderSettings a, CurrentTurnHeaderSettings b)
        {
            return a.forceTopCenterForCurrentPlayer == b.forceTopCenterForCurrentPlayer
                && a.useCardStyle == b.useCardStyle
                && a.useTopHeaderCard == b.useTopHeaderCard
                && Mathf.Approximately(a.topOffset, b.topOffset)
                && Mathf.Approximately(a.headerFontSize, b.headerFontSize)
                && Mathf.Approximately(a.countryFontSize, b.countryFontSize)
                && Mathf.Approximately(a.standaloneFlagXOffset, b.standaloneFlagXOffset)
                && Mathf.Approximately(a.standaloneFlagYOffset, b.standaloneFlagYOffset)
                && Mathf.Approximately(a.standaloneHeaderTextXOffset, b.standaloneHeaderTextXOffset)
                && a.standaloneFlagSize == b.standaloneFlagSize
                && a.topCardSprite == b.topCardSprite
                && a.topCardColor == b.topCardColor
                && a.cardBorderColor == b.cardBorderColor
                && a.cardShadowColor == b.cardShadowColor
                && a.topCardPadding == b.topCardPadding;
        }

        private void EnsureUIReferences()
        {
            bool missingAnyReference = gameInfoText == null || currentPlayerText == null || instructionsText == null;
            if (!missingAnyReference)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("RuntimeGameUICanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            if (currentPlayerText == null)
            {
                currentPlayerText = CreateTextElement(
                    "CurrentPlayerText",
                    canvas.transform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -topOffset),
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
                flagRect.anchoredPosition = new Vector2(-220f, -topOffset);
                flagRect.sizeDelta = new Vector2(56f, 36f);

                currentPlayerFlagImage = flagObject.AddComponent<Image>();
                currentPlayerFlagImage.enabled = false;
            }

            if (gameInfoText == null)
            {
                gameInfoText = CreateTextElement(
                    "GameInfoText",
                    canvas.transform,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(20f, -140f),
                    new Vector2(620f, 760f),
                    18f,
                    TextAlignmentOptions.TopLeft,
                    new Color(0.94f, 0.94f, 0.98f, 1f));
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
            if (gameManager == null)
            {
                Debug.LogWarning("Game not initialized!");
                return;
            }

            gameManager.NextTurn();
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
                if (gameInfoText != null)
                {
                    gameInfoText.text = "Press T or click 'Initialize Game' to start";
                }

                currentTurnHeaderUI?.Clear();
                return;
            }

            Player currentPlayer = gameManager.GetCurrentPlayer();
            currentTurnHeaderUI?.UpdatePlayer(currentPlayer);

            if (gameInfoText != null)
            {
                string nextInfo = BuildCitiesInfo(currentPlayer);
                if (!string.Equals(lastRenderedCitiesInfo, nextInfo, System.StringComparison.Ordinal))
                {
                    gameInfoText.text = nextInfo;
                    lastRenderedCitiesInfo = nextInfo;
                }
            }
        }

        private static string BuildCitiesInfo(Player currentPlayer)
        {
            string info = "<b>=== CITIES ===</b>\n";

            foreach (City city in currentPlayer.ownedCities)
            {
                info += $"\n<b>{city.cityName}</b> {(city.isCapital ? "*" : "")}\n";
                info += $"  HP: {city.healthPoints} | Money: {city.money} | Power: {city.cityPower}\n";
                info += $"  Upgrades: {city.upgradePoints} | Units: {city.unitsInFort.Count}\n";
                info += $"  <b>Buildings ({city.buildings.Count}):</b>\n";

                if (city.buildings.Count == 0)
                {
                    info += "    None\n";
                }
                else
                {
                    foreach (Building building in city.buildings)
                    {
                        info += $"    - {building.displayName}\n";
                    }
                }
            }

            return info;
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
                if (gameInfoCardBackground != null)
                {
                    gameInfoCardBackground.gameObject.SetActive(false);
                }

                if (instructionsCardBackground != null)
                {
                    instructionsCardBackground.gameObject.SetActive(false);
                }

                return;
            }

            gameInfoCardBackground = EnsureCardBackground(
                gameInfoText,
                gameInfoCardBackground,
                "CitiesCard",
                sideCardSprite,
                sideCardColor,
                sideCardPadding);

            instructionsCardBackground = EnsureCardBackground(
                instructionsText,
                instructionsCardBackground,
                "InstructionsCard",
                sideCardSprite,
                sideCardColor,
                sideCardPadding);

            if (gameInfoCardBackground != null)
            {
                gameInfoCardBackground.gameObject.SetActive(true);
            }

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
            if (countryFlags != null)
            {
                foreach (CountryFlagEntry flagEntry in countryFlags)
                {
                    if (flagEntry != null && flagEntry.country == country && flagEntry.flagSprite != null)
                    {
                        return flagEntry.flagSprite;
                    }
                }
            }

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
            UpdateDisplay();

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