using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages level progression, collectible tracking, and scene transitions
/// Works in conjunction with GameManager for overall game state
/// MERGED VERSION: Enhanced GameManager integration + OSM endless mode support
/// </summary>
[System.Serializable]
public class LevelConfiguration
{
    [Header("Level Info")]
    public string levelName = "Level";
    public int levelIndex = 1;
    public string nextSceneName = "";

    [Header("Collectibles")]
    public int totalCollectibles = 5;
    public int collectiblesRemaining = 5;

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
    [SerializeField] private HashSet<CollectibleController> levelCollectibles = new HashSet<CollectibleController>(); // COLLECTIBLE COUNTER FIX: ensure unique references
    [SerializeField] private bool autoFindCollectibles = true;
    [SerializeField] private HashSet<CollectibleController> collectedCollectibles; // COLLECTIBLE COUNTER FIX: track collected items

    [Header("Level Progression")]
    [SerializeField] private LevelProgressionProfile progressionProfile;
    
    [Header("UI References")]
    [SerializeField] private UIController uiController;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Private fields
    private bool levelCompleted = false;
    private float levelStartTime;
    private int initialCollectibleCount;
    private Coroutine transitionCoroutine;
    private int collectedCountCache;
    private static readonly Stack<HashSet<CollectibleController>> collectibleSetPool = new Stack<HashSet<CollectibleController>>();

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
    
    // MERGED: Added from REMOTE
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

    void OnDestroy()
    {
        // CLAUDE: FIXED - Unsubscribe from collectible events to avoid memory leaks
        UnsubscribeFromCollectibleEvents();

        if (collectedCollectibles != null)
            ReturnHashSetToPool(collectedCollectibles);

        if (Instance == this)
            Instance = null;
    }

    private static HashSet<CollectibleController> GetHashSetFromPool()
    {
        return collectibleSetPool.Count > 0 ? collectibleSetPool.Pop() : new HashSet<CollectibleController>();
    }

    private static void ReturnHashSetToPool(HashSet<CollectibleController> set)
    {
        set.Clear();
        collectibleSetPool.Push(set);
    }

    private void InitializeLevelManager()
    {
        // Initialize level config if null
        if (levelConfig == null)
        {
            levelConfig = new LevelConfiguration();
            levelConfig.levelName = SceneManager.GetActiveScene().name;
        }

        collectedCollectibles = GetHashSetFromPool();
        collectedCountCache = 0;

        // CLAUDE: FIXED - Prefer inspector assignment over runtime search
        if (!uiController)
        {
            uiController = FindFirstObjectByType<UIController>();
            if (!uiController)
            {
                GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/UI/GameUI");
                if (uiPrefab)
                {
                    var uiInstance = Instantiate(uiPrefab); // UI FIX: centralize UI via prefab
                    uiController = uiInstance.GetComponent<UIController>();
                }
            }
            if (uiController && debugMode)
                Debug.Log("[LevelManager] Auto-found UIController - consider assigning in Inspector");
        }

        levelStartTime = Time.time;
    }

    private void StartLevel()
    {
        // Auto-find collectibles if enabled
        if (autoFindCollectibles)
        {
            FindAllCollectibles();
        }

        // Auto-find goal zone if enabled
        if (autoFindGoalZone && !goalZone)
        {
            FindGoalZone();
        }

        // LEVEL PROGRESSION FIX: goal zone should remain inactive until all collectibles are collected
        SetGoalZoneActive(false);

        // Update collectible count
        UpdateCollectibleCount();

        // Subscribe to collectible events
        SubscribeToCollectibleEvents();

        // Update UI
        UpdateUI();

        // Fire level started event
        OnLevelStarted?.Invoke(levelConfig);

        if (debugMode)
        {
            Debug.Log($"Level started: {levelConfig.levelName}");
            Debug.Log($"Collectibles: {levelConfig.collectiblesRemaining}/{levelConfig.totalCollectibles}");
            if (levelConfig.hasTimeLimit)
                Debug.Log($"Time limit: {levelConfig.timeLimit}s");
        }
    }

