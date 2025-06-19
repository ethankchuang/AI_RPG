# Random Map Generator Setup Guide

## Overview
The RandomMapGenerator creates procedurally generated hex maps using your existing GrassTile and WallTile classes, with support for future tile types.

## Quick Setup

### 1. Add RandomMapGenerator to your HexGridGenerator GameObject

1. **Select your HexGridGenerator GameObject** in the scene
2. **Add Component** → Search for "RandomMapGenerator"
3. **Assign Tile Prefabs**:
   - Drag your GrassTile prefab to "Grass Tile Prefab" 
   - Drag your WallTile prefab to "Wall Tile Prefab"

### 2. Configure Generation Settings

**Map Generation Settings:**
- **Wall Density**: 0.2 = 20% walls, 0.5 = 50% walls (slider from 0 to 1)

**Generation Rules:**
- **Avoid Wall Clusters**: ✓ Prevents large solid wall areas
- **Ensure Playable Area**: ✓ Ensures units have space to move
- **Min Distance From Edge**: 1 = keeps walls away from map edges

**Debug:**
- **Generate On Start**: ✓ Automatically generates when scene starts
- **Show Debug Info**: Shows generation statistics in console

## Usage

### Automatic Generation
- Set "Generate On Start" to true
- Map generates automatically when you play the scene

### Manual Generation
- Right-click RandomMapGenerator component → "Generate Random Map"
- Or call `GenerateRandomMap()` from code

### Runtime Generation
```csharp
// Get the generator
RandomMapGenerator mapGen = FindObjectOfType<RandomMapGenerator>();

// Generate with current settings
mapGen.GenerateRandomMap();

// Or change settings and regenerate
mapGen.RegenerateWithSettings(0.3f, true, true); // 30% walls, avoid clusters, ensure playable areas
```

## How It Works

### Generation Process
1. **Clear Existing Tiles** - Removes all current tiles
2. **Fill with Grass** - Creates base grass layer
3. **Place Walls Randomly** - Adds walls based on density setting
4. **Apply Rules** - Reduces clusters and ensures playable spaces
5. **Create GameObjects** - Instantiates actual tile prefabs

### Smart Features
- **Hexagonal Neighbor Detection**: Properly handles hex grid geometry
- **Cluster Reduction**: Prevents solid wall blocks that block gameplay
- **Playable Area Guarantee**: Ensures 2x2 areas aren't completely blocked
- **Edge Protection**: Keeps walls away from map borders

## Adding New Tile Types

### Step 1: Create Tile Class
```csharp
public class YourTile : HexTile
{
    protected override void Awake()
    {
        base.Awake();
        tileType = TileType.YourType; // Add to enum first
    }
    
    protected override void ApplyTileTypeProperties()
    {
        isWalkable = true;
        movementCost = 2; // Movement cost
    }
    
    protected override void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            defaultColor = Color.red; // Your color
            spriteRenderer.color = defaultColor;
        }
    }
}
```

### Step 2: Add to TileType Enum (in HexTile.cs)
```csharp
public enum TileType
{
    Grass, Water, Mountain, Forest, Desert, Snow, Wall, Mud,
    YourType // Add here
}
```

### Step 3: Update RandomMapGenerator
1. **Add prefab field**:
```csharp
[Header("Future Tile Types")]
public GameObject yourTilePrefab;
```

2. **Add to TileType enum** (in RandomMapGenerator):
```csharp
public enum TileType
{
    Grass, Wall,
    YourType // Add here
}
```

3. **Update GetTilePrefab() method**:
```csharp
case TileType.YourType:
    return yourTilePrefab;
```

4. **Add generation logic** (in PlaceWallsRandomly or create new method):
```csharp
// Add your tile placement logic
```

## Example: Adding Mud Tiles

The included MudTile shows how to add a new tile type:

### Features:
- **2 Movement Points** - Costs double to traverse
- **Brown Color** - Visual distinction
- **Special Hover Effects** - Different interaction feedback

### To Enable Mud Tiles:
1. **Create MudTile prefab**:
   - GameObject with SpriteRenderer
   - Add MudTile component
   - Save as prefab

2. **Assign to RandomMapGenerator**:
   - Drag MudTile prefab to "Mud Tile Prefab" field

3. **Add generation logic** (modify RandomMapGenerator):
   - Uncomment Mud in TileType enum
   - Add case for Mud in GetTilePrefab()
   - Add placement logic in generation methods

## Customization Tips

### Different Map Types
- **Maze-like**: High wall density (0.4+), avoid clusters off
- **Open battlefield**: Low wall density (0.1), ensure playable areas on
- **Scattered cover**: Medium density (0.2), avoid clusters on

### Performance
- **Large maps**: Consider generating in chunks
- **Runtime generation**: Cache prefabs for faster instantiation
- **Memory**: Destroy old tiles before generating new ones (handled automatically)

## Troubleshooting

### Common Issues:
- **No tiles generate**: Check that prefab references are assigned
- **Wrong tile types**: Verify TileType enums match between classes
- **Performance drops**: Reduce map size or disable debug info
- **Clustering**: Increase "avoid wall clusters" threshold

### Debug:
- Enable "Show Debug Info" to see generation statistics
- Check console for error messages
- Use Scene view to verify tile placement

This system provides a solid foundation for procedural map generation while keeping it simple and extensible! 