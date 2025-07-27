using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages game progression, level unlocks, and player advancement
/// Integrates with save system and achievement system
/// </summary>
[System.Serializable]
public class LevelInfo
{
    [Header("Level Details")]
    public string levelId;
    public string levelName;
    public string displayName;
    public string sceneName;
    public string description;
    public Sprite levelIcon;
    
    [Header("Progression")]
    public int requiredLevel = 1;
    public List<string> requiredAchievements = new List<string>();
    public List<string> requiredCompletedLevels = new List<string>();
    public bool isUnlocked = false;
    public bool isCompleted = false;
    
    [Header("Difficulty")]
    public LevelDifficulty difficulty = LevelDifficulty.Easy;
    public int estimatedCompletionTime = 300; // seconds
    public int recommendedCollectibles = 10;
    
    [Header("Rewards")]
    public int baseScoreReward = 1000;
    public List<string> unlockAchievements = new List<string>();
    public List<string> unlockLevels = new List<string>();
    
    [Header("Best Records")]
    public float bestTime = float.MaxValue;
    public int bestScore = 0;
    public bool perfectCompletion = false; // All collectibles found
    
    public bool IsAvailable => isUnlocked || CheckUnlockConditions();
    
    public bool CheckUnlockConditions()
    {
        // Check save system for progress
        if (SaveSystem.Instance?.CurrentSave == null) return false;
        
        var save = SaveSystem.Instance.CurrentSave;
        
        // Check level requirement
        if (save.currentLevel < requiredLevel) return false;
        
        // Check required achievements
        foreach (string achievementId in requiredAchievements)
        {
            if (!save.unlockedAchievements.Contains(achievementId)) return false;
        }
        
        // Check required completed levels
        foreach (string levelId in requiredCompletedLevels)
        {
            if (!save.levelCompletions.ContainsKey(levelId) || !save.levelCompletions[levelId])
                return false;
        }
        
        return true;
    }
}

public enum LevelDifficulty
{
    Tutorial,
    Easy,
    Medium,
    Hard,
    Expert,
    Master
}

public enum ProgressionType
{
    Linear,      // Complete levels in order
    Branching,   // Multiple paths available
    Open         // All levels available once unlocked
}

[AddComponentMenu("Game/ProgressionManager")]
public class ProgressionManager : MonoBehaviour
{
    [Header("Progression Configuration")]
    // Fields assigned but never used - suppressing warnings
#pragma warning disable 0414
    [SerializeField] private ProgressionType progressionType = ProgressionType.Linear;
    [SerializeField] private List<LevelInfo> allLevels = new List<LevelInfo>();
    [SerializeField] private bool enableLevelSkipping = false;
    [SerializeField] private int maxSkippableLevels = 1;
    
    [Header("Experience System")]
    [SerializeField] private bool enableExperienceSystem = true;
    [SerializeField] private int baseExperiencePerLevel = 100;
    [SerializeField] private float experienceMultiplier = 1.5f;
    [SerializeField] private int maxPlayerLevel = 50;
    
    [Header("Unlock System")]
    [SerializeField] private bool showLockedLevels = true;
    [SerializeField] private bool hintNextRequirements = true;
    [SerializeField] private float unlockAnimationDuration = 1f;
#pragma warning restore 0414
    
    [Header("Debug")]
    [SerializeField] private bool unlockAllLevels = false;
    [SerializeField] private bool enableProgressionLogging = true;
    
    // Private fields
    private Dictionary<string, LevelInfo> levelLookup;
    private int currentPlayerLevel = 1;
    private int currentExperience = 0;
    private int experienceToNextLevel = 100;
    
    // Events
    public System.Action<LevelInfo> OnLevelUnlocked;
    public System.Action<LevelInfo> OnLevelCompleted;
    public System.Action<int, int> OnPlayerLevelChanged; // newLevel, oldLevel
    public System.Action<int, int, int> OnExperienceChanged; // current, toNext, total
    public System.Action<List<LevelInfo>> OnProgressionUpdated;
    
    // Singleton pattern
    public static ProgressionManager Instance { get; private set; }
    
