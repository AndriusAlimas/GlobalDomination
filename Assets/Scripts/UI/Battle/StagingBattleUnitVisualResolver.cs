using GlobalDomination.GameData;
using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Resolves optional <see cref="Resources"/> prefabs for staging battle units. Falls back to null (capsule primitive) when unset.
    /// </summary>
    public static class StagingBattleUnitVisualResolver
    {
        // Optional override prefab. When missing, StagingBattleWorld creates a capsule root and attaches the England soldier at runtime.
        private const string DefaultAttackerResourcePath = "Battle/StagingBattleAttacker";
        private const string DefaultDefenderResourcePath = "Battle/StagingBattleDefender";
        private const string PerTypeFolder = "Battle/Attackers/";

        private static GameObject s_cachedDefaultAttacker;
        private static GameObject s_cachedDefaultDefender;

        /// <summary>Per-<see cref="BuildingType"/> prefab, then default attacker, then null (use capsule).</summary>
        public static GameObject GetAttackerPrefab(BuildingType buildingType)
        {
            if (buildingType != BuildingType.None)
            {
                GameObject specific = Resources.Load<GameObject>(PerTypeFolder + buildingType);
                if (specific != null)
                {
                    return specific;
                }
            }

            if (s_cachedDefaultAttacker == null)
            {
                s_cachedDefaultAttacker = Resources.Load<GameObject>(DefaultAttackerResourcePath);
            }

            return s_cachedDefaultAttacker;
        }

        public static GameObject GetDefenderPrefab()
        {
            if (s_cachedDefaultDefender == null)
            {
                s_cachedDefaultDefender = Resources.Load<GameObject>(DefaultDefenderResourcePath);
            }

            if (s_cachedDefaultDefender != null)
            {
                return s_cachedDefaultDefender;
            }

            return GetAttackerPrefab(BuildingType.None);
        }
    }
}
