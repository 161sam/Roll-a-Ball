using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

[AddComponentMenu("UI/UI Controller")]
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    public static System.Action<string, float> NotificationRequested;

    [Header("Game UI")]
    [SerializeField] private GameObject gameUIPrefab;
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

    private UIState currentState = UIState.MainMenu;
    private Coroutine notificationCoroutine;
    private List<GameObject> saveSlotItems = new();
    private List<GameObject> achievementItems = new();
    private bool isTransitioning = false;

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

    void OnEnable() => NotificationRequested += OnNotificationRequested;
    void OnDisable() => NotificationRequested -= OnNotificationRequested;

    private void OnNotificationRequested(string message, float duration) =>
        ShowNotification(message, duration);

    void Start()
    {
        EnsureCollectibleCounter();
        InitializeUI();
        SetupEventListeners();
        ShowMainMenu();

        if (GameManager.Instance && GameManager.Instance.CurrentState == GameState.Playing)
            ChangeUIState(UIState.GamePlay);
    }

    private void InitializeUI()
    {
        SetAllPanelsInactive();
        if (versionText) versionText.text = $"v{Application.version}";

        if (qualityDropdown)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(QualitySettings.names.ToList());
            qualityDropdown.value = QualitySettings.GetQualityLevel();
        }

        if (languageDropdown)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "English", "Deutsch" });
        }

        if (achievementFilterDropdown)
        {
            achievementFilterDropdown.ClearOptions();
            var categories = System.Enum.GetNames(typeof(AchievementCategory)).ToList();
            categories.Insert(0, "All");
            achievementFilterDropdown.AddOptions(categories);
        }

        UpdateUIFromSaveData();
    }

    private void EnsureCollectibleCounter()
    {
        if (!collectibleCountText)
            collectibleCountText = FindFirstObjectByType<TextMeshProUGUI>();

        if (collectibleCountText && string.IsNullOrEmpty(collectibleCountText.text))
            collectibleCountText.text = "Collectibles: 0/0";
    }

    private void SetupEventListeners()
    {
        if (LevelManager.Instance)
        {
            LevelManager.Instance.OnLevelCompleted -= OnLevelCompleted;
            LevelManager.Instance.OnCollectibleCountChanged -= OnCollectibleCountChanged;
            LevelManager.Instance.OnLevelCompleted += OnLevelCompleted;
            LevelManager.Instance.OnCollectibleCountChanged += OnCollectibleCountChanged;
        }
    }

    public void UpdateCollectibleDisplay(int collected, int total)
    {
        if (collectibleCountText)
            collectibleCountText.text = $"Collectibles: {collected}/{total}";
    }

    public void UpdateCollectibleDisplay(int remaining)
    {
        var config = LevelManager.Instance?.GetLevelConfiguration();
        if (config != null)
        {
            int total = config.totalCollectibles;
            int collected = total - remaining;
            UpdateCollectibleDisplay(collected, total);
        }
    }

    public void ShowLevelCompletePanel(LevelConfiguration levelConfig)
    {
        ChangeUIState(UIState.LevelComplete);
        if (levelCompleteText)
            levelCompleteText.text = $"{levelConfig.levelName} Complete!";
    }

    private void LoadNextLevel()
    {
        var config = LevelManager.Instance?.GetLevelConfiguration();
        if (config != null && !string.IsNullOrEmpty(config.nextSceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(config.nextSceneName);
    }

    private void OnLevelCompleted(LevelConfiguration levelConfig) =>
        ShowLevelCompletePanel(levelConfig);

    private void OnCollectibleCountChanged(int remaining, int total) =>
        UpdateCollectibleDisplay(total - remaining, total);

    private void ChangeUIState(UIState newState)
    {
        if (isTransitioning) return;
        currentState = newState;
        SetAllPanelsInactive();

        switch (newState)
        {
            case UIState.MainMenu:
                if (mainMenuPanel) mainMenuPanel.SetActive(true);
                break;
            case UIState.GamePlay:
                if (gameUIPanel) gameUIPanel.SetActive(true);
                break;
            case UIState.LevelComplete:
                if (levelCompletePanel) levelCompletePanel.SetActive(true);
                break;
        }
    }

    private void SetAllPanelsInactive()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gameUIPanel) gameUIPanel.SetActive(false);
        if (levelCompletePanel) levelCompletePanel.SetActive(false);
    }

    public void ShowNotification(string message, float duration = 0f)
    {
        if (!notificationPanelPrefab) return;
        GameObject panel = Instantiate(notificationPanelPrefab, notificationRoot);
        TextMeshProUGUI text = panel.GetComponentInChildren<TextMeshProUGUI>();
        if (text) text.text = message;
        StartCoroutine(ShowNotificationCoroutine(panel, duration > 0 ? duration : notificationDuration));
    }

    private IEnumerator ShowNotificationCoroutine(GameObject panel, float duration)
    {
        panel.SetActive(true);
        yield return new WaitForSeconds(duration);
        Destroy(panel);
    }

    private void UpdateUIFromSaveData()
    {
        if (SaveSystem.Instance?.CurrentSave == null) return;
        UpdateScore(SaveSystem.Instance.CurrentSave.totalScore);
    }

    public void UpdateScore(int score)
    {
        if (scoreText)
            scoreText.text = $"Score: {score:N0}";
    }
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
