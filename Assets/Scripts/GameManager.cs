using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // Add this for LINQ methods like Select

public enum GameState
{
    InitGame,
    InProgress,  // Individual unit turns (replaces PlayerTurn/EnemyTurn)
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
    
    [Header("Enemy Types")]
    public EnemyData[] availableEnemyTypes = new EnemyData[3];  // Light, Medium, Heavy
    
    [Header("Character Types")]
    public CharacterData[] availableCharacters = new CharacterData[4];
    public CharacterType[] selectedCharacterTypes = new CharacterType[3];
    
    [Header("Skill Point System")]
    [SerializeField] private int maxSkillPoints = 5;
    [SerializeField] private int currentSkillPoints = 3;
    
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
    
    #region Skill Point Management
    
    // Get current skill points
    public int GetCurrentSkillPoints()
    {
        return currentSkillPoints;
    }
    
    // Get maximum skill points
    public int GetMaxSkillPoints()
    {
        return maxSkillPoints;
    }
    
    // Try to use skill points (returns true if successful)
    public bool TryUseSkillPoints(int amount)
    {
        // Allow 0-cost attacks (like basic attacks) to always succeed
        if (amount == 0)
        {
            return true;
        }
        
        if (currentSkillPoints >= amount && amount > 0)
        {
            currentSkillPoints -= amount;
            string skillType = amount == 1 ? "Special Skill 1" : amount == 2 ? "Special Skill 2" : $"Skill (cost: {amount})";
    
            return true;
        }
        else
        {
            string skillType = amount == 1 ? "Special Skill 1" : amount == 2 ? "Special Skill 2" : $"Skill (cost: {amount})";
    
        }
        return false;
    }
    
    // Restore skill points (capped at maximum)
    public void RestoreSkillPoints(int amount)
    {
        if (amount > 0)
        {
            int previousPoints = currentSkillPoints;
            currentSkillPoints = Mathf.Min(currentSkillPoints + amount, maxSkillPoints);
            int actualRestored = currentSkillPoints - previousPoints;
            
            if (actualRestored > 0)
            {
    
            }
            else
            {
    
            }
        }
    }
    
    // Reset skill points to maximum (useful for new game or debugging)
    public void ResetSkillPoints()
    {
        currentSkillPoints = maxSkillPoints;

    }
    
    // Check if we have enough skill points
    public bool HasEnoughSkillPoints(int amount)
    {
        return currentSkillPoints >= amount;
    }
    
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
            
        // Initialize selected character types with defaults
        InitializeDefaultCharacterTypes();
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
                
