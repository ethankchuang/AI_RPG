using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public Button endTurnButton;
    public TextMeshProUGUI turnText;
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    
    [Header("Unit Info")]
    public GameObject unitInfoPanel;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI unitMovementText;
    
    [Header("Button Colors")]
    public Color waitingColor = Color.yellow;
    public Color readyColor = Color.green;
    
    private GameManager gameManager;
    
    private void Start()
    {
        // Find game manager
        gameManager = GameManager.Instance;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
            
        // Set up button listeners
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
            
        // Hide victory/defeat panels
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
            
        if (defeatPanel != null)
            defeatPanel.SetActive(false);
            
        // Hide unit info panel
        if (unitInfoPanel != null)
            unitInfoPanel.SetActive(false);
            
        // Update UI for initial state
        UpdateUI(GameState.InitGame);
    }
    
    private void Update()
    {
        // Update end turn button color based on player unit status
        UpdateEndTurnButtonColor();
    }
    
    // Update UI based on game state
    public void UpdateUI(GameState state)
    {
        switch (state)
        {
            case GameState.PlayerTurn:
                if (turnText != null)
                    turnText.text = "Player Turn";
                if (endTurnButton != null)
                {
                    endTurnButton.interactable = true;
                    UpdateEndTurnButtonColor();
                }
                break;
                
            case GameState.EnemyTurn:
                if (turnText != null)
                    turnText.text = "Enemy Turn";
                if (endTurnButton != null)
                    endTurnButton.interactable = false;
                break;
                
            case GameState.Victory:
                if (victoryPanel != null)
                    victoryPanel.SetActive(true);
                if (endTurnButton != null)
                    endTurnButton.interactable = false;
                break;
                
            case GameState.Defeat:
                if (defeatPanel != null)
                    defeatPanel.SetActive(true);
                if (endTurnButton != null)
                    endTurnButton.interactable = false;
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
    
    // End turn button clicked
    public void OnEndTurnClicked()
    {
        if (gameManager != null)
            gameManager.EndPlayerTurn();
    }
    
    // Show unit info
    public void ShowUnitInfo(Unit unit)
    {
        if (unitInfoPanel == null || unit == null)
            return;
            
        unitInfoPanel.SetActive(true);
        
        if (unitNameText != null)
            unitNameText.text = unit.unitName;
            
        if (unitMovementText != null)
        {
            string moveStatus = unit.hasMoved ? "Moved" : "Ready";
            unitMovementText.text = $"Movement: {moveStatus}";
        }
    }
    
    // Hide unit info
    public void HideUnitInfo()
    {
        if (unitInfoPanel != null)
            unitInfoPanel.SetActive(false);
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