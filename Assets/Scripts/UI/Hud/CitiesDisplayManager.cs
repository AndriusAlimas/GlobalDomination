using System.Collections.Generic;
using UnityEngine;
using GlobalDomination.GameData;
using GlobalDomination.UI;

namespace GlobalDomination.UI.Hud
{
    /// <summary>
    /// Manages the display of all city icons in a grid layout with dynamic sizing.
    /// </summary>
    public class CitiesDisplayManager : MonoBehaviour
    {
        [Header("Layout Settings")]
        [SerializeField] private bool stackVertically = true;
        [SerializeField] private float horizontalSpacing = 525f;
        [SerializeField] private float verticalSpacing = 320f;
        [SerializeField] private int iconsPerRow = 3;
        [SerializeField] private Vector2 startPosition = new Vector2(220f, -320f);
        [SerializeField] private float sidePadding = 120f;
        
        private RectTransform containerRect;
        private List<CityIconUI> cityIcons = new List<CityIconUI>();
        
        private void Awake()
        {
            containerRect = GetComponent<RectTransform>();
            if (containerRect == null)
            {
                containerRect = gameObject.AddComponent<RectTransform>();
            }
        }
        
        /// <summary>
        /// Displays cities in a grid layout.
        /// </summary>
        public void DisplayCities(List<City> cities)
        {
            // Clear existing icons
            ClearCityIcons();
            
            if (cities == null || cities.Count == 0)
            {
                return;
            }
            
            // Create new icons
            for (int i = 0; i < cities.Count; i++)
            {
                Vector2 position = CalculateIconPosition(i, cities.Count);
                CityIconUI cityIcon = CityIconUI.CreateCityIcon(transform, position, cities[i]);
                cityIcons.Add(cityIcon);
            }
        }
        
        /// <summary>
        /// Clears all city icons.
        /// </summary>
        public void ClearCityIcons()
        {
            foreach (var cityIcon in cityIcons)
            {
                if (cityIcon != null)
                {
                    Destroy(cityIcon.gameObject);
                }
            }
            cityIcons.Clear();
        }
        
        /// <summary>
        /// Calculates the position for a city icon based on its index.
        /// </summary>
        private Vector2 CalculateIconPosition(int index, int totalCities)
        {
            if (stackVertically)
            {
                float verticalX = startPosition.x;
                float verticalY = startPosition.y - (index * Mathf.Max(180f, verticalSpacing));
                return new Vector2(verticalX, verticalY);
            }

            int row = index / iconsPerRow;
            int col = index % iconsPerRow;

            int rowStartIndex = row * iconsPerRow;
            int citiesRemaining = Mathf.Max(0, totalCities - rowStartIndex);
            int itemsInRow = Mathf.Min(iconsPerRow, citiesRemaining);

            float containerWidth = containerRect != null && containerRect.rect.width > 1f
                ? containerRect.rect.width
                : Mathf.Max(1200f, Screen.width * 0.9f);

            float maxSpacingToFit = itemsInRow > 1
                ? (containerWidth - (sidePadding * 2f)) / (itemsInRow - 1)
                : horizontalSpacing;

            float fitHorizontalSpacing = Mathf.Min(horizontalSpacing, maxSpacingToFit);
            float fitVerticalSpacing = Mathf.Min(verticalSpacing, 420f);

            float rowWidth = (itemsInRow - 1) * fitHorizontalSpacing;
            float rowLeftX = startPosition.x - (rowWidth * 0.5f);
            
            float x = rowLeftX + (col * fitHorizontalSpacing);
            float y = startPosition.y - (row * fitVerticalSpacing);
            
            return new Vector2(x, y);
        }
        
        /// <summary>
        /// Creates a cities display manager programmatically.
        /// </summary>
        public static CitiesDisplayManager CreateCitiesDisplay(Canvas canvas)
        {
            GameObject container = new GameObject("CitiesDisplayContainer");
            container.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(Mathf.Max(1200f, Screen.width * 0.92f), Mathf.Max(760f, Screen.height * 0.82f));
            
            CitiesDisplayManager manager = container.AddComponent<CitiesDisplayManager>();
            return manager;
        }
    }
}
