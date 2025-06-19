using UnityEngine;
using System.Collections.Generic;

public class RandomMapGenerator : MonoBehaviour
{
    [Header("Map Generation Settings")]
    [Range(0f, 1f)]
    public float wallDensity = 0.2f; // Percentage of tiles that will be walls
    
    [Header("Generation Rules")]
    public bool avoidWallClusters = true; // Prevent large wall clusters
    public bool ensurePlayableArea = true; // Ensure units have space to move
    public int minDistanceFromEdge = 1; // Keep walls away from edges
    
    [Header("Tile Prefabs")]
    public GameObject grassTilePrefab;
    public GameObject wallTilePrefab;
    
    [Header("Future Tile Types (Not Yet Implemented)")]
    public GameObject mudTilePrefab; // For future use
    public GameObject waterTilePrefab; // For future use
    
    [Header("Debug")]
    public bool generateOnStart = true;
    public bool showDebugInfo = false;
    
    private HexGridGenerator gridGenerator;
    private int mapWidth;
    private int mapHeight;
    private TileType[,] tileMap; // Grid representation for easier algorithms
    
    public enum TileType
    {
        Grass,
        Wall,
        // Future types can be added here:
        // Mud,
        // Water,
        // etc.
    }
    
    private void Start()
    {
        gridGenerator = GetComponent<HexGridGenerator>();
        if (gridGenerator == null)
        {
            Debug.LogError("RandomMapGenerator requires HexGridGenerator component!");
            return;
        }
        
        // Disable HexGridGenerator to prevent it from generating its own tiles
        gridGenerator.enabled = false;
        
        if (generateOnStart)
        {
            GenerateRandomMap();
        }
    }
    
    [ContextMenu("Generate Random Map")]
    public void GenerateRandomMap()
    {
        if (gridGenerator == null)
        {
            Debug.LogError("No HexGridGenerator found!");
            return;
        }
        
        // Get map dimensions
        mapWidth = gridGenerator.gridWidth;
        mapHeight = gridGenerator.gridHeight;
        
        // Clear existing tiles
        ClearExistingTiles();
        
        // Generate tile layout
        GenerateTileLayout();
        
        // Create actual tile GameObjects
        CreateTilesFromLayout();
        
        if (showDebugInfo)
        {
            PrintMapStats();
        }
    }
    
    private void ClearExistingTiles()
    {
        // Clear all child tiles
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
    
    private void GenerateTileLayout()
    {
        // Initialize the tile map
        tileMap = new TileType[mapWidth, mapHeight];
        
        // First pass: Fill with grass
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                tileMap[x, y] = TileType.Grass;
            }
        }
        
        // Second pass: Add walls randomly
        PlaceWallsRandomly();
        
        // Third pass: Apply generation rules
        if (avoidWallClusters)
        {
            ReduceWallClusters();
        }
        
