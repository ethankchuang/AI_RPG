using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    // Cache for path visualization
    public List<HexTile> highlightedTiles = new List<HexTile>();
    public Color originalColor;

    [Header("Character Data")]
    public CharacterData characterData;

    [Header("Attacks")]
    public AttackSO basicAttack;
    public AttackSO skill1;
    public AttackSO skill2;

    [Header("Combat")]
    private AttackSO currentSelectedAttack = null;
    private List<HexTile> targetableTiles = new List<HexTile>();

    [Header("References")]
    private GameUI gameUI;

    [Header("Aggro")]
    public int aggroValue = 1; // How likely enemies are to target this player

    public enum PlayerState
    {
        Idle,
        Moving,
        Targeting
    }

    private PlayerState currentState = PlayerState.Idle;

    public override void Start()
    {
        // Apply character data if available
        if (characterData != null)
        {
            ApplyCharacterData();
        }
        else
        {
            // Set default player's movement range to 5 before base initialization
            movementRange = 5;
        }
        
        base.Start();
        RegisterWithManagers();
        
        // Find GameUI reference
        gameUI = FindObjectOfType<GameUI>();
        
        // Store the original color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    // Override this to prevent showing floating health bar for player
    // (player health is shown in the UI instead)
    /*
    protected override bool ShouldShowHealthBar()
    {
        return false;
    }
    */
    
    private void RegisterWithManagers()
    {
        // Register with GameManager
        if (gameManager != null && Unit.ActiveUnit == null)
        {
            Unit.ActiveUnit = this;
        }
        
        // Register with HexGridManager
        if (gridManager != null && gridManager.playerUnit == null)
        {
            gridManager.playerUnit = this;
        }
    }
    
    public override void Select()
    {
        if (currentState == PlayerState.Targeting)
        {
            // Get the mouse position in world space
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // First check if we clicked on any tile
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                HexTile clickedTile = hit.collider.GetComponent<HexTile>();
                if (clickedTile != null)
                {
                    // If we clicked a valid target tile
                    if (targetableTiles.Contains(clickedTile))
                    {
                        SelectTarget(clickedTile);
                        // Close combat UI after successful target selection
                        if (gameUI != null)
                        {
                            gameUI.CloseCombatUI();
                        }
                        return;
                    }
                }
            }

            // If we get here, we either clicked outside any tile or on a non-target tile
            CancelTargetSelection();
        }
        else if (currentState == PlayerState.Moving && IsPlayerTurn())
        {
            // Get the mouse position in world space
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // Check if we clicked on any tile
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                HexTile clickedTile = hit.collider.GetComponent<HexTile>();
                if (clickedTile != null && !highlightedTiles.Contains(clickedTile))
                {
                    // If we clicked outside the movement range, exit move mode
                    currentState = PlayerState.Idle;
                    HideMovementRange();
                }
            }
            else
            {
                // If we clicked outside any tile, exit move mode
                currentState = PlayerState.Idle;
                HideMovementRange();
            }
        }
    }
    
    public override void Deselect()
    {
        switch (currentState)
        {
            case PlayerState.Targeting:
                CancelTargetSelection();
                break;
            case PlayerState.Moving:
                HideMovementRange();
                currentState = PlayerState.Idle;
                break;
        }
    }
    
    public override void TakeDamage(int amount)
    {
        // Call the base class implementation first
        base.TakeDamage(amount);
        
        // Show UI message
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.ShowDamageMessage(amount);
        }
    }
    
    private bool IsPlayerTurn()
    {
        // This player's turn is when they are the active unit and game is in progress
        bool isTurn = Unit.ActiveUnit == this && gameManager != null && gameManager.CurrentState == GameState.InProgress;
        return isTurn;
    }
    
    public void ToggleMoveMode()
    {
        // Only allow toggling move mode during player's turn
        if (!IsPlayerTurn()) return;

        // If we're already in move mode, just exit it
        if (currentState == PlayerState.Moving)
        {
            HandleUIButtonClick();
            return;
        }
        
        // Clear any existing state first
        HandleUIButtonClick();
        
        // Then start movement mode
        currentState = PlayerState.Moving;
        isInMoveMode = true; // SYNC WITH BASE CLASS!
        if (remainingMovementPoints > 0)
        {
            ShowMovementRange();
        }
    }
    
        public void ShowMovementRange()
    {
        if (currentTile == null || remainingMovementPoints <= 0 || !IsPlayerTurn())
        {
            // Force exit move mode if it's not player's turn
            if (currentState == PlayerState.Moving)
            {
                currentState = PlayerState.Idle;
                HideMovementRange();
            }
            return;
        }

        List<HexTile> tilesInRange = GetTilesInRange(currentTile, remainingMovementPoints);
        
        // First reset all tiles
        foreach (HexTile tile in FindObjectsOfType<HexTile>())
            tile.ResetColor();
            
        // Clear previously highlighted tiles
        highlightedTiles.Clear();
        
        // Get reference to grid manager for checking occupied tiles
        HexGridManager gridManager = FindObjectOfType<HexGridManager>();
        foreach (HexTile tile in tilesInRange)
        {
            // Skip if tile is null or destroyed
            if (tile == null) continue;
            
            // Check if tile can be a valid destination
            bool canLandOnTile = true;
            
            if (gridManager != null)
            {
                Unit unitOnTile = gridManager.GetUnitOnTile(tile);
                if (unitOnTile != null)
                {
                    // Can't land on any unit (players or enemies)
                    canLandOnTile = false;
                }
            }
            else
            {
                // Fallback to simple checking in case grid manager is null
                foreach (Unit unit in FindObjectsOfType<Unit>())
                {
                    if (unit == this) continue; // Don't check against self
                    
                    float distance = Vector2.Distance(
                        new Vector2(tile.transform.position.x, tile.transform.position.y),
                        new Vector2(unit.transform.position.x, unit.transform.position.y)
                    );
                    
                    if (distance < 0.5f)
                    {
                        canLandOnTile = false;
                        break;
                    }
                }
            }
            
            // Only highlight if the tile is walkable, can be landed on, and we can find a valid path to it
            if (tile.isWalkable && canLandOnTile)
            {
                // Check if we can find a valid path to this tile
                List<HexTile> path = gridManager.CalculatePath(currentTile, tile);
                if (path != null && path.Count > 0 && path.Count - 1 <= remainingMovementPoints)
                {
                    tile.SetAsMovementRangeTile();
                    highlightedTiles.Add(tile);
                }
            }
        }
    }
    
    private void HideMovementRange()
    {
        foreach (HexTile tile in highlightedTiles)
        {
            if (tile != null)
                tile.ResetColor();
        }
        
        highlightedTiles.Clear();
    }
    
    public override void MoveAlongPath(List<HexTile> path)
    {
        //Debug.Log($"Player: MoveAlongPath called - Current state: {gameManager?.CurrentState}, IsPlayerTurn: {IsPlayerTurn()}");
        
        // Don't allow movement if it's not player's turn
        if (!IsPlayerTurn())
        {
            //Debug.Log("Player: Not player's turn, cancelling movement");
            // Force exit move mode if it's not player's turn
            if (currentState == PlayerState.Moving)
            {
                currentState = PlayerState.Idle;
                HideMovementRange();
            }
            return;
        }

        // Clear existing path
        currentPath.Clear();
        
        // Validate path and unit state
        if (path == null || path.Count < 2)
        {
            //Debug.LogError("MoveAlongPath: Invalid path (too short or null)");
            return;
        }
        
        if (isMoving || remainingMovementPoints <= 0 || currentState != PlayerState.Moving)
        {
            //Debug.LogError($"MoveAlongPath: Cannot move - isMoving: {isMoving}, remainingPoints: {remainingMovementPoints}, isInMoveMode: {currentState == PlayerState.Moving}");
            return;
        }
        
        // First, make sure we know what tile we're on
        UpdateCurrentTile();
        
        // Hide movement range
        HideMovementRange();
        
        // Store the verified path
        currentPath = new List<HexTile>(path);
        
        // Start the movement coroutine
        StartCoroutine(MoveAlongPathCoroutine());
    }
    
    private new IEnumerator MoveAlongPathCoroutine()
    {
        // Set flags
        isMoving = true;
        IsAnyUnitMoving = true;
        
        // Skip the first tile which is the start position
        for (int i = 1; i < currentPath.Count; i++)
        {
            // Get the next tile in the path
            HexTile nextTile = currentPath[i];
            
            // Calculate positions
            Vector3 startPos = transform.position;
            Vector3 targetPos = nextTile.transform.position;
            targetPos.z = startPos.z; // Preserve z-position to keep unit in front of map
            
            // Move at a fixed speed for consistency
            float distance = Vector3.Distance(startPos, targetPos);
            float actualMoveTime = distance / moveSpeed;
            float elapsed = 0f;
            
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
        
        // Reduce remaining movement points by the number of tiles moved
        // (excluding the starting tile)
        int tilesTraversed = currentPath.Count - 1;
        remainingMovementPoints -= tilesTraversed;
        if (remainingMovementPoints <= 0)
        {
            remainingMovementPoints = 0;
            hasMoved = true;
        }
        
        // Show updated movement range if we still have points left
        if (currentState == PlayerState.Moving && remainingMovementPoints > 0)
        {
            ShowMovementRange();
        }
    }
    
    public override void StartTurn()
    {
        // Reset to idle state at turn start
        currentState = PlayerState.Idle;
        isInMoveMode = false; // SYNC WITH BASE CLASS!
        
        // Ensure we know our current tile
        UpdateCurrentTile();
        
        base.StartTurn();
    }
    
    public override void EndTurn()
    {
        // Start coroutine to handle the delay before clearing active unit
        StartCoroutine(HandleTurnEnd());
    }
    
    private IEnumerator HandleTurnEnd()
    {   
        // Call base implementation to clear the turn flag and ActiveUnit
        base.EndTurn();
        
        // Force exit move mode and clear movement range
        currentState = PlayerState.Idle;
        isInMoveMode = false; // SYNC WITH BASE CLASS!
        HideMovementRange();
        
        // Start the next turn
        if (gameManager != null)
        {
            gameManager.StartNextTurn();
        }
        
        yield break; // Properly end the coroutine
    }
    
    private IEnumerator HandleConsecutiveTurn()
    {
        // Disable UI to show turn transition
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.SetUIEnabled(false);
        }
        
        // Wait for the visual transition
        yield return new WaitForSeconds(1.0f);
        
        // Start the turn first
        base.StartTurn();
        
        // Re-enable the UI after turn starts
        if (gameUI != null)
        {
            gameUI.SetUIEnabled(true);
        }
    }

    public void PerformBasicAttack(Unit target)
    {
        if (basicAttack != null && !hasAttacked)
        {
            // Check if we have enough SP for this attack using shared skill points
            if (gameManager.TryUseSkillPoints(basicAttack.SPCost))
            {
                basicAttack.Execute(this, target);
                hasAttacked = true; // Mark that we've attacked this turn
                // Re-enable main game UI
                if (gameUI != null)
                {
                    gameUI.gameObject.SetActive(true);
                    gameUI.SetUIEnabled(true);
                }
            }
        }
    }

    public void UseSkill1(Unit target)
    {
        if (skill1 == null || hasAttacked) return;
        if (!gameManager.TryUseSkillPoints(skill1.SPCost)) return;
        skill1.Execute(this, target);
        hasAttacked = true; // Mark that we've attacked this turn
        // Re-enable main game UI
        if (gameUI != null)
        {
            gameUI.gameObject.SetActive(true);
            gameUI.SetUIEnabled(true);
        }
    }

    public void UseSkill2(Unit target)
    {
        if (skill2 == null || hasAttacked) return;
        if (!gameManager.TryUseSkillPoints(skill2.SPCost)) return;
        skill2.Execute(this, target);
        hasAttacked = true; // Mark that we've attacked this turn
        // Re-enable main game UI
        if (gameUI != null)
        {
            gameUI.gameObject.SetActive(true);
            gameUI.SetUIEnabled(true);
        }
    }

    public void HandleUIButtonClick()
    {
        // Always clear movement mode first
        if (currentState == PlayerState.Moving)
        {
            HideMovementRange();
        }
        
        // Then handle any other state
        switch (currentState)
        {
            case PlayerState.Targeting:
                HideTargetableTiles();
                break;
        }
        currentState = PlayerState.Idle;
        isInMoveMode = false; // SYNC WITH BASE CLASS!
    }

    public void StartTargetSelection(AttackSO attack)
    {
        if (!IsPlayerTurn()) return;
        
        // Clear any existing state first
        HandleUIButtonClick();
        
        // Check if this attack requires targeting
        if (!attack.requiresTarget)
        {
            // Execute immediately for non-targeted attacks
            ExecuteNonTargetedAttack(attack);
            return;
        }
        
        // Then start new targeting for targeted attacks
        currentState = PlayerState.Targeting;
        currentSelectedAttack = attack;
        ShowTargetableTiles();
    }

    private void ShowTargetableTiles()
    {
        if (currentTile == null || currentSelectedAttack == null) return;
        
        // Clear previous highlights
        HideTargetableTiles();
        
        // Get tiles in range
        List<HexTile> tilesInRange = GetTilesInRange(currentTile, currentSelectedAttack.range);
        
        // First highlight all tiles in range
        foreach (HexTile tile in tilesInRange)
        {
            tile.SetAsMovementRangeTile(); // Use the same highlight as movement
            highlightedTiles.Add(tile);
        }
        
        // Then highlight tiles that contain enemies
        foreach (HexTile tile in tilesInRange)
        {
            Unit unitOnTile = gridManager.GetUnitOnTile(tile);
            if (unitOnTile != null && unitOnTile is Enemy)
            {
                tile.SetAsTargetableTile();
                targetableTiles.Add(tile);
            }
        }
    }

    private void HideTargetableTiles()
    {
        // Reset all highlighted tiles
        foreach (HexTile tile in highlightedTiles)
        {
            if (tile != null)
                tile.ResetColor();
        }
        highlightedTiles.Clear();
        targetableTiles.Clear();
    }

    private void CancelTargetSelection()
    {
        currentState = PlayerState.Idle;
        currentSelectedAttack = null;
        HideTargetableTiles();
    }

    public void SelectTarget(HexTile targetTile)
    {
        if (currentState != PlayerState.Targeting || currentSelectedAttack == null) return;
        
        Unit targetUnit = gridManager.GetUnitOnTile(targetTile);
        if (targetUnit != null && targetUnit is Enemy)
        {
            // Execute the attack
            if (currentSelectedAttack == basicAttack)
                PerformBasicAttack(targetUnit);
            else if (currentSelectedAttack == skill1)
                UseSkill1(targetUnit);
            else if (currentSelectedAttack == skill2)
                UseSkill2(targetUnit);
        }
        
        // Clean up
        CancelTargetSelection();
    }
    
    // Execute a non-targeted attack immediately
    private void ExecuteNonTargetedAttack(AttackSO attack)
    {
        if (attack == null || hasAttacked) return;
        
        // Check if we have enough SP for this attack
        if (!gameManager.TryUseSkillPoints(attack.SPCost)) return;
        
        // Execute the attack
        attack.ExecuteNonTargeted(this);
        hasAttacked = true; // Mark that we've attacked this turn
        
        Debug.Log($"{gameObject.name} used {attack.attackName} (non-targeted)");
        
        // Re-enable main game UI and close combat UI
        if (gameUI != null)
        {
            gameUI.CloseCombatUI();
        }
    }
    
    // Apply character data to this player
    public void ApplyCharacterData()
    {
        if (characterData == null) return;
        
        // Apply base stats
        maxHealth = characterData.maxHealth;
        currentHealth = maxHealth;
        attackDamage = characterData.attackDamage;
        speed = characterData.speed;
        movementRange = characterData.movementRange;
        
        // Apply aggro value
        aggroValue = characterData.aggroValue;
        
        // Apply attacks
        basicAttack = characterData.basicAttack;
        skill1 = characterData.skill1;
        skill2 = characterData.skill2;
        
        // Apply visual settings
        if (spriteRenderer != null)
        {
            if (characterData.characterSprite != null)
                spriteRenderer.sprite = characterData.characterSprite;
            
            spriteRenderer.color = characterData.characterColor;
            originalColor = characterData.characterColor;
        }
        
        // Update game object name for easier identification
        gameObject.name = $"Player_{characterData.characterName}";
    }
    
    // Get character type for UI and other systems
    public CharacterType GetCharacterType()
    {
        return characterData != null ? characterData.characterType : CharacterType.Warrior;
    }
    
    // Get character name for UI
    public string GetCharacterName()
    {
        return characterData != null ? characterData.characterName : "Unknown";
    }
    
    // Get character description for UI
    public string GetCharacterDescription()
    {
        return characterData != null ? characterData.description : "A mysterious fighter.";
    }
}
