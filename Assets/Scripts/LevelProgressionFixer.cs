using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Fixes level progression issues across all scenes
/// Ensures proper level transitions and completion triggers
/// </summary>
[AddComponentMenu("Roll-a-Ball/Level Progression Fixer")]
public class LevelProgressionFixer : MonoBehaviour
{
    [Header("Progression Settings")]
    [SerializeField] private bool autoFixOnStart = true;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool createMissingComponents = true;
    
    [Header("Level Mapping")]
    [SerializeField] private LevelProgressionData[] levelMappings = {
        new LevelProgressionData { sceneName = "Level1", nextScene = "Level2", expectedCollectibles = 5 },
        new LevelProgressionData { sceneName = "Level2", nextScene = "Level3", expectedCollectibles = 8 },
        new LevelProgressionData { sceneName = "Level3", nextScene = "GeneratedLevel", expectedCollectibles = 10 },
        new LevelProgressionData { sceneName = "GeneratedLevel", nextScene = "GeneratedLevel", expectedCollectibles = 12 },
        new LevelProgressionData { sceneName = "Level_OSM", nextScene = "Level_OSM", expectedCollectibles = 10 },
        new LevelProgressionData { sceneName = "MiniGame", nextScene = "Level1", expectedCollectibles = 5 }
    };
    
    [Header("Manual Controls")]
    [SerializeField] private bool fixNow = false;
    [SerializeField] private bool testProgression = false;
    
    private LevelManager levelManager;
    private GameManager gameManager;
    private string currentScene;

    [System.Serializable]
    public class LevelProgressionData
    {
        public string sceneName;
        public string nextScene;
        public int expectedCollectibles;
        public float timeLimit = 0f; // 0 = no limit
    }

    private void Start()
    {
        if (autoFixOnStart)
        {
            FixCurrentLevelProgression();
        }
    }

    private void OnValidate()
    {
        if (fixNow)
        {
            fixNow = false;
            FixCurrentLevelProgression();
        }
        
        if (testProgression)
        {
            testProgression = false;
            TestLevelProgression();
        }
    }

    /// <summary>
    /// Main fix function for current level
    /// </summary>
    [ContextMenu("Fix Level Progression")]
    public void FixCurrentLevelProgression()
    {
        currentScene = SceneManager.GetActiveScene().name;
        Log($"Fixing level progression for scene: {currentScene}");
        
        // Step 1: Find or create essential components
        SetupEssentialComponents();
        
        // Step 2: Configure level data
        ConfigureLevelData();
        
        // Step 3: Setup progression events
        SetupProgressionEvents();
        
        // Step 4: Validate collectible count
        ValidateCollectibleCount();
        
        // Step 5: Setup goal zone
        SetupGoalZone();
        
        // Step 6: Test progression functionality
        TestProgressionSetup();
        
        Log($"Level progression fix completed for {currentScene}");
    }

    #region Setup Functions

    private void SetupEssentialComponents()
    {
        Log("Setting up essential components...");
        
        // Find or create LevelManager
        levelManager = FindFirstObjectByType<LevelManager>();
        if (!levelManager && createMissingComponents)
        {
            GameObject lmGO = new GameObject("LevelManager");
            levelManager = lmGO.AddComponent<LevelManager>();
            Log("Created LevelManager");
        }
        
        // Find or create GameManager
        gameManager = FindFirstObjectByType<GameManager>();
        if (!gameManager && createMissingComponents)
        {
            GameObject gmGO = new GameObject("GameManager");
            gameManager = gmGO.AddComponent<GameManager>();
            Log("Created GameManager");
        }
        
        // Ensure UI Controller exists
        UIController uiController = FindFirstObjectByType<UIController>();
        if (!uiController && createMissingComponents)
        {
            GameObject uiGO = new GameObject("UIController");
            uiController = uiGO.AddComponent<UIController>();
            Log("Created UIController");
        }
    }

    private void ConfigureLevelData()
    {
        if (!levelManager) return;
        
        Log("Configuring level data...");
        
        // Find matching level data
        LevelProgressionData levelData = GetLevelData(currentScene);
        if (levelData == null)
        {
            LogWarning($"No level progression data found for scene: {currentScene}");
            return;
        }
        
        // Set next scene
        levelManager.SetNextScene(levelData.nextScene);
        Log($"Set next scene: {levelData.nextScene}");
        
        // Configure level settings using reflection if needed
        var levelConfig = levelManager.Config;
        if (levelConfig != null)
        {
            levelConfig.levelName = currentScene;
            levelConfig.nextSceneName = levelData.nextScene;
            
            if (levelData.timeLimit > 0)
            {
                levelConfig.hasTimeLimit = true;
                levelConfig.timeLimit = levelData.timeLimit;
                Log($"Set time limit: {levelData.timeLimit}s");
            }
            else
            {
                levelConfig.hasTimeLimit = false;
            }
        }
    }

