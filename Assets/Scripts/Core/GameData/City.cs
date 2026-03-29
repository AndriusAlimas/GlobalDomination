using System.Collections.Generic;
using GlobalDomination;

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
        
        // Buildings and construction progress (e.g. toward a bonus building roll)
        public List<Building> buildings;
        public int constructionProgress;
        
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
            constructionProgress = 0;
        }

        /// <summary>
        /// Initializes the city with dice rolls for starting stats.
        /// </summary>
        public void InitializeWithDiceRolls(bool includeStartingBuilding = true)
        {
            // Roll 3 times for Health Points (Population)
            healthPoints = RollStatWithBreakdown(3, startingHealthRolls);

            // Roll 2 times for Money
            money = RollStatWithBreakdown(2, startingMoneyRolls);

            // Roll 1 time for City Power (Defense)
            cityPower = RollStatWithBreakdown(1, startingPowerRolls);
            
            if (includeStartingBuilding)
            {
                // Roll for first building
                Building firstBuilding = BuildingRollTable.RollForFirstBuilding();
                if (firstBuilding != null)
                {
                    buildings.Add(firstBuilding);
                }
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

        public override string ToString()
        {
            return cityName;
        }
    }
}
