using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public Button endTurnButton;
    public Button moveButton;
    
    [Header("Button Colors")]
    public Color waitingColor = Color.yellow;
    public Color readyColor = Color.green;
    public Color activeModeColor = Color.cyan;
    public Color inactiveModeColor = Color.white;
    
    private GameManager gameManager;
    private EventSystem eventSystem;
    
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
            
        // Update UI for initial state
        UpdateUI(GameState.InitGame);
    }
    
    private void Update()
    {
        // Update button colors based on game state
        UpdateEndTurnButtonColor();
        UpdateMoveButtonColor();
    }
    
    // Update UI based on game state
    public void UpdateUI(GameState state)
    {
        switch (state)
        {
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
        Unit playerUnit = gameManager.selectedUnit;
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
    
    // Move button clicked
    public void OnMoveButtonClicked()
    {
        if (gameManager == null || gameManager.selectedUnit == null)
            return;
            
        // Toggle move mode on the player unit
        gameManager.selectedUnit.ToggleMoveMode();
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
} 