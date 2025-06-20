using UnityEngine;

public class MudTile : HexTile
{
    [Header("Mud Tile Properties")]
    public Color mudColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Brown muddy color
    
    protected override void Awake()
    {
        base.Awake();
        tileType = TileType.Mud;
    }
    
    protected override void ApplyTileTypeProperties()
    {
        isWalkable = true;
        movementCost = 2; // Requires 2 movement points to traverse
    }
    
    protected override void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            defaultColor = mudColor;
            spriteRenderer.color = defaultColor;
        }
    }
    
    protected override void OnMouseEnter()
    {
        base.OnMouseEnter();
        
        // Add special visual feedback for difficult terrain
        if (spriteRenderer != null && !isPartOfPath)
        {
            Color hoverColor = Color.Lerp(mudColor, Color.yellow, 0.3f);
            spriteRenderer.color = hoverColor;
        }
    }
    
    protected override void OnMouseExit()
    {
        base.OnMouseExit();
        
        // Reset to mud color when not part of path
        if (spriteRenderer != null && !isPartOfPath)
        {
            spriteRenderer.color = mudColor;
        }
    }
} 