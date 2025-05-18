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
    
    private void Awake()
    {
        // Make sure we have an EventSystem
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("No EventSystem found in the scene. Creating one...");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
    }
    
    private void Start()
    {
        // Find game manager
        gameManager = GameManager.Instance;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
            
        // Set up button listeners
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
            
        if (moveButton != null)
            moveButton.onClick.AddListener(OnMoveButtonClicked);
            
        // Ensure health bar is properly set up for either hierarchy structure
        SetupHealthBarComponents();
            
        // Initialize health bar
        InitializeHealthBar();
            
        // Update UI for initial state
        UpdateUI(GameState.InitGame);
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
        
        // Update button interactability based on game state
        if (gameManager != null)
        {
            // Only enable buttons during player turn
            if (gameManager.CurrentState == GameState.PlayerTurn)
            {
                if (endTurnButton != null)
                    endTurnButton.interactable = true;
            }
        }
        
        // Update button colors based on game state
        UpdateEndTurnButtonColor();
        UpdateMoveButtonColor();
        
        // Update health bar
        UpdateHealthBar();
        
        // Simple testing keys for game testing only
        if (Input.GetKeyDown(KeyCode.T)) // Damage test
        {
            if (playerUnit != null)
            {
                playerUnit.TakeDamage(10);
                ShowDamageMessage(10);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Y)) // Heal test
        {
            if (playerUnit != null)
            {
                playerUnit.Heal(10);
            }
        }
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
        
        // Try to get player from game manager
        if (gameManager != null && gameManager.selectedUnit is Player)
        {
            playerUnit = gameManager.selectedUnit as Player;
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
    
    private void SetupHealthBarComponents()
    {
        // Configure the slider if present
        if (healthBar != null)
        {
            // Make sure slider is set up from 0 to 1 for percentage
            healthBar.minValue = 0f;
            healthBar.maxValue = 1f;
            healthBar.value = 1f; // Full health
            
            // If we're using a slider, make sure we can access its fill image
            if (directFillImage == null)
            {
                Transform fillArea = healthBar.transform.Find("Fill Area");
                if (fillArea != null)
                {
                    Transform fill = fillArea.Find("Fill");
                    if (fill != null)
                    {
                        directFillImage = fill.GetComponent<Image>();
                    }
                }
            }
        }

        // Scenario 1: We have a direct fill image assigned in the Inspector
        if (directFillImage != null)
        {
            SetupDirectFillImage(directFillImage);
            return;
        }
        
        // If we still don't have a directFillImage and no slider, search components
        if (directFillImage == null && healthBar == null)
        {
            // Try to find an image in children that might be our fill
            Image[] images = GetComponentsInChildren<Image>();
            foreach (Image img in images)
            {
                if (img.gameObject.name.ToLower().Contains("fill"))
                {
                    directFillImage = img;
                    SetupDirectFillImage(directFillImage);
                    return;
                }
            }
            
            // If none has "fill" in the name, use the first one
            if (images.Length > 0)
            {
                directFillImage = images[0];
                SetupDirectFillImage(directFillImage);
                return;
            }
        }
    }
    
    private void SetupDirectFillImage(Image fillImage)
    {
        if (fillImage == null) return;
        
        // Configure the image for proper fill behavior
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1.0f; // Start full
        
        // Set color
        fillImage.color = fullHealthColor;
    }
    
    // Update UI based on game state
    public void UpdateUI(GameState state)
    {
        Debug.Log("UpdateUI called with state: " + state);
        
        switch (state)
        {
            case GameState.InitGame:
            case GameState.PlayerTurn:
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
                break;
                
            case GameState.EnemyTurn:
                if (endTurnButton != null)
                    endTurnButton.interactable = false;
                if (moveButton != null)
                    moveButton.interactable = false;
                break;
                
            case GameState.Victory:
                if (endTurnButton != null)
                    endTurnButton.interactable = false;
                if (moveButton != null)
                    moveButton.interactable = false;
                break;
                
            case GameState.Defeat:
                if (endTurnButton != null)
                    endTurnButton.interactable = false;
                if (moveButton != null)
                    moveButton.interactable = false;
                break;
        }
    }
    
    // Update end turn button color based on player movement status
    private void UpdateEndTurnButtonColor()
    {
        if (endTurnButton == null || gameManager == null || gameManager.selectedUnit == null)
            return;
            
        // Get the button's image component
        Image buttonImage = endTurnButton.GetComponent<Image>();
        if (buttonImage == null)
            return;
            
        // Check if player unit has moved and update button color
        if (gameManager.selectedUnit.hasMoved)
            buttonImage.color = readyColor;  // Green when ready to end turn
        else
            buttonImage.color = waitingColor; // Yellow when waiting for player to move
    }
    
    // Update move button color based on move mode status
    private void UpdateMoveButtonColor()
    {
        if (moveButton == null || gameManager == null || gameManager.selectedUnit == null)
            return;
            
        // Get the button's image component
        Image buttonImage = moveButton.GetComponent<Image>();
        if (buttonImage == null)
            return;
            
        // Update button color based on move mode
        if (gameManager.selectedUnit is Player playerUnit)
        {
            if (playerUnit.isInMoveMode && playerUnit.remainingMovementPoints > 0)
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
        if (gameManager == null || gameManager.selectedUnit == null)
            return;
            
        // Toggle move mode on the player unit if it's a Player
        if (gameManager.selectedUnit is Player playerUnit)
        {
            playerUnit.ToggleMoveMode();
        }
    }
    
    // End turn button clicked
    public void OnEndTurnClicked()
    {
        if (gameManager != null)
            gameManager.EndPlayerTurn();
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
} 