using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// UI Controller for Roll-a-Ball with progression features
/// Supports save slots, achievements, settings, and main menu
/// </summary>
[AddComponentMenu("UI/UI Controller")]
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    public static System.Action<string, float> NotificationRequested;
    [Header("Game UI")]
    [SerializeField] private GameObject gameUIPrefab; // UI FIX
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private TextMeshProUGUI collectibleCountText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Slider flyEnergyBar;
    [SerializeField] private GameObject speedometerPanel;
    [SerializeField] private TextMeshProUGUI speedText;
    
    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI lastPlayedText;
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button pauseSettingsButton;
    [SerializeField] private Button restartLevelButton;
    
    [Header("Save/Load UI")]
    [SerializeField] private GameObject saveLoadPanel;
    [SerializeField] private Transform saveSlotContainer;
    [SerializeField] private GameObject saveSlotPrefab;
    [SerializeField] private Button saveLoadBackButton;
    [SerializeField] private TextMeshProUGUI saveLoadTitleText;
    
    [Header("Settings UI")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Toggle particleEffectsToggle;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button resetSettingsButton;
    
    [Header("Achievement UI")]
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private Transform achievementContainer;
    [SerializeField] private GameObject achievementItemPrefab;
    [SerializeField] private Button achievementBackButton;
    [SerializeField] private TextMeshProUGUI achievementProgressText;
    [SerializeField] private TMP_Dropdown achievementFilterDropdown;
    
    [Header("Notifications")]
    [SerializeField] private GameObject notificationPanelPrefab;
    [SerializeField] private Transform notificationRoot;
    [SerializeField] private float notificationDuration = 3f;
    private readonly Queue<GameObject> notificationPool = new();
    
    [Header("Level Complete")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI completionTimeText;
    [SerializeField] private TextMeshProUGUI bonusText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button levelCompleteMainMenuButton;
    
    [Header("Statistics")]
    [SerializeField] private GameObject statisticsPanel;
    [SerializeField] private TextMeshProUGUI totalPlayTimeText;
    [SerializeField] private TextMeshProUGUI totalCollectiblesText;
    [SerializeField] private TextMeshProUGUI totalJumpsText;
    [SerializeField] private TextMeshProUGUI maxSpeedText;
    [SerializeField] private TextMeshProUGUI maxHeightText;
    [SerializeField] private Button statisticsBackButton;
    
    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Image loadingIcon;
    
    // Private fields
    private UIState currentState = UIState.MainMenu;
    private Coroutine notificationCoroutine;
    private List<GameObject> saveSlotItems = new List<GameObject>();
    private List<GameObject> achievementItems = new List<GameObject>();
    private bool isTransitioning = false;
    
    // Properties
public UIState CurrentState => currentState;
public bool IsGameUIActive => gameUIPanel && gameUIPanel.activeSelf;

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        NotificationRequested += OnNotificationRequested;
    }

    void OnDisable()
    {
        NotificationRequested -= OnNotificationRequested;
    }

    private void OnNotificationRequested(string message, float duration)
    {
        ShowNotification(message, duration);
    }
    
    void Start()
    {
        EnsureCollectibleCounter();
        InitializeUI();
        SetupEventListeners();
        ShowMainMenu();
    }
    
    private void InitializeUI()
    {
        // Initialize all panels as inactive
        SetAllPanelsInactive();
        
        // Setup version info
        if (versionText)
            versionText.text = $"v{Application.version}";
        
        // Setup quality dropdown
        if (qualityDropdown)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(QualitySettings.names.ToList());
            qualityDropdown.value = QualitySettings.GetQualityLevel();
        }
        
        // Setup language dropdown
        if (languageDropdown)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "English", "Deutsch" });
        }
        
        // Setup achievement filter dropdown
        if (achievementFilterDropdown)
        {
            achievementFilterDropdown.ClearOptions();
            var categories = System.Enum.GetNames(typeof(AchievementCategory)).ToList();
            categories.Insert(0, "All");
            achievementFilterDropdown.AddOptions(categories);
        }
        
        UpdateUIFromSaveData();
    }

    /// <summary>
    /// Ensures the Canvas, GameUIPanel and collectible counter text exist and are positioned correctly.
    /// </summary>
    private void EnsureCollectibleCounter()
    {
        // UI FIX: Instantiate from prefab if no canvas exists
        var canvas = gameUIPanel ? gameUIPanel.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
        if (!canvas)
        {
            GameObject prefab = gameUIPrefab ? gameUIPrefab : Resources.Load<GameObject>("Prefabs/UI/GameUI");
            if (prefab)
            {
                GameObject instance = Instantiate(prefab);
                canvas = instance.GetComponent<Canvas>();
                gameUIPanel = instance.transform.Find("GameUIPanel")?.gameObject;
                collectibleCountText = gameUIPanel?.transform.Find("CollectibleText")?.GetComponent<TextMeshProUGUI>();
            }

            if (!canvas)
            {
                var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        // Ensure GameUIPanel exists
        if (!gameUIPanel)
        {
            var panelTransform = canvas.transform.Find("GameUIPanel");
            if (!panelTransform)
            {
                var panelObject = new GameObject("GameUIPanel");
                panelObject.transform.SetParent(canvas.transform, false);
                gameUIPanel = panelObject;
            }
            else
            {
                gameUIPanel = panelTransform.gameObject;
            }
        }

        // Ensure CollectibleText exists
        if (!collectibleCountText)
        {
            var textTransform = gameUIPanel.transform.Find("CollectibleText");
            if (textTransform)
            {
                collectibleCountText = textTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var textObject = new GameObject("CollectibleText", typeof(TextMeshProUGUI));
                textObject.transform.SetParent(gameUIPanel.transform, false);
                collectibleCountText = textObject.GetComponent<TextMeshProUGUI>();
                collectibleCountText.text = "Collectibles: 0/0";
            }
        }

        var rect = collectibleCountText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
    }
    
    private void SetupEventListeners()
    {
        // Main Menu buttons
        if (continueButton) continueButton.onClick.AddListener(ContinueGame);
        if (newGameButton) newGameButton.onClick.AddListener(NewGame);
        if (loadGameButton) loadGameButton.onClick.AddListener(() => ShowSaveLoadPanel(true));
        if (settingsButton) settingsButton.onClick.AddListener(ShowSettingsPanel);
        if (achievementsButton) achievementsButton.onClick.AddListener(ShowAchievementPanel);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
        
        // Pause Menu buttons
        if (resumeButton) resumeButton.onClick.AddListener(ResumeGame);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (pauseSettingsButton) pauseSettingsButton.onClick.AddListener(ShowSettingsPanel);
        if (restartLevelButton) restartLevelButton.onClick.AddListener(RestartLevel);
        
        // Save/Load buttons
        if (saveLoadBackButton) saveLoadBackButton.onClick.AddListener(HideSaveLoadPanel);
        
        // Settings buttons and controls
        if (settingsBackButton) settingsBackButton.onClick.AddListener(HideSettingsPanel);
        if (resetSettingsButton) resetSettingsButton.onClick.AddListener(ResetSettings);
        
        if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (languageDropdown) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        if (particleEffectsToggle) particleEffectsToggle.onValueChanged.AddListener(OnParticleEffectsChanged);
        
        // Achievement buttons
        if (achievementBackButton) achievementBackButton.onClick.AddListener(HideAchievementPanel);
        if (achievementFilterDropdown) achievementFilterDropdown.onValueChanged.AddListener(OnAchievementFilterChanged);
        
        // Level Complete buttons
        if (nextLevelButton) nextLevelButton.onClick.AddListener(LoadNextLevel);
        if (levelSelectButton) levelSelectButton.onClick.AddListener(ShowLevelSelect);
        if (levelCompleteMainMenuButton) levelCompleteMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        
        // Statistics button
        if (statisticsBackButton) statisticsBackButton.onClick.AddListener(HideStatisticsPanel);
        
        // Subscribe to system events
        // TODO: Unsubscribe in OnDestroy to avoid memory leaks
        if (SaveSystem.Instance)
        {
            SaveSystem.Instance.OnSaveLoaded += OnSaveLoaded;
            SaveSystem.Instance.OnSaveCompleted += OnSaveCompleted;
        }
        
        if (AchievementSystem.Instance)
        {
            AchievementSystem.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
        }
        
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
    }
    
    #region UI State Management
    
    private void ChangeUIState(UIState newState)
    {
        if (isTransitioning) return;
        
        currentState = newState;
        
        SetAllPanelsInactive();
        
        switch (newState)
        {
            case UIState.MainMenu:
                if (mainMenuPanel) mainMenuPanel.SetActive(true);
                UpdateMainMenuUI();
                break;
                
            case UIState.GamePlay:
                if (gameUIPanel) gameUIPanel.SetActive(true);
                break;
                
            case UIState.PauseMenu:
                if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
                break;
                
            case UIState.Settings:
                if (settingsPanel) settingsPanel.SetActive(true);
                UpdateSettingsUI();
                break;
                
            case UIState.SaveLoad:
                if (saveLoadPanel) saveLoadPanel.SetActive(true);
                UpdateSaveLoadUI();
                break;
                
            case UIState.Achievements:
                if (achievementPanel) achievementPanel.SetActive(true);
                UpdateAchievementUI();
                break;
                
            case UIState.LevelComplete:
                if (levelCompletePanel) levelCompletePanel.SetActive(true);
                break;
                
            case UIState.Loading:
                if (loadingPanel) loadingPanel.SetActive(true);
                break;
        }
    }

    private void ShowState(UIState state)
    {
        ChangeUIState(state);
    }
    
    private void SetAllPanelsInactive()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gameUIPanel) gameUIPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (saveLoadPanel) saveLoadPanel.SetActive(false);
        if (achievementPanel) achievementPanel.SetActive(false);
        if (levelCompletePanel) levelCompletePanel.SetActive(false);
        if (loadingPanel) loadingPanel.SetActive(false);
        if (statisticsPanel) statisticsPanel.SetActive(false);
    }
    
    #endregion
    
    #region Main Menu
    
    public void ShowMainMenu()
    {
        ShowState(UIState.MainMenu);
    }
    
    private void UpdateMainMenuUI()
    {
        // Update continue button availability
        if (continueButton)
        {
            bool hasSaveData = SaveSystem.Instance && SaveSystem.Instance.CurrentSave != null;
            continueButton.interactable = hasSaveData;
        }
        
        // Update last played info
        if (lastPlayedText && SaveSystem.Instance?.CurrentSave != null)
        {
            var lastSaved = SaveSystem.Instance.CurrentSave.lastSaved;
            if (lastSaved != System.DateTime.MinValue)
            {
                lastPlayedText.text = $"{RollABall.Utility.LocalizationManager.Get("LastPlayed")}: {lastSaved:yyyy-MM-dd HH:mm}";
            }
            else
            {
                lastPlayedText.text = RollABall.Utility.LocalizationManager.Get("NewGame");
            }
        }
    }
    
    private void ContinueGame()
    {
        if (SaveSystem.Instance)
        {
            SaveSystem.Instance.LoadCurrentGame();
            StartGame();
        }
    }
    
    private void NewGame()
    {
        if (SaveSystem.Instance)
        {
            // Create new save data
            SaveSystem.Instance.CurrentSave.currentLevel = 1;
            SaveSystem.Instance.CurrentSave.totalScore = 0;
            SaveSystem.Instance.CurrentSave.totalPlayTime = 0f;
            SaveSystem.Instance.MarkDirty();
        }
        
        StartGame();
    }
    
    private void StartGame()
    {
        ChangeUIState(UIState.GamePlay);
        
        // Start the game through GameManager
        if (GameManager.Instance)
        {
            GameManager.Instance.StartGame();
        }
    }
    
    private void QuitGame()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.QuitGame();
        }
    }
    
    #endregion
    
    #region Game UI
    
    public void UpdateCollectibleDisplay(int collected, int total)
    {
        if (collectibleCountText)
        {
            collectibleCountText.text = $"Collectibles: {collected}/{total}";
        }
    }
    
    public void UpdateCollectibleDisplay(int remaining)
    {
        if (LevelManager.Instance)
        {
            int total = LevelManager.Instance.TotalCollectibles;
            int collected = total - remaining;
            UpdateCollectibleDisplay(collected, total);
        }
    }
    
    public void UpdateScore(int score)
    {
        if (scoreText)
        {
            scoreText.text = $"Score: {score:N0}";
        }
    }
    
    public void UpdateLevelName(string levelName)
    {
        if (levelNameText)
        {
            levelNameText.text = levelName;
        }
    }
    
    public void UpdateTimer(float timeRemaining)
    {
        if (timerText)
        {
            if (timeRemaining > 0)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timerText.text = $"Time: {minutes:00}:{seconds:00}";
            }
            else
            {
                timerText.text = "Time: --:--";
            }
        }
    }
    
    public void UpdateFlyEnergy(float energy, float maxEnergy)
    {
        if (flyEnergyBar)
        {
            flyEnergyBar.value = energy / maxEnergy;
        }
    }
    
    public void UpdateSpeed(float speed)
    {
        if (speedText)
        {
            speedText.text = $"{speed:F1} m/s";
        }
        
        if (speedometerPanel)
        {
            // Show speedometer when moving fast
            speedometerPanel.SetActive(speed > 5f);
        }
    }
    
    #endregion
    
    #region Pause Menu
    
    public void ShowPauseMenu()
    {
        ShowState(UIState.PauseMenu);
    }
    
    public void HidePauseMenu()
    {
        ShowState(UIState.GamePlay);
    }
    
    private void ResumeGame()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.ResumeGame();
        }
        HidePauseMenu();
    }
    
    private void ReturnToMainMenu()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.LoadMainMenu();
        }
        ShowMainMenu();
    }
    
    private void RestartLevel()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.RestartGame();
        }
        HidePauseMenu();
    }
    
    #endregion
    
    #region Save/Load UI
    
    public void ShowSaveLoadPanel(bool isLoading)
    {
        if (saveLoadTitleText)
        {
            saveLoadTitleText.text = isLoading ? "Load Game" : "Save Game";
        }
        ShowState(UIState.SaveLoad);
    }

    public void HideSaveLoadPanel()
    {
        ShowState(UIState.MainMenu);
    }
    
    private void UpdateSaveLoadUI()
    {
        if (!SaveSystem.Instance || !saveSlotContainer || !saveSlotPrefab) return;
        
        // Clear existing slots
        foreach (GameObject slot in saveSlotItems)
        {
            if (slot) Destroy(slot);
        }
        saveSlotItems.Clear();
        
        // Create save slot items
        var saveSlots = SaveSystem.Instance.GetSaveSlots();
        foreach (var slotInfo in saveSlots)
        {
            GameObject slotItem = Instantiate(saveSlotPrefab, saveSlotContainer);
            saveSlotItems.Add(slotItem);
            
            // Configure slot item (this would depend on your save slot prefab structure)
            var slotUI = slotItem.GetComponent<SaveSlotUI>();
            if (slotUI)
            {
                slotUI.Setup(slotInfo, OnSaveSlotSelected);
            }
        }
    }
    
    private void OnSaveSlotSelected(int slotNumber)
    {
        if (SaveSystem.Instance)
        {
            SaveSystem.Instance.SwitchToSlot(slotNumber);
        }
        HideSaveLoadPanel();
    }
    
    #endregion
    
    #region Settings UI
    
    public void ShowSettingsPanel()
    {
        ShowState(UIState.Settings);
    }

    public void HideSettingsPanel()
    {
        ShowState(currentState == UIState.Settings ? UIState.MainMenu : UIState.PauseMenu);
    }
    
    private void UpdateSettingsUI()
    {
        if (SaveSystem.Instance?.CurrentSave == null) return;
        
        var save = SaveSystem.Instance.CurrentSave;
        
        if (masterVolumeSlider) masterVolumeSlider.value = save.masterVolume;
        if (musicVolumeSlider) musicVolumeSlider.value = save.musicVolume;
        if (sfxVolumeSlider) sfxVolumeSlider.value = save.sfxVolume;
        if (qualityDropdown) qualityDropdown.value = save.qualityLevel;
        if (particleEffectsToggle) particleEffectsToggle.isOn = save.enableParticleEffects;
        
        // Language dropdown
        if (languageDropdown)
        {
            languageDropdown.value = save.language == "de" ? 1 : 0;
        }
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        if (SaveSystem.Instance) SaveSystem.Instance.CurrentSave.masterVolume = value;
        if (AudioManager.Instance) AudioManager.Instance.SetMasterVolume(value);
        SaveSystem.Instance?.MarkDirty();
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        if (SaveSystem.Instance) SaveSystem.Instance.CurrentSave.musicVolume = value;
        if (AudioManager.Instance) AudioManager.Instance.SetMusicVolume(value);
        SaveSystem.Instance?.MarkDirty();
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        if (SaveSystem.Instance) SaveSystem.Instance.CurrentSave.sfxVolume = value;
        if (AudioManager.Instance) AudioManager.Instance.SetSFXVolume(value);
        SaveSystem.Instance?.MarkDirty();
    }
    
    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
        if (SaveSystem.Instance) SaveSystem.Instance.CurrentSave.qualityLevel = index;
        SaveSystem.Instance?.MarkDirty();
    }
    
    private void OnLanguageChanged(int index)
    {
        string language = index == 1 ? "de" : "en";
        if (SaveSystem.Instance) SaveSystem.Instance.CurrentSave.language = language;
        SaveSystem.Instance?.MarkDirty();
        
        // Apply language change (if localization system exists)
        // LocalizationManager.SetLanguage(language);
    }
    
    private void OnParticleEffectsChanged(bool enabled)
    {
        if (SaveSystem.Instance) SaveSystem.Instance.CurrentSave.enableParticleEffects = enabled;
        SaveSystem.Instance?.MarkDirty();
        
        // Apply particle effects setting
        // ParticleSystemManager.SetEnabled(enabled);
    }
    
    private void ResetSettings()
    {
        if (!SaveSystem.Instance) return;
        
        var save = SaveSystem.Instance.CurrentSave;
        save.masterVolume = 1f;
        save.musicVolume = 0.8f;
        save.sfxVolume = 1f;
        save.qualityLevel = 2;
        save.language = "en";
        save.enableParticleEffects = true;
        
        UpdateSettingsUI();
        SaveSystem.Instance.MarkDirty();
    }
    
    #endregion
    
    #region Achievement UI
    
    public void ShowAchievementPanel()
    {
        ShowState(UIState.Achievements);
    }

    public void HideAchievementPanel()
    {
        ShowState(UIState.MainMenu);
    }
    
    private void UpdateAchievementUI()
    {
        if (!AchievementSystem.Instance) return;
        
        // Update progress text
        if (achievementProgressText)
        {
            int unlocked = AchievementSystem.Instance.UnlockedCount;
            int total = AchievementSystem.Instance.TotalAchievements;
            float percentage = AchievementSystem.Instance.CompletionPercentage;
            achievementProgressText.text = $"Progress: {unlocked}/{total} ({percentage:F1}%)";
        }
        
        UpdateAchievementList();
    }
    
    private void UpdateAchievementList()
    {
        if (!achievementContainer || !achievementItemPrefab) return;
        
        // Clear existing items
        foreach (GameObject item in achievementItems)
        {
            if (item) Destroy(item);
        }
        achievementItems.Clear();
        
        // Get filtered achievements
        var achievements = GetFilteredAchievements();
        
        // Create achievement items
        foreach (var achievement in achievements)
        {
            GameObject achievementItem = Instantiate(achievementItemPrefab, achievementContainer);
            achievementItems.Add(achievementItem);
            
            // Configure achievement item
            var achievementUI = achievementItem.GetComponent<AchievementItemUI>();
            if (achievementUI)
            {
                achievementUI.Setup(achievement);
            }
        }
    }
    
    private List<Achievement> GetFilteredAchievements()
    {
        if (!AchievementSystem.Instance) return new List<Achievement>();
        
        var achievements = AchievementSystem.Instance.GetVisibleAchievements();
        
        // Apply category filter
        if (achievementFilterDropdown && achievementFilterDropdown.value > 0)
        {
            var selectedCategory = (AchievementCategory)(achievementFilterDropdown.value - 1);
            achievements = achievements.Where(a => a.category == selectedCategory).ToList();
        }
        
        // Sort by unlock status, then by rarity
        return achievements.OrderByDescending(a => a.isUnlocked)
                          .ThenBy(a => a.rarity)
                          .ThenBy(a => a.title)
                          .ToList();
    }
    
    private void OnAchievementFilterChanged(int index)
    {
        UpdateAchievementList();
    }
    
    #endregion
    
    #region Level Complete UI
    
    public void ShowLevelCompletePanel(LevelConfiguration levelConfig)
    {
        ChangeUIState(UIState.LevelComplete);
        
        if (levelCompleteText)
        {
            levelCompleteText.text = $"{levelConfig.levelName} Complete!";
        }
        
        if (finalScoreText && SaveSystem.Instance)
        {
            finalScoreText.text = $"Final Score: {SaveSystem.Instance.CurrentSave.totalScore:N0}";
        }
        
        if (completionTimeText && GameManager.Instance)
        {
            float time = GameManager.Instance.PlayTime;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            completionTimeText.text = $"Time: {minutes:00}:{seconds:00}";
        }
        
        // Calculate bonus
        if (bonusText)
        {
            int bonus = CalculateLevelBonus(levelConfig);
            bonusText.text = bonus > 0 ? $"Bonus: +{bonus:N0}" : "";
        }
        
        // Update next level button
        if (nextLevelButton)
        {
            string nextLevel = GetNextLevelName(levelConfig.levelName);
            nextLevelButton.interactable = !string.IsNullOrEmpty(nextLevel);
        }
    }
    
    private int CalculateLevelBonus(LevelConfiguration levelConfig)
    {
        int bonus = 0;
        
        // Time bonus
        if (GameManager.Instance)
        {
            float time = GameManager.Instance.PlayTime;
            if (time < 30f) bonus += 500;
            else if (time < 60f) bonus += 250;
            else if (time < 120f) bonus += 100;
        }
        
        // Perfect collection bonus
        if (levelConfig.collectiblesRemaining == 0)
        {
            bonus += 200;
        }
        
        return bonus;
    }
    
    private string GetNextLevelName(string currentLevel)
    {
        if (currentLevel.Contains("Level1")) return "Level2";
        if (currentLevel.Contains("Level2")) return "Level3";
        if (currentLevel.Contains("Level3")) return "GeneratedLevel";
        return "";
    }
    
    private void LoadNextLevel()
    {
        string nextLevel = GetNextLevelName(LevelManager.Instance?.Config?.levelName ?? "");
        if (!string.IsNullOrEmpty(nextLevel))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevel);
        }
    }
    
    private void ShowLevelSelect()
    {
        // TODO: Implement level select screen
        ReturnToMainMenu();
    }
    
    #endregion
    
    #region Notifications
    
    public void ShowNotification(string message, float duration = 0f)
    {
        if (!notificationPanelPrefab) return;

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        GameObject panel = GetNotificationInstance();
        TextMeshProUGUI text = panel.GetComponentInChildren<TextMeshProUGUI>();
        if (text) text.text = message;

        notificationCoroutine = StartCoroutine(ShowNotificationCoroutine(panel, duration > 0 ? duration : notificationDuration));
    }

    private GameObject GetNotificationInstance()
    {
        if (notificationPool.Count > 0)
            return notificationPool.Dequeue();

        return Instantiate(notificationPanelPrefab, notificationRoot ? notificationRoot : notificationPanelPrefab.transform.parent);
    }

    private IEnumerator ShowNotificationCoroutine(GameObject panel, float duration)
    {
        panel.SetActive(true);

        yield return new WaitForSeconds(duration);

        panel.SetActive(false);
        notificationPool.Enqueue(panel);
        notificationCoroutine = null;
    }
    
    #endregion
    
    #region Loading UI
    
    public void ShowLoadingScreen(string loadingMessage = "Loading...")
    {
        ChangeUIState(UIState.Loading);
        
        if (loadingText)
        {
            loadingText.text = loadingMessage;
        }
        
        if (loadingProgressBar)
        {
            loadingProgressBar.value = 0f;
        }
    }
    
    public void UpdateLoadingProgress(float progress)
    {
        if (loadingProgressBar)
        {
            loadingProgressBar.value = progress;
        }
    }
    
    public void HideLoadingScreen()
    {
        ChangeUIState(UIState.GamePlay);
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnSaveLoaded(SaveData saveData)
    {
        UpdateUIFromSaveData();
    }
    
    private void OnSaveCompleted(SaveData saveData)
    {
        ShowNotification("Game Saved!", 2f);
    }
    
    private void OnAchievementUnlocked(Achievement achievement)
    {
        ShowNotification($"Achievement Unlocked: {achievement.title}", 4f);
    }
    
    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                if (currentState != UIState.GamePlay)
                    ChangeUIState(UIState.GamePlay);
                break;
                
            case GameState.Paused:
                ShowPauseMenu();
                break;
                
            case GameState.GameOver:
                ShowMainMenu();
                break;
        }
    }
    
    private void OnStatisticsUpdated(GameStats stats)
    {
        // Update speed display
        UpdateSpeed(stats.maxSpeed);
        
        // Update statistics panel if visible
        if (statisticsPanel && statisticsPanel.activeSelf)
        {
            UpdateStatisticsDisplay(stats);
        }
    }
    
    private void OnLevelCompleted(LevelConfiguration levelConfig)
    {
        ShowLevelCompletePanel(levelConfig);
    }
    
    private void OnCollectibleCountChanged(int remaining, int total)
    {
        UpdateCollectibleDisplay(total - remaining, total);
    }
    
    #endregion
    
    #region Utility Methods
    
    private void UpdateUIFromSaveData()
    {
        if (SaveSystem.Instance?.CurrentSave == null) return;
        
        var save = SaveSystem.Instance.CurrentSave;
        
        // Update score
        UpdateScore(save.totalScore);
        
        // Update main menu
        if (currentState == UIState.MainMenu)
        {
            UpdateMainMenuUI();
        }
    }
    
    private void UpdateStatisticsDisplay(GameStats stats)
    {
        if (totalPlayTimeText)
        {
            int hours = Mathf.FloorToInt(stats.totalPlayTime / 3600f);
            int minutes = Mathf.FloorToInt((stats.totalPlayTime % 3600f) / 60f);
            totalPlayTimeText.text = $"Play Time: {hours:00}:{minutes:00}";
        }
        
        if (totalCollectiblesText && SaveSystem.Instance)
        {
            int total = SaveSystem.Instance.CurrentSave.totalCollectiblesCollected;
            totalCollectiblesText.text = $"Collectibles: {total:N0}";
        }
        
        if (totalJumpsText)
        {
            totalJumpsText.text = $"Jumps: {stats.totalJumps:N0}";
        }
        
        if (maxSpeedText)
        {
            maxSpeedText.text = $"Max Speed: {stats.maxSpeed:F1} m/s";
        }
        
        if (maxHeightText)
        {
            maxHeightText.text = $"Max Height: {stats.maxHeight:F1} m";
        }
    }
    
    private void HideStatisticsPanel()
    {
        if (statisticsPanel)
            statisticsPanel.SetActive(false);
        
        // Return to previous state (main menu or pause)
        if (Time.timeScale == 0)
            ChangeUIState(UIState.PauseMenu);
        else
            ChangeUIState(UIState.MainMenu);
    }

    // CLAUDE: FIXED - Event cleanup to prevent memory leaks (TODO-OPT#63)
    void OnDestroy()
    {
        // Stop notification coroutine if running
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
            notificationCoroutine = null;
        }
        
        // Unsubscribe from system events
        if (SaveSystem.Instance)
        {
            SaveSystem.Instance.OnSaveLoaded -= OnSaveLoaded;
            SaveSystem.Instance.OnSaveCompleted -= OnSaveCompleted;
        }
        
        if (AchievementSystem.Instance)
        {
            AchievementSystem.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        }
        
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
        
        // Clear UI item collections
        saveSlotItems?.Clear();
        achievementItems?.Clear();
    }

    #endregion
}

