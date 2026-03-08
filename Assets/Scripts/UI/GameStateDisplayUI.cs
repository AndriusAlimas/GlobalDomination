using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalDomination.Managers;

namespace GlobalDomination.UI
{
    /// <summary>
    /// Displays the current game state on a 2D UI panel.
    /// </summary>
    public class GameStateDisplayUI : MonoBehaviour
    {
        private Canvas canvas;
        private TextMeshProUGUI displayText;
        private RectTransform panelRect;
        private LayoutGroup layoutGroup;
        private bool isVisible = false;

        private void Awake()
        {
            SetupUI();
        }

        /// <summary>
        /// Sets up the UI canvas and text components.
        /// </summary>
        private void SetupUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("GameStateCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Set canvas scale
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            // Create Background Panel (centered)
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvas.transform, false);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(800, 600);

            // Create Text
            GameObject textObj = new GameObject("GameStateText");
            textObj.transform.SetParent(panelObj.transform, false);
            displayText = textObj.AddComponent<TextMeshProUGUI>();
            displayText.text = "Player Stats";
            displayText.fontSize = 22;
            displayText.alignment = TextAlignmentOptions.Top;
            displayText.color = new Color(0.9f, 0.9f, 1f, 1f);
            displayText.margin = new Vector4(10, 10, 10, 10);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(15, 15);
            textRect.offsetMax = new Vector2(-15, -15);

            // Hide initially
            canvasObj.SetActive(false);
        }

        /// <summary>
        /// Toggles the game state display visibility.
        /// </summary>
        public void ToggleDisplay()
        {
            isVisible = !isVisible;
            canvas.gameObject.SetActive(isVisible);

            if (isVisible)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Updates the displayed game state information.
        /// </summary>
        public void UpdateDisplay()
        {
            if (!isVisible || GameManager.Instance == null) return;

            string stateText = GetCurrentPlayerStats();
            displayText.text = stateText;
        }

        /// <summary>
        /// Gets stats for only the current player.
        /// </summary>
        private string GetCurrentPlayerStats()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.players.Count == 0)
                return "No player data available";

            var currentPlayer = gm.players[gm.currentPlayerIndex];

            string state = "  <color=#4ECDC4>╔══════════════════════════════════════════════════════╗</color>\n";
            state += "  <color=#4ECDC4>║</color> <b><color=#FFD700>           🎮 YOUR GAME STATS 🎮                        </color></b><color=#4ECDC4>║</color>\n";
            state += "  <color=#4ECDC4>╠══════════════════════════════════════════════════════╣</color>\n\n";

            state += GetPlayerInfo(currentPlayer);

            state += "\n  <color=#4ECDC4>╚══════════════════════════════════════════════════════╝</color>";
            return state;
        }

        /// <summary>
        /// Gets formatted player information.
        /// </summary>
        private string GetPlayerInfo(GlobalDomination.GameData.Player player)
        {
            string info = $"  <b><color=#FFD700>Player: {player.playerName}</color></b> | <color=#87CEEB>Country: {player.selectedCountry}</color>\n\n";

            info += "  <color=#4ECDC4>┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓</color>\n";
            info += "  <color=#4ECDC4>┃</color> <b><color=#FFD700>YOUR CITIES</color></b><color=#4ECDC4>                                     ┃</color>\n";
            info += "  <color=#4ECDC4>┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫</color>\n";

            foreach (var city in player.ownedCities)
            {
                string cityHeader = $"◆ {city.cityName}{(city.isCapital ? " ⭐ CAPITAL" : "")}";
                info += $"  <color=#4ECDC4>┃</color> <color=#1ABC9C><b>{cityHeader,-38}</b></color> <color=#4ECDC4>┃</color>\n";
                
                string healthColor = city.healthPoints > 10 ? "#2ECC71" : "#E74C3C";
                string moneyColor = city.money > 10 ? "#F39C12" : "#95A5A6";
                string powerColor = city.cityPower > 3 ? "#9B59B6" : "#BDC3C7";
                
                info += $"  <color=#4ECDC4>┃</color>   <color={healthColor}>HP: {city.healthPoints,3}</color> | <color={moneyColor}>Money: {city.money,3}</color> | <color={powerColor}>Power: {city.cityPower,2}</color> | <color=#E67E22>Upgr: {city.upgradePoints,-2}</color> | <color=#3498DB>Units: {city.unitsInFort.Count,-2}</color>" +
                    $"<color=#4ECDC4>          ┃</color>\n";

                if (city.buildings.Count > 0)
                {
                    string buildings = string.Join(", ", city.buildings);
                    if (buildings.Length > 40)
                        buildings = buildings.Substring(0, 37) + "...";
                    info += $"  <color=#4ECDC4>┃</color>   <color=#95A5A6>Buildings: {buildings,-37}</color> <color=#4ECDC4>┃</color>\n";
                }
                else
                {
                    info += $"  <color=#4ECDC4>┃</color>   <color=#7F8C8D>Buildings: None{"",-32}</color> <color=#4ECDC4>┃</color>\n";
                }
                info += "  <color=#4ECDC4>┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫</color>\n";
            }

            info += "  <color=#4ECDC4>┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛</color>\n";

            return info;
        }
    }
}


