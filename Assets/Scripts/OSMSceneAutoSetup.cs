using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically sets up OSM scene with required components when loaded
/// Ensures LevelManager and other essential components are present
/// </summary>
[AddComponentMenu("Roll-a-Ball/OSM Auto Setup")]
public class OSMSceneAutoSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool debugMode = true;
    
    [Header("Required Components")]
    [SerializeField] private GameObject levelManagerPrefab;
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject uiControllerPrefab;
    
    void Start()
    {
        if (runOnStart)
        {
            SetupOSMScene();
        }
    }
    
    [ContextMenu("Setup OSM Scene")]
    public void SetupOSMScene()
    {
        Debug.Log("[OSMAutoSetup] Setting up OSM scene...");
        
        // Ensure LevelManager exists
        EnsureLevelManager();
        
        // Ensure GameManager exists (should be persistent from previous scenes)
        EnsureGameManager();
        
        // Ensure UIController exists
        EnsureUIController();
        
        // Configure scene for OSM mode
        ConfigureSceneForOSM();
        
        if (debugMode)
        {
            Debug.Log("[OSMAutoSetup] OSM scene setup complete");
        }
    }
    
    private void EnsureLevelManager()
    {
        LevelManager existing = FindFirstObjectByType<LevelManager>();
        if (existing != null)
        {
            if (debugMode)
                Debug.Log("[OSMAutoSetup] LevelManager already exists");
            return;
        }
        
        GameObject levelManagerGO;
        
        // Try to instantiate from prefab first
        if (levelManagerPrefab != null)
        {
            levelManagerGO = Instantiate(levelManagerPrefab);
            if (debugMode)
                Debug.Log("[OSMAutoSetup] Created LevelManager from prefab");
        }
        else
        {
            // Create minimal LevelManager manually
            levelManagerGO = new GameObject("LevelManager");
            LevelManager levelManager = levelManagerGO.AddComponent<LevelManager>();
            
            // Configure for OSM level
            ConfigureLevelManagerForOSM(levelManager);
            
            if (debugMode)
                Debug.Log("[OSMAutoSetup] Created LevelManager manually");
        }
        
        levelManagerGO.name = "LevelManager";
    }
    
    private void ConfigureLevelManagerForOSM(LevelManager levelManager)
    {
        // Use reflection to set private fields if needed
        // For now, the public configuration should be sufficient
        
        if (debugMode)
            Debug.Log("[OSMAutoSetup] Configured LevelManager for OSM");
    }
    
    private void EnsureGameManager()
    {
        GameManager existing = GameManager.Instance;
        if (existing != null)
        {
            if (debugMode)
                Debug.Log("[OSMAutoSetup] GameManager already exists (persistent)");
            return;
        }
        
        // GameManager should normally be persistent from previous scenes
        // If it doesn't exist, create one
        GameObject gameManagerGO;
        
        if (gameManagerPrefab != null)
        {
            gameManagerGO = Instantiate(gameManagerPrefab);
        }
        else
        {
            gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
        }
        
        gameManagerGO.name = "GameManager";
        
        if (debugMode)
            Debug.Log("[OSMAutoSetup] Created GameManager for OSM scene");
    }
    
    private void EnsureUIController()
    {
        UIController existing = FindFirstObjectByType<UIController>();
        if (existing != null)
        {
            if (debugMode)
                Debug.Log("[OSMAutoSetup] UIController already exists");
            return;
        }
        
        // Look for existing Canvas first
        Canvas canvas = FindFirstObjectByType<Canvas>();
        
        GameObject uiControllerGO;
        
        if (uiControllerPrefab != null)
        {
            uiControllerGO = Instantiate(uiControllerPrefab);
        }
        else if (canvas != null)
        {
            // Add UIController to existing Canvas
            uiControllerGO = canvas.gameObject;
            if (uiControllerGO.GetComponent<UIController>() == null)
            {
                uiControllerGO.AddComponent<UIController>();
            }
        }
        else
        {
            // Create minimal UI setup
            uiControllerGO = new GameObject("UIController");
            uiControllerGO.AddComponent<UIController>();
        }
        
        if (debugMode)
            Debug.Log("[OSMAutoSetup] Ensured UIController exists");
    }
    
    private void ConfigureSceneForOSM()
    {
        // Set scene-specific configurations for OSM mode
        
        // Ensure proper lighting for generated content
        if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Skybox)
        {
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.5f);
        }
        
        // Set fog for atmosphere
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.6f, 0.6f, 0.7f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.01f;
        
        if (debugMode)
            Debug.Log("[OSMAutoSetup] Configured scene settings for OSM");
    }
    
    /// <summary>
    /// Check if OSM scene has all required components
    /// </summary>
    public bool ValidateOSMScene()
    {
        bool valid = true;
        
        if (FindFirstObjectByType<LevelManager>() == null)
        {
            Debug.LogError("[OSMAutoSetup] Missing LevelManager in OSM scene");
            valid = false;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[OSMAutoSetup] Missing GameManager in OSM scene");
            valid = false;
        }
        
        if (FindFirstObjectByType<RollABall.Map.MapStartupController>() == null)
        {
            Debug.LogError("[OSMAutoSetup] Missing MapStartupController in OSM scene");
            valid = false;
        }
        
        if (FindFirstObjectByType<RollABall.Map.MapGenerator>() == null)
        {
            Debug.LogError("[OSMAutoSetup] Missing MapGenerator in OSM scene");
            valid = false;
        }
        
        return valid;
    }
    
    /// <summary>
    /// Reset endless mode (for debugging)
    /// </summary>
    [ContextMenu("Reset Endless Mode")]
    public void ResetEndlessMode()
    {
        PlayerPrefs.DeleteKey("AutoGenerateOSMMode");
        PlayerPrefs.DeleteKey("OSMLocationIndex");
        PlayerPrefs.Save();
        
        Debug.Log("[OSMAutoSetup] Reset endless mode PlayerPrefs");
    }
    
    /// <summary>
    /// Get current endless mode status
    /// </summary>
    public string GetEndlessModeStatus()
    {
        bool isEndlessMode = PlayerPrefs.GetInt("AutoGenerateOSMMode", 0) == 1;
        int locationIndex = PlayerPrefs.GetInt("OSMLocationIndex", 0);
        
        return $"Endless Mode: {(isEndlessMode ? "Active" : "Inactive")}, Location Index: {locationIndex}";
    }
    
    void OnValidate()
    {
        // Ensure this script runs early
        if (GetComponent<Transform>() != null)
        {
            transform.SetAsFirstSibling();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw scene setup status
        Gizmos.color = ValidateOSMScene() ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        
        // Draw endless mode indicator
        if (PlayerPrefs.GetInt("AutoGenerateOSMMode", 0) == 1)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position + Vector3.up * 3f, 0.5f);
        }
    }
}
