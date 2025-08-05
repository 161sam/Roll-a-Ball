using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Manages level progression, collectible tracking, and scene transitions
/// FIXED: Centralized event registration, proper counting, and safe UI updates
/// </summary>
[System.Serializable]
public class LevelConfiguration
{
    [Header("Level Info")]
    public string levelName = "Level";
    public int levelIndex = 1;
    public string nextSceneName = "";

    [Header("Collectibles")]
    public int totalCollectibles = 0; 
    public int collectiblesRemaining = 0;

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

    [Header("Goal Zone")]
    [SerializeField] private GameObject goalZone;
    [SerializeField] private bool autoFindGoalZone = true;

    [Header("Collectibles")]
    [SerializeField] private List<CollectibleController> levelCollectibles = new List<CollectibleController>();
    [SerializeField] private bool autoFindCollectibles = true;
    private readonly HashSet<CollectibleController> collectedCollectibles = new HashSet<CollectibleController>();

    [Header("UI References")]
    [SerializeField] private UIController uiController;

    [Header("Level Progression")]
    [SerializeField] private LevelProgressionProfile progressionProfile;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Internal
    private bool levelCompleted = false;
    private bool eventsRegistered = false;
    private float levelStartTime;
    private readonly object lockObject = new object();

    // Events
    public System.Action<int, int> OnCollectibleCountChanged; 
    public System.Action<LevelConfiguration> OnLevelCompleted;
    public System.Action<LevelConfiguration> OnLevelStarted;
    public System.Action<float> OnTimeLimitUpdate;

    public static LevelManager Instance { get; private set; }

    public int CollectiblesRemaining => levelConfig.collectiblesRemaining;
    public int TotalCollectibles => levelConfig.totalCollectibles;
    public float LevelElapsedTime => Time.time - levelStartTime;

    /// <summary>
    /// Provides read-only access to the active level configuration.
    /// </summary>
    public LevelConfiguration Config => levelConfig;

    /// <summary>
    /// Gets the progression profile used to determine level sequence.
    /// </summary>
    public LevelProgressionProfile ProgressionProfile => progressionProfile;

