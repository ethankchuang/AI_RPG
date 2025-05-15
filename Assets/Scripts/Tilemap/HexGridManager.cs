using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HexGridManager : MonoBehaviour
{
    #region Properties
    [Header("Grid Reference")]
    public HexGridGenerator gridGenerator;
    
    [Header("Path Display")]
    public float invalidPathDisplayTime = 1.0f;
    
    // References
    [HideInInspector] public Unit playerUnit;
    private GameManager gameManager;
    
    // Path finding
    private List<HexTile> currentPath = new List<HexTile>();
    #endregion
    
    #region Initialization
    private void Start()
    {
        // Find references
        gameManager = GameManager.Instance;
        if (gameManager == null)
            Debug.LogError("GameManager not found!");
        
        // Set up grid generator if needed
        SetupGridGenerator();
        
        // Initialize neighbors for pathfinding - MUST happen AFTER grid generation
        if (transform.childCount > 0) 
        {
            SetupTileNeighbors();
        }
        
        // Find the player unit
        FindPlayerUnit();
    }
    
    private void SetupGridGenerator()
    {
        if (gridGenerator == null)
            gridGenerator = GetComponent<HexGridGenerator>();
            
        if (gridGenerator == null)
        {
            Debug.LogError("No HexGridGenerator found!");
            return;
        }
        
        // Generate grid if needed
        if (transform.childCount == 0)
            gridGenerator.GenerateGrid();
    }
    #endregion
    
    #region Update
    private void Update()
    {
        // Try to find player unit if null
        if (playerUnit == null)
            FindPlayerUnit();
        
        // Setup neighbors if needed
        if (transform.childCount > 0 && !HasNeighborsSetup())
        {
            SetupTileNeighbors();
        }
            
        // Debug keys
        if (Input.GetKeyDown(KeyCode.F))
            FindPlayerUnit();
    }
    
    // Check if neighbors are properly set up
    private bool HasNeighborsSetup()
    {
        int tilesWithNeighbors = 0;
        
        HexTile[] allTiles = GetComponentsInChildren<HexTile>();
        foreach (HexTile tile in allTiles)
        {
            if (tile != null && tile.neighbors != null && tile.neighbors.Count > 0)
                tilesWithNeighbors++;
        }
        
        return tilesWithNeighbors > 0;
    }
    #endregion
    
    #region Player Unit Management
    // Find the player unit
    private void FindPlayerUnit()
    {
        // First check GameManager reference
        if (gameManager != null && gameManager.selectedUnit != null)
        {
            playerUnit = gameManager.selectedUnit;
            return;
        }
        
        // Search all units in scene
        Unit[] units = FindObjectsOfType<Unit>();
        
        // Try to find a unit with "Player" in the name
        foreach (Unit unit in units)
        {
            if (unit.gameObject.name.Contains("Player"))
            {
                playerUnit = unit;
                return;
            }
        }
        
        // If none found, use the first unit if available
        if (units.Length > 0)
            playerUnit = units[0];
    }
    #endregion
    
    #region Tile Neighbors
    // Setup tile neighbors for all tiles in the grid
    private void SetupTileNeighbors()
    {
        HexTile[] allTiles = GetComponentsInChildren<HexTile>();
        
        if (allTiles.Length == 0)
            return;
        
        foreach (HexTile tile in allTiles)
        {
            if (tile == null)
                continue;
            
            // Make sure neighbors list exists
            if (tile.neighbors == null)
                tile.neighbors = new List<HexTile>();
            else
                tile.neighbors.Clear();
                
            // Add neighbors
            AddValidNeighbors(tile, allTiles);
        }
    }
    
    // Add valid neighbors for a specific tile using cube coordinates
    private void AddValidNeighbors(HexTile tile, HexTile[] allTiles)
    {
        // Define the 6 directions in cube coordinates
        Vector3Int[] cubeDirections = new Vector3Int[]
        {
            new Vector3Int(1, -1, 0),    // Right
            new Vector3Int(1, 0, -1),    // Upper right
            new Vector3Int(0, 1, -1),    // Upper left
            new Vector3Int(-1, 1, 0),    // Left
            new Vector3Int(-1, 0, 1),    // Lower left
            new Vector3Int(0, -1, 1)     // Lower right
        };
        
        foreach (Vector3Int dir in cubeDirections)
        {
            // Calculate neighbor cube coordinates
            HexCoordinates neighborCoord = new HexCoordinates(
                tile.cubeCoords.q + dir.x,
                tile.cubeCoords.r + dir.y,
                tile.cubeCoords.s + dir.z
            );
            
            // Convert to offset coordinates
            Vector2Int offsetCoord = neighborCoord.ToOffsetCoordinates();
            
            // Skip invalid coordinates (outside grid)
            if (offsetCoord.x < 0 || offsetCoord.x >= gridGenerator.gridWidth ||
                offsetCoord.y < 0 || offsetCoord.y >= gridGenerator.gridHeight)
                continue;
            
            // Find matching neighbor in array
            foreach (HexTile neighbor in allTiles)
            {
                if (neighbor.column == offsetCoord.x && neighbor.row == offsetCoord.y)
                {
                    tile.neighbors.Add(neighbor);
                    break;
                }
            }
        }
    }
    #endregion
    
    #region Path Visualization
    // Show path to a specific tile on hover
    public void ShowPathToTile(HexTile targetTile)
    {
        // Don't show path if any unit is currently moving
        if (Unit.IsAnyUnitMoving)
            return;
        
        // Reset all tiles to default
        ResetAllTiles();
        
        // Clear any existing path
        ClearPath();
        
        // Validate player unit state
        if (playerUnit == null || playerUnit.hasMoved)
            return;
        
        // Find the current tile of the player unit
        HexTile startTile = GetTileAtPosition(playerUnit.transform.position);
        if (startTile == null)
            return;
        
        // Don't show path to the same tile
        if (startTile == targetTile)
            return;
        
        // Calculate path using the fringe-based algorithm
        List<HexTile> path = CalculatePath(startTile, targetTile);
        
        // If we got a valid path with at least two tiles
        if (path.Count >= 2)
        {
            // Store the path for reference
            currentPath = path;
            
            // Check if path is within movement range
            bool isWithinRange = path.Count - 1 <= playerUnit.movementPoints;
            bool isValidDestination = targetTile.isWalkable && !IsUnitOnTile(targetTile);
            bool isPathValid = isWithinRange && isValidDestination;
            
            // Show all tiles in the path
            foreach (HexTile tile in path)
            {
                tile.SetAsPathTile(true, isPathValid);
            }
        }
        else if (!targetTile.isWalkable || IsUnitOnTile(targetTile))
        {
            // Flash invalid color for unwalkable tiles
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
        }
    }
    
    // Execute movement to the target tile (when clicked)
    public void ExecuteMovementToTile(HexTile targetTile)
    {
        Debug.Log($"ExecuteMovementToTile: Attempting to move to {targetTile.name}");
        
        // Immediately reset ALL tiles
        ResetAllTiles();
        
        // Validate game state
        if (gameManager == null)
        {
            Debug.LogError("ExecuteMovementToTile: GameManager is null");
            return;
        }
        
        if (gameManager.CurrentState != GameState.PlayerTurn)
        {
            Debug.LogError("ExecuteMovementToTile: Not player turn");
            return;
        }
        
        if (playerUnit == null)
        {
            Debug.LogError("ExecuteMovementToTile: No player unit found");
            return;
        }
        
        if (playerUnit.hasMoved)
        {
            Debug.LogError("ExecuteMovementToTile: Player has already moved");
            return;
        }
        
        // Skip unwalkable tiles or tiles with units
        if (!targetTile.isWalkable)
        {
            Debug.LogError($"ExecuteMovementToTile: Target tile {targetTile.name} is not walkable");
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        if (IsUnitOnTile(targetTile))
        {
            Debug.LogError($"ExecuteMovementToTile: Target tile {targetTile.name} is occupied");
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        // Get current tile of the player unit
        HexTile startTile = GetTileAtPosition(playerUnit.transform.position);
        if (startTile == null)
        {
            Debug.LogError("ExecuteMovementToTile: Could not find start tile");
            return;
        }
        
        if (startTile == targetTile)
        {
            Debug.LogError("ExecuteMovementToTile: Start and target tiles are the same");
            return;
        }
        
        Debug.Log($"ExecuteMovementToTile: Finding path from {startTile.name} to {targetTile.name}");
        
        // Create path from player to target using fringe-based algorithm
        List<HexTile> path = CalculatePath(startTile, targetTile);
        
        Debug.Log($"ExecuteMovementToTile: Path calculated with {path.Count} tiles");
        
        // Check if the path is valid
        if (path.Count < 2)
        {
            Debug.LogError("ExecuteMovementToTile: Path too short, need at least 2 tiles");
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        // Check if path is within movement range
        if (path.Count - 1 > playerUnit.movementPoints)
        {
            Debug.LogError($"ExecuteMovementToTile: Path too long ({path.Count-1} steps, {playerUnit.movementPoints} allowed)");
            // Show invalid path
            foreach (HexTile tile in path)
                tile.SetAsPathTile(true, false);
            
            // Clear the invalid path after a delay
            StartCoroutine(ClearPathAfterDelay(invalidPathDisplayTime));
            return;
        }
        
        Debug.Log("ExecuteMovementToTile: Path is valid, executing movement");
        
        // Path is valid - clear visuals and execute movement
        ClearPath();
        playerUnit.MoveAlongPath(path);
        
        // Update camera target
        if (gameManager.cameraFollow != null)
            gameManager.cameraFollow.SetTarget(playerUnit);
    }
    
    // Clear the current path
    public void ClearPath()
    {
        foreach (HexTile tile in currentPath)
            tile.ResetColor();
        
        currentPath.Clear();
    }
    
    // Reset all tiles in the grid
    private void ResetAllTiles()
    {
        foreach (HexTile tile in GetComponentsInChildren<HexTile>())
            tile.ResetColor();
    }
    
    // Coroutine to clear path after a delay
    private IEnumerator ClearPathAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearPath();
    }
    #endregion
    
    #region Pathfinding
    // Calculate path between two tiles using fringe-based algorithm
    private List<HexTile> CalculatePath(HexTile start, HexTile end)
    {
        Debug.Log($"CalculatePath: Finding path from {start?.name} to {end?.name}");
        
        if (start == null || end == null)
        {
            Debug.LogError("CalculatePath: Start or end tile is null");
            return new List<HexTile>();
        }
            
        // If we can't reach the destination (it's a wall), return empty path
        if (!end.isWalkable)
        {
            Debug.LogWarning($"CalculatePath: End tile {end.name} is not walkable");
            List<HexTile> invalidPath = new List<HexTile>();
            invalidPath.Add(start);
            return invalidPath;
        }
        
        // If start and end are the same
        if (start == end)
        {
            Debug.Log("CalculatePath: Start and end tiles are the same");
            List<HexTile> singleTilePath = new List<HexTile>();
            singleTilePath.Add(start);
            return singleTilePath;
        }
        
        // Simple breadth-first-search without using fringes - for debugging
        Queue<HexTile> frontier = new Queue<HexTile>();
        Dictionary<HexTile, HexTile> previous = new Dictionary<HexTile, HexTile>();
        
        frontier.Enqueue(start);
        previous[start] = null;
        
        bool endFound = false;
        
        // Keep exploring until we find the end or explore all reachable tiles
        while (frontier.Count > 0 && !endFound)
        {
            HexTile current = frontier.Dequeue();
            
            // Check if we've reached the end
            if (current == end)
            {
                endFound = true;
                break;
            }
            
            // Check each neighbor of the current tile
            if (current.neighbors == null)
            {
                Debug.LogError($"Tile {current.name} has null neighbors list");
                continue;
            }
            
            Debug.Log($"Tile {current.name} has {current.neighbors.Count} neighbors");
            
            foreach (HexTile neighbor in current.neighbors)
            {
                if (neighbor == null)
                {
                    Debug.LogError("Found null neighbor in neighbors list");
                    continue;
                }
                
                // Skip already visited or unwalkable tiles
                if (previous.ContainsKey(neighbor) || !neighbor.isWalkable)
                    continue;
                
                // Record where we came from
                previous[neighbor] = current;
                frontier.Enqueue(neighbor);
            }
        }
        
        // If end tile wasn't reached, return just the start
        if (!previous.ContainsKey(end))
        {
            Debug.LogWarning($"CalculatePath: No path found from {start.name} to {end.name}");
            List<HexTile> unreachablePath = new List<HexTile>();
            unreachablePath.Add(start);
            return unreachablePath;
        }
        
        // Reconstruct the path
        List<HexTile> path = new List<HexTile>();
        HexTile currentTile = end;
        
        // Build the path backwards from end to start
        while (currentTile != null)
        {
            path.Add(currentTile);
            currentTile = previous[currentTile];
        }
        
        // Reverse to get start to end
        path.Reverse();
        
        Debug.Log($"CalculatePath: Found path with {path.Count} tiles");
        return path;
    }
    
    // Calculate hex distance between two tiles (for pathfinding heuristic)
    private float HexDistance(HexTile a, HexTile b)
    {
        // Use cube coordinates for accurate hex distance
        HexCoordinates ac = a.cubeCoords;
        HexCoordinates bc = b.cubeCoords;
        
        return (Mathf.Abs(ac.q - bc.q) + Mathf.Abs(ac.r - bc.r) + Mathf.Abs(ac.s - bc.s)) / 2f;
    }
    
    // Find a tile by cube coordinates
    private HexTile FindTileByCoordinates(HexCoordinates coord)
    {
        Vector2Int offset = coord.ToOffsetCoordinates();
        
        foreach (HexTile tile in GetComponentsInChildren<HexTile>())
        {
            if (tile.column == offset.x && tile.row == offset.y)
                return tile;
        }
        
        return null;
    }
    
    // Get the tile at a specific world position
    private HexTile GetTileAtPosition(Vector3 worldPosition)
    {
        HexTile closestTile = null;
        float closestDistance = float.MaxValue;
        
        // Get all tiles
        HexTile[] allTiles = GetComponentsInChildren<HexTile>();
        
        if (allTiles.Length == 0)
        {
            Debug.LogError("GetTileAtPosition: No tiles found in the grid");
            return null;
        }
        
        // Find the closest tile to the world position
        foreach (HexTile tile in allTiles)
        {
            // Calculate the actual distance in 2D space (ignore Z)
            float distance = Vector2.Distance(
                new Vector2(worldPosition.x, worldPosition.y),
                new Vector2(tile.transform.position.x, tile.transform.position.y)
            );
            
            // Keep track of the closest tile
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTile = tile;
            }
        }
        
        // Only consider a tile "at" this position if it's reasonably close
        if (closestDistance > 1.0f)
        {
            Debug.LogWarning($"GetTileAtPosition: Closest tile is {closestDistance} units away, which may be too far");
        }
        
        if (closestTile != null)
        {
            Debug.Log($"GetTileAtPosition: Found tile {closestTile.name} at distance {closestDistance:F2}");
        }
        
        return closestTile;
    }
    
    // Check if there's a unit on a specific tile
    private bool IsUnitOnTile(HexTile tile)
    {
        if (tile == null)
            return false;
            
        foreach (Unit unit in FindObjectsOfType<Unit>())
        {
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
    #endregion
    
    #region Tile Type Management
    // Replace a tile with a wall tile
    public void ReplaceWithWallTile(HexTile currentTile)
    {
        if (currentTile == null)
            return;
            
        // Already a wall, no need to replace
        if (currentTile.GetType() == typeof(WallTile))
            return;
            
        // Check if wall prefab exists
        if (gridGenerator == null)
            return;
        
        if (gridGenerator.wallTilePrefab == null)
        {
            // Try to set up the wall prefab
            gridGenerator.SetupWallTilePrefab();
            
            if (gridGenerator.wallTilePrefab == null)
                return;
        }
        
        // Create a wall tile at the same position
        GameObject wallTileObject = Instantiate(
            gridGenerator.wallTilePrefab, 
            currentTile.transform.position,
            Quaternion.identity,
            transform
        );
        
        // Initialize the new wall tile
        WallTile wallTile = wallTileObject.GetComponent<WallTile>();
        if (wallTile != null)
        {
            wallTile.Initialize(currentTile.column, currentTile.row);
            
            // Transfer neighbor connections
            wallTile.neighbors = new List<HexTile>(currentTile.neighbors);
            
            // Update neighbors to point to the new tile
            foreach (HexTile neighbor in wallTile.neighbors)
            {
                for (int i = 0; i < neighbor.neighbors.Count; i++)
                {
                    if (neighbor.neighbors[i] == currentTile)
                    {
                        neighbor.neighbors[i] = wallTile;
                        break;
                    }
                }
            }
            
            // Name the new tile
            wallTileObject.name = $"WallTile_{currentTile.column}_{currentTile.row}";
        }
        
        // Destroy the old tile
        Destroy(currentTile.gameObject);
        
        // Reset path visualization
        ClearPath();
        
        // Update all neighbors for the entire grid
        SetupTileNeighbors();
    }
    
    // Replace a tile with a grass tile
    public void ReplaceWithGrassTile(HexTile currentTile)
    {
        if (currentTile == null)
            return;
            
        // Already a grass tile, no need to replace
        if (currentTile.GetType() == typeof(GrassTile))
            return;
            
        // Create a grass tile at the same position
        GameObject grassTileObject = Instantiate(
            gridGenerator.hexTilePrefab, 
            currentTile.transform.position,
            Quaternion.identity,
            transform
        );
        
        // Initialize the new grass tile
        GrassTile grassTile = grassTileObject.GetComponent<GrassTile>();
        if (grassTile == null)
        {
            // If the prefab doesn't have a GrassTile component but has a HexTile, add GrassTile
            grassTile = grassTileObject.AddComponent<GrassTile>();
        }
        
        if (grassTile != null)
        {
            grassTile.Initialize(currentTile.column, currentTile.row);
            
            // Transfer neighbor connections
            grassTile.neighbors = new List<HexTile>(currentTile.neighbors);
            
            // Update neighbors to point to the new tile
            foreach (HexTile neighbor in grassTile.neighbors)
            {
                for (int i = 0; i < neighbor.neighbors.Count; i++)
                {
                    if (neighbor.neighbors[i] == currentTile)
                    {
                        neighbor.neighbors[i] = grassTile;
                        break;
                    }
                }
            }
            
            // Name the new tile
            grassTileObject.name = $"GrassTile_{currentTile.column}_{currentTile.row}";
        }
        
        // Destroy the old tile
        Destroy(currentTile.gameObject);
        
        // Reset path visualization
        ClearPath();
        
        // Update all neighbors for the entire grid
        SetupTileNeighbors();
    }
    #endregion
    
    // Public method to force refresh of neighbors (can be called from editor or other scripts)
    public void RefreshTileNeighbors()
    {
        Debug.Log("Manually refreshing tile neighbors");
        SetupTileNeighbors();
    }
    
    private void OnValidate()
    {
        // This will be called in the editor when the component is modified
        if (Application.isPlaying)
        {
            RefreshTileNeighbors();
        }
    }
} 