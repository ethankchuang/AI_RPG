# Hexagonal Tile System

This document explains the tile system architecture and how to add new tile types.

## Architecture

The tile system uses an inheritance-based architecture:

- `HexTile`: Base class with common functionality
  - `GrassTile`: Basic walkable tile
  - `WallTile`: Unwalkable obstacle tile
  - (Future tile types can be added as needed)

## Interacting with Tiles

- **Left-click**: Move to a tile (if it's walkable)
- **Right-click**: Convert a tile to a wall, or a wall back to a grass tile

## Adding New Tile Types

To add a new tile type:

1. Add the new type to the `TileType` enum in `HexTile.cs`
2. Create a new class inheriting from `HexTile`
3. Override the following methods as needed:
   - `ApplyTileTypeProperties()`: Set movement costs and walkability
   - `UpdateVisuals()`: Set the visual appearance
   - `OnMouseDown()`: Custom click interactions
4. Add methods to `HexGridManager` to handle replacement

### Example: Creating a Water Tile

```csharp
public class WaterTile : HexTile
{
    protected override void Awake()
    {
        base.Awake();
        tileType = TileType.Water;
    }
    
    protected override void ApplyTileTypeProperties()
    {
        isWalkable = true; // Can walk on water, but at a cost
        movementCost = 3; // More expensive to move through
    }
    
    protected override void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            defaultColor = new Color(0.0f, 0.5f, 0.9f); // Blue
            spriteRenderer.color = defaultColor;
        }
    }
}
```

## Prefab Setup

Each tile type needs a prefab. You can:

1. Create specific prefabs for each tile type
2. Use the automatic prefab generation in `HexGridGenerator.SetupWallTilePrefab()` 