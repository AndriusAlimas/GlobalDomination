# Quick Visual Setup Guide

## UI Layout (Option 2)

```
┌─────────────────────────────────────────────────────────────────────┐
│  UNITY - GLOBAL DOMINATION TEST                                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│                     Current Turn: Player 1 (England)                 │
│                           [Yellow Text, Top]                         │
│                                                                       │
├──────────────────────────┬────────────────────────────────────────────┤
│  GAME STATE              │         INSTRUCTIONS                      │
│  [Game Info Text]        │         [Instructions Text]               │
│                          │                                           │
│  Player 1 - England      │  GAME TEST CONTROLS                       │
│    London ★              │                                           │
│    HP: 12 | Money: 7     │  Keyboard:                                │
│    Power: 4              │   T - Initialize New Game                 │
│    Buildings:            │   B - Roll for Building                   │
│      • Barack            │   N - Next Turn                           │
│      • Factory           │   P - Print to Console                    │
│                          │   R - Refresh Display                     │
│  Player 2 - Russia       │   M - Test 10 Rolls                       │
│    Moscow ★              │                                           │
│    HP: 9 | Money: 10     │  UI Buttons:                              │
│    Power: 6              │   Use buttons below                       │
│    Buildings:            │                                           │
│      • Machinery         │  Goal:                                    │
│                          │   Test dice rolling system                │
│                          │                                           │
├──────────────────────────┴────────────────────────────────────────────┤
│                                                                       │
│                    [Initialize Game]                                 │
│                    [Roll for Building]                               │
│                       [Next Turn]                                    │
│                    [Test 10 Rolls]                                   │
│                   [Print to Console]                                 │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
```

## Setup Steps (2 Options)

### 🚀 OPTION 1: SUPER FAST (1 Minute)

**Best for: Quick testing, no UI needed**

1. Create Empty GameObject → Name: "GameTester"
2. Add Component: "Game Tester"
3. Press Play ▶️
4. Check Console window
5. Press keyboard: T, B, N, P

✅ **Done! Game is working!**

---

### 🎨 OPTION 2: WITH UI (10 Minutes)

**Best for: Visual feedback, easier to understand**

#### Part 1: Create Canvas (2 min)

```
Hierarchy → Right Click → UI → Canvas
  └─ Set Canvas Scaler: Scale With Screen Size (1920x1080)
```

#### Part 2: Add Text Fields (3 min)

```
Canvas → Right Click → UI → Text - TextMeshPro

Create 3 texts:
1. GameInfoText     (Left)   600x800  Size:16  Color:White
2. CurrentPlayerText (Top)    400x60   Size:24  Color:Yellow
3. InstructionsText (Right)  400x600  Size:14  Color:Gray
```

#### Part 3: Add Buttons (3 min)

```
Canvas → Right Click → UI → Button - TextMeshPro

Create 5 buttons (stack vertically at bottom):
1. "Initialize Game"     Y: -400
2. "Roll for Building"   Y: -460
3. "Next Turn"           Y: -520
4. "Test 10 Rolls"       Y: -580
5. "Print to Console"    Y: -640

All buttons: Width 200, Height 50
```

#### Part 4: Setup Manager (2 min)

```
1. Create Empty GameObject → Name: "UITestManager"
2. Add Component: "UI Test Manager"
3. Drag texts to Inspector fields:
   - Game Info Text → GameInfoText object
   - Current Player Text → CurrentPlayerText object
   - Instructions Text → InstructionsText object
4. Check "Auto Initialize Game" ✓
```

#### Part 5: Connect Buttons (2 min)

```
For each button:
1. Select button → Inspector → Button → OnClick (+)
2. Drag UITestManager object
3. Select function:
   - Initialize Game → InitializeGame()
   - Roll for Building → RollForBuilding()
   - Next Turn → NextTurn()
   - Test 10 Rolls → TestMultipleBuildingRolls()
   - Print to Console → PrintGameState()
```

✅ **Press Play! Everything should work!**

