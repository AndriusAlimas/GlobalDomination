using UnityEngine;

namespace GlobalDomination.GameData
{
    [System.Serializable]
    public class Building
    {
        public BuildingType type;
        public string displayName;
        public int level;

        public Building(BuildingType type, string displayName = null)
        {
            this.type = type;
            this.displayName = displayName ?? DefaultDisplayName(type);
            this.level = 1;
        }

        private static string DefaultDisplayName(BuildingType type)
        {
            if (type == BuildingType.MainBase)
            {
                return "Main Base";
            }

            return type.ToString();
        }

        public override string ToString()
        {
            return $"{displayName} (Level {level})";
        }
    }
}
