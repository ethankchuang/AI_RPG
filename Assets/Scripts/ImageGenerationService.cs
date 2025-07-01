using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;

[System.Serializable]
public class ImageGenerationRequest
{
    public string model = "dall-e-2"; // Faster and cheaper than DALL-E 3
    public string prompt;
    public string size = "1024x1024"; // DALL-E 2 only supports 1024x1024
    public int n = 1;
    // Note: quality parameter is not supported by DALL-E 2
}

[System.Serializable]
public class ImageGenerationResponse
{
    public ImageData[] data;
    public long created;
}

[System.Serializable]
public class ImageData
{
    public string url;
    public string revised_prompt;
}

public class ImageGenerationService : MonoBehaviour
{
    public static ImageGenerationService Instance { get; private set; }
    
    [Header("OpenAI Configuration")]
    private string apiKey = ""; // Will use the same API key as AIService
    
    [Header("Image Settings")]
    private string defaultSize = "1024x1024"; // DALL-E 2 only supports 1024x1024
    private string defaultModel = "dall-e-2"; // Faster and cheaper than DALL-E 3
    // Note: quality parameter is not supported by DALL-E 2
    
    [Header("Model Selection")]
    [SerializeField] private ImageModel selectedModel = ImageModel.ReplicateSchnell;
    
    public enum ImageModel
    {
        Dalle2,         // OpenAI - Good balance, $0.02/image
        ReplicateSchnell // Replicate Schnell API - Fast & cost-effective
    }
    
    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Image backgroundImageDisplay;
    [SerializeField] public UnityEngine.UI.RawImage rawImageDisplay;
    
    private const string OPENAI_API_URL = "https://api.openai.com/v1/images/generations";
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Load API key from .env file
            LoadAPIKeyFromEnv();
            
            // Ensure default size is set correctly
            defaultSize = "1024x1024";
            
            Debug.Log($"ImageGenerationService initialized with defaultSize: {defaultSize}, Model: {selectedModel}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Load API key from .env file
    /// </summary>
    private void LoadAPIKeyFromEnv()
    {
        try
        {
            // Try to load from .env file in the project root
            string projectRoot = Application.dataPath.Replace("/Assets", "");
            string envPath = Path.Combine(projectRoot, ".env");
            
            if (File.Exists(envPath))
            {
                string[] lines = File.ReadAllLines(envPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("OPENAI_API_KEY="))
                    {
                        string key = line.Substring("OPENAI_API_KEY=".Length).Trim();
                        if (!string.IsNullOrEmpty(key) && key != "your_openai_api_key_here")
                        {
                            apiKey = key;
                            Debug.Log("ImageGenerationService: API key loaded from .env file");
                            return;
                        }
                    }
                }
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("ImageGenerationService: No API key found in .env file");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ImageGenerationService: Error loading API key from .env file: {e.Message}");
        }
    }
    
    /// <summary>
    /// Generate a background image based on the current story context
    /// </summary>
    /// <param name="storyContext">The current story context to base the image on</param>
    /// <returns>The generated image as a Texture2D, or null if generation failed</returns>
    public async Task<Texture2D> GenerateBackgroundImageAsync(string storyContext)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("ImageGenerationService: OpenAI API key is not set!");
            return null;
        }
        try
        { 
            // Create a prompt for image generation based on the story context
            string imagePrompt = CreateImagePromptFromContext(storyContext);
            
    
            
            // Generate image based on selected model
            Texture2D backgroundTexture = null;
            
            switch (selectedModel)
            {
                case ImageModel.Dalle2:
                    backgroundTexture = await GenerateDalle2ImageAsync(imagePrompt);
                    break;
                case ImageModel.ReplicateSchnell:
                    backgroundTexture = await GenerateSchnellImageAsync(imagePrompt);
                    break;
                default:
                    backgroundTexture = await GenerateDalle2ImageAsync(imagePrompt);
                    break;
            }
            
            if (backgroundTexture != null)
            {
        
                return backgroundTexture;
            }
            else
            {
                Debug.LogWarning("ImageGenerationService: Failed to generate image");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ImageGenerationService: Error generating image: {e.Message}");
            Debug.LogError($"ImageGenerationService: Stack trace: {e.StackTrace}");
            return null;
        }
    }
    
