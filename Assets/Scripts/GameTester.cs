using UnityEngine;
using GlobalDomination.GameData;
using GlobalDomination.Managers;
using GlobalDomination.UI;

namespace GlobalDomination
{
    /// <summary>
    /// Quick test script to verify the game systems are working.
    /// Attach this to a GameObject in your scene to test the game flow.
    /// </summary>
    public class GameTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool testBuildingRolls = true;
        [SerializeField] private int numberOfBuildingTests = 10;
        [SerializeField] private bool enableLegacyKeyboardShortcuts = false;

        private GameStateDisplayUI gameStateDisplayUI;
        private UITestManager uiTestManager;

        private void Start()
        {
            EnsureHudUI();

            // Legacy display is only needed when UITestManager is not used.
            if (uiTestManager == null)
            {
                SetupGameStateUI();
            }

            if (runTestOnStart)
            {
                RunGameTest();
            }
        }

        private void EnsureHudUI()
        {
            uiTestManager = FindFirstObjectByType<UITestManager>();
            if (uiTestManager != null)
            {
                return;
            }

            GameObject uiManagerObject = new GameObject("UITestManager");
            uiTestManager = uiManagerObject.AddComponent<UITestManager>();
        }

        private void SetupGameStateUI()
        {
            GameObject uiObject = new GameObject("GameStateDisplayUI");
            gameStateDisplayUI = uiObject.AddComponent<GameStateDisplayUI>();
        }

        private void Update()
        {
            if (!enableLegacyKeyboardShortcuts)
            {
                return;
            }

            // Press T to run test
            if (Input.GetKeyDown(KeyCode.T))
            {
                RunGameTest();
            }

            // Press B to test building rolls
            if (Input.GetKeyDown(KeyCode.B))
            {
                TestBuildingRolls();
            }

            // Press N to advance to next turn
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.NextTurn();
                }
            }

            // Press P to toggle game state display
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (gameStateDisplayUI != null)
                {
                    gameStateDisplayUI.ToggleDisplay();
                }
            }
        }

        public void RunGameTest()
        {
            Debug.Log("\n" + new string('=', 70));
            Debug.Log("STARTING GAME TEST");
            Debug.Log(new string('=', 70) + "\n");

            // Ensure GameManager exists
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                GameObject gmObject = new GameObject("GameManager");
                gm = gmObject.AddComponent<GameManager>();
            }

            // Initialize test game
            gm.InitializeTestGame();

            // Test building rolls if enabled
            if (testBuildingRolls)
            {
                TestBuildingRolls();
            }

            Debug.Log("\n" + new string('=', 70));
            Debug.Log("TEST COMPLETE");
            Debug.Log(new string('=', 70) + "\n");
            Debug.Log("Controls:");
            Debug.Log("  T - Run test again");
            Debug.Log("  B - Test building rolls");
            Debug.Log("  N - Next turn");
            Debug.Log("  P - Print game state");
        }

        public void TestBuildingRolls()
        {
            Debug.Log("\n=== Testing Building Rolls ===");
            
            var buildingCounts = new System.Collections.Generic.Dictionary<BuildingType, int>();
            int noneCount = 0;

            for (int i = 0; i < numberOfBuildingTests; i++)
            {
                Building building = BuildingRollTable.RollForBuilding();
                
                if (building != null)
                {
                    if (!buildingCounts.ContainsKey(building.type))
                    {
                        buildingCounts[building.type] = 0;
                    }
                    buildingCounts[building.type]++;
                }
                else
                {
                    noneCount++;
                }
            }

            Debug.Log($"\nResults from {numberOfBuildingTests} rolls:");
            Debug.Log($"  Empty slots: {noneCount}");
            
            foreach (var kvp in buildingCounts)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }

            Debug.Log("=== Building Roll Test Complete ===\n");
        }

        /// <summary>
        /// Tests creating a custom player setup.
        /// </summary>
        public void TestCustomGameSetup()
        {
            Debug.Log("\n=== Testing Custom Game Setup ===");

            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            gm.StartNewGame(2);
            gm.AddPlayer("Alice", CountryType.France);
            gm.AddPlayer("Bob", CountryType.America);

            gm.PrintGameState();

            Debug.Log("=== Custom Game Setup Complete ===\n");
        }
    }
}
