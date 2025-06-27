using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class SimpleChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private Button backToGameButton;
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
    private enum StoryChapter { Tavern, Plains, Desert, Mountains, Castle, ThroneRoom }
    private StoryChapter currentChapter = StoryChapter.Tavern;
    private int messagesInCurrentChapter = 0;
    private const int MESSAGES_PER_CHAPTER = 5;
    
    // Response type tracking for balancing
    private int companionResponses = 0;
    private int combatResponses = 0;
    private int storyResponses = 0;
    private const int TRACKING_WINDOW = 10; // Track last 10 responses for balancing
    
    // Track last response type to prevent consecutive story sections
    private string lastResponseType = "";
    
    // Track the last displayed action options for numeric input mapping
    private List<string> lastActionOptions = new List<string>();
    
    // Track the response types for each action option
    private List<string> lastActionResponseTypes = new List<string>();
    
    // Message data structure to track sender and content
    private class MessageData
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
    private class StoryPoint
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
        // Setup input field
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnInputSubmitted);
            inputField.onValueChanged.AddListener(OnInputChanged);
            inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Type your message here...";
        }
        
        // Setup back button
        if (backToGameButton != null)
        {
            backToGameButton.onClick.AddListener(GoBackToGame);
        }
        
        StartWithAPIIntroduction();
        EnsureAIServiceExists();
        
        SetUIState(true);
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
        isGenerating = true;
        SetUIState(false);
        
        try
        {
            // First, let the AI decide what type of response is needed
            string responseType = await DetermineResponseType(userMessage);
            
            // Check if the response is the same as the last one
            if (responseType.ToLower() == lastResponseType)
            {
                // Use fallback to get a different response type
                responseType = GetWeightedFallbackResponseType();
                Debug.Log($"AI returned same type as last ({lastResponseType}), using fallback: {responseType}");
            }
            
            // Log the output type and message
            Debug.Log($"Output Type: {responseType.ToUpper()} | Message: {userMessage}");
            
            // Call the appropriate function based on the response type
            switch (responseType.ToLower())
            {
                case "companion":
                    TrackResponseType("companion");
                    await GenerateCompanionInteraction(userMessage);
                    // Automatically add story prompt after companion interaction
                    TrackResponseType("story");
                    await GenerateStoryProgression("Continue the story after the companion interaction");
                    break;
                case "story":
                    TrackResponseType("story");
                    await GenerateStoryProgression(userMessage);
                    break;
                case "combat":
                    TrackResponseType("combat");
                    await GenerateCombatScene(userMessage);
                    // Automatically add story prompt after combat
                    TrackResponseType("story");
                    await GenerateStoryProgression("Continue the story after the combat");
                    break;
                case "unsure":
                    await GenerateClarificationRequest(userMessage);
                    break;
                default:
                    // Use weighted random selection to bias towards companion and combat
                    string fallbackType = GetWeightedFallbackResponseType();
                    
                    // Log the fallback output type
                    Debug.Log($"Fallback Output Type: {fallbackType.ToUpper()} | Message: {userMessage}");
                    
                    TrackResponseType(fallbackType);
                    
                    switch (fallbackType)
                    {
                        case "companion":
                            await GenerateCompanionInteraction(userMessage);
                            // Automatically add story prompt after companion interaction
                            TrackResponseType("story");
                            await GenerateStoryProgression("Continue the story after the companion interaction");
                            break;
                        case "combat":
                            await GenerateCombatScene(userMessage);
                            // Automatically add story prompt after combat
                            TrackResponseType("story");
                            await GenerateStoryProgression("Continue the story after the combat");
                            break;
                        case "story":
                            await GenerateStoryProgression(userMessage);
                            break;
                        default:
                            await GenerateCompanionInteraction(userMessage);
                            // Automatically add story prompt after companion interaction
                            TrackResponseType("story");
                            await GenerateStoryProgression("Continue the story after the companion interaction");
                            break;
                    }
                    break;
            }
            
            // Update story progression
            messagesInCurrentChapter++;
            if (messagesInCurrentChapter >= MESSAGES_PER_CHAPTER)
            {
                AdvanceToNextChapter();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating response: {e}");
            AddSystemMessage("Sorry, an error occurred while generating the response. Please try again.");
        }
        
        isGenerating = false;
        SetUIState(true);
    }
    
    private async System.Threading.Tasks.Task<string> DetermineResponseType(string userMessage)
    {
        // Check for numeric input with specific mappings
        // If player enters a number (1-3), use the tracked response types from the last displayed options
        string numericResponse = await GetNumericResponseType(userMessage);
        if (!string.IsNullOrEmpty(numericResponse))
        {
            return numericResponse;
        }
        
        // If player enters their own text message, let the AI determine what happens next
        // Add spontaneous combat chance (15% chance when not explicitly story)
        if (lastResponseType != "story" && UnityEngine.Random.Range(0f, 1f) < 0.15f)
        {
            return "combat";
        }
        
        string playerInfo = GetPlayerInformation();
        string storySummary = GetStorySummary();
        
        string decisionPrompt = $@"Chapter {currentChapter} ({messagesInCurrentChapter + 1}/{MESSAGES_PER_CHAPTER})
Player: {playerInfo}
Story: {storySummary}
Input: {userMessage}

Choose response type (ONE WORD):
1. ""combat"" - fighting, danger, enemies, ambush
2. ""companion"" - talking, social, questions
3. ""story"" - exploration, travel, progress
4. ""unsure"" - unclear input

Combat can be spontaneous. If unsure, respond ""unsure"".";
        
        string response = await AIService.Instance.SendMessageAsync(decisionPrompt);
        return response.Trim().ToLower();
    }
    
    private async System.Threading.Tasks.Task<string> GetNumericResponseType(string userMessage)
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
        
        // If we have a valid option and tracked action types, use them
        if (selectedOption >= 1 && selectedOption <= 3 && lastActionResponseTypes.Count >= 3)
        {
            string responseType = lastActionResponseTypes[selectedOption - 1];
            Debug.Log($"Numeric input {selectedOption} mapped to {responseType} from tracked options");
            return responseType;
        }
        
        // If no tracked options but we have a valid number, use fixed mapping as fallback
        if (selectedOption >= 1 && selectedOption <= 3)
        {
            string responseType = GetFixedNumericResponseType(trimmedMessage);
            Debug.Log($"Numeric input {selectedOption} mapped to {responseType} using fixed mapping (no tracked options)");
            return responseType;
        }
        
        return "";
    }
    
    private string GetFixedNumericResponseType(string trimmedMessage)
    {
        // Fallback to fixed mapping when no action options are tracked
        if (trimmedMessage.Length == 1 && char.IsDigit(trimmedMessage[0]))
        {
            int number = int.Parse(trimmedMessage);
            switch (number)
            {
                case 1: return "combat";
                case 2: return "companion";
                case 3: return "story";
                default: return "";
            }
        }
        
        if (trimmedMessage.Length > 1 && char.IsDigit(trimmedMessage[0]))
        {
            string numberPart = "";
            int i = 0;
            while (i < trimmedMessage.Length && (char.IsDigit(trimmedMessage[i]) || trimmedMessage[i] == '.'))
            {
                numberPart += trimmedMessage[i];
                i++;
            }
            
            if (int.TryParse(numberPart.Replace(".", ""), out int actionNumber))
            {
                switch (actionNumber)
                {
                    case 1: return "combat";
                    case 2: return "companion";
                    case 3: return "story";
                    default: return "";
                }
            }
        }
        
        return "";
    }
    
    private string GetResponseTypeFromAction(string actionText)
    {
        // Instead of using keywords, we'll ask the AI to determine the response type
        // This will be handled in the DetermineResponseType method when numeric input is detected
        return "";
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

Generate companion interaction:

=== OPENING SCENE ===
[2-3 sentences: current setting and recent events]

=== COMPANION INTERACTION SUGGESTIONS ===
1. [Paladin: Ask about their code of honor]
2. [Rogue: Tell them a joke]
3. [Mage: Ask about their studies]
4. [Warrior: Compliment their strength]

Keep suggestions SHORT (3-5 words max).";
        
        string response = await AIService.Instance.SendMessageAsync(companionPrompt);
        ParseAndDisplayNarratorResponse(response);
        await GenerateInteractionSummary(userMessage, response);
    }
    
    private async System.Threading.Tasks.Task GenerateStoryProgression(string userMessage)
    {
        string playerInfo = GetPlayerInformation();
        string storySummary = GetStorySummary();
        string chapterInstructions = GetChapterSpecificInstructions();
        
        // Determine response types ourselves (randomized)
        string[] responseTypes = { "combat", "companion", "story" };
        for (int i = responseTypes.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = responseTypes[i];
            responseTypes[i] = responseTypes[j];
            responseTypes[j] = temp;
        }
        
        // Track the response types for numeric input mapping
        lastActionResponseTypes.Clear();
        lastActionResponseTypes.AddRange(responseTypes);
        
        string storyPrompt = $@"Chapter {currentChapter} ({messagesInCurrentChapter + 1}/{MESSAGES_PER_CHAPTER})
Player: {playerInfo}
Story: {storySummary}
Instructions: {chapterInstructions}
Input: {userMessage}

Generate story progression:

=== STORY SCENE ===
[4-6 sentences: what happens next, natural and unexpected]

Generate exactly 3 action options in this order:
1. {responseTypes[0].ToUpper()} action - generate one {responseTypes[0]} action
2. {responseTypes[1].ToUpper()} action - generate one {responseTypes[1]} action  
3. {responseTypes[2].ToUpper()} action - generate one {responseTypes[2]} action

Make the actions natural and varied. Do not include any labels or brackets.";
        
        string response = await AIService.Instance.SendMessageAsync(storyPrompt);
        ParseAndDisplayStoryResponse(response);
        await GenerateInteractionSummary(userMessage, response);
    }
    
    private async System.Threading.Tasks.Task GenerateCombatScene(string userMessage)
    {
        string playerInfo = GetPlayerInformation();
        string storySummary = GetStorySummary();
        string chapterInstructions = GetChapterSpecificInstructions();
        
        // Let the AI determine if this is spontaneous or player-initiated based on context
        string combatPrompt = $@"Chapter {currentChapter} ({messagesInCurrentChapter + 1}/{MESSAGES_PER_CHAPTER})
Player: {playerInfo}
Story: {storySummary}
Instructions: {chapterInstructions}
Input: {userMessage}

Generate combat scene:

=== COMBAT SETUP ===
[2-3 sentences: enemies appear, ambush, danger - unexpected and natural]

=== BATTLE PREPARATION ===
[1-2 sentences: situation escalates, enemies act]

=== TRANSITION TO COMBAT ===
[1 sentence: natural transition to combat]

Describe events, don't give instructions.";
        
        string response = await AIService.Instance.SendMessageAsync(combatPrompt);
        ParseAndDisplayCombatResponse(response);
        await GenerateInteractionSummary(userMessage, response);
        
        // TODO: Transition to combat scene
        AddSystemMessage("Transitioning to combat...");
    }
    
    private async System.Threading.Tasks.Task GenerateClarificationRequest(string userMessage)
    {
        string playerInfo = GetPlayerInformation();
        string storySummary = GetStorySummary();
        string chapterInstructions = GetChapterSpecificInstructions();
        
        // Determine response types ourselves (randomized)
        string[] responseTypes = { "combat", "companion", "story" };
        for (int i = responseTypes.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = responseTypes[i];
            responseTypes[i] = responseTypes[j];
            responseTypes[j] = temp;
        }
        
        // Track the response types for numeric input mapping
        lastActionResponseTypes.Clear();
        lastActionResponseTypes.AddRange(responseTypes);
        
        string clarificationPrompt = $@"Chapter {currentChapter} ({messagesInCurrentChapter + 1}/{MESSAGES_PER_CHAPTER})
Player: {playerInfo}
Story: {storySummary}
Instructions: {chapterInstructions}
Input: {userMessage}

Input unclear. Ask for clarification:

=== CLARIFICATION REQUEST ===
[2-3 sentences: acknowledge input, ask for clarification]

Generate exactly 3 action options in this order:
1. {responseTypes[0].ToUpper()} action - generate one {responseTypes[0]} action
2. {responseTypes[1].ToUpper()} action - generate one {responseTypes[1]} action  
3. {responseTypes[2].ToUpper()} action - generate one {responseTypes[2]} action

Make the actions natural and varied. Do not include any labels or brackets. Be encouraging, don't assume intent.";
        
        string response = await AIService.Instance.SendMessageAsync(clarificationPrompt);
        ParseAndDisplayClarificationResponse(response);
    }
    
    private void ParseAndDisplayNarratorResponse(string fullResponse)
    {
        // Split the response into sections
        string[] sections = fullResponse.Split(new string[] { "===" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (sections.Length >= 6) // We expect 6 sections: 3 headers + 3 content
        {
            // Extract opening scene (section 1 contains the content)
            string openingScene = sections.Length > 1 ? sections[1].Trim() : "";
            if (!string.IsNullOrEmpty(openingScene))
            {
                AddAIMessage(openingScene);
            }
            
            // Extract companion suggestions (section 3 contains the content)
            string companionSuggestions = sections.Length > 3 ? sections[3].Trim() : "";
            if (!string.IsNullOrEmpty(companionSuggestions))
            {
                AddAIMessage(companionSuggestions);
            }
            
            // Extract closing scene (section 5 contains the content)
            string closingScene = sections.Length > 5 ? sections[5].Trim() : "";
            if (!string.IsNullOrEmpty(closingScene))
            {
                AddAIMessage(closingScene);
            }
        }
        else
        {
            // Fallback: display the full response as-is
            Debug.LogWarning($"Expected 6+ sections, got {sections.Length}. Using fallback display.");
            AddAIMessage(fullResponse);
        }
    }
    
    private void ParseAndDisplayCombatResponse(string fullResponse)
    {
        // Split the response into sections
        string[] sections = fullResponse.Split(new string[] { "===" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (sections.Length >= 3)
        {
            // Extract combat setup (section 1 contains the content)
            string combatSetup = sections.Length > 1 ? sections[1].Trim() : "";
            if (!string.IsNullOrEmpty(combatSetup))
            {
                AddAIMessage(combatSetup);
            }
            
            // Extract battle preparation (section 3 contains the content)
            string battlePrep = sections.Length > 3 ? sections[3].Trim() : "";
            if (!string.IsNullOrEmpty(battlePrep))
            {
                AddAIMessage(battlePrep);
            }
            
            // Extract transition (section 5 contains the content)
            string transition = sections.Length > 5 ? sections[5].Trim() : "";
            if (!string.IsNullOrEmpty(transition))
            {
                AddAIMessage(transition);
            }
        }
        else
        {
            // Fallback: display the full response as-is
            Debug.LogWarning($"Expected 6+ sections in combat response, got {sections.Length}. Using fallback display.");
            AddAIMessage(fullResponse);
        }
    }
    
    private void ParseAndDisplayStoryResponse(string fullResponse)
    {
        Debug.Log($"Parsing story response: {fullResponse}");
        
        // Just display the full response as-is
        AddAIMessage(fullResponse);
        
        // Only do minimal parsing to track action options for numeric input
        TrackActionOptionsForNumericInput(fullResponse);
    }
    
    private void TrackActionOptionsForNumericInput(string fullResponse)
    {
        lastActionOptions.Clear();
        lastActionResponseTypes.Clear();
        
        // Simple parsing: look for numbered options and extract response types
        string[] lines = fullResponse.Split('\n');
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("1.") || trimmedLine.StartsWith("2.") || trimmedLine.StartsWith("3."))
            {
                // Extract the action text (remove the number and period)
                int dotIndex = trimmedLine.IndexOf('.');
                if (dotIndex > 0 && dotIndex < trimmedLine.Length - 1)
                {
                    string actionText = trimmedLine.Substring(dotIndex + 1).Trim();
                    
                    // Simple keyword-based mapping for response types
                    string responseType = "";
                    string lowerAction = actionText.ToLower();
                    
                    if (lowerAction.Contains("combat") || lowerAction.Contains("fight") || lowerAction.Contains("attack") || 
                        lowerAction.Contains("engage") || lowerAction.Contains("defend") || lowerAction.Contains("battle"))
                        responseType = "combat";
                    else if (lowerAction.Contains("companion") || lowerAction.Contains("talk") || lowerAction.Contains("ask") || 
                             lowerAction.Contains("discuss") || lowerAction.Contains("share") || lowerAction.Contains("social"))
                        responseType = "companion";
                    else if (lowerAction.Contains("story") || lowerAction.Contains("explore") || lowerAction.Contains("investigate") || 
                             lowerAction.Contains("search") || lowerAction.Contains("prepare") || lowerAction.Contains("continue"))
                        responseType = "story";
                    else
                        responseType = "story"; // Default fallback
                    
                    lastActionOptions.Add(actionText);
                    lastActionResponseTypes.Add(responseType);
                    
                    Debug.Log($"Tracked option: {trimmedLine.Substring(0, dotIndex + 1)} -> {responseType} -> '{actionText}'");
                }
            }
        }
        
        Debug.Log($"Tracked action options (player view): {string.Join(", ", lastActionOptions)}");
        Debug.Log($"Tracked response types: {string.Join(", ", lastActionResponseTypes)}");
    }
    
    private void ParseAndDisplayClarificationResponse(string fullResponse)
    {
        // Just display the full response as-is
        AddAIMessage(fullResponse);
        
        // Only do minimal parsing to track action options for numeric input
        TrackActionOptionsForNumericInput(fullResponse);
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
        lastResponseType = ""; // Reset last response type for new chapter
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
        
        if (backToGameButton != null)
            backToGameButton.interactable = enabled;
    }
    
    public void GoBackToGame()
    {
        SceneManager.LoadScene(gameSceneName);
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

INTRODUCTION: Generate the opening story scene for the tavern chapter.

FORMAT YOUR RESPONSE EXACTLY LIKE THIS:

=== STORY SCENE ===
[4-6 sentences describing the tavern setting, introducing the companions, and setting up the adventure. This can be longer than other scenes.]

What would you like to do next?
1. [Action suggestion 1]
2. [Action suggestion 2]
3. [Action suggestion 3]";
            
            string response = await AIService.Instance.SendMessageAsync(initialPrompt);
            
            // Parse the response into sections
            ParseAndDisplayStoryResponse(response);
            
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
    
    private string GetWeightedFallbackResponseType()
    {
        // Get current weights based on recent response frequency
        var weights = GetAdjustedWeights();
        
        float random = UnityEngine.Random.Range(0f, 1f);
        
        if (random < weights.companionWeight)
        {
            return "companion";
        }
        else if (random < weights.companionWeight + weights.combatWeight)
        {
            return "combat";
        }
        else
        {
            return "story";
        }
    }
    
    private (float companionWeight, float combatWeight, float storyWeight) GetAdjustedWeights()
    {
        int totalResponses = companionResponses + combatResponses + storyResponses;
        
        if (totalResponses < 3) // Not enough data, use default weights
        {
            return (0.5f, 0.4f, 0.1f); // Updated default weights: 50% companion, 40% combat, 10% story
        }
        
        // Calculate current frequencies
        float companionFreq = (float)companionResponses / totalResponses;
        float combatFreq = (float)combatResponses / totalResponses;
        float storyFreq = (float)storyResponses / totalResponses;
        
        // Adjust weights to push towards desired balance (50% companion, 40% combat, 10% story)
        float companionWeight = 0.3f + (0.3f - companionFreq) * 0.3f; // Boost if under 50%
        float combatWeight = 0.4f + (0.4f - combatFreq) * 0.3f; // Boost if under 40%
        float storyWeight = 0.3f + (0.3f - storyFreq) * 0.3f; // Boost if under 10%
        
        // Normalize weights
        float totalWeight = companionWeight + combatWeight + storyWeight;
        companionWeight /= totalWeight;
        combatWeight /= totalWeight;
        storyWeight /= totalWeight;
        
        return (companionWeight, combatWeight, storyWeight);
    }
    
    private void TrackResponseType(string responseType)
    {
        // Update the last response type
        lastResponseType = responseType.ToLower();
        
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
} 