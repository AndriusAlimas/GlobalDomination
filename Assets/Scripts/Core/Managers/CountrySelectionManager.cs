using UnityEngine;
using GlobalDomination.GameData;
using GlobalDomination.Managers;

namespace GlobalDomination.Managers
{
    /// <summary>
    /// Manages the country selection process for players.
    /// This is a basic implementation - you'll want to connect this to actual UI buttons/panels.
    /// </summary>
    public class CountrySelectionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        [Header("Settings")]
        [SerializeField] private int maxPlayers = 4;
        
        private int currentSelectingPlayer = 0;
        private string[] playerNames = { "Player 1", "Player 2", "Player 3", "Player 4" };

        private void Start()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager != null)
            {
                // Initialize a new game
                gameManager.StartNewGame(maxPlayers);
            }
        }

        /// <summary>
        /// Called when a player selects a country.
        /// This should be connected to UI buttons for each country.
        /// </summary>
        public void OnCountrySelected(CountryType selectedCountry)
        {
            if (gameManager == null) return;

            // Check if country is already taken
            if (IsCountryTaken(selectedCountry))
            {
                Debug.LogWarning($"{selectedCountry} is already taken!");
                return;
            }

            // Add player with selected country
            string playerName = GetCurrentPlayerName();
            Player newPlayer = gameManager.AddPlayer(playerName, selectedCountry);

            if (newPlayer != null)
            {
                currentSelectingPlayer++;
                
                // Check if all players have selected
                if (currentSelectingPlayer >= maxPlayers)
                {
                    OnAllPlayersSelected();
                }
            }
        }

        /// <summary>
        /// Checks if a country has already been selected by another player.
        /// </summary>
        private bool IsCountryTaken(CountryType country)
        {
            foreach (var player in gameManager.players)
            {
                if (player.selectedCountry == country)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the name of the current player selecting a country.
        /// </summary>
        private string GetCurrentPlayerName()
        {
            if (currentSelectingPlayer < playerNames.Length)
            {
                return playerNames[currentSelectingPlayer];
            }
            return $"Player {currentSelectingPlayer + 1}";
        }

        /// <summary>
        /// Called when all players have selected their countries.
        /// </summary>
        private void OnAllPlayersSelected()
        {
            // You can transition to the main game scene here
            // SceneManager.LoadScene("MainGameScene");
        }

        // UI Button Methods - Connect these to your UI buttons
        public void SelectEngland() => OnCountrySelected(CountryType.England);
        public void SelectAmerica() => OnCountrySelected(CountryType.America);
        public void SelectFrance() => OnCountrySelected(CountryType.France);
        public void SelectRussia() => OnCountrySelected(CountryType.Russia);

        /// <summary>
        /// Gets available countries (not yet selected).
        /// </summary>
        public CountryType[] GetAvailableCountries()
        {
            var allCountries = CountryDatabase.GetAllCountryTypes();
            var available = new System.Collections.Generic.List<CountryType>();

            foreach (var country in allCountries)
            {
                if (!IsCountryTaken(country))
                {
                    available.Add(country);
                }
            }

            return available.ToArray();
        }
    }
}
