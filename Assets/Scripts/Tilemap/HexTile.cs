using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class HexTile : MonoBehaviour
{
    #region Properties and Fields
    [Header("Coordinates")]
    public int column;
    public int row;
    [HideInInspector] public HexCoordinates cubeCoords;
    
    [Header("Tile Properties")]
    public TileType tileType = TileType.Grass;
    public bool isWalkable = true;
    public int movementCost = 1;
    
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Color defaultColor = Color.white;
    public Color invalidColor = Color.red;
    public Color pathColor = new Color(0.0f, 1f, 1f, 1f); // Cyan for valid paths
    public Color movementRangeColor = new Color(0f, 1f, 0f, 1f); // Bright green for movement range
    
    // Neighbors for pathfinding
    public List<HexTile> neighbors = new List<HexTile>();
    
    // State
    public bool isPartOfPath = false;
    protected bool isValidPath = true;
    
    // References
    protected HexGridManager gridManager;
    #endregion
    
    #region Initialization
    protected virtual void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        gridManager = FindObjectOfType<HexGridManager>();
    }
    
    public virtual void Initialize(int col, int row)
    {
        this.column = col;
        this.row = row;
        
        // Create cube coordinates from offset coordinates
        this.cubeCoords = HexCoordinates.FromOffsetCoordinates(col, row);
        
        ApplyTileTypeProperties();
        
        UpdateVisuals();
    }
    
    // Apply properties based on tile type
    protected virtual void ApplyTileTypeProperties()
    {
        // Override in child classes for specific behavior
        switch (tileType)
        {
            case TileType.Wall:
                isWalkable = false;
                break;
            default:
                isWalkable = true;
                break;
        }
    }
    
    // Update visuals based on tile type
    protected virtual void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            // Only override color if not already set in prefab
            if (spriteRenderer.color == Color.white)
                spriteRenderer.color = defaultColor;
        }
    }
    #endregion
    
    #region Mouse Interactions
    // Check if the pointer is over a UI element
    protected bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
    
    protected virtual void OnMouseEnter()
    {
        // Skip if pointer is over UI
        if (IsPointerOverUI())
            return;
            
        // Skip path visualization if any unit is currently moving
        if (Unit.IsAnyUnitMoving)
            return;
        
        // Handle path visualization
        if (gridManager != null)
            gridManager.ShowPathToTile(this);
    }
    
    protected virtual void OnMouseExit()
    {
        // Skip if pointer is over UI
        if (IsPointerOverUI())
            return;
            
        // Clear tile colors
        if (gridManager != null)
            gridManager.ClearPath();
        
        if (spriteRenderer != null && !isPartOfPath)
            spriteRenderer.color = defaultColor;
    }
    
    protected virtual void OnMouseDown()
    {
        // Skip if pointer is over UI
        if (IsPointerOverUI())
            return;
            
        // Don't process clicks if any unit is moving
        if (Unit.IsAnyUnitMoving)
            return;
            
        if (Input.GetMouseButton(0)) // Left click (movement)
        {
            if (gridManager != null)
                gridManager.ExecuteMovementToTile(this);
        }

        // If there's an active player unit in target selection mode
        if (Unit.ActiveUnit is Player player && player != null)
        {
            player.Select();
        }
    }
    
    protected virtual void OnMouseOver()
    {
        // Skip if pointer is over UI
        if (IsPointerOverUI())
            return;
            
        // Handle right-click while mouse is over the tile
        if (Input.GetMouseButtonDown(1)) // Right click (place wall)
        {
            // Don't place walls while units are moving
            if (Unit.IsAnyUnitMoving)
                return;
                
            ConvertToWall();
        }
    }
    
    // Convert this tile to a wall
    protected virtual void ConvertToWall()
    {
        // Create a wall tile at this position
        if (gridManager != null)
        {
            gridManager.ReplaceWithWallTile(this);
        }
    }
    #endregion
    
    #region Visual State
    // Set this tile as part of a path
    public virtual void SetAsPathTile(bool isPath, bool isValid)
    {
        isPartOfPath = isPath;
        isValidPath = isValid;
        
        if (spriteRenderer == null)
            return;
            
        if (isPath)
            spriteRenderer.color = isValid ? pathColor : invalidColor;
        else
            spriteRenderer.color = defaultColor;
    }
    
    // Highlight this tile as within movement range
    public virtual void SetAsMovementRangeTile()
    {
        isPartOfPath = true;
        
        if (spriteRenderer == null)
            return;
        
        spriteRenderer.color = movementRangeColor;
        
        // Force the sprite renderer to update
        spriteRenderer.enabled = false;
        spriteRenderer.enabled = true;
    }
    
    // Reset tile color to default
    public virtual void ResetColor()
    {
        // Reset internal state
        isPartOfPath = false;
        
        // Force color reset
        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultColor;
            
            // Force immediate update
            spriteRenderer.enabled = false;
            spriteRenderer.enabled = true;
        }
    }
    
    // Flash a color briefly to indicate feedback
    public virtual void FlashColor(Color flashColor, float duration)
    {
        StartCoroutine(FlashColorCoroutine(flashColor, duration));
    }
    
    protected virtual IEnumerator FlashColorCoroutine(Color flashColor, float duration)
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = originalColor;
    }
    #endregion
    
    #region Tile Type Conversion
    // Convert to a different tile type
    public virtual void ConvertToType(TileType newType)
    {
        tileType = newType;
        ApplyTileTypeProperties();
        UpdateVisuals();
    }
    
    // Check if this tile is walkable
    public virtual bool IsWalkable()
    {
        return isWalkable;
    }
    #endregion

    public void SetAsTargetableTile()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.5f, 0.5f); // Red tint for targetable tiles
        }
    }

    public void SetAsInRangeTile()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f); // Blue tint for in-range tiles
        }
    }
}

// Enum for tile types
public enum TileType
{
    Grass,
    Water,
    Mountain,
    Forest,
    Desert,
    Snow,
    Wall,
    Mud
}