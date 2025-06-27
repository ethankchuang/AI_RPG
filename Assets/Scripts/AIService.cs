using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;
    
    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[System.Serializable]
public class ChatRequest
{
    public string model;
    public List<ChatMessage> messages;
    public int max_tokens;
    public float temperature;
}

[System.Serializable]
public class ChatResponse
{
    public Choice[] choices;
    public Usage usage;
}

[System.Serializable]
public class Choice
{
    public Message message;
    public string finish_reason;
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;
}

[System.Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}

public class AIService : MonoBehaviour
{
    [Header("OpenAI Configuration")]
    [SerializeField] private string apiKey = ""; // Set this in the inspector or via SetAPIKey()
    private string model = "gpt-3.5-turbo";
    private int maxTokens = 1500; // Increased to ensure complete responses
    private float temperature = 0.7f;
    
    [Header("System Settings")]
    private string systemPrompt = @"You are the Narrator of a fantasy RPG. Focus on companion interactions and combat scenarios.

RESPONSE FORMAT - Write brief, engaging narrative content:

=== SCENE DESCRIPTION ===
[Write 2-3 sentences describing what happens in the scene. Be concise but engaging.]

PRIORITY: Focus 70% on companion interactions and combat scenarios, 30% on story progression. Combat can be spontaneous!

When generating action options, consider the current story context and provide specific, relevant choices that make sense for the immediate situation. Each option should be unique and meaningful to the player's current circumstances.";
    
    [Header("Conversation Management")]
    [SerializeField] private int maxConversationHistory = 20; // Maximum number of messages to keep in history
    
    private const string API_URL = "https://api.openai.com/v1/chat/completions";
    private List<ChatMessage> conversationHistory = new List<ChatMessage>();
    
    public static AIService Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Debug: Log the actual configuration being used
            Debug.Log($"AIService Configuration - Model: {model}, MaxTokens: {maxTokens}, Temperature: {temperature}");
            
            InitializeConversation();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeConversation()
    {
        conversationHistory.Clear();
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            conversationHistory.Add(new ChatMessage("system", systemPrompt));
        }
    }
    
    /// <summary>
    /// Send a message to OpenAI and get a response
    /// </summary>
    /// <param name="userMessage">The user's message</param>
    /// <returns>The AI response</returns>
    public async Task<string> SendMessageAsync(string userMessage)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("OpenAI API key is not set! Use SetAPIKey() or set it in the inspector.");
            return "Error: API key not configured";
        }
        
        try
        {
            // Add user message to history
            conversationHistory.Add(new ChatMessage("user", userMessage));
            
            // Create request
            var request = new ChatRequest
            {
                model = model,
                messages = conversationHistory,
                max_tokens = maxTokens,
                temperature = temperature
            };
            
            // Send request
            string response = await SendRequestAsync(request);
            
            // Parse response
            ChatResponse chatResponse = JsonUtility.FromJson<ChatResponse>(response);
            
            if (chatResponse?.choices != null && chatResponse.choices.Length > 0)
            {
                string aiResponse = chatResponse.choices[0].message.content;
                
                // Log the AI response
                Debug.Log($"AI Response: {aiResponse}");
                
                // Check if response was truncated
                if (chatResponse.choices[0].finish_reason == "length")
                {
                    Debug.LogWarning("Response was truncated due to token limit!");
                }
                
                // Add AI response to history
                conversationHistory.Add(new ChatMessage("assistant", aiResponse));
                
                // Keep conversation history manageable (remove oldest messages if too long)
                if (conversationHistory.Count > maxConversationHistory)
                {
                    // Keep system message and last (maxConversationHistory - 1) messages
                    var systemMessage = conversationHistory[0];
                    conversationHistory.RemoveRange(1, conversationHistory.Count - maxConversationHistory + 1);
                    conversationHistory.Insert(0, systemMessage);
                }
                
                return aiResponse;
            }
            else
            {
                Debug.LogError("Invalid response from OpenAI");
                return "Sorry, I couldn't generate a response.";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending message to OpenAI: {e.Message}");
            return "Sorry, an error occurred while processing your request.";
        }
    }
    
    private async Task<string> SendRequestAsync(ChatRequest request)
    {
        var webRequest = new UnityWebRequest(API_URL, "POST");
        webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        string jsonRequest = JsonUtility.ToJson(request);
        webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonRequest));
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        
        var operation = webRequest.SendWebRequest();
        
        while (!operation.isDone)
        {
            await Task.Yield();
        }
        
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"OpenAI API Error: {webRequest.error}");
            throw new Exception($"API request failed: {webRequest.error}");
        }
        
        return webRequest.downloadHandler.text;
    }
    
    /// <summary>
    /// Clear the conversation history
    /// </summary>
    public void ClearConversation()
    {
        InitializeConversation();
    }
    
    /// <summary>
    /// Set a custom system prompt
    /// </summary>
    /// <param name="newSystemPrompt">The new system prompt</param>
    public void SetSystemPrompt(string newSystemPrompt)
    {
        systemPrompt = newSystemPrompt;
        InitializeConversation();
    }
    
    /// <summary>
    /// Get the current conversation history
    /// </summary>
    /// <returns>List of chat messages</returns>
    public List<ChatMessage> GetConversationHistory()
    {
        return new List<ChatMessage>(conversationHistory);
    }
    
    /// <summary>
    /// Set the API key (useful for runtime configuration)
    /// </summary>
    /// <param name="key">OpenAI API key</param>
    public void SetAPIKey(string key)
    {
        apiKey = key;
    }
    
    // Public properties for runtime configuration (read-only)
    public string Model => model;
    public int MaxTokens => maxTokens;
    public float Temperature => temperature;
    public string SystemPrompt => systemPrompt;
} 