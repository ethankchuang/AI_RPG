using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    // Static flag for tracking any unit movement
    public static bool IsAnyUnitMoving = false;
    
    [Header("Unit Properties")]
    public int movementPoints;
    public int attackDamage;
    public int maxHealth;
    public int speed;
    
    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    
    /*
    [Header("Health Bar")]
    public bool showHealthBar = true;
    public GameObject healthBarPrefab;
    protected HealthBarController healthBar;
    */
    
    [Header("Movement")]
    [HideInInspector] public float moveSpeed = 10f;
    [HideInInspector] public float tileStopDelay = 0.2f;
    
    // Current state
    [HideInInspector] public int currentActionValue = 0;
    [HideInInspector] public int currentHealth;
    [HideInInspector] public bool hasMoved = false;
    [HideInInspector] public int remainingMovementPoints;
    [HideInInspector] public bool hasAttacked = false;
    [HideInInspector] public bool isMoving = false;
    
    // References
    protected HexTile currentTile;
    protected List<HexTile> currentPath = new List<HexTile>();
    protected HexGridManager gridManager;
    protected GameManager gameManager;
    
    // Public accessor for current tile
    public HexTile CurrentTile => currentTile;
    
    protected virtual void Awake()
    {
        // Initialize components and state
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        if (spriteRenderer == null)
            Debug.LogError($"Unit {gameObject.name} has no SpriteRenderer!");
            
        currentHealth = maxHealth;
        remainingMovementPoints = movementPoints;
        
        // Find manager references
        gridManager = FindObjectOfType<HexGridManager>();
        gameManager = GameManager.Instance;
    }
    
    public virtual void Start()
    {
        UpdateCurrentTile();
        
        // Only create health bar if this unit should show it
        // The base Unit class creates health bars by default, but Player will override this
        /*
        if (showHealthBar && healthBarPrefab != null && ShouldShowHealthBar())
        {
            CreateHealthBar();
        }
        */
    }
    
    // Virtual method to determine if this unit should show a health bar
    // By default, all units show a health bar, but Player will override this
    /*
    protected virtual bool ShouldShowHealthBar()
    {
        return true;
    }
    
    private void CreateHealthBar()
    {
        // Find the Canvas in the scene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No canvas found in scene for health bar!");
            return;
        }
        
        // Instantiate the health bar prefab
        GameObject healthBarObject = Instantiate(healthBarPrefab, canvas.transform);
        
        // Get and initialize the health bar controller
        healthBar = healthBarObject.GetComponent<HealthBarController>();
        if (healthBar != null)
        {
            healthBar.Initialize(this);
        }
        else
        {
            Debug.LogWarning($"Health bar prefab for {gameObject.name} does not have a HealthBarController component!");
            Destroy(healthBarObject);
        }
    }
    */
    
    // Update current tile reference based on position
    public virtual void UpdateCurrentTile()
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
    
    // Base select/deselect methods (to be overridden by subclasses)
    public virtual void Select() { }
    
    public virtual void Deselect() { }
    
    // Get tiles within movement range using fringe-based algorithm
    protected List<HexTile> GetTilesInRange(HexTile startTile, int range)
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
    
    // Base move along path method (to be overridden by subclasses)
    public virtual void MoveAlongPath(List<HexTile> path) { }
    
    // Reset unit for a new turn
    public virtual void ResetForNewTurn()
    {
        hasMoved = false;
        hasAttacked = false;
        remainingMovementPoints = movementPoints;
    }
    
    // Take damage
    public virtual void TakeDamage(int amount)
    {
        currentHealth -= amount;
        
        // Visual feedback
        StartCoroutine(FlashSprite(Color.red, 0.2f));
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }
    
    // Visual feedback for damage
    protected IEnumerator FlashSprite(Color flashColor, float duration)
    {
        if (spriteRenderer == null)
            yield break;
            
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = flashColor;
        
        yield return new WaitForSeconds(duration);
        
        spriteRenderer.color = originalColor;
    }
    
    // Heal the unit
    public virtual void Heal(int amount)
    {
        currentHealth += amount;
        
        // Cap at max health
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
            
        // Visual feedback
        StartCoroutine(FlashSprite(Color.green, 0.2f));
    }
    
    // Die
    protected virtual void Die()
    {
        /*
        // Clean up health bar
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }
        */
        
        // Animation or effect could go here
        Destroy(gameObject);
    }
} 