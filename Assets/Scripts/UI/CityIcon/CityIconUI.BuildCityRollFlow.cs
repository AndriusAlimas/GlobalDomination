using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GlobalDomination;
using GlobalDomination.GameData;
using GlobalDomination.Managers;

namespace GlobalDomination.UI
{
    public partial class CityIconUI
    {
        private void OnActionClicked(string actionName)
        {
            if (linkedCity == null)
            {
                return;
            }

            if (linkedCity.hasTakenTurn)
            {
                CloseActionMenu();
                return;
            }

            if (actionName == "Check Buildings")
            {
                CloseActionMenu();
                ShowBuildingsListPanel();
                return;
            }

            if (actionName == "Check Fort")
            {
                CloseActionMenu();
                ShowFortStatusPanel();
                return;
            }

            SetTurnCompleted(true);

            if (actionName == "Build new city")
            {
                CloseActionMenu();
                StartCoroutine(BuildCityRollSceneScope.Run(this, PlayBuildCityDiceRollAnimation));
                return;
            }

            if (actionName == "Upgrading")
            {
                CloseActionMenu();
                StartCoroutine(BuildCityRollSceneScope.Run(this, PlayUpgradingDiceRollAnimation));
                return;
            }

            if (actionName == "Building Power")
            {
                CloseActionMenu();
                StartCoroutine(BuildCityRollSceneScope.Run(this, PlayBuildingPowerDiceRollAnimation));
                return;
            }

            if (actionName == "Constructing")
            {
                if (linkedCity.constructionProgress >= CityConstruction.PointsRequired)
                {
                    CloseActionMenu();
                    return;
                }

                CloseActionMenu();
                StartCoroutine(BuildCityRollSceneScope.Run(this, PlayConstructingDiceRollAnimation));
                return;
            }

            if (actionName == "Finish building")
            {
                if (linkedCity.constructionProgress < CityConstruction.PointsRequired)
                {
                    CloseActionMenu();
                    return;
                }

                CloseActionMenu();
                StartCoroutine(BuildCityRollSceneScope.Run(this, PlayFinishConstructionBuildingRoll));
                return;
            }

        }

        // ── Action-specific thin wrappers ──────────────────────────────────────

        private IEnumerator PlayBuildCityDiceRollAnimation(Canvas canvas, Camera sceneCamera)
        {
            int cityCreationRoll = 0;
            yield return StartCoroutine(PlayDiceRollAnimation(canvas, sceneCamera,
                "Build New City",
                "Roll a 6 to found a new city!",
                roll => cityCreationRoll = roll,
                roll => roll == 6
                    ? "Success! New city founded!"
                    : $"Failed. Need 6 (Roll: {roll})"));

            if (cityCreationRoll != 6)
            {
                yield break;
            }

            CreateNewCityFromSuccessfulRoll();
        }

        private void CreateNewCityFromSuccessfulRoll()
        {
            if (linkedCity == null)
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogWarning("[CityIconUI] Cannot create new city: GameManager not found.");
                return;
            }

            Player ownerPlayer = ResolveOwningPlayer(gameManager);
            if (ownerPlayer == null)
            {
                Debug.LogWarning("[CityIconUI] Cannot create new city: owner player not found.");
                return;
            }

            string cityName = GetNextAvailableCityName(ownerPlayer);
            City newCity = new City(cityName, capital: false, ownerId: ownerPlayer.playerId);
            // Stats only; first building is whatever the founded-city dice roll gives (not Main Base by default).
            newCity.InitializeWithDiceRolls(includeStartingBuilding: false);
            // A newly founded city should not be able to act again on the same turn.
            newCity.hasTakenTurn = true;

            ownerPlayer.AddCity(newCity);


            UITestManager uiTestManager = Object.FindFirstObjectByType<UITestManager>();
            if (uiTestManager != null)
            {
                uiTestManager.PlayFoundedCityStartupReveal(ownerPlayer, newCity);
            }
        }

        private Player ResolveOwningPlayer(GameManager gameManager)
        {
            if (gameManager.players == null)
            {
                return null;
            }

            if (linkedCity != null && linkedCity.ownerId > 0)
            {
                for (int i = 0; i < gameManager.players.Count; i++)
                {
                    Player player = gameManager.players[i];
                    if (player != null && player.playerId == linkedCity.ownerId)
                    {
                        return player;
                    }
                }
            }

            return gameManager.GetCurrentPlayer();
        }

        private string GetNextAvailableCityName(Player ownerPlayer)
        {
            HashSet<string> usedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (ownerPlayer.ownedCities != null)
            {
                for (int i = 0; i < ownerPlayer.ownedCities.Count; i++)
                {
                    City city = ownerPlayer.ownedCities[i];
                    if (city != null && !string.IsNullOrWhiteSpace(city.cityName))
                    {
                        usedNames.Add(city.cityName.Trim());
                    }
                }
            }

            CountryData countryData = CountryDatabase.GetCountryData(ownerPlayer.selectedCountry);
            if (countryData != null && countryData.cityNames != null)
            {
                for (int i = 0; i < countryData.cityNames.Count; i++)
                {
                    string candidate = countryData.cityNames[i];
                    if (!string.IsNullOrWhiteSpace(candidate) && !usedNames.Contains(candidate.Trim()))
                    {
                        return candidate.Trim();
                    }
                }
            }

            string baseName = !string.IsNullOrWhiteSpace(linkedCity.cityName)
                ? linkedCity.cityName.Trim()
                : "City";

            int suffix = 2;
            string generatedName = $"{baseName} Colony {suffix}";
            while (usedNames.Contains(generatedName))
            {
                suffix++;
                generatedName = $"{baseName} Colony {suffix}";
            }

            return generatedName;
        }

