using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GlobalDomination.GameData;
using GlobalDomination.UI.BuildingIcons;

namespace GlobalDomination.UI
{
    public partial class CityIconUI
    {
        private static GameObject activeBuildingsListPanel;
        private static GameObject activeFortPanel;
        private static GameObject activeBuildingUnitShopPanel;

        private void ShowFortStatusPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || linkedCity == null)
            {
                return;
            }

            if (activeFortPanel != null)
            {
                Object.Destroy(activeFortPanel);
                activeFortPanel = null;
            }

            GameObject overlay = new GameObject("FortStatusOverlay");
            overlay.transform.SetParent(canvas.transform, false);
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
            panelRt.sizeDelta = new Vector2(440f, 320f);

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
            titleRt.anchoredPosition = new Vector2(0f, -18f);
            titleRt.sizeDelta = new Vector2(400f, 40f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"{linkedCity.cityName} — Fort";
            titleTmp.fontSize = 24f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.93f, 0.55f, 1f);

            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(panel.transform, false);
            RectTransform bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.sizeDelta = new Vector2(380f, 160f);
            bodyRt.anchoredPosition = new Vector2(0f, 8f);
            TextMeshProUGUI bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
            IList<string> units = linkedCity.unitsInFort;
            if (units == null || units.Count == 0)
            {
                bodyTmp.text = "No units stationed in the fort yet.";
            }
            else
            {
                bodyTmp.text = string.Join("\n", units);
            }

            bodyTmp.fontSize = 18f;
            bodyTmp.alignment = TextAlignmentOptions.Center;
            bodyTmp.color = new Color(0.82f, 0.86f, 0.92f, 1f);

            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(panel.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 16f);
            closeRt.sizeDelta = new Vector2(180f, 40f);
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
            closeBtn.onClick.AddListener(() =>
            {
                if (activeFortPanel != null)
                {
                    Object.Destroy(activeFortPanel);
                    activeFortPanel = null;
                }
            });
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
                if (linkedCity.unitsInFort == null)
                {
                    linkedCity.unitsInFort = new List<string>();
                }

                linkedCity.unitsInFort.Add($"{capturedBuilding.displayName} (Lv.{capturedBuilding.level})");
                RefreshMoneyDisplayFromLinkedCity();

                if (activeBuildingUnitShopPanel != null)
                {
                    Object.Destroy(activeBuildingUnitShopPanel);
                    activeBuildingUnitShopPanel = null;
                }

                ShowBuildingUnitShopPanel(capturedBuilding);
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
