using System.Collections.Generic;
using UnityEngine;

namespace GlobalDomination.GameData
{
    [System.Serializable]
    public class Player
    {
        public int playerId;
        public string playerName;
        public CountryType selectedCountry;
        public List<City> ownedCities;

        public Player(int id, string name)
        {
            this.playerId = id;
            this.playerName = name;
            this.ownedCities = new List<City>();
        }

        /// <summary>
        /// Initializes the player with a selected country and its capital city.
        /// </summary>
        public void InitializeWithCountry(CountryType country)
        {
            selectedCountry = country;
            CountryData countryData = CountryDatabase.GetCountryData(country);
            
            if (countryData != null)
            {
                // Create and initialize the capital city
                string capitalName = countryData.GetCapitalName();
                City capital = new City(capitalName, capital: true, ownerId: playerId);
                capital.InitializeWithDiceRolls();
                
                ownedCities.Add(capital);
                
                Debug.Log($"Player {playerName} selected {countryData.countryName}");
                Debug.Log(capital.GetCitySummary());
            }
        }

        /// <summary>
        /// Gets the player's capital city.
        /// </summary>
        public City GetCapitalCity()
        {
            return ownedCities.Find(city => city.isCapital);
        }

        /// <summary>
        /// Adds a city to the player's owned cities.
        /// </summary>
        public void AddCity(City city)
        {
            city.ownerId = playerId;
            ownedCities.Add(city);
        }

        /// <summary>
        /// Removes a city from the player's owned cities (e.g., when captured).
        /// </summary>
        public void RemoveCity(City city)
        {
            ownedCities.Remove(city);
        }

        /// <summary>
        /// Checks if the player has lost (no cities remaining).
        /// </summary>
        public bool HasLost()
        {
            return ownedCities.Count == 0;
        }

        /// <summary>
        /// Gets a summary of all player's cities.
        /// </summary>
        public string GetPlayerSummary()
        {
            int totalHealth = 0;
            int totalMoney = 0;
            int totalPower = 0;
            int totalUpgrades = 0;
            int totalBuildings = 0;
            int totalUnits = 0;

            foreach (var city in ownedCities)
            {
                totalHealth += city.healthPoints;
                totalMoney += city.money;
                totalPower += city.cityPower;
                totalUpgrades += city.upgradePoints;
                totalBuildings += city.buildings.Count;
                totalUnits += city.unitsInFort.Count;
            }

            string summary = "\n";
            summary += "╔═══════════════════════════════════════════════════════════════╗\n";
            summary += "║                      👑 PLAYER SUMMARY 👑                     ║\n";
            summary += "╠═══════════════════════════════════════════════════════════════╣\n";
            summary += $"║  Player Name:  {playerName,-45} ║\n";
            summary += $"║  Country:      {selectedCountry,-45} ║\n";
            summary += $"║  Cities Owned: {ownedCities.Count,-45} ║\n";
            summary += "╠═══════════════════════════════════════════════════════════════╣\n";
            summary += "║                        TOTAL STATISTICS                      ║\n";
            summary += $"║  Health Points: {totalHealth,-4}  |  Total Money: {totalMoney,-6}  |  City Power: {totalPower,-3}  ║\n";
            summary += $"║  Upgrade Points: {totalUpgrades,-4}  |  Buildings: {totalBuildings,-5}  |  Units in Forts: {totalUnits,-2}  ║\n";
            summary += "╚═══════════════════════════════════════════════════════════════╝\n";

            foreach (var city in ownedCities)
            {
                summary += city.GetCitySummary() + "\n";
            }

            return summary;
        }
    }
}
