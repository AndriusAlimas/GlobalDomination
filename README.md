# Global Domination

Design and code documentation for a Unity 6 turn-based strategy prototype.

## What this game is about

**Global Domination** is a competitive strategy game about **controlling territory through cities** and **eliminating opponents**. Each player leads a real-world inspired nation (England, America, France, or Russia), starts with a **capital** whose economy and defenses are seeded by **dice**, then grows by **founding cities**, **constructing buildings** from a shared roll table, and eventually **fielding units** from a city **fort**.

- **Tone:** Light war / grand strategy — no single-hero narrative; the “characters” are your cities, buildings, and divisions.
- **Core loop (design intent):** Take turns improving your cities → recruit and organize **fort units** into **divisions** → threaten other players’ cities until you can **knock them out of the game** by taking their last city.
- **Win condition:** Be the last player with at least one city (everyone else has lost all cities).
- **What makes runs different:** Startup and many upgrades are **d6-driven** (population, money, defense, first building, later build rolls), so each match has different economic pressure without a separate deck-building layer.

The codebase is split between a **data/rules layer** (`Core/`, assembly **GlobalDomination.Core**) and **runtime UI** (city icons, HUD, test bootstrap in `UI/` and `Bootstrap/`). See **Project layout** below for folders.

For hands-on testing notes, see **`TESTING_GUIDE.md`**.

## Overview (systems angle)

Global Domination is implemented as a **turn-based** strategy game with a **top-down map-style HUD** (see `UITestManager` / city grid). Players compete until only one player still holds cities.

## Game Objective

**Win condition:** Destroy all enemy cities (i.e. reduce every opponent to **zero cities** — same as “last standing”).

## Core Game Systems

### 1. Country Selection

Players choose from 4 available countries:

- **England** (Capital: London)
- **America** (Capital: Washington D.C.)
- **France** (Capital: Paris)
- **Russia** (Capital: Moscow)

Each country has 12 cities defined, but only 6 are usable initially. Countries currently have no special benefits or penalties (balanced for simplicity).

### 2. City Initialization

When a player selects a country, they receive the capital city with stats determined by dice rolls:

#### City Stats (Rolled at Start)

1. **Health Points (Population):** Roll 3 dice, sum the results (3-18 range)
2. **Money:** Roll 2 dice, sum the results (2-12 range)
3. **City Power (Defense):** Roll 1 die (1-6 range)
4. **First Building:** Roll 2 dice to determine starting building (see Building System)

#### Additional City Properties

- **Buildings List:** Starts with 1 randomly rolled building
- **Upgrade Points:** Starts at 0
- **Fort:** Container for units (starts empty)
- **Owner ID:** The player who owns the city

### 3. Building System

Buildings are obtained through a two-dice roll system (6x6 grid):

#### How Building Rolls Work

1. **First Roll (1-6):** Determines the building category/row
2. **Second Roll (1-6):** Determines the specific building from that category
3. Some combinations result in "None" (empty slots)

#### Building Roll Table

| First Roll | Second Roll 1     | 2               | 3               | 4             | 5              | 6             |
| ---------- | ----------------- | --------------- | --------------- | ------------- | -------------- | ------------- |
| 1          | Barack            | Machinery       | None            | Money Builder | Farm           | Workshop      |
| 2          | Mutant Laboratory | None            | Training Ground | Hospital      | Bank           | Factory       |
| 3          | Laboratory        | Mine            | Port            | None          | Airport        | Power Plant   |
| 4          | University        | Research Center | Arsenal         | Fortress      | None           | Trading Post  |
| 5          | Spy Network       | Command Center  | Radio Station   | Bunker        | Missile Base   | None          |
| 6          | Nuclear Reactor   | None            | Space Center    | Cyber Center  | Bio Weapon Lab | Clone Factory |

**Note:** For the first building, if "None" is rolled, the system automatically re-rolls until a valid building is obtained.

### 4. Game Flow

#### Setup Phase

1. Determine number of players (2-4)
2. Each player selects a country (no duplicates allowed)
3. Each player's capital city is initialized with dice rolls
4. Each player receives their first building via dice roll

#### Gameplay

- Turn-based system
- Players take actions on their turn
- Game continues until only one player has cities remaining

## Player view, attack view, and dice flow

These are **different presentations** with the **same goal**: leave the normal map HUD, do a focused activity, then resume on the map with the same underlying game state (`GameManager`, `Player`, `City` lists).

### Player / city view (home HUD)

The **map-style HUD** is the default turn screen: current player header, flags, city icon grid ([`CitiesDisplayManager`](Assets/Scripts/UI/Hud/CitiesDisplayManager.cs)), division strip, end-turn controls, etc. [`UITestManager`](Assets/Scripts/Bootstrap/UITestManager.cs) orchestrates wiring and refresh.

### Dice view (temporary, lighter swap)

Build-city and similar rolls use a **focused dice experience**—overlay and/or isolated roll scope ([`BuildCityRollSceneScope`](Assets/Scripts/UI/CityIcon/BuildCityRollSceneScope.cs), city icon roll partials)—then hand control back to the same HUD-driven flow. The main HUD often stays in the scene or is covered by an overlay; returning is mostly closing the overlay or exiting the roll scope.

