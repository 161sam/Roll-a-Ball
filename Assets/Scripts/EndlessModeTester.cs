using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Test and debug utilities for the endless OSM mode implementation
/// </summary>
[AddComponentMenu("Roll-a-Ball/Debug/Endless Mode Tester")]
public class EndlessModeTester : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private KeyCode triggerEndlessModeKey = KeyCode.F1;
    [SerializeField] private KeyCode resetEndlessModeKey = KeyCode.F2;
    [SerializeField] private KeyCode simulateLevel3CompleteKey = KeyCode.F3;
    [SerializeField] private KeyCode loadOSMSceneKey = KeyCode.F4;
    [SerializeField] private bool enableDebugKeys = true;
    
    [Header("Testing")]
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private bool logDetailedInfo = true;
    
    private void Update()
    {
        if (!enableDebugKeys) return;
        
        if (Input.GetKeyDown(triggerEndlessModeKey))
        {
            TriggerEndlessMode();
        }
        
        if (Input.GetKeyDown(resetEndlessModeKey))
        {
            ResetEndlessMode();
        }
        
        if (Input.GetKeyDown(simulateLevel3CompleteKey))
        {
            SimulateLevel3Complete();
        }
        
        if (Input.GetKeyDown(loadOSMSceneKey))
        {
            LoadOSMScene();
        }
    }
    
    [ContextMenu("Trigger Endless Mode")]
    public void TriggerEndlessMode()
    {
        PlayerPrefs.SetInt("AutoGenerateOSMMode", 1);
        PlayerPrefs.SetInt("OSMLocationIndex", 0);
        PlayerPrefs.Save();
        
        // TODO: Replace Debug.Log with structured logging
        Debug.Log("[EndlessModeTester] Endless mode activated!");
        
        if (logDetailedInfo)
        {
            LogCurrentStatus();
        }
    }
    
    [ContextMenu("Reset Endless Mode")]
    public void ResetEndlessMode()
    {
        PlayerPrefs.DeleteKey("AutoGenerateOSMMode");
        PlayerPrefs.DeleteKey("OSMLocationIndex");
        PlayerPrefs.Save();
        
        // TODO: Replace Debug.Log with structured logging
        Debug.Log("[EndlessModeTester] Endless mode reset!");
        
        if (logDetailedInfo)
        {
            LogCurrentStatus();
        }
    }
    
    [ContextMenu("Simulate Level 3 Complete")]
    public void SimulateLevel3Complete()
    {
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            Debug.Log("[EndlessModeTester] Simulating Level 3 completion...");
            levelManager.ForceCompleteLevel();
        }
        else
        {
            Debug.LogWarning("[EndlessModeTester] No LevelManager found to complete level");
            
            // Alternative: directly set up endless mode and load OSM scene
            TriggerEndlessMode();
            LoadOSMScene();
        }
    }
    
    [ContextMenu("Load OSM Scene")]
    public void LoadOSMScene()
    {
        Debug.Log("[EndlessModeTester] Loading Level_OSM scene...");
        SceneManager.LoadScene("Level_OSM");
    }
    
    [ContextMenu("Log Current Status")]
    public void LogCurrentStatus()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool isEndlessMode = PlayerPrefs.GetInt("AutoGenerateOSMMode", 0) == 1;
        int locationIndex = PlayerPrefs.GetInt("OSMLocationIndex", 0);
        
        Debug.Log($"[EndlessModeTester] === CURRENT STATUS ===");
        Debug.Log($"Scene: {currentScene}");
        Debug.Log($"Endless Mode Active: {isEndlessMode}");
        Debug.Log($"Location Index: {locationIndex}");
        
        // Check for required components
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        GameManager gameManager = GameManager.Instance;
        var mapStartup = FindFirstObjectByType<RollABall.Map.MapStartupController>();
        var mapGenerator = FindFirstObjectByType<RollABall.Map.MapGenerator>();
        
        Debug.Log($"LevelManager: {(levelManager != null ? "✓" : "✗")}");
        Debug.Log($"GameManager: {(gameManager != null ? "✓" : "✗")}");
        Debug.Log($"MapStartupController: {(mapStartup != null ? "✓" : "✗")}");
        Debug.Log($"MapGenerator: {(mapGenerator != null ? "✓" : "✗")}");
        
        if (levelManager != null)
        {
            Debug.Log($"Collectibles: {levelManager.CollectiblesRemaining}/{levelManager.TotalCollectibles}");
            Debug.Log($"Level Completed: {levelManager.IsLevelCompleted}");
        }
    }
    
    [ContextMenu("Test Level Sequence")]
    public void TestLevelSequence()
    {
        Debug.Log("[EndlessModeTester] Testing level sequence...");
        
        string[] testScenes = { "Level1", "Level2", "Level3", "Level_OSM" };
        
        foreach (string sceneName in testScenes)
        {
            // Simulate DetermineNextScene logic
            string nextScene = GetNextScene(sceneName);
            Debug.Log($"{sceneName} -> {nextScene}");
        }
    }
    
    private string GetNextScene(string currentScene)
    {
        // Mirror the LevelManager logic for testing
        if (currentScene == "Level1")
            return "Level2";
        else if (currentScene == "Level2")
            return "Level3";
        else if (currentScene == "Level3")
        {
            PlayerPrefs.SetInt("AutoGenerateOSMMode", 1);
            PlayerPrefs.SetInt("OSMLocationIndex", 0);
            return "Level_OSM";
        }
        else if (currentScene == "Level_OSM")
        {
            int currentIndex = PlayerPrefs.GetInt("OSMLocationIndex", 0);
            PlayerPrefs.SetInt("OSMLocationIndex", currentIndex + 1);
            return "Level_OSM";
        }
        
        return "";
    }
    
    [ContextMenu("Increment Location Index")]
    public void IncrementLocationIndex()
    {
        int currentIndex = PlayerPrefs.GetInt("OSMLocationIndex", 0);
        PlayerPrefs.SetInt("OSMLocationIndex", currentIndex + 1);
        PlayerPrefs.Save();
        
        Debug.Log($"[EndlessModeTester] Location index incremented to {currentIndex + 1}");
    }
    
    void OnGUI()
    {
        if (!showDebugGUI) return;
        
        GUI.Box(new Rect(10, 10, 300, 200), "Endless Mode Tester");
        
        if (GUI.Button(new Rect(20, 40, 120, 25), "Trigger Endless"))
        {
            TriggerEndlessMode();
        }
        
        if (GUI.Button(new Rect(150, 40, 120, 25), "Reset Endless"))
        {
            ResetEndlessMode();
        }
        
        if (GUI.Button(new Rect(20, 75, 120, 25), "Complete Level 3"))
        {
            SimulateLevel3Complete();
        }
        
        if (GUI.Button(new Rect(150, 75, 120, 25), "Load OSM Scene"))
        {
            LoadOSMScene();
        }
        
        if (GUI.Button(new Rect(20, 110, 250, 25), "Log Status"))
        {
            LogCurrentStatus();
        }
        
        // Status display
        bool isEndless = PlayerPrefs.GetInt("AutoGenerateOSMMode", 0) == 1;
        int index = PlayerPrefs.GetInt("OSMLocationIndex", 0);
        
        GUI.Label(new Rect(20, 145, 250, 20), $"Endless Mode: {(isEndless ? "Active" : "Inactive")}");
        GUI.Label(new Rect(20, 165, 250, 20), $"Location Index: {index}");
        GUI.Label(new Rect(20, 185, 250, 20), $"Scene: {SceneManager.GetActiveScene().name}");
    }
    
    /// <summary>
    /// Validate that all endless mode components are working
    /// </summary>
    [ContextMenu("Validate Implementation")]
    public void ValidateImplementation()
    {
        Debug.Log("[EndlessModeTester] === VALIDATION REPORT ===");
        
        bool allValid = true;
        
        // Check LevelManager DetermineNextScene logic
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            Debug.Log("✓ LevelManager found");
            
            // Check if it has the updated DetermineNextScene method
            var method = levelManager.GetType().GetMethod("DetermineNextScene", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                Debug.Log("✓ DetermineNextScene method exists");
            }
            else
            {
                Debug.LogError("✗ DetermineNextScene method not found");
                allValid = false;
            }
        }
        else
        {
            Debug.LogError("✗ LevelManager not found");
            allValid = false;
        }
        
        // Check MapStartupController
        var mapStartup = FindFirstObjectByType<RollABall.Map.MapStartupController>();
        if (mapStartup != null)
        {
            Debug.Log("✓ MapStartupController found");
        }
        else
        {
            Debug.LogError("✗ MapStartupController not found");
            allValid = false;
        }
        
        // Check MapGenerator
        var mapGenerator = FindFirstObjectByType<RollABall.Map.MapGenerator>();
        if (mapGenerator != null)
        {
            Debug.Log("✓ MapGenerator found");
        }
        else
        {
            Debug.LogError("✗ MapGenerator not found");
            allValid = false;
        }
        
        Debug.Log($"[EndlessModeTester] Validation {(allValid ? "PASSED" : "FAILED")}");
    }
}
