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
                if (activeBuildingsListPanel != null)
                {
                    Object.Destroy(activeBuildingsListPanel);
                    activeBuildingsListPanel = null;
                }
            });
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

        private static void CreateBuildingRow(Transform parent, Building building)
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

            GameObject subObj = new GameObject("Sub");
            subObj.transform.SetParent(textCol.transform, false);
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = $"Type: {building.type}  ·  Level {building.level}";
            subTmp.fontSize = 16f;
            subTmp.color = new Color(0.65f, 0.72f, 0.82f, 0.95f);
            subTmp.alignment = TextAlignmentOptions.Left;
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
    }
}