        private IEnumerator PlayUpgradingDiceRollAnimation(Canvas canvas, Camera sceneCamera)
        {
            int firstRoll = 0;
            yield return StartCoroutine(PlayDiceRollAnimation(canvas, sceneCamera,
                "Upgrading",
                "Roll 5 or 6 for a Lucky Roll reward!",
                roll => firstRoll = roll,
                roll => roll >= 5
                    ? "<color=#FFD700>Lucky Roll!</color>"
                    : "<color=#FF4444>Failed!</color>"));

            if (linkedCity == null || firstRoll < 5)
            {
                yield break;
            }

            int slotRoll = 0;
            yield return StartCoroutine(PlayUpgradeSlotAnimation(canvas, result => slotRoll = result));

            if (linkedCity == null)
            {
                yield break;
            }

            bool rewardPopulation = (slotRoll % 2) == 1;
            int rewardAmount = 0;

            yield return StartCoroutine(PlayDiceRollAnimation(canvas, sceneCamera,
                rewardPopulation ? "Population Reward" : "Money Reward",
                rewardPopulation
                    ? "Roll to determine gained population"
                    : "Roll to determine gained money",
                roll => rewardAmount = roll,
                roll => rewardPopulation
                    ? $"+{roll} Population!"
                    : $"+{roll} Money!"));

            if (rewardPopulation)
            {
                linkedCity.healthPoints += rewardAmount;
                if (populationText != null)
                {
                    populationText.text = linkedCity.healthPoints.ToString();
                }

            }
            else
            {
                linkedCity.money += rewardAmount;
                if (moneyText != null)
                {
                    moneyText.text = linkedCity.money.ToString();
                }

            }
        }

        // ── 2D Upgrade Slot Animation ──────────────────────────────────────────

