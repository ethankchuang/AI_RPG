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
    public float moveSpeed = 5f;
    public float tileStopDelay = 0.1f; // Time to pause at each tile during movement
    
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
            currentTile = closestTile;
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
    
    // Get tiles within movement range using breadth-first search
    private List<HexTile> GetTilesInRange(HexTile startTile, int range)
    {
        List<HexTile> result = new List<HexTile>();
        Queue<HexTile> frontier = new Queue<HexTile>();
        Dictionary<HexTile, int> costSoFar = new Dictionary<HexTile, int>();
        
        frontier.Enqueue(startTile);
        costSoFar[startTile] = 0;
        
        while (frontier.Count > 0)
        {
            HexTile current = frontier.Dequeue();
            
            foreach (HexTile next in current.neighbors)
            {
                if (!next.isWalkable)
                    continue;
                    
                int newCost = costSoFar[current] + next.movementCost;
                
                if (newCost <= range && (!costSoFar.ContainsKey(next) || newCost < costSoFar[next]))
                {
                    costSoFar[next] = newCost;
                    frontier.Enqueue(next);
                    result.Add(next);
                }
            }
        }
        
        return result;
    }
    
    // Move along a path of tiles
    public void MoveAlongPath(List<HexTile> path)
    {
        // Validate path and state
        if (path == null || path.Count < 2 || isMoving || hasMoved)
        {
            Debug.LogWarning("Invalid movement: path is invalid or unit can't move");
            return;
        }

        // Reset ALL tiles to default color
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
        {
            tile.ResetColor();
        }
        
        // Store the path and start movement
        currentPath = new List<HexTile>(path);
        StartCoroutine(MoveAlongPathCoroutine());
    }
    
    // Coroutine to handle smooth movement
    private IEnumerator MoveAlongPathCoroutine()
    {
        // Set movement flags
        isMoving = true;
        IsAnyUnitMoving = true;
        
        // Clear path highlights
        if (gridManager != null)
            gridManager.ClearPath();
            
        // Hide movement range
        HideMovementRange();
        
        // Skip the first tile (current position)
        for (int i = 1; i < currentPath.Count; i++)
        {
            // Reset previous tile color
            if (i > 1)
                currentPath[i-1].ResetColor();
            else if (currentTile != null)
                currentTile.ResetColor();
            
            // Move smoothly to next tile
            Vector3 startPos = transform.position;
            Vector3 endPos = currentPath[i].transform.position;
            
            float distance = Vector3.Distance(startPos, endPos);
            float moveTime = distance / moveSpeed;
            float elapsed = 0f;
            
            while (elapsed < moveTime)
            {
                transform.position = Vector3.Lerp(startPos, endPos, elapsed / moveTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Snap to exact position and update current tile
            transform.position = endPos;
            currentTile = currentPath[i];
            
            // Pause at each tile
            yield return new WaitForSeconds(tileStopDelay);
        }
        
        // Movement complete
        isMoving = false;
        hasMoved = true;
        
        // Reset all path tile colors
        foreach (HexTile tile in currentPath)
            tile.ResetColor();
        
        // Update unit state
        UpdateCurrentTile();
        
        // Clear the static movement flag
        IsAnyUnitMoving = false;
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