### Attack view (temporary, heavier swap)

After staging is confirmed, [`UITestManager.StagingBattle.partial.cs`](Assets/Scripts/Bootstrap/UITestManager.StagingBattle.partial.cs) **deactivates the HUD canvas**, spawns a **`StagingBattleSession`** with [`StagingBattleWorld`](Assets/Scripts/UI/Battle/StagingBattleWorld.cs) (3D camera, units, simplified defender marker). That is **world-space battle presentation**, not a small overlay on the city grid.

On battle end, `EndStagingBattleAndShowHud` re-enables the HUD and `CoRestoreHudAfterBattle` runs `UpdateDisplay()`, `DisplayCities`, and division strip refresh so you land back on the **same player view**. Because this path **hides the whole HUD** and swaps the camera stack, any restore bug shows up as a blank map or partial UI until that coroutine completes cleanly.

| Aspect | Dice-style flows | Attack / staging battle |
|--------|------------------|-------------------------|
| Main HUD | Often stays active or overlaid | Root canvas **`SetActive(false)`** during battle |
| Camera | Scoped roll / overlay camera | Other cameras disabled, battle camera; restore on exit |
| Return | Close overlay / exit roll scope | Re-enable canvas + `UpdateDisplay` / `DisplayCities` / strip refresh |

## Project layout (`Assets/Scripts`)

