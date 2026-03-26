using System.Collections.Generic;
using UnityEngine;

namespace GlobalDomination.GameData
{
    [System.Serializable]
    public class City
    {
        public string cityName;
        public bool isCapital;
        
        // Core stats rolled at initialization
        public int healthPoints;      // Population (rolled 3 times)
        public int money;             // Money (rolled 2 times)
        public int cityPower;         // Defense (rolled 1 time)
        public List<int> startingHealthRolls; // Individual startup D6 rolls for health
        public List<int> startingMoneyRolls;  // Individual startup D6 rolls for money
        public List<int> startingPowerRolls;  // Individual startup D6 rolls for power
        public bool hasTakenTurn;     // Simple turn-done marker for UI dimming
        
        // Buildings and upgrades
        public List<Building> buildings;
        public int upgradePoints;
        
        // Units (fort)
        public List<string> unitsInFort;  // Will be expanded when unit system is implemented
        
        // Owner
        public int ownerId;  // Player ID who owns this city

        public City(string name, bool capital = false, int ownerId = -1)
        {
            this.cityName = name;
            this.isCapital = capital;
            this.ownerId = ownerId;
            
            buildings = new List<Building>();
            unitsInFort = new List<string>();
            startingHealthRolls = new List<int>();
            startingMoneyRolls = new List<int>();
            startingPowerRolls = new List<int>();
            upgradePoints = 0;
        }

        /// <summary>
        /// Initializes the city with dice rolls for starting stats.
        /// </summary>
        public void InitializeWithDiceRolls()
        {
            // Roll 3 times for Health Points (Population)
            healthPoints = RollStatWithBreakdown(3, startingHealthRolls);
            Debug.Log($"{cityName}: Health Points (Population) = {healthPoints}");
            
            // Roll 2 times for Money
            money = RollStatWithBreakdown(2, startingMoneyRolls);
            Debug.Log($"{cityName}: Money = {money}");
            
            // Roll 1 time for City Power (Defense)
            cityPower = RollStatWithBreakdown(1, startingPowerRolls);
            Debug.Log($"{cityName}: City Power (Defense) = {cityPower}");
            
            // Roll for first building
            Building firstBuilding = BuildingRollTable.RollForFirstBuilding();
            if (firstBuilding != null)
            {
                buildings.Add(firstBuilding);
                Debug.Log($"{cityName}: First building = {firstBuilding.displayName}");
            }
        }

        private static int RollStatWithBreakdown(int diceCount, List<int> breakdown)
        {
            if (breakdown == null)
            {
                return DiceRoller.Roll(diceCount);
            }

            breakdown.Clear();
            int total = 0;

            for (int i = 0; i < diceCount; i++)
            {
                int roll = DiceRoller.RollD6();
                breakdown.Add(roll);
                total += roll;
            }

            return total;
        }

        /// <summary>
        /// Adds a building to the city.
        /// </summary>
        public void AddBuilding(Building building)
        {
            if (building != null)
            {
                buildings.Add(building);
            }
        }

        /// <summary>
        /// Gets a summary string of the city's current state.
        /// </summary>
        public string GetCitySummary()
        {
            string summary = "";
            summary += "\n═══════════════════════════════════════════════════════════════\n";
            summary += $" ◆ {cityName}{(isCapital ? " ⭐ CAPITAL" : "")}\n";
            summary += "═══════════════════════════════════════════════════════════════\n";
            summary += $" Health Points: {healthPoints,-3}  |  Money: {money,-3}  |  City Power: {cityPower,-2}  |  Upgrades: {upgradePoints}\n";
            summary += "───────────────────────────────────────────────────────────────\n";
            summary += $" Buildings ({buildings.Count}):\n";
            
            if (buildings.Count == 0)
            {
                summary += "   → None\n";
            }
            else
            {
                foreach (var building in buildings)
                {
                    summary += $"   → {building}\n";
                }
            }
            
            summary += "───────────────────────────────────────────────────────────────\n";
            summary += $" Units in Fort: {unitsInFort.Count}\n";
            summary += "═══════════════════════════════════════════════════════════════";
            
            return summary;
        }

        public override string ToString()
        {
            return GetCitySummary();
        }
    }
}
