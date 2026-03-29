using UnityEngine;

namespace GlobalDomination.GameData
{
    /// <summary>
    /// Rules for city construction: dice contribute points toward a building roll when the bar fills.
    /// </summary>
    public static class CityConstruction
    {
        /// <summary>Construction points needed before "Finish building" is available (matches 10 UI segments).</summary>
        public const int PointsRequired = 10;

        public const int SegmentCount = 10;

        /// <summary>Construction roll: face 1 = 1 pt, face 3 = 2 pts, face 6 = 3 pts; other faces = 0.</summary>
        public static int PointsFromDie(int d6)
        {
            d6 = Mathf.Clamp(d6, 1, 6);
            switch (d6)
            {
                case 1: return 1;
                case 3: return 2;
                case 6: return 3;
                default: return 0;
            }
        }

        public static float NormalizedProgress(int constructionProgress)
        {
            return Mathf.Clamp01((float)constructionProgress / PointsRequired);
        }
    }
}
