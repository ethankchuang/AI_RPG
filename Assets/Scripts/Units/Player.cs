using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    // Player-specific properties
    [HideInInspector] public bool isInMoveMode = false;
    
    // Cache for path visualization
    public List<HexTile> highlightedTiles = new List<HexTile>();

    public override void Start()
    {
        base.Start();
        RegisterWithManagers();
    }
    
    // Override this to prevent showing floating health bar for player
    // (player health is shown in the UI instead)
    /*
    protected override bool ShouldShowHealthBar()
    {
        return false;
    }
    */
    
    private void RegisterWithManagers()
    {
        // Register with GameManager
        if (gameManager != null && gameManager.selectedUnit == null)
        {
            gameManager.selectedUnit = this;
        }
        
        // Register with HexGridManager
        if (gridManager != null && gridManager.playerUnit == null)
        {
            gridManager.playerUnit = this;
        }
    }
    
    public override void Select()
    {
        if (isInMoveMode)
            ShowMovementRange();
    }
    
    public override void Deselect()
    {
        HideMovementRange();
    }
    
    public override void TakeDamage(int amount)
    {
        // Call the base class implementation first
        base.TakeDamage(amount);
        
        // Show damage amount as floating text
        Debug.Log($"Player took {amount} damage! Health: {currentHealth}/{maxHealth}");
        
        // Show UI message
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.ShowDamageMessage(amount);
        }
    }
    
    public void ToggleMoveMode()
    {
        isInMoveMode = !isInMoveMode;
        
        if (isInMoveMode && remainingMovementPoints > 0)
        {
            ShowMovementRange();
        }
        else
        {
            HideMovementRange();
        }
    }
    
    public void ShowMovementRange()
    {
        if (currentTile == null || remainingMovementPoints <= 0)
            return;
            
        List<HexTile> tilesInRange = GetTilesInRange(currentTile, remainingMovementPoints);
        
        // First reset all tiles
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
            tile.ResetColor();
            
        // Clear previously highlighted tiles
        highlightedTiles.Clear();
        
        // Get reference to grid manager for checking occupied tiles
        HexGridManager gridManager = FindObjectOfType<HexGridManager>();
            
        // Then highlight tiles in range    
        foreach (HexTile tile in tilesInRange)
        {
            // Only highlight walkable and unoccupied tiles
            bool isOccupied = false;
            
            if (gridManager != null)
            {
                isOccupied = gridManager.IsUnitOnTile(tile);
            }
            else
            {
                // Fallback to simple checking in case grid manager is null
                foreach (Unit unit in FindObjectsOfType<Unit>())
                {
                    if (unit == this) continue; // Don't check against self
                    
                    float distance = Vector2.Distance(
                        new Vector2(tile.transform.position.x, tile.transform.position.y),
                        new Vector2(unit.transform.position.x, unit.transform.position.y)
                    );
                    
                    if (distance < 0.5f)
                    {
                        isOccupied = true;
                        break;
                    }
                }
            }
            
            // Only highlight if the tile is walkable, unoccupied, and we can find a valid path to it
            if (tile.isWalkable && !isOccupied)
            {
                // Check if we can find a valid path to this tile
                List<HexTile> path = gridManager.CalculatePath(currentTile, tile);
                if (path != null && path.Count > 0 && path.Count - 1 <= remainingMovementPoints)
                {
                    tile.SetAsMovementRangeTile();
                    highlightedTiles.Add(tile);
                }
            }
        }
    }
    
    private void HideMovementRange()
    {
        foreach (HexTile tile in highlightedTiles)
        {
            if (tile != null)
                tile.ResetColor();
        }
        
        highlightedTiles.Clear();
    }
    
    public override void MoveAlongPath(List<HexTile> path)
    {
        // Clear existing path
        currentPath.Clear();
        
        // Validate path and unit state
        if (path == null || path.Count < 2)
        {
            Debug.LogError("MoveAlongPath: Invalid path (too short or null)");
            return;
        }
        
        if (isMoving || remainingMovementPoints <= 0 || !isInMoveMode)
        {
            Debug.LogError("MoveAlongPath: Unit is already moving, has no movement points left, or is not in move mode");
            return;
        }
        
        // First, make sure we know what tile we're on
        UpdateCurrentTile();
        
        // Hide movement range
        HideMovementRange();
        
        // Debug the path
        Debug.Log($"Path to follow: {path.Count} tiles");
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log($"Tile {i}: {path[i].name} at {path[i].transform.position}");
        }
        
        // Store the verified path
        currentPath = new List<HexTile>(path);
        
        // Start the movement coroutine
        StartCoroutine(MoveAlongPathCoroutine());
    }
    
    private IEnumerator MoveAlongPathCoroutine()
    {
        // Set flags
        isMoving = true;
        IsAnyUnitMoving = true;
        
        // Skip the first tile which is the start position
        for (int i = 1; i < currentPath.Count; i++)
        {
            // Get the next tile in the path
            HexTile nextTile = currentPath[i];
            Debug.Log($"Moving to tile {i}/{currentPath.Count-1}: {nextTile.name}");
            
            // Calculate positions
            Vector3 startPos = transform.position;
            Vector3 targetPos = nextTile.transform.position;
            
            // Move at a fixed speed for consistency
            float distance = Vector3.Distance(startPos, targetPos);
            float actualMoveTime = distance / moveSpeed;
            float elapsed = 0f;
            
            // Lerp to the next position
            while (elapsed < actualMoveTime)
            {
                float t = elapsed / actualMoveTime;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we arrived exactly at the destination
            transform.position = targetPos;
            
            // Update current tile
            currentTile = nextTile;
            
            // Pause at each tile for clarity
            yield return new WaitForSeconds(tileStopDelay);
        }
        
        // Clean up
        isMoving = false;
        IsAnyUnitMoving = false;
        
        // Reduce remaining movement points by the number of tiles moved
        // (excluding the starting tile)
        int tilesTraversed = currentPath.Count - 1;
        remainingMovementPoints -= tilesTraversed;
        if (remainingMovementPoints <= 0)
        {
            remainingMovementPoints = 0;
            hasMoved = true;
        }
        
        // Show updated movement range if we still have points left
        if (isInMoveMode && remainingMovementPoints > 0)
        {
            ShowMovementRange();
        }
        
        Debug.Log($"Movement complete. Remaining movement points: {remainingMovementPoints}");
    }
    
    public override void ResetForNewTurn()
    {
        base.ResetForNewTurn();
        isInMoveMode = false;
        HideMovementRange();
    }
}
