using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Bereinigter LevelManager – stabile Zählung der Collectibles ohne doppelte Registrierung.
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
    public bool IsLevelCompleted => levelCompleted;
    public LevelConfiguration Config => levelConfig;

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
        }

        levelStartTime = Time.time;
        eventsRegistered = false;
    }

    private void StartLevel()
    {
        if (autoFindGoalZone && !goalZone) FindGoalZone();
        SetGoalZoneActive(false);

        // Wichtig: totalCollectibles nur hier setzen
        levelConfig.totalCollectibles = levelCollectibles.Count;
        levelConfig.collectiblesRemaining = levelConfig.totalCollectibles;

        SafeSubscribeToAllEvents();
        UpdateUI();

        OnLevelStarted?.Invoke(levelConfig);

        if (debugMode)
            Debug.Log($"[LevelManager] Gestartet: {levelConfig.levelName} | Total: {levelConfig.totalCollectibles}");
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
        if (eventsRegistered) return;
        foreach (var collectible in levelCollectibles)
        {
            if (collectible != null)
            {
                collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
                collectible.OnCollectiblePickedUp += OnCollectibleCollected;
            }
        }
        eventsRegistered = true;
    }

    private void SafeUnsubscribeFromAllEvents()
    {
        if (!eventsRegistered) return;
        foreach (var collectible in levelCollectibles)
        {
            if (collectible != null)
                collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
        }
        eventsRegistered = false;
    }

    public void AddCollectible(CollectibleController collectible)
    {
        if (collectible == null || levelCollectibles.Contains(collectible)) return;
        levelCollectibles.Add(collectible);

        // Nur collectiblesRemaining anpassen
        levelConfig.collectiblesRemaining = levelConfig.totalCollectibles - collectedCollectibles.Count;

        if (eventsRegistered)
        {
            collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
            collectible.OnCollectiblePickedUp += OnCollectibleCollected;
        }

        UpdateUI();
    }

    public void AddCollectiblesBulk(CollectibleController[] collectibles)
    {
        if (collectibles == null) return;

        int added = 0;
        foreach (var c in collectibles)
        {
            if (c != null && !levelCollectibles.Contains(c))
            {
                levelCollectibles.Add(c);
                added++;
            }
        }

        // Nur am Levelstart totalCollectibles setzen → hier nicht ändern
        levelConfig.collectiblesRemaining = levelConfig.totalCollectibles - collectedCollectibles.Count;

        SafeSubscribeToAllEvents();
        UpdateUI();

        if (debugMode)
            Debug.Log($"[LevelManager] Bulk hinzugefügt: {added} Collectibles");
    }

    public void OnCollectibleCollected(CollectibleController collectible)
    {
        if (levelCompleted || collectible == null) return;

        bool changed = false;
        lock (lockObject)
        {
            if (collectedCollectibles.Add(collectible))
            {
                levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.totalCollectibles - collectedCollectibles.Count);
                changed = true;
            }
        }

        if (changed)
        {
            UpdateUI();

            if (AudioManager.Instance)
                AudioManager.Instance.PlaySound("Collect");

            if (levelConfig.collectiblesRemaining <= 0)
                CompleteLevel();
        }
    }

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
        string nextScene = !string.IsNullOrEmpty(levelConfig.nextSceneName)
            ? levelConfig.nextSceneName
            : DetermineNextScene(SceneManager.GetActiveScene().name);

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

        return currentScene switch
        {
            "Level1" => "Level2",
            "Level2" => "Level3",
            "Level_OSM" => "Level_OSM",
            _ when currentScene.StartsWith("GeneratedLevel") => "GeneratedLevel",
            _ => string.Empty
        };
    }

    private void CheckTimeLimit()
    {
        float timeRemaining = levelConfig.timeLimit - (Time.time - levelStartTime);
        OnTimeLimitUpdate?.Invoke(Mathf.Max(0, timeRemaining));

        if (timeRemaining <= 0f)
            GameManager.Instance?.GameOver();
    }

    private void UpdateUI()
    {
        if (uiController)
            OnCollectibleCountChanged?.Invoke(levelConfig.collectiblesRemaining, levelConfig.totalCollectibles);
    }
}
