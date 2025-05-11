# Hexagon Strategy Game Framework

This project contains a basic framework for a top-down strategy game using hexagonal tiles in Unity.

## Features

- Hexagonal grid generation with proper spacing
- Turn-based gameplay system
- Unit movement and selection
- Basic UI for game state
- Simple AI framework
- Tile-based movement and path highlighting

## Setup Instructions

1. **Create the Required GameObjects:**

   - Create an empty GameObject called "GameManager" and attach the `GameManager.cs` script
   - Create an empty GameObject called "Grid" and attach the `HexGridGenerator.cs` and `HexGridManager.cs` scripts
   - Create an empty GameObject called "UI" and attach the `GameUI.cs` script

2. **Set Up the Hexagon Tile Prefab:**

   - Create a new Sprite GameObject with a hexagon sprite
   - Add a Box Collider 2D component for click detection
   - Add the `HexTile.cs` script to it
   - Add a SpriteRenderer component if not already present
   - Save this as a prefab in your project

3. **Set Up Unit Prefabs:**

   - Create two GameObjects for player and enemy units
   - Add appropriate sprites to distinguish them
   - Add the `Unit.cs` script to each
   - Save these as prefabs

4. **Configure the Game Manager:**

   - Drag the Grid GameObject to the "Grid Generator" and "Grid Manager" fields
   - Assign your player and enemy unit prefabs

5. **Configure the Grid Generator:**

   - Set the grid width and height (10x10 is a good start)
   - Set the hex radius to match your hexagon sprite
   - Assign your hex tile prefab

6. **Set Up the UI:**

   - Create buttons for ending turn and restarting
   - Create text elements for displaying game state
   - Create panels for victory and defeat states
   - Assign all UI elements to the GameUI component

## How It Works

### Hex Grid Generation

The hex grid uses a row-based system for placing tiles:
- Each tile has column (q) and row (r) coordinates
- Odd rows are offset horizontally by half a tile width
- Spacing is calculated to ensure tiles are flush with no gaps

### Unit Movement

Units can move a certain number of tiles per turn:
- Click on a unit to select it and see its movement range
- Click on a highlighted tile to move the unit
- Movement is tracked per turn (units can only move once)

### Turn System

The game uses a simple turn-based system:
- Player can select and move units during their turn
- Click "End Turn" to let the enemy play
- Enemy units will make simple moves
- Victory/defeat is determined by eliminating all enemy/player units

## Extending the Framework

### Adding Different Tile Types

Modify the `TileType` enum in `HexTile.cs` and add logic for different tile effects.

### Implementing Pathfinding

A basic framework for A* pathfinding is included. Enhance it by fully implementing the algorithm in `HexGridManager.cs`.

### Creating Real AI

The enemy AI is currently a placeholder. Enhance it by implementing proper decision-making in `GameManager.cs`.

### Adding Combat

The framework includes health and attack values, but no combat system. Implement combat by adding logic when units are adjacent.

## Known Issues

- The spacing of hexagons might need fine-tuning based on your specific sprite dimensions
- Pathfinding is not fully implemented
- No combat system implemented yet 