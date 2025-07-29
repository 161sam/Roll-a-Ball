using UnityEngine;
using UnityEngine.SceneManagement;
using RollABall.Environment;

/// <summary>
/// Emergency Scene Builder - Erstellt funktionierende Minimal-Versionen aller Szenen
/// Falls automatische Reparatur nicht möglich ist
/// </summary>
[AddComponentMenu("Roll-a-Ball/Emergency Scene Builder")]
public class EmergencySceneBuilder : MonoBehaviour
{
    [Header("Emergency Repair Settings")]
    [SerializeField] private bool executeOnStart = false;
    [SerializeField] private bool debugMode = true;

    [Header("Manual Controls")]
    [SerializeField] private bool buildGeneratedLevel = false;
    [SerializeField] private bool buildLevel1 = false;
    [SerializeField] private bool buildAllScenes = false;

    private void Start()
    {
        if (executeOnStart)
        {
            BuildCurrentScene();
        }
    }

    private void OnValidate()
    {
        if (buildGeneratedLevel)
        {
            buildGeneratedLevel = false;
            BuildMinimalGeneratedLevel();
        }

        if (buildLevel1)
        {
            buildLevel1 = false;
            BuildMinimalLevel1();
        }

        if (buildAllScenes)
        {
            buildAllScenes = false;
            BuildAllMinimalScenes();
        }
    }

    public void BuildCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Log($"Building minimal version of: {sceneName}");

