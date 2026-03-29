# Global Domination - Game Systems Documentation

## Overview

Global Domination is a turn-based strategy game with a chess-style top-down view where players compete to destroy all enemy cities.

## Game Objective

**Win Condition:** Destroy all enemy cities

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

## Project layout (`Assets/Scripts`)

| Folder | Role |
|--------|------|
| **Core/GameData/** | Serializable game model: cities, players, buildings, tables |
| **Core/Managers/** | Game flow coordinators (`GameManager`, `CountrySelectionManager`) |
| **Core/Helpers/** | Shared utilities (e.g. dice math) |
| **UI/Hud/** | Map HUD: city grid, turn header, flags, shared runtime canvas helper (`GlobalDomination.UI.Hud`) |
| **UI/CityIcon/** | City icon, action menus, build-city dice roll, arena audio (`GlobalDomination.UI`) |
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

### Current Implementation Status

✅ Country selection system
✅ City initialization with dice rolls
✅ Building roll system (6x6 table)
✅ Player management
✅ Turn system foundation
✅ Game state tracking
⏳ Combat system (not implemented yet)
⏳ Unit system (structure ready, needs implementation)
⏳ Full turn actions (framework ready)
⏳ UI implementation (manager structure ready)

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
