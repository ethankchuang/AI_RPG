using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class SimpleChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI chatText; // Single text component for all messages
    
    [Header("Message Settings")]
    [SerializeField] private Color userMessageColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color aiMessageColor = new Color(0.2f, 0.8f, 0.4f);
    [SerializeField] private Color systemMessageColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private int maxMessages = 50;
    [SerializeField] private string gameSceneName = "Battle";
    
    private bool isGenerating = false;
    private List<MessageData> messageHistory = new List<MessageData>(); // Track message history with sender info
    private List<StoryPoint> storyPoints = new List<StoryPoint>(); // Track key story decisions and events
    
    // Story progression tracking
    public enum StoryChapter { Tavern, Plains, Desert, Mountains, Castle, ThroneRoom }
    private StoryChapter currentChapter = StoryChapter.Tavern;
    private int messagesInCurrentChapter = 0;
    private const int MESSAGES_PER_CHAPTER = 5;
    
    // Response type tracking for balancing
    private int companionResponses = 0;
    private int combatResponses = 0;
    private int storyResponses = 0;
    private const int TRACKING_WINDOW = 10; // Track last 10 responses for balancing
    
    // Track the last displayed action options for numeric input mapping
    private List<ActionOption> currentActionOptions = new List<ActionOption>();
    
    // Flag to track if we're returning from combat
    private bool isReturningFromCombat = false;
    
    // Action option structure
    private class ActionOption
    {
        public string displayText;
        public string responseType;
        public string description;
        
        public ActionOption(string displayText, string responseType, string description)
        {
            this.displayText = displayText;
            this.responseType = responseType;
            this.description = description;
        }
    }
    
    // Message data structure to track sender and content
    public class MessageData
    {
        public string sender;
        public string content;
        public Color color;
        
        public MessageData(string sender, string content, Color color)
        {
            this.sender = sender;
            this.content = content;
            this.color = color;
        }
    }
    
    // Story point structure to track key decisions and events
    public class StoryPoint
    {
        public string description;
        public StoryChapter chapter;
        public int messageIndex;
        
        public StoryPoint(string description, StoryChapter chapter, int messageIndex)
        {
            this.description = description;
            this.chapter = chapter;
            this.messageIndex = messageIndex;
        }
    }
    
    private void Start()
    {
        Debug.Log("SimpleChatUI: Start() called");
        
        // Setup input field
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnInputSubmitted);
            inputField.onValueChanged.AddListener(OnInputChanged);
            inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Type your message here...";
        }
        
        // Setup next button
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
        
        // Ensure AIService exists
        EnsureAIServiceExists();
        
        // Check if we have saved state to load
        Debug.Log($"SimpleChatUI: Checking for saved state - ChatStateManager.Instance: {ChatStateManager.Instance != null}");
        if (ChatStateManager.Instance != null)
        {
            Debug.Log($"SimpleChatUI: ChatStateManager found, checking HasSavedState()");
            bool hasSavedState = ChatStateManager.Instance.HasSavedState();
            Debug.Log($"SimpleChatUI: HasSavedState returned: {hasSavedState}");
            
            if (hasSavedState)
            {
                Debug.Log("SimpleChatUI: Loading saved chat state...");
                ChatStateManager.Instance.LoadChatState(this);
                
                // Check if we're returning from combat using the flag
                Debug.Log($"SimpleChatUI: isReturningFromCombat flag: {isReturningFromCombat}");
                if (isReturningFromCombat)
                {
                    Debug.Log("SimpleChatUI: Detected return from combat - automatically generating post-combat story");
                    // Reset the flag
                    isReturningFromCombat = false;
                    // Clear action options to prevent re-triggering combat
                    currentActionOptions.Clear();
                    // Automatically generate post-combat story
                    StartCoroutine(GeneratePostCombatStoryAfterDelay());
                }
            }
            else
            {
                Debug.Log("SimpleChatUI: No saved state found, starting fresh");
                // Start fresh
                StartWithAPIIntroduction();
            }
        }
        else
        {
            Debug.Log("SimpleChatUI: No ChatStateManager found, starting fresh");
            // Start fresh
            StartWithAPIIntroduction();
        }
        
        SetUIState(true);
    }
    
    private System.Collections.IEnumerator GeneratePostCombatStoryAfterDelay()
    {
        // Wait a moment for the scene to fully load and UI to be ready
        yield return new WaitForSeconds(1f);
        
        // Add a brief system message to indicate the transition
        AddSystemMessage("Returning from combat...");
        
        // Wait a bit more for the message to be displayed
        yield return new WaitForSeconds(1f);
        
        // Generate the post-combat story
        GeneratePostCombatStoryAsync();
    }
    
    private async void GeneratePostCombatStoryAsync()
    {
        await GeneratePostCombatStory();
    }
    
    private void EnsureAIServiceExists()
    {
        if (AIService.Instance == null)
        {
            Debug.LogWarning("AIService not found! Creating one...");
            GameObject aiServiceObj = new GameObject("AIService");
            aiServiceObj.AddComponent<AIService>();
        }
    }
    
    private void OnInputSubmitted(string text)
    {
        SendMessage();
    }
    
    private void OnInputChanged(string text)
    {
        if (inputField != null)
        {
            inputField.interactable = !isGenerating;
        }
    }
    
    private async void SendMessage()
    {
        if (isGenerating) return;
        
        string message = inputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;
        
        AddUserMessage(message);
        inputField.text = "";
        
        await GenerateResponse(message);
    }
    
    private async System.Threading.Tasks.Task GenerateResponse(string userMessage)
    {
        Debug.Log("GenerateResponse: Starting response generation...");
        
        isGenerating = true;
        SetUIState(false);
        
        try
        {
            // Check for numeric input first
            string responseType = GetNumericResponseType(userMessage);
            
            if (!string.IsNullOrEmpty(responseType))
            {
                // Player selected a numbered option
                Debug.Log($"GenerateResponse: Numeric input detected, response type: {responseType}");
                await HandleSelectedAction(responseType, userMessage);
            }
            else
            {
                // Player entered their own text - determine what happens
                responseType = await DetermineResponseType(userMessage);
                Debug.Log($"GenerateResponse: Text input detected, response type: {responseType}");
                await HandleSelectedAction(responseType, userMessage);
            }
            
            // Update story progression
            messagesInCurrentChapter++;
            if (messagesInCurrentChapter >= MESSAGES_PER_CHAPTER)
            {
                AdvanceToNextChapter();
            }
            
            Debug.Log("GenerateResponse: Response generation complete.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating response: {e}");
            AddSystemMessage("Sorry, an error occurred while generating the response. Please try again.");
        }
        
        isGenerating = false;
        SetUIState(true);
    }
    
    private string GetNumericResponseType(string userMessage)
    {
        string trimmedMessage = userMessage.Trim();
        
        int selectedOption = -1;
        
        // Check for single digit numbers (1-3)
        if (trimmedMessage.Length == 1 && char.IsDigit(trimmedMessage[0]))
        {
            selectedOption = int.Parse(trimmedMessage);
        }
        
        // Check for numbers followed by text (e.g., "1. Go north", "2 fight", etc.)
        if (trimmedMessage.Length > 1 && char.IsDigit(trimmedMessage[0]))
        {
            // Extract the number part
            string numberPart = "";
            int i = 0;
            while (i < trimmedMessage.Length && (char.IsDigit(trimmedMessage[i]) || trimmedMessage[i] == '.'))
            {
                numberPart += trimmedMessage[i];
                i++;
            }
            
            if (int.TryParse(numberPart.Replace(".", ""), out int actionNumber))
            {
                selectedOption = actionNumber;
            }
        }
        
        // If we have a valid option and current action options, use them
        if (selectedOption >= 1 && selectedOption <= currentActionOptions.Count)
        {
            string responseType = currentActionOptions[selectedOption - 1].responseType;
            Debug.Log($"Numeric input {selectedOption} mapped to {responseType}");
            return responseType;
        }
        
        return "";
    }
    
    private async System.Threading.Tasks.Task<string> DetermineResponseType(string userMessage)
    {
        // For text input, use AI to analyze what type of action the player wants
        string analysisPrompt = $@"Analyze this player's input and determine what type of RPG action they want to take.

Player input: ""{userMessage}""

Available action types:
- COMBAT: Any action involving fighting, conflict, danger, or physical confrontation
- COMPANION: Any action involving social interaction, character bonding, or relationship development
- STORY: Any action involving exploration, discovery, investigation, or narrative progression

IMPORTANT: Only classify as a valid action type if the input clearly indicates the player's intent. If the input is unclear, nonsensical, or doesn't match any action type, respond with ""UNCLEAR"".

Use your understanding of natural language and RPG context to determine the player's intent. Consider the overall meaning and context, not just specific words.

Respond with ONLY the action type (COMBAT, COMPANION, STORY, or UNCLEAR) that best matches what the player wants to do:";
        
        try
        {
            string aiResponse = await AIService.Instance.SendMessageAsync(analysisPrompt);
            string responseType = aiResponse.Trim().ToLower();
            
            Debug.Log($"AI Analysis: Player input '{userMessage}' analyzed as '{responseType}'");
            
            // Map the AI response to our response types
            if (responseType.Contains("combat"))
                return "combat";
            else if (responseType.Contains("companion"))
                return "companion";
            else if (responseType.Contains("story"))
                return "story";
            else
            {
                Debug.LogWarning($"AI returned unexpected response type: {responseType}");
                string errorMessage = "I couldn't understand what you want to do. Please try rephrasing your request. You can:\n" +
                                    "• Use numbers (1, 2, 3) to select from the options above\n" +
                                    "• Describe what you want to do more clearly\n" +
                                    "• Try different wording for your action";
                AddSystemMessage(errorMessage);
                return ""; // Return empty to indicate no action should be taken
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error analyzing player input with AI: {e.Message}");
            string errorMessage = "I'm having trouble understanding your request. Please try:\n" +
                                "• Using numbers (1, 2, 3) to select from the options above\n" +
                                "• Rephrasing what you want to do\n" +
                                "• Being more specific about your action";
            AddSystemMessage(errorMessage);
            return ""; // Return empty to indicate no action should be taken
        }
    }
    
    private async System.Threading.Tasks.Task HandleSelectedAction(string responseType, string userMessage)
    {
        // If response type is empty, it means AI couldn't understand the player's intent
        if (string.IsNullOrEmpty(responseType))
        {
            Debug.Log("HandleSelectedAction: Empty response type - AI couldn't understand player intent, no action taken");
            return;
        }
        
        Debug.Log($"HandleSelectedAction: Handling {responseType} action...");
        
        TrackResponseType(responseType);
        
        switch (responseType.ToLower())
        {
            case "companion":
                Debug.Log("HandleSelectedAction: Calling GenerateCompanionInteraction");
                await GenerateCompanionInteraction(userMessage);
                break;
            case "combat":
                Debug.Log("HandleSelectedAction: Calling GenerateCombatScene");
                await GenerateCombatScene(userMessage);
                break;
            case "story":
                Debug.Log("HandleSelectedAction: Calling GenerateStoryProgression");
                await GenerateStoryProgression(userMessage);
                break;
            default:
                Debug.Log("HandleSelectedAction: Calling GenerateStoryProgression (default)");
                await GenerateStoryProgression(userMessage);
                break;
        }
        
        Debug.Log($"HandleSelectedAction: Completed {responseType} action.");
    }
    
    private async System.Threading.Tasks.Task GenerateCompanionInteraction(string userMessage)
    {
        string playerInfo = GetPlayerInformation();
        string storySummary = GetStorySummary();
        string chapterInstructions = GetChapterSpecificInstructions();
        
        string companionPrompt = $@"Chapter {currentChapter} ({messagesInCurrentChapter + 1}/{MESSAGES_PER_CHAPTER})
Player: {playerInfo}
Story: {storySummary}
Instructions: {chapterInstructions}
Input: {userMessage}

Generate a brief companion interaction scene. Focus on dialogue and character development.

=== COMPANION INTERACTION ===
[2-3 sentences describing the interaction with companions, their responses, and any insights gained]";
        
        string response = await AIService.Instance.SendMessageAsync(companionPrompt);
        AddAIMessage(response);
        
        // Add system message about companion interaction
        AddSystemMessage("Companion interaction complete. Press Next to continue.");
        
        // Clear action options to indicate we're in a special state
        currentActionOptions.Clear();
        
        await GenerateInteractionSummary(userMessage, response);
    }
    
    private async System.Threading.Tasks.Task GenerateStoryProgression(string userMessage)
    {
        string playerInfo = GetPlayerInformation();
        string storySummary = GetStorySummary();
        string chapterInstructions = GetChapterSpecificInstructions();
        
        string storyPrompt = $@"Chapter {currentChapter} ({messagesInCurrentChapter + 1}/{MESSAGES_PER_CHAPTER})
Player: {playerInfo}
Story: {storySummary}
Instructions: {chapterInstructions}
Input: {userMessage}

Generate brief story progression:

=== STORY SCENE ===
[2-3 sentences describing what happens next, natural and unexpected developments]";
        
        string response = await AIService.Instance.SendMessageAsync(storyPrompt);
        AddAIMessage(response);
        
        // Generate action options
        await GenerateActionOptions();
        
        await GenerateInteractionSummary(userMessage, response);
    }
    
    private async System.Threading.Tasks.Task GenerateCombatScene(string userMessage)
    {
        Debug.Log("GenerateCombatScene: Starting combat scene generation...");
        
        string playerInfo = GetPlayerInformation();
        string storySummary = GetStorySummary();
        string chapterInstructions = GetChapterSpecificInstructions();
        
        string combatPrompt = $@"Chapter {currentChapter} ({messagesInCurrentChapter + 1}/{MESSAGES_PER_CHAPTER})
Player: {playerInfo}
Story: {storySummary}
Instructions: {chapterInstructions}
Input: {userMessage}

Generate a brief combat scene:

=== COMBAT SCENE ===
[2-3 sentences describing the combat situation, enemies, and immediate danger]";
        
        string response = await AIService.Instance.SendMessageAsync(combatPrompt);
        AddAIMessage(response);
        
        // Add system message about combat transition
        AddSystemMessage("Combat scene ready. Press Next to enter battle.");
        
        // Clear action options to indicate we're in a special state
        currentActionOptions.Clear();
        
        await GenerateInteractionSummary(userMessage, response);
        
        Debug.Log("GenerateCombatScene: Combat scene generation complete. Waiting for Next button.");
    }
    
    private async System.Threading.Tasks.Task GenerateInteractionSummary(string userMessage, string aiResponse)
    {
        try
        {
            string summaryPrompt = $@"Summarize this RPG interaction in exactly ONE sentence. Focus on the key action, decision, or story development.

Player: {userMessage}
Narrator: {aiResponse}

Write a single sentence summary that captures the most important thing that happened in this interaction:";
            
            string summary = await AIService.Instance.SendMessageAsync(summaryPrompt);
            
            // Clean up the summary (remove quotes, extra formatting, etc.)
            summary = summary.Trim().Replace("\"", "").Replace("'", "");
            if (summary.EndsWith("."))
                summary = summary.Substring(0, summary.Length - 1);
            
            AddStoryPoint(summary);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating interaction summary: {e}");
            // Fallback to a simple summary
            AddStoryPoint($"Player and Narrator had an interaction in {currentChapter} chapter");
        }
    }
    
    private void AdvanceToNextChapter()
    {
        messagesInCurrentChapter = 0;
        
        // Reset response tracking for new chapter
        ResetResponseTracking();
        
        switch (currentChapter)
        {
            case StoryChapter.Tavern:
                currentChapter = StoryChapter.Plains;
                AddStoryPoint("Completed tavern chapter - party formed and bar fight won");
                break;
            case StoryChapter.Plains:
                currentChapter = StoryChapter.Desert;
                AddStoryPoint("Completed plains chapter - party bonded through easy encounters");
                break;
            case StoryChapter.Desert:
                currentChapter = StoryChapter.Mountains;
                AddStoryPoint("Completed desert chapter - party overcame environmental challenges");
                break;
            case StoryChapter.Mountains:
                currentChapter = StoryChapter.Castle;
                AddStoryPoint("Completed mountains chapter - party relationships tested and strengthened");
                break;
            case StoryChapter.Castle:
                currentChapter = StoryChapter.ThroneRoom;
                AddStoryPoint("Completed castle chapter - party reached the Dark King's throne room");
                break;
            case StoryChapter.ThroneRoom:
                AddStoryPoint("Story completed - Dark King defeated");
                break;
        }
    }
    
    private void ResetResponseTracking()
    {
        companionResponses = 0;
        combatResponses = 0;
        storyResponses = 0;
    }
    
    private void AddUserMessage(string content)
    {
        CreateSimpleMessage(content, "You", userMessageColor);
    }
    
    private void AddAIMessage(string content)
    {
        CreateSimpleMessage(content, "Narrator", aiMessageColor);
    }
    
    private void AddSystemMessage(string content)
    {
        CreateSimpleMessage(content, "System", systemMessageColor);
    }
    
    private void CreateSimpleMessage(string content, string sender, Color color)
    {
        if (chatText == null)
        {
            Debug.LogError("Chat Text component is not assigned!");
            return;
        }
        
        // Add to history first
        messageHistory.Add(new MessageData(sender, content, color));
        
        // Clean up old messages if needed
        if (messageHistory.Count > maxMessages)
        {
            RemoveOldestMessage();
        }
        
        // Rebuild the entire text
        RebuildChatText();
        
        // Scroll to bottom
        StartCoroutine(ScrollToBottom());
        
        // Save state periodically (every 5 messages to avoid too frequent saves)
        if (messageHistory.Count % 5 == 0)
        {
            SaveCurrentChatState();
        }
    }
    
    private void RemoveOldestMessage()
    {
        if (messageHistory.Count > 0)
        {
            // Remove the oldest message from history
            messageHistory.RemoveAt(0);
        }
    }
    
    private void RebuildChatText()
    {
        if (chatText == null) return;
        
        // Clear the text component properly
        chatText.text = "";
        chatText.ForceMeshUpdate();
        
        // Rebuild from message history
        foreach (var messageData in messageHistory)
        {
            string formattedMessage = $"{messageData.sender}: {messageData.content}\n\n";
            string colorHex = ColorUtility.ToHtmlStringRGB(messageData.color);
            string coloredMessage = $"<color=#{colorHex}>{formattedMessage}</color>";
            chatText.text += coloredMessage;
        }
        
        // Force multiple updates to ensure proper layout
        chatText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        
        if (messageContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer.GetComponent<RectTransform>());
        }
        
        // Force another update after layout rebuild
        chatText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
    }
    
    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
            
            // Force another update to ensure scrolling works
            yield return new WaitForEndOfFrame();
            chatScrollRect.verticalNormalizedPosition = 0f;
            
            // One more update to be sure
            yield return new WaitForEndOfFrame();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    private void SetUIState(bool enabled)
    {
        if (inputField != null)
            inputField.interactable = enabled;
        
        if (nextButton != null)
            nextButton.interactable = enabled;
    }
    
    public void GoBackToGame()
    {
        // Save current chat state before transitioning
        SaveCurrentChatState();
        
        SceneManager.LoadScene(gameSceneName);
    }
    
    public async void OnNextButtonClicked()
    {
        Debug.Log("OnNextButtonClicked: Next button clicked");
        
        // Check if we're in a special state that needs scene transition
        if (currentActionOptions.Count == 0)
        {
            Debug.Log("OnNextButtonClicked: No action options, checking for special state");
            
            // We're in a special state (like combat ready or companion interaction complete)
            // Check the last system message to determine what to do
            if (messageHistory.Count > 0)
            {
                var lastMessage = messageHistory[messageHistory.Count - 1];
                Debug.Log($"OnNextButtonClicked: Last message from {lastMessage.sender}: {lastMessage.content}");
                
                if (lastMessage.sender == "System" && lastMessage.content.Contains("Combat scene ready"))
                {
                    Debug.Log("OnNextButtonClicked: Combat scene ready detected, transitioning to combat");
                    
                    // Set flag before saving state
                    isReturningFromCombat = true;
                    
                    // Save current chat state before transitioning
                    SaveCurrentChatState();
                    
                    // Transition to combat scene
                    AddSystemMessage("Transitioning to combat...");
                    StartCoroutine(TransitionToCombatScene());
                    return;
                }
                else if (lastMessage.sender == "System" && lastMessage.content.Contains("Companion interaction complete"))
                {
                    Debug.Log("OnNextButtonClicked: Companion interaction complete, generating new options");
                    
                    // Continue with story progression
                    await GenerateActionOptions();
                    return;
                }
            }
        }
        
        Debug.Log("OnNextButtonClicked: Normal flow, generating new action options");
        
        // Save current chat state before transitioning
        SaveCurrentChatState();
        
        // Normal flow - generate new action options
        await GenerateActionOptions();
    }
    
    private IEnumerator TransitionToCombatScene()
    {
        // Wait a moment for the message to be displayed
        yield return new WaitForSeconds(2f);
        
        // Load the battle scene
        SceneManager.LoadScene(gameSceneName);
    }
    
    private async System.Threading.Tasks.Task GeneratePostCombatStory()
    {
        Debug.Log("GeneratePostCombatStory: Starting post-combat story generation...");
        
        isGenerating = true;
        SetUIState(false);
        
        try
        {
            // Use the existing story progression method for consistency
            string postCombatMessage = "The party has returned from combat. Continue the story.";
            await GenerateStoryProgression(postCombatMessage);
            
            // Add story point about the combat outcome
            AddStoryPoint($"Combat encounter completed in {currentChapter} chapter");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating post-combat story: {e.Message}");
            AddSystemMessage("The party continues their journey after the battle.");
            await GenerateActionOptions();
        }
        
        isGenerating = false;
        SetUIState(true);
    }
    
    // Debug methods for testing
    public void SendTestMessage(string content, bool isUser, bool isSystem = false)
    {
        if (isSystem)
        {
            AddSystemMessage(content);
        }
        else if (isUser)
        {
            AddUserMessage(content);
        }
        else
        {
            AddAIMessage(content);
        }
    }
    
    private async void StartWithAPIIntroduction()
    {
        // Clear any existing saved state when starting fresh
        if (ChatStateManager.Instance != null)
        {
            ChatStateManager.Instance.ClearSavedState();
        }
        
        isGenerating = true;
        SetUIState(false);
        
        try
        {
            string playerInfo = GetPlayerInformation();
            string storySummary = GetStorySummary();
            string initialPrompt = $@"CONTEXT: Chapter {currentChapter} (Message {messagesInCurrentChapter + 1}/{MESSAGES_PER_CHAPTER})
Player: {playerInfo}
Story: {storySummary}

INSTRUCTIONS: {GetChapterSpecificInstructions()}

INTRODUCTION: Generate a brief opening story scene for the tavern chapter.

=== STORY SCENE ===
[2-3 sentences describing the tavern setting, introducing the companions, and setting up the adventure.]";
            
            string response = await AIService.Instance.SendMessageAsync(initialPrompt);
            
            // Display the response
            AddAIMessage(response);
            
            // Generate action options
            await GenerateActionOptions();
            
            // Add initial story point
            AddStoryPoint("Player joined the adventuring party at the tavern");
            
            messagesInCurrentChapter++;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating initial message: {e}");
            AddSystemMessage("Welcome to the fantasy RPG! Type your message to begin your adventure.");
        }
        
        isGenerating = false;
        SetUIState(true);
    }
    
    private string GetPlayerInformation()
    {
        return "Brave adventurer seeking glory. Basic combat skills. Ready for any challenge.";
    }
    
    private string GetChapterContext()
    {
        switch (currentChapter)
        {
            case StoryChapter.Tavern:
                return "TAVERN: Adventure begins in cozy tavern. Player meets 4 adventurers and joins bar fight.";
            case StoryChapter.Plains:
                return "PLAINS: Party travels through peaceful plains. Light enemies. Happy, carefree chapter.";
            case StoryChapter.Desert:
                return "DESERT: Harsh desert terrain. Tougher enemies. Environmental challenges like sandstorms.";
            case StoryChapter.Mountains:
                return "MOUNTAINS: Most challenging chapter. Relationships tested. Member may get injured or leave.";
            case StoryChapter.Castle:
                return "CASTLE: Enter Dark King's castle. Mini-boss enemies. Relationships affect outcomes.";
            case StoryChapter.ThroneRoom:
                return "THRONE ROOM: Final confrontation with Dark King. Climax of adventure.";
            default:
                return "Unknown chapter";
        }
    }
    
    private string GetChapterSpecificInstructions()
    {
        switch (currentChapter)
        {
            case StoryChapter.Tavern:
                return "Tavern setting with 4 companions: Paladin (noble), Rogue (clever), Mage (wise), Warrior (strong). Include bar fight scene. Make tavern welcoming.";
            case StoryChapter.Plains:
                return "Keep mood light. 2-3 easy enemy encounters. Focus on party bonding. Describe beautiful plains scenery.";
            case StoryChapter.Desert:
                return "Increase enemy difficulty. Include environmental challenges (sandstorm, water shortage). Party works together. Focus on friendship.";
            case StoryChapter.Mountains:
                return "Test party relationships. Have member injured or threaten to leave. Test bonds formed. Make emotionally intense.";
            case StoryChapter.Castle:
                return "Mini-boss level enemies. Relationships affect story. Build up to final confrontation. Feel like climax approaching.";
            case StoryChapter.ThroneRoom:
                return "Brief dialogue before final boss. Focus on dramatic confrontation. Tie up loose story threads.";
            default:
                return "Continue adventure naturally.";
        }
    }
    
    private string GetStorySummary()
    {
        if (storyPoints.Count == 0)
            return "No significant story events yet.";
        
        string summary = "";
        foreach (var point in storyPoints)
        {
            summary += $"• {point.description} (Chapter: {point.chapter})\n";
        }
        
        return summary;
    }
    
    private void AddStoryPoint(string description)
    {
        storyPoints.Add(new StoryPoint(description, currentChapter, messagesInCurrentChapter));
        
        // Keep only the most recent 15 story points to prevent the summary from getting too long
        if (storyPoints.Count > 15)
        {
            storyPoints.RemoveAt(0);
        }
    }
    
    private void TrackResponseType(string responseType)
    {
        switch (responseType.ToLower())
        {
            case "companion":
                companionResponses++;
                break;
            case "combat":
                combatResponses++;
                break;
            case "story":
                storyResponses++;
                break;
        }
        
        // Keep tracking window manageable
        int total = companionResponses + combatResponses + storyResponses;
        if (total > TRACKING_WINDOW)
        {
            // Reduce all counts proportionally to maintain recent history
            float reductionFactor = (float)TRACKING_WINDOW / total;
            companionResponses = Mathf.RoundToInt(companionResponses * reductionFactor);
            combatResponses = Mathf.RoundToInt(combatResponses * reductionFactor);
            storyResponses = Mathf.RoundToInt(storyResponses * reductionFactor);
        }
    }
    
    private void OnDestroy()
    {
        // Properly dispose of TextMeshPro components
        if (chatText != null)
        {
            chatText.text = "";
            chatText.ForceMeshUpdate();
        }
        
        messageHistory.Clear();
    }
    
    // Save current chat state to ChatStateManager
    private void SaveCurrentChatState()
    {
        Debug.Log($"SimpleChatUI: SaveCurrentChatState() called - ChatStateManager.Instance: {ChatStateManager.Instance != null}, messageHistory.Count: {messageHistory.Count}, isReturningFromCombat: {isReturningFromCombat}");
        
        if (ChatStateManager.Instance != null)
        {
            // Convert SimpleChatUI types to global types for serialization
            List<global::MessageData> serializableMessages = new List<global::MessageData>();
            foreach (var msg in messageHistory)
            {
                serializableMessages.Add(new global::MessageData(msg.sender, msg.content, msg.color));
            }
            
            List<global::StoryPoint> serializableStoryPoints = new List<global::StoryPoint>();
            foreach (var point in storyPoints)
            {
                serializableStoryPoints.Add(new global::StoryPoint(point.description, (int)point.chapter, point.messageIndex));
            }
            
            Debug.Log($"SimpleChatUI: Saving state with {serializableMessages.Count} messages, {serializableStoryPoints.Count} story points, chapter: {currentChapter}, isReturningFromCombat: {isReturningFromCombat}");
            
            ChatStateManager.Instance.SaveChatState(serializableMessages, serializableStoryPoints,
                                                   (int)currentChapter, messagesInCurrentChapter,
                                                   companionResponses, combatResponses, storyResponses,
                                                   isReturningFromCombat);
        }
        else
        {
            Debug.LogError("SimpleChatUI: ChatStateManager.Instance is null, cannot save state!");
        }
    }
    
    // Load persisted state from ChatStateManager
    public void LoadPersistedState(List<SimpleChatUI.MessageData> savedMessageHistory, List<SimpleChatUI.StoryPoint> savedStoryPoints,
                                  int savedChapter, int savedMessagesInChapter,
                                  int savedCompanionResponses, int savedCombatResponses, int savedStoryResponses,
                                  bool savedIsReturningFromCombat = false)
    {
        Debug.Log($"SimpleChatUI: LoadPersistedState() called with {savedMessageHistory.Count} messages, {savedStoryPoints.Count} story points, chapter: {savedChapter}, isReturningFromCombat: {savedIsReturningFromCombat}");
        
        // Restore message history
        messageHistory.Clear();
        messageHistory.AddRange(savedMessageHistory);
        
        // Restore story points
        storyPoints.Clear();
        storyPoints.AddRange(savedStoryPoints);
        
        // Restore chapter and progress
        currentChapter = (StoryChapter)savedChapter;
        messagesInCurrentChapter = savedMessagesInChapter;
        
        // Restore response tracking
        companionResponses = savedCompanionResponses;
        combatResponses = savedCombatResponses;
        storyResponses = savedStoryResponses;
        
        // Restore combat return flag
        isReturningFromCombat = savedIsReturningFromCombat;
        
        // Rebuild the UI
        RebuildChatText();
        
        Debug.Log($"SimpleChatUI: Loaded chat state: Chapter {currentChapter}, {messageHistory.Count} messages, {storyPoints.Count} story points, isReturningFromCombat: {isReturningFromCombat}");
    }
    
    private async System.Threading.Tasks.Task GenerateActionOptions()
    {
        Debug.Log("GenerateActionOptions: Starting to generate options...");
        
        // Clear previous options
        currentActionOptions.Clear();
        
        // Get recent story context for better option generation
        string storyContext = GetRecentStoryContext();
        Debug.Log($"GenerateActionOptions: Story context: {storyContext}");
        
        // Create a prompt for the AI to generate contextually relevant action options
        string contextPrompt = $"Based on the current story situation, generate 3 different action options for the player. " +
                              $"Recent story context: {storyContext}\n\n" +
                              $"Provide one combat-focused option, one companion interaction option, and one story progression option. " +
                              $"Make each option specific and relevant to what's happening right now. " +
                              $"Format your response as:\n" +
                              $"COMBAT: [combat option description]\n" +
                              $"COMPANION: [companion interaction option]\n" +
                              $"STORY: [story progression option]\n" +
                              $"Keep each option concise but descriptive (1-2 sentences).";
        
        try
        {
            Debug.Log("GenerateActionOptions: Sending prompt to AI...");
            string aiResponse = await AIService.Instance.SendMessageAsync(contextPrompt);
            Debug.Log($"GenerateActionOptions: AI Response: {aiResponse}");
            
            // Parse the AI response to extract the three options
            string[] lines = aiResponse.Split('\n');
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("COMBAT:"))
                {
                    string combatOption = trimmedLine.Substring("COMBAT:".Length).Trim();
                    currentActionOptions.Add(new ActionOption(combatOption, "combat", "Engage in combat"));
                    Debug.Log($"GenerateActionOptions: Added combat option: {combatOption}");
                }
                else if (trimmedLine.StartsWith("COMPANION:"))
                {
                    string companionOption = trimmedLine.Substring("COMPANION:".Length).Trim();
                    currentActionOptions.Add(new ActionOption(companionOption, "companion", "Interact with companions"));
                    Debug.Log($"GenerateActionOptions: Added companion option: {companionOption}");
                }
                else if (trimmedLine.StartsWith("STORY:"))
                {
                    string storyOption = trimmedLine.Substring("STORY:".Length).Trim();
                    currentActionOptions.Add(new ActionOption(storyOption, "story", "Progress the story"));
                    Debug.Log($"GenerateActionOptions: Added story option: {storyOption}");
                }
            }
            
            // If AI parsing failed, fall back to basic options
            if (currentActionOptions.Count < 3)
            {
                Debug.LogWarning($"GenerateActionOptions: Failed to parse AI-generated options (got {currentActionOptions.Count}), using fallback options");
                currentActionOptions.Clear();
                currentActionOptions.Add(new ActionOption("Fight the enemy with all your might", "combat", "Engage in combat"));
                currentActionOptions.Add(new ActionOption("Talk to your companions about the situation", "companion", "Interact with companions"));
                currentActionOptions.Add(new ActionOption("Continue exploring the area", "story", "Progress the story"));
            }
            
            Debug.Log($"GenerateActionOptions: Generated {currentActionOptions.Count} options, calling DisplayActionOptions");
            DisplayActionOptions();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GenerateActionOptions: Error generating action options: {e.Message}");
            // Fallback to basic options if AI fails
            currentActionOptions.Clear();
            currentActionOptions.Add(new ActionOption("Fight the enemy with all your might", "combat", "Engage in combat"));
            currentActionOptions.Add(new ActionOption("Talk to your companions about the situation", "companion", "Interact with companions"));
            currentActionOptions.Add(new ActionOption("Continue exploring the area", "story", "Progress the story"));
            Debug.Log("GenerateActionOptions: Using fallback options, calling DisplayActionOptions");
            DisplayActionOptions();
        }
    }
    
    private string GetRecentStoryContext()
    {
        // Get the last few messages to provide context
        string context = "You are in a fantasy RPG world. ";
        
        // Add recent story points if available
        if (ChatStateManager.Instance != null && ChatStateManager.Instance.chatState.storyPoints.Count > 0)
        {
            var recentStoryPoints = ChatStateManager.Instance.chatState.storyPoints.TakeLast(3).ToList();
            context += "Recent events: " + string.Join(" ", recentStoryPoints.Select(sp => sp.description)) + ". ";
        }
        
        // Add recent messages for context
        if (messageHistory.Count > 0)
        {
            var recentMessages = messageHistory.TakeLast(3).ToList();
            context += "Recent conversation: " + string.Join(" ", recentMessages.Select(m => m.content)) + ". ";
        }
        
        return context;
    }
    
    private void DisplayActionOptions()
    {
        Debug.Log($"DisplayActionOptions: Called with {currentActionOptions.Count} options");
        
        if (currentActionOptions.Count == 0)
        {
            Debug.LogWarning("DisplayActionOptions: No action options to display!");
            return;
        }
        
        string optionsText = "\n\nWhat would you like to do next?\n";
        for (int i = 0; i < currentActionOptions.Count; i++)
        {
            // Only show the displayText, which is stripped of tags
            optionsText += $"{i + 1}. {currentActionOptions[i].displayText}\n";
            Debug.Log($"DisplayActionOptions: Option {i + 1}: {currentActionOptions[i].displayText}");
        }
        
        Debug.Log($"DisplayActionOptions: Final options text: {optionsText}");
        AddAIMessage(optionsText);
    }
} 