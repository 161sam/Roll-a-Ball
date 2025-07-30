using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Advanced save system for Roll-a-Ball progress, settings, and achievements
/// Supports multiple save slots and cloud sync preparation
/// </summary>
[System.Serializable]
public class SaveData
{
    [Header("Player Progress")]
    public int currentLevel = 1;
    public int totalScore = 0;
    public float totalPlayTime = 0f;
    public int totalCollectiblesCollected = 0;
    public List<int> completedLevels = new List<int>();
    public Dictionary<string, bool> levelCompletions = new Dictionary<string, bool>();
    
    [Header("Player Statistics")]
    public float bestTimeLevel1 = float.MaxValue;
    public float bestTimeLevel2 = float.MaxValue;
    public float bestTimeLevel3 = float.MaxValue;
    public int totalJumps = 0;
    public int totalDoubleJumps = 0;
    public float totalDistanceTraveled = 0f;
    public float totalFlightTime = 0f;
    public float maxHeight = 0f;
    public float maxSpeed = 0f;
    
    [Header("Achievements")]
    public List<string> unlockedAchievements = new List<string>();
    public Dictionary<string, int> achievementProgress = new Dictionary<string, int>();
    
    [Header("Settings")]
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 1f;
    public bool enableParticleEffects = true;
    public int qualityLevel = 2;
    public string language = "en";
    
    [Header("OSM Data")]
    public List<string> favoriteAddresses = new List<string>();
    public Dictionary<string, float> addressBestTimes = new Dictionary<string, float>();
    public string lastUsedAddress = "";
    
    [Header("Save Info")]
    public System.DateTime lastSaved = System.DateTime.Now;
    public string saveVersion = "1.0";
    public string playerName = "Player";
    public int saveSlot = 0;
    
    public SaveData()
    {
        // Initialize default values
        completedLevels = new List<int>();
        levelCompletions = new Dictionary<string, bool>();
        unlockedAchievements = new List<string>();
        achievementProgress = new Dictionary<string, int>();
        favoriteAddresses = new List<string>();
        addressBestTimes = new Dictionary<string, float>();
        
        // Add some default favorite addresses
        favoriteAddresses.Add("Leipzig, Markt");
        favoriteAddresses.Add("Berlin, Brandenburger Tor");
        favoriteAddresses.Add("München, Marienplatz");
    }
}

[AddComponentMenu("Game/SaveSystem")]
public class SaveSystem : MonoBehaviour
{
    [Header("Save Configuration")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 60f; // seconds
    [SerializeField] private int maxSaveSlots = 5;
    [SerializeField] private bool enableCloudSync = false;
    
    [Header("Encryption")]
    [SerializeField] private bool encryptSaveFiles = true;
    [SerializeField] private string encryptionKey = "RollABallGame2025";
    // TODO: Move encryption key to external configuration for better security
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private bool simulateCloudSync = false;
    
    // Private fields
    private SaveData currentSave;
    private string savePath;
    private int activeSaveSlot = 0;
    private float lastAutoSave = 0f;
    private bool isDirty = false; // True when save data needs saving
    
    // Events
    public System.Action<SaveData> OnSaveLoaded;
    public System.Action<SaveData> OnSaveCompleted;
    public System.Action<string> OnSaveError;
    public System.Action<int> OnSaveSlotChanged;
    
    // Singleton pattern
    public static SaveSystem Instance { get; private set; }
    
    // Properties
    public SaveData CurrentSave => currentSave;
    public int ActiveSaveSlot => activeSaveSlot;
    public bool HasUnsavedChanges => isDirty;
    public string SavePath => savePath;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        LoadLastUsedSave();
    }
    
    void Update()
    {
        // Auto-save functionality
        if (autoSave && isDirty && Time.time - lastAutoSave > autoSaveInterval)
        {
            SaveCurrentGame();
            lastAutoSave = Time.time;
        }
    }
    
