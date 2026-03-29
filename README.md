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

## Implementation Files

### Core Data Classes

- **GameEnums.cs:** Defines CountryType and BuildingType enumerations
- **Building.cs:** Building data structure with type, name, and level
- **City.cs:** Complete city data including stats, buildings, and units
- **Player.cs:** Player data including owned cities and country selection
- **CountryDatabase.cs:** Static database of all countries and their cities
- **BuildingRollTable.cs:** Handles building generation via dice rolls

### Managers

- **GameManager.cs:** Main game controller managing players, turns, and game state
- **CountrySelectionManager.cs:** Handles country selection UI and player registration

### Helpers

- **DiceRoller.cs:** D6 rolls, sums, and physical throw profile helpers used by the dice UI
- **RuntimeUiCanvasHelper.cs:** Creates runtime overlay canvases (HUD / roll flows)

### Testing

- **GameTester.cs:** Optional harness; ensures a `UITestManager` exists and exposes `RunGameTest` / `TestBuildingRolls` for the inspector

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

// Check game state
gm.PrintGameState();
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
