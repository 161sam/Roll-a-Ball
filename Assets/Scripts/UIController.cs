using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;


[AddComponentMenu("UI/UI Controller")]
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    public static System.Action<string, float> NotificationRequested;

    [Header("Game UI")]
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

    [Header("Notifications")]
    [SerializeField] private GameObject notificationPanelPrefab;
    [SerializeField] private Transform notificationRoot;
    [SerializeField] private float notificationDuration = 3f;

    [Header("Level Complete")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button levelCompleteMainMenuButton;

    private UIState currentState = UIState.MainMenu;
    private bool isTransitioning = false;

    public UIState CurrentState => currentState;
    public bool IsGameUIActive => gameUIPanel && gameUIPanel.activeSelf;

    #region Unity Lifecycle
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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        NotificationRequested -= OnNotificationRequested;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        EnsureCollectibleCounter();
        InitializeUI();
        ConnectToLevelManager();
        ShowMainMenu();

        if (GameManager.Instance && GameManager.Instance.CurrentState == GameState.Playing)
            ChangeUIState(UIState.GamePlay);
    }
    #endregion

    #region Init & Setup
    private void InitializeUI()
    {
        SetAllPanelsInactive();

        if (versionText)
            versionText.text = $"v{Application.version}";

        UpdateUIFromSaveData();

        // Position fixieren (optional)
        if (collectibleCountText != null)
        {
            RectTransform rt = collectibleCountText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); // oben links
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -20); // 20px vom Rand
        }
    }

    private void EnsureCollectibleCounter()
    {
        if (!collectibleCountText)
            collectibleCountText = FindFirstObjectByType<TextMeshProUGUI>();

        if (collectibleCountText)
            collectibleCountText.text = "Collectibles: 0/0";
    }

    private void ConnectToLevelManager()
    {
        if (LevelManager.Instance)
        {
            LevelManager.Instance.OnLevelCompleted -= OnLevelCompleted;
            LevelManager.Instance.OnCollectibleCountChanged -= OnCollectibleCountChanged;
            LevelManager.Instance.OnLevelCompleted += OnLevelCompleted;
            LevelManager.Instance.OnCollectibleCountChanged += OnCollectibleCountChanged;

            // Direkt beim Start aktuelle Werte anzeigen
            var cfg = LevelManager.Instance.GetLevelConfiguration();
            OnCollectibleCountChanged(cfg.collectiblesRemaining, cfg.totalCollectibles);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Bei neuem Level neu verbinden
        ConnectToLevelManager();
    }
    #endregion

    #region UI State Management
    public void ShowMainMenu() => ChangeUIState(UIState.MainMenu);
    public void ShowGameUI() => ChangeUIState(UIState.GamePlay);
    public void ShowPauseMenu() => ChangeUIState(UIState.PauseMenu);

    public void ShowLevelComplete(LevelConfiguration levelConfig)
    {
        ChangeUIState(UIState.LevelComplete);
        if (levelCompleteText)
            levelCompleteText.text = $"{levelConfig.levelName} Complete!";
    }

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
            case UIState.PauseMenu:
                if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
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
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (levelCompletePanel) levelCompletePanel.SetActive(false);
    }
    #endregion

    #region Collectible & Score
    private void OnLevelCompleted(LevelConfiguration levelConfig) => ShowLevelComplete(levelConfig);

    private void OnCollectibleCountChanged(int remaining, int total) =>
        UpdateCollectibleDisplay(total - remaining, total);

    public void UpdateCollectibleDisplay(int collected, int total)
    {
        if (collectibleCountText)
            collectibleCountText.text = $"Collectibles: {collected}/{total}";
    }

    public void UpdateScore(int score)
    {
        if (scoreText)
            scoreText.text = $"Score: {score:N0}";
    }
    #endregion

    #region Notifications
    private void OnNotificationRequested(string message, float duration) =>
        ShowNotification(message, duration);

    public void ShowNotification(string message, float duration = 0f)
    {
        if (!notificationPanelPrefab || !notificationRoot) return;

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
    #endregion

    #region Save Data
    private void UpdateUIFromSaveData()
    {
        if (SaveSystem.Instance?.CurrentSave == null) return;
        UpdateScore(SaveSystem.Instance.CurrentSave.totalScore);
    }
    #endregion
}

public enum UIState
{
    MainMenu,
    GamePlay,
    PauseMenu,
    LevelComplete
}
