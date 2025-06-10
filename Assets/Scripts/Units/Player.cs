using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    // Cache for path visualization
    public List<HexTile> highlightedTiles = new List<HexTile>();
    public Color originalColor;

    public override void Start()
    {
        // Set player's movement range to 5 before base initialization
        movementRange = 5;
        base.Start();
        RegisterWithManagers();
        
        // Store the original color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
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
        if (gameManager != null && Unit.ActiveUnit == null)
        {
            Unit.ActiveUnit = this;
        }
        
        // Register with HexGridManager
        if (gridManager != null && gridManager.playerUnit == null)
        {
            gridManager.playerUnit = this;
        }
    }
    
    public override void Select()
    {
        if (isInMoveMode && IsPlayerTurn())
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
        
        // Show UI message
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.ShowDamageMessage(amount);
        }
    }
    
    private bool IsPlayerTurn()
    {
        bool isTurn = Unit.ActiveUnit == this && gameManager.CurrentState == GameState.PlayerTurn;
            return isTurn;
    }
    
    public void ToggleMoveMode()
    {
        // Only allow toggling move mode during player's turn
        if (!IsPlayerTurn())
        {
            // Force exit move mode if it's not player's turn
            if (isInMoveMode)
            {
                isInMoveMode = false;
                HideMovementRange();
            }
            return;
        }

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
        if (currentTile == null || remainingMovementPoints <= 0 || !IsPlayerTurn())
        {
            // Force exit move mode if it's not player's turn
            if (isInMoveMode)
            {
                isInMoveMode = false;
                HideMovementRange();
            }
            return;
        }
            
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
        Debug.Log($"Player: MoveAlongPath called - Current state: {gameManager?.CurrentState}, IsPlayerTurn: {IsPlayerTurn()}");
        
        // Don't allow movement if it's not player's turn
        if (!IsPlayerTurn())
        {
            Debug.Log("Player: Not player's turn, cancelling movement");
            // Force exit move mode if it's not player's turn
            if (isInMoveMode)
            {
                isInMoveMode = false;
                HideMovementRange();
            }
            return;
        }

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
            Debug.LogError($"MoveAlongPath: Cannot move - isMoving: {isMoving}, remainingPoints: {remainingMovementPoints}, isInMoveMode: {isInMoveMode}");
            return;
        }
        
        // First, make sure we know what tile we're on
        UpdateCurrentTile();
        
        // Hide movement range
        HideMovementRange();
        
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
    }
    
    public override void ResetForNewTurn()
    {
        // Ensure movement range is 5 at the start of each turn
        movementRange = 5;
        base.ResetForNewTurn();
        
        // Force exit move mode and clear movement range
        isInMoveMode = false;
        HideMovementRange();
    }

    public override void OnTurnStart()
    {
        // Check if this is a consecutive player turn
        if (gameManager != null && gameManager.CurrentState == GameState.PlayerTurn)
        {
            // Start coroutine to handle the delay
            StartCoroutine(HandleConsecutiveTurn());
        }
        else
        {
            // Normal turn start
            base.OnTurnStart();
        }
    }
    
    public override void OnTurnEnd()
    {
        // Start coroutine to handle the delay before clearing active unit
        StartCoroutine(HandleTurnEnd());
    }
    
    private IEnumerator HandleTurnEnd()
    {
        // Wait for 1 second
        yield return new WaitForSeconds(1f);
        
        // Call base implementation to clear the turn flag and ActiveUnit
        base.OnTurnEnd();
        
        // Force exit move mode and clear movement range
        isInMoveMode = false;
        HideMovementRange();
        
        // Start the next turn
        if (gameManager != null)
        {
            gameManager.StartNextTurn();
        }
    }
    
    private IEnumerator HandleConsecutiveTurn()
    {
        // Wait for 1 second
        yield return new WaitForSeconds(1f);
        
        // Start the turn
        base.OnTurnStart();
    }
}
