using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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
    public Color highlightColor = Color.yellow;
    public Color invalidColor = Color.red;
    public Color validPathColor = new Color(0.0f, 1f, 1f, 1f);   // Bright cyan
    public Color invalidPathColor = new Color(1f, 0.0f, 0.0f, 1f); // Pure red
    
    // Neighbors for pathfinding
    public List<HexTile> neighbors = new List<HexTile>();
    
    // State
    private bool isHighlighted = false;
    private bool isPartOfPath = false;
    private bool isValidPath = true;
    
    // References
    private HexGridManager gridManager;
    #endregion
    
    #region Initialization
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        gridManager = FindObjectOfType<HexGridManager>();
        
        if (gridManager == null)
            Debug.LogError("Could not find HexGridManager!");
    }
    
    public void Initialize(int col, int row)
    {
        this.column = col;
        this.row = row;
        
        // Create cube coordinates from offset coordinates
        this.cubeCoords = HexCoordinates.FromOffsetCoordinates(col, row);
        
        if (spriteRenderer != null)
            spriteRenderer.color = defaultColor;
    }
    #endregion
    
    #region Mouse Interactions
    private void OnMouseEnter()
    {
        // Skip path visualization if any unit is currently moving
        if (Unit.IsAnyUnitMoving)
            return;
        
        isHighlighted = true;
        
        // First, reset all other tiles to default color
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
        {
            if (tile != this)
                tile.ResetColor();
        }
        
        // Handle path visualization
        if (gridManager != null)
            gridManager.ShowPathToTile(this);
        else if (spriteRenderer != null)
            spriteRenderer.color = highlightColor;
    }
    
    private void OnMouseExit()
    {
        isHighlighted = false;
        
        // Clear tile colors
        if (gridManager != null)
            gridManager.ClearPath();
        
        if (spriteRenderer != null)
            spriteRenderer.color = defaultColor;
    }
    
    private void OnMouseDown()
    {
        if (gridManager != null)
            gridManager.ExecuteMovementToTile(this);
    }
    #endregion
    
    #region Visual State
    // Set this tile as part of a path
    public void SetAsPathTile(bool isPath, bool isValid)
    {
        isPartOfPath = isPath;
        isValidPath = isValid;
        
        if (spriteRenderer == null)
            return;
            
        if (isPath)
            spriteRenderer.color = isValid ? validPathColor : invalidPathColor;
        else if (isHighlighted)
            spriteRenderer.color = highlightColor;
        else
            spriteRenderer.color = defaultColor;
    }
    
    // Reset tile color to default
    public void ResetColor()
    {
        // Reset internal state
        isPartOfPath = false;
        isHighlighted = false;
        
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
    public void FlashColor(Color flashColor, float duration)
    {
        StartCoroutine(FlashColorCoroutine(flashColor, duration));
    }
    
    private IEnumerator FlashColorCoroutine(Color flashColor, float duration)
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = originalColor;
    }
    #endregion
}

// Enum for tile types
public enum TileType
{
    Grass,
    Water,
    Mountain,
    Forest,
    Desert,
    Snow
}