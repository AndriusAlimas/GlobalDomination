using System.Collections.Generic;
using UnityEngine;

namespace GlobalDomination.GameData
{
    /// <summary>
    /// When a building is added to a city, picks one of 10 flavor names for that
    /// <see cref="BuildingType"/> and the owning player's <see cref="CountryType"/>.
    /// </summary>
    public static class UnitCountryFlavorNames
    {
        private static readonly Dictionary<CountryType, Dictionary<BuildingType, string[]>> Pools =
            new Dictionary<CountryType, Dictionary<BuildingType, string[]>>();

        static UnitCountryFlavorNames()
        {
            RegisterAll();
        }

        /// <summary>
        /// Assigns a random display name from the country's pool when one exists; otherwise
        /// falls back to the default <see cref="UnitCatalog"/> unit name for recruitable buildings.
        /// Does not change <see cref="BuildingType.MainBase"/> (stays "Main Base").
        /// </summary>
        public static void AssignDisplayNameForPickup(CountryType country, Building building)
        {
            if (building == null || building.type == BuildingType.MainBase || building.type == BuildingType.None)
            {
                return;
            }

            if (TryGetPool(country, building.type, out string[] names) && names != null && names.Length > 0)
            {
                building.displayName = names[Random.Range(0, names.Length)];
                return;
            }

            UnitDefinition def = UnitCatalog.GetUnitForBuilding(building.type);
            if (def != null && !string.IsNullOrEmpty(def.UnitName))
            {
                building.displayName = def.UnitName;
            }
        }

        private static bool TryGetPool(CountryType country, BuildingType type, out string[] names)
        {
            names = null;
            if (!Pools.TryGetValue(country, out Dictionary<BuildingType, string[]> byType))
            {
                return false;
            }

            return byType != null && byType.TryGetValue(type, out names) && names != null && names.Length > 0;
        }

