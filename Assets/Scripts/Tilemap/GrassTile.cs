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
            defaultColor = new Color(1f, 1f, 1f); //  white
            spriteRenderer.color = defaultColor;
        }
    }
} 