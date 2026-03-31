using System.Collections.Generic;

namespace GlobalDomination.GameData
{
    /// <summary>
    /// Maps each combat building to its recruitable unit and stats (from design notes).
    /// Heart: Barraka, MutantLab, SpecForce, ShipBase — power stat is P.
    /// Tech: LowTech, MidTech, HighTech — power stat is ★ (star).
    /// Aerial: DroneFactory, AirShipBase, SpecialWarBase — power stat is A.
    /// </summary>
    public static class UnitCatalog
    {
        private static readonly Dictionary<BuildingType, UnitDefinition> ByBuilding =
            new Dictionary<BuildingType, UnitDefinition>
            {
                {
                    BuildingType.Barraka,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.Barraka,
                        UnitName = "Soldier",
                        HpCategory = UnitHpCategory.Heart,
                        HitPoints = 2,
                        CategoryPower = 1,
                        IconStrength = 1,
                        Auxiliary = 0,
                        CostMoney = 3
                    }
                },
                {
                    BuildingType.LowTech,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.LowTech,
                        UnitName = "Bicycle Rider",
                        HpCategory = UnitHpCategory.Tech,
                        HitPoints = 5,
                        CategoryPower = 1,
                        IconStrength = 2,
                        Auxiliary = 0,
                        CostMoney = 5
                    }
                },
                {
                    BuildingType.MutantLab,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.MutantLab,
                        UnitName = "Mummy",
                        HpCategory = UnitHpCategory.Heart,
                        HitPoints = 15,
                        CategoryPower = 2,
                        IconStrength = 1,
                        Auxiliary = 0,
                        CostMoney = 6
                    }
                },
                {
                    BuildingType.MidTech,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.MidTech,
                        UnitName = "Road Truck",
                        HpCategory = UnitHpCategory.Tech,
                        HitPoints = 10,
                        CategoryPower = 2,
                        IconStrength = 2,
                        Auxiliary = 1,
                        CostMoney = 8
                    }
                },
                {
                    BuildingType.SpecForce,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.SpecForce,
                        UnitName = "Spec Army",
                        HpCategory = UnitHpCategory.Heart,
                        HitPoints = 8,
                        CategoryPower = 3,
                        IconStrength = 3,
                        Auxiliary = 1,
                        CostMoney = 9
                    }
                },
                {
                    BuildingType.ShipBase,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.ShipBase,
                        UnitName = "Shu Wan Ship",
                        HpCategory = UnitHpCategory.Heart,
                        HitPoints = 25,
                        CategoryPower = 3,
                        IconStrength = 5,
                        Auxiliary = 2,
                        CostMoney = 11
                    }
                },
                {
                    BuildingType.DroneFactory,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.DroneFactory,
                        UnitName = "Drone",
                        HpCategory = UnitHpCategory.Aerial,
                        HitPoints = 3,
                        CategoryPower = 1,
                        IconStrength = 2,
                        Auxiliary = 1,
                        CostMoney = 12
                    }
                },
                {
                    BuildingType.HighTech,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.HighTech,
                        UnitName = "Tank",
                        HpCategory = UnitHpCategory.Tech,
                        HitPoints = 16,
                        CategoryPower = 8,
                        IconStrength = 4,
                        Auxiliary = 0,
                        CostMoney = 13
                    }
                },
                {
                    BuildingType.AirShipBase,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.AirShipBase,
                        UnitName = "Plane",
                        HpCategory = UnitHpCategory.Aerial,
                        HitPoints = 7,
                        CategoryPower = 2,
                        IconStrength = 2,
                        Auxiliary = 2,
                        CostMoney = 15
                    }
                },
                {
                    BuildingType.SpecialWarBase,
                    new UnitDefinition
                    {
                        ProducedBy = BuildingType.SpecialWarBase,
                        UnitName = "War Platform",
                        HpCategory = UnitHpCategory.Aerial,
                        HitPoints = 18,
                        CategoryPower = 3,
                        IconStrength = 6,
                        Auxiliary = 5,
                        CostMoney = 18
                    }
                }
            };

        /// <summary>Returns the unit this building produces, or null if none (e.g. MainBase, MoneyBase).</summary>
        public static UnitDefinition GetUnitForBuilding(BuildingType buildingType)
        {
            if (buildingType == BuildingType.None)
            {
                return null;
            }

            return ByBuilding.TryGetValue(buildingType, out UnitDefinition def) ? def : null;
        }

        public static bool HasUnit(BuildingType buildingType)
        {
            return buildingType != BuildingType.None && ByBuilding.ContainsKey(buildingType);
        }
    }
}