    private void SetupProgressionEvents()
    {
        if (!levelManager) return;

        Log("Setting up progression events...");
        levelManager.OnLevelCompleted -= OnLevelCompleted; // EVENT DOUBLE-FIRE FIX
        Debug.Log("Unregistered OnLevelCompleted"); // EVENT DOUBLE-FIRE FIX
        levelManager.OnCollectibleCountChanged -= OnCollectibleCountChanged; // EVENT DOUBLE-FIRE FIX
        Debug.Log("Unregistered OnCollectibleCountChanged"); // EVENT DOUBLE-FIRE FIX
        levelManager.OnLevelCompleted += OnLevelCompleted; // EVENT DOUBLE-FIRE FIX
        Debug.Log("Registered OnLevelCompleted"); // EVENT DOUBLE-FIRE FIX
        levelManager.OnCollectibleCountChanged += OnCollectibleCountChanged; // EVENT DOUBLE-FIRE FIX
        Debug.Log("Registered OnCollectibleCountChanged"); // EVENT DOUBLE-FIRE FIX

        Log("Connected progression events");
    }

    private void ValidateCollectibleCount()
    {
        if (!levelManager) return;
        
        Log("Validating collectible count...");
        
        // Force update collectible count
        levelManager.UpdateCollectibleCount();
        
        int totalCollectibles = levelManager.TotalCollectibles;
        LevelProgressionData levelData = GetLevelData(currentScene);
        
        if (levelData != null && totalCollectibles != levelData.expectedCollectibles)
        {
            LogWarning($"Collectible count mismatch! Found: {totalCollectibles}, Expected: {levelData.expectedCollectibles}");
            
            // Try to fix by regenerating if it's a procedural level
            if (currentScene.ToLower().Contains("generated"))
            {
                AttemptCollectibleFix();
            }
        }
        else
        {
            Log($"Collectible count correct: {totalCollectibles}");
        }
    }

    private void SetupGoalZone()
    {
        Log("Setting up goal zone...");
        
        // Find or create goal zone
        GameObject goalZone = GameObject.FindGameObjectWithTag("Finish");
        if (!goalZone)
        {
            goalZone = GameObject.Find("GoalZone");
        }
        
        if (!goalZone && createMissingComponents)
        {
            goalZone = CreateGoalZone();
        }
        
        if (goalZone)
        {
            // Ensure goal zone has proper trigger
            Collider goalCollider = goalZone.GetComponent<Collider>();
            if (!goalCollider)
            {
                BoxCollider box = goalZone.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(3, 2, 3);
                Log("Added BoxCollider to goal zone");
            }
            else if (!goalCollider.isTrigger)
            {
                goalCollider.isTrigger = true;
                Log("Set goal zone collider as trigger");
            }
            
            // Ensure goal zone has proper tag
            if (!goalZone.CompareTag("Finish"))
            {
                try
                {
                    goalZone.tag = "Finish";
                    Log("Set Finish tag on goal zone");
                }
                catch
                {
                    LogWarning("Finish tag not found in project settings");
                }
            }
            
            // Add goal zone controller if missing
            GoalZoneController goalController = goalZone.GetComponent<GoalZoneController>();
            if (!goalController)
            {
                goalController = goalZone.AddComponent<GoalZoneController>();
                Log("Added GoalZoneController");
            }
        }
        else
        {
            LogWarning("No goal zone found and creation is disabled");
        }
    }

    #endregion

    #region Helper Functions

    private LevelProgressionData GetLevelData(string sceneName)
    {
        foreach (var data in levelMappings)
        {
            if (data.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return data;
            }
        }
        return null;
    }

    private GameObject CreateGoalZone()
    {
        Log("Creating goal zone...");
        
        GameObject goalZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goalZone.name = "GoalZone";
        
        // Position it appropriately
        Vector3 spawnPos = Vector3.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            // Place goal zone away from player
            spawnPos = player.transform.position + Vector3.forward * 20f;
        }
        
        goalZone.transform.position = spawnPos;
        goalZone.transform.localScale = new Vector3(3, 0.5f, 3);
        
        // Make it visually distinct
        Renderer renderer = goalZone.GetComponent<Renderer>();
        if (renderer)
        {
            renderer.material.color = Color.green;
            renderer.material.SetFloat("_Metallic", 0.5f);
            renderer.material.SetFloat("_Smoothness", 0.8f);
        }
        