public enum UIState
{
    MainMenu,
    GamePlay,
    PauseMenu,
    Settings,
    SaveLoad,
    Achievements,
    LevelComplete,
    Loading
}

// Helper classes for UI components
[System.Serializable]
public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private GameObject emptySlotPanel;
    [SerializeField] private GameObject filledSlotPanel;
    
    private SaveSlotInfo slotInfo;
    private System.Action<int> onSlotSelected;
    
    public void Setup(SaveSlotInfo info, System.Action<int> callback)
    {
        slotInfo = info;
        onSlotSelected = callback;
        
        if (slotNumberText) slotNumberText.text = $"Slot {info.slotNumber + 1}";
        
        if (info.isEmpty)
        {
            if (emptySlotPanel) emptySlotPanel.SetActive(true);
            if (filledSlotPanel) filledSlotPanel.SetActive(false);
        }
        else
        {
            if (emptySlotPanel) emptySlotPanel.SetActive(false);
            if (filledSlotPanel) filledSlotPanel.SetActive(true);
            
            if (playerNameText) playerNameText.text = info.playerName;
            if (levelText) levelText.text = $"Level {info.currentLevel}";
            if (timeText) timeText.text = info.lastSaved.ToString("yyyy-MM-dd HH:mm");
        }
        
        if (selectButton) selectButton.onClick.AddListener(() => onSlotSelected?.Invoke(info.slotNumber));
        if (deleteButton) deleteButton.onClick.AddListener(DeleteSlot);
    }
    
    private void DeleteSlot()
    {
        if (SaveSystem.Instance)
        {
            SaveSystem.Instance.DeleteSaveSlot(slotInfo.slotNumber);
            // Refresh the save/load UI
            FindFirstObjectByType<UIController>()?.ShowSaveLoadPanel(true);
        }
    }
}

