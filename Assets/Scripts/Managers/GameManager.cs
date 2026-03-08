using System.Collections.Generic;
using UnityEngine;
using GlobalDomination.GameData;

namespace GlobalDomination.Managers
{
    /// <summary>
    /// Main game manager that controls game flow and state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int numberOfPlayers = 2;

        [Header("Game State")]
        public List<Player> players = new List<Player>();
        public int currentPlayerIndex = 0;
        public bool gameStarted = false;
        public bool gameOver = false;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // For testing purposes - you can remove this and call InitializeGame manually
            // InitializeTestGame();
        }

        /// <summary>
        /// Initializes a test game with predefined settings.
        /// </summary>
        public void InitializeTestGame()
        {
            Debug.Log("=== Initializing Test Game ===");
            
            // Create Player 1
            Player player1 = new Player(1, "Player 1");
            player1.InitializeWithCountry(CountryType.England);
            players.Add(player1);

            // Create Player 2
            Player player2 = new Player(2, "Player 2");
            player2.InitializeWithCountry(CountryType.Russia);
            players.Add(player2);

            gameStarted = true;
            currentPlayerIndex = 0;

            Debug.Log("\n=== Game Initialized ===");
            PrintGameState();
        }

        /// <summary>
        /// Starts a new game with the specified number of players.
        /// Players must be added and initialized separately.
        /// </summary>
        public void StartNewGame(int numPlayers)
        {
            players.Clear();
            numberOfPlayers = numPlayers;
            currentPlayerIndex = 0;
            gameStarted = false;
            gameOver = false;
            
            Debug.Log($"New game created for {numPlayers} players. Add players and initialize them.");
        }

        /// <summary>
        /// Adds a player to the game with the specified country.
        /// </summary>
        public Player AddPlayer(string playerName, CountryType country)
        {
            if (players.Count >= numberOfPlayers)
            {
                Debug.LogWarning("Maximum number of players reached!");
                return null;
            }

            int playerId = players.Count + 1;
            Player newPlayer = new Player(playerId, playerName);
            newPlayer.InitializeWithCountry(country);
            players.Add(newPlayer);

            Debug.Log($"Player {playerName} added with {country}");

            // Start the game when all players have joined
            if (players.Count == numberOfPlayers)
            {
                gameStarted = true;
                Debug.Log("All players joined! Game started!");
            }

            return newPlayer;
        }

        /// <summary>
        /// Gets the current active player.
        /// </summary>
        public Player GetCurrentPlayer()
        {
            if (players.Count == 0) return null;
            return players[currentPlayerIndex];
        }

        /// <summary>
        /// Advances to the next player's turn.
        /// </summary>
        public void NextTurn()
        {
            if (!gameStarted || gameOver) return;

            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            
            // Skip eliminated players
            while (GetCurrentPlayer().HasLost() && !CheckGameOver())
            {
                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            }

            Debug.Log($"\n=== Turn: {GetCurrentPlayer().playerName} ===");
        }

        /// <summary>
        /// Checks if the game is over (only one player with cities remaining).
        /// </summary>
        public bool CheckGameOver()
        {
            int playersWithCities = 0;
            Player winner = null;

            foreach (var player in players)
            {
                if (!player.HasLost())
                {
                    playersWithCities++;
                    winner = player;
                }
            }

            if (playersWithCities <= 1)
            {
                gameOver = true;
                if (winner != null)
                {
                    Debug.Log($"\n{'='*50}");
                    Debug.Log($"GAME OVER! {winner.playerName} wins!");
                    Debug.Log($"{'='*50}\n");
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Prints the current state of the game.
        /// </summary>
        public void PrintGameState()
        {
            Debug.Log("\n╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗");
            Debug.Log("║                           🎮 GLOBAL DOMINATION - GAME STATE 🎮                                                                 ║");
            string activePlayer = players.Count > 0 ? players[currentPlayerIndex].playerName : "None";
            Debug.Log($"║                                Players: {players.Count,-2}  |  Active Player: {activePlayer,-30}                            ║");
            Debug.Log("╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝");

            foreach (var player in players)
            {
                Debug.Log(player.GetPlayerSummary());
            }
        }

        /// <summary>
        /// Gets all available countries.
        /// </summary>
        public CountryType[] GetAvailableCountries()
        {
            return CountryDatabase.GetAllCountryTypes();
        }

        /// <summary>
        /// Test method to roll for a building.
        /// </summary>
        public void TestBuildingRoll()
        {
            Debug.Log("\n=== Testing Building Roll ===");
            Building building = BuildingRollTable.RollForBuilding();
            
            if (building != null)
            {
                Debug.Log($"Rolled building: {building.displayName}");
            }
            else
            {
                Debug.Log("Rolled nothing (empty slot)");
            }
        }
    }
}
