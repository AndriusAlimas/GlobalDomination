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

        private void Awake()
        {
            // Run before AfterSceneLoad music bootstrap so UITestManager exists for optional Background Music clip.
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
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                GameObject gmObject = new GameObject("GameManager");
                gm = gmObject.AddComponent<GameManager>();
            }

            gm.InitializeTestGame();
            TestBuildingRolls();
        }

        public void TestBuildingRolls()
        {
            for (int i = 0; i < DefaultBuildingTestRolls; i++)
            {
                BuildingRollTable.RollForBuilding();
            }
        }
    }
}
