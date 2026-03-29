using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalDomination.GameData;
using GlobalDomination.UI;

namespace GlobalDomination.Managers
{
    public partial class UITestManager : MonoBehaviour
    {
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
    }
}
