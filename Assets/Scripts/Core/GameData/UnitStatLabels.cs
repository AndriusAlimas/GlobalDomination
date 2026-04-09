namespace GlobalDomination.GameData
{
    /// <summary>
    /// Display names for the three power systems (Heart = P, Tech = Star, Aerial = A).
    /// </summary>
    public static class UnitStatLabels
    {
        public const string HeartPowerName = "P";
        public const string TechPowerName = "★";
        public const string AerialPowerName = "A";

        public static string GetCategoryPowerShortName(UnitHpCategory category)
        {
            switch (category)
            {
                case UnitHpCategory.Heart:
                    return HeartPowerName;
                case UnitHpCategory.Tech:
                    return TechPowerName;
                case UnitHpCategory.Aerial:
                    return AerialPowerName;
                default:
                    return "?";
            }
        }

        public static string GetCategoryPowerDisplayName(UnitHpCategory category)
        {
            switch (category)
            {
                case UnitHpCategory.Heart:
                    return "Power (P)";
                case UnitHpCategory.Tech:
                    return "Power (★ Tech)";
                case UnitHpCategory.Aerial:
                    return "Power (A)";
                default:
                    return "Power";
            }
        }

        public static string GetHpCategoryDisplayName(UnitHpCategory category)
        {
            switch (category)
            {
                case UnitHpCategory.Heart:
                    return "HP";
                case UnitHpCategory.Tech:
                    return "Tech HP";
                case UnitHpCategory.Aerial:
                    return "Aerial HP";
                default:
                    return "HP";
            }
        }
    }
}
