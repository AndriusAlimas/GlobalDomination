using System;
using System.Collections;
using GlobalDomination.GameData;
using GlobalDomination.UI.Battle;
using GlobalDomination.UI.Hud;
using UnityEngine;

namespace GlobalDomination.Managers
{
    public partial class UITestManager : MonoBehaviour
    {
        private GameObject _stagingBattleSessionRoot;
        private Coroutine _restoreHudAfterBattleCoroutine;

        private void OnEnable()
        {
            PlayerDivisionsStripUI.AttackStagingConfirmed += HandleAttackStagingConfirmed;
        }

        private void OnDisable()
        {
            PlayerDivisionsStripUI.AttackStagingConfirmed -= HandleAttackStagingConfirmed;
            if (_restoreHudAfterBattleCoroutine != null)
            {
                StopCoroutine(_restoreHudAfterBattleCoroutine);
                _restoreHudAfterBattleCoroutine = null;
            }
        }

        private void HandleAttackStagingConfirmed(AttackStagingSummary summary)
        {
            if (_stagingBattleSessionRoot != null)
            {
                Destroy(_stagingBattleSessionRoot);
                _stagingBattleSessionRoot = null;
            }

            Canvas hud = ResolveHudCanvas();
            if (hud != null)
            {
                hud.gameObject.SetActive(false);
            }

            _stagingBattleSessionRoot = new GameObject("StagingBattleSession");
            StagingBattleWorld world = _stagingBattleSessionRoot.AddComponent<StagingBattleWorld>();
            world.Initialize(summary, EndStagingBattleAndShowHud);
        }

        private void EndStagingBattleAndShowHud()
        {
            _stagingBattleSessionRoot = null;

            Canvas hud = ResolveHudCanvas();
            if (hud != null)
            {
                hud.gameObject.SetActive(true);
            }

            if (_restoreHudAfterBattleCoroutine != null)
            {
                StopCoroutine(_restoreHudAfterBattleCoroutine);
            }

            _restoreHudAfterBattleCoroutine = StartCoroutine(CoRestoreHudAfterBattle(hud));
        }

        private IEnumerator CoRestoreHudAfterBattle(Canvas hud)
        {
            // Let battle Destroy/OnDestroy and camera teardown finish before touching TMP/materials.
            yield return new WaitForEndOfFrame();
            yield return null;

            Canvas wakeRoot = ResolveHudCanvas();
            if (wakeRoot != null)
            {
                wakeRoot.gameObject.SetActive(true);
            }

            // One frame with HUD active so Canvas/layout and cameras settle before rebuilding city TMP.
            yield return null;

            Canvas canvasHint = hud != null ? hud : wakeRoot;
            if (canvasHint == null)
            {
                canvasHint = ResolveHudCanvas();
            }

            try
            {
                EnsureUIReferences();
                EnsureGameManagerReference();
                EnsureEventSystem();
                UpdateDisplay();
                ScheduleDivisionStripRefreshDeferred(canvasHint);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HudRestoreAfterBattle] {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                Canvas root = ResolveHudCanvas();
                if (root != null)
                {
                    root.gameObject.SetActive(true);
                }

                EnsureGameManagerReference();
                if (gameManager != null && gameManager.players.Count > 0 && citiesDisplayManager != null)
                {
                    Player p = gameManager.GetCurrentPlayer();
                    if (p != null && p.ownedCities != null)
                    {
                        try
                        {
                            citiesDisplayManager.DisplayCities(p.ownedCities);
                        }
                        catch (Exception e2)
                        {
                            Debug.LogError($"[HudRestoreAfterBattle] DisplayCities fallback failed: {e2.Message}");
                        }
                    }
                }

                Canvas.ForceUpdateCanvases();
                if (root != null)
                {
                    playerDivisionsStrip.ForceRebuildStripLayout();
                    playerDivisionsStrip.BringToFront(root);
                }

                _restoreHudAfterBattleCoroutine = null;
            }
        }
    }
}
