namespace GlobalDomination.GameData
{
    public enum CountryType
    {
        England,
        America,
        France,
        Russia
    }

    public enum BuildingType
    {
        None,
        SpecForce,
        PowerBase,
        Barraka,
        LowTech,
        DroneFactory,
        MutantLab,
        MoneyBase,
        MidTech,
        AirShipBase,
        HighTech,
        SpecialWarBase,
        ShipBase,
        MainBase,
        NuclearWeapon
    }

    /// <summary>
    /// Which HP / power system a unit uses: Heart (P), Tech (★), or Aerial (A).
    /// </summary>
    public enum UnitHpCategory
    {
        Heart,
        Tech,
        Aerial
    }
}
