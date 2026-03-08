using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalDomination.GameData;

namespace GlobalDomination.Managers
{
    /// <summary>
    /// Simple UI manager for testing the game systems.
    /// Shows game information and provides buttons to test game functions.
    /// </summary>
    public class UITestManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI gameInfoText;
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private TextMeshProUGUI instructionsText;

        [Header("Settings")]
        [SerializeField] private bool autoInitializeGame = true;

        private GameManager gameManager;

        private void Start()
        {
            SetupInstructions();
            
            if (autoInitializeGame)
            {
                InitializeGame();
            }
        }

        private void SetupInstructions()
        {
            if (instructionsText != null)
            {
                instructionsText.text = @"<b>GAME TEST CONTROLS</b>

<b>Keyboard:</b>
T - Initialize New Game
B - Roll for Building
N - Next Turn
P - Print to Console
R - Refresh Display

<b>UI Buttons:</b>
Use the buttons below to test game functions

<b>Goal:</b>
Test the dice rolling system and game initialization";
            }
        }

        public void InitializeGame()
        {
            Debug.Log("=== Initializing Game ===");
            
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                GameObject gmObject = new GameObject("GameManager");
                gameManager = gmObject.AddComponent<GameManager>();
            }

            gameManager.InitializeTestGame();
            UpdateDisplay();
            
            Debug.Log("Game initialized! Use buttons or keyboard to interact.");
        }

        public void RollForBuilding()
        {
            if (gameManager == null)
            {
                Debug.LogWarning("Game not initialized! Press T or click Initialize Game.");
                return;
            }

            Player currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.ownedCities.Count == 0)
            {
                Debug.LogWarning("No player or cities available!");
                return;
            }

            City capital = currentPlayer.GetCapitalCity();
            Building newBuilding = BuildingRollTable.RollForBuilding();
            
            if (newBuilding != null)
            {
                capital.AddBuilding(newBuilding);
                Debug.Log($"<color=green>{currentPlayer.playerName} rolled: {newBuilding.displayName}</color>");
            }
            else
            {
                Debug.Log($"<color=yellow>{currentPlayer.playerName} rolled: Nothing (empty slot)</color>");
            }
            
            UpdateDisplay();
        }

        public void NextTurn()
        {
            if (gameManager == null)
            {
                Debug.LogWarning("Game not initialized!");
                return;
            }

            gameManager.NextTurn();
            UpdateDisplay();
            
            Player currentPlayer = gameManager.GetCurrentPlayer();
            Debug.Log($"<color=cyan>=== {currentPlayer.playerName}'s Turn ===</color>");
        }

        public void PrintGameState()
        {
            if (gameManager == null)
            {
                Debug.LogWarning("Game not initialized!");
                return;
            }

            gameManager.PrintGameState();
        }

        public void TestMultipleBuildingRolls()
        {
            Debug.Log("\n=== Testing 10 Building Rolls ===");
            
            var buildingCounts = new System.Collections.Generic.Dictionary<BuildingType, int>();
            int noneCount = 0;

            for (int i = 0; i < 10; i++)
            {
                int roll1 = DiceRoller.RollD6();
                int roll2 = DiceRoller.RollD6();
                Building building = BuildingRollTable.GetBuildingFromRoll(roll1, roll2);
                
                if (building != null)
                {
                    if (!buildingCounts.ContainsKey(building.type))
                    {
                        buildingCounts[building.type] = 0;
                    }
                    buildingCounts[building.type]++;
                    Debug.Log($"  Roll {i+1}: [{roll1},{roll2}] = {building.displayName}");
                }
                else
                {
                    noneCount++;
                    Debug.Log($"  Roll {i+1}: [{roll1},{roll2}] = <color=grey>Nothing</color>");
                }
            }

            Debug.Log($"\n<b>Summary:</b> {10 - noneCount} buildings, {noneCount} empty slots");
        }

        private void UpdateDisplay()
        {
            if (gameManager == null || gameManager.players.Count == 0)
            {
                if (gameInfoText != null)
                    gameInfoText.text = "Press T or click 'Initialize Game' to start";
                if (currentPlayerText != null)
                    currentPlayerText.text = "No game active";
                return;
            }

            // Update current player display
            Player currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayerText != null && currentPlayer != null)
            {
                currentPlayerText.text = $"<b>Current Turn:</b> {currentPlayer.playerName} ({currentPlayer.selectedCountry})";
            }

            // Update game info display
            if (gameInfoText != null)
            {
                string info = "<b>=== GAME STATE ===</b>\n\n";
                
                foreach (var player in gameManager.players)
                {
                    bool isCurrentPlayer = player == currentPlayer;
                    info += isCurrentPlayer ? "<color=yellow>" : "";
                    info += $"<b>{player.playerName}</b> - {player.selectedCountry}\n";
                    
                    foreach (var city in player.ownedCities)
                    {
                        info += $"\n<b>{city.cityName}</b> {(city.isCapital ? "★" : "")}\n";
                        info += $"  HP: {city.healthPoints} | Money: {city.money} | Power: {city.cityPower}\n";
                        info += $"  Upgrades: {city.upgradePoints} | Units: {city.unitsInFort.Count}\n";
                        info += $"  <b>Buildings ({city.buildings.Count}):</b>\n";
                        
                        if (city.buildings.Count == 0)
                        {
                            info += "    None\n";
                        }
                        else
                        {
                            foreach (var building in city.buildings)
                            {
                                info += $"    • {building.displayName}\n";
                            }
                        }
                    }
                    
                    info += isCurrentPlayer ? "</color>" : "";
                    info += "\n";
                }
                
                gameInfoText.text = info;
            }
        }

        // Update display and handle keyboard shortcuts
        private void Update()
        {
            UpdateDisplay();
            
            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.T))
            {
                InitializeGame();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                RollForBuilding();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                NextTurn();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                PrintGameState();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                UpdateDisplay();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                TestMultipleBuildingRolls();
            }
        }
    }
}
