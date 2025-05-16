using UnityEngine;
using System.Collections.Generic;

public enum GameState
{
    InitGame,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat
}

public class GameManager : MonoBehaviour
{
    #region Properties
    [Header("References")]
    public HexGridGenerator gridGenerator;
    public HexGridManager gridManager;
    public CameraFollow cameraFollow;
    
    [Header("Game Setup")]
    public int playerUnitsCount = 3;
    public int enemyUnitsCount = 3;
    public GameObject playerUnitPrefab;
    public GameObject enemyUnitPrefab;
    
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    // State
    private GameState currentState;
    private List<Unit> playerUnits = new List<Unit>();
    private List<Unit> enemyUnits = new List<Unit>();
    public Unit selectedUnit;
    
    // Properties
    public GameState CurrentState => currentState;
    #endregion
    
    #region Initialization
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Find references if not set
        if (gridGenerator == null)
            gridGenerator = FindObjectOfType<HexGridGenerator>();
            
        if (gridManager == null)
            gridManager = FindObjectOfType<HexGridManager>();
            
        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollow>();
    }
    
    private void Start()
    {
        // Setup camera bounds
        if (cameraFollow != null && gridGenerator != null)
        {
            cameraFollow.SetBoundsFromGrid(
                gridGenerator.gridWidth,
                gridGenerator.gridHeight,
                gridGenerator.hexRadius
            );
        }
        
        // Start the game
        SetGameState(GameState.InitGame);
    }
    #endregion
    
    #region Game State Management
    // Set the game state and trigger necessary actions
    public void SetGameState(GameState newState)
    {
        currentState = newState;
        
        switch (currentState)
        {
            case GameState.InitGame:
                InitializeGame();
                break;
                
            case GameState.PlayerTurn:
                StartPlayerTurn();
                break;
                
            case GameState.EnemyTurn:
                StartEnemyTurn();
                break;
                
            case GameState.Victory:
                HandleVictory();
                break;
                
            case GameState.Defeat:
                HandleDefeat();
                break;
        }
    }
    
    // Initialize the game - set up references and start player turn
    private void InitializeGame()
    {
        // Make sure the grid is generated
        if (gridGenerator != null && gridGenerator.transform.childCount == 0)
            gridGenerator.GenerateGrid();
        
        // Clear any existing units
        playerUnits.Clear();
        enemyUnits.Clear();
        
        // Place units
        PlaceInitialUnits(true);  // Player units
        PlaceInitialUnits(false); // Enemy units
        
        // Set up camera and references
        SetupInitialReferences();
        
        // Start player turn
        SetGameState(GameState.PlayerTurn);
    }
    
    // Setup initial references including camera and grid manager
    private void SetupInitialReferences()
    {
        // Make sure we have a player unit
        if (playerUnits.Count > 0)
        {
            selectedUnit = playerUnits[0];
            
            // Share the reference with grid manager
            if (gridManager != null)
                gridManager.playerUnit = selectedUnit;
            
            // Focus camera on player unit
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(selectedUnit);
                cameraFollow.CenterOnTarget();
            }
        }
    }
    #endregion
    
    #region Turn Management
    // Start player turn
    private void StartPlayerTurn()
    {
        // Reset all player units
        foreach (Unit unit in playerUnits)
        {
            if (unit != null)
                unit.ResetForNewTurn();
        }
        
        // Make the first player unit active
        if (playerUnits.Count > 0)
            SelectUnit(playerUnits[0]);
        else
            selectedUnit = null;
    }
    
    // End player turn (called from UI)
    public void EndPlayerTurn()
    {
        SetGameState(GameState.EnemyTurn);
    }
    
    // Start enemy turn
    private void StartEnemyTurn()
    {
        // Reset all enemy units
        foreach (Unit unit in enemyUnits)
        {
            if (unit != null)
                unit.ResetForNewTurn();
        }
        
        // Execute AI moves with small delay
        Invoke(nameof(ExecuteEnemyTurn), 0.5f);
    }
    
    // Execute enemy turn logic
    private void ExecuteEnemyTurn()
    {
        // End enemy turn after a delay
        // In a full game, implement AI logic here
        Invoke(nameof(EndEnemyTurn), 1.0f);
    }
    
    // End enemy turn
    private void EndEnemyTurn()
    {
        // Check win/lose conditions
        if (playerUnits.Count == 0)
            SetGameState(GameState.Defeat);
        else if (enemyUnits.Count == 0)
            SetGameState(GameState.Victory);
        else
            SetGameState(GameState.PlayerTurn);
    }
    #endregion
    
    #region Unit Management
    // Update the active unit (used for pathfinding and camera tracking)
    public void SelectUnit(Unit unit)
    {
        // No need to do anything if it's the same unit
        if (selectedUnit == unit)
            return;
            
        // Clear previous unit's movement range
        if (selectedUnit != null)
            selectedUnit.Deselect();
        
        // Update active unit
        selectedUnit = unit;
        
        // Update camera to follow the unit
        if (selectedUnit != null && cameraFollow != null)
            cameraFollow.SetTarget(selectedUnit);
    }
    
    // Clean up units that are destroyed
    public void RemoveUnit(Unit unit)
    {
        if (playerUnits.Contains(unit))
            playerUnits.Remove(unit);
        
        if (enemyUnits.Contains(unit))
            enemyUnits.Remove(unit);
        
        // Check win conditions
        if (playerUnits.Count == 0)
            SetGameState(GameState.Defeat);
        else if (enemyUnits.Count == 0)
            SetGameState(GameState.Victory);
    }
    
    // Place initial units for player or enemy
    private void PlaceInitialUnits(bool isPlayer)
    {
        if (isPlayer)
            PlacePlayerUnit();
        else
            PlaceEnemyUnits();
    }
    
    // Place the player unit
    private void PlacePlayerUnit()
    {
        // Find a good starting position (center bottom of grid)
        HexTile startTile = FindPlayerStartTile();
        
        // Place the player unit
        if (startTile != null && playerUnitPrefab != null)
        {
            GameObject unitObj = Instantiate(playerUnitPrefab, startTile.transform.position, Quaternion.identity);
            unitObj.name = "PlayerUnit";
            
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit != null)
            {
                playerUnits.Add(unit);
                selectedUnit = unit; // This is the active unit for pathfinding
            }
        }
    }
    
    // Find a good starting tile for the player
    private HexTile FindPlayerStartTile()
    {
        HexTile[] allTiles = gridGenerator.GetComponentsInChildren<HexTile>();
        
        // Try to find center-bottom tile
        int centerCol = gridGenerator.gridWidth / 2;
        
        foreach (HexTile tile in allTiles)
        {
            if (tile.column == centerCol && tile.row == 0 && tile.isWalkable)
                return tile;
        }
        
        // If not found, use any walkable tile
        foreach (HexTile tile in allTiles)
        {
            if (tile.isWalkable)
                return tile;
        }
        
        return null;
    }
    
    // Place enemy units
    private void PlaceEnemyUnits()
    {
        if (enemyUnitPrefab == null)
            return;
        
        // Get eligible tiles for enemy placement (top of grid)
        List<HexTile> placementTiles = new List<HexTile>();
        foreach (HexTile tile in gridGenerator.GetComponentsInChildren<HexTile>())
        {
            if (tile.row >= gridGenerator.gridHeight - 2 && tile.isWalkable)
                placementTiles.Add(tile);
        }
        
        // Randomize placement
        System.Random random = new System.Random();
        for (int i = 0; i < placementTiles.Count; i++)
        {
            int j = random.Next(i, placementTiles.Count);
            HexTile temp = placementTiles[i];
            placementTiles[i] = placementTiles[j];
            placementTiles[j] = temp;
        }
        
        // Place enemy units
        int unitsToPlace = Mathf.Min(enemyUnitsCount, placementTiles.Count);
        for (int i = 0; i < unitsToPlace; i++)
        {
            GameObject unitObj = Instantiate(enemyUnitPrefab, placementTiles[i].transform.position, Quaternion.identity);
            unitObj.name = $"EnemyUnit_{i}";
            
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit != null)
                enemyUnits.Add(unit);
        }
    }
    #endregion
    
    #region Game End States
    // Handle victory
    private void HandleVictory()
    {
        Debug.Log("Victory!");
    }
    
    // Handle defeat
    private void HandleDefeat()
    {
        Debug.Log("Defeat!");
    }
    #endregion
} 