using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CampfireManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject campfireUI;
    [SerializeField] private Transform actionButtonContainer;
    [SerializeField] private Button actionButtonPrefab;
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private TextMeshProUGUI locationText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private LinearStoryUI storyUI; // Reference to the story UI
    
    [Header("Manager References")]
    [SerializeField] private StoryManager storyManager;
    
    // Companion UI (hidden)
    // [SerializeField] private Transform companionStatusContainer;
    // [SerializeField] private GameObject companionStatusPrefab;
    
    [Header("Campfire Actions")]
    [SerializeField] private int maxActionPoints = 3;
    [SerializeField] private List<CampfireAction> availableActions = new List<CampfireAction>();
    private List<Button> actionButtons = new List<Button>();
    // private List<GameObject> companionStatusObjects = new List<GameObject>();
    
    [System.Serializable]
    public class CampfireAction
    {
        public string name;
        public string description;
        public int actionPointCost;
        public ActionType actionType;
        public string[] possibleResults;
        
        public CampfireAction(string name, string description, int cost, ActionType type)
        {
            this.name = name;
            this.description = description;
            this.actionPointCost = cost;
            this.actionType = type;
            this.possibleResults = new string[0];
        }
    }
    
    public enum ActionType
    {
        TalkToCompanion,
        Explore,
        Train,
        Rest,
        Craft,
        Trade
    }
    
    private void Awake()
    {
        // Use direct reference if assigned, otherwise fall back to searching
        if (storyManager == null)
        {
            storyManager = StoryManager.Instance;
            if (storyManager == null)
            {
                storyManager = FindObjectOfType<StoryManager>();
            }
        }
        
        // Subscribe to story events
        StoryManager.OnPhaseChanged += OnPhaseChanged;
        StoryManager.OnLocationChanged += OnLocationChanged;
    }
    
    private void Start()
    {
        SetupUI();
        CreateDefaultActions();
        
        // Ensure campfire UI is hidden at startup
        HideCampfireUI();
        
        // Auto-find references if not set
        if (storyManager == null)
        {
            storyManager = FindObjectOfType<StoryManager>();
            if (storyManager != null)
            {
                Debug.Log("Auto-found StoryManager reference");
            }
            else
            {
                Debug.LogWarning("Could not find StoryManager automatically. Please assign it in the inspector.");
            }
        }
        
        if (storyUI == null)
        {
            storyUI = FindObjectOfType<LinearStoryUI>();
            if (storyUI != null)
            {
                Debug.Log("Auto-found LinearStoryUI reference");
            }
            else
            {
                Debug.LogWarning("Could not find LinearStoryUI automatically. Please assign it in the inspector.");
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
    
    private void CreateDefaultActions()
    {
        availableActions.Clear();
        
        // Talk to companions
        availableActions.Add(new CampfireAction("Talk to Paladin", "Have a conversation with the noble Paladin", 1, ActionType.TalkToCompanion));
        availableActions.Add(new CampfireAction("Talk to Rogue", "Share stories with the clever Rogue", 1, ActionType.TalkToCompanion));
        availableActions.Add(new CampfireAction("Talk to Mage", "Discuss magic and strategy with the wise Mage", 1, ActionType.TalkToCompanion));
        availableActions.Add(new CampfireAction("Talk to Warrior", "Train and bond with the strong Warrior", 1, ActionType.TalkToCompanion));
        
        // Explore actions
        availableActions.Add(new CampfireAction("Explore Area", "Search the surrounding area for resources and clues", 1, ActionType.Explore));
        availableActions.Add(new CampfireAction("Scout Ahead", "Send someone to scout the path ahead", 2, ActionType.Explore));
        
        // Training actions
        availableActions.Add(new CampfireAction("Train Together", "Practice combat techniques as a team", 2, ActionType.Train));
        availableActions.Add(new CampfireAction("Individual Training", "Focus on personal improvement", 1, ActionType.Train));
        
        // Rest and recovery
        availableActions.Add(new CampfireAction("Rest", "Take time to rest and recover", 1, ActionType.Rest));
        availableActions.Add(new CampfireAction("Heal Wounds", "Tend to injuries and apply healing", 1, ActionType.Rest));
        
        // Crafting and trading
        availableActions.Add(new CampfireAction("Craft Items", "Use gathered materials to create useful items", 2, ActionType.Craft));
        availableActions.Add(new CampfireAction("Trade with Locals", "Find nearby settlements to trade with", 1, ActionType.Trade));
    }
    
    private void OnPhaseChanged(StoryPhase newPhase)
    {
        Debug.Log($"CampfireManager.OnPhaseChanged called with phase: {newPhase}");
        
        if (newPhase == StoryPhase.Campfire)
        {
            ShowCampfireUI();
        }
        else
        {
            HideCampfireUI();
        }
    }
    
    private void OnLocationChanged(StoryLocation newLocation)
    {
        UpdateLocationSpecificActions(newLocation);
    }
    
    private void UpdateLocationSpecificActions(StoryLocation location)
    {
        // Modify available actions based on location
        switch (location)
        {
            case StoryLocation.Town:
                // Town-specific actions
                break;
            case StoryLocation.Wilderness:
                // Wilderness-specific actions
                break;
            case StoryLocation.Desert:
                // Desert-specific actions (water management, etc.)
                break;
            case StoryLocation.Mountains:
                // Mountain-specific actions (climbing, etc.)
                break;
        }
    }
    
    public void ShowCampfireUI()
    {
        Debug.Log("CampfireManager.ShowCampfireUI() called");
        
        if (campfireUI != null)
        {
            campfireUI.SetActive(true);
            RefreshUI();
            Debug.Log("Campfire UI activated and refreshed");
        }
        else
        {
            Debug.LogError("campfireUI reference is null! Please assign the campfire UI GameObject in the inspector.");
        }
    }
    
    public void HideCampfireUI()
    {
        Debug.Log("CampfireManager.HideCampfireUI() called");
        
        if (campfireUI != null)
        {
            campfireUI.SetActive(false);
            Debug.Log("Campfire UI hidden");
        }
        else
        {
            Debug.LogWarning("campfireUI reference is null in HideCampfireUI!");
        }
    }
    
    private void RefreshUI()
    {
        UpdateActionPointsDisplay();
        UpdateLocationDisplay();
        UpdateDescriptionDisplay();
        CreateActionButtons();
        // UpdateCompanionStatus(); // Companion status hidden
    }
    
    private void UpdateActionPointsDisplay()
    {
        if (actionPointsText != null)
        {
            int currentPoints = storyManager.GetActionPoints();
            actionPointsText.text = $"Action Points: {currentPoints}/{maxActionPoints}";
        }
    }
    
    private void UpdateLocationDisplay()
    {
        if (locationText != null)
        {
            StoryLocation location = storyManager.GetCurrentLocation();
            locationText.text = $"Location: {location}";
        }
    }
    
    private void UpdateDescriptionDisplay()
    {
        if (descriptionText != null)
        {
            var currentBeat = storyManager.GetCurrentStoryBeat();
            if (currentBeat != null)
            {
                descriptionText.text = currentBeat.description;
            }
        }
    }
    
    private void CreateActionButtons()
    {
        // Clear existing buttons
        foreach (var button in actionButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        actionButtons.Clear();
        
        if (actionButtonContainer == null || actionButtonPrefab == null)
            return;
        
        int currentActionPoints = storyManager.GetActionPoints();
        
        foreach (var action in availableActions)
        {
            if (action.actionPointCost <= currentActionPoints)
            {
                Button button = Instantiate(actionButtonPrefab, actionButtonContainer);
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                
                if (buttonText != null)
                {
                    buttonText.text = $"{action.name} ({action.actionPointCost} AP)";
                }
                
                // Store action data
                var actionData = action;
                button.onClick.AddListener(() => OnActionButtonClicked(actionData));
                
                actionButtons.Add(button);
            }
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
            statusText.text = $"{companionName}: {companionName}: {relationship.currentStatus} ({relationship.relationshipLevel})";
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
    
    private void OnActionButtonClicked(CampfireAction action)
    {
        if (storyManager.GetActionPoints() < action.actionPointCost)
            return;
        
        // Spend action points
        storyManager.SpendActionPoints(action.actionPointCost);
        
        // Perform the action
        PerformAction(action);
        
        // Refresh UI
        RefreshUI();
    }
    
    private void PerformAction(CampfireAction action)
    {
        string result = "";
        
        switch (action.actionType)
        {
            case ActionType.TalkToCompanion:
                result = PerformCompanionInteraction(action);
                break;
            case ActionType.Explore:
                result = PerformExploration(action);
                break;
            case ActionType.Train:
                result = PerformTraining(action);
                break;
            case ActionType.Rest:
                result = PerformRest(action);
                break;
            case ActionType.Craft:
                result = PerformCrafting(action);
                break;
            case ActionType.Trade:
                result = PerformTrading(action);
                break;
        }
        
        // Display result
        ShowActionResult(result);
    }
    
    private string PerformCompanionInteraction(CampfireAction action)
    {
        // Determine which companion based on action name
        CharacterType companionType = CharacterType.Warrior; // Default
        
        if (action.name.Contains("Paladin"))
            companionType = CharacterType.Paladin;
        else if (action.name.Contains("Rogue"))
            companionType = CharacterType.Rogue;
        else if (action.name.Contains("Mage"))
            companionType = CharacterType.Mage;
        else if (action.name.Contains("Warrior"))
            companionType = CharacterType.Warrior;
        
        // Generate AI response for companion interaction
        string prompt = $"Generate a brief companion interaction scene. The player is talking to the {companionType} at a campfire. " +
                       $"Current relationship level: {storyManager.GetCompanionRelationship(companionType)?.relationshipLevel}. " +
                       $"Location: {storyManager.GetCurrentLocation()}. " +
                       $"Keep the response to 2-3 sentences.";
        
        // For now, return a simple response instead of using AI
        string aiResponse = $"The {companionType} shares some thoughts about the journey ahead.";
        
        // Improve relationship
        storyManager.ModifyRelationship(companionType, 5);
        
        return $"You had a meaningful conversation with the {companionType}. {aiResponse}";
    }
    
    private string PerformExploration(CampfireAction action)
    {
        string[] possibleResults = {
            "You found some useful herbs and materials.",
            "You discovered a hidden path that might be useful later.",
            "You found nothing of interest in the area.",
            "You discovered some ancient ruins with mysterious markings.",
            "You found a small cache of supplies left by previous travelers.",
            "You encountered some wildlife but managed to avoid conflict."
        };
        
        string result = possibleResults[Random.Range(0, possibleResults.Length)];
        
        // 30% chance to find an item
        if (Random.Range(0f, 1f) < 0.3f)
        {
            string[] possibleItems = { "Healing Potion", "Rope", "Torch", "Map Fragment", "Ancient Coin" };
            string item = possibleItems[Random.Range(0, possibleItems.Length)];
            storyManager.AddToInventory(item);
            result += $" You also found a {item}!";
        }
        
        return result;
    }
    
    private string PerformTraining(CampfireAction action)
    {
        string result = "You spent time training and improving your skills.";
        
        // Improve all companion relationships slightly
        storyManager.ModifyAllRelationships(2);
        
        // 20% chance to gain a temporary status effect
        if (Random.Range(0f, 1f) < 0.2f)
        {
            storyManager.AddStatusEffect("Well Rested (+1 to all stats)");
            result += " You feel well rested and ready for battle!";
        }
        
        return result;
    }
    
    private string PerformRest(CampfireAction action)
    {
        string result = "You took time to rest and recover.";
        
        // Remove negative status effects
        storyManager.RemoveStatusEffect("Exhausted");
        storyManager.RemoveStatusEffect("Injured");
        
        // 50% chance to gain positive status effect
        if (Random.Range(0f, 1f) < 0.5f)
        {
            storyManager.AddStatusEffect("Well Rested (+1 to all stats)");
            result += " You feel refreshed and ready for the journey ahead!";
        }
        
        return result;
    }
    
    private string PerformCrafting(CampfireAction action)
    {
        string result = "You crafted some useful items.";
        
        // Add crafted items to inventory
        string[] craftableItems = { "Healing Salve", "Rope", "Torch", "Simple Trap", "Bandages" };
        string item = craftableItems[Random.Range(0, craftableItems.Length)];
        storyManager.AddToInventory(item);
        
        result += $" You created a {item}!";
        
        return result;
    }
    
    private string PerformTrading(CampfireAction action)
    {
        string result = "You found some locals willing to trade.";
        
        // Gain some gold
        // Note: This would need to be implemented in PlayerStats
        result += " You gained some useful supplies and information.";
        
        return result;
    }
    
    private void ShowActionResult(string result)
    {
        // Display the result in the UI
        if (descriptionText != null)
        {
            descriptionText.text = result;
        }
        
        // You could also show a popup or notification here
        Debug.Log($"Campfire Action Result: {result}");
    }
    
    private void OnContinueClicked()
    {
        Debug.Log("CampfireManager.OnContinueClicked() called");
        
        // Reset action points
        storyManager.ResetActionPoints();
        
        // Hide campfire UI
        HideCampfireUI();
        
        // Progress to the next story beat
        storyManager.StartNextStoryBeat();
        
        // The story UI will automatically update via the OnStoryBeatStarted event
        Debug.Log("Progressed to next story beat from campfire");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        StoryManager.OnPhaseChanged -= OnPhaseChanged;
        StoryManager.OnLocationChanged -= OnLocationChanged;
    }
} 