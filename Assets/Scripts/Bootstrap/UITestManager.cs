using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GlobalDomination;
using GlobalDomination.GameData;
using GlobalDomination.UI;
using GlobalDomination.UI.Hud;

namespace GlobalDomination.Managers
{
    /// <summary>
    /// Test harness for game actions plus lightweight UI orchestration.
    /// </summary>
    public partial class UITestManager : MonoBehaviour
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
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TextMeshProUGUI endTurnButtonText;

        [Header("Settings")]
        [SerializeField] private bool autoInitializeGame = true;
        [SerializeField] private bool showStartupStatRollReveal = true;
        [SerializeField] private bool devShowSkipStartupButton = false;
        [SerializeField] private float startupStatSpinDuration = 1.7f;
        [SerializeField] private float startupAutoNextSeconds = 5f;
        [SerializeField] private float buildingRollFailToastSeconds = 2.75f;

        private GameManager gameManager;
        private CurrentTurnHeaderUI currentTurnHeaderUI;
        private CitiesDisplayManager citiesDisplayManager;

        private readonly System.Collections.Generic.Dictionary<CountryType, Sprite> generatedFlags =
            new System.Collections.Generic.Dictionary<CountryType, Sprite>();
        private Sprite[] startupDiceFaceSprites;
        private Sprite startupTransferBadgeSprite;

        private CurrentTurnHeaderSettings appliedHeaderSettings;
        private bool headerSettingsInitialized;
        private int turnIteration = 1;
        private bool startupRevealInProgress;
        private Coroutine initializeGameCoroutine;
        private Coroutine startupRevealCoroutine;
        private Button devSkipStartupButton;
        private GameObject startupRevealOverlayObj;
        private Coroutine buildingRollFailToastCoroutine;

        private void Reset()
        {
            ApplyPlayerHudDefaults();
        }

        [ContextMenu("Apply Player HUD Defaults")]
        private void ApplyPlayerHudDefaults()
        {
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
                topOffset = DefaultTopOffset,
                headerFontSize = DefaultHeaderFontSize,
                countryFontSize = DefaultCountryFontSize,
                hudRightMargin = DefaultHudRightMargin,
                hudFlagWidth = DefaultHudFlagWidth,
                hudFlagHeight = DefaultHudFlagHeight,
                hudFlagGap = DefaultHudFlagGap,
                hudTextWidth = DefaultHudTextWidth,
                hudBlockHeight = DefaultHudBlockHeight,
                hudPlayerNameOffset = Vector2.zero,
                hudCountryOffset = Vector2.zero
            };
        }

        private static bool HeaderSettingsEqual(CurrentTurnHeaderSettings a, CurrentTurnHeaderSettings b)
        {
            return Mathf.Approximately(a.hudRightMargin, b.hudRightMargin)
                && Mathf.Approximately(a.hudFlagGap, b.hudFlagGap)
                && Mathf.Approximately(a.hudTextWidth, b.hudTextWidth)
                && Mathf.Approximately(a.hudBlockHeight, b.hudBlockHeight)
                && a.hudPlayerNameOffset == b.hudPlayerNameOffset
                && a.hudCountryOffset == b.hudCountryOffset;
        }

        private void EnsureUIReferences()
        {
            EnsureEventSystem();

            bool missingAnyReference = currentPlayerText == null;
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

            initializeGameCoroutine = null;
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
                DiceFaceSpriteUtility.ApplyDiceSpinVisual(dieRect, elapsed, singleDieSpinDuration, baseScale);
                elapsed += stepDuration;
                yield return new WaitForSeconds(stepDuration);
            }

            for (int i = 0; i < 3; i++)
            {
                dieImage.sprite = diceFaces[Random.Range(1, 7)];
                DiceFaceSpriteUtility.ApplyDiceSpinVisual(dieRect, singleDieSpinDuration + (i * stepDuration), singleDieSpinDuration + (3f * stepDuration), baseScale);
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

            startupDiceFaceSprites = DiceFaceSpriteUtility.CreateIndexedDiceFaceSprites();
            return startupDiceFaceSprites;
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
                Debug.LogWarning("Game not initialized!");
                return;
            }

            Player currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.ownedCities.Count == 0)
            {
                Debug.LogWarning("No player or cities available!");
                return;
            }

            City capital = currentPlayer.GetCapitalCity();
            if (capital == null)
            {
                Debug.LogWarning("No capital city to add a building to.");
                return;
            }

            Building newBuilding = BuildingRollTable.RollForBuilding();

            if (newBuilding != null)
            {
                capital.AddBuilding(newBuilding);
            }
            else
            {
                ShowBuildingRollEmptySlotToast();
            }

            UpdateDisplay();
        }

        private void ShowBuildingRollEmptySlotToast()
        {
            if (buildingRollFailToastCoroutine != null)
            {
                StopCoroutine(buildingRollFailToastCoroutine);
            }

            buildingRollFailToastCoroutine = StartCoroutine(BuildingRollEmptySlotToastRoutine());
        }

        private IEnumerator BuildingRollEmptySlotToastRoutine()
        {
            Canvas canvas = currentPlayerText != null ? currentPlayerText.canvas : FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                buildingRollFailToastCoroutine = null;
                yield break;
            }

            GameObject toastObj = new GameObject("BuildingRollEmptySlotToast");
            toastObj.transform.SetParent(canvas.transform, false);
            toastObj.transform.SetAsLastSibling();

            RectTransform toastRect = toastObj.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.58f);
            toastRect.anchorMax = new Vector2(0.5f, 0.58f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.anchoredPosition = Vector2.zero;
            toastRect.sizeDelta = new Vector2(800f, 88f);

            TextMeshProUGUI toastLabel = toastObj.AddComponent<TextMeshProUGUI>();
            toastLabel.richText = true;
            toastLabel.text =
                "<b>Unlucky!</b>  Building roll failed — <color=#FFAB91>no building</color> this time (empty table slot).";
            toastLabel.fontSize = 26f;
            toastLabel.alignment = TextAlignmentOptions.Center;
            toastLabel.color = new Color(1f, 0.96f, 0.9f, 1f);
            toastLabel.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                toastLabel.font = TMP_Settings.defaultFontAsset;
            }

            yield return new WaitForSeconds(Mathf.Max(0.5f, buildingRollFailToastSeconds));
            Destroy(toastObj);
            buildingRollFailToastCoroutine = null;
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
        }

        public void TestMultipleBuildingRolls()
        {
            for (int i = 0; i < 10; i++)
            {
                int roll1 = DiceRoller.RollD6();
                int roll2 = DiceRoller.RollD6();
                BuildingRollTable.GetBuildingFromRoll(roll1, roll2);
            }
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

        private void UpdateCardLayouts()
        {
            currentTurnHeaderUI?.ApplyVisuals();
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

            if (Input.GetKeyDown(KeyCode.B))
            {
                RollForBuilding();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                NextTurn();
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


