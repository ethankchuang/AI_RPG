using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public Button endTurnButton;
    public Button moveButton;
    public Button attackButton;
    public Button bagButton;
    public CanvasGroup actionButtonsGroup; // Add this to control opacity of all action buttons
    public CombatUI combatUI; // Reference to the combat UI
    
    [Header("Skill Point Display")]
    public Transform skillPointIndicators; // Parent transform containing 5 skill point visual indicators (sprites)
    
    [Header("UI Settings")]
    public float disabledOpacity = 0.3f; // Opacity when buttons are disabled
    public float enabledOpacity = 1.0f;  // Opacity when buttons are enabled
    
    [Header("Health Bar")]
    public Slider healthBar;             // The slider component (optional)
    public Image directFillImage;        // Direct reference to fill image (optional)
    public TextMeshProUGUI healthText;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    
    [Header("Button Colors")]
    public Color waitingColor = Color.yellow;
    public Color readyColor = Color.green;
    public Color activeModeColor = Color.cyan;
    public Color inactiveModeColor = Color.white;
    
    // Reference to the player
    private GameManager gameManager;
    private EventSystem eventSystem;
    private Player playerUnit;
    
    // Flag to temporarily override UI state during consecutive turn transitions
    private bool isTemporarilyDisabled = false;
    
    // Track last UI state to avoid unnecessary updates in Update method
    private bool lastUIState = true;
    
    private void Awake()
    {
        // Make sure we have an EventSystem
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            //Debug.LogWarning("No EventSystem found in the scene. Creating one...");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        
        // Find or create CanvasGroup for action buttons
        if (actionButtonsGroup == null)
        {
            // Try to find existing CanvasGroup
            actionButtonsGroup = GetComponent<CanvasGroup>();
            if (actionButtonsGroup == null)
            {
                // Create new CanvasGroup
                actionButtonsGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Ensure we have a CanvasGroup
        if (actionButtonsGroup == null)
        {
            //Debug.LogError("GameUI: Failed to create or find CanvasGroup!");
        }
        else
        {
            // Initialize CanvasGroup
            actionButtonsGroup.alpha = 1f;
            actionButtonsGroup.interactable = true;
            actionButtonsGroup.blocksRaycasts = true;
        }
    }
    
    private void Start()
    {
        // Find GameManager reference
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        if (gameManager == null)
        {
            //Debug.LogError("GameUI: Could not find GameManager!");
        }
        
        // Don't initialize UI state here - let GameManager handle it when it's ready
        // The GameManager will call UpdateUI() when it properly initializes
        
        // Setup button listeners
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }
        
        if (moveButton != null)
        {
            moveButton.onClick.AddListener(OnMoveButtonClicked);
        }
        
        if (attackButton != null)
        {
            attackButton.onClick.AddListener(OnAttackButtonClicked);
        }
        
        if (bagButton != null)
        {
            bagButton.onClick.AddListener(OnBagButtonClicked);
        }
        
        // Find CombatUI if not assigned
        if (combatUI == null)
        {
            combatUI = FindObjectOfType<CombatUI>();
        }
    }
    
    private void Update()
    {
        // Make sure GameManager reference is set
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
                gameManager = FindObjectOfType<GameManager>();
        }
        
        // Update button interactability based on active unit
        if (gameManager != null)
        {
            // Only enable buttons if the active unit is a player AND not temporarily disabled
            bool shouldBeEnabled = Unit.ActiveUnit is Player && !isTemporarilyDisabled;
            
            // Use centralized function but only update if there's a change to avoid constant updates
            if (shouldBeEnabled != lastUIState)
            {
                SetUIEnabled(shouldBeEnabled);
                lastUIState = shouldBeEnabled;
            }
        }
        
        // Update button colors
        UpdateEndTurnButtonColor();
        UpdateMoveButtonColor();
        UpdateAttackButtonColor();
        UpdateBagButtonColor();
        
        // Update health bar
        UpdateHealthBar();
        
        // Update skill points display
        UpdateSkillPointsDisplay();
    }
    
    private void InitializeHealthBar()
    {
        // Try to find player if not already set
        FindPlayerUnit();
        
        if (playerUnit != null)
        {
            // Set initial health values using our universal method
            float healthPercent = (float)playerUnit.currentHealth / playerUnit.maxHealth;
            SetHealthBarByAllMeans(healthPercent);
        }
    }
    
    private void FindPlayerUnit()
    {
        if (playerUnit != null) return;
        
        // Try to get player from active unit
        if (Unit.ActiveUnit is Player activePlayer)
        {
            playerUnit = activePlayer;
            return;
        }
        
        // Fallback to finding all players in scene
        Player[] players = FindObjectsOfType<Player>();
        if (players.Length > 0)
        {
            playerUnit = players[0];
        }
    }
    
    private void UpdateHealthBar()
    {
        // Try to find player if not already set
        if (playerUnit == null)
        {
            FindPlayerUnit();
            if (playerUnit == null) return;
        }
        
        // Calculate health percentage
        float healthPercent = (float)playerUnit.currentHealth / playerUnit.maxHealth;
        
        // Update the UI using all methods
        SetHealthBarByAllMeans(healthPercent);
    }
    
    // This method tries all techniques to modify the health bar
    private void SetHealthBarByAllMeans(float healthPercent)
    {
        // 1. Standard slider value
        if (healthBar != null)
        {
            healthBar.value = healthPercent;
        }
        
        // 2. Direct rect manipulation
        if (healthBar != null && healthBar.fillRect != null)
        {
            UpdateSliderFillRectDirectly(healthBar, healthPercent);
        }
        
        // 3. Fill image amount
        if (directFillImage != null)
        {
            directFillImage.fillAmount = healthPercent;
            directFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
        }
        
        // 4. Force layout update
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Canvas.ForceUpdateCanvases();
        }
        
        // 5. Update text
        if (healthText != null)
        {
            if (playerUnit != null)
            {
                healthText.text = $"CURRENT HP: {playerUnit.currentHealth} / {playerUnit.maxHealth}";
            }
            else
            {
                int displayHealth = (int)(100 * healthPercent);
                healthText.text = $"CURRENT HP: {displayHealth} / 100";
            }
        }
    }
    
    // This directly manipulates the slider's fill rect width for maximum compatibility
    private void UpdateSliderFillRectDirectly(Slider slider, float fillPercent)
    {
        if (slider == null || slider.fillRect == null)
            return;
            
        RectTransform fillRectTransform = slider.fillRect;
        
        if (fillRectTransform == null)
            return;
        
        // Get the parent fill area rect
        RectTransform fillAreaRect = fillRectTransform.parent.GetComponent<RectTransform>();
        if (fillAreaRect == null)
            return;
            
        // IMPORTANT: Instead of changing size, we'll adjust the scale to respect original design
        // This is better than changing the width directly
        
        // Calculate the right scale based on slider direction
        if (slider.direction == Slider.Direction.LeftToRight)
        {
            // For left-to-right, we need to:
            // 1. Keep the anchor at the left edge
            // 2. Scale the width by fill percent
            // 3. Keep the position at the left edge
            
            // Ensure the proper anchors are set
            fillRectTransform.anchorMin = new Vector2(0, 0);
            fillRectTransform.anchorMax = new Vector2(fillPercent, 1);
            
            // Reset position and size
            fillRectTransform.anchoredPosition = Vector2.zero;
            fillRectTransform.sizeDelta = Vector2.zero;
        }
        else if (slider.direction == Slider.Direction.RightToLeft)
        {
            // For right-to-left, anchor from the right side
            fillRectTransform.anchorMin = new Vector2(1 - fillPercent, 0);
            fillRectTransform.anchorMax = new Vector2(1, 1);
            fillRectTransform.anchoredPosition = Vector2.zero;
            fillRectTransform.sizeDelta = Vector2.zero;
        }
        
        // Update any image on the fill rect
        Image fillImage = fillRectTransform.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, fillPercent);
        }
    }
    
    // Update UI based on active unit
    public void UpdateUI(GameState state)
    {   
        // If we're being called during early initialization (before GameManager is ready), 
        // just disable the UI and wait for proper initialization
        if (gameManager == null || state == GameState.InitGame)
        {
            SetUIEnabled(false);
            return;
        }
        
        // Use centralized UI enable/disable function
        bool isPlayerActive = Unit.ActiveUnit is Player;
        SetUIEnabled(isPlayerActive);
    }
    
    // Update end turn button color based on player movement status
    private void UpdateEndTurnButtonColor()
    {
        if (endTurnButton == null || Unit.ActiveUnit == null)
            return;
            
        // Get the button's image component
        Image buttonImage = endTurnButton.GetComponent<Image>();
        if (buttonImage == null)
            return;
            
        // Check if player unit has moved and update button color
        if (Unit.ActiveUnit.hasMoved)
            buttonImage.color = readyColor;  // Green when ready to end turn
        else
            buttonImage.color = waitingColor; // Yellow when waiting for player to move
    }
    
    // Update move button color based on move mode status
    private void UpdateMoveButtonColor()
    {
        if (moveButton == null || Unit.ActiveUnit == null)
            return;
            
        // Get the button's image component
        Image buttonImage = moveButton.GetComponent<Image>();
        if (buttonImage == null)
            return;
            
        // Update button color based on move mode
        if (Unit.ActiveUnit is Player playerUnit)
        {
            if (playerUnit.IsInMoveMode && playerUnit.remainingMovementPoints > 0)
            {
                buttonImage.color = activeModeColor;  // Cyan when in move mode
            }
            else
            {
                buttonImage.color = inactiveModeColor; // White when not in move mode
            }
            
            // Disable move button if no movement points left
            moveButton.interactable = (playerUnit.remainingMovementPoints > 0);
        }
        else
        {
            // If selected unit is not a player, disable move button
            buttonImage.color = inactiveModeColor;
            moveButton.interactable = false;
        }
    }
    
    // Move button clicked
    public void OnMoveButtonClicked()
    {
        if (Unit.ActiveUnit is Player player)
        {
            player.ToggleMoveMode();
        }
    }
    
    // Update attack button color based on player status
    private void UpdateAttackButtonColor()
    {
        if (attackButton == null || Unit.ActiveUnit == null)
            return;
            
        // Get the button's image component
        Image buttonImage = attackButton.GetComponent<Image>();
        if (buttonImage == null)
            return;
            
        // Update button color and interactability based on player status
        if (Unit.ActiveUnit is Player player)
        {
            if (player.hasAttacked)
            {
                buttonImage.color = inactiveModeColor; // Gray when already attacked
                attackButton.interactable = false;
            }
            else
            {
                buttonImage.color = activeModeColor;  // Cyan when player can attack
                attackButton.interactable = true;
            }
        }
        else
        {
            buttonImage.color = inactiveModeColor; // White when not player's turn
            attackButton.interactable = false;
        }
    }
    
    // Update bag button color based on player status
    private void UpdateBagButtonColor()
    {
        if (bagButton == null || Unit.ActiveUnit == null)
            return;
            
        // Get the button's image component
        Image buttonImage = bagButton.GetComponent<Image>();
        if (buttonImage == null)
            return;
            
        // Update button color - active when it's a player's turn
        if (Unit.ActiveUnit is Player)
        {
            buttonImage.color = activeModeColor;  // Cyan when player can open bag
        }
        else
        {
            buttonImage.color = inactiveModeColor; // White when not player's turn
        }
    }
    
    // Attack button clicked - show combat UI and hide game UI
    public void OnAttackButtonClicked()
    {
        if (Unit.ActiveUnit is Player player)
        {
            // Clear any existing state (including move highlights) before opening combat UI
            player.HandleUIButtonClick();
            
            if (combatUI == null)
            {
                combatUI = FindObjectOfType<CombatUI>();
                if (combatUI == null)
                {
            
                    return;
                }
            }
            
            // Hide this UI GameObject and show combat UI
            gameObject.SetActive(false);
            combatUI.ShowCombatUI();
            combatUI.SetGameUI(this); // Pass reference so combat UI can re-enable this UI
        }
    }
    
    // Bag button clicked - close movement highlight but don't do anything else yet
    public void OnBagButtonClicked()
    {
        if (Unit.ActiveUnit is Player player)
        {
            // Clear any existing state (including move highlights)
            player.HandleUIButtonClick();
            
            // TODO: Add bag/inventory functionality here later
            // Bag button functionality not implemented yet
        }
    }
    
    // End turn button clicked
    public void OnEndTurnClicked()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
                return;
        }
        
        if (Unit.ActiveUnit is Player playerUnit)
        {
            gameManager.EndPlayerTurn();
        }
    }
    
    // Restart game button
    public void OnRestartClicked()
    {
        // In a real game, this would reload the scene or reset the game state
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    // Quit game button
    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    // Show damage message when player is attacked
    public void ShowDamageMessage(int damageAmount)
    {
        if (healthText != null)
        {
            StartCoroutine(FlashDamageText(damageAmount));
        }
    }
    
    // Flash damage message
    private IEnumerator FlashDamageText(int damageAmount)
    {
        string originalText = healthText.text;
        Color originalColor = healthText.color;
        
        // Flash message
        healthText.text = $"DAMAGE TAKEN: {damageAmount}!";
        healthText.color = Color.red;
        
        yield return new WaitForSeconds(1.0f);
        
        // Restore original text and color
        healthText.text = originalText;
        healthText.color = originalColor;
    }
    
    // Methods to control temporary UI disable state for consecutive turn transitions
    public void SetTemporarilyDisabled(bool disabled)
    {
        isTemporarilyDisabled = disabled;
        
        // Force immediate UI update when temporarily disabled state changes
        ForceUpdateUIState();
    }
    
    // Centralized function to disable/enable UI
    public void SetUIEnabled(bool enabled)
    {
        if (enabled)
        {
            // Enable UI - player turn state
            if (endTurnButton != null)
            {
                endTurnButton.interactable = true;
                UpdateEndTurnButtonColor();
            }
            if (moveButton != null)
            {
                moveButton.interactable = true;
                UpdateMoveButtonColor();
            }
            if (attackButton != null)
            {
                attackButton.interactable = true;
                UpdateAttackButtonColor();
            }
            if (bagButton != null)
            {
                bagButton.interactable = true;
                UpdateBagButtonColor();
            }
            if (actionButtonsGroup != null)
            {
                actionButtonsGroup.alpha = enabledOpacity;
                actionButtonsGroup.interactable = true;
                actionButtonsGroup.blocksRaycasts = true;
            }
        }
        else
        {
            // Disable UI - enemy turn or transition state
            if (endTurnButton != null)
            {
                endTurnButton.interactable = false;
                endTurnButton.GetComponent<Image>().color = inactiveModeColor;
            }
            if (moveButton != null)
            {
                moveButton.interactable = false;
                moveButton.GetComponent<Image>().color = inactiveModeColor;
            }
            if (attackButton != null)
            {
                attackButton.interactable = false;
                attackButton.GetComponent<Image>().color = inactiveModeColor;
            }
            if (bagButton != null)
            {
                bagButton.interactable = false;
                bagButton.GetComponent<Image>().color = inactiveModeColor;
            }
            if (actionButtonsGroup != null)
            {
                actionButtonsGroup.alpha = disabledOpacity;
                actionButtonsGroup.interactable = false;
                actionButtonsGroup.blocksRaycasts = false;
            }
        }
    }
    
    // Force an immediate UI state update
    private void ForceUpdateUIState()
    {
        bool shouldBeEnabled = Unit.ActiveUnit is Player && !isTemporarilyDisabled;
        SetUIEnabled(shouldBeEnabled);
    }
    
    // Test method you can call from Inspector to debug the CombatUI connection
    [ContextMenu("Test Combat UI Connection")]
    public void TestCombatUIConnection()
    {
        if (combatUI == null)
        {
            combatUI = FindObjectOfType<CombatUI>();
            if (combatUI == null)
                return;
        }
        
        gameObject.SetActive(false);
        combatUI.ShowCombatUI();
    }

    public void CloseCombatUI()
    {
        // Find the combat UI
        CombatUI combatUI = FindObjectOfType<CombatUI>();
        if (combatUI != null)
        {
            combatUI.SetCombatUIActive(false);
        }
        
        // Re-activate the main game UI GameObject
        gameObject.SetActive(true);
        
        // Re-enable the main game UI
        SetUIEnabled(true);
    }

    private void UpdateSkillPointsDisplay()
    {
        if (gameManager == null) return;
        
        // Update visual indicators using sprites
        if (skillPointIndicators != null)
        {
            int current = gameManager.GetCurrentSkillPoints();
            
            // Enable/disable child sprite objects based on current skill points
            for (int i = 0; i < skillPointIndicators.childCount; i++)
            {
                GameObject indicator = skillPointIndicators.GetChild(i).gameObject;
                indicator.SetActive(i < current);
            }
        }
    }
} 