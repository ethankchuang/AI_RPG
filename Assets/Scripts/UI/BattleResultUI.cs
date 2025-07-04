using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleResultUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public TextMeshProUGUI victoryText;
    public TextMeshProUGUI defeatText;
    
    [Header("Victory Panel Buttons")]
    public Button victoryRestartButton;
    public Button victoryMainMenuButton;
    public Button victoryNextLevelButton; // Optional for future levels
    public Button victoryReturnToChatButton; // New button to return to chat
    
    [Header("Defeat Panel Buttons")]
    public Button defeatRestartButton;
    public Button defeatMainMenuButton;
    
    [Header("Animation Settings")]
    public float panelFadeInDuration = 1f;
    public float textTypewriterSpeed = 0.05f;
    
    private CanvasGroup victoryCanvasGroup;
    private CanvasGroup defeatCanvasGroup;
    
    private void Awake()
    {
        // Get or add CanvasGroup components for smooth animations
        if (victoryPanel != null)
        {
            victoryCanvasGroup = victoryPanel.GetComponent<CanvasGroup>();
            if (victoryCanvasGroup == null)
                victoryCanvasGroup = victoryPanel.AddComponent<CanvasGroup>();
        }
        
        if (defeatPanel != null)
        {
            defeatCanvasGroup = defeatPanel.GetComponent<CanvasGroup>();
            if (defeatCanvasGroup == null)
                defeatCanvasGroup = defeatPanel.AddComponent<CanvasGroup>();
        }
        
        // Setup button listeners
        SetupButtonListeners();
        
        // Hide panels initially
        HideAllPanels();
    }
    
    private void Update()
    {
        // Debug hotkeys for testing (only in editor or development builds)
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Debug.Log("Debug: Showing Victory Screen (pressed 8)");
            ShowVictoryScreen();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Debug.Log("Debug: Showing Defeat Screen (pressed 9)");
            ShowDefeatScreen();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log("Debug: Hiding all panels (pressed 0)");
            HideAllPanels();
            EnableGameUI(); // Re-enable game UI when hiding panels
        }
        #endif
        
        // Keyboard shortcuts for battle results
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Return to chat (only if victory screen is showing)
            if (victoryPanel != null && victoryPanel.activeInHierarchy)
            {
                Debug.Log("Returning to chat (pressed C)");
                ReturnToChat();
            }
        }
    }
    
    private void SetupButtonListeners()
    {
        // Victory buttons
        if (victoryRestartButton != null)
            victoryRestartButton.onClick.AddListener(RestartBattle);
            
        if (victoryMainMenuButton != null)
            victoryMainMenuButton.onClick.AddListener(GoToMainMenu);
            
        if (victoryNextLevelButton != null)
            victoryNextLevelButton.onClick.AddListener(LoadNextLevel);
            
        if (victoryReturnToChatButton != null)
            victoryReturnToChatButton.onClick.AddListener(ReturnToChat);
        
        // Defeat buttons
        if (defeatRestartButton != null)
            defeatRestartButton.onClick.AddListener(RestartBattle);
            
        if (defeatMainMenuButton != null)
            defeatMainMenuButton.onClick.AddListener(GoToMainMenu);
    }
    
    public void ShowVictoryScreen()
    {
        HideAllPanels();
        
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            StartCoroutine(FadeInPanel(victoryCanvasGroup));
            
            // Optional: Add victory text animation
            if (victoryText != null)
            {
                StartCoroutine(TypewriterEffect(victoryText, "Victory! All enemies have been defeated!"));
            }
        }
        
        // Disable game UI
        DisableGameUI();
    }
    
    public void ShowDefeatScreen()
    {
        HideAllPanels();
        
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            StartCoroutine(FadeInPanel(defeatCanvasGroup));
            
            // Optional: Add defeat text animation
            if (defeatText != null)
            {
                StartCoroutine(TypewriterEffect(defeatText, "Defeat! All your units have fallen..."));
            }
        }
        
        // Disable game UI
        DisableGameUI();
    }
    
    private void HideAllPanels()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
            if (victoryCanvasGroup != null)
                victoryCanvasGroup.alpha = 0f;
        }
        
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
            if (defeatCanvasGroup != null)
                defeatCanvasGroup.alpha = 0f;
        }
    }
    
    private void DisableGameUI()
    {
        // Disable main game UI
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.SetUIEnabled(false);
        }
        
        // Hide combat UI if visible
        CombatUI combatUI = FindObjectOfType<CombatUI>();
        if (combatUI != null)
        {
            combatUI.HideCombatUI();
        }
    }
    
    private void EnableGameUI()
    {
        // Re-enable main game UI
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.SetUIEnabled(true);
        }
    }
    
    private System.Collections.IEnumerator FadeInPanel(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        
        while (elapsed < panelFadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / panelFadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private System.Collections.IEnumerator TypewriterEffect(TextMeshProUGUI textComponent, string fullText)
    {
        if (textComponent == null) yield break;
        
        textComponent.text = "";
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            textComponent.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(textTypewriterSpeed);
        }
    }
    
    private void RestartBattle()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    private void GoToMainMenu()
    {
        // Load main menu scene (you'll need to set the correct scene name)
        SceneManager.LoadScene("MainMenu"); // Change this to your actual main menu scene name
    }
    
    private void LoadNextLevel()
    {
        // Load next level (implement based on your level system)
        Debug.Log("Loading next level...");
        // SceneManager.LoadScene("NextLevelSceneName");
    }
    
    private void ReturnToChat()
    {
        // Load the Story scene
        SceneManager.LoadScene("Story");
        
        // The LinearStoryUI.OnReturnFromCombat() will be called automatically
        // when the story scene loads and the LinearStoryUI initializes
    }
} 