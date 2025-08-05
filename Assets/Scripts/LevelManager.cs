using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
    [SerializeField] private bool autoFindCollectibles = true;

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

    public static LevelManager Instance { get; private set; }

    public int CollectiblesRemaining => levelConfig.collectiblesRemaining;
    public int TotalCollectibles => levelConfig.totalCollectibles;
    public bool IsLevelCompleted => levelCompleted;

    public event System.Action<int, int> OnCollectibleCountChanged;
    public event System.Action<LevelConfiguration> OnLevelCompleted;
    public event System.Action<LevelConfiguration> OnLevelStarted;
    public event System.Action<float> OnTimeLimitUpdate;

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

    private void InitializeLevelManager()
    {
        if (levelConfig == null)
            levelConfig = new LevelConfiguration { levelName = SceneManager.GetActiveScene().name };

        levelCollectibles.Clear();
        collectedCollectibles.Clear();

        if (!uiController)
            uiController = FindFirstObjectByType<UIController>();

        levelStartTime = Time.time;
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

    public void OnCollectibleCollected(CollectibleController collectible)
    {
        if (levelCompleted || collectible == null) return;

        lock (lockObject)
        {
            if (!collectedCollectibles.Add(collectible)) return;
            levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.totalCollectibles - collectedCollectibles.Count);
        }

        UpdateUI();

        if (levelConfig.collectiblesRemaining <= 0)
            CompleteLevel();
    }

    public void AddCollectible(CollectibleController collectible)
    {
        if (collectible == null || levelCollectibles.Contains(collectible)) return;
        levelCollectibles.Add(collectible);
        InitializeCollectibleCounts();
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
        SafeSubscribeToAllEvents();
        UpdateUI();
    }

    public bool ContainsCollectible(CollectibleController collectible) =>
        levelCollectibles.Contains(collectible);

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
        string nextScene = !string.IsNullOrEmpty(levelConfig.nextSceneName) ? 
            levelConfig.nextSceneName : DetermineNextScene(SceneManager.GetActiveScene().name);

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
