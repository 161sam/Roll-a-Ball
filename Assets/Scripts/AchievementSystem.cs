using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RollABall.Utility;

/// <summary>
/// Comprehensive achievement system for Roll-a-Ball
/// Tracks player progress and unlocks achievements
/// </summary>
[System.Serializable]
public class Achievement
{
    [Header("Basic Info")]
    public string id;
    public string title;
    [TextArea(2, 4)]
    public string description;
    public string iconPath;

    [Header("Progress")]
    public AchievementType type;
    public int targetValue = 1;
    public int currentProgress = 0;
    public bool isUnlocked = false;
    public System.DateTime unlockedDate;

    [Header("Rewards")]
    public int scoreReward = 100;
    public string rewardMessage = "Achievement unlocked!";

    [Header("Display")]
    public AchievementCategory category;
    public AchievementRarity rarity = AchievementRarity.Common;
    public bool isHidden = false; 
    public bool isSecret = false;

    public float ProgressPercentage => Mathf.Clamp01((float)currentProgress / targetValue) * 100f;
    public bool IsCompleted => currentProgress >= targetValue;

    public Achievement()
    {
        id = System.Guid.NewGuid().ToString();
        unlockedDate = System.DateTime.MinValue;
    }

    public Achievement(string achievementId, string achievementTitle, string achievementDescription,
                      AchievementType achievementType, int target, AchievementCategory cat)
    {
        id = achievementId;
        title = achievementTitle;
        description = achievementDescription;
        type = achievementType;
        targetValue = target;
        category = cat;
        unlockedDate = System.DateTime.MinValue;
    }
}

public enum AchievementType
{
    OneTime,
    Progressive,
    Cumulative,
    Conditional
}

public enum AchievementCategory
{
    General,
    Collectibles,
    Movement,
    Speed,
    Exploration,
    Levels,
    Time,
    Special
}

public enum AchievementRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[AddComponentMenu("Game/AchievementSystem")]
public class AchievementSystem : MonoBehaviour
{
    [Header("Achievement Configuration")]
    [SerializeField] private List<Achievement> allAchievements = new List<Achievement>();
    [SerializeField] private AchievementDatabase achievementDatabase;
    [SerializeField] private bool enableNotifications = true;
    [SerializeField] private float notificationDuration = 3f;
    [SerializeField] private bool enableSounds = true;

    [Header("UI References")]
    [SerializeField] private GameObject achievementNotificationPrefab;
    [SerializeField] private Transform notificationParent;
    [SerializeField] private AudioClip achievementUnlockedSound;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private bool resetAchievementsOnStart = false;

    private Dictionary<string, Achievement> achievementLookup;
    private Queue<Achievement> pendingNotifications;
    private bool isShowingNotification = false;
    private PlayerController cachedPlayer;

    public System.Action<Achievement> OnAchievementUnlocked;
    public System.Action<Achievement> OnAchievementProgress;
    public System.Action<List<Achievement>> OnAchievementsLoaded;

    public static AchievementSystem Instance { get; private set; }

