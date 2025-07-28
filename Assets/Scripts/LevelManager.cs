using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages level progression, collectible tracking, and scene transitions
/// Works in conjunction with GameManager for overall game state
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
    [SerializeField] private List<CollectibleController> levelCollectibles;
    [SerializeField] private bool autoFindCollectibles = true;

    [Header("UI References")]
    [SerializeField] private UIController uiController;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Private fields
    private bool levelCompleted = false;
    private float levelStartTime;
    private int initialCollectibleCount;
    private Coroutine transitionCoroutine;

    // Events
    public System.Action<int, int> OnCollectibleCountChanged; // remaining, total
    public System.Action<LevelConfiguration> OnLevelCompleted;
    public System.Action<LevelConfiguration> OnLevelStarted;
    public System.Action<float> OnTimeLimitUpdate; // remaining time

    // Singleton pattern
    public static LevelManager Instance { get; private set; }

    // Properties
    public LevelConfiguration Config => levelConfig;
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

    void OnDestroy()
    {
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

        // Find UI Controller if not assigned
        if (!uiController)
            uiController = FindFirstObjectByType<UIController>();

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
        CollectibleController[] foundCollectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        levelCollectibles = foundCollectibles.ToList();

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

    public void UpdateCollectibleCount()
    {
        if (levelCollectibles != null)
        {
            int remaining = levelCollectibles.Count(c => c != null && !c.IsCollected);
            levelConfig.collectiblesRemaining = remaining;
            levelConfig.totalCollectibles = levelCollectibles.Count;
            initialCollectibleCount = levelConfig.totalCollectibles;
        }
    }

    private void SubscribeToCollectibleEvents()
    {
        if (levelCollectibles == null) return;

        foreach (CollectibleController collectible in levelCollectibles)
        {
            if (collectible != null)
            {
                collectible.OnCollectiblePickedUp += OnCollectibleCollected;
            }
        }
    }

    private void UnsubscribeFromCollectibleEvents()
    {
        if (levelCollectibles == null) return;

        foreach (CollectibleController collectible in levelCollectibles)
        {
            if (collectible != null)
            {
                collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
            }
        }
    }

    public void OnCollectibleCollected(CollectibleController collectible)
    {
        if (levelCompleted) return;

        levelConfig.collectiblesRemaining--;

        // Fire count changed event
        OnCollectibleCountChanged?.Invoke(levelConfig.collectiblesRemaining, levelConfig.totalCollectibles);

        // Update UI
        UpdateUI();

        // Play collection feedback
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySound("Collect");
        }

        // Check for level completion
        if (levelConfig.collectiblesRemaining <= 0)
        {
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

        // Game over logic
        if (GameManager.Instance)
        {
            GameManager.Instance.RegisterPlayerDeath();
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

        // Update GameManager statistics
        if (GameManager.Instance)
        {
            int collected = levelConfig.totalCollectibles - levelConfig.collectiblesRemaining;
            float target = levelConfig.hasTimeLimit ? levelConfig.timeLimit : 0f;
            GameManager.Instance.RecordLevelPerformance(LevelElapsedTime, collected, levelConfig.totalCollectibles, target);
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

        // Load next scene
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

    private string DetermineNextScene(string currentScene)
    {
        // Simple level progression logic
        if (currentScene == "Level1" || currentScene == "Level_1")
            return "Level2";
        else if (currentScene == "Level2" || currentScene == "Level_2")
            return "Level3";
        else if (currentScene == "Level3" || currentScene == "Level_3")
            return ""; // Could be procedural level scene
        
        return "";
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
        if (levelCollectibles == null)
            levelCollectibles = new List<CollectibleController>();

        if (!levelCollectibles.Contains(collectible))
        {
            levelCollectibles.Add(collectible);
            collectible.OnCollectiblePickedUp += OnCollectibleCollected;
            UpdateCollectibleCount();
            UpdateUI();
        }
    }

    public void RemoveCollectible(CollectibleController collectible)
    {
        if (levelCollectibles != null && levelCollectibles.Contains(collectible))
        {
            levelCollectibles.Remove(collectible);
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