        // Remove default collider and add trigger
        Destroy(goalZone.GetComponent<Collider>());
        BoxCollider trigger = goalZone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.2f, 2f, 1.2f);
        
        try
        {
            goalZone.tag = "Finish";
        }
        catch
        {
            LogWarning("Finish tag not available");
        }
        
        Log($"Created goal zone at position: {spawnPos}");
        return goalZone;
    }

    private void AttemptCollectibleFix()
    {
        Log("Attempting to fix collectible count...");
        
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator)
        {
            // Try to regenerate level with correct collectible count
            var profiles = Resources.LoadAll<LevelProfile>("");
            if (profiles.Length > 0)
            {
                LevelProfile profile = profiles[0];
                LevelProgressionData levelData = GetLevelData(currentScene);
                
                if (levelData != null)
                {
                    // This would require extending LevelProfile to allow runtime modification
                    Log($"Attempting to regenerate level with {levelData.expectedCollectibles} collectibles");
                    generator.GenerateLevel(profile);
                }
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnLevelCompleted(LevelConfiguration config)
    {
        Log($"Level completed: {config.levelName}");
        
        // Additional completion logic if needed
        StartCoroutine(HandleLevelCompletion(config));
    }

    private void OnCollectibleCountChanged(int remaining, int total)
    {
        Log($"Collectibles: {remaining}/{total} remaining");
        
        // Update UI if available
        UIController uiController = FindFirstObjectByType<UIController>();
        if (uiController)
        {
            int collected = total - remaining;
            uiController.UpdateCollectibleDisplay(collected);
        }
    }

    private IEnumerator HandleLevelCompletion(LevelConfiguration config)
    {
        // Wait a moment before transition
        yield return new WaitForSeconds(1f);
        
        // Ensure the level actually transitions
        if (!string.IsNullOrEmpty(config.nextSceneName))
        {
            Log($"Transitioning to: {config.nextSceneName}");
            SceneManager.LoadScene(config.nextSceneName);
        }
        else
        {
            LogWarning("No next scene configured!");
        }
    }

    #endregion

    #region Testing

    private void TestProgressionSetup()
    {
        Log("Testing progression setup...");
        
        bool hasLevelManager = levelManager != null;
        bool hasCollectibles = levelManager ? levelManager.TotalCollectibles > 0 : false;
        bool hasNextScene = levelManager ? !string.IsNullOrEmpty(levelManager.Config?.nextSceneName) : false;
        bool hasGoalZone = GameObject.FindGameObjectWithTag("Finish") != null;
        
        Log($"Test Results:");
        Log($"  LevelManager: {(hasLevelManager ? "✓" : "✗")}");
        Log($"  Collectibles: {(hasCollectibles ? "✓" : "✗")} ({(levelManager ? levelManager.TotalCollectibles : 0)})");
        Log($"  Next Scene: {(hasNextScene ? "✓" : "✗")} ({(levelManager?.Config?.nextSceneName ?? "none")})");
        Log($"  Goal Zone: {(hasGoalZone ? "✓" : "✗")}");
        
        bool allGood = hasLevelManager && hasCollectibles && hasNextScene && hasGoalZone;
        Log($"Overall Status: {(allGood ? "✓ GOOD" : "✗ NEEDS ATTENTION")}");
    }

    [ContextMenu("Test Level Progression")]
    public void TestLevelProgression()
    {
        Log("=== TESTING LEVEL PROGRESSION ===");
        
        if (!levelManager)
        {
            LogWarning("No LevelManager to test!");
            return;
        }
        
        // Simulate collecting all items
        Log("Simulating collection of all items...");
        
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        int collectedCount = 0;
        
        foreach (var collectible in collectibles)
        {
            if (!collectible.IsCollected)
            {
                collectible.ForceCollect();
                collectedCount++;
            }
        }
        
        Log($"Force collected {collectedCount} items");
        
        // Check if level completes
        if (levelManager.IsLevelCompleted)
        {
            Log("✓ Level progression working correctly!");
        }
        else
        {
            LogWarning("✗ Level did not complete after collecting all items");
        }
    }

    #endregion

    #region Goal Zone Controller

    /// <summary>
    /// Simple goal zone controller that triggers level completion
    /// </summary>
    public class GoalZoneController : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                LevelManager levelManager = FindFirstObjectByType<LevelManager>();
                if (levelManager && levelManager.CollectiblesRemaining <= 0)
                {
                    levelManager.ForceCompleteLevel();
                    Debug.Log("[GoalZone] Player reached goal zone - level completed!");
                }
                else if (levelManager)
                {
                    UIController uiController = FindFirstObjectByType<UIController>();
                    if (uiController)
                    {
                        uiController.ShowNotification($"Sammle noch {levelManager.CollectiblesRemaining} Objekte!", 2f);
                    }
                }
            }
        }
    }

    #endregion

    #region Utility

    private void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[LevelProgressionFixer] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[LevelProgressionFixer] {message}");
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (levelManager)
        {
            levelManager.OnLevelCompleted -= OnLevelCompleted; // EVENT DOUBLE-FIRE FIX
            Debug.Log("Unregistered OnLevelCompleted"); // EVENT DOUBLE-FIRE FIX
            levelManager.OnCollectibleCountChanged -= OnCollectibleCountChanged; // EVENT DOUBLE-FIRE FIX
            Debug.Log("Unregistered OnCollectibleCountChanged"); // EVENT DOUBLE-FIRE FIX
        }
    }

    /// <summary>
    /// Add this component to any scene to fix progression
    /// </summary>
    [ContextMenu("Add to Current Scene")]
    public static void AddToCurrentScene()
    {
        if (FindFirstObjectByType<LevelProgressionFixer>() == null)
        {
            GameObject fixerGO = new GameObject("LevelProgressionFixer");
            fixerGO.AddComponent<LevelProgressionFixer>();
            Debug.Log("Added LevelProgressionFixer to current scene");
        }
        else
        {
            Debug.Log("LevelProgressionFixer already exists in scene");
        }
    }

    #endregion
}