        if (ensurePlayableArea)
        {
            EnsurePlayableSpaces();
        }
    }
    
    private void PlaceWallsRandomly()
    {
        int totalTiles = mapWidth * mapHeight;
        int targetWalls = Mathf.RoundToInt(totalTiles * wallDensity);
        int wallsPlaced = 0;
        
        // Create list of valid positions (avoiding edges if specified)
        List<Vector2Int> validPositions = new List<Vector2Int>();
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Skip edge tiles if minDistanceFromEdge is set
                if (minDistanceFromEdge > 0)
                {
                    if (x < minDistanceFromEdge || x >= mapWidth - minDistanceFromEdge ||
                        y < minDistanceFromEdge || y >= mapHeight - minDistanceFromEdge)
                    {
                        continue;
                    }
                }
                
                validPositions.Add(new Vector2Int(x, y));
            }
        }
        
        // Randomly select positions for walls
        while (wallsPlaced < targetWalls && validPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, validPositions.Count);
            Vector2Int pos = validPositions[randomIndex];
            
            tileMap[pos.x, pos.y] = TileType.Wall;
            wallsPlaced++;
            
            validPositions.RemoveAt(randomIndex);
        }
    }
    
    private void ReduceWallClusters()
    {
        // Remove walls that have too many wall neighbors
        List<Vector2Int> wallsToRemove = new List<Vector2Int>();
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (tileMap[x, y] == TileType.Wall)
                {
                    int wallNeighbors = CountWallNeighbors(x, y);
                    
                    // If a wall has 4 or more wall neighbors, remove it
                    if (wallNeighbors >= 4)
                    {
                        wallsToRemove.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        // Remove the identified walls
        foreach (Vector2Int pos in wallsToRemove)
        {
            tileMap[pos.x, pos.y] = TileType.Grass;
        }
    }
    
    private void EnsurePlayableSpaces()
    {
        // Ensure there are some open 2x2 areas for units to move
        List<Vector2Int> areasToOpen = new List<Vector2Int>();
        
        // Check for completely blocked 2x2 areas
        for (int x = 0; x < mapWidth - 1; x++)
        {
            for (int y = 0; y < mapHeight - 1; y++)
            {
                int wallsInArea = 0;
                if (tileMap[x, y] == TileType.Wall) wallsInArea++;
                if (tileMap[x + 1, y] == TileType.Wall) wallsInArea++;
                if (tileMap[x, y + 1] == TileType.Wall) wallsInArea++;
                if (tileMap[x + 1, y + 1] == TileType.Wall) wallsInArea++;
                
                // If entire 2x2 area is walls, mark one for removal
                if (wallsInArea == 4)
                {
                    areasToOpen.Add(new Vector2Int(x, y));
                }
            }
        }
        
        // Open up some spaces in blocked areas
        foreach (Vector2Int area in areasToOpen)
        {
            // Remove one wall from each blocked 2x2 area
            tileMap[area.x, area.y] = TileType.Grass;
        }
    }
    
    private int CountWallNeighbors(int x, int y)
    {
        int count = 0;
        
        // Check all 6 hexagonal neighbors
        Vector2Int[] hexNeighborOffsets = GetHexNeighborOffsets(x);
        
        foreach (Vector2Int offset in hexNeighborOffsets)
        {
            int nx = x + offset.x;
            int ny = y + offset.y;
            
            // Check bounds
            if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
            {
                if (tileMap[nx, ny] == TileType.Wall)
                {
                    count++;
                }
            }
        }
        
        return count;
    }
    
    private Vector2Int[] GetHexNeighborOffsets(int col)
    {
        // Hex neighbor offsets depend on whether the column is even or odd
        if (col % 2 == 0) // Even column
        {
            return new Vector2Int[]
            {
                new Vector2Int(1, 0),   // Right
                new Vector2Int(1, -1),  // Upper right
                new Vector2Int(0, -1),  // Upper left
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(-1, -1), // Lower left
                new Vector2Int(0, 1)    // Lower right
            };
        }
        else // Odd column
        {
            return new Vector2Int[]
            {
                new Vector2Int(1, 0),   // Right
                new Vector2Int(1, 1),   // Upper right
                new Vector2Int(0, -1),  // Upper left
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(-1, 1),  // Lower left
                new Vector2Int(0, 1)    // Lower right
            };
        }
    }
    
    private void CreateTilesFromLayout()
    {
        // Use the same positioning logic as HexGridGenerator
        float hexRadius = gridGenerator.hexRadius;
        float hexHeight = hexRadius * 1.73205080757f; // √3
        float xSpacing = hexRadius * 1.5f;
        float ySpacing = hexHeight;
        
        for (int col = 0; col < mapWidth; col++)
        {
            for (int row = 0; row < mapHeight; row++)
            {
                // Calculate position (same as HexGridGenerator)
                float xPos;
                float yPos;
                
                if (col % 2 == 1)
                {
                    xPos = col * xSpacing;
                    yPos = row * ySpacing + (ySpacing * 0.5f);
                }
                else
                {
                    xPos = col * xSpacing;
                    yPos = row * ySpacing;
                }
                
                Vector3 position = new Vector3(xPos, yPos, 0);
                
                // Create appropriate tile based on layout
                GameObject tilePrefab = GetTilePrefab(tileMap[col, row]);
                if (tilePrefab != null)
                {
                    GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    tile.name = $"{tileMap[col, row]}Tile_{col}_{row}";
                    
                    // Ensure proper tile component is added and initialized
                    HexTile hexTile = EnsureProperTileComponent(tile, tileMap[col, row]);
                    if (hexTile != null)
                    {
                        hexTile.Initialize(col, row);
                        

                    }
                }
            }
        }
    }
    
    private GameObject GetTilePrefab(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Grass:
                return grassTilePrefab != null ? grassTilePrefab : gridGenerator.hexTilePrefab;
            case TileType.Wall:
                return wallTilePrefab != null ? wallTilePrefab : gridGenerator.wallTilePrefab;
            // Future tile types can be added here:
            // case TileType.Mud:
            //     return mudTilePrefab;
            default:
                return grassTilePrefab != null ? grassTilePrefab : gridGenerator.hexTilePrefab;
        }
    }
    
    private HexTile EnsureProperTileComponent(GameObject tileObject, TileType tileType)
    {
        HexTile hexTile = tileObject.GetComponent<HexTile>();
        
        // Check if we need to add the specific tile component
        switch (tileType)
        {
            case TileType.Grass:
                if (!(hexTile is GrassTile))
                {
                    // Remove generic HexTile if it exists
                    if (hexTile != null && hexTile.GetType() == typeof(HexTile))
                        DestroyImmediate(hexTile);
                    
                    // Add GrassTile component
                    hexTile = tileObject.AddComponent<GrassTile>();
                }
                break;
                
            case TileType.Wall:
                if (!(hexTile is WallTile))
                {
                    // Remove generic HexTile if it exists
                    if (hexTile != null && hexTile.GetType() == typeof(HexTile))
                        DestroyImmediate(hexTile);
                    
                    // Add WallTile component
                    hexTile = tileObject.AddComponent<WallTile>();
                }
                break;
                
            // Future tile types:
            // case TileType.Mud:
            //     if (!(hexTile is MudTile))
            //     {
            //         if (hexTile != null && hexTile.GetType() == typeof(HexTile))
            //             DestroyImmediate(hexTile);
            //         hexTile = tileObject.AddComponent<MudTile>();
            //     }
            //     break;
                
            default:
                // Ensure at least a basic HexTile component exists
                if (hexTile == null)
                    hexTile = tileObject.AddComponent<HexTile>();
                break;
        }
        
        return hexTile;
    }
    
    private void PrintMapStats()
    {
        int grassCount = 0;
        int wallCount = 0;
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (tileMap[x, y] == TileType.Grass)
                    grassCount++;
                else if (tileMap[x, y] == TileType.Wall)
                    wallCount++;
            }
        }
        
        float actualWallDensity = (float)wallCount / (grassCount + wallCount);
        
        Debug.Log($"Map Generated: {grassCount} grass tiles, {wallCount} wall tiles");
        Debug.Log($"Target wall density: {wallDensity:P1}, Actual: {actualWallDensity:P1}");
    }
    
    public void RegenerateWithSettings(float newWallDensity, bool avoidClusters, bool ensurePlayable)
    {
        wallDensity = newWallDensity;
        avoidWallClusters = avoidClusters;
        ensurePlayableArea = ensurePlayable;
        GenerateRandomMap();
    }
} 