| Folder | Role |
|--------|------|
| **Core/GameData/** | Serializable game model: cities, players, buildings, tables |
| **Core/Managers/** | Game flow coordinators (`GameManager`, `CountrySelectionManager`) |
| **Core/Helpers/** | Shared utilities (e.g. dice math) |
| **UI/Hud/** | Map HUD: city grid, turn header, flags, division strip + attack staging flow (`PlayerDivisionsStripUI`), shared runtime canvas helper (`GlobalDomination.UI.Hud`) |
| **UI/CityIcon/** | City icon, action menus, build-city dice roll, arena audio (`GlobalDomination.UI`) |
| **UI/Battle/** | Staging assault 3D view: units, camera, optional **Resources** soldier prefabs (`Assets/Resources/Battle/`, see `README.txt` there) |
| **Bootstrap/** | `GameTester` scene helper and **`UITestManager`** (orchestrates HUD + test UI; references Core + UI) |
| **Editor/** | Asset pipeline / editor tools |

Assembly definitions: **`Core/GlobalDomination.Core.asmdef`** (everything under `Core/`), **`Editor/GlobalDomination.Editor.asmdef`** (editor-only). Remaining runtime scripts (`UI/`, `Bootstrap/`) compile in the default **Assembly-CSharp**, which references **GlobalDomination.Core** automatically.

## Implementation Files

### Core Data Classes (`Core/GameData/`)

- **GameEnums.cs:** Defines CountryType and BuildingType enumerations
- **Building.cs:** Building data structure with type, name, and level
- **City.cs:** Complete city data including stats, buildings, and units
- **Player.cs:** Player data including owned cities and country selection
- **CountryDatabase.cs:** Static database of all countries and their cities
- **BuildingRollTable.cs:** Handles building generation via dice rolls
- **FortUnitEntry.cs**, **AttackStagingSummary.cs:** Fort unit instances and UI→rules handoff for staged attacks

### Managers (`Core/Managers/`)

- **GameManager.cs:** Main game controller managing players, turns, and game state
- **CountrySelectionManager.cs:** Handles country selection UI and player registration

### Helpers

- **DiceRoller.cs** (`Core/Helpers/`): D6 rolls, sums, and physical throw profiles (`namespace GlobalDomination`)
- **RuntimeUiCanvasHelper.cs** (`UI/Hud/`): Runtime overlay canvases (HUD / roll flows)

### UI / HUD (`UI/Hud/`)

- **CitiesDisplayManager.cs:** City icon grid for the current player
- **CurrentTurnHeaderUI.cs:** Top-of-screen turn / player header
- **CountryFlagFactory.cs:** Procedural fallback flag sprites
- **RuntimeUiCanvasHelper.cs:** Shared runtime overlay canvas setup

### UI / Battle (staging assault)

- **StagingBattleWorld.cs**, **StagingBattleUnit.cs**, **StagingBattleLitMaterial.cs**, **StagingBattleDefenderAura.cs**, **StagingBattleUnitVisualResolver.cs**, **StagingBattleRtsCamera.cs**, **StagingBattlePlayerController.cs** (`UI/Battle/`): capsule fallback or **prefab** from `Resources` (per-type `Battle/Attackers/<BuildingType>`, defender variant). Default attacker visuals are attached at runtime from country/unit Resources, e.g. `Battle/Countries/England/Units/Soldier/Model/Soldier` plus `Battle/Countries/England/Units/Soldier/Animations/Idle`, so art iteration does not require prefab GUID wiring. **RTS camera**: pan **WASD** + **middle-mouse drag**, **scroll** zoom, **Q/E** orbit. Editor (`StagingBattlePrefabMenu.cs`, `StagingBattleEnglandFbxImportSetup.cs`): optional placeholder prefabs and one-click England FBX rig/idle import setup.

### UI / City icon & dice

- **CityIconUI.cs** + **CityIconUI.BuildCityRollFlow.cs** (`UI/CityIcon/`): City widgets and roll flows
- **BuildCityDiceUiFactory.cs**, **BuildCityRollSceneScope.cs** (`UI/CityIcon/`): Dice overlay UI and isolated roll scene
- **DiceArenaAudio.cs** (`UI/CityIcon/`): Arena impact audio (`DiceImpactAudio`, surface tags, Resources / procedural clips)

### Testing

- **GameTester.cs** (`Bootstrap/`): Optional harness; ensures a `UITestManager` exists and exposes `RunGameTest` / `TestBuildingRolls` for the inspector
- **UITestManager.cs** + **UITestManager.StartupReveal.partial.cs** (`Bootstrap/`): Test HUD and flow; startup stat / founded-city reveal coroutines live in the partial file

## Optional next steps (structure and tooling)

These are **not required** for the current game; they help when the codebase grows.

### Further split of `UITestManager`

`UITestManager` still orchestrates HUD wiring, startup reveal, end turn, flags, and dev-only UI. Next refinements could be a dedicated **startup reveal coordinator**, a **HUD builder**, or a **player-block factory**, leaving `UITestManager` as a thin orchestrator.

### Optional `GlobalDomination.UI` assembly

`UI/` and `Bootstrap/` still live in **Assembly-CSharp** so they can reference **`GlobalDomination.Core`** and each other without a **Core ↔ UI** circular dependency. If you want a **`GlobalDomination.UI`** asmdef, move presentation-only scripts under it and keep **`UITestManager`** (or any type that both **GameManager** and **CityIconUI** need from the “other side”) in a small third assembly or in **Assembly-CSharp**, with explicit **asmdef** references (`UI` → `Core`, etc.).

## How to Use

### Quick Test (In Unity Editor)

1. Create an empty GameObject in your scene (or use an existing `GameTester` in `SampleScene`)
2. Add the `GameTester` component if needed.
3. Press Play — a `UITestManager` is created if the scene did not already have one.
4. Use the **GameTester** component context menu or public methods in the Inspector to run `RunGameTest` or `TestBuildingRolls` and read output in the Console.

### Manual Setup

```csharp
// Get or create GameManager
GameManager gm = GameManager.Instance;

// Start a new game for 2 players
gm.StartNewGame(2);

// Add players with their country selections
gm.AddPlayer("Player 1", CountryType.England);
gm.AddPlayer("Player 2", CountryType.Russia);

// Game automatically starts when all players join
// Access current player
Player currentPlayer = gm.GetCurrentPlayer();

// Advance turns
gm.NextTurn();
```

### Rolling for Buildings

```csharp
// Roll for a random building
Building building = BuildingRollTable.RollForBuilding();

// Add to a city
city.AddBuilding(building);

// Roll for first building (never returns null)
Building firstBuilding = BuildingRollTable.RollForFirstBuilding();
```

## Future Expansion Notes

### Planned Features (mentioned by user)

- Custom country creation
- Country-specific benefits/penalties
- Additional 6 cities becoming usable per country
- Unit system for the Fort
- Combat mechanics
- Building upgrades system
- Turn actions and resource management

### Current implementation status (high level)

✅ Country selection (data + managers; flows vary by scene / test harness)  
✅ City initialization with dice rolls (population, money, defense, first building)  
✅ Building roll system (6×6 table) and adding buildings to cities  
✅ Player management, turn index, elimination / game-over check  
✅ Runtime HUD test path: city grid, headers, flags, city icon interactions (`UITestManager`, `CityIconUI`, etc.)  
✅ **Fort roster:** per-city `FortUnitEntry` list, divisions, HUD division strip, fort UI in city check panel  
✅ **Attack staging (UI prototype):** choose enemy player → place division units on a **4×6** grid; `AttackStagingSummary` + `PlayerDivisionsStripUI.AttackStagingConfirmed` for future combat hooks  
⏳ **Combat resolution** (dice, casualties, city capture) — not wired to staging yet  
⏳ **Full production** map / polish screens (much UI is still test-orchestrated)

## Next Steps for Development

1. **UI Implementation:**
   - Create country selection screen
   - Create game board view (chess-style top-down)
   - Create city detail panel
   - Create turn indicator

2. **Combat System:**
   - Define attack mechanics
   - Implement city capture
   - Add unit movement (when units are created)

3. **Turn Actions:**
   - Define actionable choices per turn
   - Resource spending system
   - Building construction interface

4. **Unit System:**
   - Define unit types
   - Unit creation/recruitment
   - Fort management UI

## Notes

- All dice rolls use standard 6-sided dice (D6)
- The system is designed to be expanded modularly
- Country data can be easily modified in CountryDatabase.cs
- Building table can be customized in BuildingRollTable.cs
- The DiceRoller class provides consistent randomization across all systems
