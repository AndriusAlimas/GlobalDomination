# Testing Guide - Global Domination

## Quick Start (5 Minutes Setup)

### Option 1: Simple Console Test (No UI)

This is the fastest way to test the core game systems.

1. **Open Unity** and load your GlobalDomination project

2. **Open SampleScene** (Assets/Scenes/SampleScene.unity)

3. **Create Test Object:**
   - Right-click in Hierarchy → Create Empty
   - Name it "GameTester"
   - In Inspector, click "Add Component"
   - Search for "Game Tester" and add it

4. **Press Play** ▶️
   - The game will automatically initialize with 2 players
   - Check the **Console window** to see all the dice rolls and game state

5. **Test with Keyboard:**
   - Press **T** - Initialize new game
   - Press **B** - Roll for a building (10 rolls)
   - Press **N** - Next player turn
   - Press **P** - Print full game state

### Option 2: Visual UI Test (10 Minutes Setup)

This creates a nice visual interface to see everything on screen.

#### Step 1: Install TextMeshPro

1. In Unity, when you first use TextMeshPro, it will prompt you
2. Click "Import TMP Essentials" if prompted
3. This only needs to be done once

#### Step 2: Create UI Canvas

1. **Hierarchy** → Right-click → **UI → Canvas**
2. Select the Canvas object
3. Set Canvas Scaler to "Scale With Screen Size"
4. Set Reference Resolution to 1920 x 1080

#### Step 3: Add Background Panel

1. Right-click on **Canvas** → **UI → Panel**
2. Rename it to "GameInfoPanel"
3. Set color to dark background (RGB: 20, 20, 20, Alpha: 200)

#### Step 4: Create Text Elements

Create 3 text elements on the Canvas:

**A. Game Info Text (Main Display)**

1. Right-click Canvas → **UI → Text - TextMeshPro**
2. Name: "GameInfoText"
3. Position: Left side of screen
4. Width: 600, Height: 800
5. Font Size: 16
6. Alignment: Top-Left
7. Color: White

**B. Current Player Text**

1. Right-click Canvas → **UI → Text - TextMeshPro**
2. Name: "CurrentPlayerText"
3. Position: Top center
4. Width: 400, Height: 60
5. Font Size: 24
6. Alignment: Center
7. Color: Yellow

**C. Instructions Text**

1. Right-click Canvas → **UI → Text - TextMeshPro**
2. Name: "InstructionsText"
3. Position: Right side
4. Width: 400, Height: 600
5. Font Size: 14
6. Alignment: Top-Left
7. Color: Light Gray

#### Step 5: Create Buttons

Create 5 buttons:

**Button Layout** (Stack them vertically at the bottom)

1. **Initialize Game Button**
   - Right-click Canvas → **UI → Button - TextMeshPro**
   - Position: Bottom center (Y: -400)
   - Size: Width 200, Height 50
   - Button Text: "Initialize Game"

2. **Roll Building Button**
   - Position: Below previous (Y: -460)
   - Button Text: "Roll for Building"

3. **Next Turn Button**
   - Position: Below previous (Y: -520)
   - Button Text: "Next Turn"

4. **Test 10 Rolls Button**
   - Position: Below previous (Y: -580)
   - Button Text: "Test 10 Rolls"

5. **Print State Button**
   - Position: Below previous (Y: -640)
   - Button Text: "Print to Console"

#### Step 6: Setup UI Manager

1. **Hierarchy** → Right-click → **Create Empty**
2. Name it: "UITestManager"
3. **Add Component** → Search "UI Test Manager"
4. In the Inspector, assign the references:
   - **Game Info Text**: Drag the GameInfoText object here
   - **Current Player Text**: Drag the CurrentPlayerText object here
   - **Instructions Text**: Drag the InstructionsText object here
