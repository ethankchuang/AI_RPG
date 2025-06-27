using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class ChatStateData
{
    public List<MessageData> messageHistory = new List<MessageData>();
    public List<StoryPoint> storyPoints = new List<StoryPoint>();
    public int currentChapter;
    public int messagesInCurrentChapter;
    public int companionResponses;
    public int combatResponses;
    public int storyResponses;
    public bool hasInitialized = false;
    public bool isReturningFromCombat = false;
}

[System.Serializable]
public class MessageData
{
    public string sender;
    public string content;
    public string colorHex;
    
    public MessageData(string sender, string content, Color color)
    {
        this.sender = sender;
        this.content = content;
        this.colorHex = ColorUtility.ToHtmlStringRGB(color);
    }
    
    public Color GetColor()
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + colorHex, out color);
        return color;
    }
}

[System.Serializable]
public class StoryPoint
{
    public string description;
    public int chapter;
    public int messageIndex;
    
    public StoryPoint(string description, int chapter, int messageIndex)
    {
        this.description = description;
        this.chapter = chapter;
        this.messageIndex = messageIndex;
    }
}

public class ChatStateManager : MonoBehaviour
{
    public static ChatStateManager Instance { get; private set; }
    
    [Header("Chat State")]
    public ChatStateData chatState = new ChatStateData();
    
    private void Awake()
    {
        Debug.Log($"ChatStateManager: Awake() called - Instance: {Instance}");
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ChatStateManager: Initialized and set to persist across scenes");
            
            // Initialize default state if not already set
            if (!chatState.hasInitialized)
            {
                Debug.Log("ChatStateManager: Initializing default state");
                InitializeDefaultState();
            }
            else
            {
                Debug.Log($"ChatStateManager: State already initialized with {chatState.messageHistory.Count} messages");
            }
        }
        else
        {
            Debug.Log("ChatStateManager: Another instance found, destroying duplicate");
            // Don't destroy the duplicate - instead, transfer any new state to the existing instance
            if (chatState.messageHistory.Count > 0 && Instance.chatState.messageHistory.Count == 0)
            {
                Debug.Log("ChatStateManager: Transferring state from duplicate to existing instance");
                Instance.chatState = chatState;
            }
            Destroy(gameObject);
        }
    }
    
    private void InitializeDefaultState()
    {
        chatState.currentChapter = 0; // Tavern
        chatState.messagesInCurrentChapter = 0;
        chatState.companionResponses = 0;
        chatState.combatResponses = 0;
        chatState.storyResponses = 0;
        chatState.hasInitialized = true;
    }
    
    // Save chat state from SimpleChatUI
    public void SaveChatState(List<global::MessageData> messageHistory, List<global::StoryPoint> storyPoints, 
                             int currentChapter, int messagesInCurrentChapter,
                             int companionResponses, int combatResponses, int storyResponses,
                             bool isReturningFromCombat = false)
    {
        Debug.Log($"ChatStateManager: SaveChatState() called with {messageHistory.Count} messages, {storyPoints.Count} story points, chapter: {currentChapter}, isReturningFromCombat: {isReturningFromCombat}");
        
        // Store the provided data directly (it's already in the correct format)
        chatState.messageHistory.Clear();
        chatState.messageHistory.AddRange(messageHistory);
        
        chatState.storyPoints.Clear();
        chatState.storyPoints.AddRange(storyPoints);
        
        chatState.currentChapter = currentChapter;
        chatState.messagesInCurrentChapter = messagesInCurrentChapter;
        chatState.companionResponses = companionResponses;
        chatState.combatResponses = combatResponses;
        chatState.storyResponses = storyResponses;
        chatState.isReturningFromCombat = isReturningFromCombat;
        
        Debug.Log($"ChatStateManager: Saved chat state with {chatState.messageHistory.Count} messages, isReturningFromCombat: {isReturningFromCombat}");
    }
    
    // Check if there's saved state to load
    public bool HasSavedState()
    {
        // Check if we have any saved messages - this is the primary indicator of saved state
        bool hasState = chatState.messageHistory.Count > 0;
        Debug.Log($"ChatStateManager: HasSavedState() called - hasInitialized: {chatState.hasInitialized}, messageCount: {chatState.messageHistory.Count}, returning: {hasState}");
        return hasState;
    }
    
    // Load chat state into SimpleChatUI
    public void LoadChatState(SimpleChatUI chatUI)
    {
        if (chatUI == null) 
        {
            Debug.LogError("ChatStateManager: LoadChatState called with null chatUI!");
            return;
        }
        
        Debug.Log($"ChatStateManager: LoadChatState called with {chatState.messageHistory.Count} messages, isReturningFromCombat: {chatState.isReturningFromCombat}");
        
        // Ensure the state is properly initialized
        EnsureStateInitialized();
        
        // Convert back to SimpleChatUI's format
        List<SimpleChatUI.MessageData> messageHistory = new List<SimpleChatUI.MessageData>();
        foreach (var msg in chatState.messageHistory)
        {
            messageHistory.Add(new SimpleChatUI.MessageData(msg.sender, msg.content, msg.GetColor()));
        }
        
        List<SimpleChatUI.StoryPoint> storyPoints = new List<SimpleChatUI.StoryPoint>();
        foreach (var point in chatState.storyPoints)
        {
            storyPoints.Add(new SimpleChatUI.StoryPoint(point.description, (SimpleChatUI.StoryChapter)point.chapter, point.messageIndex));
        }
        
        // Load the state into the chat UI
        chatUI.LoadPersistedState(messageHistory, storyPoints, 
                                 chatState.currentChapter, chatState.messagesInCurrentChapter,
                                 chatState.companionResponses, chatState.combatResponses, chatState.storyResponses,
                                 chatState.isReturningFromCombat);
        
        Debug.Log($"ChatStateManager: Loaded chat state with {messageHistory.Count} messages, isReturningFromCombat: {chatState.isReturningFromCombat}");
    }
    
    // Ensure the state is properly initialized when loading
    private void EnsureStateInitialized()
    {
        if (!chatState.hasInitialized && chatState.messageHistory.Count > 0)
        {
            Debug.Log("ChatStateManager: Ensuring state is properly initialized for loaded data");
            chatState.hasInitialized = true;
        }
    }
    
    // Clear saved state (for new game)
    public void ClearSavedState()
    {
        chatState = new ChatStateData();
        Debug.Log("ChatStateManager: Cleared saved state");
    }
} 