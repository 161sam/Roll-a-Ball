using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Universal Scene Fixture - Automatically fixes common UI and component connection issues
/// Validates and repairs scene setups for all Roll-a-Ball levels
/// </summary>
[AddComponentMenu("Roll-a-Ball/Universal Scene Fixture")]
public class UniversalSceneFixture : MonoBehaviour
{
    [Header("Auto-Fix Settings")]
    [SerializeField] private bool autoFixOnStart = true;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool createMissingComponents = true;
    
    [Header("Manual Control")]
    [SerializeField] private bool runFixNow = false;

    private void Start()
    {
        if (autoFixOnStart)
        {
            FixCurrentScene();
        }
    }

    private void OnValidate()
    {
        if (runFixNow)
        {
            runFixNow = false;
            FixCurrentScene();
        }
    }

    /// <summary>
    /// Main repair function for current scene
    /// </summary>
    public void FixCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Log($"Starting UniversalSceneFixture for scene: {sceneName}");

        // Step 1: Fix UI Controller connections
        FixUIController();
        
        // Step 2: Fix Level Manager configuration
        FixLevelManager();
        
        // Step 3: Fix Game Manager setup
        FixGameManager();
        
        // Step 4: Fix Collectibles
        FixCollectibles();
        
        // Step 5: Fix Player Controller
        FixPlayerController();
        
        // Step 6: Fix Camera Controller
        FixCameraController();
        
        // Step 7: Scene-specific fixes
        FixSceneSpecificIssues(sceneName);

