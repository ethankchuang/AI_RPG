# Linear Story System Implementation Guide

## Overview

This hybrid story system provides a structured narrative progression that follows predefined story beats. The story starts with meeting teammates at a bar and progresses through various biomes with a mix of choice-based decisions and simple "Next" button progression. The system consists of three main phases:

1. **Combat** - Tactical turn-based battles
2. **Campfire** - Companion interactions and resource management
3. **Transition** - Story progression with choices or "Next" button

## Core Components

### 1. StoryManager
- **Location**: `Assets/Scripts/StorySystem/StoryManager.cs`
- **Purpose**: Manages the overall story progression, companion relationships, and player state
- **Key Features**:
  - Predefined story beats for each location (Town → Wilderness → Desert → Mountains → Destroyed Town → Portal)
  - Companion relationship system (-100 to 100 scale)
  - Action point management for campfire activities
  - Inventory and status effect tracking

### 2. LinearStoryUI
- **Location**: `Assets/Scripts/StorySystem/LinearStoryUI.cs`
- **Purpose**: Handles the UI for story transitions with hybrid choice/next system
- **Key Features**:
  - Displays story beats with typewriter effect
  - Shows choice buttons for important decisions
  - Shows "Next" button for linear story progression
  - Handles transitions between phases
  - Automatically detects whether to show choices or "Next" button

### 3. CampfireManager
- **Location**: `Assets/Scripts/StorySystem/CampfireManager.cs`
- **Purpose**: Manages the campfire phase where players interact with companions and manage resources
- **Key Features**:
  - Action point system (3 points per campfire)
  - Companion interaction options
  - Exploration, training, and crafting activities
  - Relationship building mechanics

## Story Structure

### Hybrid Progression
The story follows this predefined path with a mix of choice-based decisions and linear progression:

1. **Town**
   - Bar scene with teammates (Next)
   - Campfire
   - Bar fight (combat 1)
   - King's quest assignment from mysterious figure (Next)
   - Campfire
   - recieve quest from king (next)
   - witness monsters attacking civilians (choice)
      - defend civilians (fight)
      or
      - ignore (next)
   - Store visit (Choice)
   - Bandit ambush (combat)
   - Campfire

2. **Wilderness**
   - see travelers attacked by monsters(choice)
      - help them (combat)
      - Traveler reward (Next)
      or
      - ignore travelers (next)
   - Orc ambush (combat)
   - mysterious figure gives clue about orc camp (next)
   - Campfire
   - Orc camp diplomacy attempt (Choices)
      - not really a choice, always results in battle
      - choice effect companion relation
   - Orc camp battle (combat)
   - Campfire

3. **Desert**
   - Sandstorm encounter (Next)
   - Giant scorpion battle (combat)
   - Campfire
   - Oasis resource sharing (choice)
      - choice effect companion relation
   - Wolf pack attack (combat)
   - Campfire
   - Ancient temple teamwork (choice) 
      - make it a next for now (will be implemented in future)
   - Sand elemental (combat)
   - Campfire

4. **Mountains**
   - Troll encounter (choice)
      - convince them to let you pass
      - fight
   - Campfire
   - Trapped passage teamwork (choice)
      - make it a next for now (will be implemented in future)
   - Dual guards battle (combat)
   - Campfire
   - Artifact room sacrifice choice (Choices)
      - sacrifice teammate for artifact
   - Dragon guardian (combat)
   - Campfire

5. **Destroyed Town**
   - Return to ruins (Next)
   - Mysterious stranger battle (combat)
   - Campfire

6. **Portal**
   - Final battle with Dark Magician (combat)

## Implementation Steps

### Step 1: Set Up the Story System

1. **Create StoryManager GameObject**:
   ```
   - Create empty GameObject named "StoryManager"
   - Add StoryManager component
   - Set as DontDestroyOnLoad
   ```

2. **Create LinearStoryUI GameObject**:
   ```
   - Create empty GameObject named "LinearStoryUI"
   - Add LinearStoryUI component
   - Set up UI references (title, description, choice buttons, etc.)
   ```

3. **Create CampfireManager GameObject**:
   ```
   - Create empty GameObject named "CampfireManager"
   - Add CampfireManager component
   - Set up UI references for campfire actions
   ```

### Step 2: UI Setup

#### LinearStoryUI Requirements:
- `titleText` (TextMeshProUGUI) - Story beat title
- `descriptionText` (TextMeshProUGUI) - Story description
- `choiceButtonContainer` (Transform) - Parent for choice/next buttons
- `choiceButtonPrefab` (Button) - Template for choice/next buttons
- `continueButton` (Button) - Continue to next beat (hidden in hybrid mode)
- `combatButton` (Button) - Start combat
- `locationText` (TextMeshProUGUI) - Current location display
- `phaseText` (TextMeshProUGUI) - Current phase display

