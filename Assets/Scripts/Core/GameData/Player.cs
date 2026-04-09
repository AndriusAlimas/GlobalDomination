using System.Collections.Generic;
using UnityEngine;

namespace GlobalDomination.GameData
{
    [System.Serializable]
    public class Player
    {
        /// <summary>Maximum cities one player may own (matches HUD grid).</summary>
        public const int MaxOwnedCities = 6;

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
            }
        }

        /// <summary>
        /// Gets the player's capital city.
        /// </summary>
        public City GetCapitalCity()
        {
            return ownedCities.Find(city => city.isCapital);
        }

        /// <summary>True if this player may found another city (under <see cref="MaxOwnedCities"/>).</summary>
        public bool CanFoundAdditionalCity()
        {
            return ownedCities != null && ownedCities.Count < MaxOwnedCities;
        }

        /// <summary>
        /// Adds a city to the player's owned cities.
        /// </summary>
        public void AddCity(City city)
        {
            if (city == null || ownedCities == null)
            {
                return;
            }

            if (ownedCities.Count >= MaxOwnedCities)
            {
                Debug.LogWarning($"[Player] Cannot add city — already at max ({MaxOwnedCities}).");
                return;
            }

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
    }
}