        switch (sceneName)
        {
            case "GeneratedLevel":
                BuildMinimalGeneratedLevel();
                break;
            case "Level1":
                BuildMinimalLevel1();
                break;
            case "Level2":
                BuildMinimalLevel2();
                break;
            case "Level3":
                BuildMinimalLevel3();
                break;
            case "Level_OSM":
                BuildMinimalOSMLevel();
                break;
            case "MiniGame":
                BuildMinimalMiniGame();
                break;
            default:
                LogWarning($"Unknown scene: {sceneName}");
                break;
        }
    }

    public void BuildAllMinimalScenes()
    {
        Log("Building all minimal scenes...");

        // Backup current scene
        string currentScene = SceneManager.GetActiveScene().name;
        // TODO: Restore the originally active scene after building completes

        try
        {
            BuildMinimalGeneratedLevel();
            BuildMinimalLevel1();
            BuildMinimalLevel2();
            BuildMinimalLevel3();
            BuildMinimalOSMLevel();
            BuildMinimalMiniGame();
            
            Log("All minimal scenes built successfully!");
        }
        catch (System.Exception e)
        {
            LogError($"Error building scenes: {e.Message}");
        }
    }

    // TODO-OPT#11: BuildMinimalX methods duplicate setup steps - refactor into shared BuildMinimalScene()
    private void BuildMinimalGeneratedLevel()
    {
        Log("Building minimal GeneratedLevel...");

        // Clear scene (keep only essentials)
        ClearSceneKeepEssentials();

        // Create core game objects
        CreatePlayer();
        CreateCamera();
        CreateLighting();
        CreateManagers();
        CreateUISystem();
        CreateLevelGenerator();

        Log("Minimal GeneratedLevel completed!");
    }

    private void BuildMinimalLevel1()
    {
        Log("Building minimal Level1...");

        ClearSceneKeepEssentials();

        // Create tutorial level layout
        CreatePlayer();
        CreateCamera();
        CreateLighting();
        CreateManagers();
        CreateUISystem();

        // Create simple level geometry
        CreateTutorialGeometry();

        Log("Minimal Level1 completed!");
    }

    private void BuildMinimalLevel2()
    {
        Log("Building minimal Level2...");

        ClearSceneKeepEssentials();
        CreatePlayer();
        CreateCamera();
        CreateLighting();
        CreateManagers();
        CreateUISystem();
        
        // Level2 with medium difficulty
        CreateMediumLevelGeometry();

        Log("Minimal Level2 completed!");
    }

    private void BuildMinimalLevel3()
    {
        Log("Building minimal Level3...");

        ClearSceneKeepEssentials();
        CreatePlayer();
        CreateCamera();
        CreateLighting();
        CreateManagers();
        CreateUISystem();
        
        // Level3 with hard difficulty
        CreateHardLevelGeometry();

        Log("Minimal Level3 completed!");
    }

    private void BuildMinimalOSMLevel()
    {
        Log("Building minimal OSM Level...");

        ClearSceneKeepEssentials();
        CreatePlayer();
        CreateCamera();
        CreateLighting();
        CreateManagers();
        CreateUISystem();
        CreateOSMSystem();

        Log("Minimal OSM Level completed!");
    }

    private void BuildMinimalMiniGame()
    {
        Log("Building minimal MiniGame...");

        ClearSceneKeepEssentials();
        CreatePlayer();
        CreateCamera();
        CreateLighting();
        CreateManagers();
        CreateUISystem();
        CreateMiniGameSystem();

        Log("Minimal MiniGame completed!");
    }

    private void ClearSceneKeepEssentials()
    {
        // Find and keep essential objects
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            // Keep objects that are essential
            if (obj.name.Contains("Main Camera") ||
                obj.name.Contains("Directional Light") ||
                obj.name.Contains("Canvas") ||
                obj.name.Contains("EventSystem"))
            {
                continue; // Keep these
            }

            // Remove everything else
            if (obj.transform.parent == null) // Only root objects
            {
                DestroyImmediate(obj);
            }
        }

        Log("Scene cleared, essentials kept");
    }

    private void CreatePlayer()
    {
        // Try to load prefab first
        GameObject playerPrefab = Resources.Load<GameObject>("Player");
        
        GameObject player;
        if (playerPrefab != null)
        {
            player = Instantiate(playerPrefab);
            Log("Player created from prefab");
        }
        else
        {
            // Create player manually
            player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player";
            player.tag = "Player";
            
            // Add components
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null) rb = player.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;

            // Add PlayerController if available
            PlayerController playerController = player.AddComponent<PlayerController>();
            
            Log("Player created manually");
        }

        player.transform.position = new Vector3(0, 1, 0);
    }

    private void CreateCamera()
    {
        Camera existingCamera = FindFirstObjectByType<Camera>();
        if (existingCamera != null) return;

        GameObject cameraGO = new GameObject("Main Camera");
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 10, -10);
        camera.transform.LookAt(Vector3.zero);

        // Add CameraController if available
        CameraController cameraController = cameraGO.AddComponent<CameraController>();
        
        Log("Camera created");
    }

    private void CreateLighting()
    {
        Light existingLight = FindFirstObjectByType<Light>();
        if (existingLight != null) return;

        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(45, 45, 0);

        Log("Lighting created");
    }

    private void CreateManagers()
    {
        // GameManager
        if (FindFirstObjectByType<GameManager>() == null)
        {
            GameObject gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
            Log("GameManager created");
        }

        // LevelManager
        if (FindFirstObjectByType<LevelManager>() == null)
        {
            GameObject lmGO = new GameObject("LevelManager");
            lmGO.AddComponent<LevelManager>();
            Log("LevelManager created");
        }

        // UIController
        if (FindFirstObjectByType<UIController>() == null)
        {
            GameObject uiGO = new GameObject("UIController");
            uiGO.AddComponent<UIController>();
            Log("UIController created");
        }
    }

    private void CreateUISystem()
    {
        // Canvas
        Canvas existingCanvas = FindFirstObjectByType<Canvas>();
        if (existingCanvas != null) return;

        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Basic UI elements
        CreateBasicUI(canvasGO);

        Log("UI System created");
    }

    private void CreateBasicUI(GameObject canvasGO)
    {
        // Collectible Text
        GameObject collectibleTextGO = new GameObject("CollectibleText");
        collectibleTextGO.transform.SetParent(canvasGO.transform);

        RectTransform textRect = collectibleTextGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(0, 1);
        textRect.anchoredPosition = new Vector2(20, -20);
        textRect.sizeDelta = new Vector2(200, 50);

        TMPro.TextMeshProUGUI textComponent = collectibleTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        textComponent.text = "Collectibles: 0/0";
        textComponent.fontSize = 24;
        textComponent.color = Color.white;
    }

    private void CreateLevelGenerator()
    {
        if (FindFirstObjectByType<LevelGenerator>() == null)
        {
            GameObject genGO = new GameObject("LevelGenerator");
            LevelGenerator generator = genGO.AddComponent<LevelGenerator>();
            
            // Create container structure
            GameObject levelContainer = new GameObject("LevelContainer");
            new GameObject("GroundContainer").transform.SetParent(levelContainer.transform);
            new GameObject("WallContainer").transform.SetParent(levelContainer.transform);
            new GameObject("CollectibleContainer").transform.SetParent(levelContainer.transform);
            new GameObject("EffectsContainer").transform.SetParent(levelContainer.transform);
            
            Log("LevelGenerator created");
        }
    }

    private void CreateTutorialGeometry()
    {
        // Simple ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(2, 1, 2);
        ground.transform.position = Vector3.zero;

        // Simple collectibles
        for (int i = 0; i < 5; i++)
        {
            GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collectible.name = $"Collectible_{i + 1}";
            collectible.tag = "Collectible";
            collectible.transform.position = new Vector3(i * 2 - 4, 1, 0);
            collectible.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // Make it a trigger
            Collider collider = collectible.GetComponent<Collider>();
            collider.isTrigger = true;
            
            // Add CollectibleController if available
            collectible.AddComponent<CollectibleController>();
        }

        // Goal zone
        GameObject goalZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goalZone.name = "GoalZone";
        goalZone.tag = "Finish";
        goalZone.transform.position = new Vector3(0, 0.1f, 8);
        goalZone.transform.localScale = new Vector3(2, 0.2f, 2);
        goalZone.GetComponent<Renderer>().material.color = Color.green;
        goalZone.GetComponent<Collider>().isTrigger = true;
        goalZone.SetActive(false); // Initially hidden

        Log("Tutorial geometry created");
    }

    private void CreateMediumLevelGeometry()
    {
        CreateTutorialGeometry(); // Start with basic geometry
        
        // Add some obstacles
        for (int i = 0; i < 3; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_{i + 1}";
            obstacle.transform.position = new Vector3(i * 3 - 3, 0.5f, 3);
            obstacle.GetComponent<Renderer>().material.color = Color.red;
        }

        Log("Medium level geometry created");
    }

    private void CreateHardLevelGeometry()
    {
        CreateMediumLevelGeometry(); // Start with medium geometry
        
        // Add moving platforms (simplified)
        for (int i = 0; i < 2; i++)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = $"MovingPlatform_{i + 1}";
            platform.transform.position = new Vector3(i * 4 - 2, 1, 5);
            platform.transform.localScale = new Vector3(2, 0.2f, 2);
            platform.GetComponent<Renderer>().material.color = Color.blue;
            
            // Add MovingPlatform component if available
            platform.AddComponent<MovingPlatform>();
        }

        Log("Hard level geometry created");
    }

    private void CreateOSMSystem()
    {
        // OSM system placeholder
        GameObject osmGO = new GameObject("OSMSystem");
        
        // Try to add OSM components if available
        try
        {
            osmGO.AddComponent<RollABall.Map.MapStartupController>();
        }
        catch
        {
            Log("OSM components not available, creating placeholder");
        }

        Log("OSM system created");
    }

    private void CreateMiniGameSystem()
    {
        // MiniGame system placeholder
        GameObject miniGameGO = new GameObject("MiniGameSystem");
        
        Log("MiniGame system created (placeholder)");
    }

    private void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[EmergencySceneBuilder] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[EmergencySceneBuilder] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[EmergencySceneBuilder] {message}");
    }

    /// <summary>
    /// Manual execution methods for Inspector
    /// </summary>
    [ContextMenu("Build Current Scene")]
    public void ManualBuildCurrentScene()
    {
        BuildCurrentScene();
    }

    [ContextMenu("Build All Scenes")]
    public void ManualBuildAllScenes()
    {
        BuildAllMinimalScenes();
    }
}
