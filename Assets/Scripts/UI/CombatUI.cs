using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUI : MonoBehaviour
{
    [Header("Combat UI References")]
    public Button basicAttackButton;
    public Button skill1Button;
    public Button skill2Button;
    public Button backButton;
    
    [Header("Skill Point Display")]
    public Transform skillPointIndicators; // Parent transform containing 5 skill point visual indicators (sprites)
    
    [Header("Button Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.cyan;
    public Color pressedColor = Color.green;
    public Color disabledColor = Color.gray;
    
    private GameManager gameManager; 
    private GameUI gameUI; // Reference to the main game UI
    private bool isUIActive = false; // Start as inactive
    
    private void Awake()
    {
        // Setup button listeners
        SetupButtonListeners();
    }
    
    private void Start()
    {
        // Find GameManager reference
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        // Initialize button states
        UpdateButtonStates();
        
        // Don't call SetCombatUIActive(false) here because it causes issues
        // when the GameObject is activated from GameUI - the Start() method
        // would immediately deactivate it again. Instead, make sure the 
        // GameObject starts inactive in the scene.
    }
    
    private void SetupButtonListeners()
    {
        if (basicAttackButton != null)
        {
            basicAttackButton.onClick.AddListener(OnBasicAttackClicked);
        }
        
        if (skill1Button != null)
        {
            skill1Button.onClick.AddListener(OnSkill1Clicked);
        }
        
        if (skill2Button != null)
        {
            skill2Button.onClick.AddListener(OnSkill2Clicked);
        }
        
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }
    
    private void Update()
    {
        // Update button states based on game state
        UpdateButtonStates();
        
        // Update skill points display
        UpdateSkillPointsDisplay();
    }
    
    private void UpdateButtonStates()
    {
        // Only enable combat buttons if we have an active player unit who hasn't attacked
        bool canUseAbilities = Unit.ActiveUnit is Player player && !player.hasAttacked;
        
        SetButtonInteractable(basicAttackButton, canUseAbilities);
        
        // Skill buttons require active player, no previous attack, AND available skill points
        bool canUseSkill1 = canUseAbilities && gameManager != null && gameManager.HasEnoughSkillPoints(1);
        bool canUseSkill2 = canUseAbilities && gameManager != null && gameManager.HasEnoughSkillPoints(2);
        SetButtonInteractable(skill1Button, canUseSkill1);
        SetButtonInteractable(skill2Button, canUseSkill2);
        
        // Back button should always be interactable
        SetButtonInteractable(backButton, true);
    }
    
    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button == null) return;
        
        button.interactable = interactable;
        
        // Update button color based on interactable state
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = interactable ? normalColor : disabledColor;
        }
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
    
    // Public method to show/hide the combat UI
    public void SetCombatUIActive(bool active)
    {
        isUIActive = active;
        
        // Simply turn the entire GameObject on/off
        gameObject.SetActive(active);
    }
    
    // Public method to set the GameUI reference
    public void SetGameUI(GameUI gameUIReference)
    {
        gameUI = gameUIReference;
    }
    
    // Helper method to close combat UI and re-enable game UI
    private void CloseCombatUIAndEnableGameUI()
    {
        SetCombatUIActive(false);
        if (gameUI != null)
        {
            gameUI.gameObject.SetActive(true);
        }
    }
    
    // Button event handlers
    private void OnBasicAttackClicked()
    {

        
        if (Unit.ActiveUnit is Player player)
        {
            // Clear any existing state (including move mode) before starting target selection
            player.HandleUIButtonClick();
            player.StartTargetSelection(player.basicAttack);
        }
    }
    
    private void OnSkill1Clicked()
    {

        
        if (Unit.ActiveUnit is Player player)
        {
            // Clear any existing state (including move mode) before starting target selection
            player.HandleUIButtonClick();
            player.StartTargetSelection(player.skill1);
        }
    }
    
    private void OnSkill2Clicked()
    {

        
        if (Unit.ActiveUnit is Player player)
        {
            // Clear any existing state (including move mode) before starting target selection
            player.HandleUIButtonClick();
            player.StartTargetSelection(player.skill2);
        }
    }
    
    private void OnBackClicked()
    {

        
        // Clear any existing state before closing
        if (Unit.ActiveUnit is Player player)
        {
            player.HandleUIButtonClick();
        }
        
        // Close combat UI and re-enable game UI
        CloseCombatUIAndEnableGameUI();
    }
    
    // Public method for other scripts to toggle the combat UI
    public void ToggleCombatUI()
    {
        SetCombatUIActive(!isUIActive);
    }
    
    // Public method to show the combat UI
    public void ShowCombatUI()
    {
        SetCombatUIActive(true);
    }
    
    // Public method to hide the combat UI
    public void HideCombatUI()
    {
        SetCombatUIActive(false);
    }
} 