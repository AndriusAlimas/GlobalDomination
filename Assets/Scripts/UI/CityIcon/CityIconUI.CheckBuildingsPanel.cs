using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GlobalDomination.GameData;
using GlobalDomination.Managers;
using GlobalDomination.UI.BuildingIcons;

namespace GlobalDomination.UI
{
    public partial class CityIconUI
    {
        private static GameObject activeBuildingsListPanel;
        private static GameObject activeFortPanel;
        private static GameObject activeFortDivisionDialog;
        private static GameObject activeBuildingUnitShopPanel;
        /// <summary>Root canvas used for the last opened Fort UI (division strip must share this tree).</summary>
        private static Canvas s_lastFortRootCanvas;

        private const int MaxFortDivisionsPerCity = 6;

        private static void DestroyActiveFortDivisionDialog()
        {
            if (activeFortDivisionDialog != null)
            {
                Object.Destroy(activeFortDivisionDialog);
                activeFortDivisionDialog = null;
            }
        }

        private static void CloseFortStatusPanel()
        {
            DestroyActiveFortDivisionDialog();
            if (activeFortPanel != null)
            {
                Object.Destroy(activeFortPanel);
                activeFortPanel = null;
            }
        }

        /// <summary>
        /// Closes building shop, buildings list, fort UI, assign popover, and city action menu after buying a unit.
        /// </summary>
        private static void CloseModalsAfterUnitPurchase()
        {
            CloseActionMenu();
            if (activeBuildingUnitShopPanel != null)
            {
                Object.Destroy(activeBuildingUnitShopPanel);
                activeBuildingUnitShopPanel = null;
            }

            if (activeBuildingsListPanel != null)
            {
                Object.Destroy(activeBuildingsListPanel);
                activeBuildingsListPanel = null;
            }

            CloseFortStatusPanel();
        }

        private void ShowFortStatusPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || linkedCity == null)
            {
                return;
            }

