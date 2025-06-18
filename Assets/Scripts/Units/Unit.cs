using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class Unit : MonoBehaviour
{
    // Static flags for tracking unit states
    public static bool IsAnyUnitMoving = false;
    public static Unit ActiveUnit { get; set; }
    
    // Static variable to track the last unit that completed a turn (for consecutive turn detection)
    public static Unit LastActiveUnit { get; set; }
    
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
    
    [Header("Stats")]
    public int currentHealth;
    [SerializeField] public int actionValue = 0; // Current action value (0-100)
    public int priority; // Higher number = higher priority
    
    [Header("Status Effects")]
    [SerializeField] private List<StatusEffect> activeEffects = new List<StatusEffect>();
    
    [Header("Movement")]
    public int movementRange = 3;
    [HideInInspector] public float moveSpeed = 5f;
    [HideInInspector] public float tileStopDelay = 0.1f;
    
    // Current state
    [HideInInspector] public bool hasMoved = false;
    [HideInInspector] public bool hasAttacked = false;
    [HideInInspector] public bool isSelected = false;
    [HideInInspector] public bool isMoving = false;
    [HideInInspector] protected bool isInMoveMode = false;
    [HideInInspector] public int remainingMovementPoints = 0;
    
    // Public property to access isInMoveMode
    public bool IsInMoveMode => isInMoveMode;
    
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
            //Debug.LogError($"Unit {gameObject.name} has no SpriteRenderer!");
            
        currentHealth = maxHealth;
        remainingMovementPoints = movementRange;
        
        // Find manager references
        gridManager = FindObjectOfType<HexGridManager>();
        gameManager = GameManager.Instance;
        
        // Initialize action value to 0
        actionValue = 0;
        
        // Set priority based on sibling index (position in hierarchy)
        priority = transform.GetSiblingIndex();
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
    
    // Update current tile reference based on position
    public virtual void UpdateCurrentTile()
    {
        HexTile closestTile = FindClosestTile();
        if (closestTile != null)
        {
            currentTile = closestTile;
            //Debug.Log($"Updated current tile to {currentTile.name}");
        }
        else
        {
            //Debug.LogWarning("Failed to find closest tile in UpdateCurrentTile");
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
    public virtual void Select() 
    {
        isSelected = true;
    }
    
    public virtual void Deselect() 
    {
        isSelected = false;
    }
    
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
    
    public virtual void ResetActionValue()
    {
        actionValue = 0;
    }
    
    public virtual void OnTurnStart()
    {
        // Reset turn-based flags
        hasMoved = false;
        hasAttacked = false;
        remainingMovementPoints = movementRange;
        
        // Reset movement mode
        isInMoveMode = false;
        if (currentTile != null)
            currentTile.ResetColor();
        
        // Process status effects
        ProcessStatusEffects();
            
        //Debug.Log($"Turn Start: {gameObject.name} (Speed: {speed})");
    }
    
    public virtual void OnTurnEnd()
    {
        // Clear active unit if this was the active unit
        if (ActiveUnit == this)
        {
            //Debug.Log($"Turn End: {gameObject.name} (Speed: {speed})");
            ActiveUnit = null;
            
            // Reset turn-based flags
            hasMoved = false;
            hasAttacked = false;
            remainingMovementPoints = movementRange;
            
            // Reset movement mode
            isInMoveMode = false;
            if (currentTile != null)
                currentTile.ResetColor();
        }
    }
    
    // Base move along path method (to be overridden by subclasses)
    public virtual void MoveAlongPath(List<HexTile> path) 
    {
        if (path == null || path.Count == 0)
        {
            //Debug.Log($"{gameObject.name}: No valid path to move along");
            return;
        }
        
        //Debug.Log($"{gameObject.name}: Starting movement along path of {path.Count} tiles");
        currentPath = path;
        StartCoroutine(MoveAlongPathCoroutine());
    }
    
    protected IEnumerator MoveAlongPathCoroutine()
    {
        isMoving = true;
        IsAnyUnitMoving = true;
        
        foreach (HexTile tile in currentPath)
        {
            //Debug.Log($"{gameObject.name}: Moving to tile {tile.name}");
            Vector3 targetPosition = new Vector3(tile.transform.position.x, tile.transform.position.y, transform.position.z);
            
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            
            transform.position = targetPosition;
            currentTile = tile;
            remainingMovementPoints--;
            
            yield return new WaitForSeconds(tileStopDelay);
        }
        
        isMoving = false;
        IsAnyUnitMoving = false;
        hasMoved = true;
        //Debug.Log($"{gameObject.name}: Finished movement. Remaining movement points: {remainingMovementPoints}");
    }
    
    // Reset unit for a new turn
    public virtual void ResetForNewTurn()
    {
        hasMoved = false;
        hasAttacked = false;
        remainingMovementPoints = movementRange;
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
            
        // Store the original color
        Color originalColor = spriteRenderer.color;
        
        // Flash red
        spriteRenderer.color = flashColor;
        
        yield return new WaitForSeconds(duration);
        
        // If this is a player, use their stored original color
        if (this is Player player)
        {
            spriteRenderer.color = player.originalColor;
        }
        else
        {
            spriteRenderer.color = originalColor;
        }
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
    
    #region Mouse Hover Functionality
    // Handle mouse hover to show unit information
    protected virtual void OnMouseEnter()
    {
        // Skip if pointer is over UI
        if (IsPointerOverUI())
            return;
            
        // Only show hover info for players
        if (this is Player player)
        {
            ShowPlayerHoverInfo(player);
        }
    }
    
    protected virtual void OnMouseExit()
    {
        // Skip if pointer is over UI
        if (IsPointerOverUI())
            return;
            
        // Hide hover info
        HideHoverInfo();
    }
    
    // Check if the pointer is over a UI element
    protected bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
    
    // Show hover information for a player
    protected virtual void ShowPlayerHoverInfo(Player player)
    {
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.ShowPlayerTooltip(player);
        }
    }
    
    // Hide hover information
    protected virtual void HideHoverInfo()
    {
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.HidePlayerTooltip();
        }
    }
    #endregion
    
    #region Status Effects Management
    // Apply a status effect to this unit
    public virtual void ApplyStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;
        
        // Check if effect is stackable
        if (!effect.stackable)
        {
            // Remove existing effects of the same type
            RemoveStatusEffect(effect.GetType());
        }
        
        // Clone the effect and add it
        StatusEffect newEffect = effect.Clone();
        activeEffects.Add(newEffect);
        newEffect.OnApply(this);
    }
    
    // Remove a specific status effect
    public virtual void RemoveStatusEffect(System.Type effectType)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].GetType() == effectType)
            {
                activeEffects[i].OnRemove(this);
                activeEffects.RemoveAt(i);
            }
        }
    }
    
    // Remove a specific status effect instance
    public virtual void RemoveStatusEffect(StatusEffect effect)
    {
        if (activeEffects.Contains(effect))
        {
            effect.OnRemove(this);
            activeEffects.Remove(effect);
        }
    }
    
    // Process all active status effects (called at turn start)
    private void ProcessStatusEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];
            effect.OnActionStart(this);
            
            // Remove expired effects
            if (effect.IsExpired())
            {
                activeEffects.RemoveAt(i);
            }
        }
    }
    
    // Update status effects (called every frame)
    protected virtual void UpdateStatusEffects()
    {
        foreach (StatusEffect effect in activeEffects)
        {
            effect.OnUpdate(this);
        }
    }
    
    // Get all active status effects
    public List<StatusEffect> GetActiveStatusEffects()
    {
        return new List<StatusEffect>(activeEffects);
    }
    
    // Check if unit has a specific status effect
    public bool HasStatusEffect(System.Type effectType)
    {
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect.GetType() == effectType)
                return true;
        }
        return false;
    }
    
    // Check if unit has a specific status effect by name
    public bool HasStatusEffect(string effectName)
    {
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect.effectName == effectName)
                return true;
        }
        return false;
    }
    #endregion
} 