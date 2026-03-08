using System.Collections.Generic;
using UnityEngine;

namespace GlobalDomination.GameData
{
    /// <summary>
    /// Manages building generation through dice rolls.
    /// First roll (1-6) determines the category, second roll (1-6) determines the specific building.
    /// </summary>
    public static class BuildingRollTable
    {
        // 6x6 building table - first dimension is first dice roll, second is second dice roll
        private static readonly BuildingType[,] rollTable = new BuildingType[6, 6]
        {
            // First roll = 1
            { BuildingType.Barack, BuildingType.Machinery, BuildingType.None, BuildingType.MoneyBuilder, BuildingType.Farm, BuildingType.Workshop },
            
            // First roll = 2
            { BuildingType.MutantLaboratory, BuildingType.None, BuildingType.TrainingGround, BuildingType.Hospital, BuildingType.Bank, BuildingType.Factory },
            
            // First roll = 3
            { BuildingType.Laboratory, BuildingType.Mine, BuildingType.Port, BuildingType.None, BuildingType.Airport, BuildingType.PowerPlant },
            
            // First roll = 4
            { BuildingType.University, BuildingType.ResearchCenter, BuildingType.Arsenal, BuildingType.Fortress, BuildingType.None, BuildingType.TradingPost },
            
            // First roll = 5
            { BuildingType.SpyNetwork, BuildingType.CommandCenter, BuildingType.RadioStation, BuildingType.Bunker, BuildingType.MissileBase, BuildingType.None },
            
            // First roll = 6
            { BuildingType.NuclearReactor, BuildingType.None, BuildingType.SpaceCenter, BuildingType.CyberCenter, BuildingType.BioWeaponLab, BuildingType.CloneFactory }
        };

        /// <summary>
        /// Rolls for a random building using two dice rolls.
        /// </summary>
        /// <returns>A new Building instance, or null if the roll results in None.</returns>
        public static Building RollForBuilding()
        {
            int firstRoll = DiceRoller.RollD6();
            int secondRoll = DiceRoller.RollD6();
            
            return GetBuildingFromRoll(firstRoll, secondRoll);
        }

        /// <summary>
        /// Gets a building from specific dice values (useful for testing or manual selection).
        /// </summary>
        /// <param name="firstRoll">First dice roll (1-6)</param>
        /// <param name="secondRoll">Second dice roll (1-6)</param>
        /// <returns>A new Building instance, or null if the roll results in None.</returns>
        public static Building GetBuildingFromRoll(int firstRoll, int secondRoll)
        {
            // Convert from 1-indexed dice rolls to 0-indexed array
            int categoryIndex = firstRoll - 1;
            int buildingIndex = secondRoll - 1;

            if (categoryIndex < 0 || categoryIndex >= 6 || buildingIndex < 0 || buildingIndex >= 6)
            {
                Debug.LogWarning($"Invalid dice roll values: {firstRoll}, {secondRoll}");
                return null;
            }

            BuildingType type = rollTable[categoryIndex, buildingIndex];
            
            if (type == BuildingType.None)
            {
                Debug.Log($"Rolled [{firstRoll}, {secondRoll}] - No building");
                return null;
            }

            Debug.Log($"Rolled [{firstRoll}, {secondRoll}] - {type}");
            return new Building(type);
        }

        /// <summary>
        /// Rolls for the first building a city gets.
        /// </summary>
        /// <returns>A Building instance. If None is rolled, automatically re-rolls until a valid building is obtained.</returns>
        public static Building RollForFirstBuilding()
        {
            Building building = null;
            int attempts = 0;
            const int maxAttempts = 100; // Safety limit

            while (building == null && attempts < maxAttempts)
            {
                building = RollForBuilding();
                attempts++;
            }

            if (building == null)
            {
                Debug.LogWarning("Failed to roll a valid building after max attempts. Defaulting to Barack.");
                building = new Building(BuildingType.Barack);
            }

            return building;
        }
    }
}