        Log($"UniversalSceneFixture completed for {sceneName}");
    }

    #region UI Controller Fixes
    private void FixUIController()
    {
        Log("Fixing UIController...");
        
        UIController uiController = FindFirstObjectByType<UIController>();
        if (!uiController)
        {
            Log("No UIController found. Creating one...");
            if (createMissingComponents)
            {
                GameObject uiGO = new GameObject("UIController");
                uiController = uiGO.AddComponent<UIController>();
            }
            else
            {
                LogWarning("UIController missing - enable createMissingComponents to auto-create");
                return;
            }
        }

        // Find and connect UI elements using reflection
        var uiType = typeof(UIController);
        var fields = uiType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.GetValue(uiController) == null)
            {
                ConnectUIElement(uiController, field);
            }
        }
    }

    private void ConnectUIElement(UIController uiController, System.Reflection.FieldInfo field)
    {
        string fieldName = field.Name;
        
        // Remove common prefixes
        string searchName = fieldName.Replace("_", "").Replace("Field", "").Replace("Text", "").Replace("Button", "").Replace("Panel", "").Replace("Slider", "");
        
        GameObject found = null;
        
        // Try different naming patterns
        string[] patterns = {
            fieldName,
            searchName,
            fieldName.Replace("_", ""),
            searchName + "Text",
            searchName + "Button",
            searchName + "Panel",
            searchName + "Slider"
        };

        foreach (string pattern in patterns)
        {
            found = GameObject.Find(pattern);
            if (found) break;
        }

        if (found)
        {
            if (field.FieldType == typeof(TextMeshProUGUI))
            {
                var component = found.GetComponent<TextMeshProUGUI>();
                if (component) field.SetValue(uiController, component);
            }
            else if (field.FieldType == typeof(Button))
            {
                var component = found.GetComponent<Button>();
                if (component) field.SetValue(uiController, component);
            }
            else if (field.FieldType == typeof(Slider))
            {
                var component = found.GetComponent<Slider>();
                if (component) field.SetValue(uiController, component);
            }
            else if (field.FieldType == typeof(GameObject))
            {
                field.SetValue(uiController, found);
            }
            
            Log($"Connected {fieldName} to {found.name}");
        }
        else
        {
            // Only log warnings for critical UI elements that should exist in all scenes
            string[] criticalElements = { "collectibleText", "gameManager", "levelManager" };
            bool isCritical = false;
            
            foreach (string critical in criticalElements)
            {
                if (fieldName.ToLower().Contains(critical.ToLower()))
                {
                    isCritical = true;
                    break;
                }
            }
            
            if (isCritical)
            {
                LogWarning($"Could not find critical UI element for {fieldName}");
            }
            else if (debugMode)
            {
                Log($"Optional UI element not found: {fieldName} (this is normal for scene {SceneManager.GetActiveScene().name})");
            }
        }
    }
    #endregion

    #region Level Manager Fixes
    private void FixLevelManager()
    {
        Log("Fixing LevelManager...");
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (!levelManager)
        {
            if (createMissingComponents)
            {
                GameObject lmGO = new GameObject("LevelManager");
                levelManager = lmGO.AddComponent<LevelManager>();
                Log("Created LevelManager");
            }
            else
            {
                LogWarning("LevelManager missing");
                return;
            }
        }

        // Set up next scene progression
        string currentScene = SceneManager.GetActiveScene().name;
        string nextScene = DetermineNextScene(currentScene);
        
        if (!string.IsNullOrEmpty(nextScene))
        {
            levelManager.SetNextScene(nextScene);
            Log($"Set next scene: {nextScene}");
        }

        // Update collectible references
        levelManager.UpdateCollectibleCount();
    }

    private string DetermineNextScene(string currentScene)
    {
        switch (currentScene.ToLower())
        {
            case "level1":
            case "level_1":
                return "Level2";
            case "level2":
            case "level_2":
                return "Level3";
            case "level3":
            case "level_3":
                return "GeneratedLevel";
            case "generatedlevel":
                return "GeneratedLevel"; // Regenerate same level
            default:
                return "";
        }
    }
    #endregion

    #region Game Manager Fixes
    private void FixGameManager()
    {
        Log("Fixing GameManager...");
        
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (!gameManager)
        {
            if (createMissingComponents)
            {
                GameObject gmGO = new GameObject("GameManager");
                gameManager = gmGO.AddComponent<GameManager>();
                Log("Created GameManager");
            }
        }
    }
    #endregion

    #region Collectible Fixes
    private void FixCollectibles()
    {
        Log("Fixing Collectibles...");
        
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        
        foreach (CollectibleController collectible in collectibles)
        {
            // Ensure proper collider setup
            Collider collider = collectible.GetComponent<Collider>();
            if (!collider)
            {
                SphereCollider sphere = collectible.gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.5f;
                Log($"Added SphereCollider to {collectible.name}");
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = true;
                Log($"Set trigger on {collectible.name}");
            }

            // Ensure correct tag
            if (!collectible.CompareTag("Collectible"))
            {
                try
                {
                    collectible.tag = "Collectible";
                    Log($"Set Collectible tag on {collectible.name}");
                }
                catch
                {
                    LogWarning($"Collectible tag not found in project. Please add it manually for {collectible.name}");
                }
            }

            // Reset collected state
            if (collectible.IsCollected)
            {
                collectible.ResetCollectible();
                Log($"Reset collectible state for {collectible.name}");
            }
        }
        
        Log($"Fixed {collectibles.Length} collectibles");
    }
    #endregion

    #region Player Controller Fixes
    private void FixPlayerController()
    {
        Log("Fixing PlayerController...");
        
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (!player)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO && createMissingComponents)
            {
                player = playerGO.AddComponent<PlayerController>();
                Log("Added PlayerController to Player object");
            }
        }

        if (player)
        {
            // Ensure Rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (!rb)
            {
                rb = player.gameObject.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 0.5f;
                Log("Added Rigidbody to Player");
            }

            // Ensure Collider
            Collider collider = player.GetComponent<Collider>();
            if (!collider)
            {
                SphereCollider sphere = player.gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.5f;
                Log("Added SphereCollider to Player");
            }
        }
    }
    #endregion

    #region Camera Controller Fixes
    private void FixCameraController()
    {
        Log("Fixing CameraController...");
        
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player && cameraController.transform != player.transform)
            {
                cameraController.SetTarget(player.transform);
                Log("Connected Camera to Player");
            }
        }
    }
    #endregion

    #region Scene-Specific Fixes
    private void FixSceneSpecificIssues(string sceneName)
    {
        Log($"Applying scene-specific fixes for {sceneName}...");
        
        switch (sceneName.ToLower())
        {
            case "level_osm":
                FixOSMLevel();
                break;
            case "level2":
                FixLevel2();
                break;
            case "level3":
                FixLevel3();
                break;
            case "generatedlevel":
                FixGeneratedLevel();
                break;
        }
    }

    private void FixOSMLevel()
    {
        Log("Applying OSM-specific fixes...");
        
        // Find and setup map startup controller
        RollABall.Map.MapStartupController mapController = FindFirstObjectByType<RollABall.Map.MapStartupController>();
        if (!mapController)
        {
            GameObject mapGO = new GameObject("MapStartupController");
            mapController = mapGO.AddComponent<RollABall.Map.MapStartupController>();
            Log("Created MapStartupController");
        }

        // Connect UI elements for OSM
        ConnectOSMUIElements();
    }

    private void ConnectOSMUIElements()
    {
        // Find address input field
        TMP_InputField addressInput = Object.FindFirstObjectByType<TMP_InputField>();
        if (addressInput && addressInput.name.ToLower().Contains("address"))
        {
            // Ensure end edit event is connected
            addressInput.onEndEdit.RemoveAllListeners();
            addressInput.onEndEdit.AddListener((string input) => {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    RollABall.Map.MapStartupController controller = FindFirstObjectByType<RollABall.Map.MapStartupController>();
                    if (controller)
                    {
                        controller.LoadMapFromAddress(input);
                    }
                }
            });
            Log("Connected address input field");
        }

        // Find and connect buttons
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button.name.ToLower().Contains("load") || button.name.ToLower().Contains("map"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    TMP_InputField input = Object.FindFirstObjectByType<TMP_InputField>();
                    if (input)
                    {
                        RollABall.Map.MapStartupController controller = FindFirstObjectByType<RollABall.Map.MapStartupController>();
                        if (controller)
                        {
                            controller.LoadMapFromAddress(input.text);
                        }
                    }
                });
                Log($"Connected button: {button.name}");
            }
            else if (button.name.ToLower().Contains("current") || button.name.ToLower().Contains("location"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    RollABall.Map.MapStartupController controller = FindFirstObjectByType<RollABall.Map.MapStartupController>();
                    if (controller)
                    {
                        controller.LoadMapFromCoordinates(51.3387, 12.3799); // Leipzig
                    }
                });
                Log($"Connected location button: {button.name}");
            }
        }
    }

    private void FixLevel2()
    {
        Log("Applying Level2-specific fixes...");
        // Level2 specific fixes if needed
    }

    private void FixLevel3()
    {
        Log("Applying Level3-specific fixes...");
        // Level3 specific fixes if needed
    }

    private void FixGeneratedLevel()
    {
        Log("Applying GeneratedLevel-specific fixes...");
        
        // Ensure level generator is properly set up
        LevelGenerator levelGenerator = FindFirstObjectByType<LevelGenerator>();
        if (levelGenerator)
        {
            // Trigger regeneration if no level exists
            Transform[] children = levelGenerator.GetComponentsInChildren<Transform>();
            if (children.Length <= 1) // Only self
            {
                // Generate a new level
                var profiles = Resources.LoadAll<LevelProfile>("");
                if (profiles.Length > 0)
                {
                    levelGenerator.GenerateLevel(profiles[0]);
                    Log("Generated level with default profile");
                }
            }
        }
    }
    #endregion

    #region Utility Methods
    private void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[UniversalSceneFixture] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (debugMode)
        {
            Debug.LogWarning($"[UniversalSceneFixture] {message}");
        }
    }

    /// <summary>
    /// Public method to run the fixture manually
    /// </summary>
    [ContextMenu("Run Universal Scene Fixture")]
    public void RunFixture()
    {
        FixCurrentScene();
    }

    /// <summary>
    /// Add this component to any scene to auto-fix it
    /// </summary>
    [ContextMenu("Add to Current Scene")]
    public static void AddToCurrentScene()
    {
        if (FindFirstObjectByType<UniversalSceneFixture>() == null)
        {
            GameObject fixtureGO = new GameObject("UniversalSceneFixture");
            fixtureGO.AddComponent<UniversalSceneFixture>();
            Debug.Log("Added UniversalSceneFixture to current scene");
        }
        else
        {
            Debug.Log("UniversalSceneFixture already exists in scene");
        }
    }
    #endregion
}
