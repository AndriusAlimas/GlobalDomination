namespace GlobalDomination.GameData
{
    /// <summary>
    /// One recruitable unit instance stationed in the city's fort.
    /// </summary>
    [System.Serializable]
    public class FortUnitEntry
    {
        public BuildingType buildingType;
        public int buildingLevel;
        /// <summary>0 = not assigned to a division yet.</summary>
        public int divisionNumber;

        /// <summary>-1 = full health (use catalog max). Otherwise clamped remaining HP for this instance.</summary>
        public int remainingHitPoints = -1;

        public FortUnitEntry(BuildingType buildingType, int buildingLevel, int divisionNumber = 0, int remainingHitPoints = -1)
        {
            this.buildingType = buildingType;
            this.buildingLevel = buildingLevel;
            this.divisionNumber = divisionNumber;
            this.remainingHitPoints = remainingHitPoints;
        }
    }
}
