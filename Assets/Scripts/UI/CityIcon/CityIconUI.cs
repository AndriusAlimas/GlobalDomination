using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GlobalDomination.GameData;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace GlobalDomination.UI
{
    /// <summary>
    /// Single-city icon on the HUD. Population tier still selects building artwork (sprite variant),
    /// but layout size is fixed so every city uses the same footprint.
    /// </summary>
    public partial class CityIconUI : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Image cityIcon;
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private TextMeshProUGUI cityNameText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI powerText;
        [SerializeField] private Image populationPlateImage;
        [SerializeField] private Image moneyPlateImage;
        [SerializeField] private Image powerPlateImage;
        [SerializeField] private Image cityNameBackground;
        [SerializeField] private Image capitalStarIcon;
        [SerializeField] private Image backgroundCircle;
        [SerializeField] private Image turnStatusDot;
        [SerializeField] private Image[] constructionBarSegments;
        [SerializeField] private CanvasGroup cityCanvasGroup;
        [SerializeField] private Color defaultBackgroundColor = Color.white;
        [SerializeField] private Color defaultCityIconColor = Color.white;
        [SerializeField] private Color defaultCityNameColor = Color.white;
        [SerializeField] private Color defaultCityNameBackgroundColor = Color.white;

        private static readonly Dictionary<int, Sprite> transparentSpriteCache = new Dictionary<int, Sprite>();
        private static readonly string[] animatedDiceD6PrefabPaths =
        {
            "Assets/Animated Dice (Random Art Attack)/HighPolyDice/6SidedHighPoly.prefab",
            "Assets/Animated Dice (Random Art Attack)/HighPolyDice/6SidedHighPoly Variant 17.prefab",
            "Assets/Animated Dice (Random Art Attack)/HighPolyDice/6SidedPipsHighPoly.prefab"
        };
        private const float DiceArenaWallHeight = 14f;
        private static GameObject activeActionMenu;
        private static CityIconUI activeMenuOwner;
        private static Sprite actionCardSprite;
        private static GameObject activeDiceOverlay;
        private static GameObject activeDiceWorldRoot;

        private City linkedCity;

        private sealed class AnimatedDiceWorldContext
        {
            public GameObject root;
            public GameObject diceObject;
            public DiceStats diceStats;
            public Rigidbody rigidbody;
            public DiceImpactAudio impactAudio;
            public RollDrop rollDrop;
            public Material floorMaterial;
            public PhysicsMaterial arenaPhysicsMaterial;
            public PhysicsMaterial arenaWallPhysicsMaterial;
            public List<Material> runtimeDiceMaterials;
            public Vector3 boundsCenter;
            public float boundsHalfExtent;
            public float floorY;

            public void Dispose()
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (floorMaterial != null)
                {
                    Object.Destroy(floorMaterial);
                }

                if (arenaPhysicsMaterial != null)
                {
                    Object.Destroy(arenaPhysicsMaterial);
                }

                if (arenaWallPhysicsMaterial != null)
                {
                    Object.Destroy(arenaWallPhysicsMaterial);
                }

                if (runtimeDiceMaterials != null)
                {
                    for (int i = 0; i < runtimeDiceMaterials.Count; i++)
                    {
                        if (runtimeDiceMaterials[i] != null)
                        {
                            Object.Destroy(runtimeDiceMaterials[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a city icon UI programmatically. Layout is a fixed size; population tier only affects which building sprite is shown.
        /// </summary>
        public static CityIconUI CreateCityIcon(Transform parent, Vector2 position, City city)
        {
            // Main container
            GameObject container = new GameObject($"CityIcon_{city.cityName}");
            container.transform.SetParent(parent, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            
            // Sprite variant by population tier (art only — UI scale is not tied to tier).
            int sizeTier = GetPopulationTier(city.healthPoints);
            Sprite customCitySprite = TryLoadCustomCitySprite(sizeTier);
            bool usingCustomSprite = customCitySprite != null;
            
            const float baseSize = 150f;
            float citySize = baseSize;
            float backgroundSize = citySize + 15f;
            float containerHeight = backgroundSize + 86f;
            
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(0f, 1f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = position;
            containerRect.sizeDelta = new Vector2(containerHeight, containerHeight);

            // Transparent click surface so each city can open an action menu.
            Image clickSurface = container.AddComponent<Image>();
            clickSurface.color = new Color(1f, 1f, 1f, 0.001f);
            Button clickButton = container.AddComponent<Button>();
            clickButton.transition = Selectable.Transition.None;
            clickButton.targetGraphic = clickSurface;
            
            CityIconUI cityIconUI = container.AddComponent<CityIconUI>();
            clickButton.onClick.AddListener(cityIconUI.ShowActionMenu);
            
            // Background circle
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(container.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = CreateCircleSprite();
            
            // Color based on size tier and capital status
            Color bgColor;
            if (city.isCapital)
            {
                bgColor = new Color(0.34f, 0.28f, 0.17f, 0.9f);
            }
            else
            {
                const float darkness = 0.22f;
                bgColor = new Color(darkness, darkness + 0.03f, darkness + 0.08f, 0.9f);
            }
            bgImage.color = bgColor;
            
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(0f, 15f);
            bgRect.sizeDelta = new Vector2(backgroundSize, backgroundSize);

            // When using custom PNG sprite, keep presentation transparent.
            bgImage.gameObject.SetActive(!usingCustomSprite);
            
            cityIconUI.backgroundCircle = bgImage;
            cityIconUI.defaultBackgroundColor = bgImage.color;
            
            // City icon (building symbol)
            GameObject iconObj = new GameObject("CityIcon");
            iconObj.transform.SetParent(container.transform, false);
            Image cityImage = iconObj.AddComponent<Image>();
            cityImage.sprite = usingCustomSprite
                ? MakeWhiteBackgroundTransparent(customCitySprite)
                : CreateCiv2CitySprite(sizeTier, city.isCapital);
            cityImage.color = Color.white;
            cityImage.preserveAspect = true;

            float customScaleMultiplier = usingCustomSprite ? 2f : 1f;
            
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 17f);
            iconRect.sizeDelta = new Vector2(citySize * 1.08f * customScaleMultiplier, citySize * 1.08f * customScaleMultiplier);
            
            cityIconUI.cityIcon = cityImage;
            cityIconUI.defaultCityIconColor = cityImage.color;
            cityIconUI.linkedCity = city;

            // Population plate to mimic classic Civ-style center number badge.
            GameObject plateObj = new GameObject("PopulationPlate");
            plateObj.transform.SetParent(container.transform, false);
            Image plateImage = plateObj.AddComponent<Image>();
            plateImage.sprite = CreatePopulationPlateSprite();
            plateImage.color = new Color(0.95f, 0.84f, 0.46f, usingCustomSprite ? 0.72f : 0.82f);

            RectTransform plateRect = plateObj.GetComponent<RectTransform>();
            plateRect.anchorMin = new Vector2(0.5f, 0.5f);
            plateRect.anchorMax = new Vector2(0.5f, 0.5f);
            plateRect.pivot = new Vector2(0.5f, 0.5f);
            plateRect.anchoredPosition = new Vector2(0f, 14f);
            plateRect.sizeDelta = new Vector2(citySize * 0.44f, citySize * 0.27f);
            
            // Population text (overlaid on icon)
            GameObject popTextObj = new GameObject("PopulationText");
            popTextObj.transform.SetParent(container.transform, false);
            TextMeshProUGUI popText = popTextObj.AddComponent<TextMeshProUGUI>();
            popText.text = city.healthPoints.ToString();
            popText.fontSize = 27f;
            popText.fontStyle = FontStyles.Bold;
            popText.alignment = TextAlignmentOptions.Center;
            popText.color = new Color(0.08f, 0.1f, 0.16f, 1f);
            popText.outlineWidth = 0.3f;
            popText.outlineColor = new Color(1f, 1f, 1f, 0.65f);
            
            RectTransform popTextRect = popTextObj.GetComponent<RectTransform>();
            popTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            popTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            popTextRect.pivot = new Vector2(0.5f, 0.5f);
            popTextRect.anchoredPosition = new Vector2(0f, 14f);
            popTextRect.sizeDelta = new Vector2(citySize * 0.54f, citySize * 0.32f);
            
            cityIconUI.populationText = popText;
            cityIconUI.populationPlateImage = plateImage;

            // Circular badge arrangement (population at top, money at bottom-right, power at bottom-left)
            float badgeRadius = citySize * 0.38f;
            float badgeOffsetY = 10f; // slight downward offset for better visual balance
            
            // Population badge repositioned to top of circle (90 degrees)
            plateRect.anchoredPosition = new Vector2(0f, badgeRadius + badgeOffsetY);
            popTextRect.anchoredPosition = plateRect.anchoredPosition;

            // Money badge at bottom-right (330 degrees / -30 degrees)
            float moneyAngle = -30f * Mathf.Deg2Rad;
            Vector2 moneyPos = new Vector2(badgeRadius * Mathf.Cos(moneyAngle), badgeRadius * Mathf.Sin(moneyAngle) + badgeOffsetY);
            
            GameObject moneyPlateObj = new GameObject("MoneyPlate");
            moneyPlateObj.transform.SetParent(container.transform, false);
            Image moneyPlateImage = moneyPlateObj.AddComponent<Image>();
            moneyPlateImage.sprite = CreatePopulationPlateSprite();
            moneyPlateImage.color = new Color(0.6f, 0.9f, 0.62f, usingCustomSprite ? 0.74f : 0.82f);

            RectTransform moneyPlateRect = moneyPlateObj.GetComponent<RectTransform>();
            moneyPlateRect.anchorMin = new Vector2(0.5f, 0.5f);
            moneyPlateRect.anchorMax = new Vector2(0.5f, 0.5f);
            moneyPlateRect.pivot = new Vector2(0.5f, 0.5f);
            moneyPlateRect.anchoredPosition = moneyPos;
            moneyPlateRect.sizeDelta = new Vector2(citySize * 0.35f, citySize * 0.22f);

            GameObject moneyTextObj = new GameObject("MoneyText");
            moneyTextObj.transform.SetParent(container.transform, false);
            TextMeshProUGUI moneyText = moneyTextObj.AddComponent<TextMeshProUGUI>();
            moneyText.text = city.money.ToString();
            moneyText.fontSize = 19f;
            moneyText.fontStyle = FontStyles.Bold;
            moneyText.alignment = TextAlignmentOptions.Center;
            moneyText.color = new Color(0.02f, 0.35f, 0.08f, 1f);
            moneyText.outlineWidth = 0.25f;
            moneyText.outlineColor = new Color(1f, 1f, 1f, 0.65f);

            RectTransform moneyTextRect = moneyTextObj.GetComponent<RectTransform>();
            moneyTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            moneyTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            moneyTextRect.pivot = new Vector2(0.5f, 0.5f);
            moneyTextRect.anchoredPosition = moneyPos;
            moneyTextRect.sizeDelta = new Vector2(citySize * 0.42f, citySize * 0.24f);
            cityIconUI.moneyText = moneyText;
            cityIconUI.moneyPlateImage = moneyPlateImage;

            // Power badge at bottom-left (210 degrees) in red theme
            float powerAngle = 210f * Mathf.Deg2Rad;
            Vector2 powerPos = new Vector2(badgeRadius * Mathf.Cos(powerAngle), badgeRadius * Mathf.Sin(powerAngle) + badgeOffsetY);
            
            GameObject powerPlateObj = new GameObject("PowerPlate");
            powerPlateObj.transform.SetParent(container.transform, false);
            Image powerPlateImage = powerPlateObj.AddComponent<Image>();
            powerPlateImage.sprite = CreatePopulationPlateSprite();
            powerPlateImage.color = new Color(0.95f, 0.45f, 0.42f, usingCustomSprite ? 0.74f : 0.82f);

            RectTransform powerPlateRect = powerPlateObj.GetComponent<RectTransform>();
            powerPlateRect.anchorMin = new Vector2(0.5f, 0.5f);
            powerPlateRect.anchorMax = new Vector2(0.5f, 0.5f);
            powerPlateRect.pivot = new Vector2(0.5f, 0.5f);
            powerPlateRect.anchoredPosition = powerPos;
            powerPlateRect.sizeDelta = new Vector2(citySize * 0.35f, citySize * 0.22f);

            GameObject powerTextObj = new GameObject("PowerText");
            powerTextObj.transform.SetParent(container.transform, false);
            TextMeshProUGUI powerText = powerTextObj.AddComponent<TextMeshProUGUI>();
            powerText.text = city.cityPower.ToString();
            powerText.fontSize = 19f;
            powerText.fontStyle = FontStyles.Bold;
            powerText.alignment = TextAlignmentOptions.Center;
            powerText.color = new Color(0.6f, 0.02f, 0.0f, 1f);
            powerText.outlineWidth = 0.25f;
            powerText.outlineColor = new Color(1f, 1f, 1f, 0.65f);

            RectTransform powerTextRect = powerTextObj.GetComponent<RectTransform>();
            powerTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            powerTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            powerTextRect.pivot = new Vector2(0.5f, 0.5f);
            powerTextRect.anchoredPosition = powerPos;
            powerTextRect.sizeDelta = new Vector2(citySize * 0.42f, citySize * 0.24f);
            cityIconUI.powerText = powerText;
            cityIconUI.powerPlateImage = powerPlateImage;

            // Segmented construction bar: gap above name, gap below main icon cluster.
            float barWidth = Mathf.Min(containerHeight + 20f, 296f);
            const float barHeight = 18f;
            const float gapAboveName = 8f;
            const float namePlateTopFromBottom = 3f + 56f;
            float barBottomY = namePlateTopFromBottom + gapAboveName;

            GameObject barTrackObj = new GameObject("ConstructionBarTrack");
            barTrackObj.transform.SetParent(container.transform, false);
            RectTransform barTrackRect = barTrackObj.AddComponent<RectTransform>();
            barTrackRect.anchorMin = new Vector2(0.5f, 0f);
            barTrackRect.anchorMax = new Vector2(0.5f, 0f);
            barTrackRect.pivot = new Vector2(0.5f, 0f);
            barTrackRect.anchoredPosition = new Vector2(0f, barBottomY);
            barTrackRect.sizeDelta = new Vector2(barWidth, barHeight);

            Image barTrackImage = barTrackObj.AddComponent<Image>();
            barTrackImage.sprite = CreateCityNameCardSprite();
            barTrackImage.type = Image.Type.Sliced;
            barTrackImage.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);

            GameObject barSegmentsRow = new GameObject("ConstructionBarSegments");
            barSegmentsRow.transform.SetParent(barTrackObj.transform, false);
            RectTransform rowRect = barSegmentsRow.AddComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = new Vector2(4f, 3f);
            rowRect.offsetMax = new Vector2(-4f, -3f);

            int segments = CityConstruction.SegmentCount;
            float rowInnerWidth = barWidth - 8f;
            float gap = 3f;
            float segW = (rowInnerWidth - (segments - 1) * gap) / segments;

            var segmentImages = new Image[segments];
            Sprite segmentSprite = CreateCityNameCardSprite();
            for (int si = 0; si < segments; si++)
            {
                GameObject segObj = new GameObject($"Segment_{si}");
                segObj.transform.SetParent(barSegmentsRow.transform, false);
                RectTransform segRt = segObj.AddComponent<RectTransform>();
                segRt.anchorMin = new Vector2(0f, 0f);
                segRt.anchorMax = new Vector2(0f, 1f);
                segRt.pivot = new Vector2(0f, 0.5f);
                segRt.anchoredPosition = new Vector2(si * (segW + gap), 0f);
                segRt.sizeDelta = new Vector2(segW, 0f);

                Image segImg = segObj.AddComponent<Image>();
                segImg.sprite = segmentSprite;
                segImg.type = Image.Type.Sliced;
                segImg.raycastTarget = false;
                segmentImages[si] = segImg;
            }

            cityIconUI.constructionBarSegments = segmentImages;
            cityIconUI.RefreshConstructionBarVisual();
            
            // City name plate to keep label readable above busy icon art.
            GameObject nameBgObj = new GameObject("CityNamePlate");
            nameBgObj.transform.SetParent(container.transform, false);
            Image nameBg = nameBgObj.AddComponent<Image>();
            nameBg.sprite = CreateCityNameCardSprite();
            nameBg.type = Image.Type.Sliced;
            nameBg.color = new Color(0.95f, 0.84f, 0.46f, 0.88f);

            RectTransform nameBgRect = nameBgObj.GetComponent<RectTransform>();
            nameBgRect.anchorMin = new Vector2(0.5f, 0f);
            nameBgRect.anchorMax = new Vector2(0.5f, 0f);
            nameBgRect.pivot = new Vector2(0.5f, 0f);
            nameBgRect.anchoredPosition = new Vector2(0f, 3f);
            nameBgRect.sizeDelta = new Vector2(containerHeight + 42f, 56f);

            // City name text (below icon)
            GameObject nameTextObj = new GameObject("CityNameText");
            nameTextObj.transform.SetParent(container.transform, false);
            TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
            nameText.text = city.cityName;
            nameText.fontSize = 27f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.fontWeight = FontWeight.Black;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = new Color(0.06f, 0.07f, 0.1f, 1f);
            nameText.outlineWidth = 0.3f;
            nameText.outlineColor = new Color(1f, 1f, 1f, 0.65f);
            
            RectTransform nameTextRect = nameTextObj.GetComponent<RectTransform>();
            nameTextRect.anchorMin = new Vector2(0.5f, 0f);
            nameTextRect.anchorMax = new Vector2(0.5f, 0f);
            nameTextRect.pivot = new Vector2(0.5f, 0f);
            nameTextRect.anchoredPosition = new Vector2(0f, 5f);
            nameTextRect.sizeDelta = new Vector2(containerHeight + 24f, 56f);
            
            cityIconUI.cityNameBackground = nameBg;
            cityIconUI.defaultCityNameBackgroundColor = nameBg.color;
            cityIconUI.cityNameText = nameText;
            cityIconUI.defaultCityNameColor = nameText.color;
            
            // Capital star icon (top-right corner)
            if (city.isCapital)
            {
                GameObject starObj = new GameObject("CapitalStar");
                starObj.transform.SetParent(container.transform, false);
                Image starImage = starObj.AddComponent<Image>();
                starImage.sprite = CreateStarSprite();
                starImage.color = new Color(1f, 0.9f, 0.35f, 1f);
                
                RectTransform starRect = starObj.GetComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                float starOffset = backgroundSize * 0.45f;
                starRect.anchoredPosition = new Vector2(starOffset + 8f, starOffset + 2f);
                starRect.sizeDelta = new Vector2(35f, 35f);

                cityIconUI.capitalStarIcon = starImage;
            }

            // Small turn-status dot near the top-right star area (green=ready, red=finished).
            GameObject statusDotObj = new GameObject("TurnStatusDot");
            statusDotObj.transform.SetParent(container.transform, false);
            Image statusDotImage = statusDotObj.AddComponent<Image>();
            statusDotImage.sprite = CreateCircleSprite();
            statusDotImage.color = city.hasTakenTurn
                ? new Color(0.9f, 0.22f, 0.22f, 0.95f)
                : new Color(0.2f, 0.85f, 0.28f, 0.95f);

            RectTransform statusDotRect = statusDotObj.GetComponent<RectTransform>();
            statusDotRect.anchorMin = new Vector2(0.5f, 0.5f);
            statusDotRect.anchorMax = new Vector2(0.5f, 0.5f);
            statusDotRect.pivot = new Vector2(0.5f, 0.5f);
            float indicatorOffset = backgroundSize * 0.45f;
            statusDotRect.anchoredPosition = new Vector2(indicatorOffset - 16f, indicatorOffset + 12f);
            statusDotRect.sizeDelta = new Vector2(15f, 15f);
            cityIconUI.turnStatusDot = statusDotImage;

            // Easy turn indicator: dim the whole city icon when this city already took its turn.
            CanvasGroup cityCanvas = container.AddComponent<CanvasGroup>();
            cityCanvas.alpha = city.hasTakenTurn ? 0.34f : 1f;
            cityIconUI.cityCanvasGroup = cityCanvas;
            
            return cityIconUI;
        }

        private void RefreshConstructionBarVisual()
        {
            if (constructionBarSegments == null || constructionBarSegments.Length == 0 || linkedCity == null)
            {
                return;
            }

            int progress = Mathf.Clamp(linkedCity.constructionProgress, 0, CityConstruction.PointsRequired);
            int n = constructionBarSegments.Length;
            for (int i = 0; i < n; i++)
            {
                Image img = constructionBarSegments[i];
                if (img == null)
                {
                    continue;
                }

                bool filled = i < progress;
                float heat = (i + 0.5f) / n;
                if (filled)
                {
                    img.color = GetConstructionHeatColor(heat);
                }
                else
                {
                    img.color = new Color(0.14f, 0.14f, 0.16f, 0.72f);
                }
            }
        }

        private static Color GetConstructionHeatColor(float t)
        {
            t = Mathf.Clamp01(t);
            Color red = new Color(0.9f, 0.2f, 0.22f, 1f);
            Color yellow = new Color(0.98f, 0.82f, 0.18f, 1f);
            Color green = new Color(0.22f, 0.78f, 0.4f, 1f);
            if (t < 0.5f)
            {
                return Color.Lerp(red, yellow, t * 2f);
            }

            return Color.Lerp(yellow, green, (t - 0.5f) * 2f);
        }
        
        /// <summary>
        /// Population band (1–4) for sprite / artwork selection only.
        /// </summary>
        private static int GetPopulationTier(int population)
        {
            if (population >= 30) return 4; // Metropolis
            if (population >= 20) return 3; // Large City
            if (population >= 10) return 2; // Medium City
            return 1; // Small City/Village
        }

        private static Sprite LoadCitySpriteOrFallback(int sizeTier, bool isCapital)
        {
            Sprite custom = TryLoadCustomCitySprite(sizeTier);
            if (custom != null)
            {
                return custom;
            }

            // If no external asset is available, keep procedural sprite fallback.
            return CreateCiv2CitySprite(sizeTier, isCapital);
        }

        private static Sprite TryLoadCustomCitySprite(int sizeTier)
        {
            // Prefer exact tier; if missing, use tier1 as universal fallback.
            int[] candidates = { sizeTier, 1 };

            foreach (int tier in candidates)
            {
                // Runtime/build path if user places sprites under any Resources folder.
                string[] resourcePaths =
                {
                    $"Art/CitySkins/Default/city_tier{tier}",
                    $"Art/CitySkins/city_tier{tier}",
                    $"Art/city_tier{tier}",
                    $"CitySkins/Default/city_tier{tier}",
                    $"CitySkins/city_tier{tier}",
                    $"city_tier{tier}"
                };

                foreach (string resourcePath in resourcePaths)
                {
                    Sprite loaded = Resources.Load<Sprite>(resourcePath);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }

#if UNITY_EDITOR
                // Editor path for assets placed directly under Assets/Art/... without Resources folder.
                string[] assetPaths =
                {
                    $"Assets/Art/CitySkins/Default/city_tier{tier}.png",
                    $"Assets/Art/CitySkins/city_tier{tier}.png",
                    $"Assets/Art/city_tier{tier}.png"
                };

                foreach (string assetPath in assetPaths)
                {
                    EnsureSpriteImporterSettings(assetPath);
                    Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }

                Sprite searched = TryLoadEditorSpriteBySearch(tier);
                if (searched != null)
                {
                    return searched;
                }
#endif
            }

            return null;
        }

        private static Sprite MakeWhiteBackgroundTransparent(Sprite source)
        {
            if (source == null)
            {
                return null;
            }

            int key = source.GetInstanceID();
            if (transparentSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D sourceTexture = source.texture;
            if (sourceTexture == null)
            {
                return source;
            }

            try
            {
                Rect rect = source.textureRect;
                int width = Mathf.RoundToInt(rect.width);
                int height = Mathf.RoundToInt(rect.height);
                int x = Mathf.RoundToInt(rect.x);
                int y = Mathf.RoundToInt(rect.y);

                Color[] pixels = sourceTexture.GetPixels(x, y, width, height);
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color p = pixels[i];
                    // Treat near-white as background and make it transparent.
                    if (p.r > 0.9f && p.g > 0.9f && p.b > 0.9f)
                    {
                        pixels[i] = new Color(p.r, p.g, p.b, 0f);
                    }
                }

                Texture2D transparentTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                transparentTexture.filterMode = sourceTexture.filterMode;
                transparentTexture.SetPixels(pixels);
                transparentTexture.Apply();

                Sprite processed = Sprite.Create(
                    transparentTexture,
                    new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.5f),
                    source.pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    source.border);

                transparentSpriteCache[key] = processed;
                return processed;
            }
            catch
            {
                // If texture is not readable, keep original sprite.
                return source;
            }
        }

#if UNITY_EDITOR
        private static Sprite TryLoadEditorSpriteBySearch(int tier)
        {
            string[] folders =
            {
                "Assets/Art/CitySkins/Default",
                "Assets/Art/CitySkins",
                "Assets/Art"
            };

            string tierToken = $"tier{tier}";

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("city tier t:Texture2D", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                    // Match exact tier first, but allow generic city_tier1 fallback call path to handle both.
                    if (!fileName.Contains("city") || !fileName.Contains(tierToken))
                    {
                        continue;
                    }

                    EnsureSpriteImporterSettings(path);

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        return sprite;
                    }

                    // If imported as Texture2D instead of Sprite, create a runtime sprite fallback.
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null)
                    {
                        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
            }

            return null;
        }

        private static void EnsureSpriteImporterSettings(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
#endif
        
        /// <summary>
        /// Creates a simple circle sprite for the background.
        /// </summary>
        private static Sprite CreateCircleSprite()
        {
            int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (distance <= radius)
                    {
                        // Soft edge
                        float alpha = Mathf.Clamp01((radius - distance + 2f) / 4f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Creates a ring sprite for the pulsing border effect.
        /// </summary>
        private static Sprite CreateRingSprite()
        {
            int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size / 2f - 2f;
            float ringThickness = 4f;
            float innerRadius = outerRadius - ringThickness;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (distance >= innerRadius && distance <= outerRadius)
                    {
                        // Create smooth edges for the ring
                        float alpha = 1f;
                        
                        // Soft outer edge
                        if (distance > outerRadius - 2f)
                        {
                            alpha *= Mathf.Clamp01((outerRadius - distance + 1f) / 2f);
                        }
                        
                        // Soft inner edge
                        if (distance < innerRadius + 2f)
                        {
                            alpha *= Mathf.Clamp01((distance - innerRadius + 1f) / 2f);
                        }
                        
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Creates a simple 5-point star sprite to mark capital cities.
        /// </summary>
        private static Sprite CreateStarSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerRadius = size * 0.42f;
            float innerRadius = outerRadius * 0.45f;

            Vector2[] points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angleDeg = -90f + i * 36f;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                points[i] = center + new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (!IsPointInPolygon(p, points))
                    {
                        continue;
                    }

                    Color fill = new Color(1f, 1f, 1f, 1f);
                    bool nearEdge = false;
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector2 a = points[i];
                        Vector2 b = points[(i + 1) % points.Length];
                        if (DistancePointToSegment(p, a, b) < 1.6f)
                        {
                            nearEdge = true;
                            break;
                        }
                    }

                    if (nearEdge)
                    {
                        fill = new Color(0.8f, 0.62f, 0.12f, 1f);
                    }

                    texture.SetPixel(x, y, fill);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                bool intersect = ((polygon[i].y > point.y) != (polygon[j].y > point.y))
                    && (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                    / (polygon[j].y - polygon[i].y + Mathf.Epsilon) + polygon[i].x);
                if (intersect)
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(point - a, ab) / (ab.sqrMagnitude + Mathf.Epsilon);
            t = Mathf.Clamp01(t);
            Vector2 closest = a + t * ab;
            return Vector2.Distance(point, closest);
        }
        
        /// <summary>
        /// Creates a Civilization-2 inspired city sprite with an isometric base and skyline.
        /// </summary>
        private static Sprite CreateCiv2CitySprite(int sizeTier, bool isCapital)
        {
            int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            
            // Clear background
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
            
            Color baseTop = isCapital ? new Color(0.58f, 0.52f, 0.31f, 1f) : new Color(0.27f, 0.46f, 0.34f, 1f);
            Color baseLeft = isCapital ? new Color(0.37f, 0.3f, 0.17f, 1f) : new Color(0.16f, 0.29f, 0.22f, 1f);
            Color baseRight = isCapital ? new Color(0.47f, 0.39f, 0.23f, 1f) : new Color(0.2f, 0.36f, 0.27f, 1f);
            Color buildingMain = isCapital ? new Color(0.97f, 0.89f, 0.63f, 1f) : new Color(0.88f, 0.94f, 1f, 1f);
            Color buildingShade = isCapital ? new Color(0.78f, 0.68f, 0.44f, 1f) : new Color(0.66f, 0.76f, 0.86f, 1f);
            Color windows = isCapital ? new Color(1f, 0.95f, 0.72f, 1f) : new Color(1f, 1f, 1f, 1f);

            int cx = size / 2;
            int topY = 56;
            int midY = 68;
            int botY = 82;

            // Isometric ground tile.
            DrawTriangle(texture, new Vector2Int(cx, topY), new Vector2Int(20, midY), new Vector2Int(cx, botY), baseLeft);
            DrawTriangle(texture, new Vector2Int(cx, topY), new Vector2Int(cx, botY), new Vector2Int(76, midY), baseRight);
            DrawTriangle(texture, new Vector2Int(cx, topY - 1), new Vector2Int(20, midY), new Vector2Int(76, midY), baseTop);

            // Road stripe.
            DrawLine(texture, 24, midY - 1, 72, midY - 1, new Color(0.24f, 0.27f, 0.31f, 1f));
            DrawLine(texture, 26, midY, 70, midY, new Color(0.52f, 0.56f, 0.6f, 1f));
            
            switch (sizeTier)
            {
                case 1:
                    DrawIsoBuilding(texture, 40, 36, 16, 16, buildingMain, buildingShade, windows);
                    break;
                    
                case 2:
                    DrawIsoBuilding(texture, 30, 38, 12, 14, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 46, 30, 16, 22, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 62, 40, 10, 12, buildingMain, buildingShade, windows);
                    break;
                    
                case 3:
                    DrawIsoBuilding(texture, 26, 38, 10, 14, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 36, 34, 12, 18, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 48, 24, 16, 28, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 64, 34, 12, 18, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 74, 40, 8, 12, buildingMain, buildingShade, windows);
                    break;
                    
                default:
                    DrawIsoBuilding(texture, 20, 40, 8, 12, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 28, 34, 10, 18, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 38, 26, 12, 26, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 50, 20, 14, 32, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 64, 26, 12, 26, buildingMain, buildingShade, windows);
                    DrawIsoBuilding(texture, 76, 36, 8, 16, buildingMain, buildingShade, windows);
                    break;
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f);
        }

        private static Sprite CreatePopulationPlateSprite()
        {
            const int width = 64;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            float rx = width * 0.48f;
            float ry = height * 0.45f;
            Vector2 c = new Vector2(width * 0.5f, height * 0.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - c.x) / rx;
                    float dy = (y - c.y) / ry;
                    float d = dx * dx + dy * dy;
                    if (d > 1f)
                    {
                        continue;
                    }

                    Color color = new Color(1f, 1f, 1f, 0.94f);
                    if (d > 0.84f)
                    {
                        color = new Color(0.58f, 0.66f, 0.78f, 0.98f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
        }

        private static void DrawIsoBuilding(Texture2D texture, int x, int y, int w, int h, Color left, Color right, Color window)
        {
            int half = Mathf.Max(1, w / 2);

            // Main facade split into left/right shades for isometric feel.
            DrawRect(texture, x, y, half, h, left);
            DrawRect(texture, x + half, y, w - half, h, right);

            // Roof strip.
            DrawRect(texture, x - 1, y - 2, w + 2, 2, new Color(0.9f, 0.95f, 1f, 0.7f));

            // Crisp silhouette outline so city shape remains readable at small sizes.
            Color outline = new Color(0.08f, 0.1f, 0.14f, 1f);
            DrawRect(texture, x - 1, y - 1, w + 2, 1, outline);
            DrawRect(texture, x - 1, y + h, w + 2, 1, outline);
            DrawRect(texture, x - 1, y - 1, 1, h + 2, outline);
            DrawRect(texture, x + w, y - 1, 1, h + 2, outline);

            // Windows grid.
            for (int wy = y + 3; wy < y + h - 2; wy += 4)
            {
                for (int wx = x + 2; wx < x + w - 1; wx += 4)
                {
                    DrawRect(texture, wx, wy, 2, 2, window);
                }
            }

            // Small rooftop antenna detail to reinforce "city" shape.
            int antennaX = x + (w / 2);
            DrawRect(texture, antennaX, y - 5, 1, 3, outline);
            DrawRect(texture, antennaX - 1, y - 5, 3, 1, outline);
        }

        private static void DrawTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
        {
            int minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            int maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            int maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (PointInTriangle(new Vector2(x + 0.5f, y + 0.5f), a, b, c))
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = Sign(p, a, b);
            float s2 = Sign(p, b, c);
            float s3 = Sign(p, c, a);
            bool hasNeg = (s1 < 0f) || (s2 < 0f) || (s3 < 0f);
            bool hasPos = (s1 > 0f) || (s2 > 0f) || (s3 > 0f);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                SetPixelSafe(texture, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }
        
        /// <summary>
        /// Helper method to draw a rectangle on a texture.
        /// </summary>
        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height && py < texture.height; py++)
            {
                for (int px = x; px < x + width && px < texture.width; px++)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }

        private void ShowActionMenu()
        {
            if (linkedCity == null)
            {
                Debug.LogError("[CityIconUI] ShowActionMenu: linkedCity is null!");
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[CityIconUI] ShowActionMenu: No Canvas found in parent!");
                return;
            }

            if (activeActionMenu != null && activeMenuOwner == this)
            {
                CloseActionMenu();
                return;
            }

            if (activeActionMenu != null)
            {
                Destroy(activeActionMenu);
                activeActionMenu = null;
                activeMenuOwner = null;
            }

            GameObject panelObj = new GameObject($"CityActionMenu_{linkedCity.cityName}");
            panelObj.transform.SetParent(canvas.transform, false);
            panelObj.transform.SetAsLastSibling();
            activeActionMenu = panelObj;
            activeMenuOwner = this;

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(380f, 420f);

            RectTransform cityRect = transform as RectTransform;
            RectTransform canvasRect = canvas.transform as RectTransform;
            float panelWidth = panelRect.sizeDelta.x;
            float panelHeight = panelRect.sizeDelta.y;
            const float safeMargin = 18f;
            const float menuNudgeRight = 14f;

            if (cityRect != null && canvasRect != null)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, cityRect.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 cityLocalPoint);

                float cityHalfWidth = Mathf.Max(70f, cityRect.rect.width * 0.5f);
                float horizontalOffset = cityHalfWidth + 30f + menuNudgeRight;
                float canvasHalfWidth = canvasRect.rect.width * 0.5f;
                float canvasHalfHeight = canvasRect.rect.height * 0.5f;

                bool placeRight = cityLocalPoint.x + horizontalOffset + panelWidth <= canvasHalfWidth - safeMargin;
                panelRect.pivot = placeRight ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);

                Vector2 desiredPosition = placeRight
                    ? cityLocalPoint + new Vector2(horizontalOffset, 0f)
                    : cityLocalPoint - new Vector2(horizontalOffset, 0f);

                float minX = -canvasHalfWidth + safeMargin + panelWidth * panelRect.pivot.x;
                float maxX = canvasHalfWidth - safeMargin - panelWidth * (1f - panelRect.pivot.x);
                float minY = -canvasHalfHeight + safeMargin + panelHeight * panelRect.pivot.y;
                float maxY = canvasHalfHeight - safeMargin - panelHeight * (1f - panelRect.pivot.y);

                panelRect.anchoredPosition = new Vector2(
                    Mathf.Clamp(desiredPosition.x, minX, maxX),
                    Mathf.Clamp(desiredPosition.y, minY, maxY));
            }
            else
            {
                panelRect.pivot = new Vector2(1f, 0.5f);
                panelRect.anchoredPosition = new Vector2(-30f, 0f);
            }

            Image panelBg = panelObj.AddComponent<Image>();
            if (actionCardSprite == null)
            {
                actionCardSprite = CreateRoundedCardSprite();
            }

            panelBg.sprite = actionCardSprite;
            panelBg.type = Image.Type.Sliced;
            panelBg.color = new Color(0.07f, 0.12f, 0.2f, 0.96f);

            if (linkedCity.hasTakenTurn)
            {
                CreateMenuTitle(panelObj.transform, linkedCity.cityName + " already moved");
                CreateMenuButton(panelObj.transform, new Vector2(0f, 24f), "This city already moved this turn", () => { }, false);
                CreateMenuButton(panelObj.transform, new Vector2(0f, -34f), "Press Next Turn to use again", () => { }, false);
                return;
            }

            CreateMenuTitle(panelObj.transform, linkedCity.cityName + " Commands");

            bool canBuildNewCity = linkedCity.isCapital;
            string buildCityLabel = canBuildNewCity
                ? "1. Build new city"
                : "1. Build new city (Main city only)";
            CreateMenuButton(panelObj.transform, new Vector2(0f, 104f), buildCityLabel, () => OnActionClicked("Build new city"), canBuildNewCity);
            CreateMenuButton(panelObj.transform, new Vector2(0f, 56f), "2. Upgrading", () => OnActionClicked("Upgrading"));
            CreateMenuButton(panelObj.transform, new Vector2(0f, 8f), "3. Building Power", () => OnActionClicked("Building Power"));
            bool constructionFull = linkedCity.constructionProgress >= CityConstruction.PointsRequired;
            string constructionLabel = constructionFull
                ? "4. Finish building"
                : "4. Constructing";
            string constructionAction = constructionFull ? "Finish building" : "Constructing";
            CreateMenuButton(panelObj.transform, new Vector2(0f, -40f), constructionLabel, () => OnActionClicked(constructionAction));
            CreateMenuButton(panelObj.transform, new Vector2(0f, -88f), "5. Check Buildings", () => OnActionClicked("Check Buildings"));
            CreateMenuButton(panelObj.transform, new Vector2(0f, -136f), "6. Check Fort", () => OnActionClicked("Check Fort"));
        }

        private void CreateMenuTitle(Transform parent, string title)
        {
            GameObject titleObj = new GameObject("ActionMenuTitle");
            titleObj.transform.SetParent(parent, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 168f);
            titleRect.sizeDelta = new Vector2(330f, 42f);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 26f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.97f, 0.91f, 0.66f, 1f);
        }

        private void CreateMenuButton(Transform parent, Vector2 position, string label, UnityEngine.Events.UnityAction action, bool interactable = true)
        {
            GameObject buttonObj = new GameObject("MenuButton_" + label.Replace(" ", "_"));
            buttonObj.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(330f, 42f);

            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = interactable
                ? new Color(0.16f, 0.26f, 0.44f, 0.95f)
                : new Color(0.16f, 0.2f, 0.27f, 0.62f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonBg;
            button.interactable = interactable;
            if (interactable)
            {
                button.onClick.AddListener(action);
            }

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = interactable ? Color.white : new Color(0.74f, 0.76f, 0.78f, 0.95f);

            if (interactable)
            {
                Color normalBg   = buttonBg.color;
                Color hoverBg    = new Color(0.88f, 0.74f, 0.06f, 1f);
                Color normalText = Color.white;
                Color hoverText  = new Color(0.08f, 0.05f, 0.01f, 1f);
                Vector3 hoverScale = new Vector3(1.06f, 1.18f, 1f);

                EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();

                var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener(_ =>
                {
                    buttonBg.color        = hoverBg;
                    text.color            = hoverText;
                    buttonRect.localScale = hoverScale;
                });
                trigger.triggers.Add(enterEntry);

                var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener(_ =>
                {
                    buttonBg.color        = normalBg;
                    text.color            = normalText;
                    buttonRect.localScale = Vector3.one;
                });
                trigger.triggers.Add(exitEntry);
            }
        }

        private static Sprite CreateRoundedCardSprite()
        {
            const int width = 128;
            const int height = 160;
            const int cornerRadius = 16;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool insideOuter = IsInsideRoundedRect(x, y, width, height, cornerRadius);
                    bool insideInner = IsInsideRoundedRect(x, y, width - 4, height - 4, cornerRadius - 2, 2, 2);

                    if (!insideOuter)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    if (insideInner)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 1f));
                    }
                    else
                    {
                        texture.SetPixel(x, y, new Color(0.72f, 0.84f, 1f, 1f));
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        }

        private static Sprite CreateCityNameCardSprite()
        {
            const int width = 224;
            const int height = 72;
            const int cornerRadius = 10;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool insideOuter = IsInsideRoundedRect(x, y, width, height, cornerRadius);
                    bool insideInner = IsInsideRoundedRect(x, y, width - 4, height - 4, cornerRadius - 2, 2, 2);

                    if (!insideOuter)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    texture.SetPixel(x, y, insideInner ? Color.white : new Color(0.74f, 0.81f, 0.9f, 1f));
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius, int offsetX = 0, int offsetY = 0)
        {
            int localX = x - offsetX;
            int localY = y - offsetY;

            if (localX < 0 || localY < 0 || localX >= width || localY >= height)
            {
                return false;
            }

            if (localX >= radius && localX < width - radius)
            {
                return true;
            }

            if (localY >= radius && localY < height - radius)
            {
                return true;
            }

            int cornerCenterX = localX < radius ? radius : width - radius - 1;
            int cornerCenterY = localY < radius ? radius : height - radius - 1;
            int dx = localX - cornerCenterX;
            int dy = localY - cornerCenterY;

            return (dx * dx + dy * dy) <= (radius * radius);
        }

        private static AnimatedDiceWorldContext TryCreateAnimatedD6WorldRoller(Camera sceneCamera)
        {
#if UNITY_EDITOR
            const float trayForwardOffset = 18f;
            const float diceSpawnHeight = 3.2f;
            const float dicePickupHeight = 10.5f;

            GameObject prefab = TryLoadAnimatedD6Prefab();
            if (prefab == null)
            {
                return null;
            }

            Plane worldFloor = new Plane(Vector3.up, Vector3.zero);
            Ray cityRay = sceneCamera.ViewportPointToRay(new Vector3(0.5f, 0.42f, 0f));
            float rayHitDist;
            Vector3 floorCenter;
            if (worldFloor.Raycast(cityRay, out rayHitDist))
            {
                floorCenter = cityRay.GetPoint(rayHitDist);
            }
            else
            {
                Ray fallbackRay = sceneCamera.ViewportPointToRay(new Vector3(0.5f, 0.35f, 0f));
                floorCenter = worldFloor.Raycast(fallbackRay, out rayHitDist)
                    ? fallbackRay.GetPoint(rayHitDist)
                    : new Vector3(sceneCamera.transform.position.x, 0f, sceneCamera.transform.position.z);
            }

            // Push the tray farther from the camera for a "table a few feet away" feel.
            Vector3 forwardFlat = Vector3.ProjectOnPlane(sceneCamera.transform.forward, Vector3.up).normalized;
            if (forwardFlat.sqrMagnitude < 0.001f)
            {
                forwardFlat = Vector3.forward;
            }
            floorCenter += forwardFlat * trayForwardOffset;

            GameObject rootInstance = new GameObject("AnimatedDiceD6Runtime");
            rootInstance.transform.position = floorCenter;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "DiceFloor";
            floor.transform.SetParent(rootInstance.transform, false);
            floor.transform.position = floorCenter;
            floor.transform.rotation = Quaternion.identity;
            floor.transform.localScale = new Vector3(22f, 0.35f, 22f);
            PhysicsMaterial arenaPhysicsMaterial = CreateRuntimeArenaPhysicsMaterial();
            PhysicsMaterial arenaWallPhysicsMaterial = CreateRuntimeArenaWallPhysicsMaterial();
            Renderer floorRenderer = floor.GetComponent<Renderer>();
            Material floorMaterial = CreateRuntimeDiceMaterial(new Color(0.56f, 0.42f, 0.27f, 1f));
            if (floorRenderer != null && floorMaterial != null)
            {
                floorRenderer.sharedMaterial = floorMaterial;
                // Keep the table visible in dedicated roll view.
                floorRenderer.enabled = true;
            }

            Collider floorCollider = floor.GetComponent<Collider>();
            if (floorCollider != null && arenaPhysicsMaterial != null)
            {
                floorCollider.sharedMaterial = arenaPhysicsMaterial;
            }
            DiceSurfaceTag floorTag = floor.AddComponent<DiceSurfaceTag>();
            floorTag.surfaceType = DiceSurfaceType.Floor;

            float halfExtent = 9.5f;
            float wallLength = halfExtent * 2f + 0.3f;
            float wallHalfHeight = DiceArenaWallHeight * 0.5f;
            CreateDiceArenaWall(rootInstance.transform, floorCenter + Vector3.right * halfExtent + Vector3.up * wallHalfHeight, Quaternion.identity, new Vector3(0.9f, DiceArenaWallHeight, wallLength), arenaWallPhysicsMaterial);
            CreateDiceArenaWall(rootInstance.transform, floorCenter - Vector3.right * halfExtent + Vector3.up * wallHalfHeight, Quaternion.identity, new Vector3(0.9f, DiceArenaWallHeight, wallLength), arenaWallPhysicsMaterial);
            CreateDiceArenaWall(rootInstance.transform, floorCenter + Vector3.forward * halfExtent + Vector3.up * wallHalfHeight, Quaternion.identity, new Vector3(wallLength, DiceArenaWallHeight, 0.9f), arenaWallPhysicsMaterial);
            CreateDiceArenaWall(rootInstance.transform, floorCenter - Vector3.forward * halfExtent + Vector3.up * wallHalfHeight, Quaternion.identity, new Vector3(wallLength, DiceArenaWallHeight, 0.9f), arenaWallPhysicsMaterial);

            // Add explicit corner posts so corner impacts always produce a solid collision response.
            float cornerY = floorCenter.y + wallHalfHeight;
            float cornerSize = 1.15f;
            CreateDiceArenaCorner(rootInstance.transform, floorCenter + new Vector3(halfExtent, cornerY, halfExtent), cornerSize, arenaWallPhysicsMaterial, DiceArenaWallHeight);
            CreateDiceArenaCorner(rootInstance.transform, floorCenter + new Vector3(-halfExtent, cornerY, halfExtent), cornerSize, arenaWallPhysicsMaterial, DiceArenaWallHeight);
            CreateDiceArenaCorner(rootInstance.transform, floorCenter + new Vector3(halfExtent, cornerY, -halfExtent), cornerSize, arenaWallPhysicsMaterial, DiceArenaWallHeight);
            CreateDiceArenaCorner(rootInstance.transform, floorCenter + new Vector3(-halfExtent, cornerY, -halfExtent), cornerSize, arenaWallPhysicsMaterial, DiceArenaWallHeight);

            GameObject diceInstance = Object.Instantiate(prefab, floorCenter + Vector3.up * diceSpawnHeight, Quaternion.identity);
            diceInstance.name = "AnimatedDiceD6";
            diceInstance.transform.SetParent(rootInstance.transform, true);
            diceInstance.transform.localScale *= 0.72f;
            List<Material> runtimeDiceMaterials = CreateTransparentDiceNumberMaterials(diceInstance);

            // The bought asset includes DiceHighlight for animated top-face number feedback.
            DiceHighlight highlight = diceInstance.GetComponent<DiceHighlight>();
            if (highlight != null)
            {
                highlight.enabled = true;
            }

            DiceStats diceStats = diceInstance.GetComponent<DiceStats>();
            Rigidbody rigidbody = diceInstance.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                Object.Destroy(rootInstance);
                if (floorMaterial != null)
                {
                    Object.Destroy(floorMaterial);
                }
                if (arenaPhysicsMaterial != null)
                {
                    Object.Destroy(arenaPhysicsMaterial);
                }
                if (arenaWallPhysicsMaterial != null)
                {
                    Object.Destroy(arenaWallPhysicsMaterial);
                }
                return null;
            }

            rigidbody.useGravity = true;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.maxAngularVelocity = 65f;
            rigidbody.linearDamping = 0.08f;
            rigidbody.angularDamping = 0.1f;
            rigidbody.WakeUp();

            CityIconDiceAudioClips.EnsureLoaded();
            DiceImpactAudio impactAudio = diceInstance.AddComponent<DiceImpactAudio>();
            impactAudio.Setup(CityIconDiceAudioClips.FloorClip, CityIconDiceAudioClips.WallClip);

            RollDrop rollDrop = rootInstance.AddComponent<RollDrop>();
            SetPrivateField(rollDrop, "diceGroup", new List<GameObject> { diceInstance });
            SetPrivateField(rollDrop, "pickUpHeight", floorCenter.y + dicePickupHeight);
            SetPrivateField(rollDrop, "followStrength", 32f);
            SetPrivateField(rollDrop, "maxFollowSpeed", 30f);
            SetPrivateField(rollDrop, "minThrowSpeed", 8f);
            SetPrivateField(rollDrop, "maxThrowSpeed", 30f);
            SetPrivateField(rollDrop, "throwUpBoost", 3.2f);
            SetPrivateField(rollDrop, "throwTorqueStrength", 24f);
            SetPrivateField(rollDrop, "chargeDuration", 0.9f);
            SetPrivateField(rollDrop, "holdShakePosition", 0.75f);
            SetPrivateField(rollDrop, "holdShakeTorque", 24f);
            SetPrivateField(rollDrop, "cam", sceneCamera);
            rollDrop.enabled = false;

            GameObject keyLightObj = new GameObject("AnimatedDiceKeyLight");
            keyLightObj.transform.SetParent(rootInstance.transform, false);
            keyLightObj.transform.position = floorCenter + new Vector3(2f, 5f, -2f);
            keyLightObj.transform.rotation = Quaternion.Euler(55f, -40f, 0f);
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.15f;
            keyLight.color = new Color(1f, 0.97f, 0.9f, 1f);

            GameObject fillLightObj = new GameObject("AnimatedDiceFillLight");
            fillLightObj.transform.SetParent(rootInstance.transform, false);
            fillLightObj.transform.position = floorCenter + new Vector3(-2.8f, 3.8f, 2.2f);
            fillLightObj.transform.rotation = Quaternion.Euler(42f, 140f, 0f);
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.65f;
            fillLight.color = new Color(0.85f, 0.92f, 1f, 1f);

            return new AnimatedDiceWorldContext
            {
                root = rootInstance,
                diceObject = diceInstance,
                diceStats = diceStats,
                rigidbody = rigidbody,
                impactAudio = impactAudio,
                rollDrop = rollDrop,
                floorMaterial = floorMaterial,
                arenaPhysicsMaterial = arenaPhysicsMaterial,
                arenaWallPhysicsMaterial = arenaWallPhysicsMaterial,
                runtimeDiceMaterials = runtimeDiceMaterials,
                boundsCenter = floorCenter,
                boundsHalfExtent = halfExtent,
                floorY = floorCenter.y + 0.15f
            };
#else
            return null;
#endif
        }

        private static GameObject TryLoadAnimatedD6Prefab()
        {
#if UNITY_EDITOR
            for (int i = 0; i < animatedDiceD6PrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(animatedDiceD6PrefabPaths[i]);
                if (prefab != null)
                {
                    return prefab;
                }
            }
#endif

            return null;
        }

        private static void CreateDiceArenaWall(Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, PhysicsMaterial physicsMaterial)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "DiceWall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.transform.rotation = rotation;
            wall.transform.localScale = scale;

            Collider collider = wall.GetComponent<Collider>();
            if (collider != null && physicsMaterial != null)
            {
                collider.sharedMaterial = physicsMaterial;
            }

            DiceSurfaceTag wallTag = wall.AddComponent<DiceSurfaceTag>();
            wallTag.surfaceType = DiceSurfaceType.Wall;

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static void RebuildDiceScreenBounds(AnimatedDiceWorldContext context, Camera sceneCamera)
        {
            if (context == null || context.root == null || sceneCamera == null)
            {
                return;
            }

            Transform rootTransform = context.root.transform;
            Transform existing = rootTransform.Find("DiceScreenBounds");
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }

            GameObject boundsRoot = new GameObject("DiceScreenBounds");
            boundsRoot.transform.SetParent(rootTransform, false);

            Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, context.floorY, 0f));
            const float viewportInset = 0.08f;
            if (!TryGetViewportFloorPoint(sceneCamera, floorPlane, new Vector2(viewportInset, viewportInset), out Vector3 bottomLeft)
                || !TryGetViewportFloorPoint(sceneCamera, floorPlane, new Vector2(1f - viewportInset, viewportInset), out Vector3 bottomRight)
                || !TryGetViewportFloorPoint(sceneCamera, floorPlane, new Vector2(viewportInset, 1f - viewportInset), out Vector3 topLeft)
                || !TryGetViewportFloorPoint(sceneCamera, floorPlane, new Vector2(1f - viewportInset, 1f - viewportInset), out Vector3 topRight))
            {
                Object.Destroy(boundsRoot);
                return;
            }

            PhysicsMaterial wallMaterial = context.arenaWallPhysicsMaterial != null
                ? context.arenaWallPhysicsMaterial
                : context.arenaPhysicsMaterial;

            const float wallHeight = DiceArenaWallHeight;
            const float wallThickness = 0.95f;
            const float cornerSize = 1.25f;

            CreateDiceScreenEdge(boundsRoot.transform, "ScreenWallLeft", bottomLeft, topLeft, wallThickness, wallHeight, wallMaterial);
            CreateDiceScreenEdge(boundsRoot.transform, "ScreenWallRight", bottomRight, topRight, wallThickness, wallHeight, wallMaterial);
            CreateDiceScreenEdge(boundsRoot.transform, "ScreenWallBottom", bottomLeft, bottomRight, wallThickness, wallHeight, wallMaterial);
            CreateDiceScreenEdge(boundsRoot.transform, "ScreenWallTop", topLeft, topRight, wallThickness, wallHeight, wallMaterial);

            CreateDiceArenaCorner(boundsRoot.transform, bottomLeft + Vector3.up * (wallHeight * 0.5f), cornerSize, wallMaterial, wallHeight);
            CreateDiceArenaCorner(boundsRoot.transform, bottomRight + Vector3.up * (wallHeight * 0.5f), cornerSize, wallMaterial, wallHeight);
            CreateDiceArenaCorner(boundsRoot.transform, topLeft + Vector3.up * (wallHeight * 0.5f), cornerSize, wallMaterial, wallHeight);
            CreateDiceArenaCorner(boundsRoot.transform, topRight + Vector3.up * (wallHeight * 0.5f), cornerSize, wallMaterial, wallHeight);
        }

        private static bool TryGetViewportFloorPoint(Camera sceneCamera, Plane floorPlane, Vector2 viewportPos, out Vector3 point)
        {
            Ray ray = sceneCamera.ViewportPointToRay(new Vector3(viewportPos.x, viewportPos.y, 0f));
            if (floorPlane.Raycast(ray, out float hitDist))
            {
                point = ray.GetPoint(hitDist);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private static void CreateDiceScreenEdge(Transform parent, string wallName, Vector3 start, Vector3 end, float thickness, float height, PhysicsMaterial physicsMaterial)
        {
            Vector3 flatStart = new Vector3(start.x, 0f, start.z);
            Vector3 flatEnd = new Vector3(end.x, 0f, end.z);
            Vector3 flatDirection = flatEnd - flatStart;
            float length = flatDirection.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            Vector3 center = (start + end) * 0.5f + Vector3.up * (height * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(parent, false);
            wall.transform.position = center;
            wall.transform.rotation = rotation;
            wall.transform.localScale = new Vector3(thickness, height, length + thickness);

            Collider collider = wall.GetComponent<Collider>();
            if (collider != null && physicsMaterial != null)
            {
                collider.sharedMaterial = physicsMaterial;
            }

            DiceSurfaceTag wallTag = wall.AddComponent<DiceSurfaceTag>();
            wallTag.surfaceType = DiceSurfaceType.Wall;

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static void CreateDiceArenaCorner(Transform parent, Vector3 position, float size, PhysicsMaterial physicsMaterial, float height = 4f)
        {
            GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corner.name = "DiceCorner";
            corner.transform.SetParent(parent, false);
            corner.transform.position = position;
            corner.transform.rotation = Quaternion.identity;
            corner.transform.localScale = new Vector3(size, height, size);

            Collider collider = corner.GetComponent<Collider>();
            if (collider != null && physicsMaterial != null)
            {
                collider.sharedMaterial = physicsMaterial;
            }

            DiceSurfaceTag cornerTag = corner.AddComponent<DiceSurfaceTag>();
            cornerTag.surfaceType = DiceSurfaceType.Wall;

            Renderer renderer = corner.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static PhysicsMaterial CreateRuntimeArenaPhysicsMaterial()
        {
            PhysicsMaterial material = new PhysicsMaterial("DiceArenaRuntime");
            material.dynamicFriction = 0.72f;
            material.staticFriction = 0.78f;
            material.bounciness = 0.04f;
            material.frictionCombine = PhysicsMaterialCombine.Maximum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            return material;
        }

        private static PhysicsMaterial CreateRuntimeArenaWallPhysicsMaterial()
        {
            PhysicsMaterial material = new PhysicsMaterial("DiceArenaWallRuntime");
            material.dynamicFriction = 0.42f;
            material.staticFriction = 0.5f;
            material.bounciness = 0.22f;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            material.bounceCombine = PhysicsMaterialCombine.Maximum;
            return material;
        }

        private static Material CreateRuntimeDiceMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static void SetDiceRenderersVisible(Renderer[] renderers, bool isVisible)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = isVisible;
            }
        }

        private static List<Material> CreateTransparentDiceNumberMaterials(GameObject diceInstance)
        {
            List<Material> runtimeMaterials = new List<Material>();
            if (diceInstance == null)
            {
                return runtimeMaterials;
            }

            Renderer[] renderers = diceInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                Material source = renderer.sharedMaterial;
                if (!source.name.Contains("Numbers"))
                {
                    continue;
                }

                Material mat = new Material(source);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                renderer.sharedMaterial = mat;
                runtimeMaterials.Add(mat);
            }

            return runtimeMaterials;
        }

        public static void CloseActionMenu()
        {
            if (activeActionMenu == null)
            {
                return;
            }

            Destroy(activeActionMenu);
            activeActionMenu = null;
            activeMenuOwner = null;
        }

        /// <summary>
        /// Updates this city's turn status visuals (green/ready, red/finished).
        /// </summary>
        public void SetTurnCompleted(bool completed)
        {
            if (linkedCity != null)
            {
                linkedCity.hasTakenTurn = completed;
            }

            if (cityCanvasGroup != null)
            {
                cityCanvasGroup.alpha = completed ? 0.34f : 1f;
            }

            if (backgroundCircle != null)
            {
                backgroundCircle.color = completed
                    ? new Color(0.02f, 0.02f, 0.02f, 0.98f)
                    : defaultBackgroundColor;
            }

            if (cityIcon != null)
            {
                cityIcon.color = completed
                    ? new Color(0.5f, 0.5f, 0.5f, 1f)
                    : defaultCityIconColor;
            }

            if (cityNameText != null)
            {
                cityNameText.color = completed
                    ? new Color(0.78f, 0.78f, 0.78f, 0.95f)
                    : defaultCityNameColor;
            }

            if (cityNameBackground != null)
            {
                cityNameBackground.color = defaultCityNameBackgroundColor;
            }

            if (turnStatusDot != null)
            {
                turnStatusDot.color = completed
                    ? new Color(0.9f, 0.22f, 0.22f, 0.95f)
                    : new Color(0.2f, 0.85f, 0.28f, 0.95f);
            }
        }

        public City LinkedCity => linkedCity;

        public void HideStatBadgeNumbers()
        {
            if (populationText != null) populationText.text = "";
            if (moneyText != null) moneyText.text = "";
            if (powerText != null) powerText.text = "";
        }

        public void HideAllStatBadges()
        {
            if (populationPlateImage != null) populationPlateImage.gameObject.SetActive(false);
            if (populationText != null) populationText.gameObject.SetActive(false);

            if (moneyPlateImage != null) moneyPlateImage.gameObject.SetActive(false);
            if (moneyText != null) moneyText.gameObject.SetActive(false);

            if (powerPlateImage != null) powerPlateImage.gameObject.SetActive(false);
            if (powerText != null) powerText.gameObject.SetActive(false);
        }

        public void RevealHealthBadgeNumber()
        {
            if (populationPlateImage != null) populationPlateImage.gameObject.SetActive(true);
            if (populationText != null) populationText.gameObject.SetActive(true);

            if (populationText != null && linkedCity != null)
            {
                populationText.text = linkedCity.healthPoints.ToString();
            }
        }

        public void RevealMoneyBadgeNumber()
        {
            if (moneyPlateImage != null) moneyPlateImage.gameObject.SetActive(true);
            if (moneyText != null) moneyText.gameObject.SetActive(true);

            if (moneyText != null && linkedCity != null)
            {
                moneyText.text = linkedCity.money.ToString();
            }
        }

        public void RevealPowerBadgeNumber()
        {
            if (powerPlateImage != null) powerPlateImage.gameObject.SetActive(true);
            if (powerText != null) powerText.gameObject.SetActive(true);

            if (powerText != null && linkedCity != null)
            {
                powerText.text = linkedCity.cityPower.ToString();
            }
        }

        public void RevealAllBadgeNumbers()
        {
            RevealHealthBadgeNumber();
            RevealMoneyBadgeNumber();
            RevealPowerBadgeNumber();
        }

        public bool TryGetHealthBadgeWorldPosition(out Vector3 worldPosition)
        {
            if (populationText != null)
            {
                worldPosition = populationText.rectTransform.position;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        public bool TryGetMoneyBadgeWorldPosition(out Vector3 worldPosition)
        {
            if (moneyText != null)
            {
                worldPosition = moneyText.rectTransform.position;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        public bool TryGetPowerBadgeWorldPosition(out Vector3 worldPosition)
        {
            if (powerText != null)
            {
                worldPosition = powerText.rectTransform.position;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }
        
    }
}
