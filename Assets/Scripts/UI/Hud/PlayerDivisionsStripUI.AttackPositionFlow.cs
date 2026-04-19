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
    /// Multi-step UI: choose an enemy player, then stage division units on a 4×6 grid (tap unit, tap cell).
    /// Placement is presentation-only until combat rules consume <see cref="AttackStagingSummary"/>.
    /// </summary>
    public sealed partial class PlayerDivisionsStripUI
    {
        private const float AttackFlowModalWidth = 460f;
        private const int AttackGridColumns = 4;
        private const int AttackGridRows = 6;
        private const int AttackGridCellCount = AttackGridColumns * AttackGridRows;
        private const int AttackGridCellSize = 52;
        private const int AttackGridSpacing = 5;

        private sealed class AttackFlowPayload
        {
            public Canvas Canvas;
            public City SourceCity;
            public int DivisionNumber;
            public int AttackerOwnerId;
            public List<FortUnitEntry> Units;
            public Player TargetPlayer;
        }

        /// <summary>Raised when the player confirms a non-empty staging grid (for future combat resolution).</summary>
        public static event System.Action<AttackStagingSummary> AttackStagingConfirmed;

        private void BeginAttackPositionFlow(Canvas canvas, DivisionRef dr, List<FortUnitEntry> divisionUnits)
        {
            if (canvas == null || dr.City == null || divisionUnits == null || divisionUnits.Count == 0)
            {
                return;
            }

            GameManager gm = GameManager.Instance;
            if (gm == null || gm.players == null || gm.players.Count < 2)
            {
                DestroyActiveDetailDialog();
                ShowAttackFlowMessageModal(canvas, "A multi-player game is required to attack another player.");
                return;
            }

            List<Player> enemies = new List<Player>();
            for (int i = 0; i < gm.players.Count; i++)
            {
                Player p = gm.players[i];
                if (p == null || p.HasLost())
                {
                    continue;
                }

                if (p.playerId == dr.City.ownerId)
                {
                    continue;
                }

                enemies.Add(p);
            }

            DestroyActiveDetailDialog();

            if (enemies.Count == 0)
            {
                ShowAttackFlowMessageModal(canvas, "No opposing players are available.");
                return;
            }

            AttackFlowPayload payload = new AttackFlowPayload
            {
                Canvas = canvas,
                SourceCity = dr.City,
                DivisionNumber = dr.DivisionNumber,
                AttackerOwnerId = dr.City.ownerId,
                Units = new List<FortUnitEntry>(divisionUnits),
                TargetPlayer = null,
            };
            ShowAttackEnemySelectionOverlay(payload, enemies);
        }

        private static void ShowAttackFlowMessageModal(Canvas canvas, string message)
        {
            if (canvas == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            GameObject overlay = CreateAttackFlowOverlayRoot(canvas);
            activeDetailDialog = overlay;

            GameObject sheet = CreateAttackFlowSheet(overlay.transform, AttackFlowModalWidth);
            RectTransform sheetRt = UiRect(sheet);

            VerticalLayoutGroup v = sheet.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 18, 16);
            v.spacing = 14f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            ContentSizeFitter csf = sheet.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI msg = CreateAttackTmp(sheet.transform, message, 15f, TextAlignmentOptions.Center);
            msg.color = new Color(0.88f, 0.9f, 0.94f, 1f);

            GameObject okBtn = CreateAttackPrimaryButton(sheet.transform, "OK", new Color(0.35f, 0.38f, 0.42f, 0.95f), DestroyActiveDetailDialog);
            LayoutElement okLe = okBtn.GetComponent<LayoutElement>() ?? okBtn.AddComponent<LayoutElement>();
            okLe.minHeight = 40f;
            okLe.preferredHeight = 40f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(sheetRt);
        }

        private static void ShowAttackEnemySelectionOverlay(AttackFlowPayload payload, List<Player> enemies)
        {
            if (payload?.Canvas == null || enemies == null)
            {
                return;
            }

            GameObject overlay = CreateAttackFlowOverlayRoot(payload.Canvas);
            activeDetailDialog = overlay;

            GameObject sheet = CreateAttackFlowSheet(overlay.transform, AttackFlowModalWidth);
            RectTransform sheetRt = UiRect(sheet);

            VerticalLayoutGroup sheetV = sheet.AddComponent<VerticalLayoutGroup>();
            sheetV.padding = new RectOffset(18, 18, 18, 14);
            sheetV.spacing = 10f;
            sheetV.childAlignment = TextAnchor.UpperCenter;
            sheetV.childControlHeight = true;
            sheetV.childControlWidth = true;
            sheetV.childForceExpandWidth = true;
            sheetV.childForceExpandHeight = false;

            ContentSizeFitter sheetCsf = sheet.AddComponent<ContentSizeFitter>();
            sheetCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sheetCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = CreateAttackTmp(sheet.transform, "<b>Choose enemy</b>", 20f, TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.94f, 0.62f, 1f);
            title.richText = true;

            string sub = $"Division {payload.DivisionNumber}";
            TextMeshProUGUI subTmp = CreateAttackTmp(sheet.transform, sub, 13f, TextAlignmentOptions.Center);
            subTmp.color = new Color(0.72f, 0.78f, 0.86f, 1f);

            GameObject scrollRoot = new GameObject("EnemyScroll", typeof(RectTransform));
            scrollRoot.transform.SetParent(sheet.transform, false);
            LayoutElement scrollLe = scrollRoot.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 0f;
            scrollLe.minHeight = 160f;
            scrollLe.preferredHeight = 260f;

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
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup listV = content.AddComponent<VerticalLayoutGroup>();
            listV.padding = new RectOffset(6, 6, 6, 8);
            listV.spacing = 8f;
            listV.childAlignment = TextAnchor.UpperCenter;
            listV.childControlHeight = true;
            listV.childControlWidth = true;
            listV.childForceExpandWidth = true;
            listV.childForceExpandHeight = false;

            ContentSizeFitter listCsf = content.AddComponent<ContentSizeFitter>();
            listCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            listCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;

            for (int i = 0; i < enemies.Count; i++)
            {
                Player enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                string label = $"{enemy.playerName}  ({enemy.selectedCountry})";
                GameObject row = CreateAttackPrimaryButton(content.transform, label, new Color(0.2f, 0.4f, 0.66f, 0.96f), () =>
                {
                    payload.TargetPlayer = enemy;
                    DestroyActiveDetailDialog();
                    ShowAttackDeploymentGridOverlay(payload);
                });
                LayoutElement rowLe = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
                rowLe.minHeight = 44f;
                rowLe.preferredHeight = 44f;
            }

            GameObject cancelBtn = CreateAttackPrimaryButton(sheet.transform, "Cancel", new Color(0.35f, 0.38f, 0.42f, 0.95f), DestroyActiveDetailDialog);
            LayoutElement cLe = cancelBtn.GetComponent<LayoutElement>() ?? cancelBtn.AddComponent<LayoutElement>();
            cLe.minHeight = 40f;
            cLe.preferredHeight = 40f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(sheetRt);
        }

        private static void ShowAttackDeploymentGridOverlay(AttackFlowPayload payload)
        {
            if (payload?.Canvas == null || payload.SourceCity == null || payload.TargetPlayer == null || payload.Units == null)
            {
                return;
            }

            GameObject overlay = CreateAttackFlowOverlayRoot(payload.Canvas);
            activeDetailDialog = overlay;

            GameObject sheet = CreateAttackFlowSheet(overlay.transform, AttackFlowModalWidth);
            RectTransform sheetRt = UiRect(sheet);

            VerticalLayoutGroup sheetV = sheet.AddComponent<VerticalLayoutGroup>();
            sheetV.padding = new RectOffset(16, 16, 16, 14);
            sheetV.spacing = 10f;
            sheetV.childAlignment = TextAnchor.UpperCenter;
            sheetV.childControlHeight = true;
            sheetV.childControlWidth = true;
            sheetV.childForceExpandWidth = true;
            sheetV.childForceExpandHeight = false;

            ContentSizeFitter sheetCsf = sheet.AddComponent<ContentSizeFitter>();
            sheetCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sheetCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = CreateAttackTmp(sheet.transform, "<b>Deploy formation</b>", 19f, TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.94f, 0.62f, 1f);
            title.richText = true;

            string sub = $"vs <b>{payload.TargetPlayer.playerName}</b>  ·  Division {payload.DivisionNumber}";
            TextMeshProUGUI subTmp = CreateAttackTmp(sheet.transform, sub, 12.5f, TextAlignmentOptions.Center);
            subTmp.color = new Color(0.75f, 0.8f, 0.88f, 1f);
            subTmp.richText = true;

            TextMeshProUGUI hint = CreateAttackTmp(
                sheet.transform,
                "Tap a unit below, then an empty cell to place it. Tap a placed unit on the grid to return it.",
                12f,
                TextAlignmentOptions.Center);
            hint.color = new Color(0.62f, 0.68f, 0.76f, 1f);

            FortUnitEntry[] gridSlots = new FortUnitEntry[AttackGridCellCount];
            FortUnitEntry selectedUnit = null;
            Outline selectedOutline = null;
            Color poolRingBase = new Color(0.45f, 0.65f, 0.9f, 0.35f);
            Color poolRingPick = new Color(1f, 0.9f, 0.42f, 0.9f);

            Dictionary<FortUnitEntry, GameObject> poolTileByEntry = new Dictionary<FortUnitEntry, GameObject>();

            GameObject poolScroll = new GameObject("PoolScroll", typeof(RectTransform));
            poolScroll.transform.SetParent(sheet.transform, false);
            LayoutElement poolScrollLe = poolScroll.AddComponent<LayoutElement>();
            poolScrollLe.minHeight = 108f;
            poolScrollLe.preferredHeight = 108f;

            Image poolScrollBg = poolScroll.AddComponent<Image>();
            poolScrollBg.color = new Color(0.05f, 0.08f, 0.14f, 0.65f);
            ScrollRect poolSr = poolScroll.AddComponent<ScrollRect>();
            poolSr.horizontal = true;
            poolSr.vertical = false;
            poolSr.movementType = ScrollRect.MovementType.Clamped;
            poolSr.scrollSensitivity = 24f;

            GameObject poolViewport = new GameObject("PoolViewport", typeof(RectTransform));
            poolViewport.transform.SetParent(poolScroll.transform, false);
            RectTransform poolVpRt = UiRect(poolViewport);
            poolVpRt.anchorMin = Vector2.zero;
            poolVpRt.anchorMax = Vector2.one;
            poolVpRt.offsetMin = new Vector2(4f, 4f);
            poolVpRt.offsetMax = new Vector2(-4f, -4f);
            Image poolVpImg = poolViewport.AddComponent<Image>();
            poolVpImg.color = new Color(0.02f, 0.04f, 0.08f, 0.2f);
            poolSr.viewport = poolVpRt;

            GameObject poolContent = new GameObject("PoolContent", typeof(RectTransform));
            poolContent.transform.SetParent(poolViewport.transform, false);
            RectTransform poolContentRt = UiRect(poolContent);
            poolContentRt.anchorMin = new Vector2(0f, 0f);
            poolContentRt.anchorMax = new Vector2(0f, 1f);
            poolContentRt.pivot = new Vector2(0f, 0.5f);
            poolContentRt.anchoredPosition = Vector2.zero;
            poolContentRt.sizeDelta = new Vector2(0f, 0f);

            HorizontalLayoutGroup poolH = poolContent.AddComponent<HorizontalLayoutGroup>();
            poolH.padding = new RectOffset(6, 6, 4, 4);
            poolH.spacing = 8f;
            poolH.childAlignment = TextAnchor.MiddleLeft;
            poolH.childControlHeight = true;
            poolH.childControlWidth = false;
            poolH.childForceExpandHeight = true;
            poolH.childForceExpandWidth = false;

            ContentSizeFitter poolCsf = poolContent.AddComponent<ContentSizeFitter>();
            poolCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            poolCsf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            poolSr.content = poolContentRt;

            void ClearPoolSelection()
            {
                if (selectedOutline != null)
                {
                    selectedOutline.effectColor = poolRingBase;
                }

                selectedOutline = null;
                selectedUnit = null;
            }

            for (int u = 0; u < payload.Units.Count; u++)
            {
                FortUnitEntry entry = payload.Units[u];
                if (entry == null)
                {
                    continue;
                }

                GameObject tile = CreateAttackPoolUnitTile(poolContent.transform, entry, poolRingBase, outline =>
                {
                    if (IsUnitPlacedInGrid(gridSlots, entry))
                    {
                        return;
                    }

                    if (ReferenceEquals(selectedUnit, entry))
                    {
                        ClearPoolSelection();
                        return;
                    }

                    ClearPoolSelection();
                    selectedUnit = entry;
                    selectedOutline = outline;
                    if (selectedOutline != null)
                    {
                        selectedOutline.effectColor = poolRingPick;
                    }
                });
                poolTileByEntry[entry] = tile;
            }

            int gridW = AttackGridColumns * AttackGridCellSize + (AttackGridColumns - 1) * AttackGridSpacing;
            int gridH = AttackGridRows * AttackGridCellSize + (AttackGridRows - 1) * AttackGridSpacing;

            GameObject gridHost = new GameObject("AttackGrid", typeof(RectTransform));
            gridHost.transform.SetParent(sheet.transform, false);
            LayoutElement gridHostLe = gridHost.AddComponent<LayoutElement>();
            gridHostLe.minHeight = gridH + 8f;
            gridHostLe.preferredHeight = gridH + 8f;
            gridHostLe.minWidth = gridW + 8f;
            gridHostLe.preferredWidth = gridW + 8f;

            GridLayoutGroup attackGrid = gridHost.AddComponent<GridLayoutGroup>();
            attackGrid.cellSize = new Vector2(AttackGridCellSize, AttackGridCellSize);
            attackGrid.spacing = new Vector2(AttackGridSpacing, AttackGridSpacing);
            attackGrid.padding = new RectOffset(4, 4, 4, 4);
            attackGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            attackGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            attackGrid.childAlignment = TextAnchor.MiddleCenter;
            attackGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            attackGrid.constraintCount = AttackGridColumns;

            Image[] cellFills = new Image[AttackGridCellCount];
            Image[] cellIcons = new Image[AttackGridCellCount];

            void RefreshCellVisual(int index)
            {
                FortUnitEntry slot = gridSlots[index];
                bool occupied = slot != null;
                if (cellFills[index] != null)
                {
                    cellFills[index].color = occupied
                        ? new Color(0.12f, 0.18f, 0.28f, 0.95f)
                        : new Color(0.06f, 0.09f, 0.14f, 0.75f);
                }

                if (cellIcons[index] != null)
                {
                    cellIcons[index].gameObject.SetActive(occupied);
                    if (occupied)
                    {
                        cellIcons[index].sprite = BuildingIconProvider.GetIcon(slot.buildingType);
                        cellIcons[index].color = Color.white;
                    }
                }
            }

            for (int i = 0; i < AttackGridCellCount; i++)
            {
                int cellIndex = i;
                GameObject cell = new GameObject($"Cell_{cellIndex}", typeof(RectTransform));
                cell.transform.SetParent(gridHost.transform, false);

                Image bg = cell.AddComponent<Image>();
                bg.color = new Color(0.06f, 0.09f, 0.14f, 0.75f);
                cellFills[cellIndex] = bg;
                Button cellBtn = cell.AddComponent<Button>();
                cellBtn.targetGraphic = bg;
                ColorBlock cb = cellBtn.colors;
                cb.highlightedColor = new Color(0.14f, 0.2f, 0.3f, 0.92f);
                cb.pressedColor = new Color(0.1f, 0.14f, 0.22f, 0.98f);
                cellBtn.colors = cb;

                GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(cell.transform, false);
                RectTransform iconRt = UiRect(iconGo);
                iconRt.anchorMin = new Vector2(0.12f, 0.12f);
                iconRt.anchorMax = new Vector2(0.88f, 0.88f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                Image iconImg = iconGo.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconGo.SetActive(false);
                cellIcons[cellIndex] = iconImg;

                cellBtn.onClick.AddListener(() =>
                {
                    FortUnitEntry occupant = gridSlots[cellIndex];
                    if (occupant != null)
                    {
                        gridSlots[cellIndex] = null;
                        if (poolTileByEntry.TryGetValue(occupant, out GameObject pt) && pt != null)
                        {
                            pt.SetActive(true);
                        }

                        if (ReferenceEquals(selectedUnit, occupant))
                        {
                            ClearPoolSelection();
                        }

                        RefreshCellVisual(cellIndex);
                        return;
                    }

                    if (selectedUnit == null)
                    {
                        return;
                    }

                    if (IsUnitPlacedInGrid(gridSlots, selectedUnit))
                    {
                        ClearPoolSelection();
                        return;
                    }

                    gridSlots[cellIndex] = selectedUnit;
                    if (poolTileByEntry.TryGetValue(selectedUnit, out GameObject poolGo) && poolGo != null)
                    {
                        poolGo.SetActive(false);
                    }

                    ClearPoolSelection();
                    RefreshCellVisual(cellIndex);
                });

                RefreshCellVisual(cellIndex);
            }

            GameObject btnRow = new GameObject("BtnRow", typeof(RectTransform));
            btnRow.transform.SetParent(sheet.transform, false);
            LayoutElement btnRowLe = btnRow.AddComponent<LayoutElement>();
            btnRowLe.minHeight = 44f;
            btnRowLe.preferredHeight = 44f;
            HorizontalLayoutGroup btnH = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnH.spacing = 10f;
            btnH.childAlignment = TextAnchor.MiddleCenter;
            btnH.childControlHeight = true;
            btnH.childControlWidth = true;
            btnH.childForceExpandHeight = true;
            btnH.childForceExpandWidth = true;
            btnH.padding = new RectOffset(0, 0, 0, 0);

            GameObject backBtn = CreateAttackPrimaryButton(btnRow.transform, "Back", new Color(0.32f, 0.36f, 0.44f, 0.95f), () =>
            {
                List<Player> enemies = CollectEnemyPlayersForAttack(payload.AttackerOwnerId);
                DestroyActiveDetailDialog();
                payload.TargetPlayer = null;
                ShowAttackEnemySelectionOverlay(payload, enemies);
            });
            LayoutElement backLe = backBtn.GetComponent<LayoutElement>() ?? backBtn.AddComponent<LayoutElement>();
            backLe.flexibleWidth = 1f;

            GameObject confirmBtn = CreateAttackPrimaryButton(btnRow.transform, "Confirm staging", new Color(0.22f, 0.52f, 0.34f, 0.96f), () =>
            {
                List<int> occupiedIndices = new List<int>();
                List<FortUnitEntry> staged = new List<FortUnitEntry>();
                for (int g = 0; g < gridSlots.Length; g++)
                {
                    if (gridSlots[g] != null)
                    {
                        occupiedIndices.Add(g);
                        staged.Add(gridSlots[g]);
                    }
                }

                if (staged.Count == 0)
                {
                    return;
                }

                AttackStagingSummary summary = new AttackStagingSummary(
                    payload.AttackerOwnerId,
                    payload.TargetPlayer.playerId,
                    payload.SourceCity,
                    payload.DivisionNumber,
                    staged,
                    occupiedIndices);
                AttackStagingConfirmed?.Invoke(summary);
                Debug.Log(
                    $"[AttackStaging] {staged.Count} unit(s) staged vs {payload.TargetPlayer.playerName} " +
                    $"(target id {payload.TargetPlayer.playerId}) from division {payload.DivisionNumber}.");
                DestroyActiveDetailDialog();
            });
            LayoutElement confLe = confirmBtn.GetComponent<LayoutElement>() ?? confirmBtn.AddComponent<LayoutElement>();
            confLe.flexibleWidth = 1f;

            GameObject closeBtn = CreateAttackPrimaryButton(btnRow.transform, "Close", new Color(0.35f, 0.38f, 0.42f, 0.95f), DestroyActiveDetailDialog);
            LayoutElement xLe = closeBtn.GetComponent<LayoutElement>() ?? closeBtn.AddComponent<LayoutElement>();
            xLe.flexibleWidth = 1f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(sheetRt);
        }

        private static List<Player> CollectEnemyPlayersForAttack(int attackerOwnerId)
        {
            List<Player> enemies = new List<Player>();
            GameManager gm = GameManager.Instance;
            if (gm?.players == null)
            {
                return enemies;
            }

            for (int i = 0; i < gm.players.Count; i++)
            {
                Player p = gm.players[i];
                if (p == null || p.HasLost() || p.playerId == attackerOwnerId)
                {
                    continue;
                }

                enemies.Add(p);
            }

            return enemies;
        }

        private static bool IsUnitPlacedInGrid(FortUnitEntry[] gridSlots, FortUnitEntry entry)
        {
            if (gridSlots == null || entry == null)
            {
                return false;
            }

            for (int i = 0; i < gridSlots.Length; i++)
            {
                if (ReferenceEquals(gridSlots[i], entry))
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject CreateAttackFlowOverlayRoot(Canvas canvas)
        {
            GameObject overlay = new GameObject("AttackFlowOverlay", typeof(RectTransform));
            overlay.transform.SetParent(canvas.transform, false);
            overlay.transform.SetAsLastSibling();

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
            dimImg.color = new Color(0.02f, 0.05f, 0.1f, 0.45f);
            dimImg.raycastTarget = true;
            Button dimBtn = dimGo.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            ColorBlock dcb = dimBtn.colors;
            dcb.highlightedColor = dimImg.color;
            dcb.pressedColor = dimImg.color;
            dimBtn.colors = dcb;
            dimBtn.onClick.AddListener(DestroyActiveDetailDialog);

            return overlay;
        }

        private static GameObject CreateAttackFlowSheet(Transform overlayParent, float width)
        {
            GameObject sheet = new GameObject("AttackFlowSheet", typeof(RectTransform));
            sheet.transform.SetParent(overlayParent, false);
            sheet.transform.SetAsLastSibling();
            RectTransform sheetRt = UiRect(sheet);
            sheetRt.anchorMin = new Vector2(0.5f, 0.5f);
            sheetRt.anchorMax = new Vector2(0.5f, 0.5f);
            sheetRt.pivot = new Vector2(0.5f, 0.5f);
            sheetRt.anchoredPosition = Vector2.zero;
            sheetRt.sizeDelta = new Vector2(width, 0f);

            Image sheetBg = sheet.AddComponent<Image>();
            sheetBg.sprite = CityIconUI.GetSharedActionCardSprite();
            sheetBg.type = Image.Type.Sliced;
            sheetBg.color = new Color(0.08f, 0.12f, 0.2f, 0.99f);
            sheetBg.raycastTarget = true;

            Outline sheetOutline = sheet.AddComponent<Outline>();
            sheetOutline.effectColor = new Color(0.45f, 0.65f, 0.9f, 0.22f);
            sheetOutline.effectDistance = new Vector2(1.5f, -1.5f);
            sheetOutline.useGraphicAlpha = true;

            LayoutElement sheetLe = sheet.AddComponent<LayoutElement>();
            sheetLe.minWidth = width;
            sheetLe.preferredWidth = width;

            return sheet;
        }

        private static TextMeshProUGUI CreateAttackTmp(Transform parent, string text, float fontSize, TextAlignmentOptions align)
        {
            GameObject go = new GameObject("Tmp", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = 4f;
            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(tmp);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;

            return tmp;
        }

        private static GameObject CreateAttackPrimaryButton(Transform parent, string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject row = new GameObject($"Btn_{label}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            LayoutElement rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 40f;
            rowLe.preferredHeight = 40f;

            Image img = row.AddComponent<Image>();
            img.color = bgColor;
            Button btn = row.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            GameObject lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(row.transform, false);
            RectTransform lr = UiRect(lbl);
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(tmp);
            tmp.text = label;
            tmp.fontSize = 15f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            return row;
        }

        private static GameObject CreateAttackPoolUnitTile(
            Transform parent,
            FortUnitEntry entry,
            Color ringBase,
            System.Action<Outline> onSelected)
        {
            GameObject tile = new GameObject($"PoolTile_{entry.buildingType}", typeof(RectTransform));
            tile.transform.SetParent(parent, false);
            LayoutElement tileLe = tile.AddComponent<LayoutElement>();
            tileLe.minWidth = 72f;
            tileLe.preferredWidth = 72f;
            tileLe.minHeight = 92f;
            tileLe.preferredHeight = 92f;

            Image tileBg = tile.AddComponent<Image>();
            tileBg.color = new Color(0.06f, 0.09f, 0.14f, 0.78f);
            Button btn = tile.AddComponent<Button>();
            btn.targetGraphic = tileBg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.12f, 0.16f, 0.24f, 0.92f);
            cb.pressedColor = new Color(0.1f, 0.14f, 0.22f, 0.96f);
            btn.colors = cb;

            VerticalLayoutGroup vlg = tile.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 6, 5);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            GameObject iconPlate = new GameObject("IconPlate", typeof(RectTransform));
            iconPlate.transform.SetParent(tile.transform, false);
            LayoutElement ipLe = iconPlate.AddComponent<LayoutElement>();
            ipLe.minWidth = 52f;
            ipLe.preferredWidth = 52f;
            ipLe.minHeight = 52f;
            ipLe.preferredHeight = 52f;

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

            UnitDefinition def = UnitCatalog.GetUnitForBuilding(entry.buildingType);
            string shortName = def != null ? def.UnitName : entry.buildingType.ToString();
            if (shortName.Length > 10)
            {
                shortName = shortName.Substring(0, 9) + "…";
            }

            GameObject cap = new GameObject("Caption", typeof(RectTransform));
            cap.transform.SetParent(tile.transform, false);
            LayoutElement capLe = cap.AddComponent<LayoutElement>();
            capLe.minHeight = 16f;
            capLe.preferredHeight = 16f;
            TextMeshProUGUI capTmp = cap.AddComponent<TextMeshProUGUI>();
            TmpFontResolve.AssignIfNeeded(capTmp);
            capTmp.text = shortName;
            capTmp.fontSize = 10f;
            capTmp.alignment = TextAlignmentOptions.Center;
            capTmp.color = new Color(0.82f, 0.86f, 0.9f, 1f);
            capTmp.textWrappingMode = TextWrappingModes.Normal;

            btn.onClick.AddListener(() => onSelected(plateRing));
            return tile;
        }
    }
}
