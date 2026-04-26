#if UNITY_EDITOR
using UnityEditor;

namespace GlobalDomination.Editor
{
    /// <summary>
    /// When <c>Resources/Battle/StagingBattleAttacker.prefab</c> is missing but the soldier FBX exists under
    /// <c>Assets/Art/Battle/CartoonSoldier/</c>, creates the prefab automatically so staging battle does not fall back to capsules.
    /// </summary>
    [InitializeOnLoad]
    internal static class StagingBattleAttackerPrefabEnsure
    {
        static StagingBattleAttackerPrefabEnsure()
        {
            EditorApplication.delayCall += OnDelayCall;
        }

        private static void OnDelayCall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            StagingBattlePrefabMenu.EnsureAttackerPrefabFromSoldierFbxIfMissing();
        }
    }
}
#endif
