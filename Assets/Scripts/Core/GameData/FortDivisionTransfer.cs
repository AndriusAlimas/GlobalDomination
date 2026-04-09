namespace GlobalDomination.GameData
{
    /// <summary>
    /// Moves every fort unit in a division from one city to another’s fort as unassigned (division 0).
    /// </summary>
    public static class FortDivisionTransfer
    {
        public static bool TryMoveDivisionToCityFort(City fromCity, int divisionNumber, City toCity, out string errorMessage)
        {
            errorMessage = null;

            if (fromCity == null || toCity == null)
            {
                errorMessage = "Invalid city.";
                return false;
            }

            if (fromCity == toCity)
            {
                errorMessage = "Choose a different city.";
                return false;
            }

            if (divisionNumber <= 0)
            {
                errorMessage = "Invalid division.";
                return false;
            }

            if (fromCity.fortUnits == null)
            {
                errorMessage = "No fort roster at source.";
                return false;
            }

            if (toCity.fortUnits == null)
            {
                toCity.fortUnits = new System.Collections.Generic.List<FortUnitEntry>();
            }

            if (fromCity.ownerId != toCity.ownerId)
            {
                errorMessage = "Cities must belong to the same owner.";
                return false;
            }

            int moved = 0;
            for (int i = fromCity.fortUnits.Count - 1; i >= 0; i--)
            {
                FortUnitEntry e = fromCity.fortUnits[i];
                if (e == null || e.divisionNumber != divisionNumber)
                {
                    continue;
                }

                fromCity.fortUnits.RemoveAt(i);
                toCity.fortUnits.Add(new FortUnitEntry(e.buildingType, e.buildingLevel, 0, e.remainingHitPoints));
                moved++;
            }

            if (moved == 0)
            {
                errorMessage = "No units in that division.";
                return false;
            }

            return true;
        }
    }
}