#### CampfireManager Requirements:
- `campfireUI` (GameObject) - Main campfire UI panel
- `actionButtonContainer` (Transform) - Parent for action buttons
- `actionButtonPrefab` (Button) - Template for action buttons
- `actionPointsText` (TextMeshProUGUI) - Action points display
- `locationText` (TextMeshProUGUI) - Location display
- `descriptionText` (TextMeshProUGUI) - Action result display
- `continueButton` (Button) - Continue to next story beat

### Step 3: Scene Integration

1. **Add StorySceneSetup to your main scene**:
   ```
   - Create empty GameObject named "StorySceneSetup"
   - Add StorySceneSetup component
   - Assign prefabs for StoryManager, LinearStoryUI, and CampfireManager
   ```

2. **Update scene names**:
   - Ensure your battle scene is named "Battle"
   - Ensure your story scene is named "Chat" (or update the names in the scripts)

### Step 4: Combat Integration

The existing combat system remains largely unchanged. The integration points are:

1. **Victory handling**: Modified `GameManager.HandleVictory()` to return to story
2. **Story transitions**: `LinearStoryUI.OnCombatClicked()` loads battle scene
3. **Return from combat**: `LinearStoryUI.OnReturnFromCombat()` continues story

## Hybrid Story Features

### Choice-Based Decisions:
- **Important Story Moments**: Key decisions that affect companion relationships
- **Strategic Choices**: Options that impact gameplay and story progression
- **Character Development**: Choices that build or strain companion bonds

### Linear Progression:
- **Story Narration**: Simple "Next" button for story exposition
- **Team Bonding**: Campfire scenes that emphasize companion relationships
- **Combat Preparation**: Linear progression through combat scenarios

### Automatic Detection:
- **Smart UI**: Automatically shows choice buttons or "Next" button based on story beat
- **Seamless Transitions**: Smooth movement between choice-based and linear story elements
- **Consistent Experience**: Maintains story flow regardless of interaction type

## Action Point System

### Campfire Actions (3 points per campfire):
- **Talk to Companion**: 1 AP
- **Explore Area**: 1 AP
- **Individual Training**: 1 AP
- **Rest**: 1 AP
- **Trade with Locals**: 1 AP
- **Scout Ahead**: 2 AP
- **Train Together**: 2 AP
- **Craft Items**: 2 AP

### Action Results:
- **Exploration**: 30% chance to find items
- **Training**: 20% chance for status effects
- **Rest**: 50% chance for positive status effects
- **Crafting**: Always produces useful items

## Customization

### Adding New Story Beats:
1. Modify `StoryManager.CreateStoryBeats()` method
2. Add new `StoryBeat` objects with appropriate choices and consequences
3. Update `ApplyChoiceConsequences()` to handle new choice effects

### Adding New Campfire Actions:
1. Modify `CampfireManager.CreateDefaultActions()` method
2. Add new `CampfireAction` objects
3. Implement corresponding action in `PerformAction()` method

### Modifying Companion Relationships:
1. Update `StoryManager.ApplyChoiceConsequences()` method
2. Add new relationship modifiers based on story choices
3. Customize relationship thresholds in `UpdateRelationshipStatus()`

## Testing the System

1. **Start the game** in the story scene
2. **Follow the linear progression** through story beats
3. **Test combat transitions** by clicking combat buttons
4. **Test campfire interactions** by spending action points
5. **Verify relationship changes** through companion status displays

## Troubleshooting

### Common Issues:
1. **StoryManager not found**: Ensure StoryManager is created and set as DontDestroyOnLoad
2. **UI not updating**: Check that all UI references are properly assigned
3. **Combat not transitioning**: Verify scene names match the script configuration
4. **Relationships not changing**: Check that choice consequences are properly implemented

### Debug Information:
- All story events are logged to the console
- Relationship changes are displayed in real-time
- Action point spending is tracked and displayed

## Future Enhancements

### Potential Additions:
1. **Save/Load system** for story progress
2. **Multiple story branches** based on choices
3. **Companion-specific story beats**
4. **Dynamic difficulty scaling** based on relationships
5. **Achievement system** for story milestones
6. **Sound effects and music** for different phases
7. **Particle effects** for transitions and actions

This system provides a solid foundation for your linear story-driven RPG while maintaining the existing combat mechanics and adding meaningful companion interactions. 