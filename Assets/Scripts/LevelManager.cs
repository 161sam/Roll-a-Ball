using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public class LevelConfiguration
{
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
    public LevelConfiguration Config => levelConfig;
    public LevelProgressionProfile ProgressionProfile => progressionProfile;
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

        // Nur EINMAL am Anfang setzen
        levelConfig.totalCollectibles = levelCollectibles.Count;
        levelConfig.collectiblesRemaining = levelConfig.totalCollectibles;

        if (autoFindGoalZone && !goalZone) FindGoalZone();
        SetGoalZoneActive(false);
        SafeSubscribeToAllEvents();
        UpdateUI();
        OnLevelStarted?.Invoke(levelConfig);

        if (debugMode)
            Debug.Log($"[LevelManager] Started {levelConfig.levelName} with {levelConfig.totalCollectibles} collectibles.");
    }

    private void FindAllCollectibles()
    {
        levelCollectibles.Clear();
        collectedCollectibles.Clear();
        foreach (var collectible in FindObjectsByType<CollectibleController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (collectible != null && !collectible.IsCollected)
                levelCollectibles.Add(collectible);
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
                return; // Bereits gezählt → ignorieren

            levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.collectiblesRemaining - 1);
        }

        UpdateUI();

        if (AudioManager.Instance)
            AudioManager.Instance.PlaySound("Collect");

        if (levelConfig.collectiblesRemaining <= 0)
            CompleteLevel();
    }

    public void AddCollectible(CollectibleController collectible)
    {
        if (collectible == null || levelCollectibles.Contains(collectible)) return;
        levelCollectibles.Add(collectible);

        // Neue Collectibles erhöhen nur total, wenn Level noch nicht gestartet oder Rescan
        levelConfig.totalCollectibles++;
        levelConfig.collectiblesRemaining++;
        collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
        collectible.OnCollectiblePickedUp += OnCollectibleCollected;
        UpdateUI();
    }

    public void AddCollectiblesBulk(CollectibleController[] collectibles)
    {
        if (collectibles == null) return;
        foreach (var c in collectibles)
            if (c != null && !levelCollectibles.Contains(c))
            {
                levelCollectibles.Add(c);
                levelConfig.totalCollectibles++;
                levelConfig.collectiblesRemaining++;
            }
        SafeSubscribeToAllEvents();
        UpdateUI();
    }

    public void RemoveCollectible(CollectibleController collectible)
    {
        if (collectible == null) return;
        if (levelCollectibles.Remove(collectible) && !collectedCollectibles.Contains(collectible))
            levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.collectiblesRemaining - 1);
        UpdateUI();
    }

    public void RescanCollectibles()
    {
        SafeUnsubscribeFromAllEvents();
        FindAllCollectibles();
        levelConfig.totalCollectibles = levelCollectibles.Count;
        levelConfig.collectiblesRemaining = levelConfig.totalCollectibles;
        SafeSubscribeToAllEvents();
        UpdateUI();
    }

    public void SetNextScene(string sceneName) => levelConfig.nextSceneName = sceneName;
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
