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
    [HideInInspector] public Player playerUnit;
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
            return;
        
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
            return;
        
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
        // First check active unit
        if (Unit.ActiveUnit is Player player)
        {
            playerUnit = player;
            return;
        }
        
        // Search all units in scene
        Player[] players = FindObjectsOfType<Player>();
        
        // Use the first player found
        if (players.Length > 0)
        {
            playerUnit = players[0];
            return;
        }
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
        
        // Clear any existing path without resetting movement range tiles
        ClearPath();
        
        // Validate player unit state
        if (playerUnit == null)
            return;
        
        // Only show path if in move mode
        if (!playerUnit.IsInMoveMode || playerUnit.remainingMovementPoints <= 0)
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
            bool isWithinRange = path.Count - 1 <= playerUnit.remainingMovementPoints;
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
        // Immediately reset ALL tiles
        ResetAllTiles();
        
        // Validate game state
        if (gameManager == null)
            return;
        
        // Check if it's player turn and we have a player unit
        if (gameManager.CurrentState != GameState.PlayerTurn)
            return;
        
        if (playerUnit == null)
            return;
        
        // Check if player is in move mode and has movement points
        if (!playerUnit.IsInMoveMode)
            return;
        
        if (playerUnit.remainingMovementPoints <= 0)
            return;
        
        // Skip unwalkable tiles or tiles with units
        if (!targetTile.isWalkable)
        {
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        if (IsUnitOnTile(targetTile))
        {
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        // Get current tile of the player unit
        HexTile startTile = GetTileAtPosition(playerUnit.transform.position);
        if (startTile == null)
            return;
        
        if (startTile == targetTile)
            return;
        
        // Create path from player to target using fringe-based algorithm
        List<HexTile> path = CalculatePath(startTile, targetTile);
        
        // Check if the path is valid
        if (path.Count < 2)
        {
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        // Check if path is within movement range
        if (path.Count - 1 > playerUnit.remainingMovementPoints)
        {
            // Show invalid path
            foreach (HexTile tile in path)
                tile.SetAsPathTile(true, false);
            
            // Clear the invalid path after a delay
            StartCoroutine(ClearPathAfterDelay(invalidPathDisplayTime));
            return;
        }
        
        // Path is valid - clear visuals and execute movement
        ClearPath();
        playerUnit.MoveAlongPath(path);
    }
    
    // Clear the current path
    public void ClearPath()
    {
        foreach (HexTile tile in currentPath)
        {
            // Only reset if it's not in player's highlighted movement range
            bool isInMovementRange = playerUnit != null && 
                                   playerUnit.highlightedTiles != null && 
                                   playerUnit.highlightedTiles.Contains(tile);
            
            if (!isInMovementRange)
                tile.ResetColor();
            else
                tile.SetAsMovementRangeTile(); // Restore movement range highlight
        }
        
        currentPath.Clear();
    }
    
    // Reset all tiles in the grid
    private void ResetAllTiles()
    {
        if (playerUnit == null || playerUnit.highlightedTiles == null)
        {
            // If player is not available, reset all tiles
            foreach (HexTile tile in GetComponentsInChildren<HexTile>())
                tile.ResetColor();
            return;
        }
        
        // Otherwise, only reset tiles that are not part of the movement range
        foreach (HexTile tile in GetComponentsInChildren<HexTile>())
        {
            if (!playerUnit.highlightedTiles.Contains(tile))
                tile.ResetColor();
        }
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
    public List<HexTile> CalculatePath(HexTile start, HexTile end)
    {
        if (start == null || end == null)
            return new List<HexTile>();
            
        // If we can't reach the destination (it's a wall), return empty path
        if (!end.isWalkable)
        {
            List<HexTile> invalidPath = new List<HexTile>();
            invalidPath.Add(start);
            return invalidPath;
        }
        
        // If start and end are the same
        if (start == end)
        {
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
                continue;
            
            foreach (HexTile neighbor in current.neighbors)
            {
                if (neighbor == null)
                    continue;
                
                // Skip already visited tiles
                if (previous.ContainsKey(neighbor))
                    continue;
                
                // Skip unwalkable tiles
                if (!neighbor.isWalkable)
                    continue;
                
                // Skip tiles that have units on them (except for the destination)
                if (neighbor != end && IsUnitOnTile(neighbor))
                    continue;
                
                // Record where we came from
                previous[neighbor] = current;
                frontier.Enqueue(neighbor);
            }
        }
        
        // If end tile wasn't reached, return just the start
        if (!previous.ContainsKey(end))
        {
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
            return null;
        
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
        
        return closestTile;
    }
    
    // Check if there's a unit on a specific tile
    public bool IsUnitOnTile(HexTile tile)
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