    // Properties
    public List<LevelInfo> AllLevels => allLevels;
    public List<LevelInfo> UnlockedLevels => allLevels.Where(l => l.IsAvailable).ToList();
    public List<LevelInfo> CompletedLevels => allLevels.Where(l => l.isCompleted).ToList();
    public int CurrentPlayerLevel => currentPlayerLevel;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => experienceToNextLevel;
    public float ProgressionPercentage => allLevels.Count > 0 ? (float)CompletedLevels.Count / allLevels.Count * 100f : 0f;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeProgressionSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        LoadProgressionData();
        InitializeLevels();
        UpdateProgression();
    }
    
    private void InitializeProgressionSystem()
    {
        levelLookup = new Dictionary<string, LevelInfo>();
        
        LogProgression("Progression system initialized");
    }
    
    private void InitializeLevels()
    {
        // Create default levels if none exist
        if (allLevels.Count == 0)
        {
            CreateDefaultLevels();
        }
        
        // Build lookup dictionary
        levelLookup.Clear();
        foreach (LevelInfo level in allLevels)
        {
            if (!levelLookup.ContainsKey(level.levelId))
            {
                levelLookup[level.levelId] = level;
            }
        }
        
        // Debug unlock all levels
        if (unlockAllLevels)
        {
            foreach (LevelInfo level in allLevels)
            {
                level.isUnlocked = true;
            }
        }
        
        LogProgression($"Initialized {allLevels.Count} levels");
    }
    
    private void CreateDefaultLevels()
    {
        allLevels.Clear();
        
        // Tutorial Level
        var tutorial = new LevelInfo
        {
            levelId = "tutorial",
            levelName = "Tutorial",
            displayName = "Learning the Ropes",
            sceneName = "Level1",
            description = "Learn the basic controls and mechanics of Roll-a-Ball.",
            difficulty = LevelDifficulty.Tutorial,
            requiredLevel = 1,
            estimatedCompletionTime = 120,
            recommendedCollectibles = 5,
            baseScoreReward = 500,
            isUnlocked = true
        };
        tutorial.unlockLevels.Add("level1");
        allLevels.Add(tutorial);
        
        // Level 1 - Beginner
        var level1 = new LevelInfo
        {
            levelId = "level1",
            levelName = "Level 1",
            displayName = "First Steps",
            sceneName = "Level1",
            description = "Your first real challenge. Collect items and reach the goal!",
            difficulty = LevelDifficulty.Easy,
            requiredLevel = 1,
            estimatedCompletionTime = 180,
            recommendedCollectibles = 8,
            baseScoreReward = 1000
        };
        level1.requiredCompletedLevels.Add("tutorial");
        level1.unlockLevels.Add("level2");
        allLevels.Add(level1);
        
        // Level 2 - Intermediate
        var level2 = new LevelInfo
        {
            levelId = "level2",
            levelName = "Level 2",
            displayName = "Rising Challenge",
            sceneName = "Level2",
            description = "Things get more complex with moving platforms and obstacles.",
            difficulty = LevelDifficulty.Medium,
            requiredLevel = 2,
            estimatedCompletionTime = 240,
            recommendedCollectibles = 12,
            baseScoreReward = 1500
        };
        level2.requiredCompletedLevels.Add("level1");
        level2.unlockLevels.Add("level3");
        allLevels.Add(level2);
        
        // Level 3 - Advanced
        var level3 = new LevelInfo
        {
            levelId = "level3",
            levelName = "Level 3",
            displayName = "Master's Trial",
            sceneName = "Level3",
            description = "The ultimate test of your rolling skills with complex puzzles.",
            difficulty = LevelDifficulty.Hard,
            requiredLevel = 3,
            estimatedCompletionTime = 360,
            recommendedCollectibles = 15,
            baseScoreReward = 2000
        };
        level3.requiredCompletedLevels.Add("level2");
        level3.unlockLevels.Add("procedural_basic");
        allLevels.Add(level3);
        
        // Procedural Level - Basic
        var procedural = new LevelInfo
        {
            levelId = "procedural_basic",
            levelName = "Generated Level",
            displayName = "Endless Adventures",
            sceneName = "GeneratedLevel",
            description = "Explore procedurally generated worlds with unlimited replayability.",
            difficulty = LevelDifficulty.Medium,
            requiredLevel = 4,
            estimatedCompletionTime = 300,
            recommendedCollectibles = 10,
            baseScoreReward = 1800
        };
        procedural.requiredCompletedLevels.Add("level3");
        procedural.unlockLevels.Add("osm_explorer");
        allLevels.Add(procedural);
        
        // OSM Explorer
        var osmLevel = new LevelInfo
        {
            levelId = "osm_explorer",
            levelName = "Real World Explorer",
            displayName = "Your World, Your Game",
            sceneName = "Level_OSM",
            description = "Explore real cities and locations from around the world!",
            difficulty = LevelDifficulty.Expert,
            requiredLevel = 5,
            estimatedCompletionTime = 600,
            recommendedCollectibles = 20,
            baseScoreReward = 3000
        };
        osmLevel.requiredCompletedLevels.Add("procedural_basic");
        osmLevel.requiredAchievements.Add("level_complete");
        allLevels.Add(osmLevel);
        
        // Challenge Levels (unlock after specific achievements)
        var speedRun = new LevelInfo
        {
            levelId = "speed_challenge",
            levelName = "Speed Challenge",
            displayName = "Race Against Time",
            sceneName = "Level1", // Reuse Level1 with time pressure
            description = "Complete Level 1 in record time! Can you beat 30 seconds?",
            difficulty = LevelDifficulty.Expert,
            requiredLevel = 3,
            estimatedCompletionTime = 30,
            recommendedCollectibles = 8,
            baseScoreReward = 2500
        };
        speedRun.requiredAchievements.Add("quick_level_1");
        allLevels.Add(speedRun);
        
        // Master Challenge (unlock after completing everything)
        var masterChallenge = new LevelInfo
        {
            levelId = "master_challenge",
            levelName = "Master Challenge",
            displayName = "Ultimate Test",
            sceneName = "GeneratedLevel",
            description = "The final challenge for true masters. Are you ready?",
            difficulty = LevelDifficulty.Master,
            requiredLevel = 10,
            estimatedCompletionTime = 900,
            recommendedCollectibles = 50,
            baseScoreReward = 5000
        };
        masterChallenge.requiredCompletedLevels.AddRange(new[] { "level1", "level2", "level3", "procedural_basic", "osm_explorer" });
        masterChallenge.requiredAchievements.Add("master_collector");
        allLevels.Add(masterChallenge);
        
        LogProgression($"Created {allLevels.Count} default levels");
    }
    
    #region Progression Logic
    
    /// <summary>
    /// Update progression based on current game state
    /// </summary>
    public void UpdateProgression()
    {
        bool hasChanges = false;
        
        // Check for newly unlocked levels
        foreach (LevelInfo level in allLevels)
        {
            if (!level.isUnlocked && level.CheckUnlockConditions())
            {
                UnlockLevel(level);
                hasChanges = true;
            }
        }
        
        // Update level completion status from save data
        if (SaveSystem.Instance?.CurrentSave != null)
        {
            var save = SaveSystem.Instance.CurrentSave;
            foreach (LevelInfo level in allLevels)
            {
                bool wasCompleted = level.isCompleted;
                level.isCompleted = save.levelCompletions.ContainsKey(level.levelId) && save.levelCompletions[level.levelId];
                
                if (!wasCompleted && level.isCompleted)
                {
                    OnLevelCompleted?.Invoke(level);
                    hasChanges = true;
                }
                
                // Update best records
                if (level.levelId == "level1" && save.bestTimeLevel1 < level.bestTime)
                    level.bestTime = save.bestTimeLevel1;
                else if (level.levelId == "level2" && save.bestTimeLevel2 < level.bestTime)
                    level.bestTime = save.bestTimeLevel2;
                else if (level.levelId == "level3" && save.bestTimeLevel3 < level.bestTime)
                    level.bestTime = save.bestTimeLevel3;
            }
        }
        
        // Update experience and player level
        if (enableExperienceSystem)
        {
            UpdateExperienceSystem();
        }
        
        if (hasChanges)
        {
            OnProgressionUpdated?.Invoke(allLevels);
            SaveProgressionData();
        }
    }
    
    /// <summary>
    /// Unlock a specific level
    /// </summary>
    public void UnlockLevel(LevelInfo level)
    {
        if (level.isUnlocked) return;
        
        level.isUnlocked = true;
        
        // Unlock dependent levels
        foreach (string unlockLevelId in level.unlockLevels)
        {
            if (levelLookup.ContainsKey(unlockLevelId))
            {
                LevelInfo dependentLevel = levelLookup[unlockLevelId];
                if (dependentLevel.CheckUnlockConditions())
                {
                    UnlockLevel(dependentLevel);
                }
            }
        }
        
        // Unlock achievements
        if (AchievementSystem.Instance)
        {
            foreach (string achievementId in level.unlockAchievements)
            {
                AchievementSystem.Instance.UnlockAchievement(achievementId);
            }
        }
        
        OnLevelUnlocked?.Invoke(level);
        LogProgression($"Level unlocked: {level.displayName}");
    }
    
    /// <summary>
    /// Mark a level as completed
    /// </summary>
    public void CompleteLevel(string levelId, float completionTime, int score, bool perfectCompletion = false)
    {
        if (!levelLookup.ContainsKey(levelId)) return;
        
        LevelInfo level = levelLookup[levelId];
        
        // Update completion status
        level.isCompleted = true;
        level.perfectCompletion = perfectCompletion;
        
        // Update best records
        if (completionTime < level.bestTime)
            level.bestTime = completionTime;
        
        if (score > level.bestScore)
            level.bestScore = score;
        
        // Award experience
        if (enableExperienceSystem)
        {
            int baseExp = level.baseScoreReward / 10; // Base experience
            int timeBonus = completionTime < level.estimatedCompletionTime ? 50 : 0;
            int perfectBonus = perfectCompletion ? 100 : 0;
            
            int totalExp = baseExp + timeBonus + perfectBonus;
            AddExperience(totalExp);
        }
        
        // Update save data
        if (SaveSystem.Instance?.CurrentSave != null)
        {
            SaveSystem.Instance.CurrentSave.levelCompletions[levelId] = true;
            SaveSystem.Instance.MarkDirty();
        }
        
        // Update progression
        UpdateProgression();
        
        LogProgression($"Level completed: {level.displayName} (Time: {completionTime:F1}s, Score: {score})");
    }
    
    #endregion
    
    #region Experience System
    
    private void UpdateExperienceSystem()
    {
        if (SaveSystem.Instance?.CurrentSave == null) return;
        
        var save = SaveSystem.Instance.CurrentSave;
        
        // Calculate total experience from completed levels
        int totalExp = 0;
        foreach (LevelInfo level in allLevels)
        {
            if (level.isCompleted)
            {
                totalExp += level.baseScoreReward / 10;
            }
        }
        
        // Add experience from other sources
        totalExp += save.totalCollectiblesCollected * 2; // 2 exp per collectible
        totalExp += save.totalScore / 100; // 1 exp per 100 score points
        
        // Update player level based on total experience
        int newPlayerLevel = CalculatePlayerLevel(totalExp);
        if (newPlayerLevel != currentPlayerLevel)
        {
            int oldLevel = currentPlayerLevel;
            currentPlayerLevel = newPlayerLevel;
            OnPlayerLevelChanged?.Invoke(currentPlayerLevel, oldLevel);
            
            LogProgression($"Player level up! {oldLevel} → {currentPlayerLevel}");
        }
        
        currentExperience = totalExp;
        experienceToNextLevel = CalculateExperienceForLevel(currentPlayerLevel + 1) - totalExp;
        
        OnExperienceChanged?.Invoke(currentExperience, experienceToNextLevel, totalExp);
    }
    
    private int CalculatePlayerLevel(int totalExperience)
    {
        int level = 1;
        int expRequired = baseExperiencePerLevel;
        int totalRequired = 0;
        
        while (totalExperience >= totalRequired + expRequired && level < maxPlayerLevel)
        {
            totalRequired += expRequired;
            level++;
            expRequired = Mathf.RoundToInt(baseExperiencePerLevel * Mathf.Pow(experienceMultiplier, level - 1));
        }
        
        return level;
    }
    
    private int CalculateExperienceForLevel(int level)
    {
        int totalExp = 0;
        int expRequired = baseExperiencePerLevel;
        
        for (int i = 1; i < level; i++)
        {
            totalExp += expRequired;
            expRequired = Mathf.RoundToInt(baseExperiencePerLevel * Mathf.Pow(experienceMultiplier, i));
        }
        
        return totalExp;
    }
    
    public void AddExperience(int amount)
    {
        if (!enableExperienceSystem) return;
        
        int oldExp = currentExperience;
        currentExperience += amount;
        
        // Check for level up
        int newPlayerLevel = CalculatePlayerLevel(currentExperience);
        if (newPlayerLevel > currentPlayerLevel)
        {
            int oldLevel = currentPlayerLevel;
            currentPlayerLevel = newPlayerLevel;
            experienceToNextLevel = CalculateExperienceForLevel(currentPlayerLevel + 1) - currentExperience;
            
            OnPlayerLevelChanged?.Invoke(currentPlayerLevel, oldLevel);
            LogProgression($"Player level up! {oldLevel} → {currentPlayerLevel} (+{amount} exp)");
        }
        else
        {
            experienceToNextLevel = CalculateExperienceForLevel(currentPlayerLevel + 1) - currentExperience;
        }
        
        OnExperienceChanged?.Invoke(currentExperience, experienceToNextLevel, currentExperience);
        
        // Update save system
        if (SaveSystem.Instance?.CurrentSave != null)
        {
            SaveSystem.Instance.CurrentSave.currentLevel = currentPlayerLevel;
            SaveSystem.Instance.MarkDirty();
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Get level by ID
    /// </summary>
    public LevelInfo GetLevel(string levelId)
    {
        return levelLookup.ContainsKey(levelId) ? levelLookup[levelId] : null;
    }
    
    /// <summary>
    /// Get levels by difficulty
    /// </summary>
    public List<LevelInfo> GetLevelsByDifficulty(LevelDifficulty difficulty)
    {
        return allLevels.Where(l => l.difficulty == difficulty).ToList();
    }
    
    /// <summary>
    /// Get next recommended level
    /// </summary>
    public LevelInfo GetNextRecommendedLevel()
    {
        // Find first uncompleted, unlocked level
        var availableLevels = allLevels.Where(l => l.IsAvailable && !l.isCompleted).ToList();
        
        if (availableLevels.Count > 0)
        {
            // Sort by difficulty and estimated completion time
            return availableLevels.OrderBy(l => l.difficulty)
                                 .ThenBy(l => l.estimatedCompletionTime)
                                 .First();
        }
        
        // If all levels completed, suggest replay of favorite or hardest
        var completedLevels = allLevels.Where(l => l.isCompleted).ToList();
        if (completedLevels.Count > 0)
        {
            return completedLevels.OrderByDescending(l => l.difficulty)
                                 .ThenByDescending(l => l.baseScoreReward)
                                 .First();
        }
        
        return null;
    }
    
    /// <summary>
    /// Check if player can skip to a level
    /// </summary>
    public bool CanSkipToLevel(string levelId)
    {
        if (!enableLevelSkipping) return false;
        if (!levelLookup.ContainsKey(levelId)) return false;
        
        LevelInfo targetLevel = levelLookup[levelId];
        
        // Count completed levels before this one
        int levelIndex = allLevels.IndexOf(targetLevel);
        int completedBefore = 0;
        
        for (int i = 0; i < levelIndex; i++)
        {
            if (allLevels[i].isCompleted) completedBefore++;
        }
        
        // Allow skipping if within skip limit
        return (levelIndex - completedBefore) <= maxSkippableLevels;
    }
    
    /// <summary>
    /// Load a specific level
    /// </summary>
    public void LoadLevel(string levelId)
    {
        if (!levelLookup.ContainsKey(levelId))
        {
            LogProgression($"Level not found: {levelId}", true);
            return;
        }
        
        LevelInfo level = levelLookup[levelId];
        
        if (!level.IsAvailable && !CanSkipToLevel(levelId))
        {
            LogProgression($"Level not available: {level.displayName}", true);
            return;
        }
        
        LogProgression($"Loading level: {level.displayName}");
        
        // Load the scene
        if (!string.IsNullOrEmpty(level.sceneName))
        {
            SceneManager.LoadScene(level.sceneName);
        }
    }
    
    /// <summary>
    /// Get unlock requirements for a level
    /// </summary>
    public List<string> GetUnlockRequirements(string levelId)
    {
        List<string> requirements = new List<string>();
        
        if (!levelLookup.ContainsKey(levelId)) return requirements;
        
        LevelInfo level = levelLookup[levelId];
        
        if (level.IsAvailable) return requirements; // Already unlocked
        
        // Check level requirement
        if (SaveSystem.Instance?.CurrentSave != null)
        {
            int currentLevel = SaveSystem.Instance.CurrentSave.currentLevel;
            if (currentLevel < level.requiredLevel)
            {
                requirements.Add($"Reach player level {level.requiredLevel} (currently {currentLevel})");
            }
        }
        
        // Check required levels
        foreach (string requiredLevelId in level.requiredCompletedLevels)
        {
            if (levelLookup.ContainsKey(requiredLevelId))
            {
                LevelInfo requiredLevel = levelLookup[requiredLevelId];
                if (!requiredLevel.isCompleted)
                {
                    requirements.Add($"Complete {requiredLevel.displayName}");
                }
            }
        }
        
        // Check required achievements
        foreach (string achievementId in level.requiredAchievements)
        {
            if (AchievementSystem.Instance)
            {
                var achievement = AchievementSystem.Instance.GetAchievement(achievementId);
                if (achievement != null && !achievement.isUnlocked)
                {
                    requirements.Add($"Unlock achievement: {achievement.title}");
                }
            }
        }
        
        return requirements;
    }
    
    /// <summary>
    /// Reset all progression (for testing)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ResetProgression()
    {
        foreach (LevelInfo level in allLevels)
        {
            level.isUnlocked = level.levelId == "tutorial"; // Keep tutorial unlocked
            level.isCompleted = false;
            level.bestTime = float.MaxValue;
            level.bestScore = 0;
            level.perfectCompletion = false;
        }
        
        currentPlayerLevel = 1;
        currentExperience = 0;
        experienceToNextLevel = baseExperiencePerLevel;
        
        if (SaveSystem.Instance?.CurrentSave != null)
        {
            SaveSystem.Instance.CurrentSave.levelCompletions.Clear();
            SaveSystem.Instance.CurrentSave.currentLevel = 1;
            SaveSystem.Instance.MarkDirty();
        }
        
        UpdateProgression();
        LogProgression("Progression reset");
    }
    
    #endregion
    
    #region Save/Load
    
    private void LoadProgressionData()
    {
        // Progression data is stored in SaveSystem
        // Update our level status based on save data
        UpdateProgression();
    }
    
    private void SaveProgressionData()
    {
        // Progression is automatically saved through SaveSystem integration
        if (SaveSystem.Instance)
        {
            SaveSystem.Instance.MarkDirty();
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    private void LogProgression(string message, bool isError = false)
    {
        if (!enableProgressionLogging && !isError) return;
        
        if (isError)
            Debug.LogError($"[ProgressionManager] {message}");
        else
            Debug.Log($"[ProgressionManager] {message}");
    }
    
    /// <summary>
    /// Get progression statistics
    /// </summary>
    public ProgressionStats GetProgressionStats()
    {
        return new ProgressionStats
        {
            totalLevels = allLevels.Count,
            unlockedLevels = UnlockedLevels.Count,
            completedLevels = CompletedLevels.Count,
            perfectCompletions = allLevels.Count(l => l.perfectCompletion),
            currentPlayerLevel = currentPlayerLevel,
            totalExperience = currentExperience,
            completionPercentage = ProgressionPercentage,
            averageCompletionTime = CompletedLevels.Count > 0 ? CompletedLevels.Average(l => l.bestTime) : 0f,
            totalScore = CompletedLevels.Sum(l => l.bestScore)
        };
    }
    
    #endregion
    
    void OnDestroy()
    {
        SaveProgressionData();
    }
}

[System.Serializable]
public class ProgressionStats
{
    public int totalLevels;
    public int unlockedLevels;
    public int completedLevels;
    public int perfectCompletions;
    public int currentPlayerLevel;
    public int totalExperience;
    public float completionPercentage;
    public float averageCompletionTime;
    public int totalScore;
}
