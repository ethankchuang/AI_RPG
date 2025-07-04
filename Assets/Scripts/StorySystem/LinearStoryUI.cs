using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LinearStoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject storyUI;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI locationText;
    [SerializeField] private TextMeshProUGUI phaseText;
    
    [Header("Manager References")]
    [SerializeField] private StoryManager storyManager;
    [SerializeField] private CampfireManager campfireManager;
    
    // Companion Status (hidden)
    // [SerializeField] private Transform companionStatusContainer;
    // [SerializeField] private GameObject companionStatusPrefab;
    
    [Header("Story Settings")]
    [SerializeField] private string battleSceneName = "Battle";
    [SerializeField] private float textDisplayDelay = 0.05f;
    
    private List<Button> choiceButtons = new List<Button>();
    // private List<GameObject> companionStatusObjects = new List<GameObject>();
    private bool isTransitioning = false;
    
    private void Awake()
    {
        // Always use the singleton StoryManager instance
        storyManager = StoryManager.Instance;
        if (storyManager == null)
        {
            storyManager = FindObjectOfType<StoryManager>();
        }
        
        if (campfireManager == null)
        {
            campfireManager = FindObjectOfType<CampfireManager>();
        }
        
        // Subscribe to story events
        StoryManager.OnStoryBeatStarted += OnStoryBeatStarted;
        StoryManager.OnStoryBeatCompleted += OnStoryBeatCompleted;
        StoryManager.OnPhaseChanged += OnPhaseChanged;
        StoryManager.OnLocationChanged += OnLocationChanged;
    }
    
    private void Start()
    {
        SetupUI();
        
        // Ensure story UI is active at startup
        if (storyUI != null && !storyUI.activeInHierarchy)
        {
            storyUI.SetActive(true);
        }
        else if (storyUI == null)
        {
            Debug.LogError("storyUI reference is null in Start()!");
        }
        
        // Check if we're returning from combat by looking at the current story beat
        if (storyManager.GetCurrentStoryBeat() == null)
        {
            storyManager.StartNextStoryBeat();
        }
        else
        {
            var currentBeat = storyManager.GetCurrentStoryBeat();
            if (currentBeat.phase == StoryPhase.Combat && currentBeat.isCompleted)
            {
                // We're returning from combat, automatically progress to next story beat
                OnReturnFromCombat();
            }
            else
            {
                // Normal story progression, display current beat
                DisplayCurrentStoryBeat();
            }
        }
    }
    
    private void SetupUI()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }
    
    private void OnStoryBeatStarted(StoryBeat beat)
    {
        DisplayStoryBeat(beat);
    }
    
    private void OnStoryBeatCompleted(StoryBeat beat)
    {
        // Handle story beat completion
    }
    
    private void OnPhaseChanged(StoryPhase newPhase)
    {
        UpdatePhaseDisplay(newPhase);
        
        switch (newPhase)
        {
            case StoryPhase.Combat:
                ShowCombatUI();
                break;
            case StoryPhase.Campfire:
                ShowCampfireUI();
                break;
            case StoryPhase.Transition:
                ShowTransitionUI();
                break;
        }
    }
    
    private void OnLocationChanged(StoryLocation newLocation)
    {
        UpdateLocationDisplay(newLocation);
    }
    
    public void DisplayCurrentStoryBeat()
    {
        var beat = storyManager.GetCurrentStoryBeat();
        if (beat != null)
        {
            // Ensure story UI is active before displaying the beat
            if (storyUI != null && !storyUI.activeInHierarchy)
            {
                storyUI.SetActive(true);
            }
            else if (storyUI == null)
            {
                Debug.LogError("storyUI reference is null in DisplayCurrentStoryBeat!");
            }
            
            DisplayStoryBeat(beat);
        }
        else
        {
            Debug.LogError("Current story beat is null!");
        }
    }
    
    private void DisplayStoryBeat(StoryBeat beat)
    {
        if (beat == null) return;
        
        Debug.Log($"DisplayStoryBeat: Setting title to '{beat.title}' and description to '{beat.description}'");
        
        // Check for gibberish in the story beat data
        if (beat.description.Contains("Ays.") || beat.description.Contains("yTohue") || beat.description.Contains("gsahtohpekre"))
        {
            Debug.LogError($"GIBBERISH DETECTED IN STORY BEAT DATA!");
            Debug.LogError($"Beat ID: {beat.id}");
            Debug.LogError($"Beat Title: {beat.title}");
            Debug.LogError($"Beat Description: {beat.description}");
        }
        
        // Update UI elements
        if (titleText != null)
        {
            Debug.Log($"Before setting title - current text: '{titleText.text}'");
            titleText.text = beat.title;
            Debug.Log($"Set title text to: {beat.title}");
        }
        
        if (descriptionText != null && storyUI != null && storyUI.activeInHierarchy)
        {
            Debug.Log($"Before starting TypeText - current description text: '{descriptionText.text}'");
            Debug.Log($"Starting TypeText coroutine with description: {beat.description}");
            StartCoroutine(TypeText(descriptionText, beat.description));
        }
        else if (descriptionText != null)
        {
            // If story UI is inactive, just set the text directly
            Debug.Log($"Before setting description directly - current text: '{descriptionText.text}'");
            descriptionText.text = beat.description;
            Debug.Log($"Set description text directly to: {beat.description}");
        }
        else
        {
            Debug.LogWarning("Could not set description text - missing references or inactive UI");
        }
        
        // Update location and phase
        UpdateLocationDisplay(beat.location);
        UpdatePhaseDisplay(beat.phase);
        
        // Handle the different types of story points
        switch (beat.phase)
        {
            case StoryPhase.Combat:
                // COMBAT: Show continue button that will trigger combat
                ShowCombatUI();
                CreateContinueButton(beat);
                break;
                
            case StoryPhase.Campfire:
                // CAMPFIRE: Hide story UI, let CampfireManager handle it
                ShowCampfireUI();
                break;
                
            case StoryPhase.Transition:
                // TRANSITION: Check if it's choice-based or linear
                if (beat.choices != null && beat.choices.Length > 0)
                {
                    // CHOICE: Show multiple choice buttons
                    ShowTransitionUI();
                    CreateChoiceButtons(beat);
                }
                else
                {
                    // NEXT: Show single "Next" button
                    ShowTransitionUI();
                    CreateNextButton(beat);
                }
                break;
        }
    }
    
    private IEnumerator TypeText(TextMeshProUGUI textComponent, string text)
    {
        Debug.Log($"TypeText coroutine started with text: '{text}'");
        textComponent.text = "";
        
        for (int i = 0; i < text.Length; i++)
        {
            textComponent.text += text[i];
            
            // Check for gibberish during typing
            if (textComponent.text.Contains("Ays.") || textComponent.text.Contains("yTohue") || textComponent.text.Contains("gsahtohpekre"))
            {
                Debug.LogError($"GIBBERISH DETECTED DURING TYPING! Current text: '{textComponent.text}'");
            }
            
            yield return new WaitForSeconds(textDisplayDelay);
        }
        
        Debug.Log($"TypeText coroutine completed. Final text: '{textComponent.text}'");
    }
    
    private void CreateChoiceButtons(StoryBeat beat)
    {
        // Clear existing buttons
        foreach (var button in choiceButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        choiceButtons.Clear();
        
        if (choiceButtonContainer == null || choiceButtonPrefab == null)
            return;
        
        for (int i = 0; i < beat.choices.Length; i++)
        {
            Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                buttonText.text = beat.choices[i];
            }
            
            // Store choice index
            int choiceIndex = i;
            button.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
            
            choiceButtons.Add(button);
        }
    }
    
    private void CreateNextButton(StoryBeat beat)
    {
        // Clear existing buttons
        foreach (var existingButton in choiceButtons)
        {
            if (existingButton != null)
                Destroy(existingButton.gameObject);
        }
        choiceButtons.Clear();
        
        if (choiceButtonContainer == null || choiceButtonPrefab == null)
            return;
        
        // Create a single "Next" button for linear story
        Button nextButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);
        TextMeshProUGUI buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        
        if (buttonText != null)
        {
            buttonText.text = "Next";
        }
        
        nextButton.onClick.AddListener(() => OnNextClicked());
        
        choiceButtons.Add(nextButton);
    }
    
    private void CreateContinueButton(StoryBeat beat)
    {
        // Clear existing buttons
        foreach (var existingButton in choiceButtons)
        {
            if (existingButton != null)
                Destroy(existingButton.gameObject);
        }
        choiceButtons.Clear();
        
        if (choiceButtonContainer == null || choiceButtonPrefab == null)
            return;
        
        Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        
        if (buttonText != null)
        {
            buttonText.text = "Continue";
        }
        
        button.onClick.AddListener(() => OnContinueClicked());
        
        choiceButtons.Add(button);
    }
    
    private void UpdateLocationDisplay(StoryLocation location)
    {
        if (locationText != null)
        {
            locationText.text = $"Location: {location}";
        }
    }
    
    private void UpdatePhaseDisplay(StoryPhase phase)
    {
        if (phaseText != null)
        {
            phaseText.text = $"Phase: {phase}";
        }
    }
    
    // Companion status methods (hidden)
    /*
    private void UpdateCompanionStatus()
    {
        // Clear existing companion status objects
        foreach (var obj in companionStatusObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        companionStatusObjects.Clear();
        
        if (companionStatusContainer == null || companionStatusPrefab == null)
            return;
        
        // Create status displays for each companion
        var paladinRel = storyManager.GetCompanionRelationship(CharacterType.Paladin);
        var rogueRel = storyManager.GetCompanionRelationship(CharacterType.Rogue);
        var mageRel = storyManager.GetCompanionRelationship(CharacterType.Mage);
        var warriorRel = storyManager.GetCompanionRelationship(CharacterType.Warrior);
        
        CreateCompanionStatusDisplay("Paladin", paladinRel);
        CreateCompanionStatusDisplay("Rogue", rogueRel);
        CreateCompanionStatusDisplay("Mage", mageRel);
        CreateCompanionStatusDisplay("Warrior", warriorRel);
    }
    
    private void CreateCompanionStatusDisplay(string companionName, CompanionRelationship relationship)
    {
        GameObject statusObj = Instantiate(companionStatusPrefab, companionStatusContainer);
        TextMeshProUGUI statusText = statusObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (statusText != null && relationship != null)
        {
            statusText.text = $"{companionName}: {relationship.currentStatus} ({relationship.relationshipLevel})";
            statusText.color = GetRelationshipColor(relationship.relationshipLevel);
        }
        
        companionStatusObjects.Add(statusObj);
    }
    
    private Color GetRelationshipColor(int relationshipLevel)
    {
        if (relationshipLevel >= 50)
            return Color.green;
        else if (relationshipLevel >= 20)
            return Color.cyan;
        else if (relationshipLevel >= -20)
            return Color.white;
        else if (relationshipLevel >= -50)
            return Color.yellow;
        else
            return Color.red;
    }
    */
    
    private void ShowCombatUI()
    {
        if (storyUI != null)
        {
            storyUI.SetActive(true);
        }
        
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
        
        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.gameObject.SetActive(true); // Show choice/next buttons for combat
        }
    }
    
    private void ShowCampfireUI()
    {
        Debug.Log("LinearStoryUI.ShowCampfireUI called - hiding story UI only");
        
        if (storyUI != null)
        {
            storyUI.SetActive(false); // Hide story UI, campfire manager will show its own
            Debug.Log("Story UI hidden");
        }
        else
        {
            Debug.LogWarning("storyUI reference is null!");
        }
        
        // Use direct reference to show campfire UI
        if (campfireManager != null)
        {
            campfireManager.ShowCampfireUI();
        }
        else
        {
            Debug.LogWarning("campfireManager reference is null!");
        }
    }
    
    private void ShowTransitionUI()
    {
        Debug.Log("LinearStoryUI.ShowTransitionUI() called");
        
        if (storyUI != null)
        {
            storyUI.SetActive(true);
            Debug.Log("Story UI activated in ShowTransitionUI");
        }
        else
        {
            Debug.LogError("storyUI is null in ShowTransitionUI!");
        }
        
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false); // Hide continue button, use choice/next buttons instead
        }
        
        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.gameObject.SetActive(true);
            Debug.Log("Choice button container activated");
        }
        else
        {
            Debug.LogError("choiceButtonContainer is null in ShowTransitionUI!");
        }
    }
    
    private void OnChoiceSelected(int choiceIndex)
    {
        if (isTransitioning) return;
        
        storyManager.MakeChoice(choiceIndex);
        
        // Move to next story beat
        storyManager.StartNextStoryBeat();
    }
    
    private void OnNextClicked()
    {
        if (isTransitioning) return;
        
        var currentBeat = storyManager.GetCurrentStoryBeat();
        if (currentBeat != null)
        {
            // Just move to next story beat - completion is handled automatically
            storyManager.StartNextStoryBeat();
        }
    }
    
    private void OnContinueClicked()
    {
        if (isTransitioning) return;
        
        var currentBeat = storyManager.GetCurrentStoryBeat();
        if (currentBeat != null && currentBeat.phase == StoryPhase.Combat)
        {
            // If it's a combat phase, trigger combat
            StartCoroutine(TransitionToCombat());
        }
        else
        {
            // Otherwise, move to next story beat
            storyManager.StartNextStoryBeat();
        }
    }
    

    
    private IEnumerator TransitionToCombat()
    {
        isTransitioning = true;
        
        // Mark current story beat as completed
        storyManager.CompleteCurrentStoryBeat();
        
        // Show transition message
        if (descriptionText != null)
        {
            descriptionText.text = "Transitioning to combat...";
        }
        
        // Wait a moment
        yield return new WaitForSeconds(2f);
        
        // Load combat scene
        SceneManager.LoadScene(battleSceneName);
    }
    
    public void OnReturnFromCombat()
    {
        // This is called when returning from combat
        isTransitioning = false;
        
        // Automatically progress to the next story beat
        storyManager.StartNextStoryBeat();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        StoryManager.OnStoryBeatStarted -= OnStoryBeatStarted;
        StoryManager.OnStoryBeatCompleted -= OnStoryBeatCompleted;
        StoryManager.OnPhaseChanged -= OnPhaseChanged;
        StoryManager.OnLocationChanged -= OnLocationChanged;
    }
} 