    /// <summary>
    /// Generate image using DALL-E 2
    /// </summary>
    private async Task<Texture2D> GenerateDalle2ImageAsync(string prompt)
    {
        // Create request
        var request = new ImageGenerationRequest
        {
            model = "dall-e-2",
            prompt = prompt,
            size = "1024x1024",
            n = 1
        };
        

        
        // Send request
        string response = await SendDalleRequestAsync(request);
        
        // Parse response and download image
        return await ParseImageResponseAsync(response);
    }
    

    

    
    /// <summary>
    /// Load Replicate API key from .env file
    /// </summary>
    private string LoadReplicateApiKey()
    {
        try
        {
            string projectRoot = Application.dataPath.Replace("/Assets", "");
            string envPath = Path.Combine(projectRoot, ".env");
            if (File.Exists(envPath))
            {
                string[] lines = File.ReadAllLines(envPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("REPLICATE_API_KEY="))
                    {
                        string key = line.Substring("REPLICATE_API_KEY=".Length).Trim();
                        key = key.TrimEnd('%', ' ', '\t', '\r', '\n');
                
                        return key;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading Replicate API key: {e.Message}");
        }
        return null;
    }
    
    /// <summary>
    /// Send image generation request to OpenAI
    /// </summary>
    /// <param name="request">The image generation request</param>
    /// <returns>The API response as a string</returns>
    private async Task<string> SendDalleRequestAsync(ImageGenerationRequest request)
    {
        var webRequest = new UnityWebRequest(OPENAI_API_URL, "POST");
        webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        // Create JSON request - DALL-E 2 doesn't support quality parameter
        string jsonRequest = $@"{{
            ""model"": ""{request.model}"",
            ""prompt"": ""{request.prompt}"",
            ""size"": ""{request.size}"",
            ""n"": {request.n}
        }}";
        

        
        webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonRequest));
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        
        var operation = webRequest.SendWebRequest();
        
        while (!operation.isDone)
        {
            await Task.Yield();
        }
        
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"ImageGenerationService: OpenAI API Error: {webRequest.error}");
            Debug.LogError($"ImageGenerationService: Response: {webRequest.downloadHandler.text}");
            throw new Exception($"API request failed: {webRequest.error}");
        }
        
        return webRequest.downloadHandler.text;
    }
    
    /// <summary>
    /// Parse OpenAI image response and download the image
    /// </summary>
    /// <param name="response">The API response</param>
    /// <returns>The downloaded image as Texture2D</returns>
    private async Task<Texture2D> ParseImageResponseAsync(string response)
    {
        try
        {
            // Parse the JSON response to get the image URL
            var responseData = JsonUtility.FromJson<ImageResponse>(response);
            
            if (responseData?.data != null && responseData.data.Length > 0)
            {
                string imageUrl = responseData.data[0].url;
                
                // Download the image
                return await DownloadImageAsync(imageUrl);
            }
            else
            {
                Debug.LogError("ImageGenerationService: Invalid response format");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ImageGenerationService: Error parsing response: {e.Message}");
            return null;
        }
    }
    

    
    /// <summary>
    /// Generate image using Replicate Schnell API
    /// </summary>
    private async Task<Texture2D> GenerateSchnellImageAsync(string prompt)
    {
        try
        {
            string replicateApiKey = LoadReplicateApiKey();
            if (string.IsNullOrEmpty(replicateApiKey))
            {
                Debug.LogWarning("ImageGenerationService: Replicate API key not found, falling back to DALL-E 2");
                return await GenerateDalle2ImageAsync(prompt);
            }
            var webRequest = new UnityWebRequest("https://api.replicate.com/v1/predictions", "POST");
            webRequest.SetRequestHeader("Authorization", $"Token {replicateApiKey}");
            webRequest.SetRequestHeader("Content-Type", "application/json");
            string jsonRequest = $@"{{
                ""version"": ""black-forest-labs/flux-schnell"",
                ""input"": {{
                    ""prompt"": ""{prompt}"",
                    ""go_fast"": true,
                    ""num_outputs"": 1,
                    ""aspect_ratio"": ""1:1"",
                    ""output_format"": ""png"",
                    ""output_quality"": 80,
                    ""num_inference_steps"": 4
                }}
            }}";

            webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonRequest));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = 60;
            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Replicate API Error: {webRequest.error}");
                Debug.LogError($"Replicate API Response: {webRequest.downloadHandler.text}");
                Debug.LogError($"Replicate API Response Code: {webRequest.responseCode}");
                Debug.LogWarning("ImageGenerationService: Falling back to DALL-E 2 due to Replicate API error");
                return await GenerateDalle2ImageAsync(prompt);
            }
            // Parse Replicate response and download image
            return await ParseReplicateResponseAsync(webRequest.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error with Replicate: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parse Replicate image response and download the image
    /// </summary>
    private async Task<Texture2D> ParseReplicateResponseAsync(string response)
    {
        try
        {
            // The Replicate API returns a prediction object. We need to poll for completion.
            var prediction = JsonUtility.FromJson<ReplicatePrediction>(response);
            string getUrl = prediction.urls.get;
            string status = prediction.status;
            int maxTries = 30;
            int tries = 0;
            while (status != "succeeded" && status != "failed" && tries < maxTries)
            {
                await Task.Delay(2000); // Wait 2 seconds
                using (var getRequest = UnityWebRequest.Get(getUrl))
                {
                    getRequest.SetRequestHeader("Authorization", $"Token {LoadReplicateApiKey()}");
                    var getOp = getRequest.SendWebRequest();
                    while (!getOp.isDone)
                        await Task.Yield();
                    if (getRequest.result == UnityWebRequest.Result.Success)
                    {
                        prediction = JsonUtility.FromJson<ReplicatePrediction>(getRequest.downloadHandler.text);
                        status = prediction.status;
                    }
                    else
                    {
                        Debug.LogError($"Replicate polling error: {getRequest.error}");
                        break;
                    }
                }
                tries++;
            }
            if (status == "succeeded" && prediction.output != null && prediction.output.Length > 0)
            {
                string imageUrl = prediction.output[0];
    
                return await DownloadImageAsync(imageUrl);
            }
            Debug.LogError($"Replicate prediction failed or timed out. Status: {status}");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing Replicate response: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Download image from URL
    /// </summary>
    /// <param name="url">The image URL</param>
    /// <returns>The downloaded image as Texture2D</returns>
    private async Task<Texture2D> DownloadImageAsync(string url)
    {
        try
        {
            var webRequest = UnityWebRequestTexture.GetTexture(url);
            webRequest.timeout = 60; // 60 second timeout
            var operation = webRequest.SendWebRequest();
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"ImageGenerationService: Error downloading image: {webRequest.error}");
                Debug.LogError($"ImageGenerationService: Response code: {webRequest.responseCode}");
                Debug.LogError($"ImageGenerationService: Response headers: {webRequest.GetResponseHeader("content-type")}");
                return null;
            }
            
            Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
            return texture;
        }
        catch (Exception e)
        {
            Debug.LogError($"ImageGenerationService: Error downloading image: {e.Message}");
            Debug.LogError($"ImageGenerationService: Stack trace: {e.StackTrace}");
            return null;
        }
    }
    
    /// <summary>
    /// Create an image prompt based on the current story context
    /// </summary>
    /// <param name="storyContext">The current story context</param>
    /// <returns>A prompt suitable for image generation</returns>
    private string CreateImagePromptFromContext(string storyContext)
    {
        // Start with base fantasy RPG scene
        string basePrompt = "Fantasy RPG scene, ";
        
        // Extract characters mentioned in the story
        string characterPrompt = ExtractCharacterPrompt(storyContext);
        
        // Extract action/event from the story
        string actionPrompt = ExtractActionPrompt(storyContext);
        
        // Extract setting/location from the story
        string settingPrompt = ExtractSettingPrompt(storyContext);
        
        // Extract mood/atmosphere from the story
        string moodPrompt = ExtractMoodPrompt(storyContext);
        
        // Combine all elements
        basePrompt += settingPrompt + characterPrompt + actionPrompt + moodPrompt;
        
        // Add art style and technical requirements
        basePrompt += "fantasy art style, detailed, cinematic composition, suitable for background image, no text, no UI elements";
        
        // Limit prompt length to prevent API issues
        if (basePrompt.Length > 1000)
        {
            basePrompt = basePrompt.Substring(0, 1000);
        }
        
        return basePrompt;
    }
    
    /// <summary>
    /// Extract character information from story context
    /// </summary>
    private string ExtractCharacterPrompt(string storyContext)
    {
        string characterPrompt = "";
        
        // Look for specific character mentions
        if (storyContext.ToLower().Contains("paladin"))
        {
            characterPrompt += "armored paladin in gleaming plate armor, ";
        }
        if (storyContext.ToLower().Contains("rogue"))
        {
            characterPrompt += "stealthy rogue in dark leather armor, ";
        }
        if (storyContext.ToLower().Contains("mage"))
        {
            characterPrompt += "wise mage in flowing robes with magical aura, ";
        }
        if (storyContext.ToLower().Contains("warrior"))
        {
            characterPrompt += "strong warrior with battle-worn armor, ";
        }
        if (storyContext.ToLower().Contains("dark king"))
        {
            characterPrompt += "imposing dark king on throne, ";
        }
        if (storyContext.ToLower().Contains("tavern owner"))
        {
            characterPrompt += "friendly tavern owner, ";
        }
        if (storyContext.ToLower().Contains("bandit"))
        {
            characterPrompt += "dangerous bandits, ";
        }
        
        // Look for player character
        if (storyContext.ToLower().Contains("player") || storyContext.ToLower().Contains("you"))
        {
            characterPrompt += "brave adventurer, ";
        }
        
        return characterPrompt;
    }
    
    /// <summary>
    /// Extract action/event information from story context
    /// </summary>
    private string ExtractActionPrompt(string storyContext)
    {
        string actionPrompt = "";
        
        // Combat actions
        if (storyContext.ToLower().Contains("fight") || storyContext.ToLower().Contains("battle") || storyContext.ToLower().Contains("combat"))
        {
            actionPrompt += "intense battle scene, weapons drawn, ";
        }
        else if (storyContext.ToLower().Contains("bar fight"))
        {
            actionPrompt += "chaotic bar fight, flying tankards, overturned tables, ";
        }
        else if (storyContext.ToLower().Contains("ambush"))
        {
            actionPrompt += "surprise ambush, hidden attackers, ";
        }
        
        // Social actions
        else if (storyContext.ToLower().Contains("talk") || storyContext.ToLower().Contains("conversation") || storyContext.ToLower().Contains("discuss"))
        {
            actionPrompt += "peaceful conversation, characters gathered, ";
        }
        else if (storyContext.ToLower().Contains("gesture") || storyContext.ToLower().Contains("invite"))
        {
            actionPrompt += "welcoming gesture, invitation, ";
        }
        
        // Exploration actions
        else if (storyContext.ToLower().Contains("explore") || storyContext.ToLower().Contains("journey") || storyContext.ToLower().Contains("travel"))
        {
            actionPrompt += "adventure scene, exploration, ";
        }
        else if (storyContext.ToLower().Contains("quest") || storyContext.ToLower().Contains("mission"))
        {
            actionPrompt += "quest preparation, mission briefing, ";
        }
        
        // Environmental actions
        else if (storyContext.ToLower().Contains("sandstorm"))
        {
            actionPrompt += "raging sandstorm, harsh conditions, ";
        }
        else if (storyContext.ToLower().Contains("climb") || storyContext.ToLower().Contains("mountain"))
        {
            actionPrompt += "mountain climbing, treacherous terrain, ";
        }
        
        return actionPrompt;
    }
    
    /// <summary>
    /// Extract setting/location information from story context
    /// </summary>
    private string ExtractSettingPrompt(string storyContext)
    {
        string settingPrompt = "";
        
        // Chapter-specific settings
        if (storyContext.ToLower().Contains("tavern"))
        {
            settingPrompt += "cozy medieval tavern interior, warm firelight, wooden tables and chairs, ale barrels, ";
        }
        else if (storyContext.ToLower().Contains("plains"))
        {
            settingPrompt += "peaceful green plains, rolling hills, clear blue sky, tall grass, ";
        }
        else if (storyContext.ToLower().Contains("forest"))
        {
            settingPrompt += "dense mystical forest, towering trees, dappled sunlight, moss-covered ground, ";
        }
        else if (storyContext.ToLower().Contains("desert"))
        {
            settingPrompt += "harsh desert landscape, sand dunes, scorching sun, sparse vegetation, ";
        }
        else if (storyContext.ToLower().Contains("mountain"))
        {
            settingPrompt += "rugged mountain peaks, rocky terrain, misty atmosphere, steep cliffs, ";
        }
        else if (storyContext.ToLower().Contains("castle"))
        {
            settingPrompt += "dark castle exterior, imposing stone walls, ominous towers, ";
        }
        else if (storyContext.ToLower().Contains("throne room"))
        {
            settingPrompt += "grand throne room, royal architecture, ornate decorations, dramatic lighting, ";
        }
        else if (storyContext.ToLower().Contains("forest"))
        {
            settingPrompt += "dense forest, ancient trees, dappled sunlight, ";
        }
        
        return settingPrompt;
    }
    
    /// <summary>
    /// Extract mood/atmosphere information from story context
    /// </summary>
    private string ExtractMoodPrompt(string storyContext)
    {
        string moodPrompt = "";
        
        // Emotional tones
        if (storyContext.ToLower().Contains("warm") || storyContext.ToLower().Contains("cozy") || storyContext.ToLower().Contains("friendly"))
        {
            moodPrompt += "warm, welcoming atmosphere, ";
        }
        else if (storyContext.ToLower().Contains("dangerous") || storyContext.ToLower().Contains("threatening") || storyContext.ToLower().Contains("ominous"))
        {
            moodPrompt += "dark, threatening atmosphere, ";
        }
        else if (storyContext.ToLower().Contains("peaceful") || storyContext.ToLower().Contains("calm") || storyContext.ToLower().Contains("serene"))
        {
            moodPrompt += "peaceful, serene atmosphere, ";
        }
        else if (storyContext.ToLower().Contains("epic") || storyContext.ToLower().Contains("grand") || storyContext.ToLower().Contains("dramatic"))
        {
            moodPrompt += "epic, dramatic atmosphere, ";
        }
        else if (storyContext.ToLower().Contains("mysterious") || storyContext.ToLower().Contains("enigmatic"))
        {
            moodPrompt += "mysterious, enigmatic atmosphere, ";
        }
        else if (storyContext.ToLower().Contains("chaotic") || storyContext.ToLower().Contains("wild"))
        {
            moodPrompt += "chaotic, wild atmosphere, ";
        }
        
        // Lighting based on mood
        if (storyContext.ToLower().Contains("warm") || storyContext.ToLower().Contains("cozy"))
        {
            moodPrompt += "warm lighting, ";
        }
        else if (storyContext.ToLower().Contains("dark") || storyContext.ToLower().Contains("ominous"))
        {
            moodPrompt += "dark lighting, shadows, ";
        }
        else if (storyContext.ToLower().Contains("bright") || storyContext.ToLower().Contains("sunny"))
        {
            moodPrompt += "bright daylight, ";
        }
        else if (storyContext.ToLower().Contains("mysterious"))
        {
            moodPrompt += "mystical lighting, ";
        }
        
        return moodPrompt;
    }
    
    /// <summary>
    /// Display the generated image in the UI
    /// </summary>
    /// <param name="texture">The texture to display</param>
    public void DisplayImage(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogWarning("ImageGenerationService: Cannot display null texture");
            return;
        }
        
        // Display in RawImage if available
        if (rawImageDisplay != null)
        {
            rawImageDisplay.texture = texture;
            rawImageDisplay.gameObject.SetActive(true);
    
        }
        // Display in Image if available
        else if (backgroundImageDisplay != null)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            backgroundImageDisplay.sprite = sprite;
            backgroundImageDisplay.gameObject.SetActive(true);
    
        }
        else
        {
            Debug.LogWarning("ImageGenerationService: No UI component assigned for image display");
        }
    }
    
    /// <summary>
    /// Set the image generation model
    /// </summary>
    /// <param name="model">The model to use</param>
    public void SetImageModel(ImageModel model)
    {
        selectedModel = model;

    }
    
    /// <summary>
    /// Get cost estimate for current model
    /// </summary>
    /// <returns>Estimated cost per image</returns>
    public string GetCostEstimate()
    {
        switch (selectedModel)
        {
            case ImageModel.Dalle2:
                return "$0.02 per image";
            case ImageModel.ReplicateSchnell:
                return "$0.001-0.003 per image";
            default:
                return "$0.02 per image";
        }
    }
}

// Response classes for different APIs
[System.Serializable]
public class ImageResponse
{
    public ImageData[] data;
}



[System.Serializable]
public class ReplicatePrediction
{
    public string status;
    public string[] output;
    public ReplicateUrls urls;
}

[System.Serializable]
public class ReplicateUrls
{
    public string get;
}