---

## What You Should See

### Console Output (Option 1 & 2)

```
=== Initializing Test Game ===

Player Player 1 selected England
London: Health Points (Population) = 12
London: Money = 7
London: City Power (Defense) = 4
Rolled [2, 1] - MutantLaboratory
London: First building = MutantLaboratory

Player Player 2 selected Russia
Moscow: Health Points (Population) = 9
Moscow: Money = 10
Moscow: City Power (Defense) = 6
Rolled [1, 2] - Machinery
Moscow: First building = Machinery
```

### On Screen (Option 2 Only)

```
Current Turn: Player 1 (England)

=== GAME STATE ===

Player 1 - England
  London ★
  HP: 12 | Money: 7 | Power: 4
  Upgrades: 0 | Units: 0
  Buildings (1):
    • Mutant Laboratory

Player 2 - Russia
  Moscow ★
  HP: 9 | Money: 10 | Power: 6
  Upgrades: 0 | Units: 0
  Buildings (1):
    • Machinery
```

---

## Common Issues & Fixes

| Problem                 | Solution                                        |
| ----------------------- | ----------------------------------------------- |
| Can't find GameTester   | Wait for Unity to compile (bottom-right)        |
| TextMeshPro not found   | Window → TextMeshPro → Import Essentials        |
| Buttons don't work      | Check OnClick events are connected              |
| Nothing on screen       | Check Canvas is set to "Screen Space - Overlay" |
| Text is black/invisible | Change text color to White                      |

---

## Test Checklist

After setup, verify these work:

**Keyboard Tests:**

- [ ] Press T → Game restarts with new dice rolls
- [ ] Press B → Adds building to current player
- [ ] Press N → Switches to other player
- [ ] Press P → Prints full details to Console
- [ ] Press M → Shows 10 building roll results

**Button Tests (Option 2):**

- [ ] All 5 buttons clickable
- [ ] Buttons do same as keyboard
- [ ] Display updates when clicking
- [ ] No errors in Console

**Dice System Tests:**

- [ ] Each restart has different HP (3-18)
- [ ] Each restart has different Money (2-12)
- [ ] Each restart has different Power (1-6)
- [ ] Buildings vary each restart
- [ ] Sometimes get "Nothing" on building rolls

---

## File Structure After Setup

```
GlobalDomination/
├── Assets/
│   ├── Scenes/
│   │   └── SampleScene.unity ← Your test scene
│   └── Scripts/
│       ├── GameData/
│       │   ├── Building.cs
│       │   ├── BuildingRollTable.cs
│       │   ├── City.cs
│       │   ├── CountryDatabase.cs
│       │   ├── GameEnums.cs
│       │   └── Player.cs
│       ├── Managers/
│       │   ├── GameManager.cs
│       │   ├── CountrySelectionManager.cs
│       │   └── UITestManager.cs ← New
│       ├── Helpers/
│       │   └── DiceRoller.cs
│       └── GameTester.cs
├── README.md
└── TESTING_GUIDE.md ← You are here
```

---

## Next: After Successful Test

1. ✅ **Verified dice rolling works**
2. ✅ **Confirmed city initialization**
3. ✅ **Tested building system**
4. ✅ **Checked turn system**

**Ready to build:**

- Game board view (chess-style grid)
- City visual representations
- Country selection screen
- Combat mechanics
- Unit system

---

## Support Commands

If you need to debug:

```csharp
// In Console (Unity)
// Check if GameManager exists
GameManager.Instance != null

// Get current player info
GameManager.Instance.GetCurrentPlayer().playerName

// Check city stats
GameManager.Instance.GetCurrentPlayer().GetCapitalCity().healthPoints
```

---

## Success! 🎉

If you can:

- ✅ See dice rolls in Console
- ✅ See different buildings appearing
- ✅ Switch between players
- ✅ Cities have different stats each game
- ✅ No red errors

**Your game systems are working perfectly!**

Time to start building the visual game board! 🎮
