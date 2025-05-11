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
        
        // Initialize neighbors for pathfinding
        SetupTileNeighbors();
        
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
            
        // Debug keys
        if (Input.GetKeyDown(KeyCode.F))
            FindPlayerUnit();
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
        
        foreach (HexTile tile in allTiles)
        {
            tile.neighbors.Clear();
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
        
        // Skip unwalkable or occupied tiles
        if (!targetTile.isWalkable || IsUnitOnTile(targetTile))
        {
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        // Find the current tile of the player unit
        HexTile startTile = GetTileAtPosition(playerUnit.transform.position);
        if (startTile == null || startTile == targetTile)
            return;
            
        // Calculate path from player to target tile
        List<HexTile> path = CalculatePath(startTile, targetTile);
        
        // Check if the path is valid and within movement range
        bool isPathValid = path.Count > 0;
        if (isPathValid && path.Count - 1 > playerUnit.movementPoints)
            isPathValid = false;
        
        // Show the path with appropriate coloring
        if (path.Count > 0)
        {
            currentPath = path;
            
            foreach (HexTile tile in path)
                tile.SetAsPathTile(true, isPathValid);
        }
    }
    
    // Execute movement to the target tile (when clicked)
    public void ExecuteMovementToTile(HexTile targetTile)
    {
        // Immediately reset ALL tiles
        ResetAllTiles();
        
        // Validate game state
        if (gameManager == null || gameManager.CurrentState != GameState.PlayerTurn ||
            playerUnit == null || playerUnit.hasMoved)
            return;
        
        // Skip unwalkable tiles or tiles with units
        if (!targetTile.isWalkable || IsUnitOnTile(targetTile))
        {
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        // Get current tile of the player unit
        HexTile startTile = GetTileAtPosition(playerUnit.transform.position);
        if (startTile == null || startTile == targetTile)
            return;
        
        // Create a direct path from player to target
        List<HexTile> path = CalculatePath(startTile, targetTile);
        
        // Check if the path is valid
        if (path.Count < 2)
        {
            targetTile.FlashColor(targetTile.invalidColor, 0.3f);
            return;
        }
        
        // Check if path is within movement range
        if (path.Count - 1 > playerUnit.movementPoints)
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
    // Calculate path between two tiles using cube coordinates
    private List<HexTile> CalculatePath(HexTile start, HexTile end)
    {
        List<HexTile> path = new List<HexTile>();
        
        if (start == null || end == null)
            return path;
            
        // Use cube coordinates line drawing algorithm
        HexCoordinates[] lineCoords = HexCoordinates.DrawLine(start.cubeCoords, end.cubeCoords);
        
        // Convert coordinates to tiles
        foreach (HexCoordinates coord in lineCoords)
        {
            HexTile tile = FindTileByCoordinates(coord);
            
            if (tile != null)
            {
                path.Add(tile);
                
                // Stop at unwalkable tiles (except the start)
                if (!tile.isWalkable && tile != start)
                    break;
            }
        }
        
        return path;
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
        
        foreach (HexTile tile in GetComponentsInChildren<HexTile>())
        {
            float distance = Vector2.Distance(
                new Vector2(worldPosition.x, worldPosition.y),
                new Vector2(tile.transform.position.x, tile.transform.position.y)
            );
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTile = tile;
            }
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
} 