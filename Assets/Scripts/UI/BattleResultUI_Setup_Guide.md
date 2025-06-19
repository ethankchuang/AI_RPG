# Battle Result UI Setup Guide

## Overview
This guide explains how to set up the victory/defeat screen system for your zombie game.

## Setup Steps

### 1. Create UI Panels in Unity

#### Victory Panel Setup:
1. **Create Victory Panel:**
   - Right-click in Hierarchy → UI → Panel
   - Name it "VictoryPanel"
   - Set its parent to your main Canvas

2. **Add Victory Text:**
   - Right-click VictoryPanel → UI → Text (TextMeshPro)
   - Name it "VictoryText"
   - Set text to "Victory!" (will be overridden by code)

3. **Add Victory Buttons:**
   - Right-click VictoryPanel → UI → Button (TextMeshPro)
   - Create 3 buttons named:
     - "RestartButton" (text: "Restart Battle")
     - "MainMenuButton" (text: "Main Menu") 
     - "NextLevelButton" (text: "Next Level", optional)

#### Defeat Panel Setup:
1. **Create Defeat Panel:**
   - Right-click in Hierarchy → UI → Panel
   - Name it "DefeatPanel"
   - Set its parent to your main Canvas

2. **Add Defeat Text:**
   - Right-click DefeatPanel → UI → Text (TextMeshPro)
   - Name it "DefeatText"
   - Set text to "Defeat!" (will be overridden by code)

3. **Add Defeat Buttons:**
   - Right-click DefeatPanel → UI → Button (TextMeshPro)
   - Create 2 buttons named:
     - "RestartButton" (text: "Restart Battle")
     - "MainMenuButton" (text: "Main Menu")

### 2. Add BattleResultUI Component

1. **Create BattleResultUI GameObject:**
   - Right-click in Hierarchy → Create Empty
   - Name it "BattleResultUI"
   - Add the `BattleResultUI` script component

2. **Assign References:**
   - Drag VictoryPanel to "Victory Panel" field
   - Drag DefeatPanel to "Defeat Panel" field
   - Drag VictoryText to "Victory Text" field
   - Drag DefeatText to "Defeat Text" field
   - Drag all buttons to their respective fields

### 3. Configure Panel Styling

#### Recommended Settings:
- **Panel Background:** Semi-transparent dark color (0, 0, 0, 180)
- **Text Style:** Large, bold, contrasting color
- **Button Style:** Consistent with your game's UI theme

#### Animation Settings (Optional):
- Adjust `Panel Fade In Duration` (default: 1f)
- Adjust `Text Typewriter Speed` (default: 0.05f)

### 4. Scene Setup

1. **Initially Hide Panels:**
   - Set both VictoryPanel and DefeatPanel to inactive in the Inspector
   - The script will handle showing them when needed

2. **Canvas Setup:**
   - Ensure your Canvas is set to "Screen Space - Overlay"
   - Set Canvas Scaler to "Scale With Screen Size" for responsiveness

### 5. Testing

#### Test Victory:
1. Start play mode
2. Kill all enemy units
3. Victory screen should automatically appear

#### Test Defeat:
1. Start play mode  
2. Let enemies kill all player units
3. Defeat screen should automatically appear

## Customization Options

### Text Messages:
- Modify messages in `BattleResultUI.cs`:
  ```csharp
  // In ShowVictoryScreen():
  "Victory! All enemies have been defeated!"
  
  // In ShowDefeatScreen():
  "Defeat! All your units have fallen..."
  ```

### Scene Names:
- Update the main menu scene name in `GoToMainMenu()`:
  ```csharp
  SceneManager.LoadScene("YourMainMenuSceneName");
  ```

### Additional Features:
- Add sound effects in `ShowVictoryScreen()` and `ShowDefeatScreen()`
- Add particle effects or screen shakes
- Implement statistics display (turns taken, damage dealt, etc.)
- Add scoring system

## Troubleshooting

### Victory/Defeat Screen Not Showing:
1. Check that BattleResultUI component is in the scene
2. Verify panel references are assigned correctly
3. Check console for error messages
4. Ensure panels have CanvasGroup components (added automatically by script)

### Buttons Not Working:
1. Verify button references are assigned in Inspector
2. Check that buttons have the correct scene names for loading
3. Make sure EventSystem exists in scene

### UI Layout Issues:
1. Use Canvas Scaler for multi-resolution support
2. Set proper anchoring for responsive design
3. Test on different screen resolutions

## Performance Notes

- The script uses `FindObjectOfType<>()` calls, which are acceptable for end-game states
- Consider caching references if you plan to show/hide panels multiple times
- Coroutines are used for smooth animations and won't impact performance significantly 