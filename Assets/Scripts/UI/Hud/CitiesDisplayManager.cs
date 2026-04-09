using System.Collections.Generic;
using UnityEngine;
using GlobalDomination.GameData;
using GlobalDomination.UI;

namespace GlobalDomination.UI.Hud
{
    /// <summary>
    /// Manages city icons on the HUD. Default layout: 2 columns × 3 rows (max 6 cities).
    /// </summary>
    public class CitiesDisplayManager : MonoBehaviour
    {
        /// <inheritdoc cref="Player.MaxOwnedCities"/>
        public const int MaxCityIcons = Player.MaxOwnedCities;

        [Header("Layout Settings")]
        [Tooltip("Legacy single-column stack. Leave off for 2×3 grid.")]
        [SerializeField] private bool stackVertically = false;
        [Tooltip("Center-to-center horizontal gap in a row (room for action menu).")]
        [SerializeField] private float horizontalSpacing = 760f;
        [Tooltip("Vertical gap between rows (centers).")]
        [SerializeField] private float verticalSpacing = 300f;
        [SerializeField] private int iconsPerRow = 2;
        [Tooltip("Grid: only Y is used (first row). Legacy stack uses both X and Y.")]
        [SerializeField] private Vector2 startPosition = new Vector2(0f, -280f);
        [SerializeField] private float sidePadding = 100f;
        [SerializeField] private float minHorizontalCenterSpacing = 680f;
        [Tooltip("Half-width of city icon widget for left/right inset math (~CreateCityIcon footprint).")]
        [SerializeField] private float cityIconApproxHalfWidth = 138f;

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
            ClearCityIcons();

            if (cities == null || cities.Count == 0)
            {
                return;
            }

            int count = Mathf.Min(cities.Count, MaxCityIcons);

            UpdateContentSizeForCities(count);
            Canvas.ForceUpdateCanvases();

            for (int i = 0; i < count; i++)
            {
                Vector2 position = CalculateIconPosition(i, count);
                CityIconUI cityIcon = CityIconUI.CreateCityIcon(transform, position, cities[i]);
                cityIcons.Add(cityIcon);
            }
        }

        /// <summary>Returns the first HUD city icon rect (grid index 0), if any.</summary>
        public bool TryGetFirstCityIconRect(out RectTransform cityIconRect)
        {
            cityIconRect = null;
            if (cityIcons == null || cityIcons.Count == 0 || cityIcons[0] == null)
            {
                return false;
            }

            cityIconRect = cityIcons[0].transform as RectTransform;
            return cityIconRect != null;
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

        private void UpdateContentSizeForCities(int count)
        {
            if (containerRect == null)
            {
                return;
            }

            float contentWidth = Mathf.Max(containerRect.sizeDelta.x, 1400f);
            float contentHeight = Mathf.Max(containerRect.sizeDelta.y, 820f);

            if (stackVertically)
            {
                float per = Mathf.Max(180f, verticalSpacing);
                float bottom = 420f;
                contentHeight = Mathf.Max(contentHeight, Mathf.Abs(startPosition.y) + ((count - 1) * per) + bottom);
            }
            else
            {
                int cols = Mathf.Max(1, iconsPerRow);
                int rows = Mathf.CeilToInt(count / (float)cols);
                rows = Mathf.Clamp(rows, 1, 3);
                float rowStep = Mathf.Max(180f, verticalSpacing);
                float bottom = 360f;
                contentHeight = Mathf.Max(contentHeight, Mathf.Abs(startPosition.y) + ((rows - 1) * rowStep) + bottom);

                float hSpacing = Mathf.Max(minHorizontalCenterSpacing, horizontalSpacing);
                float approxRowWidth = sidePadding * 2f + hSpacing + (cityIconApproxHalfWidth * 4f);
                contentWidth = Mathf.Max(contentWidth, approxRowWidth);
            }

            containerRect.sizeDelta = new Vector2(contentWidth, contentHeight);
        }

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

            float layoutWidth = GetLayoutWidthForGrid();

            float innerLeft = sidePadding + cityIconApproxHalfWidth;
            float innerUsable = Mathf.Max(
                1f,
                layoutWidth - (2f * sidePadding) - (2f * cityIconApproxHalfWidth));

            float maxSpacingToFit = itemsInRow > 1 ? innerUsable / (itemsInRow - 1) : horizontalSpacing;

            float fitHorizontalSpacing = Mathf.Max(
                minHorizontalCenterSpacing,
                Mathf.Min(horizontalSpacing, maxSpacingToFit));
            float fitVerticalSpacing = Mathf.Min(verticalSpacing, 420f);

            // Left-anchored rows: col 0 is always near the left inset (does not sit under center popups).
            float firstCenterX = innerLeft;

            float x = firstCenterX + (col * fitHorizontalSpacing);
            float y = startPosition.y - (row * fitVerticalSpacing);

            return new Vector2(x, y);
        }

        private float GetLayoutWidthForGrid()
        {
            if (containerRect == null)
            {
                return Mathf.Max(1200f, Screen.width * 0.92f);
            }

            float w = containerRect.rect.width;
            if (w < 2f)
            {
                w = containerRect.sizeDelta.x;
            }

            return Mathf.Max(w, 800f);
        }

        /// <summary>
        /// Creates the cities display: one container under the canvas (no scroll/zoom).
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
            manager.stackVertically = false;
            manager.iconsPerRow = 2;
            manager.horizontalSpacing = 760f;
            manager.verticalSpacing = 300f;
            manager.startPosition = new Vector2(0f, -280f);
            manager.minHorizontalCenterSpacing = 680f;
            manager.sidePadding = 100f;
            manager.cityIconApproxHalfWidth = 138f;

            return manager;
        }
    }
}