        private IEnumerator PlayUpgradeSlotAnimation(Canvas canvas, System.Action<int> onResult)
        {
            int finalValue = Random.Range(1, 7);

            GameObject overlayObj = new GameObject("UpgradeSlotOverlay");
            if (canvas != null)
            {
                overlayObj.transform.SetParent(canvas.transform, false);
                overlayObj.transform.SetAsLastSibling();
            }

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayBg = overlayObj.AddComponent<Image>();
            overlayBg.color = new Color(0.02f, 0.06f, 0.12f, 0.86f);
            overlayBg.raycastTarget = true;

            TextMeshProUGUI titleTmp = CreateSlotTextElement(overlayObj.transform, new Vector2(0f, 120f), new Vector2(500f, 60f), "LUCKY DICE", 38f, new Color(1f, 0.85f, 0.2f, 1f));
            titleTmp.characterSpacing = 10f;
            CreateSlotTextElement(overlayObj.transform, new Vector2(0f, 70f), new Vector2(500f, 40f), "Odd = Population   |   Even = Money", 18f, new Color(0.8f, 0.9f, 1f, 0.9f));

            GameObject dieGo = new GameObject("SlotDie");
            dieGo.transform.SetParent(overlayObj.transform, false);
            RectTransform dieRect = dieGo.AddComponent<RectTransform>();
            dieRect.anchorMin = new Vector2(0.5f, 0.5f);
            dieRect.anchorMax = new Vector2(0.5f, 0.5f);
            dieRect.pivot = new Vector2(0.5f, 0.5f);
            dieRect.anchoredPosition = Vector2.zero;
            dieRect.sizeDelta = new Vector2(120f, 120f);
            Image dieImage = dieGo.AddComponent<Image>();
            dieImage.raycastTarget = false;
            dieImage.preserveAspect = true;

            TextMeshProUGUI resultTmp = CreateSlotTextElement(overlayObj.transform, new Vector2(0f, -100f), new Vector2(500f, 60f), string.Empty, 30f, Color.white);

            Sprite[] diceFaces = DiceFaceSpriteUtility.CreateIndexedDiceFaceSprites();
            dieImage.sprite = diceFaces[1];

            float elapsed = 0f;
            const float spinDuration = 1.1f;
            const float stepDuration = 0.07f;
            Vector3 baseScale = dieRect.localScale;

            while (elapsed < spinDuration)
            {
                dieImage.sprite = diceFaces[Random.Range(1, 7)];
                DiceFaceSpriteUtility.ApplyDiceSpinVisual(dieRect, elapsed, spinDuration, baseScale);
                elapsed += stepDuration;
                yield return new WaitForSeconds(stepDuration);
            }

            for (int i = 0; i < 3; i++)
            {
                dieImage.sprite = diceFaces[Random.Range(1, 7)];
                DiceFaceSpriteUtility.ApplyDiceSpinVisual(dieRect, spinDuration + (i * stepDuration), spinDuration + (3f * stepDuration), baseScale);
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

            bool isOdd = (finalValue % 2) == 1;
            resultTmp.text = isOdd
                ? "<color=#7CE2FF>ODD — Population Reward!</color>"
                : "<color=#9CFF7C>EVEN — Money Reward!</color>";

            yield return new WaitForSeconds(1.8f);

            onResult?.Invoke(finalValue);
            Destroy(overlayObj);
        }

        private static TextMeshProUGUI CreateSlotTextElement(Transform parent, Vector2 anchoredPos, Vector2 sizeDelta, string text, float fontSize, Color color)
        {
            GameObject go = new GameObject("SlotText");
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private IEnumerator PlayBuildingPowerDiceRollAnimation(Canvas canvas, Camera sceneCamera)
        {
            int firstRoll = 0;
            yield return StartCoroutine(PlayDiceRollAnimation(canvas, sceneCamera,
                "Building Power",
                "Roll a 6 to gain power — then roll again for the amount!",
                roll => firstRoll = roll,
                roll => roll == 6
                    ? "<color=#FFD700>Critical! Roll again!</color>"
                    : "<color=#FF4444>Failed!</color>"));

            if (firstRoll == 6)
            {
                yield return StartCoroutine(PlayDiceRollAnimation(canvas, sceneCamera,
                    "Building Power — Bonus Roll",
                    "1-3 = +1 Power, 4-5 = +2 Power, 6 = +3 Power",
                    roll =>
                    {
                        int gainedPower = ConvertPowerBonusRollToPower(roll);

                        if (linkedCity != null)
                        {
                            linkedCity.cityPower += gainedPower;
                            if (powerText != null) powerText.text = linkedCity.cityPower.ToString();
                        }
                    },
                    roll => $"+{ConvertPowerBonusRollToPower(roll)} City Power!"));
            }
        }

        private static int ConvertPowerBonusRollToPower(int roll)
        {
            if (roll <= 3)
            {
                return 1;
            }

            if (roll <= 5)
            {
                return 2;
            }

            return 3;
        }

        private IEnumerator PlayConstructingDiceRollAnimation(Canvas canvas, Camera sceneCamera)
        {
            if (linkedCity == null || linkedCity.constructionProgress >= CityConstruction.PointsRequired)
            {
                yield break;
            }

            yield return StartCoroutine(PlayDiceRollAnimation(
                canvas,
                sceneCamera,
                "Constructing",
                "Face 1 = +1 point, face 3 = +2, face 6 = +3 (other faces = 0). Fill the bar, then use Finish building to roll for a new building.",
                roll =>
                {
                    if (linkedCity == null)
                    {
                        return;
                    }

                    int gained = CityConstruction.PointsFromDie(roll);
                    linkedCity.constructionProgress = Mathf.Min(
                        linkedCity.constructionProgress + gained,
                        CityConstruction.PointsRequired);
                    RefreshConstructionBarVisual();
                },
                roll =>
                {
                    int gained = CityConstruction.PointsFromDie(roll);
                    int p = linkedCity != null ? linkedCity.constructionProgress : 0;
                    string pts = gained > 0
                        ? $"+{gained} construction"
                        : "No points (need 1, 3, or 6)";
                    return $"{pts} — {p}/{CityConstruction.PointsRequired}";
                }));
        }

        private IEnumerator PlayFinishConstructionBuildingRoll(Canvas canvas, Camera sceneCamera)
        {
            if (linkedCity == null)
            {
                yield break;
            }

            yield return StartCoroutine(PlayStartupBuildingRoll(linkedCity, canvas, sceneCamera, null));

            linkedCity.constructionProgress = 0;
            RefreshConstructionBarVisual();

            UITestManager uiTestManager = Object.FindFirstObjectByType<UITestManager>();
            if (uiTestManager != null)
            {
                uiTestManager.RefreshCurrentTurnDisplay();
            }
        }

        // ── Public method for startup building rolls ────────────────────────

        /// <summary>
        /// Public method to roll for a building during startup with 3D dice animation.
        /// Performs two rolls (category + specific building).
        /// </summary>
        public static IEnumerator PlayStartupBuildingRoll(
            GameData.City targetCity,
            Canvas canvas,
            Camera sceneCamera,
            System.Action<GameData.Building, int, int> onCompleted = null)
        {
            if (targetCity == null || canvas == null || sceneCamera == null)
            {
                yield break;
            }

            int firstRoll = 0;
            int secondRoll = 0;

            // Temporary CityIconUI instance just for the dice rolling
            GameObject tempObj = new GameObject("_TempDiceRoller");
            CityIconUI tempRoller = tempObj.AddComponent<CityIconUI>();
            tempRoller.linkedCity = targetCity;

            // First roll: Building category — table reveals while camera still faces dice.
            yield return tempRoller.PlayDiceRollAnimation(canvas, sceneCamera,
                "Roll 1 / 2",
                string.Empty,
                roll => firstRoll = roll,
                roll => string.Empty,
                () => tempRoller.PlayBuildingRollTableReveal(canvas, firstRoll, null));

            // Second roll: Specific building — table reveals while camera still faces dice.
            yield return tempRoller.PlayDiceRollAnimation(canvas, sceneCamera,
                "Roll 2 / 2",
                string.Empty,
                roll => secondRoll = roll,
                roll => string.Empty,
                () => tempRoller.PlayBuildingRollTableReveal(canvas, firstRoll, secondRoll));

            // Respect empty table slots (None): do not substitute a random building.
            var building = GameData.BuildingRollTable.GetBuildingFromRoll(firstRoll, secondRoll);
            if (building != null)
            {
                targetCity.AddBuilding(building);
            }

            onCompleted?.Invoke(building, firstRoll, secondRoll);

            Destroy(tempObj);
        }

        private IEnumerator PlayBuildingRollTableReveal(Canvas canvas, int firstRoll, int? secondRoll)
        {
            if (canvas == null)
            {
                yield break;
            }

            int clampedFirstRoll = Mathf.Clamp(firstRoll, 1, 6);
            int clampedSecondRoll = secondRoll.HasValue ? Mathf.Clamp(secondRoll.Value, 1, 6) : -1;

            GameObject overlayObj = new GameObject("BuildingRollTableOverlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.transform.SetAsLastSibling();

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayBg = overlayObj.AddComponent<Image>();
            // Keep this as a quick lookup layer over the dice view, not a full blocking modal.
            overlayBg.color = new Color(0.03f, 0.06f, 0.12f, 0.18f);
            overlayBg.raycastTarget = true;

            GameObject panelObj = new GameObject("BuildingRollTablePanel");
            panelObj.transform.SetParent(overlayObj.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1260f, 780f);
            panelRect.localScale = Vector3.one;

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.14f, 0.24f, 0.55f);

            // Resolve picked building early so we can embed it into the hint.
            string pickedLabel = string.Empty;
            if (secondRoll.HasValue &&
                GameData.BuildingRollTable.TryGetBuildingTypeFromRoll(clampedFirstRoll, clampedSecondRoll, out GameData.BuildingType earlyType))
            {
                pickedLabel = FormatBuildingTypeLabel(earlyType);
            }

            TextMeshProUGUI titleText = BuildCityDiceUiFactory.CreateDiceText(panelObj.transform, "TableTitle", 40f, new Vector2(0f, 330f));
            titleText.color = new Color(1f, 0.95f, 0.58f, 1f);
            titleText.text = secondRoll.HasValue
                ? "BUILDING TABLE  —  FINAL PICK"
                : "BUILDING TABLE  —  CATEGORY PICK";

            TextMeshProUGUI hintText = BuildCityDiceUiFactory.CreateDiceText(panelObj.transform, "TableHint", 22f, new Vector2(0f, 286f));
            hintText.rectTransform.sizeDelta = new Vector2(1120f, 58f);
            hintText.color = new Color(0.78f, 0.91f, 1f, 1f);
            hintText.text = secondRoll.HasValue
                ? $"Column  {clampedFirstRoll}   ×   Row  {clampedSecondRoll}     →     {pickedLabel}"
                : $"Column  {clampedFirstRoll}  is highlighted  —  roll again to pick a building.";

            Button okButton = CreateTableOkButton(panelObj.transform, out TextMeshProUGUI okButtonText);

            // 6 columns centred symmetrically: leftmost at -405, rightmost at +405.
            const float startX = -405f;
            const float startY = 168f;
            const float cellWidth = 162f;
            const float cellHeight = 76f;

            // Column headers for the first die.
            for (int col = 1; col <= 6; col++)
            {
                GameObject colObj = new GameObject($"ColHeader_{col}");
                colObj.transform.SetParent(panelObj.transform, false);
                RectTransform colRect = colObj.AddComponent<RectTransform>();
                colRect.anchorMin = new Vector2(0.5f, 0.5f);
                colRect.anchorMax = new Vector2(0.5f, 0.5f);
                colRect.pivot = new Vector2(0.5f, 0.5f);
                colRect.anchoredPosition = new Vector2(startX + ((col - 1) * cellWidth), startY + 72f);
                colRect.sizeDelta = new Vector2(cellWidth - 6f, 54f);

                Image colBg = colObj.AddComponent<Image>();
                bool selectedColumn = col == clampedFirstRoll;
                colBg.color = selectedColumn
                    ? new Color(0.99f, 0.82f, 0.28f, 0.95f)
                    : new Color(0.22f, 0.32f, 0.45f, 0.9f);

                TextMeshProUGUI colText = BuildCityDiceUiFactory.CreateDiceText(colObj.transform, "HeaderText", 22f, Vector2.zero);
                colText.rectTransform.sizeDelta = new Vector2(cellWidth - 6f, 54f);
                colText.text = $"1st: {col}";
                colText.color = selectedColumn
                    ? new Color(0.09f, 0.08f, 0.05f, 1f)
                    : new Color(0.88f, 0.94f, 1f, 1f);
            }

            RectTransform selectedCellRect = null;

            // 6x6 table body: rows are second die, columns are first die.
            for (int row = 1; row <= 6; row++)
            {
                GameObject rowLabelObj = new GameObject($"RowLabel_{row}");
                rowLabelObj.transform.SetParent(panelObj.transform, false);
                RectTransform rowLabelRect = rowLabelObj.AddComponent<RectTransform>();
                rowLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
                rowLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
                rowLabelRect.pivot = new Vector2(1f, 0.5f);
                // Place row labels just outside the first data column with a fixed gutter.
                rowLabelRect.anchoredPosition = new Vector2(startX - (cellWidth * 0.5f) - 18f, startY - ((row - 1) * cellHeight));
                rowLabelRect.sizeDelta = new Vector2(112f, cellHeight - 6f);

                Image rowLabelBg = rowLabelObj.AddComponent<Image>();
                rowLabelBg.color = secondRoll.HasValue && row == clampedSecondRoll
                    ? new Color(0.99f, 0.82f, 0.28f, 0.95f)
                    : new Color(0.22f, 0.32f, 0.45f, 0.9f);

                TextMeshProUGUI rowLabelText = BuildCityDiceUiFactory.CreateDiceText(rowLabelObj.transform, "RowText", 21f, Vector2.zero);
                                rowLabelText.rectTransform.sizeDelta = new Vector2(112f, cellHeight - 6f);
                rowLabelText.text = $"2nd: {row}";
                rowLabelText.color = secondRoll.HasValue && row == clampedSecondRoll
                    ? new Color(0.09f, 0.08f, 0.05f, 1f)
                    : new Color(0.88f, 0.94f, 1f, 1f);

                for (int col = 1; col <= 6; col++)
                {
                    GameObject cellObj = new GameObject($"Cell_{col}_{row}");
                    cellObj.transform.SetParent(panelObj.transform, false);

                    RectTransform cellRect = cellObj.AddComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                    cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                    cellRect.pivot = new Vector2(0.5f, 0.5f);
                    cellRect.anchoredPosition = new Vector2(startX + ((col - 1) * cellWidth), startY - ((row - 1) * cellHeight));
                    cellRect.sizeDelta = new Vector2(cellWidth - 8f, cellHeight - 8f);

                    bool isSelectedColumn = col == clampedFirstRoll;
                    bool isSelectedCell = secondRoll.HasValue && isSelectedColumn && row == clampedSecondRoll;

                    Image cellBg = cellObj.AddComponent<Image>();
                    if (isSelectedCell)
                    {
                        cellBg.color = new Color(0.98f, 0.92f, 0.46f, 0.98f);
                        selectedCellRect = cellRect;
                    }
                    else if (isSelectedColumn)
                    {
                        cellBg.color = new Color(0.93f, 0.75f, 0.24f, 0.86f);
                    }
                    else
                    {
                        cellBg.color = new Color(0.16f, 0.24f, 0.35f, 0.9f);
                    }

                    if (GameData.BuildingRollTable.TryGetBuildingTypeFromRoll(col, row, out GameData.BuildingType tableType))
                    {
                        TextMeshProUGUI cellText = BuildCityDiceUiFactory.CreateDiceText(cellObj.transform, "CellText", 15f, Vector2.zero);
                        cellText.rectTransform.sizeDelta = new Vector2(cellWidth - 10f, cellHeight - 6f);
                        cellText.textWrappingMode = TextWrappingModes.Normal;
                        cellText.enableAutoSizing = true;
                        cellText.fontSizeMin = 14f;
                        cellText.fontSizeMax = 18f;
                        cellText.text = FormatBuildingTypeLabel(tableType);
                        cellText.color = isSelectedCell
                            ? new Color(0.1f, 0.08f, 0.03f, 1f)
                            : new Color(0.92f, 0.96f, 1f, 1f);
                    }
                }
            }

            if (secondRoll.HasValue && selectedCellRect != null)
            {

                float pickElapsed = 0f;
                const float pickDuration = 0.62f;
                Vector3 baseScale = selectedCellRect.localScale;
                while (pickElapsed < pickDuration)
                {
                    pickElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(pickElapsed / pickDuration);
                    float pulse = 1f + Mathf.Sin(t * Mathf.PI * 3f) * 0.16f * (1f - t * 0.45f);
                    selectedCellRect.localScale = baseScale * pulse;
                    yield return null;
                }

                selectedCellRect.localScale = baseScale;
            }
            else
            {
                yield return new WaitForSeconds(0.55f);
            }

            yield return StartCoroutine(WaitForOkOrTimeout(okButton, okButtonText, 5f));

            CanvasGroup fadeGroup = panelObj.AddComponent<CanvasGroup>();
            float fadeElapsed = 0f;
            const float fadeDuration = 0.33f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
                fadeGroup.alpha = 1f - t;
                panelRect.localScale = Vector3.Lerp(new Vector3(1f, 1f, 1f), new Vector3(1.04f, 1.04f, 1f), t);
                overlayBg.color = new Color(0.03f, 0.06f, 0.12f, Mathf.Lerp(0.18f, 0f, t));
                yield return null;
            }

            Destroy(overlayObj);
        }

        private static Button CreateTableOkButton(Transform parent, out TextMeshProUGUI buttonText)
        {
            GameObject buttonObj = new GameObject("TableOkButton");
            buttonObj.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -328f);
            buttonRect.sizeDelta = new Vector2(260f, 64f);

            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = new Color(0.93f, 0.75f, 0.24f, 0.92f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonBg;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "OK";
            buttonText.fontSize = 26f;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = new Color(0.1f, 0.08f, 0.03f, 1f);
            buttonText.fontStyle = FontStyles.Bold;

            return button;
        }

        private static IEnumerator WaitForOkOrTimeout(Button okButton, TextMeshProUGUI okButtonText, float timeoutSeconds)
        {
            if (okButton == null)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, timeoutSeconds));
                yield break;
            }

            bool pressed = false;
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(() => pressed = true);

            float remaining = Mathf.Max(0.25f, timeoutSeconds);
            while (!pressed && remaining > 0f)
            {
                if (okButtonText != null)
                {
                    okButtonText.text = $"OK ({Mathf.CeilToInt(remaining)})";
                }

                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (okButtonText != null)
            {
                okButtonText.text = "OK";
            }
        }

        private static string FormatBuildingTypeLabel(GameData.BuildingType type)
        {
            if (type == GameData.BuildingType.None)
            {
                return "No Building";
            }

            string raw = type.ToString();
            System.Text.StringBuilder formatted = new System.Text.StringBuilder(raw.Length + 8);
            for (int i = 0; i < raw.Length; i++)
            {
                char ch = raw[i];
                if (i > 0 && char.IsUpper(ch) && !char.IsUpper(raw[i - 1]))
                {
                    formatted.Append(' ');
                }
                formatted.Append(ch);
            }

            return formatted.ToString();
        }

        // ── Shared dice-roll animation engine ─────────────────────────────────

        private IEnumerator PlayDiceRollAnimation(
            Canvas canvas,
            Camera sceneCamera,
            string actionTitle,
            string hintLabel,
            System.Action<int> onResult,
            System.Func<int, string> resultFormatter,
            System.Func<IEnumerator> afterResultShown = null)
        {
            if (sceneCamera == null)
            {
                yield break;
            }

            EnsureAudioListenerForRoll(sceneCamera);

            Vector3 originalCamPos = sceneCamera.transform.position;
            Quaternion originalCamRot = sceneCamera.transform.rotation;
            float originalCamFov = sceneCamera.fieldOfView;
            bool originalCamOrtho = sceneCamera.orthographic;

            if (activeDiceOverlay != null)
            {
                Destroy(activeDiceOverlay);
                activeDiceOverlay = null;
            }

            if (activeDiceWorldRoot != null)
            {
                Destroy(activeDiceWorldRoot);
                activeDiceWorldRoot = null;
            }

            GameObject overlayObj = new GameObject("DiceRollOverlay");
            if (canvas != null)
            {
                overlayObj.transform.SetParent(canvas.transform, false);
                overlayObj.transform.SetAsLastSibling();
            }
            activeDiceOverlay = overlayObj;

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayBg = overlayObj.AddComponent<Image>();
            // Keep interactions blocked without darkening the dedicated rolling view.
            overlayBg.color = new Color(0.23f, 0.37f, 0.62f, 0.06f);
            // Block other UI interactions while dice roll is active.
            overlayBg.raycastTarget = true;

            AnimatedDiceWorldContext animatedDiceContext = TryCreateAnimatedD6WorldRoller(sceneCamera);
            if (animatedDiceContext == null)
            {
                if (activeDiceOverlay == overlayObj)
                {
                    Destroy(activeDiceOverlay);
                    activeDiceOverlay = null;
                }

                yield break;
            }

            activeDiceWorldRoot = animatedDiceContext.root;

            // Keep a top-heavy camera angle so the roll is clearly visible from above.
            Vector3 viewCenter = animatedDiceContext.boundsCenter + new Vector3(0f, 0.15f, 0f);
            sceneCamera.orthographic = false;
            sceneCamera.fieldOfView = 30f;
            sceneCamera.transform.position = viewCenter + new Vector3(0f, 30.5f, -4.5f);
            sceneCamera.transform.rotation = Quaternion.LookRotation(viewCenter - sceneCamera.transform.position, Vector3.up);

            // Build colliders from the current screen corners so edge/corner hits always collide.
            RebuildDiceScreenBounds(animatedDiceContext, sceneCamera);

            TextMeshProUGUI resultText = BuildCityDiceUiFactory.CreateDiceText(overlayObj.transform, "Result", 38f, new Vector2(0f, -270f));
            resultText.color = new Color(1f, 0.92f, 0.2f, 1f);
            resultText.text = string.Empty;
            resultText.raycastTarget = false;

            // Action title banner at top of the dice view.
            TextMeshProUGUI titleText = BuildCityDiceUiFactory.CreateDiceText(overlayObj.transform, "ActionTitle", 32f, new Vector2(0f, 290f));
            titleText.text = actionTitle;
            titleText.color = new Color(1f, 0.95f, 0.6f, 1f);
            titleText.raycastTarget = false;

            TextMeshProUGUI hintText = BuildCityDiceUiFactory.CreateDiceText(overlayObj.transform, "Hint", 15f, new Vector2(0f, -340f));
            hintText.text = hintLabel;
            hintText.color = new Color(0.8f, 0.9f, 1f, 0.9f);
            hintText.raycastTarget = false;

            Image handImage = BuildCityDiceUiFactory.CreateDiceHandImage(overlayObj.transform);
            RectTransform handRect = handImage != null ? handImage.rectTransform : null;

            Rigidbody diceRb = animatedDiceContext.rigidbody;
            Transform diceTransform = animatedDiceContext.diceObject != null
                ? animatedDiceContext.diceObject.transform
                : (diceRb != null ? diceRb.transform : null);
            Renderer[] diceRenderers = animatedDiceContext.diceObject != null
                ? animatedDiceContext.diceObject.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
            bool releaseDetected = false;
            bool motionDetected = false;
            float settleTimer = 0f;
            bool holdStarted = Input.GetMouseButton(0);
            float holdStartTime = holdStarted ? Time.time : 0f;
            float handReleaseStartTime = -1f;
            float releaseStartTime = -1f;
            Vector2 lastMousePos = Input.mousePosition;
            float shakeTravel = 0f;

            Vector3 flatForward = Vector3.ProjectOnPlane(sceneCamera.transform.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.forward;
            }

            Vector3 flatRight = Vector3.ProjectOnPlane(sceneCamera.transform.right, Vector3.up).normalized;
            if (flatRight.sqrMagnitude < 0.001f)
            {
                flatRight = Vector3.right;
            }

            DiceRoller.BuildCityLaunchProfile launchProfile = DiceRoller.CreateBuildCityLaunchProfile();
            float sideSign = launchProfile.sideSign;
            Vector2 handHoldPos = launchProfile.handHoldPos;
            Vector2 handReleasePos = launchProfile.handReleasePos;
            Vector2 nearZeroThrowFallback = launchProfile.nearZeroThrowFallback;

            Vector3 holdAnchor = animatedDiceContext.boundsCenter
                + flatRight * launchProfile.sideDistance
                + flatForward * launchProfile.forwardOffset
                + Vector3.up * launchProfile.holdHeight;
            Quaternion holdRotationBase = Quaternion.Euler(
                Random.Range(12f, 25f),
                sideSign * Random.Range(14f, 42f),
                -sideSign * Random.Range(10f, 30f));

            if (diceRb != null)
            {
                if (!diceRb.isKinematic)
                {
                    diceRb.linearVelocity = Vector3.zero;
                    diceRb.angularVelocity = Vector3.zero;
                }
                diceRb.isKinematic = true;
            }

            SetDiceRenderersVisible(diceRenderers, false);

            while (true)
            {
                if (animatedDiceContext.root == null || diceRb == null || diceTransform == null)
                {
                    break;
                }

                Vector3 pos = diceRb.position;
                bool isOutOfArena = pos.y < animatedDiceContext.floorY - 6f;
                if (isOutOfArena)
                {
                    if (releaseDetected)
                    {
                        break;
                    }

                    if (!diceRb.isKinematic)
                    {
                        diceRb.linearVelocity = Vector3.zero;
                        diceRb.angularVelocity = Vector3.zero;
                    }
                    diceRb.isKinematic = true;
                    diceRb.position = holdAnchor;
                    diceTransform.rotation = holdRotationBase;
                    releaseDetected = false;
                    holdStarted = false;
                    motionDetected = false;
                    settleTimer = 0f;
                    holdStartTime = 0f;
                    handReleaseStartTime = -1f;
                    releaseStartTime = -1f;
                    lastMousePos = Input.mousePosition;
                    shakeTravel = 0f;
                    SetDiceRenderersVisible(diceRenderers, false);
                    if (handImage != null)
                    {
                        handImage.color = Color.white;
                    }
                    if (handRect != null)
                    {
                        handRect.anchoredPosition = handHoldPos;
                        handRect.localRotation = Quaternion.identity;
                    }
                    hintText.text = hintLabel;
                }

                if (!releaseDetected)
                {
                    SetDiceRenderersVisible(diceRenderers, false);

                    if (!holdStarted && (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)))
                    {
                        holdStarted = true;
                        holdStartTime = Time.time;
                        lastMousePos = Input.mousePosition;
                        shakeTravel = 0f;
                    }

                    float timeCharge = holdStarted ? Mathf.Clamp01((Time.time - holdStartTime) / 0.95f) : 0f;
                    if (holdStarted && Input.GetMouseButton(0))
                    {
                        Vector2 currentMousePos = Input.mousePosition;
                        Vector2 mouseDelta = currentMousePos - lastMousePos;
                        float deltaMagnitude = mouseDelta.magnitude;
                        shakeTravel = Mathf.Min(shakeTravel + deltaMagnitude, 2000f);
                        lastMousePos = currentMousePos;
                    }

                    float shakeCharge = Mathf.Clamp01(shakeTravel / 540f);
                    float holdCharge = Mathf.Clamp01(timeCharge * 0.35f + shakeCharge * 0.65f);
                    float shakeTime = Time.time * 24f;
                    float worldShakeScale = 0.06f + holdCharge * 0.36f;
                    Vector3 worldShake = flatRight * Mathf.Sin(shakeTime * 1.9f) * worldShakeScale
                        + flatForward * Mathf.Cos(shakeTime * 1.4f) * worldShakeScale * 0.75f
                        + Vector3.up * Mathf.Abs(Mathf.Sin(shakeTime * 2.8f)) * worldShakeScale * 0.35f;

                    diceRb.position = holdAnchor + worldShake;
                    diceTransform.rotation = holdRotationBase * Quaternion.Euler(
                        Mathf.Sin(shakeTime * 2.3f) * (8f + holdCharge * 18f),
                        shakeTime * (6f + holdCharge * 18f),
                        Mathf.Cos(shakeTime * 1.7f) * (5f + holdCharge * 12f));

                    if (handRect != null)
                    {
                        Vector2 targetHandPos = new Vector2(
                            Mathf.Clamp(Input.mousePosition.x - Screen.width * 0.5f, -300f, 300f),
                            Mathf.Clamp(Input.mousePosition.y, 70f, Screen.height * 0.58f));

                        Vector2 handShake = new Vector2(
                            Mathf.Sin(shakeTime * 1.5f),
                            Mathf.Cos(shakeTime * 1.2f)) * (4f + holdCharge * 18f);
                        Vector2 handBasePos = holdStarted ? Vector2.Lerp(handRect.anchoredPosition, targetHandPos, 0.2f) : handHoldPos;
                        handRect.anchoredPosition = handBasePos + handShake;
                        handRect.localRotation = Quaternion.Euler(0f, 0f, -6f + Mathf.Sin(shakeTime * 1.3f) * (2f + holdCharge * 7f));
                    }

                    if (holdStarted && Input.GetMouseButtonUp(0))
                    {
                        releaseDetected = true;
                        handReleaseStartTime = Time.time;
                        releaseStartTime = Time.time;
                        hintText.text = string.Empty;

                        Vector2 releaseHandUiPos = handRect != null ? handRect.anchoredPosition : handHoldPos;
                        handHoldPos = releaseHandUiPos;
                        handReleasePos = releaseHandUiPos + new Vector2(-sideSign * Random.Range(118f, 154f), Random.Range(16f, 36f));
                        Vector2 releaseHandScreenPos = new Vector2(
                            Screen.width * 0.5f + releaseHandUiPos.x,
                            Mathf.Max(8f, releaseHandUiPos.y));

                        float handPlaneY = holdAnchor.y + 0.05f;
                        Plane handThrowPlane = new Plane(Vector3.up, new Vector3(0f, handPlaneY, 0f));

                        Ray handRay = sceneCamera.ScreenPointToRay(releaseHandScreenPos);
                        Vector3 releasePoint = holdAnchor;
                        if (handThrowPlane.Raycast(handRay, out float handHitDist))
                        {
                            releasePoint = handRay.GetPoint(handHitDist);
                        }
                        releasePoint += Vector3.up * 0.06f;

                        Vector2 targetScreenPos = Input.mousePosition;
                        Vector2 releaseToMouse = targetScreenPos - releaseHandScreenPos;
                        if (releaseToMouse.sqrMagnitude < 64f)
                        {
                            // Avoid near-zero throws if the cursor is too close to the release point.
                            targetScreenPos = releaseHandScreenPos + nearZeroThrowFallback;
                        }
                        Ray targetRay = sceneCamera.ScreenPointToRay(targetScreenPos);
                        Vector3 targetPoint = releasePoint + flatRight;
                        if (handThrowPlane.Raycast(targetRay, out float targetHitDist))
                        {
                            targetPoint = targetRay.GetPoint(targetHitDist);
                        }

                        int throwStyle = Random.Range(0, 5);
                        Vector3 styleOffset = Vector3.zero;
                        if (throwStyle == 1)
                        {
                            // Side skim toward walls.
                            styleOffset = flatRight * sideSign * Random.Range(0.9f, 1.7f);
                        }
                        else if (throwStyle == 2)
                        {
                            // Counter-side cross throw.
                            styleOffset = flatRight * -sideSign * Random.Range(0.8f, 1.5f) + flatForward * Random.Range(-0.3f, 0.5f);
                        }
                        else if (throwStyle == 3)
                        {
                            // Forward-heavy push.
                            styleOffset = flatForward * Random.Range(0.9f, 1.7f);
                        }
                        else if (throwStyle == 4)
                        {
                            // Slight backward/diagonal pull.
                            styleOffset = flatForward * Random.Range(-1.2f, -0.35f) + flatRight * sideSign * Random.Range(0.35f, 1.1f);
                        }

                        Vector3 throwDir = Vector3.ProjectOnPlane((targetPoint + styleOffset) - releasePoint, Vector3.up);
                        if (throwDir.sqrMagnitude < 0.0001f)
                        {
                            throwDir = (flatRight * -sideSign) + (flatForward * Random.Range(-0.08f, 0.12f));
                        }
                        throwDir.Normalize();

                        DiceRoller.ThrowForceProfile throwForces = DiceRoller.CreateThrowForceProfile(throwDir, holdCharge);

                        SetDiceRenderersVisible(diceRenderers, true);
                        diceRb.isKinematic = false;
                        diceRb.useGravity = true;
                        diceRb.position = releasePoint;
                        diceTransform.rotation = throwForces.releaseRotation;
                        diceRb.rotation = throwForces.releaseRotation;
                        diceRb.linearVelocity = Vector3.zero;
                        diceRb.angularVelocity = Vector3.zero;
                        diceRb.AddForce(throwDir * throwForces.throwImpulse + Vector3.up * throwForces.upImpulse, ForceMode.Impulse);
                        diceRb.AddTorque(throwForces.throwSpin, ForceMode.Impulse);
                        diceRb.WakeUp();
                    }
                }

                if (releaseDetected)
                {
                    if (releaseStartTime > 0f && Time.time - releaseStartTime >= 6.5f)
                    {
                        break;
                    }

                    if (handImage != null && handRect != null && handReleaseStartTime >= 0f)
                    {
                        float releaseT = Mathf.Clamp01((Time.time - handReleaseStartTime) / 0.18f);
                        handRect.anchoredPosition = Vector2.Lerp(handHoldPos, handReleasePos, releaseT);
                        handRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-10f, -24f, releaseT));
                        handImage.color = new Color(1f, 1f, 1f, 1f - releaseT);
                        if (releaseT >= 1f)
                        {
                            Object.Destroy(handImage.gameObject);
                            handImage = null;
                            handRect = null;
                        }
                    }

                    float linearSpeed = diceRb.linearVelocity.magnitude;
                    float angularSpeed = diceRb.angularVelocity.magnitude;

                    if (linearSpeed > 0.2f || angularSpeed > 0.2f)
                    {
                        motionDetected = true;
                    }

                    if (motionDetected)
                    {
                        bool looksSettled = diceRb.IsSleeping()
                            || (linearSpeed < 0.08f && angularSpeed < 0.08f);

                        settleTimer = looksSettled ? settleTimer + Time.deltaTime : 0f;
                        if (settleTimer >= 0.55f)
                        {
                            break;
                        }
                    }
                }

                yield return null;
            }

            if (diceRb != null)
            {
                if (!diceRb.isKinematic)
                {
                    diceRb.linearVelocity = Vector3.zero;
                    diceRb.angularVelocity = Vector3.zero;
                }

                diceRb.useGravity = false;
                diceRb.isKinematic = true;
            }

            int finalRoll = DiceRoller.ResolveAnimatedD6Result(animatedDiceContext.diceStats != null ? animatedDiceContext.diceStats.side : -1);

            onResult?.Invoke(finalRoll);
            resultText.text = resultFormatter != null ? resultFormatter(finalRoll) : $"Result: {finalRoll}";

            yield return new WaitForSeconds(2.5f);

            // Clear dice overlay text so it doesn't bleed through the transparent table panel.
            titleText.text = string.Empty;
            hintText.text = string.Empty;
            resultText.text = string.Empty;

            // Show table overlay while camera is still aimed at the dice scene.
            if (afterResultShown != null)
            {
                yield return StartCoroutine(afterResultShown());
            }

            sceneCamera.transform.position = originalCamPos;
            sceneCamera.transform.rotation = originalCamRot;
            sceneCamera.fieldOfView = originalCamFov;
            sceneCamera.orthographic = originalCamOrtho;

            animatedDiceContext.Dispose();
            activeDiceWorldRoot = null;

            if (activeDiceOverlay == overlayObj)
            {
                Destroy(activeDiceOverlay);
                activeDiceOverlay = null;
            }
        }

        private static void EnsureAudioListenerForRoll(Camera sceneCamera)
        {
            if (sceneCamera == null)
            {
                return;
            }

            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            bool hasEnabled = false;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].enabled)
                {
                    hasEnabled = true;
                    break;
                }
            }

            AudioListener rollListener = sceneCamera.GetComponent<AudioListener>();
            if (!hasEnabled)
            {
                if (rollListener == null)
                {
                    rollListener = sceneCamera.gameObject.AddComponent<AudioListener>();
                }

                if (!rollListener.enabled)
                {
                    rollListener.enabled = true;
                }
            }

            AudioListener.pause = false;
            AudioListener.volume = 1f;
        }
    }
}
