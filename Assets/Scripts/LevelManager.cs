using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using RollABall.Utility;

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
    [SerializeField] private List<CollectibleController> levelCollectibles = new();
    private readonly HashSet<CollectibleController> collectedCollectibles = new();
    private readonly HashSet<CollectibleController> registeredCollectibles = new();
    [SerializeField] private bool autoFindCollectibles = true;
    public bool AutoFindCollectibles => autoFindCollectibles;

    [Header("UI References")]
    [SerializeField] private UIController uiController;

    [Header("Level Progression")]
    [SerializeField] private LevelProgressionProfile progressionProfile;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private bool levelCompleted = false;
    private float levelStartTime;
    private readonly object lockObject = new();

    public static LevelManager Instance { get; private set; }

    public int CollectiblesRemaining => levelConfig.collectiblesRemaining;
    public int TotalCollectibles => levelConfig.totalCollectibles;
    public bool IsLevelCompleted => levelCompleted;

    public LevelConfiguration Config
    {
        get => levelConfig;
        set => levelConfig = value;
    }

    public event System.Action<int, int> OnCollectibleCountChanged;
    public event System.Action<LevelConfiguration> OnLevelCompleted;
    public event System.Action<LevelConfiguration> OnLevelStarted;
    public event System.Action<float> OnTimeLimitUpdate;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeLevelManager();
    }

    private void Start() => StartLevel();

    private void Update()
    {
        if (levelConfig.hasTimeLimit && !levelCompleted)
            CheckTimeLimit();
    }

    public LevelConfiguration GetLevelConfiguration() => levelConfig;

    private void InitializeLevelManager()
    {
        if (levelConfig == null)
            levelConfig = new LevelConfiguration { levelName = SceneManager.GetActiveScene().name };

        levelCollectibles.Clear();
        collectedCollectibles.Clear();
        registeredCollectibles.Clear();

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
        RegisterAllCollectibleEvents();
        UpdateUI();
        OnLevelStarted?.Invoke(levelConfig);

        if (debugMode)
            Debug.Log($"[LevelManager] Level gestartet: {levelConfig.totalCollectibles} Collectibles gefunden");
    }

    private void FindAllCollectibles()
    {
        var foundCollectibles = FindObjectsByType<CollectibleController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(c => c != null && c.isActiveAndEnabled && !c.IsCollected)
            .ToArray();

        levelCollectibles.Clear();
        levelCollectibles.AddRange(foundCollectibles);

        if (debugMode)
            Debug.Log($"[LevelManager] FindAllCollectibles: {foundCollectibles.Length} Collectibles gefunden");
    }

    private void InitializeCollectibleCounts()
    {
        // Eindeutige Liste erzwingen - alle null-Referenzen und doppelt referenzierte
        // GameObjects entfernen
        levelCollectibles = levelCollectibles
            .Where(c => c != null && c.isActiveAndEnabled && !c.IsCollected)
            .GroupBy(c => c.gameObject)
            .Select(g => g.First())
            .ToList();

        levelConfig.totalCollectibles = levelCollectibles.Count;
        levelConfig.collectiblesRemaining = levelConfig.totalCollectibles;

        if (debugMode)
        {
            foreach (var collectible in levelCollectibles)
            {
                Debug.Log($"[LevelManager] Collectible gezählt: {collectible.name} | Pfad: {collectible.transform.GetHierarchyPath()} | Aktiv: {collectible.gameObject.activeInHierarchy}");
            }
            Debug.Log($"[LevelManager] InitializeCollectibleCounts: {levelConfig.totalCollectibles} eindeutige Collectibles");
        }
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

    private void RegisterAllCollectibleEvents()
    {
        lock (lockObject)
        {
            registeredCollectibles.Clear();
            
            foreach (var collectible in levelCollectibles)
            {
                if (collectible != null && registeredCollectibles.Add(collectible))
                {
                    // Erst abmelden, dann anmelden um doppelte Events zu verhindern
                    collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
                    collectible.OnCollectiblePickedUp += OnCollectibleCollected;
                }
            }

            if (debugMode)
                Debug.Log($"[LevelManager] Events registriert für {registeredCollectibles.Count} Collectibles");
        }
    }

    public void OnCollectibleCollected(CollectibleController collectible)
    {
        if (levelCompleted || collectible == null) return;

        bool wasNewCollection = false;
        lock (lockObject)
        {
            wasNewCollection = collectedCollectibles.Add(collectible);
            if (wasNewCollection)
            {
                levelConfig.collectiblesRemaining = Mathf.Max(0, levelConfig.totalCollectibles - collectedCollectibles.Count);
            }
        }

        if (!wasNewCollection)
        {
            if (debugMode)
                Debug.LogWarning($"[LevelManager] Collectible '{collectible.name}' bereits eingesammelt - ignoriert");
            return;
        }

        UpdateUI();

        if (debugMode)
            Debug.Log($"[LevelManager] Collectible eingesammelt: {collectedCollectibles.Count}/{levelConfig.totalCollectibles} (verbleibend: {levelConfig.collectiblesRemaining})");

        if (levelConfig.collectiblesRemaining <= 0)
            CompleteLevel();
    }

    public void AddCollectible(CollectibleController collectible)
    {
        if (collectible == null) return;

        lock (lockObject)
        {
            // Nur hinzufügen wenn GameObject noch nicht vorhanden
            bool exists = levelCollectibles.Any(c => c != null && c.gameObject == collectible.gameObject);
            if (!exists)
            {
                levelCollectibles.Add(collectible);
                InitializeCollectibleCounts();

                // Event für neues Collectible registrieren
                if (registeredCollectibles.Add(collectible))
                {
                    collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
                    collectible.OnCollectiblePickedUp += OnCollectibleCollected;
                }

                UpdateUI();

                if (debugMode)
                    Debug.Log($"[LevelManager] Collectible '{collectible.name}' hinzugefügt. Total: {levelConfig.totalCollectibles}");
            }
        }
    }

    public void AddCollectiblesBulk(CollectibleController[] collectibles)
    {
        if (collectibles == null) return;

        lock (lockObject)
        {
            var existing = new HashSet<GameObject>(levelCollectibles.Where(c => c != null).Select(c => c.gameObject));
            bool anyAdded = false;
            foreach (var collectible in collectibles)
            {
                if (collectible != null && existing.Add(collectible.gameObject))
                {
                    levelCollectibles.Add(collectible);
                    anyAdded = true;
                }
            }

            if (anyAdded)
            {
                InitializeCollectibleCounts();
                RegisterAllCollectibleEvents();
                UpdateUI();
            }
        }
    }

    public bool ContainsCollectible(CollectibleController collectible) =>
        collectible != null && levelCollectibles.Any(c => c != null && c.gameObject == collectible.gameObject);

    private void CompleteLevel()
    {
        if (levelCompleted) return;
        
        levelCompleted = true;
        SetGoalZoneActive(true);
        OnLevelCompleted?.Invoke(levelConfig);
        
        if (debugMode)
            Debug.Log($"[LevelManager] Level abgeschlossen! Alle {levelConfig.totalCollectibles} Collectibles eingesammelt");
        
        Invoke(nameof(LoadNextLevel), 0.2f);
    }

    public void ForceCompleteLevel()
    {
        if (levelCompleted) return;
        levelCompleted = true;
        SetGoalZoneActive(true);
        OnLevelCompleted?.Invoke(levelConfig);
        LoadNextLevel();
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
            _ when currentScene.StartsWith("GeneratedLevel") => "GeneratedLevel",
            _ when currentScene.StartsWith("Level_OSM") => "Level_OSM",
            _ => string.Empty
        };
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

    public void RescanCollectibles()
    {
        if (debugMode)
            Debug.Log("[LevelManager] Rescan Collectibles gestartet");
            
        FindAllCollectibles();
        InitializeCollectibleCounts();
        RegisterAllCollectibleEvents();
        UpdateUI();
        
        if (debugMode)
            Debug.Log($"[LevelManager] Rescan abgeschlossen: {levelConfig.totalCollectibles} Collectibles aktiv");
    }

    private void OnDestroy()
    {
        // Events abmelden um Memory Leaks zu verhindern
        lock (lockObject)
        {
            foreach (var collectible in registeredCollectibles)
            {
                if (collectible != null)
                    collectible.OnCollectiblePickedUp -= OnCollectibleCollected;
            }
            registeredCollectibles.Clear();
        }
    }
}
