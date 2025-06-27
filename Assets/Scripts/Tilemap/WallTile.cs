using UnityEngine;

public class WallTile : HexTile
{
    protected override void Awake()
    {
        base.Awake();
        tileType = TileType.Wall;
        
        // Ensure wall properties are set immediately
        isWalkable = false;
        movementCost = 99; // Effectively impassable
    }
    
    protected override void ApplyTileTypeProperties()
    {
        isWalkable = false;
        movementCost = 99; // Effectively impassable
        
        // Double-check that we're properly marked as a wall
        tileType = TileType.Wall;
    }
    
    protected override void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            // Set wall color to dark gray
            defaultColor = new Color(0.3f, 0.3f, 0.3f);
            spriteRenderer.color = defaultColor;
        }
    }
    
    // Override mouse interactions to prevent typical hex movement behavior
    protected override void OnMouseDown()
    {
        // Only flash feedback for left-click on walls since walls can't be moved to
        if (Input.GetMouseButton(0))
        {
            // Flash invalid color to indicate wall can't be a destination
            FlashColor(invalidColor, 0.3f);
        }
    }
    
    protected override void OnMouseOver()
    {
        // Right click on wall converts it back to grass
        if (Input.GetMouseButtonDown(1))
        {
            if (Unit.IsAnyUnitMoving)
                return;
                
            if (gridManager != null)
                gridManager.ReplaceWithGrassTile(this);
        }
    }
    
    // Override to ensure wall tiles are never considered walkable
    public override bool IsWalkable()
    {
        return false;
    }
} 