            ShowFortStatusPanelForCity(canvas, linkedCity);
        }

        private static void ShowFortStatusPanelForCity(Canvas canvas, City city)
        {
            if (canvas == null || city == null)
            {
                return;
            }

            city.MigrateLegacyFortUnitLabelsIfNeeded();
            if (city.fortUnits == null)
            {
                city.fortUnits = new List<FortUnitEntry>();
            }

            if (activeFortPanel != null)
            {
                CloseFortStatusPanel();
            }

            Canvas fortRoot = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            s_lastFortRootCanvas = fortRoot;

            GameObject overlay = new GameObject("FortStatusOverlay");
            overlay.transform.SetParent(fortRoot.transform, false);
            overlay.transform.SetAsLastSibling();
            activeFortPanel = overlay;

            RectTransform overlayRt = overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.06f, 0.12f, 0.9f);
            dim.raycastTarget = true;

            GameObject panel = new GameObject("FortPanel");
            panel.transform.SetParent(overlay.transform, false);
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(540f, 480f);

            Image panelBg = panel.AddComponent<Image>();
            panelBg.sprite = actionCardSprite ?? CreateRoundedCardSprite();
            panelBg.type = Image.Type.Sliced;
            panelBg.color = new Color(0.07f, 0.11f, 0.18f, 0.98f);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -14f);
            titleRt.sizeDelta = new Vector2(500f, 36f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"{city.cityName} — Fort";
            titleTmp.fontSize = 24f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.93f, 0.55f, 1f);

            GameObject scrollRoot = new GameObject("Scroll");
            scrollRoot.transform.SetParent(panel.transform, false);
            RectTransform scrollRt = scrollRoot.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.anchoredPosition = new Vector2(0f, 8f);
            scrollRt.sizeDelta = new Vector2(500f, 340f);

            Image scrollBg = scrollRoot.AddComponent<Image>();
            scrollBg.color = new Color(0.04f, 0.07f, 0.12f, 0.65f);
            ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollRoot.transform, false);
            RectTransform viewportRt = viewport.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(6f, 6f);
            viewportRt.offsetMax = new Vector2(-6f, -6f);
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0.02f, 0.04f, 0.08f, 0.4f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewportRt;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1f);
            contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(480f, 0f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;

            List<FortUnitEntry> fullRoster = city.fortUnits;
            List<FortUnitEntry> unassignedRoster = CollectUnassignedFortUnits(fullRoster);

            if (fullRoster.Count == 0)
            {
                GameObject empty = new GameObject("EmptyFort");
                empty.transform.SetParent(content.transform, false);
                TextMeshProUGUI emptyTmp = empty.AddComponent<TextMeshProUGUI>();
                emptyTmp.text = "No units stationed in the fort yet.";
                emptyTmp.fontSize = 18f;
                emptyTmp.alignment = TextAlignmentOptions.Center;
                emptyTmp.color = new Color(0.82f, 0.86f, 0.92f, 1f);
            }
            else if (unassignedRoster.Count == 0)
            {
                GameObject empty = new GameObject("EmptyFort");
                empty.transform.SetParent(content.transform, false);
                TextMeshProUGUI emptyTmp = empty.AddComponent<TextMeshProUGUI>();
                emptyTmp.text = "No unassigned units.";
                emptyTmp.fontSize = 17f;
                emptyTmp.alignment = TextAlignmentOptions.Center;
                emptyTmp.color = new Color(0.82f, 0.86f, 0.92f, 1f);
            }
            else
            {
                Dictionary<FortStackKey, int> buckets = BuildFortStackBuckets(unassignedRoster);
                List<FortStackKey> order = new List<FortStackKey>(buckets.Keys);
                order.Sort(CompareFortStackKeys);

                for (int i = 0; i < order.Count; i++)
                {
                    FortStackKey key = order[i];
                    int count = buckets[key];
                    CreateFortUnitRow(
                        content.transform,
                        overlay.transform,
                        fortRoot,
                        city,
                        key,
                        count);
                }
            }

            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(panel.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 14f);
            closeRt.sizeDelta = new Vector2(200f, 40f);
            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.color = new Color(0.2f, 0.45f, 0.72f, 0.95f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            GameObject closeLabelObj = new GameObject("Label");
            closeLabelObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform closeLabelRt = closeLabelObj.AddComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI closeTmp = closeLabelObj.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "Close";
            closeTmp.fontSize = 20f;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;
            closeBtn.onClick.AddListener(CloseFortStatusPanel);

            UITestManager utmStrip = Object.FindFirstObjectByType<UITestManager>(FindObjectsInactive.Include);
            utmStrip?.BringDivisionStripToFront();
        }

        private static List<FortUnitEntry> CollectUnassignedFortUnits(List<FortUnitEntry> roster)
        {
            List<FortUnitEntry> list = new List<FortUnitEntry>();
            if (roster == null)
            {
                return list;
            }

            for (int i = 0; i < roster.Count; i++)
            {
                FortUnitEntry e = roster[i];
                if (e != null && e.divisionNumber == 0)
                {
                    list.Add(e);
                }
            }

            return list;
        }

        private struct FortStackKey : System.IEquatable<FortStackKey>
        {
            public BuildingType BuildingType;
            public int BuildingLevel;
            public int DivisionNumber;

            public bool Equals(FortStackKey other)
            {
                return BuildingType == other.BuildingType &&
                       BuildingLevel == other.BuildingLevel &&
                       DivisionNumber == other.DivisionNumber;
            }

            public override bool Equals(object obj)
            {
                return obj is FortStackKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = (int)BuildingType;
                    h = (h * 397) ^ BuildingLevel;
                    h = (h * 397) ^ DivisionNumber;
                    return h;
                }
            }
        }

        private static Dictionary<FortStackKey, int> BuildFortStackBuckets(List<FortUnitEntry> roster)
        {
            Dictionary<FortStackKey, int> buckets = new Dictionary<FortStackKey, int>();
            for (int i = 0; i < roster.Count; i++)
            {
                FortUnitEntry e = roster[i];
                if (e == null)
                {
                    continue;
                }

                FortStackKey key = new FortStackKey
                {
                    BuildingType = e.buildingType,
                    BuildingLevel = e.buildingLevel,
                    DivisionNumber = e.divisionNumber
                };

                if (buckets.TryGetValue(key, out int c))
                {
                    buckets[key] = c + 1;
                }
                else
                {
                    buckets[key] = 1;
                }
            }

            return buckets;
        }

        private static int CompareFortStackKeys(FortStackKey a, FortStackKey b)
        {
            int sa = a.DivisionNumber == 0 ? -1 : a.DivisionNumber;
            int sb = b.DivisionNumber == 0 ? -1 : b.DivisionNumber;
            int c = sa.CompareTo(sb);
            if (c != 0)
            {
                return c;
            }

            c = a.BuildingType.CompareTo(b.BuildingType);
            if (c != 0)
            {
                return c;
            }

            return a.BuildingLevel.CompareTo(b.BuildingLevel);
        }

        private static void CreateFortUnitRow(
            Transform listParent,
            Transform overlayRoot,
            Canvas canvas,
            City city,
            FortStackKey key,
            int count)
        {
            UnitDefinition def = UnitCatalog.GetUnitForBuilding(key.BuildingType);
            string unitTitle = def != null ? def.UnitName : key.BuildingType.ToString();

            GameObject rowObj = new GameObject("FortRow");
            rowObj.transform.SetParent(listParent, false);

            LayoutElement rowLe = rowObj.AddComponent<LayoutElement>();
            rowLe.minHeight = 76f;
            rowLe.preferredHeight = 76f;

            HorizontalLayoutGroup rowH = rowObj.AddComponent<HorizontalLayoutGroup>();
            rowH.spacing = 12f;
            rowH.padding = new RectOffset(8, 12, 6, 6);
            rowH.childAlignment = TextAnchor.MiddleLeft;
            rowH.childControlHeight = true;
            rowH.childControlWidth = false;
            rowH.childForceExpandHeight = true;
            rowH.childForceExpandWidth = false;

            GameObject iconWrap = new GameObject("IconWrap");
            iconWrap.transform.SetParent(rowObj.transform, false);
            LayoutElement iconLe = iconWrap.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 64f;
            iconLe.preferredHeight = 64f;
            iconLe.minWidth = 64f;
            iconLe.minHeight = 64f;

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(iconWrap.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.sprite = BuildingIconProvider.GetIcon(key.BuildingType);
            iconImg.color = Color.white;
            iconImg.raycastTarget = true;

            bool unassigned = key.DivisionNumber == 0;
            Button iconBtn = iconObj.AddComponent<Button>();
            iconBtn.targetGraphic = iconImg;
            ColorBlock iconColors = iconBtn.colors;
            iconColors.highlightedColor = new Color(1f, 1f, 1f, 0.55f);
            iconColors.pressedColor = new Color(0.85f, 0.9f, 1f, 0.65f);
            iconBtn.colors = iconColors;
            iconBtn.interactable = unassigned && count > 0;
            if (unassigned && count > 0)
            {
                FortStackKey capturedKey = key;
                int capturedCount = count;
                City capturedCity = city;
                iconBtn.onClick.AddListener(() =>
                {
                    OnUnassignedFortStackSelected(overlayRoot, canvas, capturedCity, capturedKey, capturedCount);
                });
            }

            GameObject textCol = new GameObject("TextCol");
            textCol.transform.SetParent(rowObj.transform, false);
            LayoutElement textLe = textCol.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f;
            textLe.minWidth = 120f;
            VerticalLayoutGroup textV = textCol.AddComponent<VerticalLayoutGroup>();
            textV.spacing = 2f;
            textV.childAlignment = TextAnchor.MiddleLeft;
            textV.childControlWidth = true;
            textV.childControlHeight = true;
            textV.childForceExpandWidth = true;
            textV.childForceExpandHeight = false;

            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(textCol.transform, false);
            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = unitTitle;
            nameTmp.fontSize = 19f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.color = new Color(0.93f, 0.95f, 0.98f, 1f);

            GameObject subObj = new GameObject("Sub");
            subObj.transform.SetParent(textCol.transform, false);
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = $"Building Lv.{key.BuildingLevel}";
            subTmp.fontSize = 14f;
            subTmp.alignment = TextAlignmentOptions.Left;
            subTmp.color = new Color(0.7f, 0.76f, 0.84f, 1f);

            GameObject rightWrap = new GameObject("RightWrap");
            rightWrap.transform.SetParent(rowObj.transform, false);
            LayoutElement rightWrapLe = rightWrap.AddComponent<LayoutElement>();
            rightWrapLe.preferredHeight = 64f;
            rightWrapLe.minHeight = 64f;
            rightWrapLe.preferredWidth = 52f;
            rightWrapLe.minWidth = 44f;

            HorizontalLayoutGroup rightH = rightWrap.AddComponent<HorizontalLayoutGroup>();
            rightH.spacing = 0f;
            rightH.padding = new RectOffset(0, 0, 0, 0);
            rightH.childAlignment = TextAnchor.MiddleCenter;
            rightH.childControlHeight = true;
            rightH.childControlWidth = false;
            rightH.childForceExpandHeight = true;
            rightH.childForceExpandWidth = false;

            GameObject countOnly = new GameObject("UnassignedCount");
            countOnly.transform.SetParent(rightWrap.transform, false);
            LayoutElement coLe = countOnly.AddComponent<LayoutElement>();
            coLe.preferredWidth = 48f;
            coLe.minWidth = 40f;
            coLe.flexibleWidth = 0f;
            TextMeshProUGUI coTmp = countOnly.AddComponent<TextMeshProUGUI>();
            coTmp.text = $"×{count}";
            coTmp.fontSize = 24f;
            coTmp.fontStyle = FontStyles.Bold;
            coTmp.alignment = TextAlignmentOptions.MidlineRight;
            coTmp.color = new Color(1f, 0.92f, 0.65f, 1f);
            coTmp.textWrappingMode = TextWrappingModes.NoWrap;
            coTmp.overflowMode = TextOverflowModes.Overflow;
            coTmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                coTmp.font = TMP_Settings.defaultFontAsset;
            }
        }

        private static void FinishFortDivisionAssignAndRefreshHud()
        {
            Canvas fortHost = s_lastFortRootCanvas;
            UITestManager utm = Object.FindFirstObjectByType<UITestManager>(FindObjectsInactive.Include);
            utm?.RefreshCurrentTurnDisplay();
            if (utm != null && fortHost != null)
            {
                utm.RefreshDivisionStripUsingCanvas(fortHost);
            }
            else
            {
                utm?.BringDivisionStripToFront();
            }

            Canvas.ForceUpdateCanvases();
            utm?.ScheduleDivisionStripRefreshDeferred(fortHost);
        }

        private static void OnUnassignedFortStackSelected(Transform overlayRoot, Canvas canvas, City city, FortStackKey unassignedStack, int stackCount)
        {
            city = ResolveFortCityForCurrentPlayer(city);
            if (city == null || city.fortUnits == null || stackCount <= 0 || canvas == null)
            {
                return;
            }

            List<int> existingDivs = CollectExistingFortDivisionNumbers(city);
            ShowFortDivisionAssignPopover(overlayRoot, city, unassignedStack, stackCount, existingDivs);
        }

        /// <summary>
        /// Compact prompt over the fort (not a canvas-level sidebar) so division ovals on the right stay visible.
        /// </summary>
        private static void ShowFortDivisionAssignPopover(
            Transform overlayRoot,
            City city,
            FortStackKey unassignedStack,
            int stackCount,
            List<int> existingDivs)
        {
            if (overlayRoot == null || city == null)
            {
                return;
            }

            if (existingDivs == null)
            {
                existingDivs = CollectExistingFortDivisionNumbers(city);
            }

            DestroyActiveFortDivisionDialog();
            City capCity = city;

            GameObject layer = new GameObject("FortDivisionAssignPopover", typeof(RectTransform));
            layer.transform.SetParent(overlayRoot, false);
            layer.transform.SetAsLastSibling();
            activeFortDivisionDialog = layer;

            RectTransform layerRt = layer.GetComponent<RectTransform>();
            layerRt.anchorMin = Vector2.zero;
            layerRt.anchorMax = Vector2.one;
            layerRt.offsetMin = Vector2.zero;
            layerRt.offsetMax = Vector2.zero;

            GameObject dimGo = new GameObject("DismissArea", typeof(RectTransform));
            dimGo.transform.SetParent(layer.transform, false);
            RectTransform dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            Image dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.05f, 0.12f, 0.45f);
            dimImg.raycastTarget = true;
            Button dimBtn = dimGo.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            ColorBlock dcb = dimBtn.colors;
            dcb.highlightedColor = dimImg.color;
            dcb.pressedColor = dimImg.color;
            dimBtn.colors = dcb;
            dimBtn.onClick.AddListener(DestroyActiveFortDivisionDialog);

            const float cardWidth = 420f;
            GameObject card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(layer.transform, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(cardWidth, 0f);

            Image cardBg = card.AddComponent<Image>();
            cardBg.sprite = actionCardSprite ?? CreateRoundedCardSprite();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.08f, 0.12f, 0.2f, 0.99f);
            cardBg.raycastTarget = true;

            Outline cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.5f, 0.7f, 0.95f, 0.28f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
            cardOutline.useGraphicAlpha = true;

            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(18, 18, 18, 16);
            cardLayout.spacing = 10f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            ContentSizeFitter cardCsf = card.AddComponent<ContentSizeFitter>();
            cardCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement cardLe = card.AddComponent<LayoutElement>();
            cardLe.preferredWidth = cardWidth;
            cardLe.minWidth = cardWidth;

            UnitDefinition def = UnitCatalog.GetUnitForBuilding(unassignedStack.BuildingType);
            string unitName = def != null ? def.UnitName : unassignedStack.BuildingType.ToString();

            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(card.transform, false);
            LayoutElement headerLe = header.AddComponent<LayoutElement>();
            headerLe.minHeight = 48f;
            TextMeshProUGUI headerTmp = header.AddComponent<TextMeshProUGUI>();
            headerTmp.text =
                "<b>Assign this stack</b>\n" +
                $"<size=15><color=#9fb3c8>{unitName} · ×{stackCount} · Lv.{unassignedStack.BuildingLevel}</color></size>";
            headerTmp.fontSize = 19f;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = new Color(1f, 0.94f, 0.62f, 1f);
            headerTmp.richText = true;
            if (TMP_Settings.defaultFontAsset != null)
            {
                headerTmp.font = TMP_Settings.defaultFontAsset;
            }

            bool atDivisionCap = existingDivs.Count >= MaxFortDivisionsPerCity;
            if (!atDivisionCap)
            {
                int nextNewId = ComputeNextFortDivisionNumber(city);
                AddFortDialogButton(card.transform, "Create new division", new Color(0.14f, 0.52f, 0.34f, 0.98f), () =>
                {
                    AssignUnassignedFortStackToDivision(capCity, unassignedStack, nextNewId, refreshDisplay: false);
                    DestroyActiveFortDivisionDialog();
                    CloseFortStatusPanel();
                    FinishFortDivisionAssignAndRefreshHud();
                });
            }

            if (existingDivs.Count > 0)
            {
                GameObject joinLabel = new GameObject("ExistingLabel", typeof(RectTransform));
                joinLabel.transform.SetParent(card.transform, false);
                LayoutElement joinLabelLe = joinLabel.AddComponent<LayoutElement>();
                joinLabelLe.minHeight = 22f;
                TextMeshProUGUI joinLabelTmp = joinLabel.AddComponent<TextMeshProUGUI>();
                joinLabelTmp.text = atDivisionCap
                    ? $"Move stack into a division (max {MaxFortDivisionsPerCity} per fort):"
                    : "Or move into an existing division";
                joinLabelTmp.fontSize = 13f;
                joinLabelTmp.fontStyle = FontStyles.Bold;
                joinLabelTmp.alignment = TextAlignmentOptions.Center;
                joinLabelTmp.color = new Color(0.72f, 0.8f, 0.9f, 1f);
                if (TMP_Settings.defaultFontAsset != null)
                {
                    joinLabelTmp.font = TMP_Settings.defaultFontAsset;
                }

                Transform joinButtonParent = card.transform;
                const int joinScrollThreshold = 5;
                if (existingDivs.Count > joinScrollThreshold)
                {
                    GameObject scrollRoot = new GameObject("JoinScroll", typeof(RectTransform));
                    scrollRoot.transform.SetParent(card.transform, false);
                    LayoutElement scrollAreaLe = scrollRoot.AddComponent<LayoutElement>();
                    scrollAreaLe.minHeight = 156f;
                    scrollAreaLe.preferredHeight = 156f;
                    scrollAreaLe.flexibleHeight = 0f;

                    RectTransform scrollRootRt = scrollRoot.GetComponent<RectTransform>();
                    scrollRootRt.anchorMin = Vector2.zero;
                    scrollRootRt.anchorMax = Vector2.one;
                    scrollRootRt.sizeDelta = Vector2.zero;

                    Image scrollBg = scrollRoot.AddComponent<Image>();
                    scrollBg.color = new Color(0.04f, 0.07f, 0.12f, 0.55f);
                    ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
                    scroll.horizontal = false;
                    scroll.vertical = true;
                    scroll.movementType = ScrollRect.MovementType.Clamped;
                    scroll.scrollSensitivity = 22f;

                    GameObject vp = new GameObject("Viewport", typeof(RectTransform));
                    vp.transform.SetParent(scrollRoot.transform, false);
                    RectTransform vpRt = vp.GetComponent<RectTransform>();
                    vpRt.anchorMin = Vector2.zero;
                    vpRt.anchorMax = Vector2.one;
                    vpRt.offsetMin = new Vector2(4f, 4f);
                    vpRt.offsetMax = new Vector2(-4f, -4f);
                    Image vpImg = vp.AddComponent<Image>();
                    vpImg.color = new Color(0.02f, 0.04f, 0.08f, 0.25f);
                    Mask mask = vp.AddComponent<Mask>();
                    mask.showMaskGraphic = false;
                    scroll.viewport = vpRt;

                    GameObject content = new GameObject("Content", typeof(RectTransform));
                    content.transform.SetParent(vp.transform, false);
                    RectTransform contentRt = content.GetComponent<RectTransform>();
                    contentRt.anchorMin = new Vector2(0.5f, 1f);
                    contentRt.anchorMax = new Vector2(0.5f, 1f);
                    contentRt.pivot = new Vector2(0.5f, 1f);
                    contentRt.sizeDelta = new Vector2(360f, 0f);

                    VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = 8f;
                    vlg.padding = new RectOffset(2, 2, 4, 4);
                    vlg.childForceExpandWidth = true;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;

                    ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    scroll.content = contentRt;
                    joinButtonParent = content.transform;
                }

                for (int i = 0; i < existingDivs.Count; i++)
                {
                    int divId = existingDivs[i];
                    AddFortDialogButton(joinButtonParent, $"Division {divId}", new Color(0.2f, 0.4f, 0.66f, 0.96f), () =>
                    {
                        AssignUnassignedFortStackToDivision(capCity, unassignedStack, divId, refreshDisplay: false);
                        DestroyActiveFortDivisionDialog();
                        CloseFortStatusPanel();
                        FinishFortDivisionAssignAndRefreshHud();
                    });
                }
            }
            else if (atDivisionCap)
            {
                GameObject capInfo = new GameObject("CapInfo", typeof(RectTransform));
                capInfo.transform.SetParent(card.transform, false);
                LayoutElement capLe = capInfo.AddComponent<LayoutElement>();
                capLe.minHeight = 40f;
                TextMeshProUGUI capTmp = capInfo.AddComponent<TextMeshProUGUI>();
                capTmp.text =
                    "This fort already has the maximum number of divisions, but none could be listed. Try closing and reopening the fort.";
                capTmp.fontSize = 13f;
                capTmp.alignment = TextAlignmentOptions.Center;
                capTmp.color = new Color(0.9f, 0.65f, 0.55f, 1f);
                if (TMP_Settings.defaultFontAsset != null)
                {
                    capTmp.font = TMP_Settings.defaultFontAsset;
                }
            }

            AddFortDialogButton(card.transform, "Cancel", new Color(0.32f, 0.36f, 0.42f, 0.94f), DestroyActiveFortDivisionDialog);

            UITestManager utmStrip = Object.FindFirstObjectByType<UITestManager>(FindObjectsInactive.Include);
            utmStrip?.BringDivisionStripToFront();
        }

        private static void AddFortDialogButton(Transform parent, string label, Color bg, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(label.Replace(" ", ""), typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 44f;
            Image img = btnObj.AddComponent<Image>();
            img.color = bg;
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject lo = new GameObject("Label", typeof(RectTransform));
            lo.transform.SetParent(btnObj.transform, false);
            RectTransform lrt = lo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = lo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            btn.onClick.AddListener(onClick);
        }

        private static List<int> CollectExistingFortDivisionNumbers(City city)
        {
            HashSet<int> ids = new HashSet<int>();
            List<FortUnitEntry> roster = city?.fortUnits;
            if (roster == null)
            {
                return new List<int>();
            }

            for (int i = 0; i < roster.Count; i++)
            {
                FortUnitEntry e = roster[i];
                if (e == null || e.divisionNumber <= 0)
                {
                    continue;
                }

                ids.Add(e.divisionNumber);
            }

            List<int> list = new List<int>(ids);
            list.Sort();
            return list;
        }

        private static int ComputeNextFortDivisionNumber(City city)
        {
            int max = 0;
            List<FortUnitEntry> roster = city.fortUnits;
            for (int i = 0; i < roster.Count; i++)
            {
                FortUnitEntry e = roster[i];
                if (e != null && e.divisionNumber > max)
                {
                    max = e.divisionNumber;
                }
            }

            return max + 1;
        }

        private static City ResolveFortCityForCurrentPlayer(City city)
        {
            if (city == null)
            {
                return null;
            }

            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                return city;
            }

            Player player = gm.GetCurrentPlayer();
            if (player?.ownedCities == null)
            {
                return city;
            }

            for (int i = 0; i < player.ownedCities.Count; i++)
            {
                if (ReferenceEquals(player.ownedCities[i], city))
                {
                    return city;
                }
            }

            for (int i = 0; i < player.ownedCities.Count; i++)
            {
                City c = player.ownedCities[i];
                if (c != null && c.cityName == city.cityName)
                {
                    return c;
                }
            }

            return city;
        }

        private static void AssignUnassignedFortStackToDivision(City city, FortStackKey stack, int divisionId, bool refreshDisplay = true)
        {
            city = ResolveFortCityForCurrentPlayer(city);
            if (city?.fortUnits == null || divisionId <= 0)
            {
                return;
            }

            List<int> existingDivisions = CollectExistingFortDivisionNumbers(city);
            bool isNewDivisionNumber = !existingDivisions.Contains(divisionId);
            if (isNewDivisionNumber && existingDivisions.Count >= MaxFortDivisionsPerCity)
            {
                Debug.LogWarning(
                    $"[Fort] Cannot assign to new division {divisionId}: fort already has the maximum of {MaxFortDivisionsPerCity} divisions. Move units into an existing division.");
                return;
            }

            List<FortUnitEntry> roster = city.fortUnits;
            int assignedCount = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                FortUnitEntry e = roster[i];
                if (e == null)
                {
                    continue;
                }

                if (e.divisionNumber == 0 &&
                    e.buildingType == stack.BuildingType &&
                    e.buildingLevel == stack.BuildingLevel)
                {
                    e.divisionNumber = divisionId;
                    assignedCount++;
                }
            }

            if (assignedCount == 0)
            {
                Debug.LogWarning(
                    "[Fort] Create division matched no unassigned units (building type / level mismatch vs fort roster). " +
                    $"Expected {stack.BuildingType} Lv.{stack.BuildingLevel}. Division HUD will stay empty until units match.");
            }

            if (refreshDisplay)
            {
                UITestManager utm = Object.FindFirstObjectByType<UITestManager>(FindObjectsInactive.Include);
                utm?.RefreshCurrentTurnDisplay();
            }
        }

        private void ShowBuildingsListPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || linkedCity == null)
            {
                return;
            }

            if (activeBuildingsListPanel != null)
            {
                Object.Destroy(activeBuildingsListPanel);
                activeBuildingsListPanel = null;
            }

            GameObject overlay = new GameObject("BuildingsListOverlay");
            overlay.transform.SetParent(canvas.transform, false);
            overlay.transform.SetAsLastSibling();
            activeBuildingsListPanel = overlay;

            RectTransform overlayRt = overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.06f, 0.12f, 0.9f);
            dim.raycastTarget = true;

            GameObject panel = new GameObject("BuildingsListPanel");
            panel.transform.SetParent(overlay.transform, false);
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(520f, 560f);

            Image panelBg = panel.AddComponent<Image>();
            panelBg.sprite = actionCardSprite ?? CreateRoundedCardSprite();
            panelBg.type = Image.Type.Sliced;
            panelBg.color = new Color(0.07f, 0.11f, 0.18f, 0.98f);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -20f);
            titleRt.sizeDelta = new Vector2(480f, 44f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"{linkedCity.cityName} — Buildings";
            titleTmp.fontSize = 26f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.93f, 0.55f, 1f);

            GameObject scrollRoot = new GameObject("Scroll");
            scrollRoot.transform.SetParent(panel.transform, false);
            RectTransform scrollRt = scrollRoot.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.anchoredPosition = new Vector2(0f, -8f);
            scrollRt.sizeDelta = new Vector2(480f, 400f);

            Image scrollBg = scrollRoot.AddComponent<Image>();
            scrollBg.color = new Color(0.04f, 0.07f, 0.12f, 0.65f);
            ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollRoot.transform, false);
            RectTransform viewportRt = viewport.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(8f, 8f);
            viewportRt.offsetMax = new Vector2(-8f, -8f);
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0.02f, 0.04f, 0.08f, 0.4f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewportRt;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1f);
            contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(440f, 0f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;

            List<Building> buildings = linkedCity.buildings;
            if (buildings == null || buildings.Count == 0)
            {
                CreateEmptyBuildingsRow(content.transform);
            }
            else
            {
                for (int i = 0; i < buildings.Count; i++)
                {
                    Building b = buildings[i];
                    if (b == null)
                    {
                        continue;
                    }

                    CreateBuildingRow(content.transform, b);
                }
            }

            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(panel.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 18f);
            closeRt.sizeDelta = new Vector2(200f, 44f);
            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.color = new Color(0.2f, 0.45f, 0.72f, 0.95f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            GameObject closeLabelObj = new GameObject("Label");
            closeLabelObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform closeLabelRt = closeLabelObj.AddComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI closeTmp = closeLabelObj.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "Close";
            closeTmp.fontSize = 22f;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;
            closeBtn.onClick.AddListener(() =>
            {
                CloseBuildingsListAndShop();
            });
        }

        private static void CloseBuildingsListAndShop()
        {
            if (activeBuildingUnitShopPanel != null)
            {
                Object.Destroy(activeBuildingUnitShopPanel);
                activeBuildingUnitShopPanel = null;
            }

            if (activeBuildingsListPanel != null)
            {
                Object.Destroy(activeBuildingsListPanel);
                activeBuildingsListPanel = null;
            }
        }

        private static void CreateEmptyBuildingsRow(Transform parent)
        {
            GameObject row = new GameObject("EmptyRow");
            row.transform.SetParent(parent, false);
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = 120f;
            le.preferredHeight = 120f;
            TextMeshProUGUI tmp = row.AddComponent<TextMeshProUGUI>();
            tmp.text = "No buildings in this city yet.";
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.75f, 0.8f, 0.88f, 0.95f);
        }

        private void CreateBuildingRow(Transform parent, Building building)
        {
            GameObject row = new GameObject($"BuildingRow_{building.type}");
            row.transform.SetParent(parent, false);

            LayoutElement rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 88f;
            rowLe.preferredHeight = 88f;

            HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.padding = new RectOffset(14, 14, 10, 10);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;
            h.childControlHeight = true;
            h.childControlWidth = true;

            Image rowBg = row.AddComponent<Image>();
            rowBg.sprite = actionCardSprite ?? CreateRoundedCardSprite();
            rowBg.type = Image.Type.Sliced;
            rowBg.color = new Color(0.12f, 0.18f, 0.28f, 0.92f);

            Button rowBtn = row.AddComponent<Button>();
            rowBtn.targetGraphic = rowBg;
            ColorBlock colors = rowBtn.colors;
            colors.highlightedColor = new Color(0.2f, 0.28f, 0.4f, 0.98f);
            colors.pressedColor = new Color(0.15f, 0.22f, 0.35f, 1f);
            rowBtn.colors = colors;
            Building captured = building;
            rowBtn.onClick.AddListener(() => ShowBuildingUnitShopPanel(captured));

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            LayoutElement iconLe = iconObj.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 72f;
            iconLe.preferredHeight = 72f;
            iconLe.minWidth = 72f;
            iconLe.minHeight = 72f;
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.sprite = BuildingIconProvider.GetIcon(building.type);
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            GameObject textCol = new GameObject("TextColumn");
            textCol.transform.SetParent(row.transform, false);
            LayoutElement textColLe = textCol.AddComponent<LayoutElement>();
            textColLe.flexibleWidth = 1f;
            textColLe.minWidth = 200f;
            VerticalLayoutGroup v = textCol.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childAlignment = TextAnchor.MiddleLeft;

            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(textCol.transform, false);
            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = FormatBuildingDisplayName(building);
            nameTmp.fontSize = 22f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.raycastTarget = false;

            GameObject subObj = new GameObject("Sub");
            subObj.transform.SetParent(textCol.transform, false);
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = $"Type: {building.type}  ·  Level {building.level}";
            subTmp.fontSize = 16f;
            subTmp.color = new Color(0.65f, 0.72f, 0.82f, 0.95f);
            subTmp.alignment = TextAlignmentOptions.Left;
            subTmp.raycastTarget = false;

            GameObject hintObj = new GameObject("TapHint");
            hintObj.transform.SetParent(row.transform, false);
            LayoutElement hintLe = hintObj.AddComponent<LayoutElement>();
            hintLe.preferredWidth = 56f;
            hintLe.minWidth = 56f;
            TextMeshProUGUI hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "›";
            hintTmp.fontSize = 28f;
            hintTmp.fontStyle = FontStyles.Bold;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.55f, 0.75f, 0.95f, 0.85f);
            hintTmp.raycastTarget = false;
        }

        private void RefreshMoneyDisplayFromLinkedCity()
        {
            if (moneyText != null && linkedCity != null)
            {
                moneyText.text = linkedCity.money.ToString();
            }
        }

        private void ShowBuildingUnitShopPanel(Building building)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || linkedCity == null || building == null)
            {
                return;
            }

            if (activeBuildingUnitShopPanel != null)
            {
                Object.Destroy(activeBuildingUnitShopPanel);
                activeBuildingUnitShopPanel = null;
            }

            UnitDefinition unit = UnitCatalog.GetUnitForBuilding(building.type);

            GameObject overlay = new GameObject("BuildingUnitShopOverlay");
            overlay.transform.SetParent(canvas.transform, false);
            overlay.transform.SetAsLastSibling();
            activeBuildingUnitShopPanel = overlay;

            RectTransform overlayRt = overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.06f, 0.12f, 0.88f);
            dim.raycastTarget = true;

            GameObject panel = new GameObject("BuildingUnitShopPanel");
            panel.transform.SetParent(overlay.transform, false);
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 540f);

            Image panelBg = panel.AddComponent<Image>();
            panelBg.sprite = actionCardSprite ?? CreateRoundedCardSprite();
            panelBg.type = Image.Type.Sliced;
            panelBg.color = new Color(0.07f, 0.11f, 0.18f, 0.98f);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -16f);
            titleRt.sizeDelta = new Vector2(440f, 36f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = FormatBuildingStructuralTitle(building.type);
            titleTmp.fontSize = 24f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.93f, 0.55f, 1f);

            GameObject subTitleObj = new GameObject("UnitSubtitle");
            subTitleObj.transform.SetParent(panel.transform, false);
            RectTransform subTitleRt = subTitleObj.AddComponent<RectTransform>();
            subTitleRt.anchorMin = new Vector2(0.5f, 1f);
            subTitleRt.anchorMax = new Vector2(0.5f, 1f);
            subTitleRt.pivot = new Vector2(0.5f, 1f);
            subTitleRt.anchoredPosition = new Vector2(0f, -52f);
            subTitleRt.sizeDelta = new Vector2(440f, 28f);
            TextMeshProUGUI subTitleTmp = subTitleObj.AddComponent<TextMeshProUGUI>();
            subTitleTmp.text = unit != null
                ? GetShopUnitFlavorName(building, unit)
                : "No recruitable unit";
            subTitleTmp.fontSize = 20f;
            subTitleTmp.fontStyle = FontStyles.Bold;
            subTitleTmp.alignment = TextAlignmentOptions.Center;
            subTitleTmp.color = unit != null
                ? new Color(0.75f, 0.92f, 0.78f, 1f)
                : new Color(0.72f, 0.72f, 0.78f, 1f);

            GameObject bodyRow = new GameObject("BodyRow");
            bodyRow.transform.SetParent(panel.transform, false);
            RectTransform bodyRowRt = bodyRow.AddComponent<RectTransform>();
            bodyRowRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRowRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRowRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRowRt.anchoredPosition = new Vector2(0f, 28f);
            bodyRowRt.sizeDelta = new Vector2(440f, 260f);

            HorizontalLayoutGroup bodyH = bodyRow.AddComponent<HorizontalLayoutGroup>();
            bodyH.spacing = 16f;
            bodyH.padding = new RectOffset(12, 12, 0, 0);
            bodyH.childAlignment = TextAnchor.UpperCenter;
            bodyH.childForceExpandHeight = true;
            bodyH.childForceExpandWidth = false;
            bodyH.childControlHeight = true;
            bodyH.childControlWidth = true;

            GameObject bigIconObj = new GameObject("LargeIcon");
            bigIconObj.transform.SetParent(bodyRow.transform, false);
            LayoutElement bigIconLe = bigIconObj.AddComponent<LayoutElement>();
            bigIconLe.preferredWidth = 128f;
            bigIconLe.preferredHeight = 128f;
            bigIconLe.minWidth = 128f;
            bigIconLe.minHeight = 128f;
            Image bigIconImg = bigIconObj.AddComponent<Image>();
            bigIconImg.preserveAspect = true;
            bigIconImg.sprite = BuildingIconProvider.GetIcon(building.type);
            bigIconImg.color = Color.white;

            GameObject statsCol = new GameObject("StatsColumn");
            statsCol.transform.SetParent(bodyRow.transform, false);
            LayoutElement statsLe = statsCol.AddComponent<LayoutElement>();
            statsLe.flexibleWidth = 1f;
            statsLe.minWidth = 220f;
            VerticalLayoutGroup statsV = statsCol.AddComponent<VerticalLayoutGroup>();
            statsV.spacing = 8f;
            statsV.childAlignment = TextAnchor.UpperLeft;
            statsV.childControlWidth = true;
            statsV.childControlHeight = true;
            statsV.childForceExpandWidth = true;
            statsV.childForceExpandHeight = false;
            if (unit != null)
            {
                BuildShopUnitStatLines(statsCol.transform, unit);
            }
            else
            {
                GameObject msgObj = new GameObject("NoUnitMessage");
                msgObj.transform.SetParent(statsCol.transform, false);
                TextMeshProUGUI msgTmp = msgObj.AddComponent<TextMeshProUGUI>();
                msgTmp.text =
                    "This building does not produce a unit in the catalog.\n(Main Base, Money Base, and some others have no shop unit.)";
                msgTmp.fontSize = 15f;
                msgTmp.alignment = TextAlignmentOptions.TopLeft;
                msgTmp.color = new Color(0.88f, 0.9f, 0.94f, 1f);
            }

            GameObject moneyObj = new GameObject("MoneyLine");
            moneyObj.transform.SetParent(panel.transform, false);
            RectTransform moneyRt = moneyObj.AddComponent<RectTransform>();
            moneyRt.anchorMin = new Vector2(0.5f, 0f);
            moneyRt.anchorMax = new Vector2(0.5f, 0f);
            moneyRt.pivot = new Vector2(0.5f, 0f);
            moneyRt.anchoredPosition = new Vector2(0f, 132f);
            moneyRt.sizeDelta = new Vector2(440f, 32f);
            TextMeshProUGUI moneyLineTmp = moneyObj.AddComponent<TextMeshProUGUI>();
            int cityMoney = linkedCity.money;
            if (unit != null)
            {
                moneyLineTmp.text = $"City money: <b>{cityMoney}</b>  ·  Unit cost: <b>{unit.CostMoney}</b>";
            }
            else
            {
                moneyLineTmp.text = $"City money: <b>{cityMoney}</b>";
            }

            moneyLineTmp.fontSize = 17f;
            moneyLineTmp.alignment = TextAlignmentOptions.Center;
            moneyLineTmp.color = new Color(0.9f, 0.88f, 0.75f, 1f);

            GameObject buyBtnObj = new GameObject("BuyButton");
            buyBtnObj.transform.SetParent(panel.transform, false);
            RectTransform buyRt = buyBtnObj.AddComponent<RectTransform>();
            buyRt.anchorMin = new Vector2(0.5f, 0f);
            buyRt.anchorMax = new Vector2(0.5f, 0f);
            buyRt.pivot = new Vector2(0.5f, 0f);
            buyRt.anchoredPosition = new Vector2(0f, 72f);
            buyRt.sizeDelta = new Vector2(280f, 48f);
            Image buyBg = buyBtnObj.AddComponent<Image>();
            buyBg.color = new Color(0.15f, 0.62f, 0.28f, 0.95f);
            Button buyBtn = buyBtnObj.AddComponent<Button>();
            buyBtn.targetGraphic = buyBg;
            ColorBlock buyColors = buyBtn.colors;
            buyColors.disabledColor = new Color(0.35f, 0.38f, 0.42f, 0.85f);
            buyBtn.colors = buyColors;

            GameObject buyLabelObj = new GameObject("Label");
            buyLabelObj.transform.SetParent(buyBtnObj.transform, false);
            RectTransform buyLabelRt = buyLabelObj.AddComponent<RectTransform>();
            buyLabelRt.anchorMin = Vector2.zero;
            buyLabelRt.anchorMax = Vector2.one;
            buyLabelRt.offsetMin = Vector2.zero;
            buyLabelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI buyLabelTmp = buyLabelObj.AddComponent<TextMeshProUGUI>();
            buyLabelTmp.fontSize = 22f;
            buyLabelTmp.fontStyle = FontStyles.Bold;
            buyLabelTmp.alignment = TextAlignmentOptions.Center;
            buyLabelTmp.color = Color.white;

            bool canAfford = unit != null && cityMoney >= unit.CostMoney;
            buyBtn.interactable = unit != null && canAfford;
            if (unit == null)
            {
                buyLabelTmp.text = "No unit to buy";
            }
            else if (!canAfford)
            {
                buyLabelTmp.text = "Not enough money";
            }
            else
            {
                buyLabelTmp.text = "Buy";
            }

            UnitDefinition capturedUnit = unit;
            Building capturedBuilding = building;
            buyBtn.onClick.AddListener(() =>
            {
                if (capturedUnit == null || linkedCity == null)
                {
                    return;
                }

                if (linkedCity.money < capturedUnit.CostMoney)
                {
                    return;
                }

                linkedCity.money -= capturedUnit.CostMoney;
                if (linkedCity.fortUnits == null)
                {
                    linkedCity.fortUnits = new List<FortUnitEntry>();
                }

                linkedCity.fortUnits.Add(new FortUnitEntry(capturedBuilding.type, capturedBuilding.level, 0));
                RefreshMoneyDisplayFromLinkedCity();
                CloseModalsAfterUnitPurchase();
                UITestManager utmAfterBuy = Object.FindFirstObjectByType<UITestManager>(FindObjectsInactive.Include);
                utmAfterBuy?.CloseDivisionDetailIfOpen();
                utmAfterBuy?.RefreshCurrentTurnDisplay();
                utmAfterBuy?.BringDivisionStripToFront();
            });

            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(panel.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 16f);
            closeRt.sizeDelta = new Vector2(200f, 44f);
            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.color = new Color(0.2f, 0.45f, 0.72f, 0.95f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            GameObject closeLabelObj = new GameObject("Label");
            closeLabelObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform closeLabelRt = closeLabelObj.AddComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI closeTmp = closeLabelObj.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "Back";
            closeTmp.fontSize = 20f;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;
            closeBtn.onClick.AddListener(() =>
            {
                if (activeBuildingUnitShopPanel != null)
                {
                    Object.Destroy(activeBuildingUnitShopPanel);
                    activeBuildingUnitShopPanel = null;
                }
            });
        }

        private const float ShopStatLabelColumnWidth = 34f;
        private const float ShopStatRowHeight = 28f;

        private static Sprite shopTechStarSprite;
        private static Sprite shopIconStrengthStarSprite;

        private static Sprite GetShopTechStarSprite()
        {
            if (shopTechStarSprite == null)
            {
                shopTechStarSprite = CreateShopTechStarSprite();
            }

            return shopTechStarSprite;
        }

        private static Sprite GetShopIconStrengthStarSprite()
        {
            if (shopIconStrengthStarSprite == null)
            {
                shopIconStrengthStarSprite = CreateShopIconStrengthStarSprite();
            }

            return shopIconStrengthStarSprite;
        }

        /// <summary>
        /// Outline star (cyan, rotated) so it reads differently from a filled gold “capital” style badge.
        /// </summary>
        private static Sprite CreateShopTechStarSprite()
        {
            const int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            Vector2 center = new Vector2(cx, cy);
            float ro = size * 0.42f;
            float ri = size * 0.15f;
            Vector2[] outer = BuildFivePointStarVertices(center, ro, ri);
            RotateVerticesInPlace(outer, center, 18f);
            float innerScale = 0.56f;
            Vector2[] inner = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                inner[i] = center + (outer[i] - center) * innerScale;
            }

            Color line = new Color(0.48f, 0.9f, 1f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    bool o = PointInPolygon(px, py, outer);
                    bool inn = PointInPolygon(px, py, inner);
                    tex.SetPixel(x, y, o && !inn ? line : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Frosted / translucent star for “icon power” column (distinct from cyan tech outline star).
        /// </summary>
        private static Sprite CreateShopIconStrengthStarSprite()
        {
            const int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            Vector2 center = new Vector2(cx, cy);
            float ro = size * 0.4f;
            float ri = size * 0.16f;
            Vector2[] outer = BuildFivePointStarVertices(center, ro, ri);
            Vector2[] coreRegion = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                coreRegion[i] = center + (outer[i] - center) * 0.78f;
            }

            Color rim = new Color(1f, 1f, 1f, 0.78f);
            Color fill = new Color(1f, 1f, 1f, 0.22f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    if (!PointInPolygon(px, py, outer))
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    bool inCore = PointInPolygon(px, py, coreRegion);
                    tex.SetPixel(x, y, inCore ? fill : rim);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Vector2[] BuildFivePointStarVertices(Vector2 center, float outerRadius, float innerRadius)
        {
            Vector2[] poly = new Vector2[10];
            for (int i = 0; i < 5; i++)
            {
                float ao = (-90f + i * 72f) * Mathf.Deg2Rad;
                float ai = (-90f + 36f + i * 72f) * Mathf.Deg2Rad;
                poly[i * 2] = center + new Vector2(Mathf.Cos(ao) * outerRadius, Mathf.Sin(ao) * outerRadius);
                poly[(i * 2) + 1] = center + new Vector2(Mathf.Cos(ai) * innerRadius, Mathf.Sin(ai) * innerRadius);
            }

            return poly;
        }

        private static void RotateVerticesInPlace(Vector2[] verts, Vector2 pivot, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            for (int i = 0; i < verts.Length; i++)
            {
                Vector2 p = verts[i] - pivot;
                verts[i] = pivot + new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
            }
        }

        private static bool PointInPolygon(float x, float y, Vector2[] poly)
        {
            bool inside = false;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static void AddShopStatRow(Transform parent, string valueText, Sprite iconLeft, string textLeft)
        {
            GameObject row = new GameObject("StatRow");
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            h.childControlHeight = true;
            h.childControlWidth = true;
            LayoutElement rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = ShopStatRowHeight;
            rowLe.minHeight = ShopStatRowHeight - 2f;

            GameObject leftCell = new GameObject("LabelColumn");
            leftCell.transform.SetParent(row.transform, false);
            LayoutElement leL = leftCell.AddComponent<LayoutElement>();
            leL.preferredWidth = ShopStatLabelColumnWidth;
            leL.minWidth = ShopStatLabelColumnWidth;
            leL.preferredHeight = ShopStatRowHeight - 4f;
            leL.minHeight = ShopStatRowHeight - 4f;

            if (iconLeft != null)
            {
                Image img = leftCell.AddComponent<Image>();
                img.sprite = iconLeft;
                img.preserveAspect = true;
                img.color = Color.white;
                img.raycastTarget = false;
            }
            else
            {
                TextMeshProUGUI tl = leftCell.AddComponent<TextMeshProUGUI>();
                tl.text = textLeft ?? "";
                tl.fontSize = 15f;
                tl.fontStyle = FontStyles.Bold;
                tl.alignment = TextAlignmentOptions.Center;
                tl.color = new Color(0.88f, 0.9f, 0.94f, 1f);
                tl.raycastTarget = false;
            }

            GameObject rightCell = new GameObject("ValueColumn");
            rightCell.transform.SetParent(row.transform, false);
            LayoutElement leR = rightCell.AddComponent<LayoutElement>();
            leR.flexibleWidth = 1f;
            TextMeshProUGUI tr = rightCell.AddComponent<TextMeshProUGUI>();
            tr.text = $"<b>{valueText}</b>";
            tr.fontSize = 15f;
            tr.alignment = TextAlignmentOptions.MidlineLeft;
            tr.color = new Color(0.88f, 0.9f, 0.94f, 1f);
            tr.richText = true;
            tr.raycastTarget = false;
        }

        private static void BuildShopUnitStatLines(Transform parent, UnitDefinition unit)
        {
            if (unit == null)
            {
                return;
            }

            AddShopStatRow(parent, unit.HitPoints.ToString(), null, "HP");

            switch (unit.HpCategory)
            {
                case UnitHpCategory.Tech:
                    AddShopStatRow(parent, unit.CategoryPower.ToString(), GetShopTechStarSprite(), null);
                    break;
                case UnitHpCategory.Aerial:
                    AddShopStatRow(parent, unit.CategoryPower.ToString(), null, "P");
                    break;
                default:
                    AddShopStatRow(parent, unit.CategoryPower.ToString(), null, "P");
                    break;
            }

            AddShopStatRow(parent, unit.IconStrength.ToString(), GetShopIconStrengthStarSprite(), null);
            AddShopStatRow(parent, unit.Auxiliary.ToString(), null, "A");
        }

        private static string FormatBuildingDisplayName(Building building)
        {
            if (building == null)
            {
                return "?";
            }

            if (!string.IsNullOrEmpty(building.displayName))
            {
                return building.displayName;
            }

            return building.type.ToString();
        }

        /// <summary>Shop header: building category (not the random flavor name).</summary>
        private static string FormatBuildingStructuralTitle(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.MainBase:
                    return "Main Base";
                case BuildingType.MoneyBase:
                    return "Money Base";
                case BuildingType.PowerBase:
                    return "Power Base";
                case BuildingType.SpecForce:
                    return "Spec Force";
                case BuildingType.LowTech:
                    return "Low Tech";
                case BuildingType.MidTech:
                    return "Mid Tech";
                case BuildingType.HighTech:
                    return "High Tech";
                case BuildingType.Barraka:
                    return "Barracks";
                case BuildingType.MutantLab:
                    return "Mutant Lab";
                case BuildingType.DroneFactory:
                    return "Drone Factory";
                case BuildingType.AirShipBase:
                    return "Air Ship Base";
                case BuildingType.SpecialWarBase:
                    return "Special War Base";
                case BuildingType.ShipBase:
                    return "Ship Base";
                case BuildingType.NuclearWeapon:
                    return "Nuclear Weapon";
                case BuildingType.None:
                    return "—";
                default:
                    return type.ToString();
            }
        }

        private static string GetShopUnitFlavorName(Building building, UnitDefinition unit)
        {
            if (building != null && !string.IsNullOrEmpty(building.displayName))
            {
                string rawType = building.type.ToString();
                if (building.displayName != rawType)
                {
                    return building.displayName;
                }
            }

            return unit != null ? unit.UnitName : "";
        }
    }
}
