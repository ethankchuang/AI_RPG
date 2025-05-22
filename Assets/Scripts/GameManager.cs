using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // Add this for LINQ methods like Select

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
    public GameState CurrentState = GameState.PlayerTurn;
    private List<Player> playerUnits = new List<Player>();
    private List<Enemy> enemyUnits = new List<Enemy>();
    private List<Unit> genericUnits = new List<Unit>(); // For any units that don't inherit from Player/Enemy
    public Unit selectedUnit;
    
    // Turn order management
    private List<Unit> allUnits = new List<Unit>();
    private List<Unit> turnOrder = new List<Unit>();
    private int currentTurnIndex = 0;
    private Unit currentActiveUnit;
    
    // Properties
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
        
        // Initialize the game
        InitializeGame();
        
        // Find all units in the scene
        FindAllUnits();
        
        // Start the first turn
        StartNextTurn();
    }
    #endregion
    
    #region Game State Management
    // Set the game state and trigger necessary actions
    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
        
        // Update UI with the new game state
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.UpdateUI(CurrentState);
        }
        
        switch (CurrentState)
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
        genericUnits.Clear();
        
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
                gridManager.playerUnit = playerUnits[0];
            
            // Focus camera on player unit
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(playerUnits[0]);
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
        foreach (Player unit in playerUnits)
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
        if (CurrentState == GameState.PlayerTurn)
        {
            SetGameState(GameState.EnemyTurn);
            StartNextTurn();
        }
    }
    
    // Start enemy turn
    private void StartEnemyTurn()
    {
        // Reset all enemy units
        foreach (Enemy unit in enemyUnits)
        {
            if (unit != null)
                unit.ResetForNewTurn();
        }
        
        // Execute AI moves with small delay
        StartCoroutine(ExecuteEnemyTurn());
    }
    
    // Execute enemy turn logic
    private IEnumerator ExecuteEnemyTurn()
    {
        // Wait a short time before starting enemy moves
        yield return new WaitForSeconds(0.5f);
        
        // Process each enemy one at a time
        foreach (Enemy enemy in enemyUnits)
        {
            if (enemy != null)
            {
                enemy.ExecuteTurn();
                
                // Wait for this enemy to finish moving
                float waitTime = 5.0f; // Maximum wait time (prevents infinite waiting)
                float elapsed = 0f;
                
                // Wait until the enemy has moved or time expires
                while (!enemy.IsTurnComplete() && elapsed < waitTime)
                {
                    elapsed += 0.1f;
                    yield return new WaitForSeconds(0.1f);
                }
                
                // Additional delay between enemy turns
                yield return new WaitForSeconds(0.5f);
                
                // Check for defeat condition after each enemy move
                if (playerUnits.Count == 0)
                {
                    SetGameState(GameState.Defeat);
                    yield break;
                }
            }
        }
        
        // End enemy turn and start player turn
        SetGameState(GameState.PlayerTurn);
        StartPlayerTurn();
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
        
        // If it's a player unit, update grid manager reference
        if (unit is Player playerUnit && gridManager != null)
            gridManager.playerUnit = playerUnit;
        
        // Update camera to follow the unit
        if (selectedUnit != null && cameraFollow != null)
            cameraFollow.SetTarget(selectedUnit);
    }
    
    // Clean up units that are destroyed
    public void RemoveUnit(Unit unit)
    {
        if (unit is Player playerUnit)
            playerUnits.Remove(playerUnit);
        else if (unit is Enemy enemyUnit)
            enemyUnits.Remove(enemyUnit);
        else if (genericUnits.Contains(unit))
            genericUnits.Remove(unit);
        
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
        {
            // Place player units
            for (int i = 0; i < playerUnitsCount; i++)
            {
                PlacePlayerUnit();
            }
        }
        else
        {
            // Place enemy units
            for (int i = 0; i < enemyUnitsCount; i++)
            {
                PlaceEnemyUnit();
            }
        }
    }
    
    // Place a single player unit
    private void PlacePlayerUnit()
    {
        // Find a good starting position (center bottom of grid)
        HexTile startTile = FindPlayerStartTile();
        
        // Place the player unit
        if (startTile != null && playerUnitPrefab != null)
        {
            GameObject unitObj = Instantiate(playerUnitPrefab, startTile.transform.position, Quaternion.identity);
            unitObj.name = $"PlayerUnit_{playerUnits.Count}";
            
            Player playerUnit = unitObj.GetComponent<Player>();
            if (playerUnit != null)
            {
                playerUnits.Add(playerUnit);
                if (selectedUnit == null)
                    selectedUnit = playerUnit; // This is the active unit for pathfinding
            }
            else
            {
                // Fall back to using Unit if Player component isn't found
                Unit unit = unitObj.GetComponent<Unit>();
                if (unit != null)
                {
                    genericUnits.Add(unit);
                    if (selectedUnit == null)
                        selectedUnit = unit;
                }
            }
        }
    }
    
    // Place a single enemy unit
    private void PlaceEnemyUnit()
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
        
        // Place enemy unit
        if (placementTiles.Count > 0)
        {
            HexTile placementTile = placementTiles[0];
            GameObject unitObj = Instantiate(enemyUnitPrefab, placementTile.transform.position, Quaternion.identity);
            unitObj.name = $"EnemyUnit_{enemyUnits.Count}";
            
            Enemy enemyUnit = unitObj.GetComponent<Enemy>();
            if (enemyUnit != null)
            {
                enemyUnits.Add(enemyUnit);
            }
            else
            {
                // Fall back to using Unit if Enemy component isn't found
                Unit unit = unitObj.GetComponent<Unit>();
                if (unit != null)
                    genericUnits.Add(unit);
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
    #endregion
    
    #region Game End States
    // Handle victory
    private void HandleVictory()
    {
    }
    
    // Handle defeat
    private void HandleDefeat()
    {
    }
    #endregion
    
    private void FindAllUnits()
    {
        allUnits.Clear();
        allUnits.AddRange(FindObjectsOfType<Unit>());
        
        // Calculate turn order based on speed
        CalculateTurnOrder();
    }
    
    private void CalculateTurnOrder()
    {
        turnOrder.Clear();
        
        // Create a list of units with their increment thresholds
        var unitThresholds = new List<(Unit unit, int threshold)>();
        
        foreach (Unit unit in allUnits)
        {
            // Calculate how many increments this unit needs for a turn
            // Higher speed means lower threshold (goes more often)
            int threshold = 100 / unit.speed;
            unitThresholds.Add((unit, threshold));
        }
        
        // Sort by threshold (ascending) to get fastest units first
        unitThresholds.Sort((a, b) => a.threshold.CompareTo(b.threshold));
        
        // Calculate the least common multiple of all thresholds
        int lcm = CalculateLCM(unitThresholds.Select(x => x.threshold).ToList());
        
        // Create the turn order sequence
        for (int increment = 1; increment <= lcm; increment++)
        {
            foreach (var (unit, threshold) in unitThresholds)
            {
                // If this increment is a multiple of the unit's threshold, add them to the sequence
                if (increment % threshold == 0)
                {
                    turnOrder.Add(unit);
                }
            }
        }
        
        currentTurnIndex = 0;
    }
    
    private int CalculateLCM(List<int> numbers)
    {
        if (numbers.Count == 0) return 1;
        
        int lcm = numbers[0];
        for (int i = 1; i < numbers.Count; i++)
        {
            lcm = CalculateLCM(lcm, numbers[i]);
        }
        return lcm;
    }
    
    private int CalculateLCM(int a, int b)
    {
        return Mathf.Abs(a * b) / CalculateGCD(a, b);
    }
    
    private int CalculateGCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
    
    private void StartNextTurn()
    {
        if (turnOrder.Count == 0)
            return;
        
        // Get the next unit in the turn order
        currentActiveUnit = turnOrder[currentTurnIndex];
        
        // Move to next unit in the list
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        
        // Update game state based on unit type
        if (currentActiveUnit is Player)
        {
            CurrentState = GameState.PlayerTurn;
            selectedUnit = currentActiveUnit;
        }
        else
        {
            CurrentState = GameState.EnemyTurn;
            selectedUnit = null;
        }
        
        // Start the unit's turn
        currentActiveUnit.OnTurnStart();
        
        // Update UI
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.UpdateUI(CurrentState);
        }
        
        // Update camera target
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(currentActiveUnit);
        }
    }
    
    public void EndCurrentTurn()
    {
        if (currentActiveUnit != null)
        {
            // Start the next turn
            StartNextTurn();
        }
    }
    
    public void OnUnitDeath(Unit unit)
    {
        // Remove the unit from both lists
        allUnits.Remove(unit);
        
        // Recalculate turn order
        CalculateTurnOrder();
        
        // If it was the current active unit, end the turn
        if (unit == currentActiveUnit)
        {
            EndCurrentTurn();
        }
    }
} 