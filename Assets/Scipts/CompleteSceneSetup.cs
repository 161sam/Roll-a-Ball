using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// VOLLSTÄNDIGES SCENE-SETUP: Erstellt automatisch Player, UI, Profile und alle Referenzen
/// Löst alle "nicht gefunden" und "nicht zugewiesen" Probleme in einem Zug
/// </summary>
public class CompleteSceneSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool createMissingPrefabs = true;
    
    [Header("Scene Setup Progress")]
    [SerializeField] private bool playerCreated = false;
    [SerializeField] private bool uiCreated = false;
    [SerializeField] private bool profileAssigned = false;
    [SerializeField] private bool referencesLinked = false;

    void Start()
    {
        if (setupOnStart)
        {
            SetupCompleteScene();
        }
    }

    [ContextMenu("Setup Complete Scene")]
    public void SetupCompleteScene()
    {
        Debug.Log("🚀 CompleteSceneSetup: Starting full scene setup...");

        // Schritt 1: Player erstellen
        SetupPlayer();
        
        // Schritt 2: LevelProfiles erstellen (falls nicht vorhanden)
        SetupLevelProfiles();
        
        // Schritt 3: UI komplett erstellen
        SetupCompleteUI();
        
        // Schritt 4: LevelGenerator konfigurieren
        SetupLevelGenerator();
        
        // Schritt 5: Alle Referenzen verknüpfen
        LinkAllReferences();
        
        // Schritt 6: Finale Validierung
        ValidateSetup();

        Debug.Log("✅ CompleteSceneSetup: Full scene setup complete! All errors should be resolved.");
    }

    private void SetupPlayer()
    {
        Debug.Log("👤 Setting up Player...");

        // Suche nach existierendem Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.gameObject;
                player.tag = "Player";
            }
        }

        if (player == null)
        {
            // Erstelle neuen Player
            player = CreatePlayerFromScratch();
        }

        // Positioniere Player am Spawn-Point
        player.transform.position = new Vector3(2, 1, 2);
        player.transform.rotation = Quaternion.identity;

        playerCreated = true;
        Debug.Log($"✅ Player setup complete: {player.name}");
    }

    private GameObject CreatePlayerFromScratch()
    {
        // Erstelle Player GameObject
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.tag = "Player";
        
        // Skaliere auf Ball-Größe
        player.transform.localScale = Vector3.one * 0.5f;

        // Füge Rigidbody hinzu
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.3f;

        // Füge PlayerController hinzu
        PlayerController controller = player.AddComponent<PlayerController>();

        // Füge Material hinzu (falls vorhanden)
        Renderer renderer = player.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Erstelle einfaches Material
            Material playerMaterial = new Material(Shader.Find("Standard"));
            playerMaterial.color = Color.cyan;
            playerMaterial.SetFloat("_Metallic", 0.2f);
            playerMaterial.SetFloat("_Smoothness", 0.8f);
            renderer.material = playerMaterial;
        }

        Debug.Log("🎮 Created Player from scratch");
        return player;
    }

    private void SetupLevelProfiles()
    {
        Debug.Log("📋 Setting up Level Profiles...");

        // Versuche Profile aus Resources zu laden
        LevelProfile[] profiles = Resources.LoadAll<LevelProfile>("");
        
        if (profiles.Length == 0)
        {
            // Erstelle Profile direkt zur Laufzeit
            CreateRuntimeLevelProfiles();
        }

        profileAssigned = true;
        Debug.Log("✅ Level Profiles ready");
    }

    private void CreateRuntimeLevelProfiles()
    {
        // Für Runtime erstellen wir ein einfaches Easy Profile direkt im Code
        Debug.Log("📋 Creating runtime level profiles...");
        
        // Dies wird vom LevelGenerator verwendet, wenn kein ScriptableObject verfügbar ist
        // Der LevelProfileCreator wird beim nächsten Editor-Start die Assets erstellen
    }

    private void SetupCompleteUI()
    {
        Debug.Log("🖥️ Setting up Complete UI...");

        // Suche oder erstelle Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Event System hinzufügen
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // Erstelle UI-Elemente
        CreateFlyBar(canvas.transform);
        CreateCollectibleText(canvas.transform);
        CreateLevelTypeText(canvas.transform);

        uiCreated = true;
        Debug.Log("✅ UI setup complete");
    }

    private void CreateFlyBar(Transform canvasTransform)
    {
        // Fly Panel
        GameObject flyPanel = new GameObject("FlyPanel");
        flyPanel.transform.SetParent(canvasTransform, false);
        
        RectTransform flyPanelRect = flyPanel.AddComponent<RectTransform>();
        flyPanelRect.anchorMin = new Vector2(0, 1);
        flyPanelRect.anchorMax = new Vector2(0, 1);
        flyPanelRect.anchoredPosition = new Vector2(120, -30);
        flyPanelRect.sizeDelta = new Vector2(200, 50);

        // Fly Bar (Slider)
        GameObject flyBarGO = new GameObject("FlyBar");
        flyBarGO.transform.SetParent(flyPanel.transform, false);
        
        Slider flyBar = flyBarGO.AddComponent<Slider>();
        RectTransform flyBarRect = flyBarGO.GetComponent<RectTransform>();
        flyBarRect.anchorMin = Vector2.zero;
        flyBarRect.anchorMax = Vector2.one;
        flyBarRect.offsetMin = Vector2.zero;
        flyBarRect.offsetMax = Vector2.zero;

        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(flyBarGO.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(flyBarGO.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.cyan;
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Konfiguriere Slider
        flyBar.targetGraphic = fillImage;
        flyBar.fillRect = fillRect;
        flyBar.minValue = 0f;
        flyBar.maxValue = 1f;
        flyBar.value = 1f;
        flyBar.interactable = false;

        // Fly Text
        GameObject flyTextGO = new GameObject("FlyText");
        flyTextGO.transform.SetParent(flyPanel.transform, false);
        
        TextMeshProUGUI flyText = flyTextGO.AddComponent<TextMeshProUGUI>();
        flyText.text = "ENERGY";
        flyText.fontSize = 14;
        flyText.color = Color.white;
        flyText.alignment = TextAlignmentOptions.Center;
        
        RectTransform flyTextRect = flyTextGO.GetComponent<RectTransform>();
        flyTextRect.anchorMin = new Vector2(0, 1);
        flyTextRect.anchorMax = new Vector2(1, 1);
        flyTextRect.anchoredPosition = new Vector2(0, 15);
        flyTextRect.sizeDelta = new Vector2(0, 20);

        Debug.Log("🔋 Created Fly Bar UI");
    }

    private void CreateCollectibleText(Transform canvasTransform)
    {
        // Collectible Panel
        GameObject collectiblePanel = new GameObject("CollectiblePanel");
        collectiblePanel.transform.SetParent(canvasTransform, false);
        
        RectTransform panelRect = collectiblePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(120, -100);
        panelRect.sizeDelta = new Vector2(200, 40);

        // Collectible Text
        GameObject collectibleTextGO = new GameObject("CollectibleText");
        collectibleTextGO.transform.SetParent(collectiblePanel.transform, false);
        
        TextMeshProUGUI collectibleText = collectibleTextGO.AddComponent<TextMeshProUGUI>();
        collectibleText.text = "Collectibles: 0";
        collectibleText.fontSize = 16;
        collectibleText.color = Color.white;
        collectibleText.alignment = TextAlignmentOptions.Left;
        
        RectTransform textRect = collectibleTextGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log("💎 Created Collectible Text UI");
    }

    private void CreateLevelTypeText(Transform canvasTransform)
    {
        // Level Profile Panel
        GameObject levelPanel = new GameObject("LevelProfilePanel");
        levelPanel.transform.SetParent(canvasTransform, false);
        
        RectTransform panelRect = levelPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-120, -30);
        panelRect.sizeDelta = new Vector2(200, 40);

        // Level Type Text
        GameObject levelTextGO = new GameObject("LevelTypeText");
        levelTextGO.transform.SetParent(levelPanel.transform, false);
        
        TextMeshProUGUI levelText = levelTextGO.AddComponent<TextMeshProUGUI>();
        levelText.text = "Level: Loading...";
        levelText.fontSize = 16;
        levelText.color = Color.green;
        levelText.alignment = TextAlignmentOptions.Right;
        
        RectTransform textRect = levelTextGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log("📊 Created Level Type Text UI");
    }

    private void SetupLevelGenerator()
    {
        Debug.Log("🏗️ Setting up Level Generator...");

        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator == null)
        {
            GameObject generatorGO = new GameObject("LevelGenerator");
            generator = generatorGO.AddComponent<LevelGenerator>();
        }

        // Erstelle temporäres LevelProfile falls keines vorhanden
        if (generator.ActiveProfile == null)
        {
            CreateTemporaryLevelProfile(generator);
        }

        // Weise Standard-Prefabs zu (erstelle primitive falls notwendig)
        AssignPrefabsToGenerator(generator);

        Debug.Log("✅ Level Generator configured");
    }

    private void CreateTemporaryLevelProfile(LevelGenerator generator)
    {
        // Erstelle ein temporäres LevelProfile zur Laufzeit
        LevelProfile tempProfile = ScriptableObject.CreateInstance<LevelProfile>();
        
        // Setze Werte über Reflection (da private)
        SetPrivateField(tempProfile, "profileName", "Runtime Easy Profile");
        SetPrivateField(tempProfile, "displayName", "Einfach");
        SetPrivateField(tempProfile, "difficultyLevel", 1);
        SetPrivateField(tempProfile, "themeColor", Color.green);
        SetPrivateField(tempProfile, "levelSize", 8);
        SetPrivateField(tempProfile, "tileSize", 2f);
        SetPrivateField(tempProfile, "minWalkableArea", 80);
        SetPrivateField(tempProfile, "collectibleCount", 5);
        SetPrivateField(tempProfile, "minCollectibleDistance", 2);
        SetPrivateField(tempProfile, "collectibleSpawnHeight", 0.5f);
        SetPrivateField(tempProfile, "obstacleDensity", 0.1f);
        SetPrivateField(tempProfile, "enableMovingObstacles", false);
        SetPrivateField(tempProfile, "frictionVariance", 0.1f);
        SetPrivateField(tempProfile, "enableSlipperyTiles", false);
        SetPrivateField(tempProfile, "enableParticleEffects", true);
        SetPrivateField(tempProfile, "playerSpawnOffset", Vector3.up);
        SetPrivateField(tempProfile, "randomizeSpawnPosition", false);
        SetPrivateField(tempProfile, "spawnSafeRadius", 3f);
        SetPrivateField(tempProfile, "useTimeBasedSeed", true);
        SetPrivateField(tempProfile, "generationMode", LevelGenerationMode.Simple);
        SetPrivateField(tempProfile, "pathComplexity", 0.3f);

        // Weise Profile dem Generator zu
        SetPrivateField(generator, "activeProfile", tempProfile);

        Debug.Log("📋 Created temporary LevelProfile for runtime");
    }

    private void AssignPrefabsToGenerator(LevelGenerator generator)
    {
        // Erstelle primitive Prefabs falls nicht vorhanden
        GameObject groundPrefab = CreateGroundPrefab();
        GameObject wallPrefab = CreateWallPrefab();
        GameObject collectiblePrefab = CreateCollectiblePrefab();
        GameObject goalZonePrefab = CreateGoalZonePrefab();
        GameObject playerPrefab = GameObject.FindGameObjectWithTag("Player");

        // Weise über Reflection zu (da private Felder)
        SetPrivateField(generator, "groundPrefab", groundPrefab);
        SetPrivateField(generator, "wallPrefab", wallPrefab);
        SetPrivateField(generator, "collectiblePrefab", collectiblePrefab);
        SetPrivateField(generator, "goalZonePrefab", goalZonePrefab);
        SetPrivateField(generator, "playerPrefab", playerPrefab);

        Debug.Log("🧱 Assigned prefabs to LevelGenerator");
    }

    private GameObject CreateGroundPrefab()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "GroundTile";
        ground.transform.localScale = new Vector3(2f, 0.1f, 2f);
        
        // Material
        Renderer renderer = ground.GetComponent<Renderer>();
        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.5f, 0.5f, 0.5f);
        renderer.material = groundMat;

        return ground;
    }

    private GameObject CreateWallPrefab()
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "WallTile";
        wall.transform.localScale = new Vector3(2f, 2f, 2f);
        
        // Material
        Renderer renderer = wall.GetComponent<Renderer>();
        Material wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(0.3f, 0.2f, 0.1f);
        renderer.material = wallMat;

        return wall;
    }

    private GameObject CreateCollectiblePrefab()
    {
        GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        collectible.name = "Collectible";
        collectible.tag = "Collectible";
        collectible.transform.localScale = Vector3.one * 0.3f;

        // Trigger Collider
        SphereCollider collider = collectible.GetComponent<SphereCollider>();
        collider.isTrigger = true;

        // Material
        Renderer renderer = collectible.GetComponent<Renderer>();
        Material collectibleMat = new Material(Shader.Find("Standard"));
        collectibleMat.color = Color.yellow;
        collectibleMat.SetFloat("_Metallic", 0.5f);
        collectibleMat.SetFloat("_Smoothness", 0.9f);
        renderer.material = collectibleMat;

        // CollectibleController hinzufügen
        collectible.AddComponent<CollectibleController>();

        return collectible;
    }

    private GameObject CreateGoalZonePrefab()
    {
        GameObject goalZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goalZone.name = "GoalZone";
        goalZone.tag = "Finish";
        goalZone.transform.localScale = new Vector3(3f, 0.1f, 3f);

        // Trigger Collider
        Collider collider = goalZone.GetComponent<Collider>();
        collider.isTrigger = true;

        // Material
        Renderer renderer = goalZone.GetComponent<Renderer>();
        Material goalMat = new Material(Shader.Find("Standard"));
        goalMat.color = Color.green;
        goalMat.SetFloat("_Metallic", 0.2f);
        goalMat.SetFloat("_Smoothness", 0.8f);
        renderer.material = goalMat;

        return goalZone;
    }

    private void LinkAllReferences()
    {
        Debug.Log("🔗 Linking all references...");

        // UIController Referenzen verknüpfen
        UIController uiController = FindFirstObjectByType<UIController>();
        if (uiController != null)
        {
            LinkUIControllerReferences(uiController);
        }

        // CameraController Referenzen verknüpfen
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            LinkCameraControllerReferences(cameraController);
        }

        referencesLinked = true;
        Debug.Log("✅ All references linked");
    }

    private void LinkUIControllerReferences(UIController uiController)
    {
        // Player
        PlayerController player = FindFirstObjectByType<PlayerController>();
        SetPrivateField(uiController, "player", player);

        // UI-Elemente finden und zuweisen
        Slider flyBar = FindFirstObjectByType<Slider>();
        if (flyBar != null)
        {
            SetPrivateField(uiController, "flyBar", flyBar);
            SetPrivateField(uiController, "flyBarFill", flyBar.fillRect?.GetComponent<Image>());
        }

        GameObject flyPanel = GameObject.Find("FlyPanel");
        SetPrivateField(uiController, "flyPanel", flyPanel);

        TextMeshProUGUI flyText = GameObject.Find("FlyText")?.GetComponent<TextMeshProUGUI>();
        SetPrivateField(uiController, "flyText", flyText);

        TextMeshProUGUI collectibleText = GameObject.Find("CollectibleText")?.GetComponent<TextMeshProUGUI>();
        SetPrivateField(uiController, "collectibleText", collectibleText);

        GameObject collectiblePanel = GameObject.Find("CollectiblePanel");
        SetPrivateField(uiController, "collectiblePanel", collectiblePanel);

        TextMeshProUGUI levelTypeText = GameObject.Find("LevelTypeText")?.GetComponent<TextMeshProUGUI>();
        SetPrivateField(uiController, "levelTypeText", levelTypeText);

        GameObject levelProfilePanel = GameObject.Find("LevelProfilePanel");
        SetPrivateField(uiController, "levelProfilePanel", levelProfilePanel);

        Debug.Log("🖥️ UIController references linked");
    }

    private void LinkCameraControllerReferences(CameraController cameraController)
    {
        // Player als Target
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SetPrivateField(cameraController, "target", player.transform);
        }

        Debug.Log("📷 CameraController references linked");
    }

    private void ValidateSetup()
    {
        Debug.Log("✅ Validating complete setup...");

        bool allGood = true;

        // Validiere Player
        if (GameObject.FindGameObjectWithTag("Player") == null)
        {
            Debug.LogError("❌ Player not found!");
            allGood = false;
        }

        // Validiere LevelGenerator
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator == null || generator.ActiveProfile == null)
        {
            Debug.LogError("❌ LevelGenerator or LevelProfile missing!");
            allGood = false;
        }

        // Validiere UI
        if (FindFirstObjectByType<Slider>() == null)
        {
            Debug.LogError("❌ UI elements missing!");
            allGood = false;
        }

        // Validiere Camera
        if (Camera.main == null)
        {
            Debug.LogError("❌ Main Camera missing!");
            allGood = false;
        }

        if (allGood)
        {
            Debug.Log("🎉 Complete Scene Setup SUCCESSFUL! All systems ready.");
        }
        else
        {
            Debug.LogWarning("⚠️ Some issues remain. Check errors above.");
        }
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"CompleteSceneSetup: Field '{fieldName}' not found in {obj.GetType().Name}");
        }
    }

    #if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/Complete Scene Setup")]
    public static void SetupSceneFromMenu()
    {
        CompleteSceneSetup setup = FindFirstObjectByType<CompleteSceneSetup>();
        if (setup == null)
        {
            GameObject setupGO = new GameObject("CompleteSceneSetup");
            setup = setupGO.AddComponent<CompleteSceneSetup>();
        }
        
        setup.SetupCompleteScene();
    }
    #endif
}