    public List<Achievement> AllAchievements => allAchievements;
    public List<Achievement> UnlockedAchievements => allAchievements.Where(a => a.isUnlocked).ToList();
    public List<Achievement> LockedAchievements => allAchievements.Where(a => !a.isUnlocked).ToList();
    public int TotalAchievements => allAchievements.Count;
    public int UnlockedCount => allAchievements.Count(a => a.isUnlocked);
    public float CompletionPercentage => TotalAchievements > 0 ? (float)UnlockedCount / TotalAchievements * 100f : 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAchievementSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeAchievements();
        LoadAchievementProgress();
        SubscribeToGameEvents();
    }

    private void InitializeAchievementSystem()
    {
        achievementLookup = new Dictionary<string, Achievement>();
        pendingNotifications = new Queue<Achievement>();
        LogAchievement("Achievement system initialized");
    }

    private void InitializeAchievements()
    {
        if (resetAchievementsOnStart)
        {
            ResetAllAchievements();
        }

        if (allAchievements.Count == 0)
        {
            if (achievementDatabase && achievementDatabase.achievements.Count > 0)
            {
                allAchievements = new List<Achievement>(achievementDatabase.achievements);
            }
            else
            {
                CreateDefaultAchievements();
            }
        }

        achievementLookup.Clear();
        foreach (Achievement achievement in allAchievements)
        {
            if (!achievementLookup.ContainsKey(achievement.id))
            {
                achievementLookup[achievement.id] = achievement;
            }
        }

        OnAchievementsLoaded?.Invoke(allAchievements);
        LogAchievement($"Loaded {allAchievements.Count} achievements");
    }

    private void CreateDefaultAchievements()
    {
        allAchievements.Clear();

        AddAchievement("first_steps", "First Steps", "Start your first Roll-a-Ball adventure!", AchievementType.OneTime, 1, AchievementCategory.General);
        AddAchievement("collector", "Collector", "Collect your first item!", AchievementType.OneTime, 1, AchievementCategory.Collectibles);
        AddAchievement("level_complete", "Level Master", "Complete your first level!", AchievementType.OneTime, 1, AchievementCategory.Levels);

        AddAchievement("collect_10", "Gathering Steam", "Collect 10 items", AchievementType.Cumulative, 10, AchievementCategory.Collectibles);
        AddAchievement("collect_50", "Item Hunter", "Collect 50 items", AchievementType.Cumulative, 50, AchievementCategory.Collectibles);
        AddAchievement("collect_100", "Master Collector", "Collect 100 items", AchievementType.Cumulative, 100, AchievementCategory.Collectibles);

        AddAchievement("jump_master", "Jump Master", "Perform 100 jumps", AchievementType.Cumulative, 100, AchievementCategory.Movement);
        AddAchievement("double_jump", "Air Walker", "Perform your first double jump!", AchievementType.OneTime, 1, AchievementCategory.Movement);
        AddAchievement("frequent_flyer", "Frequent Flyer", "Spend 60 seconds in the air total", AchievementType.Cumulative, 60, AchievementCategory.Movement);

        AddAchievement("speed_demon", "Speed Demon", "Reach a speed of 20 units/second", AchievementType.Progressive, 20, AchievementCategory.Speed);
        AddAchievement("sonic_boom", "Sonic Boom", "Reach a speed of 30 units/second", AchievementType.Progressive, 30, AchievementCategory.Speed);

        AddAchievement("level_1_complete", "Beginner's Luck", "Complete Level 1", AchievementType.OneTime, 1, AchievementCategory.Levels);
        AddAchievement("level_2_complete", "Getting Warmed Up", "Complete Level 2", AchievementType.OneTime, 1, AchievementCategory.Levels);
        AddAchievement("level_3_complete", "Challenge Accepted", "Complete Level 3", AchievementType.OneTime, 1, AchievementCategory.Levels);
        AddAchievement("all_levels", "World Conqueror", "Complete all main levels", AchievementType.Progressive, 3, AchievementCategory.Levels);

        AddAchievement("quick_level_1", "Speed Runner", "Complete Level 1 in under 30 seconds", AchievementType.OneTime, 1, AchievementCategory.Time);
        AddAchievement("marathon", "Marathon Runner", "Play for 30 minutes total", AchievementType.Cumulative, 1800, AchievementCategory.Time);

        AddAchievement("explorer", "Explorer", "Generate your first map from a real address", AchievementType.OneTime, 1, AchievementCategory.Exploration);
        AddAchievement("globe_trotter", "Globe Trotter", "Explore 5 different cities", AchievementType.Cumulative, 5, AchievementCategory.Exploration);
        AddAchievement("hometown_hero", "Hometown Hero", "Complete a level in your own city", AchievementType.OneTime, 1, AchievementCategory.Exploration);

        Achievement secret1 = new Achievement("secret_height", "Sky High", "Reach a height of 50 units", AchievementType.Progressive, 50, AchievementCategory.Special);
        secret1.isSecret = true;
        secret1.rarity = AchievementRarity.Rare;
        allAchievements.Add(secret1);

        Achievement legendary = new Achievement("legendary_collector", "Legendary Collector", "Collect 500 items", AchievementType.Cumulative, 500, AchievementCategory.Collectibles);
        legendary.rarity = AchievementRarity.Legendary;
        legendary.scoreReward = 1000;
        allAchievements.Add(legendary);
    }

    private void AddAchievement(string id, string title, string description, AchievementType type, int target, AchievementCategory category)
    {
        Achievement achievement = new Achievement(id, title, description, type, target, category);
        allAchievements.Add(achievement);
    }

    private void SubscribeToGameEvents()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnStatisticsUpdated += OnStatisticsUpdated;
        }

        if (LevelManager.Instance)
        {
            LevelManager.Instance.OnLevelCompleted -= OnLevelCompleted;
            LevelManager.Instance.OnCollectibleCountChanged -= OnCollectibleCountChanged;
            LevelManager.Instance.OnLevelCompleted += OnLevelCompleted;
            LevelManager.Instance.OnCollectibleCountChanged += OnCollectibleCountChanged;
        }

        cachedPlayer = FindFirstObjectByType<PlayerController>();
        if (cachedPlayer)
        {
            cachedPlayer.OnGroundedChanged += OnPlayerGrounded;
            cachedPlayer.OnFlyingChanged += OnPlayerFlying;
        }
    }

    private void UnsubscribeFromGameEvents()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            GameManager.Instance.OnStatisticsUpdated -= OnStatisticsUpdated;
        }

        if (LevelManager.Instance)
        {
            LevelManager.Instance.OnLevelCompleted -= OnLevelCompleted;
            LevelManager.Instance.OnCollectibleCountChanged -= OnCollectibleCountChanged;
        }

        if (cachedPlayer)
        {
            cachedPlayer.OnGroundedChanged -= OnPlayerGrounded;
            cachedPlayer.OnFlyingChanged -= OnPlayerFlying;
        }
    }

    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        if (newState == GameState.Playing && previousState != GameState.Paused)
        {
            UpdateAchievementProgress("first_steps", 1);
        }
    }

    private void OnStatisticsUpdated(GameStats stats)
    {
        UpdateAchievementProgress("jump_master", stats.totalJumps);
        UpdateAchievementProgress("frequent_flyer", Mathf.RoundToInt(stats.totalFlightTime));
        UpdateAchievementProgress("marathon", Mathf.RoundToInt(stats.totalPlayTime));

        if (stats.maxSpeed >= 20f) UpdateAchievementProgress("speed_demon", 1);
        if (stats.maxSpeed >= 30f) UpdateAchievementProgress("sonic_boom", 1);
        if (stats.maxHeight >= 50f) UpdateAchievementProgress("secret_height", 1);
        if (stats.totalDoubleJumps > 0) UpdateAchievementProgress("double_jump", 1);
    }

    private void OnLevelCompleted(LevelConfiguration levelConfig)
    {
        string levelName = levelConfig.levelName;
        int levelIndex = levelConfig.levelIndex;

        switch (levelIndex)
        {
            case 1:
                UpdateAchievementProgress("level_1_complete", 1);
                if (GameManager.Instance && GameManager.Instance.PlayTime < 30f)
                {
                    UpdateAchievementProgress("quick_level_1", 1);
                }
                break;
            case 2:
                UpdateAchievementProgress("level_2_complete", 1);
                break;
            case 3:
                UpdateAchievementProgress("level_3_complete", 1);
                break;
        }

        if (levelName.StartsWith("OSM:"))
        {
            UpdateAchievementProgress("explorer", 1);
            if (SaveSystem.Instance?.CurrentSave != null)
            {
                var addresses = SaveSystem.Instance.CurrentSave.addressBestTimes.Keys;
                UpdateAchievementProgress("globe_trotter", addresses.Count);
            }
        }

        UpdateAchievementProgress("level_complete", 1);

        var save = SaveSystem.Instance?.CurrentSave;
        if (save != null)
        {
            int mainLevelsCompleted = 0;
            if (save.levelCompletions.ContainsKey("Level1")) mainLevelsCompleted++;
            if (save.levelCompletions.ContainsKey("Level2")) mainLevelsCompleted++;
            if (save.levelCompletions.ContainsKey("Level3")) mainLevelsCompleted++;

            UpdateAchievementProgress("all_levels", mainLevelsCompleted);
        }
    }

    private void OnCollectibleCountChanged(int remaining, int total)
    {
        int collected = total - remaining;
        if (collected > 0)
        {
            UpdateAchievementProgress("collector", 1);
        }

        if (SaveSystem.Instance?.CurrentSave != null)
        {
            int totalCollected = SaveSystem.Instance.CurrentSave.totalCollectiblesCollected;
            UpdateAchievementProgress("collect_10", totalCollected);
            UpdateAchievementProgress("collect_50", totalCollected);
            UpdateAchievementProgress("collect_100", totalCollected);
            UpdateAchievementProgress("legendary_collector", totalCollected);
        }
    }

    private void OnPlayerGrounded(bool grounded) { }
    private void OnPlayerFlying(bool flying) { }

    public void UpdateAchievementProgress(string achievementId, int progress)
    {
        if (!achievementLookup.ContainsKey(achievementId))
        {
            LogAchievement($"Achievement not found: {achievementId}", true);
            return;
        }

        Achievement achievement = achievementLookup[achievementId];
        if (achievement.isUnlocked) return;

        bool wasCompleted = achievement.IsCompleted;

        switch (achievement.type)
        {
            case AchievementType.OneTime:
                achievement.currentProgress = progress > 0 ? 1 : 0;
                break;
            case AchievementType.Progressive:
                achievement.currentProgress = Mathf.Max(achievement.currentProgress, progress);
                break;
            case AchievementType.Cumulative:
                achievement.currentProgress = progress;
                break;
            case AchievementType.Conditional:
                achievement.currentProgress = progress;
                break;
        }

        OnAchievementProgress?.Invoke(achievement);

        if (!wasCompleted && achievement.IsCompleted)
        {
            UnlockAchievement(achievement);
        }

        SaveAchievementProgress();
    }

    public void UnlockAchievement(Achievement achievement)
    {
        if (achievement.isUnlocked) return;

        achievement.isUnlocked = true;
        achievement.unlockedDate = System.DateTime.Now;

        if (SaveSystem.Instance?.CurrentSave != null)
        {
            var save = SaveSystem.Instance.CurrentSave;
            if (!save.unlockedAchievements.Contains(achievement.id))
            {
                save.unlockedAchievements.Add(achievement.id);
                save.totalScore += achievement.scoreReward;
            }
            SaveSystem.Instance.MarkDirty();
        }

        OnAchievementUnlocked?.Invoke(achievement);

        if (enableNotifications)
        {
            ShowAchievementNotification(achievement);
        }

        if (enableSounds && achievementUnlockedSound)
        {
            AudioSource.PlayClipAtPoint(achievementUnlockedSound, Camera.main.transform.position);
        }

        LogAchievement($"Achievement unlocked: {achievement.title}");
    }

    public void UnlockAchievement(string achievementId)
    {
        if (achievementLookup.ContainsKey(achievementId))
        {
            UnlockAchievement(achievementLookup[achievementId]);
        }
    }

    private void ShowAchievementNotification(Achievement achievement)
    {
        if (!achievementNotificationPrefab || !notificationParent)
        {
            LogAchievement("Notification prefab or parent not assigned");
            return;
        }

        if (isShowingNotification)
        {
            pendingNotifications.Enqueue(achievement);
            return;
        }

        StartCoroutine(DisplayNotificationCoroutine(achievement));
    }

    private System.Collections.IEnumerator DisplayNotificationCoroutine(Achievement achievement)
    {
        isShowingNotification = true;

        GameObject notification = PrefabPooler.Get(achievementNotificationPrefab, Vector3.zero, Quaternion.identity, notificationParent);

        yield return new WaitForSeconds(notificationDuration);

        if (notification)
        {
            PrefabPooler.Release(notification);
        }

        isShowingNotification = false;

        if (pendingNotifications.Count > 0)
        {
            Achievement nextAchievement = pendingNotifications.Dequeue();
            ShowAchievementNotification(nextAchievement);
        }
    }

    private void LoadAchievementProgress()
    {
        if (SaveSystem.Instance?.CurrentSave == null) return;

        var save = SaveSystem.Instance.CurrentSave;

        foreach (string achievementId in save.unlockedAchievements)
        {
            if (achievementLookup.ContainsKey(achievementId))
            {
                Achievement achievement = achievementLookup[achievementId];
                achievement.isUnlocked = true;
                achievement.currentProgress = achievement.targetValue;
            }
        }

        foreach (var kvp in save.achievementProgress)
        {
            if (achievementLookup.ContainsKey(kvp.Key))
            {
                Achievement achievement = achievementLookup[kvp.Key];
                if (!achievement.isUnlocked)
                {
                    achievement.currentProgress = kvp.Value;
                }
            }
        }

        LogAchievement($"Loaded achievement progress - {UnlockedCount}/{TotalAchievements} unlocked");
    }

    private void SaveAchievementProgress()
    {
        if (SaveSystem.Instance?.CurrentSave == null) return;

        var save = SaveSystem.Instance.CurrentSave;
        save.achievementProgress.Clear();

        foreach (Achievement achievement in allAchievements)
        {
            if (!achievement.isUnlocked && achievement.currentProgress > 0)
            {
                save.achievementProgress[achievement.id] = achievement.currentProgress;
            }
        }

        SaveSystem.Instance.MarkDirty();
    }

    public Achievement GetAchievement(string id)
    {
        return achievementLookup.ContainsKey(id) ? achievementLookup[id] : null;
    }

    public List<Achievement> GetAchievementsByCategory(AchievementCategory category)
    {
        return allAchievements.Where(a => a.category == category).ToList();
    }

    public List<Achievement> GetAchievementsByRarity(AchievementRarity rarity)
    {
        return allAchievements.Where(a => a.rarity == rarity).ToList();
    }

    public List<Achievement> GetVisibleAchievements()
    {
        return allAchievements.Where(a => !a.isSecret && (!a.isHidden || a.isUnlocked)).ToList();
    }

    public void ResetAllAchievements()
    {
        foreach (Achievement achievement in allAchievements)
        {
            achievement.isUnlocked = false;
            achievement.currentProgress = 0;
            achievement.unlockedDate = System.DateTime.MinValue;
        }

        if (SaveSystem.Instance?.CurrentSave != null)
        {
            SaveSystem.Instance.CurrentSave.unlockedAchievements.Clear();
            SaveSystem.Instance.CurrentSave.achievementProgress.Clear();
            SaveSystem.Instance.MarkDirty();
        }

        LogAchievement("All achievements reset");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugUnlockAchievement(string achievementId)
    {
        if (achievementLookup.ContainsKey(achievementId))
        {
            UnlockAchievement(achievementLookup[achievementId]);
        }
    }

    private void LogAchievement(string message, bool isError = false)
    {
        if (!enableDebugLogging && !isError) return;

        if (isError)
            Debug.LogError($"[AchievementSystem] {message}");
        else
            Debug.Log($"[AchievementSystem] {message}");
    }

    void OnDestroy()
    {
        SaveAchievementProgress();
        UnsubscribeFromGameEvents();
    }
}
