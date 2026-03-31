namespace GlobalDomination.GameData
{
    /// <summary>
    /// Static definition for the unit tied to a building (recruitment / fort display).
    /// <see cref="CategoryPower"/> is labeled P (Heart), ★ (Tech), or A (Aerial) depending on <see cref="HpCategory"/>.
    /// </summary>
    [System.Serializable]
    public sealed class UnitDefinition
    {
        public BuildingType ProducedBy;
        public string UnitName;
        public UnitHpCategory HpCategory;
        public int HitPoints;
        /// <summary>Primary power: P (Heart), ★ (Tech), or A (Aerial).</summary>
        public int CategoryPower;
        /// <summary>Secondary rating (heart / star / triangle icon strength from design notes).</summary>
        public int IconStrength;
        /// <summary>Auxiliary value from design notes (A column where applicable).</summary>
        public int Auxiliary;
        public int CostMoney;

        public string GetPowerStatSymbol()
        {
            return UnitStatLabels.GetCategoryPowerShortName(HpCategory);
        }
    }
}