    private void InitializeSaveSystem()
    {
        // Determine save path based on platform
        #if UNITY_ANDROID && !UNITY_EDITOR
            savePath = Path.Combine(Application.persistentDataPath, "Saves");
        #elif UNITY_IOS && !UNITY_EDITOR
            savePath = Path.Combine(Application.persistentDataPath, "Saves");
        #elif UNITY_WEBGL && !UNITY_EDITOR
            savePath = Path.Combine(Application.persistentDataPath, "Saves");
        #else
            savePath = Path.Combine(Application.persistentDataPath, "RollABall", "Saves");
        #endif
        
        // Create save directory if it doesn't exist
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            LogSave($"Created save directory: {savePath}");
        }
        
        // Initialize with empty save data
        currentSave = new SaveData();
        
        LogSave("Save system initialized successfully");
    }
    
    #region Save/Load Operations
    
    /// <summary>
    /// Save current game state to the active slot
    /// </summary>
    public void SaveCurrentGame()
    {
        if (currentSave == null)
        {
            LogSave("No save data to save!", true);
            return;
        }
        
        SaveToSlot(activeSaveSlot);
    }
    
    /// <summary>
    /// Save current game state to a specific slot
    /// </summary>
    public void SaveToSlot(int slot)
    {
        if (slot < 0 || slot >= maxSaveSlots)
        {
            LogSave($"Invalid save slot: {slot}", true);
            OnSaveError?.Invoke($"Invalid save slot: {slot}");
            return;
        }
        
        try
        {
            // Update save metadata
            currentSave.lastSaved = System.DateTime.Now;
            currentSave.saveSlot = slot;
            
            // Collect current game state
            UpdateSaveDataFromGameState();
            
            // Generate file path
            string fileName = $"save_slot_{slot:D2}.dat";
            string filePath = Path.Combine(savePath, fileName);
            
            // Save the data
            if (encryptSaveFiles)
            {
                SaveEncrypted(filePath, currentSave);
            }
            else
            {
                SaveUnencrypted(filePath, currentSave);
            }
            
            isDirty = false;
            OnSaveCompleted?.Invoke(currentSave);
            
            LogSave($"Game saved to slot {slot} successfully");
            
            // Cloud sync if enabled
            if (enableCloudSync)
            {
                StartCoroutine(SyncToCloud(currentSave));
            }
        }
        catch (System.Exception e)
        {
            LogSave($"Failed to save game: {e.Message}", true);
            OnSaveError?.Invoke($"Failed to save: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load game state from the active slot
    /// </summary>
    public void LoadCurrentGame()
    {
        LoadFromSlot(activeSaveSlot);
    }
    
    /// <summary>
    /// Load game state from a specific slot
    /// </summary>
    public void LoadFromSlot(int slot)
    {
        if (slot < 0 || slot >= maxSaveSlots)
        {
            LogSave($"Invalid save slot: {slot}", true);
            OnSaveError?.Invoke($"Invalid save slot: {slot}");
            return;
        }
        
        try
        {
            string fileName = $"save_slot_{slot:D2}.dat";
            string filePath = Path.Combine(savePath, fileName);
            
            if (!File.Exists(filePath))
            {
                LogSave($"No save file found in slot {slot}, creating new save");
                currentSave = new SaveData();
                currentSave.saveSlot = slot;
                activeSaveSlot = slot;
                isDirty = true;
                return;
            }
            
            // Load the data
            SaveData loadedSave;
            if (encryptSaveFiles)
            {
                loadedSave = LoadEncrypted(filePath);
            }
            else
            {
                loadedSave = LoadUnencrypted(filePath);
            }
            
            if (loadedSave != null)
            {
                currentSave = loadedSave;
                activeSaveSlot = slot;
                
                // Apply loaded data to game state
                ApplySaveDataToGameState();
                
                OnSaveLoaded?.Invoke(currentSave);
                OnSaveSlotChanged?.Invoke(slot);
                
                LogSave($"Game loaded from slot {slot} successfully");
            }
            else
            {
                throw new System.Exception("Failed to deserialize save data");
            }
        }
        catch (System.Exception e)
        {
            LogSave($"Failed to load game: {e.Message}", true);
            OnSaveError?.Invoke($"Failed to load: {e.Message}");
            
            // Create fallback save
            currentSave = new SaveData();
            currentSave.saveSlot = slot;
            activeSaveSlot = slot;
            isDirty = true;
        }
    }
    
    /// <summary>
    /// Load the last used save slot
    /// </summary>
    private void LoadLastUsedSave()
    {
        // Try to find the most recent save file
        int mostRecentSlot = 0;
        System.DateTime mostRecentTime = System.DateTime.MinValue;
        
        for (int i = 0; i < maxSaveSlots; i++)
        {
            string fileName = $"save_slot_{i:D2}.dat";
            string filePath = Path.Combine(savePath, fileName);
            
            if (File.Exists(filePath))
            {
                var lastWriteTime = File.GetLastWriteTime(filePath);
                if (lastWriteTime > mostRecentTime)
                {
                    mostRecentTime = lastWriteTime;
                    mostRecentSlot = i;
                }
            }
        }
        
        LoadFromSlot(mostRecentSlot);
    }
    
    #endregion
    
    #region File Operations
    
    private void SaveUnencrypted(string filePath, SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        // TODO: Use async file IO to prevent frame spikes on save
    }
    
    private SaveData LoadUnencrypted(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<SaveData>(json);
    }
    
    private void SaveEncrypted(string filePath, SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string encrypted = EncryptString(json, encryptionKey);
        File.WriteAllText(filePath, encrypted);
        // TODO: Use async file IO to prevent frame spikes on save
    }
    
    private SaveData LoadEncrypted(string filePath)
    {
        string encrypted = File.ReadAllText(filePath);
        string json = DecryptString(encrypted, encryptionKey);
        return JsonUtility.FromJson<SaveData>(json);
    }
    
    // Simple XOR encryption (for basic save protection)
    private string EncryptString(string plainText, string key)
    {
        System.Text.StringBuilder encrypted = new System.Text.StringBuilder();
        for (int i = 0; i < plainText.Length; i++)
        {
            encrypted.Append((char)(plainText[i] ^ key[i % key.Length]));
        }
        return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(encrypted.ToString()));
    }
    
    private string DecryptString(string encryptedText, string key)
    {
        byte[] bytes = System.Convert.FromBase64String(encryptedText);
        string encrypted = System.Text.Encoding.UTF8.GetString(bytes);
        
        System.Text.StringBuilder decrypted = new System.Text.StringBuilder();
        for (int i = 0; i < encrypted.Length; i++)
        {
            decrypted.Append((char)(encrypted[i] ^ key[i % key.Length]));
        }
        return decrypted.ToString();
    }
    
    #endregion
    
    #region Game State Integration
    
    /// <summary>
    /// Update save data with current game state
    /// </summary>
    private void UpdateSaveDataFromGameState()
    {
        // Update from GameManager
        if (GameManager.Instance)
        {
            var stats = GameManager.Instance.Stats;
            currentSave.totalPlayTime += stats.totalPlayTime;
            currentSave.totalJumps += stats.totalJumps;
            currentSave.totalDoubleJumps += stats.totalDoubleJumps;
            currentSave.totalDistanceTraveled += stats.totalDistanceTraveled;
            currentSave.totalFlightTime += stats.totalFlightTime;
            
            if (stats.maxHeight > currentSave.maxHeight)
                currentSave.maxHeight = stats.maxHeight;
            
            if (stats.maxSpeed > currentSave.maxSpeed)
                currentSave.maxSpeed = stats.maxSpeed;
        }
        
        // Update from LevelManager
        if (LevelManager.Instance)
        {
            var config = LevelManager.Instance.Config;
            string levelName = config.levelName;
            
            // Track level completion
            if (LevelManager.Instance.IsLevelCompleted)
            {
                if (!currentSave.levelCompletions.ContainsKey(levelName))
                {
                    currentSave.levelCompletions[levelName] = true;
                }
                
                // Track best times for numbered levels
                if (levelName.Contains("Level"))
                {
                    float levelTime = GameManager.Instance.PlayTime;
                    if (levelName.Contains("1") && levelTime < currentSave.bestTimeLevel1)
                        currentSave.bestTimeLevel1 = levelTime;
                    else if (levelName.Contains("2") && levelTime < currentSave.bestTimeLevel2)
                        currentSave.bestTimeLevel2 = levelTime;
                    else if (levelName.Contains("3") && levelTime < currentSave.bestTimeLevel3)
                        currentSave.bestTimeLevel3 = levelTime;
                }
                
                // Track OSM address times
                if (levelName.StartsWith("OSM:"))
                {
                    string address = levelName.Substring(5);
                    float levelTime = GameManager.Instance.PlayTime;
                    
                    if (!currentSave.addressBestTimes.ContainsKey(address) || 
                        levelTime < currentSave.addressBestTimes[address])
                    {
                        currentSave.addressBestTimes[address] = levelTime;
                    }
                    
                    currentSave.lastUsedAddress = address;
                }
            }
            
            // Update collectibles
            int collectedThisLevel = config.totalCollectibles - config.collectiblesRemaining;
            currentSave.totalCollectiblesCollected += collectedThisLevel;
        }
        
        // Update current level (simple progression)
        if (currentSave.levelCompletions.ContainsKey("Level1") && currentSave.currentLevel < 2)
            currentSave.currentLevel = 2;
        if (currentSave.levelCompletions.ContainsKey("Level2") && currentSave.currentLevel < 3)
            currentSave.currentLevel = 3;
        if (currentSave.levelCompletions.ContainsKey("Level3") && currentSave.currentLevel < 4)
            currentSave.currentLevel = 4; // OSM levels unlocked
        
        // Mark as dirty
        isDirty = true;
    }
    
    /// <summary>
    /// Apply save data to current game state
    /// </summary>
    private void ApplySaveDataToGameState()
    {
        // Apply audio settings
        if (AudioManager.Instance)
        {
            AudioManager.Instance.SetMasterVolume(currentSave.masterVolume);
            AudioManager.Instance.SetMusicVolume(currentSave.musicVolume);
            AudioManager.Instance.SetSFXVolume(currentSave.sfxVolume);
        }
        
        // Apply quality settings
        QualitySettings.SetQualityLevel(currentSave.qualityLevel);
        
        // Apply language settings (if localization system exists)
        // LocalizationManager.SetLanguage(currentSave.language);
        
        LogSave($"Applied save data to game state");
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Mark current save as dirty (needs saving)
    /// </summary>
    public void MarkDirty()
    {
        isDirty = true;
    }
    
    /// <summary>
    /// Get available save slots with metadata
    /// </summary>
    public List<SaveSlotInfo> GetSaveSlots()
    {
        List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
        
        for (int i = 0; i < maxSaveSlots; i++)
        {
            string fileName = $"save_slot_{i:D2}.dat";
            string filePath = Path.Combine(savePath, fileName);
            
            SaveSlotInfo slotInfo = new SaveSlotInfo
            {
                slotNumber = i,
                isEmpty = !File.Exists(filePath),
                isCurrentSlot = (i == activeSaveSlot)
            };
            
            if (!slotInfo.isEmpty)
            {
                try
                {
                    SaveData slotData = encryptSaveFiles ? LoadEncrypted(filePath) : LoadUnencrypted(filePath);
                    slotInfo.playerName = slotData.playerName;
                    slotInfo.currentLevel = slotData.currentLevel;
                    slotInfo.totalScore = slotData.totalScore;
                    slotInfo.playTime = slotData.totalPlayTime;
                    slotInfo.lastSaved = slotData.lastSaved;
                    slotInfo.completedLevels = slotData.levelCompletions.Count;
                }
                catch (System.Exception e)
                {
                    LogSave($"Failed to read slot {i} metadata: {e.Message}");
                    slotInfo.isCorrupted = true;
                }
            }
            
            slots.Add(slotInfo);
        }
        
        return slots;
    }
    
    /// <summary>
    /// Switch to a different save slot
    /// </summary>
    public void SwitchToSlot(int slot)
    {
        if (slot == activeSaveSlot) return;
        
        // Save current progress if dirty
        if (isDirty)
        {
            SaveCurrentGame();
        }
        
        // Load new slot
        LoadFromSlot(slot);
    }
    
    /// <summary>
    /// Delete a save slot
    /// </summary>
    public void DeleteSaveSlot(int slot)
    {
        if (slot < 0 || slot >= maxSaveSlots)
        {
            LogSave($"Invalid save slot for deletion: {slot}", true);
            return;
        }
        
        try
        {
            string fileName = $"save_slot_{slot:D2}.dat";
            string filePath = Path.Combine(savePath, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                LogSave($"Deleted save slot {slot}");
                
                // If we deleted the current slot, create a new save
                if (slot == activeSaveSlot)
                {
                    currentSave = new SaveData();
                    currentSave.saveSlot = slot;
                    isDirty = true;
                }
            }
        }
        catch (System.Exception e)
        {
            LogSave($"Failed to delete save slot {slot}: {e.Message}", true);
            OnSaveError?.Invoke($"Failed to delete save: {e.Message}");
        }
    }
    
    /// <summary>
    /// Export save data as JSON for backup
    /// </summary>
    public string ExportSaveData()
    {
        return JsonUtility.ToJson(currentSave, true);
    }
    
    /// <summary>
    /// Import save data from JSON backup
    /// </summary>
    public bool ImportSaveData(string jsonData)
    {
        try
        {
            SaveData importedSave = JsonUtility.FromJson<SaveData>(jsonData);
            if (importedSave != null)
            {
                currentSave = importedSave;
                currentSave.saveSlot = activeSaveSlot;
                isDirty = true;
                ApplySaveDataToGameState();
                LogSave("Save data imported successfully");
                return true;
            }
        }
        catch (System.Exception e)
        {
            LogSave($"Failed to import save data: {e.Message}", true);
            OnSaveError?.Invoke($"Import failed: {e.Message}");
        }
        
        return false;
    }
    
    #endregion
    
    #region Cloud Sync (Placeholder)
    
    private System.Collections.IEnumerator SyncToCloud(SaveData saveData)
    {
        if (!enableCloudSync && !simulateCloudSync) yield break;
        
        LogSave("Starting cloud sync...");
        
        // Simulate cloud upload delay
        yield return new UnityEngine.WaitForSeconds(1f);
        
        if (simulateCloudSync)
        {
            LogSave("Cloud sync completed (simulated)");
        }
        else
        {
            // TODO: Implement actual cloud sync
            // This could integrate with:
            // - Steam Cloud
            // - Google Play Games
            // - Apple Game Center
            // - Custom cloud service
            LogSave("Cloud sync not yet implemented");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    private void LogSave(string message, bool isError = false)
    {
        if (!enableDebugLogging && !isError) return;
        
        if (isError)
            Debug.LogError($"[SaveSystem] {message}");
        else
            Debug.Log($"[SaveSystem] {message}");
    }
    
    /// <summary>
    /// Calculate total game completion percentage
    /// </summary>
    public float GetCompletionPercentage()
    {
        float completed = 0f;
        float total = 0f;
        
        // Level completion (40% of total)
        total += 40f;
        if (currentSave.levelCompletions.ContainsKey("Level1")) completed += 10f;
        if (currentSave.levelCompletions.ContainsKey("Level2")) completed += 15f;
        if (currentSave.levelCompletions.ContainsKey("Level3")) completed += 15f;
        
        // Collectibles (30% of total)
        total += 30f;
        float collectibleRatio = Mathf.Min(currentSave.totalCollectiblesCollected / 100f, 1f);
        completed += collectibleRatio * 30f;
        
        // Achievements (20% of total)
        total += 20f;
        float achievementRatio = Mathf.Min(currentSave.unlockedAchievements.Count / 10f, 1f);
        completed += achievementRatio * 20f;
        
        // OSM exploration (10% of total)
        total += 10f;
        float osmRatio = Mathf.Min(currentSave.addressBestTimes.Count / 5f, 1f);
        completed += osmRatio * 10f;
        
        return (completed / total) * 100f;
    }
    
    #endregion
    
    void OnApplicationPause(bool pauseStatus)
    {
        // Save on pause (mobile platforms)
        if (pauseStatus && isDirty)
        {
            SaveCurrentGame();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        // Save when losing focus
        if (!hasFocus && isDirty)
        {
            SaveCurrentGame();
        }
    }
    
    void OnDestroy()
    {
        // Final save before destruction
        if (isDirty && Instance == this)
        {
            SaveCurrentGame();
        }
    }
}

[System.Serializable]
public class SaveSlotInfo
{
    public int slotNumber;
    public bool isEmpty;
    public bool isCurrentSlot;
    public bool isCorrupted;
    public string playerName;
    public int currentLevel;
    public int totalScore;
    public float playTime;
    public System.DateTime lastSaved;
    public int completedLevels;
}