            case GameState.InProgress:
                // Individual unit turns are now handled directly in StartNextTurn()
                // No need for separate player/enemy turn logic
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
    // End current player's turn (called from UI)
    public void EndPlayerTurn()
    {
        // Only allow ending turn if the active unit is a player
        if (!(Unit.ActiveUnit is Player player))
        {
            //Debug.LogWarning("Cannot end turn - active unit is not a player!");
            return;
        }

        // End the current player's turn (StartNextTurn will be called from the player's coroutine)
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
        
        // If it's a player unit, update grid manager reference for backward compatibility
        // (though the grid manager now uses Unit.ActiveUnit directly)
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
        // Find a good starting position (different for each player)
        HexTile startTile = FindPlayerStartTile(playerUnits.Count);
        
        // Place the player unit
        if (startTile != null && playerUnitPrefab != null)
        {
            GameObject unitObj = Instantiate(playerUnitPrefab, startTile.transform.position, Quaternion.identity);
            
            Player playerUnit = unitObj.GetComponent<Player>();
            if (playerUnit != null)
            {
                // Assign character data based on selected types
                if (playerUnits.Count < selectedCharacterTypes.Length)
                {
                    CharacterType selectedType = selectedCharacterTypes[playerUnits.Count];
                    CharacterData characterData = GetCharacterData(selectedType);
                    
                    if (characterData != null)
                    {
                        playerUnit.characterData = characterData;
                        // Force apply character data immediately after assignment
                        playerUnit.ApplyCharacterData();
                    }
                }
                
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
            
        // Get eligible tiles for enemy placement (top of grid) that are not occupied
        List<HexTile> placementTiles = new List<HexTile>();
        foreach (HexTile tile in gridGenerator.GetComponentsInChildren<HexTile>())
        {
            if (tile.row >= gridGenerator.gridHeight - 2 && tile.isWalkable && !IsUnitOnTile(tile))
                placementTiles.Add(tile);
        }
        
        // If no tiles are available in the top rows, expand search to more rows
        if (placementTiles.Count == 0)
        {
            foreach (HexTile tile in gridGenerator.GetComponentsInChildren<HexTile>())
            {
                if (tile.row >= gridGenerator.gridHeight - 4 && tile.isWalkable && !IsUnitOnTile(tile))
                    placementTiles.Add(tile);
            }
        }
        
        // Last resort: any available walkable tile
        if (placementTiles.Count == 0)
        {
            foreach (HexTile tile in gridGenerator.GetComponentsInChildren<HexTile>())
            {
                if (tile.isWalkable && !IsUnitOnTile(tile))
                    placementTiles.Add(tile);
            }
        }
        
        // Randomize placement order
        if (placementTiles.Count > 0)
        {
            System.Random random = new System.Random();
            for (int i = 0; i < placementTiles.Count; i++)
            {
                int j = random.Next(i, placementTiles.Count);
                HexTile temp = placementTiles[i];
                placementTiles[i] = placementTiles[j];
                placementTiles[j] = temp;
            }
            
            // Place enemy unit on the first available tile
            HexTile placementTile = placementTiles[0];
            GameObject unitObj = Instantiate(enemyUnitPrefab, placementTile.transform.position, Quaternion.identity);
            unitObj.name = $"EnemyUnit_{enemyUnits.Count}";
            
            Enemy enemyUnit = unitObj.GetComponent<Enemy>();
            if (enemyUnit != null)
            {
                // Assign random enemy data
                AssignRandomEnemyData(enemyUnit);
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
        else
        {
            Debug.LogWarning($"GameManager: Could not find any available tiles to place enemy unit {enemyUnits.Count}!");
        }
    }
    
    // Find a good starting tile for the player (different positions for each player)
    private HexTile FindPlayerStartTile(int playerIndex)
    {
        HexTile[] allTiles = gridGenerator.GetComponentsInChildren<HexTile>();
        
        // Get center column and try different positions for each player
        int centerCol = gridGenerator.gridWidth / 2;
        
        // Define starting positions relative to center
        int[] columnOffsets = { 0, -1, 1, -2, 2 }; // Center, left, right, far left, far right
        int[] rowOffsets = { 0, 0, 0, 0, 0 };      // All on bottom row initially
        
        // Try the designated position for this player index
        if (playerIndex < columnOffsets.Length)
        {
            int targetCol = centerCol + columnOffsets[playerIndex];
            int targetRow = rowOffsets[playerIndex];
            
            foreach (HexTile tile in allTiles)
            {
                if (tile.column == targetCol && tile.row == targetRow && tile.isWalkable && !IsUnitOnTile(tile))
                    return tile;
            }
        }
        
        // If designated position is not available, try any walkable tile in the bottom rows
        for (int row = 0; row < 3; row++) // Check bottom 3 rows
        {
            for (int col = 0; col < gridGenerator.gridWidth; col++)
            {
                foreach (HexTile tile in allTiles)
                {
                    if (tile.column == col && tile.row == row && tile.isWalkable && !IsUnitOnTile(tile))
                        return tile;
                }
            }
        }
        
        // Last resort: any walkable tile
        foreach (HexTile tile in allTiles)
        {
            if (tile.isWalkable && !IsUnitOnTile(tile))
                return tile;
        }
        
        return null;
    }
    
    // Check if a unit is already on a tile
    private bool IsUnitOnTile(HexTile tile)
    {
        if (gridManager != null)
            return gridManager.IsUnitOnTile(tile);
            
        // Fallback check
        Collider2D[] colliders = Physics2D.OverlapPointAll(tile.transform.position);
        foreach (Collider2D collider in colliders)
        {
            if (collider.GetComponent<Unit>() != null)
                return true;
        }
        return false;
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
        
        // Set game state to InProgress (handles both player and enemy turns)
        if (CurrentState != GameState.InProgress)
        {
            SetGameState(GameState.InProgress);
        }
        
        // Update UI to reflect the current active unit (player or enemy)
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.UpdateUI(CurrentState);
        }
        
        // Handle the specific unit type
        if (nextUnit is Player)
        {
            //Debug.Log("StartNextTurn: Starting individual player turn");
            // Call OnTurnStart to handle consecutive turn detection
            nextUnit.OnTurnStart();
        }
        else if (nextUnit is Enemy)
        {
            //Debug.Log("StartNextTurn: Starting enemy turn");
            Enemy currentEnemy = nextUnit as Enemy;
            currentEnemy.ExecuteTurn();
            
            // Start coroutine to wait for enemy turn completion
            StartCoroutine(WaitForEnemyTurn(currentEnemy));
        }
        
        //Debug.Log("StartNextTurn: Completed");
    }
    
    // Get character data for a given character type
    private CharacterData GetCharacterData(CharacterType characterType)
    {
        foreach (CharacterData data in availableCharacters)
        {
            if (data != null && data.characterType == characterType)
                return data;
        }
        return null;
    }
    
    // Set the selected character types (can be called from UI)
    public void SetSelectedCharacterTypes(CharacterType[] types)
    {
        if (types.Length <= selectedCharacterTypes.Length)
        {
            for (int i = 0; i < types.Length; i++)
            {
                selectedCharacterTypes[i] = types[i];
            }
        }
    }
    
    // Get available character types
    public CharacterType[] GetAvailableCharacterTypes()
    {
        List<CharacterType> types = new List<CharacterType>();
        foreach (CharacterData data in availableCharacters)
        {
            if (data != null)
                types.Add(data.characterType);
        }
        return types.ToArray();
    }
    
    // Initialize default character selection
    private void InitializeDefaultCharacterTypes()
    {
        // Default setup: first 3 character types or fallback to Warrior
        for (int i = 0; i < selectedCharacterTypes.Length; i++)
        {
            if (i < 4) // We have 4 character types
            {
                selectedCharacterTypes[i] = (CharacterType)i;
            }
            else
            {
                selectedCharacterTypes[i] = CharacterType.Warrior; // Fallback
            }
        }
        

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
    
    // Assign random enemy data to an enemy unit
    private void AssignRandomEnemyData(Enemy enemy)
    {
        if (enemy == null || availableEnemyTypes == null || availableEnemyTypes.Length == 0)
            return;
        
        // Filter out null enemy data
        List<EnemyData> validEnemyTypes = new List<EnemyData>();
        foreach (EnemyData data in availableEnemyTypes)
        {
            if (data != null)
                validEnemyTypes.Add(data);
        }
        
        if (validEnemyTypes.Count == 0)
            return;
        
        // Select random enemy type
        EnemyData selectedData = validEnemyTypes[Random.Range(0, validEnemyTypes.Count)];
        enemy.enemyData = selectedData;
        
        // Force apply the data immediately
        enemy.ApplyEnemyData();
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