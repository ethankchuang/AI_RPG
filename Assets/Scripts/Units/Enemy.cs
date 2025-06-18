using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Unit
{
    [Header("Enemy Data")]
    public EnemyData enemyData;
    
    [Header("AI Settings")]
    private Coroutine movementCoroutine;
    private float attackDelay = 0.5f;
    private EnemyHealthDisplay healthDisplay;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    public override void Start()
    {
        // Apply enemy data if available
        if (enemyData != null)
        {
            ApplyEnemyData();
        }
        else
        {
            // Set default values if no enemy data
            maxHealth = 20;
            attackDamage = 5;
            speed = 15;
            movementRange = 3;
        }
        
        base.Start();
        
        // Ensure health is properly initialized
        if (currentHealth <= 0 && maxHealth > 0)
            currentHealth = maxHealth;
        else if (maxHealth <= 0)
            maxHealth = 20; // Default health if not set
            
        if (currentHealth <= 0)
            currentHealth = maxHealth;
            
        CreateHealthDisplay();
    }
    
    private void CreateHealthDisplay()
    {
        // Add the health display component
        healthDisplay = gameObject.AddComponent<EnemyHealthDisplay>();
        healthDisplay.Initialize(this);
    }
    
    protected override void Die()
    {
        // Clean up health display
        if (healthDisplay != null)
        {
            Destroy(healthDisplay);
        }
        
        // Call base die method
        base.Die();
    }
    
    // Explicitly ensure that enemies always show health bars
    /*
    protected override bool ShouldShowHealthBar()
    {
        return true;
    }
    */
    
    // Called at the start of enemy turn
    
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
    }
    
    // Execute AI turn logic
    public void ExecuteTurn()
    {
        // Update the last active unit tracker for consecutive turn detection
        LastActiveUnit = this;
        
        // Reset movement state
        hasMoved = false;
        hasAttacked = false;
        
        // Start the coroutine and store the reference so we can track if it's done
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        movementCoroutine = StartCoroutine(ExecuteTurnCoroutine());
    }
    
    // Check if this enemy has finished its turn
    public bool IsTurnComplete()
    {
        return hasMoved && hasAttacked;
    }
    
    // Coroutine to handle enemy turn execution
    private IEnumerator ExecuteTurnCoroutine()
    {
        // Wait a moment before acting
        yield return new WaitForSeconds(0.5f);
        
        // Get our current tile
        UpdateCurrentTile();
        
        if (currentTile == null)
        {
            hasMoved = true;
            hasAttacked = true;
            movementCoroutine = null;
            yield break;
        }
        
        // First check if we can attack any player (prioritize attacking)
        Player attackablePlayer = FindAttackablePlayer();
        if (attackablePlayer != null)
        {
            // Attack and end turn
            yield return StartCoroutine(AttackPlayer(attackablePlayer));
            hasMoved = true;
            hasAttacked = true;
        }
        else
        {
            // No attack available, find target using aggro-based targeting
            Player targetPlayer = FindPlayerByAggro();
            if (targetPlayer == null)
            {
                hasMoved = true;
                hasAttacked = true;
                movementCoroutine = null;
                yield break;
            }
            
            // Move towards the aggro-selected target
            yield return StartCoroutine(MoveTowardsPlayer(targetPlayer));
            hasMoved = true;
            
            // After moving, check again if we can now attack any player
            attackablePlayer = FindAttackablePlayer();
            if (attackablePlayer != null)
            {
                yield return StartCoroutine(AttackPlayer(attackablePlayer));
            }
            hasAttacked = true;
        }
        
        movementCoroutine = null;
    }
    
    // Find the best target player using aggro-based targeting
    private Player FindPlayerByAggro()
    {
        // Get all alive players
        Player[] allPlayers = Object.FindObjectsOfType<Player>();
        List<Player> alivePlayers = new List<Player>();
        
        foreach (Player player in allPlayers)
        {
            if (player.currentHealth > 0)
            {
                alivePlayers.Add(player);
            }
        }
        
        if (alivePlayers.Count == 0) return null;
        if (alivePlayers.Count == 1) return alivePlayers[0];
        
        // Use weighted random selection based on aggro values
        return SelectPlayerByAggro(alivePlayers);
    }
    
    // Select a player based on their aggro values using weighted random selection
    private Player SelectPlayerByAggro(List<Player> players)
    {
        // Calculate weights based on aggro values
        List<float> weights = new List<float>();
        float totalWeight = 0f;
        
        foreach (Player player in players)
        {
            // Use aggro value as weight (higher aggro = more likely to be targeted)
            float weight = player.aggroValue;
            weights.Add(weight);
            totalWeight += weight;
        }
        
        // Generate random number between 0 and total weight
        float randomValue = Random.Range(0f, totalWeight);
        
        // Find which player the random value lands on
        float currentWeight = 0f;
        for (int i = 0; i < players.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return players[i];
            }
        }
        
        // Fallback to last player (shouldn't happen)
        return players[players.Count - 1];
    }

    // Find any player that can be attacked (adjacent) - uses aggro for selection if multiple
    private Player FindAttackablePlayer()
    {
        Player[] allPlayers = Object.FindObjectsOfType<Player>();
        List<Player> attackablePlayers = new List<Player>();
        
        foreach (Player player in allPlayers)
        {
            if (player.currentHealth <= 0) continue; // Skip dead players
            
            player.UpdateCurrentTile();
            if (player.CurrentTile != null && IsAdjacentToTile(player.CurrentTile))
            {
                attackablePlayers.Add(player);
            }
        }
        
        if (attackablePlayers.Count == 0) return null;
        if (attackablePlayers.Count == 1) return attackablePlayers[0];
        
        // Multiple players can be attacked - use aggro to choose
        return SelectPlayerByAggro(attackablePlayers);
    }
    
    // Check if this enemy is adjacent to a specific tile
    private bool IsAdjacentToTile(HexTile tile)
    {
        if (currentTile == null || tile == null)
            return false;
            
        // If the tiles are the same, they're not adjacent
        if (currentTile == tile)
            return false;
            
        // Check if the tile is in our neighbors list
        return currentTile.neighbors != null && currentTile.neighbors.Contains(tile);
    }
    
    // Move towards the player
    private IEnumerator MoveTowardsPlayer(Player player)
    {
        if (currentTile == null || player.CurrentTile == null || remainingMovementPoints <= 0)
        {
            //Debug.Log($"{gameObject.name}: Cannot move - MP: {remainingMovementPoints}");
            yield break;
        }
            
        // Find a path to the player using the same pathfinding as player movement
        HexGridManager gridManager = FindObjectOfType<HexGridManager>();
        if (gridManager == null)
        {
            yield break;
        }
            
        // Get path using the same algorithm as player movement
        List<HexTile> path = CalculatePathToTarget(currentTile, player.CurrentTile);
        
        // If no path or just starting tile, we can't move
        if (path.Count <= 1)
        {
            //Debug.Log($"{gameObject.name}: No valid path found");
            yield break;
        }
        
        // Check each tile in the path to make sure they're not occupied
        List<HexTile> validPath = new List<HexTile>();
        validPath.Add(path[0]); // Always add the starting tile
        
        for (int i = 1; i < path.Count - 1; i++) // Skip the last tile (player's tile)
        {
            // Only add unoccupied tiles to the valid path
            if (!gridManager.IsUnitOnTile(path[i]))
            {
                validPath.Add(path[i]);
            }
            else
            {
                break;
            }
        }
        
        // If the valid path is just the starting tile, we can't move
        if (validPath.Count <= 1)
        {
            //Debug.Log($"{gameObject.name}: No valid unoccupied tiles in path");
            yield break;
        }
            
        // Limit path by our movement points
        int maxMoveSteps = Mathf.Min(validPath.Count - 1, remainingMovementPoints);
        List<HexTile> limitedPath = validPath.GetRange(0, maxMoveSteps + 1); // +1 because we include starting tile
        
        //Debug.Log($"{gameObject.name}: Moving along path of length {limitedPath.Count}");
        // Move along the path
        yield return StartCoroutine(MoveAlongPathCoroutine(limitedPath));
    }
    
    // Attack the player
    private IEnumerator AttackPlayer(Player player)
    {
        // Animation delay
        yield return new WaitForSeconds(attackDelay);
        
        // Flash red to indicate attack
        if (spriteRenderer != null)
            StartCoroutine(FlashSprite(Color.red, 0.2f));
            
        // Deal damage to player
        player.TakeDamage(attackDamage);
        
        // Mark that we've attacked
        hasAttacked = true;
    }
    
    // Calculate a path to the target tile using BFS with distance-based sorting
    private List<HexTile> CalculatePathToTarget(HexTile start, HexTile end)
    {
        if (start == null || end == null)
            return new List<HexTile>();
            
        // Get grid manager for unit detection
        HexGridManager gridManager = FindObjectOfType<HexGridManager>();
        
        // Use a dictionary to track distance to each tile from start
        Dictionary<HexTile, int> distance = new Dictionary<HexTile, int>();
        Dictionary<HexTile, HexTile> previous = new Dictionary<HexTile, HexTile>();
        
        // Priority queue to process closest tiles to player first
        List<HexTile> toProcess = new List<HexTile>();
        HashSet<HexTile> processed = new HashSet<HexTile>();
        
        // Initialize with start tile
        distance[start] = 0;
        previous[start] = null;
        toProcess.Add(start);
        
        bool endFound = false;
        
        // Keep exploring until we find the end or explore all reachable tiles
        while (toProcess.Count > 0 && !endFound)
        {
            // Sort by distance to player (not distance from start)
            toProcess.Sort((a, b) => HexDistance(a, end).CompareTo(HexDistance(b, end)));
            
            // Get the tile closest to player
            HexTile current = toProcess[0];
            toProcess.RemoveAt(0);
            processed.Add(current);
            
            // Check if we've reached the end
            if (current == end)
            {
                endFound = true;
                break;
            }
            
            // Check each neighbor of the current tile
            if (current.neighbors == null)
                continue;
                
            foreach (HexTile neighbor in current.neighbors)
            {
                if (neighbor == null || processed.Contains(neighbor))
                    continue;
                    
                // Skip unwalkable tiles
                if (!neighbor.isWalkable)
                    continue;
                
                // Skip occupied tiles (except the destination)
                if (neighbor != end)
                {
                    bool isOccupied = gridManager != null ? 
                        gridManager.IsUnitOnTile(neighbor) : 
                        IsUnitOnTileFallback(neighbor);
                    
                    if (isOccupied)
                        continue;
                }
                
                // Calculate new distance - each step costs 1
                int newDist = distance[current] + 1;
                
                // If we haven't visited this tile or found a shorter path
                if (!distance.ContainsKey(neighbor) || newDist < distance[neighbor])
                {
                    // Update distance
                    distance[neighbor] = newDist;
                    previous[neighbor] = current;
                    
                    // Add to processing queue if not already there
                    if (!toProcess.Contains(neighbor))
                        toProcess.Add(neighbor);
                }
            }
        }
        
        // If we didn't find the end tile but have explored neighbors, find the one closest to target
        if (!endFound && processed.Count > 1)
        {
            HexTile bestTile = null;
            float bestDistance = float.MaxValue;
            
            foreach (HexTile tile in processed)
            {
                if (tile == start) continue;
                
                float dist = HexDistance(tile, end);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestTile = tile;
                }
            }
            
            if (bestTile != null)
            {
                end = bestTile;
                endFound = true;
            }
        }
        
        // If path still not found, return just the start
        if (!endFound || !previous.ContainsKey(end))
        {
            List<HexTile> fallbackPath = new List<HexTile>();
            fallbackPath.Add(start);
            return fallbackPath;
        }
        
        // Reconstruct the path
        List<HexTile> path = new List<HexTile>();
        HexTile currentTile = end;
        
        // Build the path backwards from end to start
        while (currentTile != null)
        {
            path.Add(currentTile);
            currentTile = previous.ContainsKey(currentTile) ? previous[currentTile] : null;
        }
        
        // Reverse to get start to end
        path.Reverse();
        
        return path;
    }
    
    // Calculate hex distance between two tiles (for pathfinding heuristic)
    private float HexDistance(HexTile a, HexTile b)
    {
        if (a == null || b == null)
            return float.MaxValue;
            
        // Use cube coordinates for accurate hex distance
        int q1 = a.cubeCoords.q;
        int r1 = a.cubeCoords.r;
        int s1 = a.cubeCoords.s;
        
        int q2 = b.cubeCoords.q;
        int r2 = b.cubeCoords.r;
        int s2 = b.cubeCoords.s;
        
        return (Mathf.Abs(q1 - q2) + Mathf.Abs(r1 - r2) + Mathf.Abs(s1 - s2)) / 2f;
    }
    
    // Check if a unit is on a specific tile
    private bool IsUnitOnTileFallback(HexTile tile)
    {
        if (tile == null)
            return false;
            
        foreach (Unit unit in Object.FindObjectsOfType<Unit>())
        {
            if (unit == this) continue; // Skip self
            
            float distance = Vector2.Distance(
                new Vector2(tile.transform.position.x, tile.transform.position.y),
                new Vector2(unit.transform.position.x, unit.transform.position.y)
            );
            
            // If the unit is close enough to this tile, consider it "on" this tile
            if (distance < 0.5f)
                return true;
        }
        
        return false;
    }
    
    // Move along a path similar to player movement
    private IEnumerator MoveAlongPathCoroutine(List<HexTile> path)
    {
        // If path is empty or only contains start tile, mark as moved and return
        if (path == null || path.Count <= 1)
        {
            //Debug.Log($"{gameObject.name}: No valid path to move along");
            hasMoved = true;
            yield break;
        }

        // Set flags
        isMoving = true;
        IsAnyUnitMoving = true;
        
        // Get grid manager for unit detection
        HexGridManager gridManager = FindObjectOfType<HexGridManager>();
        
        // Skip the first tile which is the starting position
        for (int i = 1; i < path.Count; i++)
        {
            // Get the next tile in the path
            HexTile nextTile = path[i];
            
            // Double-check that the tile is still unoccupied before moving
            bool isOccupied = gridManager != null ? 
                gridManager.IsUnitOnTile(nextTile) : 
                IsUnitOnTileFallback(nextTile);
                
            if (isOccupied)
            {
                //Debug.Log($"{gameObject.name}: Tile {i} is occupied, stopping movement");
                break; // Stop the path here
            }
            
            // Calculate positions
            Vector3 startPos = transform.position;
            Vector3 targetPos = nextTile.transform.position;
            
            // Move at a fixed speed for consistency
            float distance = Vector3.Distance(startPos, targetPos);
            float actualMoveTime = distance / moveSpeed;
            float elapsed = 0f;
            
            //Debug.Log($"{gameObject.name}: Moving to tile {i} of {path.Count}");
            
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
        
        // Reduce remaining movement points
        int tilesTraversed = path.Count - 1;
        remainingMovementPoints -= tilesTraversed;
        if (remainingMovementPoints <= 0)
        {
            remainingMovementPoints = 0;
        }
        
        // Always mark as moved after attempting movement
        hasMoved = true;
        
        //Debug.Log($"{gameObject.name}: Movement complete. Remaining MP: {remainingMovementPoints}");
    }
    
    // Apply enemy data to this enemy
    public void ApplyEnemyData()
    {
        if (enemyData == null) return;
        
        // Apply base stats
        maxHealth = enemyData.maxHealth;
        currentHealth = maxHealth;
        attackDamage = enemyData.attackDamage;
        speed = enemyData.speed;
        movementRange = enemyData.movementRange;
        
        // Apply visual settings
        if (spriteRenderer != null)
        {
            if (enemyData.enemySprite != null)
                spriteRenderer.sprite = enemyData.enemySprite;
            
            spriteRenderer.color = enemyData.enemyColor;
        }
        
        // Update game object name for easier identification
        gameObject.name = $"Enemy_{enemyData.enemyName}";
    }
    
    // Get enemy type for UI and other systems
    public EnemyType GetEnemyType()
    {
        return enemyData != null ? enemyData.enemyType : EnemyType.Light;
    }
    
    // Get enemy name for UI
    public string GetEnemyName()
    {
        return enemyData != null ? enemyData.enemyName : "Unknown Enemy";
    }
    
    // Get enemy description for UI
    public string GetEnemyDescription()
    {
        return enemyData != null ? enemyData.description : "A mysterious foe.";
    }
    
    // Check if this enemy should use special attack
    private bool ShouldUseSpecialAttack()
    {
        if (enemyData == null || enemyData.specialAttack == null)
            return false;
            
        return Random.value < enemyData.specialAttackChance;
    }
}
