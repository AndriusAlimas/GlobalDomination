using UnityEngine;
using GlobalDomination.GameData;
using GlobalDomination.Managers;

namespace GlobalDomination
{
    /// <summary>
    /// Quick test script to verify game systems. Attach to a scene object or call methods from the inspector.
    /// </summary>
    public class GameTester : MonoBehaviour
    {
        private const int DefaultBuildingTestRolls = 10;

        private void Start()
        {
            EnsureHudUI();
        }

        private static void EnsureHudUI()
        {
            if (Object.FindFirstObjectByType<UITestManager>() != null)
            {
                return;
            }

            GameObject uiManagerObject = new GameObject("UITestManager");
            uiManagerObject.AddComponent<UITestManager>();
        }

        public void RunGameTest()
        {
            Debug.Log("\n" + new string('=', 70));
            Debug.Log("STARTING GAME TEST");
            Debug.Log(new string('=', 70) + "\n");

            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                GameObject gmObject = new GameObject("GameManager");
                gm = gmObject.AddComponent<GameManager>();
            }

            gm.InitializeTestGame();
            TestBuildingRolls();

            Debug.Log("\n" + new string('=', 70));
            Debug.Log("TEST COMPLETE");
            Debug.Log(new string('=', 70) + "\n");
            Debug.Log("Invoke RunGameTest or TestBuildingRolls again from the inspector as needed.");
        }

        public void TestBuildingRolls()
        {
            Debug.Log("\n=== Testing Building Rolls ===");

            var buildingCounts = new System.Collections.Generic.Dictionary<BuildingType, int>();
            int noneCount = 0;

            for (int i = 0; i < DefaultBuildingTestRolls; i++)
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

            Debug.Log($"\nResults from {DefaultBuildingTestRolls} rolls:");
            Debug.Log($"  Empty slots: {noneCount}");

            foreach (var kvp in buildingCounts)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }

            Debug.Log("=== Building Roll Test Complete ===\n");
        }
    }
}