[System.Serializable]
public class AchievementItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject unlockedPanel;
    [SerializeField] private GameObject lockedPanel;
    [SerializeField] private Image rarityBorder;
    
    public void Setup(Achievement achievement)
    {
        if (titleText) titleText.text = achievement.title;
        if (descriptionText) descriptionText.text = achievement.description;
        
        if (achievement.isUnlocked)
        {
            if (unlockedPanel) unlockedPanel.SetActive(true);
            if (lockedPanel) lockedPanel.SetActive(false);
            if (progressText) progressText.text = "Unlocked!";
            if (progressBar) progressBar.value = 1f;
        }
        else
        {
            if (unlockedPanel) unlockedPanel.SetActive(false);
            if (lockedPanel) lockedPanel.SetActive(true);
            
            if (progressText)
            {
                if (achievement.type == AchievementType.OneTime)
                {
                    progressText.text = "Not unlocked";
                }
                else
                {
                    progressText.text = $"{achievement.currentProgress}/{achievement.targetValue}";
                }
            }
            
            if (progressBar) progressBar.value = achievement.ProgressPercentage / 100f;
        }
        
        // Set rarity border color
        if (rarityBorder)
        {
            rarityBorder.color = achievement.rarity switch
            {
                AchievementRarity.Common => Color.gray,
                AchievementRarity.Uncommon => Color.green,
                AchievementRarity.Rare => Color.blue,
                AchievementRarity.Epic => Color.magenta,
                AchievementRarity.Legendary => Color.yellow,
                _ => Color.white
            };
        }
    }
}
