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
            this.displayName = displayName ?? type.ToString();
            this.level = 1;
        }

        public override string ToString()
        {
            return $"{displayName} (Level {level})";
        }
    }
}
