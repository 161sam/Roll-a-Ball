using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages level progression, collectible tracking, and scene transitions
/// Works in conjunction with GameManager for overall game state
/// FIXED: Proper event registration and collectible counting
/// </summary>
[System.Serializable]
public class LevelConfiguration
{
    [Header("Level Info")]
    public string levelName = "Level";
    public int levelIndex = 1;
    public string nextSceneName = "";

    [Header("Collectibles")]
    public int totalCollectibles = 0; // Will be set automatically
    public int collectiblesRemaining = 0; // Will be calculated

    [Header("Difficulty")]
    public float difficultyMultiplier = 1f;
    public bool hasTimeLimit = false;
    public float timeLimit = 120f;

    [Header("Visual Theme")]
    public string themeName = "Steampunk";
    public Color themeColor = Color.cyan;
}

[AddComponentMenu("Game/LevelManager")]
public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private LevelConfiguration levelConfig;

    [Header("Scene Transition")]
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private bool showLevelComplete = true;
    [SerializeField] private float levelCompleteDisplayTime = 2f;

    [Header("Goal Zone")]
    [SerializeField] private GameObject goalZone;
    [SerializeField] private bool autoFindGoalZone = true;

    [Header("Collectibles")]
    [SerializeField] private List<CollectibleController> levelCollectibles = new List<CollectibleController>();
    [SerializeField] private bool autoFindCollectibles = true;
    [SerializeField] private HashSet<CollectibleController> collectedCollectibles = new HashSet<CollectibleController>();

    [Header("Level Progression")]
    [SerializeField] private LevelProgressionProfile progressionProfile;
    
    [Header("UI References")]
    [SerializeField] private UIController uiController;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Private fields
    private bool levelCompleted = false;
    private float levelStartTime;
    private bool eventsRegistered = false; // Track registration state
    private readonly object lockObject = new object(); // Thread safety

    // Events
    public System.Action<int, int> OnCollectibleCountChanged; // remaining, total
    public System.Action<LevelConfiguration> OnLevelCompleted;
    public System.Action<LevelConfiguration> OnLevelStarted;
    public System.Action<float> OnTimeLimitUpdate; // remaining time

    // Singleton pattern
    public static LevelManager Instance { get; private set; }

    // Properties
    public LevelConfiguration Config => levelConfig;
    public LevelProgressionProfile ProgressionProfile => progressionProfile;
    public bool IsLevelCompleted => levelCompleted;
    public int CollectiblesRemaining => levelConfig.collectiblesRemaining;
    public int TotalCollectibles => levelConfig.totalCollectibles;
    public float TimeRemaining => levelConfig.hasTimeLimit ? 
        Mathf.Max(0, levelConfig.timeLimit - (Time.time - levelStartTime)) : float.MaxValue;
    public float LevelElapsedTime => Time.time - levelStartTime;

    void Awake()
    {
        // Singleton setup (local to scene)
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple LevelManagers found in scene. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        InitializeLevelManager();
    }

    void Start()
    {
        StartLevel();
    }

    void Update()
    {
        if (levelConfig.hasTimeLimit && !levelCompleted)
        {
            CheckTimeLimit();
        }
    }

    void OnDisable()
    {
        SafeUnsubscribeFromAllEvents();
    }

    void OnDestroy()
    {
        SafeUnsubscribeFromAllEvents();

        if (Instance == this)
            Instance = null;
    }

    private void InitializeLevelManager()
    {
        // Initialize level config if null
        if (levelConfig == null)
        {
            levelConfig = new LevelConfiguration();
            levelConfig.levelName = SceneManager.GetActiveScene().name;
        }

        // Clear collections
        if (levelCollectibles == null)
            levelCollectibles = new List<CollectibleController>();
        else
            levelCollectibles.Clear();

        if (collectedCollectibles == null)
            collectedCollectibles = new HashSet<CollectibleController>();
        else
            collectedCollectibles.Clear();

        // Find UI controller
        if (!uiController)
        {
            uiController = FindFirstObjectByType<UIController>();
            if (!uiController)
            {
                GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/UI/GameUI");
                if (uiPrefab)
                {
                    var uiInstance = Instantiate(uiPrefab);
                    uiController = uiInstance.GetComponent<UIController>();
                }
            }
            if (uiController && debugMode)
                Debug.Log("[LevelManager] Auto-found UIController");
        }

        levelStartTime = Time.time;
        eventsRegistered = false;
    }

    private void StartLevel()
    {
        // STEP 1: Find all collectibles first
        if (autoFindCollectibles)
        {
            FindAllCollectibles();
        }

        // STEP 2: Initialize counts BEFORE subscribing to events
        InitializeCollectibleCounts();

        // STEP 3: Find goal zone
        if (autoFindGoalZone && !goalZone)
        {
            FindGoalZone();
        }

        // STEP 4: Setup goal zone (inactive until all collected)
        SetGoalZoneActive(false);

        // STEP 5: Subscribe to events AFTER everything is initialized
        SafeSubscribeToAllEvents();

        // STEP 6: Update UI
        UpdateUI();

        // STEP 7: Fire level started event
        OnLevelStarted?.Invoke(levelConfig);

        if (debugMode)
        {
            Debug.Log($"[LevelManager] Level started: {levelConfig.levelName}");
            Debug.Log($"[LevelManager] Total collectibles found: {levelConfig.totalCollectibles}");
            Debug.Log($"[LevelManager] Collectibles remaining: {levelConfig.collectiblesRemaining}");
            if (levelConfig.hasTimeLimit)
                Debug.Log($"[LevelManager] Time limit: {levelConfig.timeLimit}s");
        }
    }

    private void FindAllCollectibles()
    {
        // Clear existing list
        levelCollectibles.Clear();
        collectedCollectibles.Clear();

        // Find all collectibles in scene
        var foundCollectibles = FindObjectsByType<CollectibleController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        foreach (var collectible in foundCollectibles)
        {
            if (collectible != null && !collectible.IsCollected)
            {
                levelCollectibles.Add(collectible);
            }
        }

        if (debugMode)
            Debug.Log($"[LevelManager] Found {levelCollectibles.Count} active collectibles in scene");
    }

    private void InitializeCollectibleCounts()
    {
        // Set total based on found collectibles
        levelConfig.totalCollectibles = levelCollectibles.Count;
        levelConfig.collectiblesRemaining = levelConfig.totalCollectibles;

        if (debugMode)
            Debug.Log($"[LevelManager] Initialized counts - Total: {levelConfig.totalCollectibles}, Remaining: {levelConfig.collectiblesRemaining}");
    }

    private void FindGoalZone()
    {
        // Look for object with specific tag or name
        GameObject[] candidates = GameObject.FindGameObjectsWithTag("Finish");
        if (candidates.Length > 0)
        {
            goalZone = candidates[0];
        }
        else
        {
            goalZone = GameObject.Find("GoalZone");
        }

        if (goalZone && debugMode)
        {
            Debug.Log($"[LevelManager] Found goal zone: {goalZone.name}");
        }
    }

    /// <summary>
    /// Safely subscribe to all collectible events, ensuring no double registration
    /// </summary>
    private void SafeSubscribeToAllEvents()
    {
        lock (lockObject)
        {
            if (eventsRegistered)
            {
                if (debugMode)
                    Debug.LogWarning("[LevelManager] Events already registered, skipping...");
                return;
            }

            if (levelCollectibles == null || levelCollectibles.Count == 0)
            {
                if (debugMode)
                    Debug.LogWarning("[LevelManager] No collectibles to register events for");
                return;
            }

            int registeredCount = 0;
            foreach (var collectible in levelCollectibles)
            {
                if (collectible != null)
                {
                    // Ensure clean state before registering
                    collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
                    collectible.OnCollectiblePickedUp += OnCollectibleCollected;
                    registeredCount++;
                }
            }

            eventsRegistered = true;

            if (debugMode)
                Debug.Log($"[LevelManager] Registered events for {registeredCount} collectibles");
        }
    }

    /// <summary>
    /// Safely unsubscribe from all collectible events
    /// </summary>
    private void SafeUnsubscribeFromAllEvents()
    {
        lock (lockObject)
        {
            if (!eventsRegistered)
                return;

            if (levelCollectibles != null)
            {
                int unregisteredCount = 0;
                foreach (var collectible in levelCollectibles)
                {
                    if (collectible != null)
                    {
                        collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
                        unregisteredCount++;
                    }
                }

                if (debugMode)
                    Debug.Log($"[LevelManager] Unregistered events for {unregisteredCount} collectibles");
            }

            eventsRegistered = false;
        }
    }

    /// <summary>
    /// Enables or disables the goal zone trigger.
    /// </summary>
    private void SetGoalZoneActive(bool active)
    {
        if (!goalZone) return;

        goalZone.SetActive(active);

        Collider collider = goalZone.GetComponent<Collider>();
        if (collider)
            collider.enabled = active;
    }

    /// <summary>
    /// Handle collectible collection - THREAD SAFE with proper counting
    /// </summary>
    public void OnCollectibleCollected(CollectibleController collectible)
    {
        if (levelCompleted || collectible == null)
            return;

        lock (lockObject)
        {
            // Check if already collected (prevent double counting)
            if (!collectedCollectibles.Add(collectible))
            {
                if (debugMode)
                    Debug.LogWarning($"[LevelManager] Collectible {collectible.name} already collected, ignoring...");
                return;
            }

            // Update counts
            int collectedCount = collectedCollectibles.Count;
            levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.totalCollectibles - collectedCount);

            if (debugMode)
                Debug.Log($"[LevelManager] Collectible {collectible.name} collected! Progress: {collectedCount}/{levelConfig.totalCollectibles} (Remaining: {levelConfig.collectiblesRemaining})");
        }

        // Update UI (outside lock to prevent deadlocks)
        UpdateUI();

        // Play collection feedback
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySound("Collect");
        }

        // Check for level completion
        if (levelConfig.collectiblesRemaining <= 0 && !levelCompleted)
        {
            CompleteLevel();
        }
    }

    /// <summary>
    /// Add a collectible at runtime and register its events
    /// </summary>
    public void AddCollectible(CollectibleController collectible)
    {
        if (collectible == null || levelCollectibles.Contains(collectible))
            return;

        lock (lockObject)
        {
            levelCollectibles.Add(collectible);
            collectedCollectibles.Remove(collectible); // Reset collected state for pooled objects
            
            // Update total count
            levelConfig.totalCollectibles = levelCollectibles.Count;
            levelConfig.collectiblesRemaining = levelConfig.totalCollectibles - collectedCollectibles.Count;

            // Register event for new collectible
            if (eventsRegistered)
            {
                collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
                collectible.OnCollectiblePickedUp += OnCollectibleCollected;
            }
        }

        UpdateUI();

        if (debugMode)
            Debug.Log($"[LevelManager] Added collectible {collectible.name}. New total: {levelConfig.totalCollectibles}");
    }

    /// <summary>
    /// Public method to update collectible count - for compatibility with tool scripts
    /// </summary>
    public void UpdateCollectibleCount()
    {
        lock (lockObject)
        {
            if (levelCollectibles == null) return;

            if (levelConfig.totalCollectibles <= 0)
            {
                levelConfig.totalCollectibles = levelCollectibles.Count;
            }

            levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.totalCollectibles - collectedCollectibles.Count);
        }

        UpdateUI();

        if (debugMode)
            Debug.Log($"[LevelManager] Updated collectible count: {levelConfig.collectiblesRemaining}/{levelConfig.totalCollectibles}");
    }

    /// <summary>
    /// Register events for a specific collectible - for compatibility with tool scripts
    /// </summary>
    public void RegisterCollectibleEvents(CollectibleController collectible)
    {
        if (collectible == null) return;

        lock (lockObject)
        {
            // Ensure clean state before registering
            collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
            collectible.OnCollectiblePickedUp += OnCollectibleCollected;
        }

        if (debugMode)
            Debug.Log($"[LevelManager] Registered events for collectible: {collectible.name}");
    }

    /// <summary>
    /// Unregister events for a specific collectible - for compatibility with tool scripts
    /// </summary>
    public void UnregisterCollectibleEvents(CollectibleController collectible)
    {
        if (collectible == null) return;

        lock (lockObject)
        {
            collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
        }

        if (debugMode)
            Debug.Log($"[LevelManager] Unregistered events for collectible: {collectible.name}");
    }

    /// <summary>
    /// Remove a collectible and unregister its events
    /// </summary>
    public void RemoveCollectible(CollectibleController collectible)
    {
        if (collectible == null || !levelCollectibles.Contains(collectible))
            return;

        lock (lockObject)
        {
            levelCollectibles.Remove(collectible);
            collectedCollectibles.Remove(collectible);

            // Update counts
            levelConfig.totalCollectibles = levelCollectibles.Count;
            levelConfig.collectiblesRemaining = levelConfig.totalCollectibles - collectedCollectibles.Count;

            // Unregister event
            collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
        }

        UpdateUI();

        if (debugMode)
            Debug.Log($"[LevelManager] Removed collectible {collectible.name}. New total: {levelConfig.totalCollectibles}");
    }

    /// <summary>
    /// Rebuilds collectible tracking and synchronizes counts.
    /// </summary>
    public void RescanCollectibles()
    {
        SafeUnsubscribeFromAllEvents();
        FindAllCollectibles();
        InitializeCollectibleCounts();
        SafeSubscribeToAllEvents();
        UpdateUI();

        if (debugMode)
            Debug.Log($"[LevelManager] Rescanned collectibles: {levelConfig.collectiblesRemaining}/{levelConfig.totalCollectibles}");
    }

    private void CheckTimeLimit()
    {
        float timeRemaining = TimeRemaining;
        
        // Fire time update event
        OnTimeLimitUpdate?.Invoke(timeRemaining);

        // Check if time is up
        if (timeRemaining <= 0f && !levelCompleted)
        {
            TimeUp();
        }
    }

    private void TimeUp()
    {
        if (debugMode)
            Debug.Log("Time limit reached!");

        // Enhanced GameManager integration
        if (GameManager.Instance)
        {
            // Check if RegisterPlayerDeath method exists
            try
            {
                var methodInfo = GameManager.Instance.GetType().GetMethod("RegisterPlayerDeath");
                if (methodInfo != null)
                {
                    methodInfo.Invoke(GameManager.Instance, null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"RegisterPlayerDeath method not available: {e.Message}");
            }

            GameManager.Instance.GameOver();
        }
    }

    private void CompleteLevel()
    {
        if (levelCompleted) return;

        levelCompleted = true;

        if (debugMode)
            Debug.Log("[LevelManager] Level Complete! All collectibles collected → Loading next level");

        // Activate goal zone for completion effects
        SetGoalZoneActive(true);

        // Fire level completed event
        OnLevelCompleted?.Invoke(levelConfig);

        // Enhanced GameManager statistics integration
        if (GameManager.Instance)
        {
            try
            {
                var methodInfo = GameManager.Instance.GetType().GetMethod("RecordLevelPerformance");
                if (methodInfo != null)
                {
                    int collected = collectedCollectibles.Count;
                    float target = levelConfig.hasTimeLimit ? levelConfig.timeLimit : 0f;
                    methodInfo.Invoke(GameManager.Instance, new object[] { LevelElapsedTime, collected, levelConfig.totalCollectibles, target });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"RecordLevelPerformance method not available: {e.Message}");
            }
        }
        StartCoroutine(LoadNextLevelRoutine());
    }

    /// <summary>
    /// Wartet die konfigurierte Anzeigedauer ab und lädt anschließend den nächsten Level.
    /// Verwendet echte Zeit, damit auch bei pausiertem Spiel fortgefahren wird.
    /// </summary>
    private System.Collections.IEnumerator LoadNextLevelRoutine()
    {
        float delay = Mathf.Max(0.1f, levelCompleteDisplayTime);
        yield return new WaitForSecondsRealtime(delay);
        LoadNextLevel();
    }

    private void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(levelConfig.nextSceneName))
        {
            if (debugMode)
                Debug.Log($"[LevelManager] Loading next level: {levelConfig.nextSceneName}");
            SceneManager.LoadScene(levelConfig.nextSceneName);
        }
        else
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            string nextScene = DetermineNextScene(currentSceneName);
            
            if (!string.IsNullOrEmpty(nextScene))
            {
                if (debugMode)
                    Debug.Log($"[LevelManager] Loading next level: {nextScene}");
                SceneManager.LoadScene(nextScene);
            }
            else
            {
                if (debugMode)
                    Debug.Log("No more levels available!");
                
                if (GameManager.Instance)
                {
                    GameManager.Instance.GameOver();
                }
            }
        }
    }

    /// <summary>
    /// Configurable level progression using LevelProgressionProfile
    /// </summary>
    private string DetermineNextScene(string currentScene)
    {
        // After Level3 always switch to procedurally generated levels
        if (currentScene == "Level3" || currentScene == "Level_3")
            return "GeneratedLevel";

        // Use progression profile if available
        if (progressionProfile && progressionProfile.ValidateProgression())
        {
            string nextScene = progressionProfile.GetNextScene(currentScene);

            if (!string.IsNullOrEmpty(nextScene))
            {
                // Handle special endless mode setup
                if (nextScene == "Level_OSM" && currentScene == "Level3")
                {
                    PlayerPrefs.SetInt("AutoGenerateOSMMode", 1);
                    PlayerPrefs.SetInt("OSMLocationIndex", 0);
                    PlayerPrefs.Save();
                }
                else if (nextScene == "Level_OSM" && currentScene == "Level_OSM")
                {
                    int currentIndex = PlayerPrefs.GetInt("OSMLocationIndex", 0);
                    PlayerPrefs.SetInt("OSMLocationIndex", currentIndex + 1);
                    PlayerPrefs.Save();
                }

                return nextScene;
            }
        }

        // FALLBACK: Hardcoded progression for backward compatibility
        if (debugMode)
            Debug.LogWarning("[LevelManager] Using fallback level progression");

        if (currentScene == "Level1" || currentScene == "Level_1")
            return "Level2";
        else if (currentScene == "Level2" || currentScene == "Level_2")
            return "Level3";
        else if (currentScene.StartsWith("GeneratedLevel"))
            return "GeneratedLevel";
        else if (currentScene == "Level_OSM")
            return "Level_OSM";

        return string.Empty;
    }

    private void UpdateUI()
    {
        if (!uiController) return;

        // Update collectible counter
        OnCollectibleCountChanged?.Invoke(levelConfig.collectiblesRemaining, levelConfig.totalCollectibles);

        // Update time display if applicable
        if (levelConfig.hasTimeLimit)
        {
            OnTimeLimitUpdate?.Invoke(TimeRemaining);
        }
    }

    // Public methods for external control
    public void ForceCompleteLevel()
    {
        if (debugMode)
            Debug.Log("Forcing level completion");
        
        CompleteLevel();
    }

    public void SetNextScene(string sceneName)
    {
        levelConfig.nextSceneName = sceneName;
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Debug methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugCollectAll()
    {
        if (levelCollectibles == null) return;

        foreach (CollectibleController collectible in levelCollectibles)
        {
            if (collectible != null && !collectible.IsCollected)
            {
                collectible.ForceCollect();
            }
        }
    }

    // Debug info for inspector
    public string GetDebugInfo()
    {
        return $"Events Registered: {eventsRegistered}\n" +
               $"Collectibles Found: {levelCollectibles?.Count ?? 0}\n" +
               $"Collectibles Collected: {collectedCollectibles?.Count ?? 0}\n" +
               $"Total: {levelConfig.totalCollectibles}\n" +
               $"Remaining: {levelConfig.collectiblesRemaining}";
    }

    void OnDrawGizmosSelected()
    {
        if (!debugMode) return;

        // Draw level bounds
        Gizmos.color = levelConfig.themeColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);

        // Draw collectible positions
        if (levelCollectibles != null)
        {
            Gizmos.color = Color.yellow;
            foreach (CollectibleController collectible in levelCollectibles)
            {
                if (collectible != null)
                {
                    Gizmos.DrawWireSphere(collectible.transform.position, 0.5f);
                }
            }
        }

        // Draw goal zone
        if (goalZone)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(goalZone.transform.position, goalZone.transform.localScale);
        }
    }
}
