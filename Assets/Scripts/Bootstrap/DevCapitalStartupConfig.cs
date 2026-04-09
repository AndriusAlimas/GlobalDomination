using System.Collections.Generic;
using GlobalDomination.GameData;
using UnityEngine;

namespace GlobalDomination.Managers
{
    /// <summary>
    /// Inspector values used when <see cref="UITestManager"/> dev skip startup is enabled:
    /// overrides capital dice for each test player after <see cref="GameManager.InitializeTestGame"/>.
    /// </summary>
    [System.Serializable]
    public class DevCapitalStartupConfig
    {
        [Tooltip("Capital population (health points).")]
        public int population = 12;

        [Tooltip("Starting money for the capital.")]
        public int money = 8;

        [Tooltip("Starting city power.")]
        public int power = 4;

        [Tooltip("Extra buildings besides Main Base. If empty, the normal startup building dice roll runs.")]
        public List<BuildingType> extraStartupBuildings = new List<BuildingType>();

        public bool HasPresetExtraBuildings()
        {
            if (extraStartupBuildings == null || extraStartupBuildings.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < extraStartupBuildings.Count; i++)
            {
                BuildingType t = extraStartupBuildings[i];
                if (t != BuildingType.None && t != BuildingType.MainBase)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
