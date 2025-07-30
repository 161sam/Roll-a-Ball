using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using RollABall.InputSystem;

[System.Serializable]
public class GameStats
{
    public float totalPlayTime;
    public int totalJumps;
    public int totalDoubleJumps;
    public float totalDistanceTraveled;
    public float totalFlightTime;
    public float maxHeight;
    public float maxSpeed;
}

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver,
    Loading
}

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private float gameTimeScale = 1f;
    [SerializeField, HideInInspector] private KeyCode pauseKey = KeyCode.Escape; // moved to InputManager
    [SerializeField, HideInInspector] private KeyCode restartKey = KeyCode.R;

    [Header("Player Reference")]
    [SerializeField] private PlayerController player;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("UI References")]
    [SerializeField] private UIController uiController;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject gameOverScreen;

    [Header("Scene Management")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameScene = "Game";
    [SerializeField] private float sceneTransitionDelay = 1f;

    [Header("Statistics")]
    [SerializeField] private bool trackStatistics = true;
    [SerializeField] private GameStats gameStats;

    [Header("Checkpoints")]
    [SerializeField] private Transform[] checkpoints;
    [SerializeField] private float checkpointRadius = 2f;
    [SerializeField] private LayerMask checkpointLayer = 1;

    [Header("Game Rules")]
    [SerializeField] private float fallDeathHeight = -50f;
    [SerializeField] private bool enableRespawn = true;
    [SerializeField] private float respawnDelay = 2f;

    // Private fields
    private GameState currentState = GameState.Playing;
    private GameState previousState;
    private float gameStartTime;
    private Vector3 lastPlayerPosition;
    private int currentCheckpointIndex = 0;
    private bool isPaused = false;
    private Coroutine statisticsCoroutine;

    // Events
    public System.Action<GameState, GameState> OnGameStateChanged;
    public System.Action<GameStats> OnStatisticsUpdated;
    public System.Action<int> OnCheckpointReached;

    // Singleton pattern
    public static GameManager Instance { get; private set; }

    // Properties
    public GameState CurrentState => currentState;
    public GameStats Stats => gameStats;
    public bool IsPlaying => currentState == GameState.Playing;
    public bool IsPaused => isPaused;
    public float PlayTime => Time.time - gameStartTime;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        HandleInput();
        
        if (currentState == GameState.Playing)
        {
            CheckPlayerFall();
            CheckCheckpoints();
        }
    }

    private void InitializeGameManager()
    {
        gameStats = new GameStats();
        gameStartTime = Time.time;
        
        // Set initial time scale
        Time.timeScale = gameTimeScale;

        // Ensure an InputManager exists and apply configured keys
        if (InputManager.Instance == null)
        {
            var obj = new GameObject("InputManager");
            obj.AddComponent<InputManager>();
        }
        if (InputManager.Instance != null)
        {
            // migrate legacy key bindings if they were set in the scene
            var mgr = InputManager.Instance;
            var pauseField = pauseKey;
            var restartField = restartKey;
            typeof(InputManager).GetField("pauseKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(mgr, pauseField);
            typeof(InputManager).GetField("restartKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(mgr, restartField);
        }
        
        // CLAUDE: FIXED - Use cached references instead of Find calls
        if (autoFindPlayer && !player)
        {
            // Only search if explicitly enabled and no reference set
            player = FindFirstObjectByType<PlayerController>();
            if (player && debugMode)
                Debug.Log("[GameManager] Auto-found PlayerController");
        }
        
        if (!uiController)
        {
            // Prefer inspector assignment over runtime search
            uiController = FindFirstObjectByType<UIController>();
            if (uiController && debugMode)
                Debug.Log("[GameManager] Auto-found UIController");
        }
    }

    public void StartGame()
    {
        ChangeGameState(GameState.Playing);
        
        if (player)
        {
            lastPlayerPosition = player.transform.position;
            SubscribeToPlayerEvents();
        }
        
        if (trackStatistics)
            StartStatisticsTracking();
    }

    private void SubscribeToPlayerEvents()
    {
        if (!player) return;

        player.OnGroundedChanged += OnPlayerGrounded;
        player.OnFlyingChanged += OnPlayerFlying;
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (!player) return;

        player.OnGroundedChanged -= OnPlayerGrounded;
        player.OnFlyingChanged -= OnPlayerFlying;
    }

    private void HandleInput()
    {
        if (InputManager.Instance && InputManager.Instance.PausePressed)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        if (InputManager.Instance && InputManager.Instance.RestartPressed && (debugMode || currentState == GameState.GameOver))
        {
            RestartGame();
        }
    }

    private void CheckPlayerFall()
    {
        if (!player) return;

        if (player.transform.position.y < fallDeathHeight)
        {
            if (enableRespawn)
                StartCoroutine(RespawnPlayer());
            else
                GameOver();
        }
    }

    private void CheckCheckpoints()
    {
        if (!player || checkpoints == null || checkpoints.Length == 0) return;

        for (int i = currentCheckpointIndex; i < checkpoints.Length; i++)
        {
            if (!checkpoints[i]) continue;

            float distance = Vector3.Distance(player.transform.position, checkpoints[i].position);
            if (distance <= checkpointRadius)
            {
                ReachCheckpoint(i);
                break;
            }
        }
    }

    private void ReachCheckpoint(int index)
    {
        if (index <= currentCheckpointIndex) return;

        currentCheckpointIndex = index;
        OnCheckpointReached?.Invoke(index);

        if (uiController)
            uiController.ShowNotification($"Checkpoint {index + 1} erreicht!", 2f);

        if (AudioManager.Instance)
            AudioManager.Instance.PlaySound("Checkpoint");
    }

    // ===== Game State Management =====

    public void ChangeGameState(GameState newState)
    {
        if (newState == currentState) return;

        previousState = currentState;
        currentState = newState;

        OnGameStateChanged?.Invoke(previousState, currentState);

        switch (currentState)
        {
            case GameState.Playing:
                Time.timeScale = gameTimeScale;
                if (pauseMenu) pauseMenu.SetActive(false);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                if (pauseMenu) pauseMenu.SetActive(true);
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                if (gameOverScreen) gameOverScreen.SetActive(true);
                break;

            case GameState.Loading:
                Time.timeScale = 1f;
                break;
        }
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        isPaused = true;
        ChangeGameState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        isPaused = false;
        ChangeGameState(GameState.Playing);
    }

    public void GameOver()
    {
        ChangeGameState(GameState.GameOver);
        
        if (statisticsCoroutine != null)
        {
            StopCoroutine(statisticsCoroutine);
            statisticsCoroutine = null;
        }

        FinalizeStatistics();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        // Use gameScene if specified, otherwise reload current scene
        string sceneToLoad = !string.IsNullOrEmpty(gameScene) ? gameScene : SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneToLoad);
    }

    /// <summary>
    /// Reset game state without reloading scene (for map regeneration)
    /// </summary>
    public void ResetGame()
    {
        // Reset game statistics
        gameStats = new GameStats();
        gameStartTime = Time.time;
        
        // Reset checkpoints
        currentCheckpointIndex = 0;
        
        // Reset player position if available
        if (player)
        {
            lastPlayerPosition = player.transform.position;
            
            // Reset player velocity
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            PhysicsUtils.ResetMotion(playerRb);
        }
        
        // Reset UI if available
        if (uiController)
        {
            uiController.UpdateCollectibleDisplay(0);
        }
        
        // Change to playing state
        ChangeGameState(GameState.Playing);
        
        Debug.Log("[GameManager] Game reset completed");
    }

    /// <summary>
    /// Update collectible count in UI
    /// </summary>
    public void UpdateCollectibleCount()
    {
        if (uiController)
        {
            int collected = GetCollectedCount();
            uiController.UpdateCollectibleDisplay(collected);
        }
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadSceneAsync(mainMenuScene));
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // ===== Respawn System =====

    private IEnumerator RespawnPlayer()
    {
        if (!player) yield break;

        ChangeGameState(GameState.Loading);

        // Disable player temporarily
        player.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // Respawn at last checkpoint
        Vector3 respawnPosition = checkpoints != null && checkpoints.Length > 0 && currentCheckpointIndex < checkpoints.Length
            ? checkpoints[currentCheckpointIndex].position
            : Vector3.zero;

        player.transform.position = respawnPosition + Vector3.up * 2f;
        
        // Reset player velocity via helper
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        PhysicsUtils.ResetMotion(playerRb);

        // Re-enable player
        player.enabled = true;
        
        ChangeGameState(GameState.Playing);

        if (uiController)
            uiController.ShowNotification("Respawned!", 1.5f);
    }

    // ===== Statistics System =====

    private void StartStatisticsTracking()
    {
        if (statisticsCoroutine != null)
            StopCoroutine(statisticsCoroutine);
        
        statisticsCoroutine = StartCoroutine(TrackStatistics());
    }

    private IEnumerator TrackStatistics()
    {
        while (currentState == GameState.Playing)
        {
            UpdateStatistics();
            yield return new WaitForSeconds(0.1f); // TODO: Expose update interval in inspector
        }
    }

    private void UpdateStatistics()
    {
        if (!player || !trackStatistics) return;

        // Update play time
        gameStats.totalPlayTime = Time.time - gameStartTime;

        // Update distance traveled
        Vector3 currentPosition = player.transform.position;
        float distanceThisFrame = Vector3.Distance(currentPosition, lastPlayerPosition);
        gameStats.totalDistanceTraveled += distanceThisFrame;
        lastPlayerPosition = currentPosition;

        // Update max height
        if (currentPosition.y > gameStats.maxHeight)
            gameStats.maxHeight = currentPosition.y;

        // Update max speed
        float currentSpeed = player.Velocity.magnitude;
        if (currentSpeed > gameStats.maxSpeed)
            gameStats.maxSpeed = currentSpeed;

        // Update flight time
        if (player.IsFlying)
            gameStats.totalFlightTime += 0.1f;

        OnStatisticsUpdated?.Invoke(gameStats);
    }

    private void FinalizeStatistics()
    {
        gameStats.totalPlayTime = Time.time - gameStartTime;
        OnStatisticsUpdated?.Invoke(gameStats);
        
        if (debugMode)
            PrintStatistics();
    }

    private void PrintStatistics()
    {
        Debug.Log("=== GAME STATISTICS ===");
        Debug.Log($"Total Play Time: {gameStats.totalPlayTime:F2}s");
        Debug.Log($"Total Jumps: {gameStats.totalJumps}");
        Debug.Log($"Total Double Jumps: {gameStats.totalDoubleJumps}");
        Debug.Log($"Distance Traveled: {gameStats.totalDistanceTraveled:F2}m");
        Debug.Log($"Flight Time: {gameStats.totalFlightTime:F2}s");
        Debug.Log($"Max Height: {gameStats.maxHeight:F2}m");
        Debug.Log($"Max Speed: {gameStats.maxSpeed:F2}m/s");
    }

    // ===== Event Handlers =====

    private void OnPlayerGrounded(bool grounded)
    {
        if (!grounded && trackStatistics)
        {
            // Player jumped
            gameStats.totalJumps++;
        }
    }

    private void OnPlayerFlying(bool flying)
    {
        // Flight time is tracked in UpdateStatistics
    }

    // ===== Scene Management =====

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        ChangeGameState(GameState.Loading);
        
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    // ===== Collectibles & Level Management (Delegated to LevelManager) =====

    public int GetCollectedCount()
    {
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.TotalCollectibles - LevelManager.Instance.CollectiblesRemaining;
        }
        return 0;
    }

    public bool IsLevelComplete()
    {
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.IsLevelCompleted;
        }
        return false;
    }

    public int GetRemainingCollectibles()
    {
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.CollectiblesRemaining;
        }
        return 0;
    }

    public int GetTotalCollectibles()
    {
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.TotalCollectibles;
        }
        return 0;
    }

    // ===== Public Utility Methods =====

    public void AddCheckpoint(Transform checkpoint)
    {
        List<Transform> checkpointList = new List<Transform>(checkpoints ?? new Transform[0]);
        checkpointList.Add(checkpoint);
        checkpoints = checkpointList.ToArray();
    }

    public void SetTimeScale(float scale)
    {
        gameTimeScale = Mathf.Clamp(scale, 0.1f, 2f);
        if (currentState == GameState.Playing)
            Time.timeScale = gameTimeScale;
    }

    public Vector3 GetCurrentCheckpointPosition()
    {
        if (checkpoints == null || checkpoints.Length == 0 || currentCheckpointIndex >= checkpoints.Length)
            return Vector3.zero;
        
        return checkpoints[currentCheckpointIndex].position;
    }

    // ===== Debug =====

    void OnDrawGizmosSelected()
    {
        if (checkpoints == null) return;

        // Draw checkpoints
        Gizmos.color = Color.green;
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (!checkpoints[i]) continue;

            Gizmos.DrawWireSphere(checkpoints[i].position, checkpointRadius);
            
            // Number labels would require custom editor drawing
            if (i == currentCheckpointIndex)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(checkpoints[i].position, checkpointRadius * 1.2f);
                Gizmos.color = Color.green;
            }
        }

        // Draw fall death line
        Gizmos.color = Color.red;
        Vector3 center = transform.position;
        Gizmos.DrawLine(center + Vector3.left * 100 + Vector3.up * fallDeathHeight, 
                       center + Vector3.right * 100 + Vector3.up * fallDeathHeight);
    }

    void OnDestroy()
    {
        UnsubscribeFromPlayerEvents();
        
        if (statisticsCoroutine != null)
            StopCoroutine(statisticsCoroutine);
    }
}