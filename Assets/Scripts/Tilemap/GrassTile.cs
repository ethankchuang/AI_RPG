using UnityEngine;

public class GrassTile : HexTile
{
    protected override void Awake()
    {
        base.Awake();
        tileType = TileType.Grass;
    }
    
    protected override void ApplyTileTypeProperties()
    {
        isWalkable = true;
        movementCost = 1;
    }
    
    protected override void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            defaultColor = new Color(0.2f, 0.8f, 0.2f); // Green color
            spriteRenderer.color = defaultColor;
        }
    }
} 