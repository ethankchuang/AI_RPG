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
    public GameState CurrentState = GameState.InitGame;
    private List<Player> playerUnits = new List<Player>();
    private List<Enemy> enemyUnits = new List<Enemy>();
    private List<Unit> genericUnits = new List<Unit>(); // For any units that don't inherit from Player/Enemy
    
    // Turn order management
    private List<Unit> allUnits = new List<Unit>();
    private Unit currentActiveUnit;
    
    // Properties
    #endregion
    
    // Public method to get all units
    public List<Unit> GetAllUnits()
    {
        if (allUnits == null || allUnits.Count == 0)
        {
            //Debug.LogWarning("Units list is empty, finding all units...");
            FindAllUnits();
        }
        return new List<Unit>(allUnits);
    }
    
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
        
        // Start in InitGame state
        SetGameState(GameState.InitGame);
        
        // Find all units in the scene
        FindAllUnits();
        
        // Start the first turn using action value system
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
        else
        {
            //Debug.LogError("GameManager: Could not find GameUI component!");
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
                // Enemy turns are now handled directly in StartNextTurn()
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
    }
    
    // Setup initial references including camera and grid manager
    private void SetupInitialReferences()
    {
        // Make sure we have a player unit
        if (playerUnits.Count > 0)
        {
            Unit.ActiveUnit = playerUnits[0];
            
            // Share the reference with grid manager
            if (gridManager != null)
                gridManager.playerUnit = playerUnits[0];
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
            Unit.ActiveUnit = null;
    }
    
    // End player turn (called from UI)
    public void EndPlayerTurn()
    {
        if (CurrentState != GameState.PlayerTurn)
        {
            //Debug.LogWarning($"Cannot end player turn in state {CurrentState}");
            return;
        }

        // Find the player unit
        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            //Debug.LogWarning("No player found!");
            return;
        }

        // End the player's turn (StartNextTurn will be called from the player's coroutine)
        player.OnTurnEnd();
    }
    

    #endregion
    
    #region Unit Management
    // Update the active unit (used for pathfinding and camera tracking)
    public void SelectUnit(Unit unit)
    {
        // No need to do anything if it's the same unit
        if (Unit.ActiveUnit == unit)
            return;
            
        // Clear previous unit's movement range
        if (Unit.ActiveUnit != null)
            Unit.ActiveUnit.Deselect();
        
        // Update active unit
        Unit.ActiveUnit = unit;
        
        // If it's a player unit, update grid manager reference
        if (unit is Player playerUnit && gridManager != null)
            gridManager.playerUnit = playerUnit;
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
                if (Unit.ActiveUnit == null)
                    Unit.ActiveUnit = playerUnit; // This is the active unit for pathfinding
            }
            else
            {
                // Fall back to using Unit if Player component isn't found
                Unit unit = unitObj.GetComponent<Unit>();
                if (unit != null)
                {
                    genericUnits.Add(unit);
                    if (Unit.ActiveUnit == null)
                        Unit.ActiveUnit = unit;
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
    }
    
    public void StartNextTurn()
    {
        //Debug.Log("StartNextTurn: Beginning turn calculation");
        
        // Make sure we have all units
        FindAllUnits();
        //Debug.Log($"StartNextTurn: Found {allUnits.Count} units");
        
        // Find the minimum action value needed to reach threshold
        int leastActionNeeded = int.MaxValue;
        
        foreach (Unit unit in allUnits)
        {
            int threshold = 100 - unit.speed;
            int actionNeeded = threshold - unit.actionValue;
            
            //Debug.Log($"{unit.gameObject.name}: Action Value: {unit.actionValue}, Threshold: {threshold}, Action Needed: {actionNeeded}");
            
            if (actionNeeded < leastActionNeeded)
            {
                leastActionNeeded = actionNeeded;
            }
        }
        
        // Find all units that need the least action (tied units)
        List<Unit> tiedUnits = new List<Unit>();
        foreach (Unit unit in allUnits)
        {
            int threshold = 100 - unit.speed;
            int actionNeeded = threshold - unit.actionValue;
            
            if (actionNeeded == leastActionNeeded)
            {
                tiedUnits.Add(unit);
            }
        }
        
        // Use priority to break ties
        Unit nextUnit = null;
        int highestPriority = int.MinValue;
        
        foreach (Unit unit in tiedUnits)
        {
            if (unit.priority > highestPriority)
            {
                highestPriority = unit.priority;
                nextUnit = unit;
            }
        }
        
        if (nextUnit == null)
        {
            //Debug.LogWarning("No units found!");
            return;
        }
        
        //Debug.Log($"StartNextTurn: Selected {nextUnit.gameObject.name} as next unit (Priority: {nextUnit.priority})");
        //if (tiedUnits.Count > 1)
        //{
        //    Debug.Log($"Tie-breaker used: {tiedUnits.Count} units tied, {nextUnit.gameObject.name} won with priority {nextUnit.priority}");
        //}
        
        // Calculate how much action value to distribute to other units (only the amount this unit actually needed)
        int nextUnitThreshold = 100 - nextUnit.speed;
        int actionToDistribute = nextUnitThreshold - nextUnit.actionValue;
        
        //Debug.Log($"Next Turn: {nextUnit.gameObject.name} (Speed: {nextUnit.speed}, Action Value: {nextUnit.actionValue}, Threshold: {nextUnitThreshold})");
        //Debug.Log($"Distributing {actionToDistribute} action value to other units");
        
        // Reset the active unit's action value to 0
        nextUnit.actionValue = 0;
        
        // Distribute the action value to all other units (only the amount the active unit needed)
        foreach (Unit unit in allUnits)
        {
            if (unit != nextUnit)
            {
                unit.actionValue += actionToDistribute;
                //Debug.Log($"{unit.gameObject.name} new action value: {unit.actionValue}");
            }
        }
        
        // Set the current active unit
        Unit.ActiveUnit = nextUnit;
        //Debug.Log($"StartNextTurn: Set {nextUnit.gameObject.name} as ActiveUnit");
        
        // Update game state based on unit type
        if (nextUnit is Player)
        {
            //Debug.Log("StartNextTurn: Starting player turn");
            SetGameState(GameState.PlayerTurn);
            // Call OnTurnStart to handle consecutive turn detection
            nextUnit.OnTurnStart();
        }
        else if (nextUnit is Enemy)
        {
            CurrentState = GameState.EnemyTurn;
            
            // Update UI to show enemy turn (should disable UI)
            GameUI gameUI = FindObjectOfType<GameUI>();
            if (gameUI != null)
            {
                gameUI.UpdateUI(CurrentState);
            }
            
            Enemy currentEnemy = nextUnit as Enemy;
            currentEnemy.ExecuteTurn();
            
            // Start coroutine to wait for enemy turn completion
            StartCoroutine(WaitForEnemyTurn(currentEnemy));
        }
        
        //Debug.Log("StartNextTurn: Completed");
    }
    
    private IEnumerator WaitForEnemyTurn(Enemy enemy)
    {
        // Wait for the enemy to complete their turn
        while (!enemy.IsTurnComplete())
        {
            yield return null; // Wait one frame
        }
        
        // End their turn (this will clear ActiveUnit)
        enemy.OnTurnEnd();
        
        // Check for defeat condition
        if (playerUnits.Count == 0)
        {
            SetGameState(GameState.Defeat);
            yield break;
        }
        
        // Start the next turn
        StartNextTurn();
    }
    
    public void EndCurrentTurn()
    {
        if (currentActiveUnit == null)
        {
            //Debug.LogWarning("GameManager: No active unit to end turn for!");
            return;
        }
        
        //Debug.Log($"GameManager: Ending turn for {currentActiveUnit.gameObject.name}");
        
        // Clear the current active unit reference but NOT the static ActiveUnit
        currentActiveUnit = null;
        
        // Start the next turn
        StartNextTurn();
    }
    
    public void OnUnitDeath(Unit unit)
    {
        // Remove the unit from the list
        allUnits.Remove(unit);
        
        // If it was the current active unit, end the turn
        if (unit == currentActiveUnit)
        {
            EndCurrentTurn();
        }
    }
} 