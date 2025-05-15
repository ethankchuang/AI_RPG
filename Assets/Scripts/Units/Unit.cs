using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    // Static flag for tracking any unit movement
    public static bool IsAnyUnitMoving = false;
    
    [Header("Unit Properties")]
    public string unitName = "Unit";
    public int movementPoints = 3;
    public int attackRange = 1;
    public int attackDamage = 1;
    public int maxHealth = 5;
    
    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float tileStopDelay = 0.2f;
    
    // Current state
    public int currentHealth;
    public bool hasMoved = false;
    public bool hasAttacked = false;
    private bool isMoving = false;
    
    // References
    private HexTile currentTile;
    private List<HexTile> currentPath = new List<HexTile>();
    private HexGridManager gridManager;
    private GameManager gameManager;
    
    private void Awake()
    {
        // Initialize components and state
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        if (spriteRenderer == null)
            Debug.LogError($"Unit {gameObject.name} has no SpriteRenderer!");
            
        currentHealth = maxHealth;
        
        // Find manager references
        gridManager = FindObjectOfType<HexGridManager>();
        gameManager = GameManager.Instance;
        
        // Auto-register if this is a player unit
        RegisterWithManagers();
    }
    
    private void Start()
    {
        UpdateCurrentTile();
        
        // Final check for player unit registration
        if (gameObject.name.Contains("Player"))
            RegisterWithManagers();
    }
    
    // Register this unit with managers if it's a player unit
    private void RegisterWithManagers()
    {
        if (!gameObject.name.Contains("Player"))
            return;
            
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
    
    // Update current tile reference based on position
    public void UpdateCurrentTile()
    {
        HexTile closestTile = FindClosestTile();
        if (closestTile != null)
        {
            currentTile = closestTile;
            Debug.Log($"Updated current tile to {currentTile.name}");
        }
        else
        {
            Debug.LogWarning("Failed to find closest tile in UpdateCurrentTile");
        }
    }
    
    // Find the closest tile to this unit
    public HexTile FindClosestTile()
    {
        HexTile closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
        {
            float distance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(tile.transform.position.x, tile.transform.position.y)
            );
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = tile;
            }
        }
        
        return closest;
    }
    
    // These are kept for GameManager compatibility but simplified
    public void Select()
    {
        ShowMovementRange();
    }
    
    public void Deselect()
    {
        HideMovementRange();
    }
    
    // Show tiles within movement range
    private void ShowMovementRange()
    {
        if (currentTile == null || hasMoved)
            return;
            
        List<HexTile> tilesInRange = GetTilesInRange(currentTile, movementPoints);
        
        // First reset all tiles
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
            tile.ResetColor();
            
        // Then highlight tiles in range    
        foreach (HexTile tile in tilesInRange)
        {
            if (tile.isWalkable)
                tile.SetAsPathTile(true, true);
        }
    }
    
    // Hide movement range
    private void HideMovementRange()
    {
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
            tile.ResetColor();
    }
    
    // Get tiles within movement range using fringe-based algorithm
    private List<HexTile> GetTilesInRange(HexTile startTile, int range)
    {
        List<HexTile> result = new List<HexTile>();
        HashSet<HexTile> visited = new HashSet<HexTile>();
        List<List<HexTile>> fringes = new List<List<HexTile>>();
        
        // Initialize with start tile
        visited.Add(startTile);
        fringes.Add(new List<HexTile> { startTile });
        
        // Process fringes for each movement point
        for (int k = 1; k <= range; k++)
        {
            fringes.Add(new List<HexTile>());
            
            foreach (HexTile hex in fringes[k-1])
            {
                foreach (HexTile neighbor in hex.neighbors)
                {
                    // Skip if already visited or not walkable (blocked by wall or other obstacle)
                    if (visited.Contains(neighbor) || !neighbor.isWalkable)
                        continue;
                        
                    visited.Add(neighbor);
                    fringes[k].Add(neighbor);
                    result.Add(neighbor);
                }
            }
        }
        
        return result;
    }
    
    // Move along a path of tiles
    public void MoveAlongPath(List<HexTile> path)
    {
        // Clear existing path
        currentPath.Clear();
        
        // Validate path and unit state
        if (path == null || path.Count < 2)
        {
            Debug.LogError("MoveAlongPath: Invalid path (too short or null)");
            return;
        }
        
        if (isMoving || hasMoved)
        {
            Debug.LogError("MoveAlongPath: Unit is already moving or has moved");
            return;
        }
        
        // First, make sure we know what tile we're on
        UpdateCurrentTile();
        
        // Reset all tiles to default color
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
        {
            tile.ResetColor();
        }
        
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
    
    // Coroutine to handle smooth movement from tile to tile
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
            
            // Highlight the destination tile
            foreach (HexTile tile in FindObjectsOfType<HexTile>())
            {
                tile.ResetColor();
            }
            nextTile.SetAsPathTile(true, true);
            
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
        hasMoved = true;
        IsAnyUnitMoving = false;
        
        // Reset all tile colors
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
        {
            tile.ResetColor();
        }
        
        Debug.Log("Movement complete");
    }
    
    // Reset unit for a new turn
    public void ResetForNewTurn()
    {
        hasMoved = false;
        hasAttacked = false;
    }
    
    // Take damage
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        
        if (currentHealth <= 0)
            Die();
    }
    
    // Die
    private void Die()
    {
        // Animation or effect could go here
        Destroy(gameObject);
    }
} 