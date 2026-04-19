using System.Collections.Generic;
using GlobalDomination.GameData;
using GlobalDomination.Managers;
using GlobalDomination.UI;
using GlobalDomination.UI.BuildingIcons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalDomination.UI.Hud
{
    /// <summary>
    /// Fort division chips (number + unit count) on the screen’s right edge. Tap for roster and redeploy.
    /// </summary>
    public sealed partial class PlayerDivisionsStripUI
    {
        private const float DivisionDetailModalWidth = 420f;

        private struct DivisionRef
        {
            public City City;
            public int DivisionNumber;
            public int UnitCount;
        }

        private static GameObject activeDetailDialog;

        private GameObject stripRoot;
        private RectTransform stripContainerRt;
        private RectTransform stripChipsColumnRt;

        public void DestroyStrip()
        {
            if (stripRoot != null)
            {
                Object.Destroy(stripRoot);
                stripRoot = null;
                stripContainerRt = null;
                stripChipsColumnRt = null;
            }

            DestroyActiveDetailDialog();
        }

        public void CloseDivisionDetailIfOpen()
        {
            DestroyActiveDetailDialog();
        }

        /// <summary>
        /// Fullscreen overlays (Fort panel, etc.) call <see cref="Transform.SetAsLastSibling"/> after the strip
        /// was built; bring the strip forward so division chips stay visible on the right edge.
        /// </summary>
        public void BringToFront(Canvas canvas)
        {
            if (stripRoot == null || canvas == null)
            {
                return;
            }

            Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            if (stripRoot.transform.parent != root.transform)
            {
                stripRoot.transform.SetParent(root.transform, false);
            }

            stripRoot.transform.SetAsLastSibling();
        }

        private static void DestroyActiveDetailDialog()
        {
            if (activeDetailDialog != null)
            {
                Object.Destroy(activeDetailDialog);
                activeDetailDialog = null;
            }
        }

        /// <summary>Runtime UI objects must be created with <c>typeof(RectTransform)</c>; parenting under a plain <see cref="Transform"/> does not upgrade children.</summary>
        private static RectTransform UiRect(GameObject go)
        {
            return go.GetComponent<RectTransform>();
        }

        public void Refresh(Player player, Canvas canvas, CurrentTurnHeaderSettings settings)
        {
            if (canvas == null)
            {
                return;
            }

            Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            Transform parent = root.transform;
            EnsureStripBuilt(parent, settings);

            if (stripRoot == null)
            {
                return;
            }

            // First build may have parented under a different Canvas (e.g. FindFirstObjectByType order).
            // RepositionStrip uses `canvas` local space; stay parented to that canvas or layout goes off-screen.
            if (stripRoot.transform.parent != parent)
            {
                stripRoot.transform.SetParent(parent, false);
            }

            if (player == null)
            {
                GameManager gm = GameManager.Instance;
                if (gm != null && gm.players != null && gm.players.Count > 0)
                {
                    player = gm.GetCurrentPlayer();
                }
            }

            if (player == null || player.ownedCities == null)
            {
                stripRoot.SetActive(false);
                return;
            }

            List<DivisionRef> divisions = CollectPlayerDivisions(player);
            stripRoot.SetActive(divisions.Count > 0);
            if (divisions.Count == 0)
            {
                return;
            }

            RepositionStrip(root, settings);
            stripRoot.transform.SetAsLastSibling();

            Transform content = stripChipsColumnRt;
            if (content == null)
            {
                return;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(content.GetChild(i).gameObject);
            }

            float labelSize = Mathf.Clamp(settings.countryFontSize * 0.95f, 13f, 18f);
            for (int i = 0; i < divisions.Count; i++)
            {
                DivisionRef dr = divisions[i];
                CreateDivisionChip(content, root, dr, labelSize);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(stripChipsColumnRt);
        }

        /// <summary>
        /// Use after <see cref="Refresh"/> in the same frame as <c>Destroy</c> on other UI so layout settles next frame.
        /// </summary>
        public void ForceRebuildStripLayout()
        {
            if (stripContainerRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(stripContainerRt);
            }

            if (stripChipsColumnRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(stripChipsColumnRt);
            }
        }

        private static List<DivisionRef> CollectPlayerDivisions(Player player)
        {
            List<DivisionRef> list = new List<DivisionRef>();
            HashSet<long> seen = new HashSet<long>();

            if (player == null)
            {
                GameManager fallbackGm = GameManager.Instance;
                if (fallbackGm != null)
                {
                    player = fallbackGm.GetCurrentPlayer();
                }
            }

            if (player?.ownedCities == null)
            {
                return list;
            }

            for (int c = 0; c < player.ownedCities.Count; c++)
            {
                City city = player.ownedCities[c];
                if (city?.fortUnits == null || city.fortUnits.Count == 0)
                {
                    continue;
                }

                HashSet<int> divIds = new HashSet<int>();
                for (int i = 0; i < city.fortUnits.Count; i++)
                {
                    FortUnitEntry e = city.fortUnits[i];
                    if (e != null && e.divisionNumber > 0)
                    {
                        divIds.Add(e.divisionNumber);
                    }
                }

                foreach (int d in divIds)
                {
                    long key = (c + 1L) << 32 | (uint)d;
                    if (seen.Add(key))
                    {
                        list.Add(new DivisionRef
                        {
                            City = city,
                            DivisionNumber = d,
                            UnitCount = CountUnitsInFortDivision(city, d),
                        });
                    }
                }
            }

            list.Sort((a, b) =>
            {
                int cityCmp = string.CompareOrdinal(a.City.cityName, b.City.cityName);
                if (cityCmp != 0)
                {
                    return cityCmp;
                }

                return a.DivisionNumber.CompareTo(b.DivisionNumber);
            });

            return list;
        }

        private static int CountUnitsInFortDivision(City city, int divisionNumber)
        {
            if (city?.fortUnits == null || divisionNumber <= 0)
            {
                return 0;
            }

            int n = 0;
            for (int i = 0; i < city.fortUnits.Count; i++)
            {
                FortUnitEntry e = city.fortUnits[i];
                if (e != null && e.divisionNumber == divisionNumber)
                {
                    n++;
                }
            }

            return n;
        }

        private void EnsureStripBuilt(Transform parent, CurrentTurnHeaderSettings settings)
        {
            if (stripRoot != null)
            {
                return;
            }

            stripRoot = new GameObject("PlayerDivisionsStrip", typeof(RectTransform));
            stripRoot.transform.SetParent(parent, false);
            stripRoot.transform.SetAsLastSibling();

            RectTransform stripRt = UiRect(stripRoot);
            stripRt.anchorMin = new Vector2(1f, 0.5f);
            stripRt.anchorMax = new Vector2(1f, 0.5f);
            stripRt.pivot = new Vector2(1f, 0.5f);
            stripRt.sizeDelta = new Vector2(88f, 460f);

            Image stripBg = stripRoot.AddComponent<Image>();
            stripBg.color = Color.clear;
            stripBg.raycastTarget = false;

            GameObject content = new GameObject("DivisionChipsColumn", typeof(RectTransform));
            content.transform.SetParent(stripRoot.transform, false);
            RectTransform contentRt = UiRect(content);
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;

            LayoutElement contentWidthLe = content.AddComponent<LayoutElement>();
            contentWidthLe.minWidth = 82f;
            contentWidthLe.preferredWidth = 82f;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            stripChipsColumnRt = contentRt;

            stripContainerRt = stripRt;
            stripRoot.SetActive(false);
        }

        private void RepositionStrip(Canvas canvas, CurrentTurnHeaderSettings settings)
        {
            if (stripContainerRt == null || canvas == null)
            {
                return;
            }

            const float maxStripHeight = 460f;
            const float stripWidth = 88f;
            const float gapFromFlag = 10f;
            float insetFromRight = settings.hudRightMargin + settings.hudFlagWidth + gapFromFlag;

            stripContainerRt.anchorMin = new Vector2(1f, 0.5f);
            stripContainerRt.anchorMax = new Vector2(1f, 0.5f);
            stripContainerRt.pivot = new Vector2(1f, 0.5f);
            stripContainerRt.sizeDelta = new Vector2(stripWidth, maxStripHeight);
            stripContainerRt.anchoredPosition = new Vector2(-insetFromRight, 24f);
        }

        private void CreateDivisionChip(Transform parent, Canvas canvas, DivisionRef dr, float fontSize)
        {
            GameObject chip = new GameObject($"DivChip_{dr.City.cityName}_{dr.DivisionNumber}", typeof(RectTransform));
            chip.transform.SetParent(parent, false);

            LayoutElement le = chip.AddComponent<LayoutElement>();
            le.minHeight = 92f;
            le.preferredHeight = 96f;
            le.minWidth = 72f;
            le.preferredWidth = 78f;

            Color chipAccent = DivisionChipAccent(dr.DivisionNumber);
            Color centerTextColor = DivisionChipCenterTextColor(chipAccent);

            GameObject ovalBack = new GameObject("OvalPlate", typeof(RectTransform));
            ovalBack.transform.SetParent(chip.transform, false);
            RectTransform ovalRt = UiRect(ovalBack);
            ovalRt.anchorMin = Vector2.zero;
            ovalRt.anchorMax = Vector2.one;
            ovalRt.offsetMin = new Vector2(2f, 2f);
            ovalRt.offsetMax = new Vector2(-2f, -2f);
            Image ovalImg = ovalBack.AddComponent<Image>();
            ovalImg.sprite = CityIconUI.GetSharedPopulationPlateSprite();
            ovalImg.type = Image.Type.Simple;
            ovalImg.color = chipAccent;

            Outline ovalRing = ovalBack.AddComponent<Outline>();
            ovalRing.effectColor = new Color(0.04f, 0.06f, 0.1f, 0.55f);
            ovalRing.effectDistance = new Vector2(1.2f, -1.2f);
            ovalRing.useGraphicAlpha = true;

            Button btn = chip.AddComponent<Button>();
            btn.targetGraphic = ovalImg;
            ColorBlock colors = btn.colors;
            colors.highlightedColor = Color.Lerp(chipAccent, Color.white, 0.22f);
            colors.pressedColor = Color.Lerp(chipAccent, Color.black, 0.12f);
            btn.colors = colors;

            GameObject divNumObj = new GameObject("DivisionNumber", typeof(RectTransform));
            divNumObj.transform.SetParent(chip.transform, false);
            RectTransform divNumRt = UiRect(divNumObj);
            divNumRt.anchorMin = Vector2.zero;
            divNumRt.anchorMax = Vector2.one;
            divNumRt.offsetMin = new Vector2(2f, 2f);
            divNumRt.offsetMax = new Vector2(-2f, -2f);
            TextMeshProUGUI divTmp = divNumObj.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(divTmp);
            divTmp.text = dr.DivisionNumber.ToString();
            divTmp.fontSize = Mathf.Max(fontSize + 12f, 28f);
            divTmp.fontStyle = FontStyles.Bold;
            divTmp.alignment = TextAlignmentOptions.Center;
            divTmp.color = centerTextColor;
            divTmp.textWrappingMode = TextWrappingModes.NoWrap;
            divTmp.overflowMode = TextOverflowModes.Overflow;
            divTmp.raycastTarget = false;

            GameObject badgeRoot = new GameObject("UnitCountBadge", typeof(RectTransform));
            badgeRoot.transform.SetParent(chip.transform, false);
            RectTransform badgeRt = UiRect(badgeRoot);
            badgeRt.anchorMin = new Vector2(1f, 1f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = new Vector2(-2f, -2f);
            badgeRt.sizeDelta = new Vector2(36f, 28f);

            GameObject badgeOval = new GameObject("BadgeOval", typeof(RectTransform));
            badgeOval.transform.SetParent(badgeRoot.transform, false);
            RectTransform boRt = UiRect(badgeOval);
            boRt.anchorMin = Vector2.zero;
            boRt.anchorMax = Vector2.one;
            boRt.offsetMin = Vector2.zero;
            boRt.offsetMax = Vector2.zero;
            Image boImg = badgeOval.AddComponent<Image>();
            boImg.sprite = CityIconUI.GetSharedPopulationPlateSprite();
            boImg.type = Image.Type.Simple;
            boImg.color = new Color(0.12f, 0.18f, 0.32f, 0.98f);
            Outline badgeRing = badgeOval.AddComponent<Outline>();
            badgeRing.effectColor = new Color(1f, 1f, 1f, 0.18f);
            badgeRing.effectDistance = new Vector2(0.6f, -0.6f);

            GameObject countObj = new GameObject("Count", typeof(RectTransform));
            countObj.transform.SetParent(badgeRoot.transform, false);
            RectTransform countRt = UiRect(countObj);
            countRt.anchorMin = Vector2.zero;
            countRt.anchorMax = Vector2.one;
            countRt.offsetMin = Vector2.zero;
            countRt.offsetMax = Vector2.zero;
            TextMeshProUGUI countTmp = countObj.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(countTmp);
            countTmp.text = dr.UnitCount > 0 ? dr.UnitCount.ToString() : "0";
            countTmp.fontSize = 15f;
            countTmp.fontStyle = FontStyles.Bold;
            countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.color = Color.white;
            countTmp.textWrappingMode = TextWrappingModes.NoWrap;
            countTmp.overflowMode = TextOverflowModes.Overflow;
            countTmp.raycastTarget = false;

            DivisionRef captured = dr;
            btn.onClick.AddListener(() => ShowDivisionDetailDialog(canvas, captured));
        }

        /// <summary>
        /// Same panel as tapping a chip on the right HUD strip, e.g. after Fort assign flow.
        /// </summary>
        public void ShowDivisionDetailForCity(Canvas canvas, Player player, City city, int divisionNumber)
        {
            if (canvas == null || player == null || city == null || divisionNumber <= 0)
            {
                return;
            }

            DivisionRef dr = new DivisionRef
            {
                City = city,
                DivisionNumber = divisionNumber,
                UnitCount = CountUnitsInFortDivision(city, divisionNumber),
            };
            ShowDivisionDetailDialog(canvas, dr);
        }

        private static Color DivisionChipAccent(int divisionNumber)
        {
            float h = (divisionNumber * 0.21f) % 1f;
            return Color.HSVToRGB(h, 0.48f, 0.93f);
        }

        private static Color DivisionChipCenterTextColor(Color accent)
        {
            Color.RGBToHSV(accent, out float h, out float s, out float v);
            return v > 0.62f ? new Color(0.07f, 0.09f, 0.14f, 1f) : new Color(0.96f, 0.97f, 1f, 1f);
        }

        private void ShowDivisionDetailDialog(Canvas canvas, DivisionRef dr)
        {
            if (canvas == null || dr.City == null)
            {
                return;
            }

            DestroyActiveDetailDialog();

            GameObject overlay = new GameObject("DivisionDetailOverlay", typeof(RectTransform));
            overlay.transform.SetParent(canvas.transform, false);
            overlay.transform.SetAsLastSibling();
            activeDetailDialog = overlay;

            RectTransform overlayRt = UiRect(overlay);
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            GameObject dimGo = new GameObject("DimDismiss", typeof(RectTransform));
            dimGo.transform.SetParent(overlay.transform, false);
            RectTransform dimRt = UiRect(dimGo);
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            Image dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.05f, 0.1f, 0.4f);
            dimImg.raycastTarget = true;
            Button dimBtn = dimGo.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            ColorBlock dcb = dimBtn.colors;
            dcb.highlightedColor = dimImg.color;
            dcb.pressedColor = dimImg.color;
            dimBtn.colors = dcb;
            dimBtn.onClick.AddListener(DestroyActiveDetailDialog);

            float modalWidth = DivisionDetailModalWidth;
            GameObject sheet = new GameObject("DivisionDetailSheet", typeof(RectTransform));
            sheet.transform.SetParent(overlay.transform, false);
            sheet.transform.SetAsLastSibling();
            RectTransform sheetRt = UiRect(sheet);
            sheetRt.anchorMin = new Vector2(0.5f, 0.5f);
            sheetRt.anchorMax = new Vector2(0.5f, 0.5f);
            sheetRt.pivot = new Vector2(0.5f, 0.5f);
            sheetRt.anchoredPosition = Vector2.zero;
            sheetRt.sizeDelta = new Vector2(modalWidth, 0f);

            Image sheetBg = sheet.AddComponent<Image>();
            sheetBg.sprite = CityIconUI.GetSharedActionCardSprite();
            sheetBg.type = Image.Type.Sliced;
            sheetBg.color = new Color(0.08f, 0.12f, 0.2f, 0.99f);
            sheetBg.raycastTarget = true;

            Outline sheetOutline = sheet.AddComponent<Outline>();
            sheetOutline.effectColor = new Color(0.45f, 0.65f, 0.9f, 0.2f);
            sheetOutline.effectDistance = new Vector2(1.5f, -1.5f);
            sheetOutline.useGraphicAlpha = true;

            VerticalLayoutGroup sheetLayout = sheet.AddComponent<VerticalLayoutGroup>();
            sheetLayout.padding = new RectOffset(18, 18, 18, 16);
            sheetLayout.spacing = 12f;
            sheetLayout.childAlignment = TextAnchor.UpperCenter;
            sheetLayout.childControlHeight = true;
            sheetLayout.childControlWidth = true;
            sheetLayout.childForceExpandWidth = true;
            sheetLayout.childForceExpandHeight = false;

            ContentSizeFitter sheetCsf = sheet.AddComponent<ContentSizeFitter>();
            sheetCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sheetCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement sheetLe = sheet.AddComponent<LayoutElement>();
            sheetLe.minWidth = modalWidth;
            sheetLe.preferredWidth = modalWidth;

            Color accent = DivisionChipAccent(dr.DivisionNumber);

            GameObject headerRow = new GameObject("HeaderRow", typeof(RectTransform));
            headerRow.transform.SetParent(sheet.transform, false);
            LayoutElement headerRowLe = headerRow.AddComponent<LayoutElement>();
            headerRowLe.minHeight = 48f;
            HorizontalLayoutGroup headerH = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerH.spacing = 12f;
            headerH.childAlignment = TextAnchor.MiddleLeft;
            headerH.childControlHeight = true;
            headerH.childControlWidth = false;
            headerH.childForceExpandHeight = true;
            headerH.childForceExpandWidth = false;

            GameObject swatch = new GameObject("DivisionSwatch", typeof(RectTransform));
            swatch.transform.SetParent(headerRow.transform, false);
            LayoutElement swLe = swatch.AddComponent<LayoutElement>();
            swLe.preferredWidth = 36f;
            swLe.preferredHeight = 36f;
            swLe.minWidth = 36f;
            swLe.minHeight = 36f;
            Image swImg = swatch.AddComponent<Image>();
            swImg.sprite = CityIconUI.GetSharedPopulationPlateSprite();
            swImg.color = accent;
            Outline swOut = swatch.AddComponent<Outline>();
            swOut.effectColor = new Color(0f, 0f, 0f, 0.4f);
            swOut.effectDistance = new Vector2(1f, -1f);

            GameObject titleCol = new GameObject("TitleCol", typeof(RectTransform));
            titleCol.transform.SetParent(headerRow.transform, false);
            LayoutElement titleColLe = titleCol.AddComponent<LayoutElement>();
            titleColLe.flexibleWidth = 1f;
            titleColLe.minWidth = 120f;
            TextMeshProUGUI titleTmp = titleCol.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(titleTmp);
            titleTmp.text = $"<size=22><b>Division {dr.DivisionNumber}</b></size>";
            titleTmp.fontSize = 16f;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.color = new Color(1f, 0.94f, 0.62f, 1f);
            titleTmp.richText = true;

            GameObject scrollRoot = new GameObject("Scroll", typeof(RectTransform));
            scrollRoot.transform.SetParent(sheet.transform, false);
            LayoutElement scrollAreaLe = scrollRoot.AddComponent<LayoutElement>();
            scrollAreaLe.flexibleHeight = 0f;
            scrollAreaLe.minHeight = 180f;
            scrollAreaLe.preferredHeight = 320f;
            RectTransform scrollRootRt = UiRect(scrollRoot);
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

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollRoot.transform, false);
            RectTransform viewportRt = UiRect(viewport);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(4f, 4f);
            viewportRt.offsetMax = new Vector2(-4f, -4f);
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0.02f, 0.04f, 0.08f, 0.25f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewportRt;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = UiRect(content);
            contentRt.anchorMin = new Vector2(0.5f, 1f);
            contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(modalWidth - 48f, 0f);

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(76f, 94f);
            grid.spacing = new Vector2(12f, 12f);
            grid.padding = new RectOffset(4, 4, 4, 8);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            int gridCols = Mathf.Clamp(Mathf.FloorToInt((modalWidth - 52f) / (76f + 12f)), 1, 6);
            grid.constraintCount = gridCols;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;

            GameObject statsRow = new GameObject("SelectionStats", typeof(RectTransform));
            statsRow.transform.SetParent(sheet.transform, false);
            statsRow.SetActive(false);
            LayoutElement statsRowLe = statsRow.AddComponent<LayoutElement>();
            statsRowLe.minHeight = 4f;
            ContentSizeFitter statsRowCsf = statsRow.AddComponent<ContentSizeFitter>();
            statsRowCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            statsRowCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI statsReadout = statsRow.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(statsReadout);
            statsReadout.text = string.Empty;
            statsReadout.fontSize = 13.5f;
            statsReadout.alignment = TextAlignmentOptions.Center;
            statsReadout.color = new Color(0.88f, 0.91f, 0.96f, 1f);
            statsReadout.richText = true;

            List<FortUnitEntry> divUnits = new List<FortUnitEntry>();
            List<FortUnitEntry> roster = dr.City.fortUnits;
            if (roster != null)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    FortUnitEntry e = roster[i];
                    if (e != null && e.divisionNumber == dr.DivisionNumber)
                    {
                        divUnits.Add(e);
                    }
                }
            }

            if (divUnits.Count == 0)
            {
                GameObject empty = new GameObject("Empty", typeof(RectTransform));
                empty.transform.SetParent(content.transform, false);
                LayoutElement emptyLe = empty.AddComponent<LayoutElement>();
                emptyLe.minHeight = 40f;
                emptyLe.minWidth = modalWidth - 56f;
                TextMeshProUGUI emptyTmp = empty.AddComponent<TextMeshProUGUI>();
                TmpFontResolve.AssignIfNeeded(emptyTmp);
                emptyTmp.text = "(No units in this division)";
                emptyTmp.fontSize = 14f;
                emptyTmp.alignment = TextAlignmentOptions.Center;
                emptyTmp.color = new Color(0.7f, 0.72f, 0.76f, 1f);
            }
            else
            {
                Color ringBase = new Color(0.45f, 0.65f, 0.9f, 0.22f);
                Color ringPick = new Color(1f, 0.9f, 0.42f, 0.88f);
                Outline lastRing = null;
                FortUnitEntry lastEntry = null;

                void OnUnitTilePressed(Outline ring, FortUnitEntry entry)
                {
                    if (ReferenceEquals(lastEntry, entry) && statsRow.activeSelf)
                    {
                        if (lastRing != null)
                        {
                            lastRing.effectColor = ringBase;
                        }

                        lastRing = null;
                        lastEntry = null;
                        statsRow.SetActive(false);
                        LayoutRebuilder.ForceRebuildLayoutImmediate(sheetRt);
                        return;
                    }

                    if (lastRing != null)
                    {
                        lastRing.effectColor = ringBase;
                    }

                    lastRing = ring;
                    lastEntry = entry;
                    if (ring != null)
                    {
                        ring.effectColor = ringPick;
                    }

                    statsRow.SetActive(true);
                    statsReadout.text = BuildDivisionUnitSelectionReadout(entry);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(sheetRt);
                }

                for (int i = 0; i < divUnits.Count; i++)
                {
                    CreateDivisionUnitRosterTile(content.transform, divUnits[i], ringBase, OnUnitTilePressed);
                }
            }

            GameObject attackBtnObj = new GameObject("AttackPosition", typeof(RectTransform));
            attackBtnObj.transform.SetParent(sheet.transform, false);
            LayoutElement attackBtnLe = attackBtnObj.AddComponent<LayoutElement>();
            attackBtnLe.minHeight = 42f;
            attackBtnLe.preferredHeight = 42f;
            Image attackBg = attackBtnObj.AddComponent<Image>();
            attackBg.color = new Color(0.62f, 0.22f, 0.18f, 0.96f);
            Button attackBtn = attackBtnObj.AddComponent<Button>();
            attackBtn.targetGraphic = attackBg;
            ColorBlock atkCb = attackBtn.colors;
            atkCb.highlightedColor = new Color(0.75f, 0.3f, 0.24f, 1f);
            atkCb.pressedColor = new Color(0.5f, 0.16f, 0.12f, 1f);
            atkCb.disabledColor = new Color(0.28f, 0.28f, 0.3f, 0.65f);
            attackBtn.colors = atkCb;
            attackBtn.interactable = divUnits.Count > 0;
            GameObject attackLbl = new GameObject("Label", typeof(RectTransform));
            attackLbl.transform.SetParent(attackBtnObj.transform, false);
            RectTransform alr = UiRect(attackLbl);
            alr.anchorMin = Vector2.zero;
            alr.anchorMax = Vector2.one;
            alr.offsetMin = Vector2.zero;
            alr.offsetMax = Vector2.zero;
            TextMeshProUGUI aTmp = attackLbl.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(aTmp);
            aTmp.text = divUnits.Count > 0 ? "Attack position" : "Attack position (no units)";
            aTmp.fontSize = 16f;
            aTmp.fontStyle = FontStyles.Bold;
            aTmp.alignment = TextAlignmentOptions.Center;
            aTmp.color = Color.white;

            DivisionRef attackDr = dr;
            Canvas attackCanvas = canvas;
            List<FortUnitEntry> attackUnitsSnapshot = new List<FortUnitEntry>(divUnits);
            attackBtn.onClick.AddListener(() => BeginAttackPositionFlow(attackCanvas, attackDr, attackUnitsSnapshot));

            GameObject closeBtnObj = new GameObject("Close", typeof(RectTransform));
            closeBtnObj.transform.SetParent(sheet.transform, false);
            LayoutElement closeBtnLe = closeBtnObj.AddComponent<LayoutElement>();
            closeBtnLe.minHeight = 40f;
            closeBtnLe.preferredHeight = 40f;
            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.color = new Color(0.35f, 0.38f, 0.42f, 0.95f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            GameObject closeLbl = new GameObject("Label", typeof(RectTransform));
            closeLbl.transform.SetParent(closeBtnObj.transform, false);
            RectTransform clr = UiRect(closeLbl);
            clr.anchorMin = Vector2.zero;
            clr.anchorMax = Vector2.one;
            clr.offsetMin = Vector2.zero;
            clr.offsetMax = Vector2.zero;
            TextMeshProUGUI cTmp = closeLbl.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(cTmp);
            cTmp.text = "Close";
            cTmp.fontSize = 18f;
            cTmp.fontStyle = FontStyles.Bold;
            cTmp.alignment = TextAlignmentOptions.Center;
            cTmp.color = Color.white;

            closeBtn.onClick.AddListener(DestroyActiveDetailDialog);
        }

        private static void GetFortUnitHitPoints(FortUnitEntry entry, UnitDefinition def, out int current, out int max)
        {
            max = def != null && def.HitPoints > 0 ? def.HitPoints : 1;
            if (entry.remainingHitPoints < 0)
            {
                current = max;
            }
            else
            {
                current = Mathf.Clamp(entry.remainingHitPoints, 0, max);
            }
        }

        private static string BuildDivisionUnitSelectionReadout(FortUnitEntry entry)
        {
            UnitDefinition def = UnitCatalog.GetUnitForBuilding(entry.buildingType);
            GetFortUnitHitPoints(entry, def, out int cur, out int max);
            if (def == null)
            {
                return $"<b>{entry.buildingType}</b>\n<color=#9ec5e8>HP</color> <b>{cur}</b> / <b>{max}</b>\n<size=12>Training Lv.{entry.buildingLevel}</size>";
            }

            string hpName = UnitStatLabels.GetHpCategoryDisplayName(def.HpCategory);
            string powerSym = def.GetPowerStatSymbol();
            string auxPart = def.Auxiliary > 0
                ? $"\n<color=#9ec5e8>Aux</color> <b>{def.Auxiliary}</b>"
                : string.Empty;

            return
                $"<b>{def.UnitName}</b>  <size=12><color=#9fb3c8>Lv.{entry.buildingLevel}</color></size>\n" +
                $"<color=#9ec5e8>{hpName}</color>  <b>{cur}</b> / <b>{max}</b>\n" +
                $"<color=#9ec5e8>{powerSym}</color> <b>{def.CategoryPower}</b>  " +
                $"<color=#5a6a78>·</color>  <color=#9ec5e8>Str</color> <b>{def.IconStrength}</b>" +
                auxPart;
        }

        private static void CreateDivisionUnitRosterTile(
            Transform parent,
            FortUnitEntry entry,
            Color ringBase,
            System.Action<Outline, FortUnitEntry> onPressed)
        {
            UnitDefinition def = UnitCatalog.GetUnitForBuilding(entry.buildingType);
            GetFortUnitHitPoints(entry, def, out int curHp, out int maxHp);
            float hpFrac = maxHp > 0 ? (float)curHp / maxHp : 1f;

            GameObject tile = new GameObject($"UnitTile_{entry.buildingType}", typeof(RectTransform));
            tile.transform.SetParent(parent, false);

            LayoutElement tileLe = tile.AddComponent<LayoutElement>();
            tileLe.minWidth = 76f;
            tileLe.preferredWidth = 76f;
            tileLe.minHeight = 94f;
            tileLe.preferredHeight = 94f;

            Image tileBg = tile.AddComponent<Image>();
            tileBg.color = new Color(0.06f, 0.09f, 0.14f, 0.72f);
            Button btn = tile.AddComponent<Button>();
            btn.targetGraphic = tileBg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.12f, 0.16f, 0.24f, 0.9f);
            cb.pressedColor = new Color(0.1f, 0.14f, 0.22f, 0.95f);
            btn.colors = cb;

            VerticalLayoutGroup vlg = tile.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 6, 5);
            vlg.spacing = 5f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            GameObject iconPlate = new GameObject("IconPlate", typeof(RectTransform));
            iconPlate.transform.SetParent(tile.transform, false);
            LayoutElement ipLe = iconPlate.AddComponent<LayoutElement>();
            ipLe.minWidth = 54f;
            ipLe.preferredWidth = 54f;
            ipLe.minHeight = 54f;
            ipLe.preferredHeight = 54f;
            ipLe.flexibleWidth = 0f;

            Image plateImg = iconPlate.AddComponent<Image>();
            plateImg.sprite = CityIconUI.GetSharedPopulationPlateSprite();
            plateImg.color = new Color(0.05f, 0.09f, 0.15f, 1f);

            Outline plateRing = iconPlate.AddComponent<Outline>();
            plateRing.effectColor = ringBase;
            plateRing.effectDistance = new Vector2(1.1f, -1.1f);
            plateRing.useGraphicAlpha = true;

            GameObject iconInner = new GameObject("Icon", typeof(RectTransform));
            iconInner.transform.SetParent(iconPlate.transform, false);
            RectTransform iconRt = UiRect(iconInner);
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconInner.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.sprite = BuildingIconProvider.GetIcon(entry.buildingType);
            iconImg.color = Color.white;

            GameObject barTrack = new GameObject("HpTrack", typeof(RectTransform));
            barTrack.transform.SetParent(tile.transform, false);
            LayoutElement barLe = barTrack.AddComponent<LayoutElement>();
            barLe.minHeight = 6f;
            barLe.preferredHeight = 6f;
            barLe.minWidth = 58f;
            barLe.preferredWidth = 58f;
            barLe.flexibleWidth = 0f;
            Image trackImg = barTrack.AddComponent<Image>();
            trackImg.sprite = CityIconUI.GetSharedPopulationPlateSprite();
            trackImg.type = Image.Type.Simple;
            trackImg.color = new Color(0.07f, 0.08f, 0.1f, 0.98f);

            GameObject barFill = new GameObject("HpFill", typeof(RectTransform));
            barFill.transform.SetParent(barTrack.transform, false);
            RectTransform fillRt = UiRect(barFill);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(hpFrac), 1f);
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.sprite = CityIconUI.GetSharedPopulationPlateSprite();
            fillImg.type = Image.Type.Simple;
            fillImg.color = hpFrac >= 0.999f
                ? new Color(0.25f, 0.82f, 0.48f, 1f)
                : Color.Lerp(new Color(0.95f, 0.2f, 0.22f), new Color(0.95f, 0.8f, 0.25f), hpFrac);

            btn.onClick.AddListener(() => onPressed(plateRing, entry));
        }
    }
}
