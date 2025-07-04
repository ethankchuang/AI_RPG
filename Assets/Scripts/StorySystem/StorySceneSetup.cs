using UnityEngine;
using UnityEngine.SceneManagement;

public class StorySceneSetup : MonoBehaviour
{
    [Header("Story System References")]
    [SerializeField] private StoryManager storyManagerPrefab;
    [SerializeField] private LinearStoryUI linearStoryUIPrefab;
    [SerializeField] private CampfireManager campfireManagerPrefab;
    
    [Header("Scene Configuration")]
    [SerializeField] private string battleSceneName = "Battle";
    [SerializeField] private string storySceneName = "Chat";
    
    private void Awake()
    {
        // Wait a frame to ensure any existing StoryManager has finished initializing
        StartCoroutine(SetupStorySystem());
    }
    
    private System.Collections.IEnumerator SetupStorySystem()
    {
        // Wait for the end of frame to ensure all Awake() methods have been called
        yield return new WaitForEndOfFrame();
        
        // Ensure StoryManager exists
        if (StoryManager.Instance == null)
        {
            if (storyManagerPrefab != null)
            {
                Instantiate(storyManagerPrefab);
            }
            else
            {
                // Create a default StoryManager if no prefab is assigned
                GameObject storyManagerObj = new GameObject("StoryManager");
                storyManagerObj.AddComponent<StoryManager>();
            }
        }
        
        // Ensure LinearStoryUI exists
        if (FindObjectOfType<LinearStoryUI>() == null)
        {
            if (linearStoryUIPrefab != null)
            {
                Instantiate(linearStoryUIPrefab);
            }
            else
            {
                // Create a default LinearStoryUI if no prefab is assigned
                GameObject storyUIObj = new GameObject("LinearStoryUI");
                storyUIObj.AddComponent<LinearStoryUI>();
            }
        }
        
        // Ensure CampfireManager exists
        if (FindObjectOfType<CampfireManager>() == null)
        {
            if (campfireManagerPrefab != null)
            {
                Instantiate(campfireManagerPrefab);
            }
            else
            {
                // Create a default CampfireManager if no prefab is assigned
                GameObject campfireManagerObj = new GameObject("CampfireManager");
                campfireManagerObj.AddComponent<CampfireManager>();
            }
        }
    }
    
    private void Start()
    {
        // No longer automatically transitioning to combat
        // Combat transitions are now handled by LinearStoryUI when player presses Combat button
    }
    
    public void LoadStoryScene()
    {
        SceneManager.LoadScene(storySceneName);
    }
    
    public void LoadBattleScene()
    {
        SceneManager.LoadScene(battleSceneName);
    }
    
    private void OnDestroy()
    {
        // No events to unsubscribe from
    }
} 