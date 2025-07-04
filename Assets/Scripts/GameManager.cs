using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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
            Debug.Log("GameManager: Initialized as singleton");
        }
        else
        {
            Debug.Log("GameManager: Another instance found, destroying duplicate");
            // If we're destroying a duplicate, it means we're returning to combat
            // Trigger reset on the persistent instance
            if (Instance != this)
            {
                Debug.Log("GameManager: Triggering reset on persistent instance");
                Instance.ResetGameState();
            }
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
        Debug.Log("GameManager: Start() called");
        
        // Setup camera bounds
        if (cameraFollow != null && gridGenerator != null)
        {
            cameraFollow.SetBoundsFromGrid(
                gridGenerator.gridWidth,
                gridGenerator.gridHeight,
                gridGenerator.hexRadius
            );
        }
        
        // Start in InitGame state for fresh game
        SetGameState(GameState.InitGame);
        
        // Find all units in the scene
        FindAllUnits();
        
        // Start the first turn using action value system
        StartNextTurn();
    }
    
    private void Update()
    {
        // Check for tilde key (~) to automatically win the battle
        if (Input.GetKeyDown(KeyCode.Tilde) || Input.GetKeyDown(KeyCode.BackQuote))
        {
            Debug.Log("Tilde key pressed - Auto-winning battle!");
            ForceVictory();
        }
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
        // Start the initialization process as a coroutine to handle timing
        StartCoroutine(InitializeGameCoroutine());
    }
    
    private IEnumerator InitializeGameCoroutine()
    {
        // Make sure the grid is generated
        if (gridGenerator != null && gridGenerator.transform.childCount == 0)
            gridGenerator.GenerateGrid();
        
        // Wait for map generation to complete
        RandomMapGenerator mapGenerator = FindObjectOfType<RandomMapGenerator>();
        if (mapGenerator != null)
        {
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForEndOfFrame();
        }
        
        // Clear any existing units
        playerUnits.Clear();
        enemyUnits.Clear();
        genericUnits.Clear();
        
        // Place units after map generation is complete
        PlaceInitialUnits(true);  // Player units
        PlaceInitialUnits(false); // Enemy units
        
        // Set up camera and references
        SetupInitialReferences();
        
        // Final validation to catch any remaining issues
        StartCoroutine(PostMapGenerationValidation());
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
        
        // Start the first turn after initialization
        StartNextTurn();
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
        player.EndTurn();
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
        
        // Note: Win condition checking is now handled by CheckBattleEndConditions() in OnUnitDeath()
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
        
        // Additional safety check to ensure we never spawn in walls
        if (startTile != null && !IsValidPlacementTile(startTile))
        {
            startTile = FindAlternativePlayerStartTile(playerUnits.Count);
        }
        
        // Place the player unit
        if (startTile != null && playerUnitPrefab != null)
        {
            // Final validation before spawning
            if (!IsValidPlacementTile(startTile))
            {
                return;
            }
            
            Vector3 spawnPosition = startTile.transform.position;
            spawnPosition.z = -1f; // Ensure unit is in front of the map
            GameObject unitObj = Instantiate(playerUnitPrefab, spawnPosition, Quaternion.identity);
            
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
            if (tile.row >= gridGenerator.gridHeight - 2 && IsValidPlacementTile(tile))
                placementTiles.Add(tile);
        }
        
        // If no tiles are available in the top rows, expand search to more rows
        if (placementTiles.Count == 0)
        {
            foreach (HexTile tile in gridGenerator.GetComponentsInChildren<HexTile>())
            {
                if (tile.row >= gridGenerator.gridHeight - 4 && IsValidPlacementTile(tile))
                    placementTiles.Add(tile);
            }
        }
        
        // Last resort: any available valid tile
        if (placementTiles.Count == 0)
        {
            foreach (HexTile tile in gridGenerator.GetComponentsInChildren<HexTile>())
            {
                if (IsValidPlacementTile(tile))
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
            
            // Final safety check before spawning
            if (!IsValidPlacementTile(placementTile))
            {
                return;
            }
            
            Vector3 spawnPosition = placementTile.transform.position;
            spawnPosition.z = -1f; // Ensure unit is in front of the map
            GameObject unitObj = Instantiate(enemyUnitPrefab, spawnPosition, Quaternion.identity);
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
                if (tile.column == targetCol && 
                    tile.row == targetRow && 
                    IsValidPlacementTile(tile))
                    return tile;
            }
        }
        
        // If designated position is not available, try any valid tile in the bottom rows
        for (int row = 0; row < 3; row++) // Check bottom 3 rows
        {
            for (int col = 0; col < gridGenerator.gridWidth; col++)
            {
                foreach (HexTile tile in allTiles)
                {
                    if (tile.column == col && 
                        tile.row == row && 
                        IsValidPlacementTile(tile))
                        return tile;
                }
            }
        }
        
        // Last resort: any valid tile
        foreach (HexTile tile in allTiles)
        {
            if (IsValidPlacementTile(tile))
                return tile;
        }
        
        return null;
    }
    
    // Find an alternative starting tile for the player if the primary one is blocked
    private HexTile FindAlternativePlayerStartTile(int playerIndex)
    {
        HexTile[] allTiles = gridGenerator.GetComponentsInChildren<HexTile>();
        
        // Try any valid tile in the bottom 5 rows
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < gridGenerator.gridWidth; col++)
            {
                foreach (HexTile tile in allTiles)
                {
                    if (tile.column == col && 
                        tile.row == row && 
                        IsValidPlacementTile(tile))
                        return tile;
                }
            }
        }
        
        // Last resort: any valid tile
        foreach (HexTile tile in allTiles)
        {
            if (IsValidPlacementTile(tile))
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
    
    // Check if a tile is a wall (either by type or walkable property)
    private bool IsWallTile(HexTile tile)
    {
        if (tile == null)
            return false;
            
        // Check if it's explicitly a WallTile type
        if (tile.GetType() == typeof(WallTile))
            return true;
            
        // Check if it's not walkable (walls should not be walkable)
        if (!tile.isWalkable || !tile.IsWalkable())
            return true;
            
        // Check if the tile type is Wall
        if (tile.tileType == TileType.Wall)
            return true;
            
        return false;
    }
    
    // Check if a tile is valid for unit placement
    private bool IsValidPlacementTile(HexTile tile)
    {
        if (tile == null)
            return false;
            
        // Must be walkable
        if (!tile.isWalkable || !tile.IsWalkable())
            return false;
            
        // Must not be a wall
        if (IsWallTile(tile))
            return false;
            
        // Must not have a unit on it
        if (IsUnitOnTile(tile))
            return false;
            
        return true;
    }
    
    // Validate and fix unit placements after map generation completes
    private IEnumerator PostMapGenerationValidation()
    {
        // Wait a frame to ensure map generation has completed
        yield return null;
        
        // Check all units and relocate any that are on walls
        List<Unit> unitsToRelocate = new List<Unit>();
        
        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;
            
            HexTile unitTile = FindTileAtPosition(unit.transform.position);
            if (unitTile != null && IsWallTile(unitTile))
            {
                unitsToRelocate.Add(unit);
            }
        }
        
        // Relocate units that are on walls
        foreach (Unit unit in unitsToRelocate)
        {
            RelocateUnitFromWall(unit);
        }
        
        ValidateUnitPlacements();
    }
    
    // Relocate a unit that is currently on a wall tile
    private void RelocateUnitFromWall(Unit unit)
    {
        if (unit == null) return;
        
        // Find a valid placement tile
        HexTile newTile = FindValidPlacementTileForUnit(unit);
        
        if (newTile != null)
        {
            // Move the unit to the new position
            Vector3 newPosition = newTile.transform.position;
            newPosition.z = unit.transform.position.z; // Preserve z-position
            unit.transform.position = newPosition;
        }
        else
        {
            Destroy(unit.gameObject);
        }
    }
    
    // Validate that no units are currently on walls
    private void ValidateUnitPlacements()
    {
        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;
            
            HexTile unitTile = FindTileAtPosition(unit.transform.position);
            if (unitTile != null)
            {
                if (IsWallTile(unitTile))
                {
                    // Optionally, handle this case if needed
                }
                else if (!unitTile.isWalkable || !unitTile.IsWalkable())
                {
                    // Optionally, handle this case if needed
                }
            }
        }
    }
    
    // Find the tile at a given world position
    private HexTile FindTileAtPosition(Vector3 worldPosition)
    {
        if (gridGenerator == null) return null;
        
        HexTile[] allTiles = gridGenerator.GetComponentsInChildren<HexTile>();
        HexTile closestTile = null;
        float closestDistance = float.MaxValue;
        
        foreach (HexTile tile in allTiles)
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
        
        // Only return the tile if the unit is close enough to it
        return closestDistance < 0.5f ? closestTile : null;
    }
    #endregion
    
    #region Game End States
    // Handle victory
    private void HandleVictory()
    {
        Debug.Log("Victory! All enemies defeated!");
        
        // Find and show victory screen
        BattleResultUI battleResultUI = FindObjectOfType<BattleResultUI>();
        if (battleResultUI != null)
        {
            battleResultUI.ShowVictoryScreen();
        }
        else
        {
            Debug.LogWarning("BattleResultUI not found! Cannot show victory screen.");
        }
        
        // Don't automatically return to story - let the continue button handle it
    }
    
    // Handle defeat
    private void HandleDefeat()
    {
        Debug.Log("Defeat! All players defeated!");
        
        // Find and show defeat screen
        BattleResultUI battleResultUI = FindObjectOfType<BattleResultUI>();
        if (battleResultUI != null)
        {
            battleResultUI.ShowDefeatScreen();
        }
        else
        {
            Debug.LogWarning("BattleResultUI not found! Cannot show defeat screen.");
        }
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
            // Call StartTurn to handle consecutive turn detection
            nextUnit.StartTurn();
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
        enemy.EndTurn();
        
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
        // Remove the unit from the appropriate lists
        allUnits.Remove(unit);
        
        if (unit is Player)
        {
            playerUnits.Remove(unit as Player);
            Debug.Log($"Player {unit.name} died. Remaining players: {playerUnits.Count}");
        }
        else if (unit is Enemy)
        {
            enemyUnits.Remove(unit as Enemy);
            Debug.Log($"Enemy {unit.name} died. Remaining enemies: {enemyUnits.Count}");
        }
        
        // If it was the current active unit, end the turn
        if (unit == currentActiveUnit)
        {
            EndCurrentTurn();
        }
        
        // Check win/loss conditions after removing the unit
        CheckBattleEndConditions();
    }
    
    private void CheckBattleEndConditions()
    {
        // Don't check conditions if game is already over
        if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat)
            return;
            
        // Count living units of each type
        int livingPlayers = 0;
        int livingEnemies = 0;
        
        foreach (Unit unit in allUnits)
        {
            if (unit is Player)
                livingPlayers++;
            else if (unit is Enemy)
                livingEnemies++;
        }
        
        // Check victory condition (all enemies dead)
        if (livingEnemies == 0 && livingPlayers > 0)
        {
            SetGameState(GameState.Victory);
            HandleVictory();
        }
        // Check defeat condition (all players dead)
        else if (livingPlayers == 0)
        {
            SetGameState(GameState.Defeat);
            HandleDefeat();
        }
    }
    
    // Find a valid placement tile for a specific unit
    private HexTile FindValidPlacementTileForUnit(Unit unit)
    {
        if (unit == null) return null;
        
        HexTile[] allTiles = gridGenerator.GetComponentsInChildren<HexTile>();
        
        // For player units, prefer bottom rows
        if (unit is Player)
        {
            // Try bottom 3 rows first
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < gridGenerator.gridWidth; col++)
                {
                    foreach (HexTile tile in allTiles)
                    {
                        if (tile.column == col && 
                            tile.row == row && 
                            IsValidPlacementTile(tile))
                            return tile;
                    }
                }
            }
        }
        // For enemy units, prefer top rows
        else if (unit is Enemy)
        {
            // Try top 3 rows first
            for (int row = gridGenerator.gridHeight - 3; row < gridGenerator.gridHeight; row++)
            {
                for (int col = 0; col < gridGenerator.gridWidth; col++)
                {
                    foreach (HexTile tile in allTiles)
                    {
                        if (tile.column == col && 
                            tile.row == row && 
                            IsValidPlacementTile(tile))
                            return tile;
                    }
                }
            }
        }
        
        // Fallback: any valid tile
        foreach (HexTile tile in allTiles)
        {
            if (IsValidPlacementTile(tile))
                return tile;
        }
        
        return null;
    }
    
    // Force victory for debugging/testing purposes
    public void ForceVictory()
    {
        if (CurrentState == GameState.InProgress)
        {
            Debug.Log("GameManager: Force victory triggered!");
            SetGameState(GameState.Victory);
            HandleVictory();
        }
    }

    // Reset the game state when returning to combat
    private void ResetGameState()
    {
        Debug.Log("GameManager: ResetGameState() called");
        
        // Clear all unit lists
        playerUnits.Clear();
        enemyUnits.Clear();
        genericUnits.Clear();
        allUnits.Clear();
        Debug.Log("GameManager: Cleared all unit lists");
        
        // Reset static references
        Unit.ActiveUnit = null;
        Unit.LastActiveUnit = null;
        Debug.Log("GameManager: Reset static unit references");
        
        // Reset game state
        CurrentState = GameState.InitGame;
        Debug.Log("GameManager: Reset game state to InitGame");
        
        // Reset skill points
        ResetSkillPoints();
        Debug.Log("GameManager: Reset skill points");
        
        // Regenerate the grid if needed
        if (gridGenerator != null)
        {
            Debug.Log("GameManager: Regenerating grid");
            gridGenerator.GenerateGrid();
        }
        else
        {
            Debug.Log("GameManager: gridGenerator is null!");
        }
        
        // Regenerate random map if RandomMapGenerator exists
        RandomMapGenerator mapGenerator = FindObjectOfType<RandomMapGenerator>();
        if (mapGenerator != null)
        {
            Debug.Log("GameManager: Regenerating random map");
            mapGenerator.GenerateRandomMap();
        }
        else
        {
            Debug.Log("GameManager: RandomMapGenerator not found");
        }
        
        // Re-find references after grid regeneration
        if (gridGenerator == null)
        {
            gridGenerator = FindObjectOfType<HexGridGenerator>();
            Debug.Log($"GameManager: Re-found gridGenerator: {gridGenerator != null}");
        }
            
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<HexGridManager>();
            Debug.Log($"GameManager: Re-found gridManager: {gridManager != null}");
        }
            
        if (cameraFollow == null)
        {
            cameraFollow = FindObjectOfType<CameraFollow>();
            Debug.Log($"GameManager: Re-found cameraFollow: {cameraFollow != null}");
        }
        
        // Refresh tile neighbors after grid regeneration
        if (gridManager != null)
        {
            Debug.Log("GameManager: Refreshing tile neighbors");
            gridManager.RefreshTileNeighbors();
        }
        
        // Setup camera bounds after grid regeneration
        if (cameraFollow != null && gridGenerator != null)
        {
            Debug.Log("GameManager: Setting up camera bounds");
            cameraFollow.SetBoundsFromGrid(
                gridGenerator.gridWidth,
                gridGenerator.gridHeight,
                gridGenerator.hexRadius
            );
        }
        
        // Start fresh initialization
        Debug.Log("GameManager: Starting fresh initialization");
        SetGameState(GameState.InitGame);
        
        // Find all units in the scene
        Debug.Log("GameManager: Finding all units");
        FindAllUnits();
        
        // Start the first turn using action value system
        Debug.Log("GameManager: Starting next turn");
        StartNextTurn();
        
        Debug.Log("GameManager: ResetGameState() completed");
    }
} 