    private void FindAllCollectibles()
    {
        levelCollectibles.Clear();
        collectedCollectibles.Clear(); // COLLECTIBLE COUNTER FIX: reset collected tracking
        collectedCountCache = 0;
        initialCollectibleCount = 0;
        foreach (var collectible in FindObjectsByType<CollectibleController>(FindObjectsSortMode.None))
        {
            levelCollectibles.Add(collectible);
        }

        if (debugMode)
            Debug.Log($"Found {levelCollectibles.Count} collectibles in scene");
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
            Debug.Log($"Found goal zone: {goalZone.name}");
        }
    }

    /// <summary>
    /// Enables or disables the goal zone trigger.
    /// </summary>
    /// <param name="active">Whether the goal zone should be active.</param>
    private void SetGoalZoneActive(bool active)
    {
        if (!goalZone) return;

        goalZone.SetActive(active);

        Collider collider = goalZone.GetComponent<Collider>();
        if (collider)
            collider.enabled = active;
    }

    public void UpdateCollectibleCount()
    {
        if (levelCollectibles == null) return;

        if (initialCollectibleCount == 0)
        {
            levelConfig.totalCollectibles = levelCollectibles.Count;
            initialCollectibleCount = levelConfig.totalCollectibles;
        }

        levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.totalCollectibles - collectedCountCache);
    }

    private void SubscribeToCollectibleEvents()
    {
        ToggleCollectibleEvents(true);
    }

    private void UnsubscribeFromCollectibleEvents()
    {
        ToggleCollectibleEvents(false);
    }

    private void ToggleCollectibleEvents(bool subscribe)
    {
        if (levelCollectibles == null) return;

        foreach (var collectible in levelCollectibles)
        {
            if (!collectible) continue;
            collectible.OnCollectiblePickedUp -= OnCollectibleCollected; // COLLECTIBLE COUNTER FIX: avoid duplicate handlers
            if (subscribe)
                collectible.OnCollectiblePickedUp += OnCollectibleCollected;
        }
    }

    public void OnCollectibleCollected(CollectibleController collectible)
    {
        if (levelCompleted) return;

        if (!collectedCollectibles.Add(collectible)) return; // COLLECTIBLE COUNTER FIX: process each collectible once

        collectedCountCache++;
        UpdateCollectibleCount();

        // Update UI (fires OnCollectibleCountChanged internally)
        UpdateUI();

        // Play collection feedback
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySound("Collect");
        }

        // Check for level completion
        if (levelConfig.collectiblesRemaining <= 0)
        {
            // Reactivate goal zone and trigger automatic level progression
            SetGoalZoneActive(true);
            CompleteLevel();
        }

        if (debugMode)
        {
            Debug.Log($"Collectible collected: {collectible.ItemName}");
            Debug.Log($"Remaining: {levelConfig.collectiblesRemaining}/{levelConfig.totalCollectibles}");
        }
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

        // MERGED: Enhanced GameManager integration from REMOTE
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
            Debug.Log($"Level completed: {levelConfig.levelName}");

        // Fire level completed event
        OnLevelCompleted?.Invoke(levelConfig);

        // MERGED: Enhanced GameManager statistics from REMOTE
        if (GameManager.Instance)
        {
            // Check if RecordLevelPerformance method exists
            try
            {
                var methodInfo = GameManager.Instance.GetType().GetMethod("RecordLevelPerformance");
                if (methodInfo != null)
                {
                    int collected = levelConfig.totalCollectibles - levelConfig.collectiblesRemaining;
                    float target = levelConfig.hasTimeLimit ? levelConfig.timeLimit : 0f;
                    methodInfo.Invoke(GameManager.Instance, new object[] { LevelElapsedTime, collected, levelConfig.totalCollectibles, target });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"RecordLevelPerformance method not available: {e.Message}");
            }
        }

        // Start transition to next level
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(LevelCompleteSequence());
    }

    private IEnumerator LevelCompleteSequence()
    {
        // Show level complete message
        if (showLevelComplete && uiController)
        {
            uiController.ShowNotification("Level Complete!", levelCompleteDisplayTime);
            yield return new WaitForSeconds(levelCompleteDisplayTime);
        }

        // Fade out or transition effect
        yield return StartCoroutine(FadeOutTransition());

        // LEVEL PROGRESSION FIX: automatically load the following level
        LoadNextLevel();
    }

    private IEnumerator FadeOutTransition()
    {
        // Simple fade effect - could be enhanced with actual UI fade
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            // Here you could add actual fade effect
            yield return null;
        }
    }

    private void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(levelConfig.nextSceneName))
        {
            // Load specific next scene
            SceneManager.LoadScene(levelConfig.nextSceneName);
        }
        else
        {
            // Auto-determine next level
            string currentSceneName = SceneManager.GetActiveScene().name;
            string nextScene = DetermineNextScene(currentSceneName);
            
            if (!string.IsNullOrEmpty(nextScene))
            {
                SceneManager.LoadScene(nextScene);
            }
            else
            {
                // No more levels - could show victory screen
                if (debugMode)
                    Debug.Log("No more levels available!");
                
                if (GameManager.Instance)
                {
                    // Could trigger victory sequence
                    GameManager.Instance.GameOver();
                }
            }
        }
    }

    /// <summary>
    /// CLAUDE: FIXED - Configurable level progression using LevelProgressionProfile
    /// Replaces hardcoded scene names with data-driven approach (TODO-OPT#8)
    /// </summary>
    private string DetermineNextScene(string currentScene)
    {
        // After Level3 always switch to procedurally generated levels
        if (currentScene.StartsWith("Level3"))
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
                    // MERGED: OSM integration - Activate endless OSM mode after Level 3
                    PlayerPrefs.SetInt("AutoGenerateOSMMode", 1);
                    PlayerPrefs.SetInt("OSMLocationIndex", 0);
                    PlayerPrefs.Save();
                }
                else if (nextScene == "Level_OSM" && currentScene == "Level_OSM")
                {
                    // MERGED: Continue endless OSM mode with next location
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
            return "GeneratedLevel"; // Loop generated levels
        else if (currentScene == "Level_OSM")
            return "Level_OSM"; // Stay in OSM mode

        return string.Empty;
    }

    private void UpdateUI()
    {
        if (!uiController) return;

        // Update collectible counter (this will be extended in UIController)
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

    public void AddCollectible(CollectibleController collectible)
    {
        if (levelCollectibles.Add(collectible))
        {
            collectedCollectibles.Remove(collectible); // COLLECTIBLE COUNTER FIX: reset collected state for pooled objects
            collectible.OnCollectiblePickedUp -= OnCollectibleCollected; // COLLECTIBLE COUNTER FIX: prevent double subscription
            collectible.OnCollectiblePickedUp += OnCollectibleCollected;
            UpdateCollectibleCount();
            UpdateUI();
        }
    }

    public void RemoveCollectible(CollectibleController collectible)
    {
        if (levelCollectibles.Remove(collectible))
        {
            collectedCollectibles.Remove(collectible); // COLLECTIBLE COUNTER FIX: maintain tracking
            collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
            UpdateCollectibleCount();
            UpdateUI();
        }
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

    void OnDrawGizmosSelected()
    {
        if (!debugMode) return;

        // Draw level bounds or important positions
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