    /// <summary>
    /// Indicates whether the current level has been completed.
    /// </summary>
    public bool IsLevelCompleted => levelCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeLevelManager();
    }

    void Start() => StartLevel();
    void Update()
    {
        if (levelConfig.hasTimeLimit && !levelCompleted)
            CheckTimeLimit();
    }
    void OnDisable() => SafeUnsubscribeFromAllEvents();
    void OnDestroy()
    {
        SafeUnsubscribeFromAllEvents();
        if (Instance == this) Instance = null;
    }

    private void InitializeLevelManager()
    {
        if (levelConfig == null)
            levelConfig = new LevelConfiguration { levelName = SceneManager.GetActiveScene().name };

        levelCollectibles.Clear();
        collectedCollectibles.Clear();

        if (!uiController)
        {
            uiController = FindFirstObjectByType<UIController>();
            if (!uiController)
            {
                var uiPrefab = Resources.Load<GameObject>("Prefabs/UI/GameUI");
                if (uiPrefab) uiController = Instantiate(uiPrefab).GetComponent<UIController>();
            }
        }

        levelStartTime = Time.time;
        eventsRegistered = false;
    }

    private void StartLevel()
    {
        if (autoFindCollectibles) FindAllCollectibles();
        InitializeCollectibleCounts();
        if (autoFindGoalZone && !goalZone) FindGoalZone();
        SetGoalZoneActive(false);
        SafeSubscribeToAllEvents();
        UpdateUI();
        OnLevelStarted?.Invoke(levelConfig);

        if (debugMode)
        {
            Debug.Log($"[LevelManager] Started {levelConfig.levelName} with {levelConfig.totalCollectibles} collectibles.");
        }
    }

    private void FindAllCollectibles()
    {
        levelCollectibles.Clear();
        collectedCollectibles.Clear();
        foreach (var collectible in FindObjectsByType<CollectibleController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (collectible != null && !collectible.IsCollected)
                levelCollectibles.Add(collectible);
    }

    private void InitializeCollectibleCounts()
    {
        levelConfig.totalCollectibles = levelCollectibles.Count;
        levelConfig.collectiblesRemaining = levelConfig.totalCollectibles;
    }

    private void FindGoalZone()
    {
        var candidates = GameObject.FindGameObjectsWithTag("Finish");
        goalZone = candidates.Length > 0 ? candidates[0] : GameObject.Find("GoalZone");
    }

    private void SetGoalZoneActive(bool active)
    {
        if (!goalZone) return;
        goalZone.SetActive(active);
        var col = goalZone.GetComponent<Collider>();
        if (col) col.enabled = active;
    }

    private void SafeSubscribeToAllEvents()
    {
        lock (lockObject)
        {
            if (eventsRegistered) return;
            foreach (var collectible in levelCollectibles)
            {
                collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
                collectible.OnCollectiblePickedUp += OnCollectibleCollected;
            }
            eventsRegistered = true;
        }
    }

    private void SafeUnsubscribeFromAllEvents()
    {
        lock (lockObject)
        {
            if (!eventsRegistered) return;
            foreach (var collectible in levelCollectibles)
                collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
            eventsRegistered = false;
        }
    }

    public void OnCollectibleCollected(CollectibleController collectible)
    {
        if (levelCompleted || collectible == null) return;

        lock (lockObject)
        {
            if (!collectedCollectibles.Add(collectible))
            {
                if (debugMode)
                    Debug.Log($"[LevelManager] Duplicate event for {collectible.name}");
                return;
            }

            int prevRemaining = levelConfig.collectiblesRemaining;
            levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.totalCollectibles - collectedCollectibles.Count);
            if (levelConfig.collectiblesRemaining != prevRemaining)
                UpdateUI();
        }

        if (AudioManager.Instance)
            AudioManager.Instance.PlaySound("Collect");

        if (levelConfig.collectiblesRemaining <= 0)
            CompleteLevel();
    }

    public void AddCollectible(CollectibleController collectible)
    {
        if (collectible == null || levelCollectibles.Contains(collectible)) return;
        levelCollectibles.Add(collectible);
        collectedCollectibles.Remove(collectible);
        InitializeCollectibleCounts();
        eventsRegistered = false; // Force re-subscribe centrally
        SafeSubscribeToAllEvents();
        UpdateUI();
    }

    public void AddCollectiblesBulk(CollectibleController[] collectibles)
    {
        if (collectibles == null) return;
        foreach (var c in collectibles)
            if (c != null && !levelCollectibles.Contains(c))
                levelCollectibles.Add(c);
        InitializeCollectibleCounts();
        eventsRegistered = false;
        SafeSubscribeToAllEvents();
        UpdateUI();
    }

    /// <summary>
    /// Removes a collectible from tracking and updates counters accordingly.
    /// </summary>
    /// <param name="collectible">Collectible to remove.</param>
    public void RemoveCollectible(CollectibleController collectible)
    {
        if (collectible == null) return;
        levelCollectibles.Remove(collectible);
        collectedCollectibles.Remove(collectible);
        InitializeCollectibleCounts();
        eventsRegistered = false;
        SafeSubscribeToAllEvents();
        UpdateUI();
    }

    public void RescanCollectibles()
    {
        SafeUnsubscribeFromAllEvents();
        FindAllCollectibles();
        InitializeCollectibleCounts();
        SafeSubscribeToAllEvents();
        UpdateUI();
    }

    /// <summary>
    /// Sets the name of the scene to load after this level completes.
    /// </summary>
    /// <param name="sceneName">Name of the next scene.</param>
    public void SetNextScene(string sceneName) => levelConfig.nextSceneName = sceneName;

    /// <summary>
    /// Forces immediate completion of the level regardless of current state.
    /// </summary>
    public void ForceCompleteLevel() => CompleteLevel();

    private void CompleteLevel()
    {
        if (levelCompleted) return;
        levelCompleted = true;
        SetGoalZoneActive(true);
        OnLevelCompleted?.Invoke(levelConfig);
        Invoke(nameof(LoadNextLevel), 0.2f);
    }

    private void LoadNextLevel()
    {
        string nextScene = !string.IsNullOrEmpty(levelConfig.nextSceneName) ? levelConfig.nextSceneName : DetermineNextScene(SceneManager.GetActiveScene().name);
        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
        else
            GameManager.Instance?.GameOver();
    }

    private string DetermineNextScene(string currentScene)
    {
        if (currentScene.StartsWith("Level3")) return "GeneratedLevel";
        if (progressionProfile && progressionProfile.ValidateProgression())
            return progressionProfile.GetNextScene(currentScene);
        if (currentScene == "Level1") return "Level2";
        if (currentScene == "Level2") return "Level3";
        if (currentScene.StartsWith("GeneratedLevel")) return "GeneratedLevel";
        if (currentScene == "Level_OSM") return "Level_OSM";
        return string.Empty;
    }

    private void CheckTimeLimit()
    {
        float timeRemaining = levelConfig.timeLimit - (Time.time - levelStartTime);
        OnTimeLimitUpdate?.Invoke(Mathf.Max(0, timeRemaining));
        if (timeRemaining <= 0f) GameManager.Instance?.GameOver();
    }

    private void UpdateUI()
    {
        if (!uiController) return;
        OnCollectibleCountChanged?.Invoke(levelConfig.collectiblesRemaining, levelConfig.totalCollectibles);
    }
}
