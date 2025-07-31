using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
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
    public bool isHidden = false; // Hidden until unlocked
    public bool isSecret = false; // Completely hidden until unlocked
    
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
    OneTime,           // Unlocked once when condition is met
    Progressive,       // Progress towards a target value
    Cumulative,        // Accumulate over multiple sessions
    Conditional        // Complex conditions (custom logic)
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
    
    // Private fields
    private Dictionary<string, Achievement> achievementLookup;
    private Queue<Achievement> pendingNotifications;
    private bool isShowingNotification = false;
    private PlayerController cachedPlayer;
    
    // Events
    public System.Action<Achievement> OnAchievementUnlocked;
    public System.Action<Achievement> OnAchievementProgress;
    public System.Action<List<Achievement>> OnAchievementsLoaded;
    
    // Singleton pattern
    public static AchievementSystem Instance { get; private set; }
    
    // Properties
    public List<Achievement> AllAchievements => allAchievements;
    public List<Achievement> UnlockedAchievements => allAchievements.Where(a => a.isUnlocked).ToList();
    public List<Achievement> LockedAchievements => allAchievements.Where(a => !a.isUnlocked).ToList();
    public int TotalAchievements => allAchievements.Count;
    public int UnlockedCount => allAchievements.Count(a => a.isUnlocked);
    public float CompletionPercentage => TotalAchievements > 0 ? (float)UnlockedCount / TotalAchievements * 100f : 0f;
    
    void Awake()
    {
        // Singleton setup
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
        
        // Load achievements from external database if assigned
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
        
        // Build lookup dictionary
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
        // Fallback definitions when no external database is assigned
        allAchievements.Clear();
        
        // General Achievements
        AddAchievement("first_steps", "First Steps", "Start your first Roll-a-Ball adventure!", 
                      AchievementType.OneTime, 1, AchievementCategory.General);
        
        AddAchievement("collector", "Collector", "Collect your first item!", 
                      AchievementType.OneTime, 1, AchievementCategory.Collectibles);
        
        AddAchievement("level_complete", "Level Master", "Complete your first level!", 
                      AchievementType.OneTime, 1, AchievementCategory.Levels);
        
        // Collectible Achievements
        AddAchievement("collect_10", "Gathering Steam", "Collect 10 items", 
                      AchievementType.Cumulative, 10, AchievementCategory.Collectibles);
        
        AddAchievement("collect_50", "Item Hunter", "Collect 50 items", 
                      AchievementType.Cumulative, 50, AchievementCategory.Collectibles);
        
        AddAchievement("collect_100", "Master Collector", "Collect 100 items", 
                      AchievementType.Cumulative, 100, AchievementCategory.Collectibles);
        
        // Movement Achievements
        AddAchievement("jump_master", "Jump Master", "Perform 100 jumps", 
                      AchievementType.Cumulative, 100, AchievementCategory.Movement);
        
        AddAchievement("double_jump", "Air Walker", "Perform your first double jump!", 
                      AchievementType.OneTime, 1, AchievementCategory.Movement);
        
        AddAchievement("frequent_flyer", "Frequent Flyer", "Spend 60 seconds in the air total", 
                      AchievementType.Cumulative, 60, AchievementCategory.Movement);
        
        // Speed Achievements
        AddAchievement("speed_demon", "Speed Demon", "Reach a speed of 20 units/second", 
                      AchievementType.Progressive, 20, AchievementCategory.Speed);
        
        AddAchievement("sonic_boom", "Sonic Boom", "Reach a speed of 30 units/second", 
                      AchievementType.Progressive, 30, AchievementCategory.Speed);
        
        // Level Achievements
        AddAchievement("level_1_complete", "Beginner's Luck", "Complete Level 1", 
                      AchievementType.OneTime, 1, AchievementCategory.Levels);
        
        AddAchievement("level_2_complete", "Getting Warmed Up", "Complete Level 2", 
                      AchievementType.OneTime, 1, AchievementCategory.Levels);
        
        AddAchievement("level_3_complete", "Challenge Accepted", "Complete Level 3", 
                      AchievementType.OneTime, 1, AchievementCategory.Levels);
        
        AddAchievement("all_levels", "World Conqueror", "Complete all main levels", 
                      AchievementType.Progressive, 3, AchievementCategory.Levels);
        
        // Time Achievements
        AddAchievement("quick_level_1", "Speed Runner", "Complete Level 1 in under 30 seconds", 
                      AchievementType.OneTime, 1, AchievementCategory.Time);
        
        AddAchievement("marathon", "Marathon Runner", "Play for 30 minutes total", 
                      AchievementType.Cumulative, 1800, AchievementCategory.Time); // 30 minutes in seconds
        
        // Exploration Achievements
        AddAchievement("explorer", "Explorer", "Generate your first map from a real address", 
                      AchievementType.OneTime, 1, AchievementCategory.Exploration);
        
        AddAchievement("globe_trotter", "Globe Trotter", "Explore 5 different cities", 
                      AchievementType.Cumulative, 5, AchievementCategory.Exploration);
        
        AddAchievement("hometown_hero", "Hometown Hero", "Complete a level in your own city", 
                      AchievementType.OneTime, 1, AchievementCategory.Exploration);
        
        // Special/Secret Achievements
        Achievement secret1 = new Achievement("secret_height", "Sky High", "Reach a height of 50 units", 
                                            AchievementType.Progressive, 50, AchievementCategory.Special);
        secret1.isSecret = true;
        secret1.rarity = AchievementRarity.Rare;
        allAchievements.Add(secret1);
        
        Achievement legendary = new Achievement("legendary_collector", "Legendary Collector", "Collect 500 items", 
                                              AchievementType.Cumulative, 500, AchievementCategory.Collectibles);
        legendary.rarity = AchievementRarity.Legendary;
        legendary.scoreReward = 1000;
        allAchievements.Add(legendary);
    }
    
    private void AddAchievement(string id, string title, string description, 
                               AchievementType type, int target, AchievementCategory category)
    {
        Achievement achievement = new Achievement(id, title, description, type, target, category);
        allAchievements.Add(achievement);
    }
    
    #region Game Event Subscription
    
    private void SubscribeToGameEvents()
    {
        // Subscribe to game events for automatic progress tracking
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnStatisticsUpdated += OnStatisticsUpdated;
        }
        
        if (LevelManager.Instance)
        {
            LevelManager.Instance.OnLevelCompleted += OnLevelCompleted;
            LevelManager.Instance.OnCollectibleCountChanged += OnCollectibleCountChanged;
        }
        
        // Subscribe to player events
        // TODO: Inject PlayerController dependency instead of using FindFirstObjectByType
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
            // Game started
            UpdateAchievementProgress("first_steps", 1);
        }
    }
    
    private void OnStatisticsUpdated(GameStats stats)
    {
        // Update cumulative achievements based on statistics
        UpdateAchievementProgress("jump_master", stats.totalJumps);
        UpdateAchievementProgress("frequent_flyer", Mathf.RoundToInt(stats.totalFlightTime));
        UpdateAchievementProgress("marathon", Mathf.RoundToInt(stats.totalPlayTime));
        
        // Check speed achievements
        if (stats.maxSpeed >= 20f) UpdateAchievementProgress("speed_demon", 1);
        if (stats.maxSpeed >= 30f) UpdateAchievementProgress("sonic_boom", 1);
        
        // Check height achievements
        if (stats.maxHeight >= 50f) UpdateAchievementProgress("secret_height", 1);
        
        // Check double jump
        if (stats.totalDoubleJumps > 0) UpdateAchievementProgress("double_jump", 1);
    }
    
    private void OnLevelCompleted(LevelConfiguration levelConfig)
    {
        string levelName = levelConfig.levelName;

        int levelIndex = levelConfig.levelIndex;
        var profile = LevelManager.Instance ? LevelManager.Instance.ProgressionProfile : null;
        if (profile)
        {
            var entry = profile.GetLevelEntry(levelName);
            if (entry != null) levelIndex = entry.levelIndex;
        }

        // Level-specific achievements via level index
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
            
            // Track unique cities
            string address = levelName.Substring(5);
            if (SaveSystem.Instance?.CurrentSave != null)
            {
                var addresses = SaveSystem.Instance.CurrentSave.addressBestTimes.Keys;
                UpdateAchievementProgress("globe_trotter", addresses.Count);
            }
        }
        
        // General level completion
        UpdateAchievementProgress("level_complete", 1);
        
        // Check if all main levels completed
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
        
        // Update cumulative collection count
        if (SaveSystem.Instance?.CurrentSave != null)
        {
            int totalCollected = SaveSystem.Instance.CurrentSave.totalCollectiblesCollected;
            UpdateAchievementProgress("collect_10", totalCollected);
            UpdateAchievementProgress("collect_50", totalCollected);
            UpdateAchievementProgress("collect_100", totalCollected);
            UpdateAchievementProgress("legendary_collector", totalCollected);
        }
    }
    
    private void OnPlayerGrounded(bool grounded)
    {
        // Player landed (could be used for landing achievements)
    }
    
    private void OnPlayerFlying(bool flying)
    {
        // Player started flying (could be used for flight achievements)
    }
    
    #endregion
    
    #region Achievement Progress
    
    /// <summary>
    /// Update progress for a specific achievement
    /// </summary>
    public void UpdateAchievementProgress(string achievementId, int progress)
    {
        if (!achievementLookup.ContainsKey(achievementId))
        {
            LogAchievement($"Achievement not found: {achievementId}", true);
            return;
        }
        
        Achievement achievement = achievementLookup[achievementId];
        
        if (achievement.isUnlocked) return; // Already unlocked
        
        bool wasCompleted = achievement.IsCompleted;
        
        // Update progress based on type
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
                // Custom logic handled elsewhere
                achievement.currentProgress = progress;
                break;
        }
        
        // Fire progress event
        OnAchievementProgress?.Invoke(achievement);
        
        // Check for unlock
        if (!wasCompleted && achievement.IsCompleted)
        {
            UnlockAchievement(achievement);
        }
        
        // Save progress
        SaveAchievementProgress();
    }
    
    /// <summary>
    /// Unlock an achievement
    /// </summary>
    public void UnlockAchievement(Achievement achievement)
    {
        if (achievement.isUnlocked) return;
        
        achievement.isUnlocked = true;
        achievement.unlockedDate = System.DateTime.Now;
        
        // Add to save data
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
        
        // Fire unlock event
        OnAchievementUnlocked?.Invoke(achievement);
        
        // Show notification
        if (enableNotifications)
        {
            ShowAchievementNotification(achievement);
        }
        
        // Play sound
        if (enableSounds && achievementUnlockedSound)
        {
            AudioSource.PlayClipAtPoint(achievementUnlockedSound, Camera.main.transform.position);
        }
        
        LogAchievement($"Achievement unlocked: {achievement.title}");
    }
    
    /// <summary>
    /// Unlock achievement by ID
    /// </summary>
    public void UnlockAchievement(string achievementId)
    {
        if (achievementLookup.ContainsKey(achievementId))
        {
            UnlockAchievement(achievementLookup[achievementId]);
        }
    }
    
    #endregion
    
    #region Notifications
    
    private void ShowAchievementNotification(Achievement achievement)
    {
        if (!achievementNotificationPrefab || !notificationParent)
        {
            LogAchievement("Notification prefab or parent not assigned");
            return;
        }
        
        // Queue notification if one is already showing
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
        
        // Get notification UI from pool
        GameObject notification = PrefabPooler.Get(achievementNotificationPrefab, Vector3.zero, Quaternion.identity, notificationParent);
        
        // Configure notification (this would depend on your notification prefab structure)
        // TODO: Implement AchievementNotificationUI to display icon and text
        // notification.GetComponent<AchievementNotificationUI>().Setup(achievement);
        
        // Wait for notification duration
        yield return new WaitForSeconds(notificationDuration);
        
        // Return notification to pool
        if (notification)
        {
            PrefabPooler.Release(notification);
        }
        
        isShowingNotification = false;
        
        // Show next queued notification
        if (pendingNotifications.Count > 0)
        {
            Achievement nextAchievement = pendingNotifications.Dequeue();
            ShowAchievementNotification(nextAchievement);
        }
    }
    
    #endregion
    
    #region Save/Load
    
    private void LoadAchievementProgress()
    {
        if (SaveSystem.Instance?.CurrentSave == null) return;
        
        var save = SaveSystem.Instance.CurrentSave;
        
        // Load unlocked achievements
        foreach (string achievementId in save.unlockedAchievements)
        {
            if (achievementLookup.ContainsKey(achievementId))
            {
                Achievement achievement = achievementLookup[achievementId];
                achievement.isUnlocked = true;
                achievement.currentProgress = achievement.targetValue;
            }
        }
        
        // Load progress for achievements in progress
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

        // TODO: Implement cross-device cloud sync of achievements
        
        var save = SaveSystem.Instance.CurrentSave;
        save.achievementProgress.Clear();
        
        // Save progress for achievements in progress
        foreach (Achievement achievement in allAchievements)
        {
            if (!achievement.isUnlocked && achievement.currentProgress > 0)
            {
                save.achievementProgress[achievement.id] = achievement.currentProgress;
            }
        }
        
        SaveSystem.Instance.MarkDirty();
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Get achievement by ID
    /// </summary>
    public Achievement GetAchievement(string id)
    {
        return achievementLookup.ContainsKey(id) ? achievementLookup[id] : null;
    }
    
    /// <summary>
    /// Get achievements by category
    /// </summary>
    public List<Achievement> GetAchievementsByCategory(AchievementCategory category)
    {
        return allAchievements.Where(a => a.category == category).ToList();
    }
    
    /// <summary>
    /// Get achievements by rarity
    /// </summary>
    public List<Achievement> GetAchievementsByRarity(AchievementRarity rarity)
    {
        return allAchievements.Where(a => a.rarity == rarity).ToList();
    }
    
    /// <summary>
    /// Get visible achievements (not secret or hidden until unlocked)
    /// </summary>
    public List<Achievement> GetVisibleAchievements()
    {
        return allAchievements.Where(a => !a.isSecret && (!a.isHidden || a.isUnlocked)).ToList();
    }
    
    /// <summary>
    /// Reset all achievement progress
    /// </summary>
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
    
    /// <summary>
    /// Force unlock achievement (for testing)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugUnlockAchievement(string achievementId)
    {
        if (achievementLookup.ContainsKey(achievementId))
        {
            UnlockAchievement(achievementLookup[achievementId]);
        }
    }
    
    #endregion
    
    #region Utility
    
    private void LogAchievement(string message, bool isError = false)
    {
        if (!enableDebugLogging && !isError) return;
        
        if (isError)
            Debug.LogError($"[AchievementSystem] {message}");
        else
            Debug.Log($"[AchievementSystem] {message}");
        // TODO: Replace Debug.Log calls with a dedicated logging service
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Save progress before destruction
        SaveAchievementProgress();
        UnsubscribeFromGameEvents();
    }
}
