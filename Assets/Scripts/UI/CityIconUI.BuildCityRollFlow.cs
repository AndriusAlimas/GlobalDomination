using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                Debug.Log($"City '{linkedCity.cityName}' already moved this turn.");
                CloseActionMenu();
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

            if (actionName == "Researching")
            {
                CloseActionMenu();
                StartCoroutine(BuildCityRollSceneScope.Run(this, PlayResearchingDiceRollAnimation));
                return;
            }

            Debug.Log($"City '{linkedCity?.cityName}' selected action: {actionName}");
        }

        // ── Action-specific thin wrappers ──────────────────────────────────────

        private IEnumerator PlayBuildCityDiceRollAnimation(Canvas canvas, Camera sceneCamera)
            => PlayDiceRollAnimation(canvas, sceneCamera,
                "Build New City",
                "Roll to discover your new city's starting building!",
                roll =>
                {
                    var building = GameData.BuildingRollTable.GetBuildingFromRoll(
                        UnityEngine.Random.Range(1, 7), roll);
                    if (building != null)
                    {
                        linkedCity?.AddBuilding(building);
                        Debug.Log($"City '{linkedCity?.cityName}' gained building: {building.displayName}");
                    }
                },
                roll => roll >= 4 ? $"New building unlocked! (Roll: {roll})" : $"No building this time. (Roll: {roll})");

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

                Debug.Log($"City '{linkedCity.cityName}' gained {rewardAmount} population (slot roll {slotRoll}).");
            }
            else
            {
                linkedCity.money += rewardAmount;
                if (moneyText != null)
                {
                    moneyText.text = linkedCity.money.ToString();
                }

                Debug.Log($"City '{linkedCity.cityName}' gained {rewardAmount} money (slot roll {slotRoll}).");
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

            Sprite[] diceFaces = CreateSlotDiceFaceSprites();
            dieImage.sprite = diceFaces[1];

            float elapsed = 0f;
            const float spinDuration = 1.1f;
            const float stepDuration = 0.07f;
            Vector3 baseScale = dieRect.localScale;

            while (elapsed < spinDuration)
            {
                dieImage.sprite = diceFaces[Random.Range(1, 7)];
                SlotSetDiceSpinVisual(dieRect, elapsed, spinDuration, baseScale);
                elapsed += stepDuration;
                yield return new WaitForSeconds(stepDuration);
            }

            for (int i = 0; i < 3; i++)
            {
                dieImage.sprite = diceFaces[Random.Range(1, 7)];
                SlotSetDiceSpinVisual(dieRect, spinDuration + (i * stepDuration), spinDuration + (3f * stepDuration), baseScale);
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

        private static Sprite[] CreateSlotDiceFaceSprites()
        {
            Sprite[] faces = new Sprite[7];
            for (int i = 1; i <= 6; i++)
            {
                faces[i] = CreateSlotDieFaceSprite(i);
            }

            return faces;
        }

        private static Sprite CreateSlotDieFaceSprite(int value)
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
                SlotDrawPip(texture, center, pipRadius, pipColor);
            }

            if (value >= 2)
            {
                SlotDrawPip(texture, topLeft, pipRadius, pipColor);
                SlotDrawPip(texture, botRight, pipRadius, pipColor);
            }

            if (value >= 4)
            {
                SlotDrawPip(texture, topRight, pipRadius, pipColor);
                SlotDrawPip(texture, botLeft, pipRadius, pipColor);
            }

            if (value == 6)
            {
                SlotDrawPip(texture, midLeft, pipRadius, pipColor);
                SlotDrawPip(texture, midRight, pipRadius, pipColor);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void SlotDrawPip(Texture2D texture, Vector2 center, int radius, Color color)
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
                    if (Mathf.Sqrt(dx * dx + dy * dy) <= maxDist)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void SlotSetDiceSpinVisual(RectTransform dieRect, float elapsed, float duration, Vector3 baseScale)
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
                        Debug.Log($"City '{linkedCity?.cityName}' gained {gainedPower} city power (bonus roll: {roll}).");
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

        private IEnumerator PlayResearchingDiceRollAnimation(Canvas canvas, Camera sceneCamera)
            => PlayDiceRollAnimation(canvas, sceneCamera,
                "Researching",
                "Roll to generate gold from your researchers!",
                roll =>
                {
                    if (linkedCity != null) linkedCity.money += roll;
                    Debug.Log($"City '{linkedCity?.cityName}' gained {roll} gold from research.");
                },
                roll => $"+{roll} Gold from Research!");

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

            // First roll: Building category
            yield return tempRoller.PlayDiceRollAnimation(canvas, sceneCamera,
                "Rolling for Starting Building",
                "First roll: Building category (1-6)!",
                roll => firstRoll = roll,
                roll => $"Category: {roll}");

            // Second roll: Specific building
            yield return tempRoller.PlayDiceRollAnimation(canvas, sceneCamera,
                "Rolling for Starting Building",
                "Second roll: Specific building (1-6)!",
                roll => secondRoll = roll,
                roll => $"Building: {roll}");

            // Get the building from both rolls
            var building = GameData.BuildingRollTable.GetBuildingFromRoll(firstRoll, secondRoll);
            if (building == null)
            {
                building = GameData.BuildingRollTable.RollForFirstBuilding();
            }

            if (building != null)
            {
                targetCity.AddBuilding(building);
                Debug.Log($"{targetCity.cityName} gained starting building: {building.displayName} (Rolls: {firstRoll}, {secondRoll})");
            }

            onCompleted?.Invoke(building, firstRoll, secondRoll);

            Destroy(tempObj);
        }

        // ── Shared dice-roll animation engine ─────────────────────────────────

        private IEnumerator PlayDiceRollAnimation(
            Canvas canvas,
            Camera sceneCamera,
            string actionTitle,
            string hintLabel,
            System.Action<int> onResult,
            System.Func<int, string> resultFormatter)
        {
            if (sceneCamera == null)
            {
                yield break;
            }

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
            Debug.Log($"City '{linkedCity?.cityName}' [{actionTitle}] roll: {finalRoll}");

            yield return new WaitForSeconds(2.5f);

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
    }
}