        private static void RegisterAll()
        {
            foreach (CountryType c in System.Enum.GetValues(typeof(CountryType)))
            {
                Pools[c] = new Dictionary<BuildingType, string[]>();
            }

            Reg(CountryType.England, BuildingType.Barraka,
                "Redcoat Squad", "Tommies", "Grenadier Guard", "Rifle Section", "Foot Guards",
                "Territorial Line", "Yeoman Platoon", "Highland Rifles", "Crown Fusiliers", "Queen's Own");
            Reg(CountryType.America, BuildingType.Barraka,
                "Dogface Squad", "Grunts", "Leathernecks", "G.I. Platoon", "Bluecoat Line",
                "Yankee Rifles", "Regular Battalion", "Frontline Team", "Rifle Squad", "Patriot Line");
            Reg(CountryType.France, BuildingType.Barraka,
                "Poilus", "Chasseurs Bleu", "Voltigeurs", "Fantassins", "Tirailleurs",
                "Garde Mobile", "Légion Detachment", "Zouave Patrol", "Poilu Section", "Bleu Horizon");
            Reg(CountryType.Russia, BuildingType.Barraka,
                "Krasnaya Gruppa", "Strelki Squad", "Pekhotnyy Otryad", "Opolcheniye", "Motostrelki",
                "Boyevaya Druzhina", "Narodnoye Opolo", "Zashchitniki", "Shturmoviki", "Frontoviki");

            Reg(CountryType.England, BuildingType.LowTech,
                "Post Riders", "Bobby Bikes", "Lane Scouts", "Swift Pedals", "Cycle Troop",
                "Spoke Patrol", "Dispatch Riders", "Iron Pedals", "Two-Wheel Line", "Courier Cycles");
            Reg(CountryType.America, BuildingType.LowTech,
                "Dust Bowl Bikes", "Route Scouts", "Highway Pedals", "County Riders", "Strip Runners",
                "Flatland Cycles", "Roadside Messengers", "Prairie Pedalers", "Main Street Bikes", "Crossroad Riders");
            Reg(CountryType.France, BuildingType.LowTech,
                "Vélo Bleu", "Messagers", "Route Légère", "Éclaireurs", "Porteurs",
                "Cyclistes Rapides", "Patrouille Pédale", "Coursiers", "Ligne Légère", "Deux-Roues");
            Reg(CountryType.Russia, BuildingType.LowTech,
                "Velogruppa", "Kuryerskiy Vzvod", "Legkiy Razved", "Dorozhnyye Razy", "Pedsotnya",
                "Bystryy Velo", "Svyaznoy Otryad", "Polk Velosipedov", "Liniya Legkaya", "Razvedvelo");

            Reg(CountryType.England, BuildingType.MutantLab,
                "Fog Crypt Lab", "Moors Strain", "London Below", "Crown Mutation", "Thames Toxin",
                "Ward 7 Project", "Greyfriars Batch", "Stonehenge Serum", "Blackpool Breed", "Soho Spore");
            Reg(CountryType.America, BuildingType.MutantLab,
                "Desert Strain", "Bunker Batch", "Area Grey", "Swamp Culture", "Radiation Row",
                "Heartland Hive", "Rust Belt Lab", "Canyon Culture", "Bayou Breed", "Prairie Plague");
            Reg(CountryType.France, BuildingType.MutantLab,
                "Catacombes X", "Marne Mutagène", "Lyon Lyse", "Normandie Nerve", "Alpes Alpha",
                "Seine Souche", "Garonne Gène", "Bordeaux Biome", "Marseille Miasme", "Paris Plasmide");
            Reg(CountryType.Russia, BuildingType.MutantLab,
                "Taiga Toxin", "Kremlin Kultura", "Sibirskiy Shtamm", "Ural Uklad", "Volga Virus",
                "Tundra Test", "Stepnoy Sputnik", "Baikal Batch", "Kavkaz Kletka", "Dvina DNK");

            Reg(CountryType.England, BuildingType.MidTech,
                "Lorry Line", "Motor Pool A", "Convoy Crown", "Haulage Guard", "Transit Tommies",
                "Diesel Division", "Cargo Column", "Motorised Section", "Road Regiment", "Fleet Freight");
            Reg(CountryType.America, BuildingType.MidTech,
                "Convoy Kings", "Interstate Haul", "Diesel Dogs", "Freight Fighters", "Rig Runners",
                "Highway Heavy", "Cargo Corps", "Motor Pool US", "Road Train", "Trailer Team");
            Reg(CountryType.France, BuildingType.MidTech,
                "Routiers Bleus", "Convoi Lourd", "Moteurs de France", "Ligne Diesel", "Transport Rapide",
                "Colonne Cargo", "Peloton Routier", "Brigade Bitume", "Escadron Lourd", "Train des Routes");
            Reg(CountryType.Russia, BuildingType.MidTech,
                "Avtokolonna", "Ural Convoy", "Tyagachi Line", "Gruzovoy Polk", "Shosseynaya Gruppa",
                "Motornaya Brigada", "Dorozhnyy Korpus", "Tyazhelyy Konvoy", "Avtopark Fronta", "Marshrutnaya");

            Reg(CountryType.England, BuildingType.SpecForce,
                "SAS Shadow", "MI6 Field", "Royal Recon", "Crown Black", "Thames Ghost",
                "Scot Yard Spec", "Highland Hunter", "Empire Edge", "Silent Section", "Night Watch");
            Reg(CountryType.America, BuildingType.SpecForce,
                "Delta Dust", "Ranger Recon", "Seals Shadow", "Green Beret Cell", "CIA Field",
                "Night Stalkers", "Spec Ops Line", "Black Team", "Ghost Platoon", "Covert Corps");
            Reg(CountryType.France, BuildingType.SpecForce,
                "GCP Ombre", "DGSE Ligne", "Commando Noir", "Légion Ombre", "Forces Silencieuses",
                "Rapace Section", "Fusiliers Fantômes", "Opération Éclipse", "Peloton Secret", "Brigade Noire");
            Reg(CountryType.Russia, BuildingType.SpecForce,
                "Spetsnaz Tuman", "Alfa Ugol", "GRU Polya", "Nochnoy Otryad", "Tenevaya Gruppa",
                "Vympel Liniya", "Specnaz Vostok", "Boyevoy Prizrak", "Skrytaya Sekta", "Otryad Shturm");

            Reg(CountryType.England, BuildingType.ShipBase,
                "HMS Convoy", "Channel Fleet", "Atlantic Line", "Portsmouth Pride", "Liverpool Leviathan",
                "Thames Tide", "Admiralty Anchor", "Crown Carrier", "North Sea Nail", "Jutland Jack");
            Reg(CountryType.America, BuildingType.ShipBase,
                "Liberty Hull", "Yankee Carrier", "Pacific Pride", "Chesapeake Line", "Brooklyn Battleship",
                "Golden Gate Fleet", "Everglades Escort", "Alamo Anchor", "Potomac Patrol", "Hudson Heavy");
            Reg(CountryType.France, BuildingType.ShipBase,
                "Marine Bleue", "Méditerranée Majeure", "Normandie Navire", "Brest Ligne", "Toulon Tonnerre",
                "Atlantique Aigle", "Côte Courage", "Flotte Rapide", "Escadre Légère", "Port d'Honneur");
            Reg(CountryType.Russia, BuildingType.ShipBase,
                "Severnyy Flot", "Chernomorskaya Liniya", "Baltiyskiy Bereg", "Tikhookeanskaya Gruppa",
                "Admiralteyskiy Otryad", "Kreysernaya Sekta", "Lodka Volna", "Morskaya Stena", "Brigada Fregat", "Eskadra Groza");

            Reg(CountryType.England, BuildingType.DroneFactory,
                "Skylark Works", "Wren Wing", "Camden Copter", "Crown UAV", "Heathrow Hive",
                "Radar Rook", "Sparrow Shop", "Kestrel Key", "Merlin Motor", "Raven Row");
            Reg(CountryType.America, BuildingType.DroneFactory,
                "Eagle Eye UAV", "Silicon Swarm", "Desert Drone", "Patriot Prop", "Blue Sky Bots",
                "Hawk Hangar", "Falcon Foundry", "Raptor Rack", "Buzzard Bay", "Sparrow Systems");
            Reg(CountryType.France, BuildingType.DroneFactory,
                "Aiglon Usine", "Faucon Fabrique", "Alouette Ligne", "Milan Motor", "Épervier Atelier",
                "UAV Rapide", "Essaim Bleu", "Drone Doré", "Vol Silencieux", "Atelier Ciel");
            Reg(CountryType.Russia, BuildingType.DroneFactory,
                "Zavod Zmei", "Orlan Liniya", "Voron Vypusk", "Sokol Sistema", "Lastochka Lekal",
                "Bespilotnik Uzel", "Nebo Fabrika", "Rotornaya Brigada", "Kopter Korpus", "Gruppa BPLA");

            Reg(CountryType.England, BuildingType.HighTech,
                "Challenger Shed", "Centurion Cell", "Crown Track", "Armour Alley", "Steel Yeoman",
                "Iron Duke Line", "Tankers Trust", "Barrel Battalion", "Track Team", "Plate Platoon");
            Reg(CountryType.America, BuildingType.HighTech,
                "Abrams Alley", "Sherman Shop", "Patton Plant", "Armor Alley", "Steel Eagle",
                "Track Titan", "Iron Legion", "Bradley Bay", "Tank Town", "Heavy Haul");
            Reg(CountryType.France, BuildingType.HighTech,
                "Leclerc Ligne", "Blindé Bleu", "Acier Rapide", "Char Courage", "Lourde Légion",
                "Chenille Cell", "Blindage Noble", "Tonnerre Track", "Marteau Motor", "Forteresse Mobile");
            Reg(CountryType.Russia, BuildingType.HighTech,
                "Tanchikovy Zavod", "Ural Track", "Korpus T-90", "Stalnaya Brigada", "Gusenichnaya Liniya",
                "Bronya Fronta", "Tyazhelyy Polk", "Krepost na Kolesakh", "Udarnaya Chast", "Tankovaya Sekta");

            Reg(CountryType.England, BuildingType.AirShipBase,
                "Spitfire Shed", "Hurricane Hangar", "RAF Row", "Typhoon Bay", "Lancaster Line",
                "Viscount Vault", "Sopwith Slot", "Jetstream Joint", "Nimrod Nest", "Harrier House");
            Reg(CountryType.America, BuildingType.AirShipBase,
                "Mustang Motor", "Eagle Hangar", "Thunderchief Bay", "Skystreak Shop", "Wildcat Wing",
                "Phantom Plant", "Tomcat Tower", "Stratofort Row", "Lightning Loft", "Freedom Flight");
            Reg(CountryType.France, BuildingType.AirShipBase,
                "Mirage Moteur", "Rafale Rack", "Étendard Hangar", "Concorde Cell", "Ciel Rapide",
                "Aile Bleue", "Vol Noble", "Escadre Légère", "Atelier Aéro", "Porte-Ailes");
            Reg(CountryType.Russia, BuildingType.AirShipBase,
                "MiG Masters", "Sukhoi Shed", "Bear Bunker", "Albatros Angar", "Strizhi Shop",
                "Nebo Udara", "Istrebitelnaya Liniya", "Shassi Krylyev", "Aviazavod Fronta", "Gruppa Perron");

            Reg(CountryType.England, BuildingType.SpecialWarBase,
                "Orbital Office", "Starlight Stack", "Crown Orbit", "Skynet Shed", "Empire Elevator",
                "Thames Tower X", "Zenith Zone", "Nimbus Node", "Stratosphere Cell", "Aether Anchor");
            Reg(CountryType.America, BuildingType.SpecialWarBase,
                "Orbital Ops", "Starforge Bay", "Skyhook Hub", "Horizon Heavy", "Exosphere Edge",
                "Launchpad Line", "Celestial Corps", "Altitude Array", "Stratos Stack", "Blue Yonder Base");
            Reg(CountryType.France, BuildingType.SpecialWarBase,
                "Plateforme Stellaire", "Nexus Noble", "Orbite Bleue", "Astre Atelier", "Ciel Stratégique",
                "Tour de l'Espace", "Ligne Zénith", "Base Élevée", "Cellule Cosmos", "Pont Céleste");
            Reg(CountryType.Russia, BuildingType.SpecialWarBase,
                "Kosmicheskaya Ploshchadka", "Orbitalnaya Brigada", "Zvezdnaya Liniya", "Stratosfernyy Uzel",
                "Vyshka Neba", "Platforma Grozy", "Uzel Vysoty", "Baza Orbita", "Sektor Kosmos", "Stantsiya Sputnik");

            Reg(CountryType.England, BuildingType.PowerBase,
                "Gridley Grid", "Turbine Trust", "Watts & Crown", "Coal & Crown", "Reactor Row",
                "Joule Junction", "Volt Vault", "Steam Stack", "Fusion Fringe", "Current Court");
            Reg(CountryType.America, BuildingType.PowerBase,
                "Grid Giant", "Tesla Town", "Watt Works", "Dynamo Depot", "Reactor Ranch",
                "Surge Station", "Volt Valley", "Amp Alley", "Current Corps", "Power Plant Prime");
            Reg(CountryType.France, BuildingType.PowerBase,
                "Centrale Bleue", "Turbine Trône", "Réseau Rapide", "Volt Noble", "Ampère Atelier",
                "Fusion Française", "Courant Couronne", "Ligne Lumen", "Noyau National", "Énergie Éclair");
            Reg(CountryType.Russia, BuildingType.PowerBase,
                "Energo Uzel", "Turbinnaya Brigada", "Set LES", "Reaktornaya Liniya", "Megavatt Masters",
                "Tokovaya Chast", "Podstantsiya Fronta", "Energeticheskiy Korpus", "Silovoy Blok", "Zavod Energii");

            Reg(CountryType.England, BuildingType.MoneyBase,
                "Bank of Crown", "Sterling House", "Threadneedle Annex", "Guinea Vault", "Sovereign Safe",
                "Exchequer Edge", "Pound Palace", "Ledger Line", "Treasury Trust", "Mint Row");
            Reg(CountryType.America, BuildingType.MoneyBase,
                "Federal Reserve Row", "Dollar Depot", "Wall Annex", "Bullion Bay", "Capital Vault",
                "Greenback Grid", "Fortune Foundry", "Trust Tower", "Cash Corridor", "Liberty Ledger");
            Reg(CountryType.France, BuildingType.MoneyBase,
                "Banque Bleue", "Franc Fort", "Trésor Rapide", "Livret Ligne", "Coffre Couronne",
                "Or National", "Compte Noble", "Bourse Bastion", "Ligne Liquidité", "Sûreté Souveraine");
            Reg(CountryType.Russia, BuildingType.MoneyBase,
                "Gosbank Uzel", "Rublevaya Liniya", "Kaznacheyskaya Chast", "Zolotoy Zapas", "Finansovyy Fort",
                "Kreditnaya Kolonna", "Schetnaya Sekta", "Kazna Fronta", "Monetnyy Dvor", "Valyutnaya Baza");

            Reg(CountryType.England, BuildingType.NuclearWeapon,
                "Trident Annex", "Aldermaston Alley", "Fission Fringe", "Warhead Ward", "Silent Key UK",
                "Plutonium Post", "Deterrent Den", "Uranium Unit", "Shield Stack", "Atom Alley");
            Reg(CountryType.America, BuildingType.NuclearWeapon,
                "Minuteman Row", "Silo Seven", "Warhead Works", "Deterrent Depot", "Trident Tower",
                "Fat Man Foundry", "Uranium Unit US", "Strategic Stack", "Nuclear Nook", "Peacekeeper Plant");
            Reg(CountryType.France, BuildingType.NuclearWeapon,
                "Force de Frappe", "Tête Nucléaire", "Dissuasion Bleue", "Silo Silencieux", "Arsenal Atomique",
                "Ligne Létale", "Clef Stratégique", "Poste Plutonium", "Bastion Bombe", "Cellule Critique");
            Reg(CountryType.Russia, BuildingType.NuclearWeapon,
                "RS-24 Line", "Topol Tower", "Yadernaya Yacheyka", "Raketnaya Rotatsiya", "Silo Sibir",
                "Boyegolovka Bunker", "Strategicheskiy Sklad", "Atomnyy Ugol", "Udar Ugrozy", "Yadernyy Uzel");
        }

        private static void Reg(CountryType country, BuildingType type, params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                return;
            }

            Pools[country][type] = names;
        }
    }
}
