using System.Collections.Generic;
using UnityEngine;

namespace GlobalDomination.GameData
{
    /// <summary>
    /// Holds data for a country including its name and available cities.
    /// </summary>
    [System.Serializable]
    public class CountryData
    {
        public CountryType countryType;
        public string countryName;
        public List<string> cityNames;
        
        // Index of the capital city in the cityNames list
        public int capitalIndex;

        public CountryData(CountryType type, string name, List<string> cities, int capitalIndex = 0)
        {
            this.countryType = type;
            this.countryName = name;
            this.cityNames = cities;
            this.capitalIndex = capitalIndex;
        }

        public string GetCapitalName()
        {
            if (capitalIndex >= 0 && capitalIndex < cityNames.Count)
            {
                return cityNames[capitalIndex];
            }
            return cityNames.Count > 0 ? cityNames[0] : "Unknown Capital";
        }
    }

    /// <summary>
    /// Static database of all available countries and their cities.
    /// Each country can have up to 12 cities, but only 6 are usable initially.
    /// </summary>
    public static class CountryDatabase
    {
        private static Dictionary<CountryType, CountryData> countries;

        static CountryDatabase()
        {
            InitializeCountries();
        }

        private static void InitializeCountries()
        {
            countries = new Dictionary<CountryType, CountryData>();

            // England - Capital: London
            countries[CountryType.England] = new CountryData(
                CountryType.England,
                "England",
                new List<string>
                {
                    "London",       // Capital (index 0)
                    "Manchester",
                    "Birmingham",
                    "Liverpool",
                    "Leeds",
                    "Sheffield",
                    "Bristol",      // Unused initially
                    "Newcastle",    // Unused initially
                    "Nottingham",   // Unused initially
                    "Southampton",  // Unused initially
                    "Brighton",     // Unused initially
                    "Oxford"        // Unused initially
                },
                capitalIndex: 0
            );

            // America - Capital: Washington
            countries[CountryType.America] = new CountryData(
                CountryType.America,
                "America",
                new List<string>
                {
                    "Washington D.C.", // Capital (index 0)
                    "New York",
                    "Los Angeles",
                    "Chicago",
                    "Houston",
                    "Phoenix",
                    "Philadelphia",    // Unused initially
                    "San Antonio",     // Unused initially
                    "San Diego",       // Unused initially
                    "Dallas",          // Unused initially
                    "San Jose",        // Unused initially
                    "Austin"           // Unused initially
                },
                capitalIndex: 0
            );

            // France - Capital: Paris
            countries[CountryType.France] = new CountryData(
                CountryType.France,
                "France",
                new List<string>
                {
                    "Paris",        // Capital (index 0)
                    "Marseille",
                    "Lyon",
                    "Toulouse",
                    "Nice",
                    "Nantes",
                    "Strasbourg",   // Unused initially
                    "Montpellier",  // Unused initially
                    "Bordeaux",     // Unused initially
                    "Lille",        // Unused initially
                    "Rennes",       // Unused initially
                    "Reims"         // Unused initially
                },
                capitalIndex: 0
            );

            // Russia - Capital: Moscow
            countries[CountryType.Russia] = new CountryData(
                CountryType.Russia,
                "Russia",
                new List<string>
                {
                    "Moscow",           // Capital (index 0)
                    "Saint Petersburg",
                    "Novosibirsk",
                    "Yekaterinburg",
                    "Kazan",
                    "Nizhny Novgorod",
                    "Chelyabinsk",      // Unused initially
                    "Samara",           // Unused initially
                    "Omsk",             // Unused initially
                    "Rostov-on-Don",    // Unused initially
                    "Ufa",              // Unused initially
                    "Krasnoyarsk"       // Unused initially
                },
                capitalIndex: 0
            );
        }

        /// <summary>
        /// Gets country data for a specific country type.
        /// </summary>
        public static CountryData GetCountryData(CountryType type)
        {
            if (countries.ContainsKey(type))
            {
                return countries[type];
            }
            
            Debug.LogError($"Country data not found for {type}");
            return null;
        }

        /// <summary>
        /// Gets all available country types.
        /// </summary>
        public static CountryType[] GetAllCountryTypes()
        {
            return new CountryType[] 
            { 
                CountryType.England, 
                CountryType.America, 
                CountryType.France, 
                CountryType.Russia 
            };
        }

        /// <summary>
        /// Gets the number of initially usable cities per country.
        /// </summary>
        public static int GetUsableCityCount()
        {
            return 6;
        }
    }
}