5. Check "Auto Initialize Game" (it's checked by default)

#### Step 7: Connect Buttons

For each button:

1. Select the button in Hierarchy
2. In Inspector, find the **Button** component
3. Under **OnClick()**, click the **+** button
4. Drag the **UITestManager** object into the object field
5. Click the dropdown → **UITestManager** → Select the function:
   - Initialize Game Button → `InitializeGame()`
   - Roll Building Button → `RollForBuilding()`
   - Next Turn Button → `NextTurn()`
   - Test 10 Rolls Button → `TestMultipleBuildingRolls()`
   - Print State Button → `PrintGameState()`

#### Step 8: Test!

1. **Press Play** ▶️
2. The game should automatically initialize and show on screen
3. Click buttons to interact
4. Also works with keyboard shortcuts!

---

## What You'll See When Testing

### Initial State (Player 1 - England)

```
London ★ (Capital)
HP: 12 (rolled 3 dice: maybe 5+4+3)
Money: 7 (rolled 2 dice: maybe 4+3)
Power: 4 (rolled 1 die: 4)
Buildings: Barack (or whatever was rolled)
```

### Initial State (Player 2 - Russia)

```
Moscow ★ (Capital)
HP: 9 (rolled 3 dice: maybe 2+3+4)
Money: 10 (rolled 2 dice: maybe 5+5)
Power: 6 (rolled 1 die: 6)
Buildings: Machinery (or whatever was rolled)
```

### When You Roll for Building

```
Console: "Rolled [3, 5] - Airport"
Building gets added to current player's capital
```

### When You Press Next Turn

Current player switches to the other player

---

## Keyboard Shortcuts (Work in Both Modes)

| Key   | Action                                              |
| ----- | --------------------------------------------------- |
| **T** | Initialize/Reset Game                               |
| **B** | Roll for 1 Building (adds to current player's city) |
| **N** | Next Turn (switch players)                          |
| **P** | Print full game state to Console                    |
| **R** | Refresh UI display                                  |
| **M** | Test 10 building rolls (see variety)                |

---

## Testing Checklist

Use this to verify everything works:

- [ ] Game initializes with 2 players
- [ ] Player 1 has England with London as capital
- [ ] Player 2 has Russia with Moscow as capital
- [ ] Each city shows different HP (3-18 range)
- [ ] Each city shows different Money (2-12 range)
- [ ] Each city shows different Power (1-6 range)
- [ ] Each city starts with 1 building
- [ ] Can roll for new buildings
- [ ] Buildings are different types
- [ ] Sometimes rolls return "Nothing" (empty slots)
- [ ] Can switch between players with Next Turn
- [ ] Current player is highlighted
- [ ] Console shows all dice rolls
- [ ] No errors in Console

---

## Troubleshooting

### "Cannot find GameTester"

- Make sure you saved the script files
- Wait for Unity to compile (check bottom-right corner)
- Try right-clicking in Project window → Reimport All

### "Cannot find UITestManager"

- Install TextMeshPro first (Window → TextMeshPro → Import TMP Essentials)
- Ensure the script is saved and compiled

### UI Text Not Showing

- Make sure you're using **Text - TextMeshPro**, not old Text
- Check that Canvas is in Screen Space - Overlay mode
- Verify text color is white/light (not black on black)

### Buttons Don't Work

- Check that UITestManager object exists in scene
- Verify button OnClick events are connected
- Make sure you dragged the UITestManager object (not the script)

### Nothing Happens When I Press Play

- Check Auto Initialize Game is checked on UITestManager
- Open Console window to see if there are any errors
- Try pressing T to manually initialize

---

## What to Test

### 1. Dice Rolling System

- Press **M** or **Test 10 Rolls** button
- Verify you see different buildings
- Verify some rolls result in "Nothing"
- Check that rolls show as [1-6, 1-6] pairs

### 2. City Initialization

- Press **T** to restart
- Notice each time you restart, cities have different stats
- HP should be between 3-18
- Money should be between 2-12
- Power should be between 1-6

### 3. Building System

- Press **B** multiple times
- Watch buildings accumulate in current player's city
- Sometimes you'll get nothing (this is correct)
- Buildings will show in the list

### 4. Turn System

- Press **N** to change turns
- Current player should change
- Both players should be able to roll buildings
- Each player's cities are independent

---

## Next Steps After Testing

Once you've verified everything works:

1. **Create the game board view** (chess-style top-down)
2. **Add visual city representations** (sprites/3D models)
3. **Create proper country selection screen** (UI for 4 countries)
4. **Implement combat system** (attack/defend mechanics)
5. **Add unit system** (create units, place in fort)
6. **Build upgrade system** (spend upgrade points)

---

## Quick Reference - Code Examples

### Manually Create a Game

```csharp
GameManager gm = GameManager.Instance;
gm.StartNewGame(2);
gm.AddPlayer("Alice", CountryType.France);
gm.AddPlayer("Bob", CountryType.America);
```

### Roll for a Building

```csharp
Building newBuilding = BuildingRollTable.RollForBuilding();
if (newBuilding != null)
{
    city.AddBuilding(newBuilding);
}
```

### Check Game State

```csharp
Player current = gameManager.GetCurrentPlayer();
City capital = current.GetCapitalCity();
Debug.Log($"HP: {capital.healthPoints}");
```

---

## Support

If you encounter issues:

1. Check the Console window for error messages
2. Verify all script files are saved
3. Make sure Unity has finished compiling (bottom-right corner)
4. Try restarting Unity if scripts don't appear
5. Check that you're using Unity 2021.3 or newer

**The system is working when you see:**

- ✅ Dice rolls in Console
- ✅ Different building types appearing
- ✅ City stats changing each game
- ✅ No red errors in Console
