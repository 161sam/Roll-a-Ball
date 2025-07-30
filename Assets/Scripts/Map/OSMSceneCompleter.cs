using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RollABall.Map;
using RollABall.Utility;

/// <summary>
/// Automated setup tool for completing the Level_OSM scene with all required UI components
/// Run this script to automatically configure the OSM map integration system
/// </summary>
public class OSMSceneCompleter : MonoBehaviour
{
    [Header("Auto-Setup Configuration")]
    [SerializeField] private bool setupUIComponents = true;
    [SerializeField] private bool assignReferences = true;
    [SerializeField] private bool configureMaterials = true;
    [SerializeField] private bool addCameraController = true;
    [SerializeField] private bool setupLighting = true;

    [Header("Debug")]
    [SerializeField] private bool verbose = false;

    private void Start()
    {
        if (Application.isPlaying)
        {
            CompleteOSMSceneSetup();
        }
    }

    /// <summary>
    /// Complete the OSM scene setup automatically
    /// </summary>
    [ContextMenu("Complete OSM Scene Setup")]
    public void CompleteOSMSceneSetup()
    {
        Log("🗺️ Starting OSM Scene Completion...");

        try
        {
            if (setupUIComponents) SetupUIComponents();
            if (assignReferences) AssignAllReferences();
            if (configureMaterials) ConfigureMaterials();
            if (addCameraController) SetupCameraController();
            if (setupLighting) ConfigureLighting();

            Log("✅ OSM Scene Setup Complete!");
            
            if (Application.isPlaying)
            {
                // Automatically start with fallback location for testing
                var mapController = Object.FindFirstObjectByType<MapStartupController>();
                if (mapController != null)
                {
                    mapController.LoadFallbackMap();
                }
            }
        }
        catch (System.Exception e)
        {
            LogError($"❌ OSM Scene Setup Failed: {e.Message}");
        }
    }

    private void SetupUIComponents()
    {
        // TODO-OPT#24: Many Setup* methods below repeat AddComponent and property assignments
        //              for UI elements. Extract generic builders to reduce duplication.
        Log("Setting up UI components...");

        var canvas = FindUICanvas();
        if (canvas == null)
        {
            LogError("UI Canvas not found!");
            return;
        }

        // Complete AddressInputPanel
        var addressPanel = FindOrCreateUIGameObject("AddressInputPanel", canvas.transform);
        CompleteAddressInputPanel(addressPanel);

        // Complete LoadingPanel  
        var loadingPanel = FindOrCreateUIGameObject("LoadingPanel", canvas.transform);
        CompleteLoadingPanel(loadingPanel);

        // Complete GameUIPanel
        var gameUIPanel = FindOrCreateUIGameObject("GameUIPanel", canvas.transform);
        CompleteGameUIPanel(gameUIPanel);

        Log("UI components setup complete");
    }

    private Canvas FindUICanvas()
    {
        var canvasObj = GameObject.Find("UI_Canvas");
        return canvasObj?.GetComponent<Canvas>();
    }

    private T FindOrCreateUI<T>(string name, Transform parent) where T : Component
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            return existing.GetComponent<T>() ?? existing.gameObject.AddComponent<T>();
        }

        var newObj = new GameObject(name);
        newObj.transform.SetParent(parent, false);
        return newObj.AddComponent<T>();
    }

    private GameObject FindOrCreateUIGameObject(string name, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        var newObj = new GameObject(name);
        newObj.transform.SetParent(parent, false);
        return newObj;
    }

    private void CompleteAddressInputPanel(GameObject panel)
    {
        if (panel == null) return;

        // Ensure Image component for background
        var image = PhysicsUtils.EnsureComponent<Image>(panel);
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(500, 300);

        // Address Input Field
        SetupAddressInputField(panel.transform);

        // Load Map Button
        SetupLoadMapButton(panel.transform);

        // Use Current Location Button
        SetupCurrentLocationButton(panel.transform);

        Log("AddressInputPanel completed");
    }

    private void SetupAddressInputField(Transform parent)
    {
        var inputFieldObj = FindOrCreateChild("AddressInputField", parent);
        var rectTransform = inputFieldObj.GetComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0.1f, 0.7f);
        rectTransform.anchorMax = new Vector2(0.9f, 0.85f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        // Setup TMP_InputField
        var inputField = PhysicsUtils.EnsureComponent<TMP_InputField>(inputFieldObj);
        
        // Background
        var background = PhysicsUtils.EnsureComponent<Image>(inputFieldObj);
        background.color = Color.white;
        inputField.targetGraphic = background;

        // Text Component
        var textObj = FindOrCreateChild("Text", inputFieldObj.transform);
        var text = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(textObj);
        text.text = "";
        text.fontSize = 14;
        text.color = Color.black;
        inputField.textComponent = text;

        // Placeholder
        var placeholderObj = FindOrCreateChild("Placeholder", inputFieldObj.transform);
        var placeholder = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(placeholderObj);
        placeholder.text = "z.B. Leipzig, Markt";
        placeholder.fontSize = 14;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        inputField.placeholder = placeholder;

        Log("AddressInputField setup complete");
    }

    private void SetupLoadMapButton(Transform parent)
    {
        var buttonObj = FindOrCreateChild("LoadMapButton", parent);
        var rectTransform = buttonObj.GetComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0.1f, 0.4f);
        rectTransform.anchorMax = new Vector2(0.45f, 0.6f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        var button = PhysicsUtils.EnsureComponent<Button>(buttonObj);
        var image = PhysicsUtils.EnsureComponent<Image>(buttonObj);
        image.color = new Color(0.2f, 0.6f, 1f, 1f);
        button.targetGraphic = image;

        // Button Text
        var textObj = FindOrCreateChild("Text", buttonObj.transform);
        var text = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(textObj);
        text.text = "Karte laden";
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        Log("LoadMapButton setup complete");
    }

    private void SetupCurrentLocationButton(Transform parent)
    {
        var buttonObj = FindOrCreateChild("UseCurrentLocationButton", parent);
        var rectTransform = buttonObj.GetComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0.55f, 0.4f);
        rectTransform.anchorMax = new Vector2(0.9f, 0.6f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        var button = PhysicsUtils.EnsureComponent<Button>(buttonObj);
        var image = PhysicsUtils.EnsureComponent<Image>(buttonObj);
        image.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        button.targetGraphic = image;

        // Button Text
        var textObj = FindOrCreateChild("Text", buttonObj.transform);
        var text = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(textObj);
        text.text = "Mein Standort";
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        Log("UseCurrentLocationButton setup complete");
    }

    private void CompleteLoadingPanel(GameObject panel)
    {
        if (panel == null) return;

        var image = PhysicsUtils.EnsureComponent<Image>(panel);
        image.color = new Color(0f, 0f, 0f, 0.7f);

        var rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        // Loading Text
        var textObj = FindOrCreateChild("LoadingText", panel.transform);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.6f);
        textRect.anchorMax = new Vector2(0.5f, 0.6f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(400, 50);

        var text = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(textObj);
        text.text = "Lade Kartendaten...";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        // Progress Bar
        var progressObj = FindOrCreateChild("LoadingProgressBar", panel.transform);
        var progressRect = progressObj.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.3f, 0.4f);
        progressRect.anchorMax = new Vector2(0.7f, 0.45f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = new Vector2(0, 20);

        var slider = PhysicsUtils.EnsureComponent<Slider>(progressObj);
        var sliderImage = PhysicsUtils.EnsureComponent<Image>(progressObj);
        sliderImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Initially hidden
        panel.SetActive(false);

        Log("LoadingPanel completed");
    }

    private void CompleteGameUIPanel(GameObject panel)
    {
        if (panel == null) return;

        // Ensure RectTransform component exists
        var rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = panel.AddComponent<RectTransform>();
        }
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        // Collectible Text
        SetupCollectibleText(panel.transform);

        // Location Text
        SetupLocationText(panel.transform);

        // Level Type Text
        SetupLevelTypeText(panel.transform);

        // Fly Bar (for player)
        SetupFlyBar(panel.transform);

        // Control Buttons
        SetupControlButtons(panel.transform);

        // Initially hidden
        panel.SetActive(false);

        Log("GameUIPanel completed");
    }

    private void SetupCollectibleText(Transform parent)
    {
        var textObj = FindOrCreateChild("CollectibleText", parent);
        var rectTransform = textObj.GetComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0.02f, 0.02f);
        rectTransform.anchorMax = new Vector2(0.35f, 0.12f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        var text = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(textObj);
        text.text = "Collectibles: 0";
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
    }

    private void SetupLocationText(Transform parent)
    {
        var textObj = FindOrCreateChild("LocationText", parent);
        var rectTransform = textObj.GetComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0.4f, 0.02f);
        rectTransform.anchorMax = new Vector2(0.8f, 0.12f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        var text = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(textObj);
        text.text = "Ort: Unbekannt";
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
    }

    private void SetupLevelTypeText(Transform parent)
    {
        var textObj = FindOrCreateChild("LevelTypeText", parent);
        var rectTransform = textObj.GetComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0.4f, 0.02f);
        rectTransform.anchorMax = new Vector2(0.7f, 0.12f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        var text = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(textObj);
        text.text = "OSM Map";
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
    }

    private void SetupFlyBar(Transform parent)
    {
        var barObj = FindOrCreateChild("FlyBar", parent);
        var rectTransform = barObj.GetComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0.02f, 0.85f);
        rectTransform.anchorMax = new Vector2(0.25f, 0.95f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        var slider = PhysicsUtils.EnsureComponent<Slider>(barObj);
        var image = PhysicsUtils.EnsureComponent<Image>(barObj);
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
    }

    private void SetupControlButtons(Transform parent)
    {
        // Regenerate Map Button
        var regenObj = FindOrCreateChild("RegenerateMapButton", parent);
        var regenRect = regenObj.GetComponent<RectTransform>();
        regenRect.anchorMin = new Vector2(0.75f, 0.85f);
        regenRect.anchorMax = new Vector2(0.95f, 0.95f);
        regenRect.anchoredPosition = Vector2.zero;
        regenRect.sizeDelta = Vector2.zero;

        var regenButton = PhysicsUtils.EnsureComponent<Button>(regenObj);
        var regenImage = PhysicsUtils.EnsureComponent<Image>(regenObj);
        regenImage.color = new Color(1f, 0.6f, 0f, 1f);

        var regenTextObj = FindOrCreateChild("Text", regenObj.transform);
        var regenText = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(regenTextObj);
        regenText.text = "Neu laden";
        regenText.fontSize = 14;
        regenText.color = Color.white;
        regenText.alignment = TextAlignmentOptions.Center;

        // Back to Menu Button
        var backObj = FindOrCreateChild("BackToMenuButton", parent);
        var backRect = backObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.75f, 0.75f);
        backRect.anchorMax = new Vector2(0.95f, 0.85f);
        backRect.anchoredPosition = Vector2.zero;
        backRect.sizeDelta = Vector2.zero;

        var backButton = PhysicsUtils.EnsureComponent<Button>(backObj);
        var backImage = PhysicsUtils.EnsureComponent<Image>(backObj);
        backImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        var backTextObj = FindOrCreateChild("Text", backObj.transform);
        var backText = PhysicsUtils.EnsureComponent<TextMeshProUGUI>(backTextObj);
        backText.text = "Menü";
        backText.fontSize = 14;
        backText.color = Color.white;
        backText.alignment = TextAlignmentOptions.Center;
    }

    private GameObject FindOrCreateChild(string name, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        var newObj = new GameObject(name);
        newObj.transform.SetParent(parent, false);
        
        // Add RectTransform for UI objects
        if (parent.GetComponent<RectTransform>() != null)
        {
            newObj.AddComponent<RectTransform>();
        }

        return newObj;
    }

    private void AssignAllReferences()
    {
        Log("Assigning component references...");

        var mapController = FindFirstObjectByType<MapStartupController>();
        var gameManager = FindFirstObjectByType<GameManager>();
        var uiController = FindFirstObjectByType<UIController>();

        if (mapController == null)
        {
            LogError("MapStartupController not found!");
            return;
        }

        // Auto-assign UI references using reflection for robustness
        AutoAssignUIReferences(mapController);

        Log("Component references assigned");
    }

    private void AutoAssignUIReferences(MapStartupController controller)
    {
        var canvas = FindUICanvas();
        if (canvas == null) return;

        // Use reflection to assign references automatically
        var fields = typeof(MapStartupController).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(GameObject))
            {
                AssignGameObjectField(controller, field, canvas.transform);
            }
            else if (field.FieldType.IsSubclassOf(typeof(Component)))
            {
                AssignComponentField(controller, field, canvas.transform);
            }
        }
    }

    private void AssignGameObjectField(MapStartupController controller, System.Reflection.FieldInfo field, Transform searchRoot)
    {
        var fieldName = field.Name;
        var targetName = GetUINameFromFieldName(fieldName);
        
        if (!string.IsNullOrEmpty(targetName))
        {
            var target = FindUIElementRecursive(searchRoot, targetName);
            if (target != null)
            {
                field.SetValue(controller, target.gameObject);
                Log($"Assigned {fieldName} -> {targetName}");
            }
        }
    }

    private void AssignComponentField(MapStartupController controller, System.Reflection.FieldInfo field, Transform searchRoot)
    {
        var fieldName = field.Name;
        var targetName = GetUINameFromFieldName(fieldName);
        
        if (!string.IsNullOrEmpty(targetName))
        {
            var target = FindUIElementRecursive(searchRoot, targetName);
            if (target != null)
            {
                var component = target.GetComponent(field.FieldType);
                if (component != null)
                {
                    field.SetValue(controller, component);
                    Log($"Assigned {fieldName} -> {targetName} ({field.FieldType.Name})");
                }
            }
        }
    }

    private string GetUINameFromFieldName(string fieldName)
    {
        // Convert camelCase field names to UI element names
        switch (fieldName)
        {
            case "addressInputPanel": return "AddressInputPanel";
            case "addressInputField": return "AddressInputField";
            case "loadMapButton": return "LoadMapButton";
            case "useCurrentLocationButton": return "UseCurrentLocationButton";
            case "loadingPanel": return "LoadingPanel";
            case "loadingText": return "LoadingText";
            case "loadingProgressBar": return "LoadingProgressBar";
            case "gameUIPanel": return "GameUIPanel";
            case "collectibleText": return "CollectibleText";
            case "locationText": return "LocationText";
            case "levelTypeText": return "LevelTypeText";
            case "flyBar": return "FlyBar";
            case "regenerateMapButton": return "RegenerateMapButton";
            case "backToMenuButton": return "BackToMenuButton";
            default: return null;
        }
    }

    private Transform FindUIElementRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindUIElementRecursive(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private void ConfigureMaterials()
    {
        Log("Configuring materials...");

        var mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null)
        {
            LogError("MapGenerator not found!");
            return;
        }

        // Try to find existing materials from other scenes
        AssignExistingMaterials(mapGenerator);

        Log("Materials configured");
    }

    private void AssignExistingMaterials(MapGenerator generator)
    {
        // This would typically load materials from the Resources folder or Asset Database
        // For now, create simple colored materials as fallback
        CreateFallbackMaterials(generator);
    }

    private void CreateFallbackMaterials(MapGenerator generator)
    {
        // Create basic materials if none exist
        Log("Creating fallback materials...");
        
        try
        {
            // Try to load existing materials first
            var existingGroundMats = LoadExistingMaterialsContaining("Ground", "Steam");
            var existingWallMats = LoadExistingMaterialsContaining("Wall", "Steam");
            var existingGoalMat = LoadExistingMaterial("GoalZone");
            
            // Assign via reflection since MapGenerator fields might be private
            AssignMaterialsToMapGenerator(generator, "groundMaterials", existingGroundMats.ToArray());
            AssignMaterialsToMapGenerator(generator, "wallMaterials", existingWallMats.ToArray());
            AssignMaterialToMapGenerator(generator, "goalZoneMaterial", existingGoalMat);
            
            if (existingGroundMats.Count > 0 || existingWallMats.Count > 0)
            {
                Log($"✅ Assigned {existingGroundMats.Count} ground, {existingWallMats.Count} wall materials to MapGenerator");
            }
            else
            {
                LogWarning("No suitable materials found - creating basic fallbacks");
                CreateBasicFallbackMaterials(generator);
            }
        }
        catch (System.Exception e)
        {
            LogError($"Failed to assign materials: {e.Message}");
            LogWarning("Materials need to be assigned manually in the MapGenerator component");
        }
    }

    private void SetupCameraController()
    {
        Log("Setting up camera controller...");

        var camera = Camera.main;
        if (camera == null)
        {
            LogError("Main Camera not found!");
            return;
        }

        // Add CameraController if it doesn't exist
        var cameraController = camera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = camera.gameObject.AddComponent<CameraController>();
            Log("CameraController added to Main Camera");
        }

        // Add AudioListener if it doesn't exist
        var audioListener = camera.GetComponent<AudioListener>();
        if (audioListener == null)
        {
            camera.gameObject.AddComponent<AudioListener>();
            Log("AudioListener added to Main Camera");
        }

        Log("Camera controller setup complete");
    }

    private void ConfigureLighting()
    {
        Log("Configuring lighting...");

        // Setup ambient lighting for steampunk atmosphere
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.4f, 0.35f, 0.25f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.3f, 0.25f, 0.2f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.15f, 0.1f, 1f);

        // Setup fog for atmosphere
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.5f, 0.4f, 0.3f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.02f;

        Log("Lighting configured for steampunk atmosphere");
    }

    private void Log(string message)
    {
        if (verbose)
            Debug.Log($"[OSMSceneCompleter] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[OSMSceneCompleter] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[OSMSceneCompleter] {message}");
    }
    
    private System.Collections.Generic.List<Material> LoadExistingMaterialsContaining(params string[] keywords)
    {
        var materials = new System.Collections.Generic.List<Material>();
        
#if UNITY_EDITOR
        string[] materialGUIDs = UnityEditor.AssetDatabase.FindAssets("t:Material");
        
        foreach (string guid in materialGUIDs)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            bool containsKeyword = false;
            foreach (string keyword in keywords)
            {
                if (fileName.ToLower().Contains(keyword.ToLower()))
                {
                    containsKeyword = true;
                    break;
                }
            }
            
            if (containsKeyword)
            {
                Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                {
                    materials.Add(material);
                }
            }
        }
#endif
        return materials;
    }
    
    private Material LoadExistingMaterial(string nameContains)
    {
#if UNITY_EDITOR
        string[] materialGUIDs = UnityEditor.AssetDatabase.FindAssets("t:Material");
        
        foreach (string guid in materialGUIDs)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            if (fileName.ToLower().Contains(nameContains.ToLower()))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }
#endif
        return null;
    }
    
    private void AssignMaterialsToMapGenerator(MapGenerator generator, string fieldName, Material[] materials)
    {
        if (materials == null || materials.Length == 0) return;
        
        var field = typeof(MapGenerator).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(generator, materials);
            Log($"Assigned {materials.Length} materials to {fieldName}");
        }
    }
    
    private void AssignMaterialToMapGenerator(MapGenerator generator, string fieldName, Material material)
    {
        if (material == null) return;
        
        var field = typeof(MapGenerator).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(generator, material);
            Log($"Assigned material to {fieldName}");
        }
    }
    
    private void CreateBasicFallbackMaterials(MapGenerator generator)
    {
        // Create runtime materials as absolute fallback
        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = Color.gray;
        groundMat.name = "Runtime_GroundMaterial";
        
        Material wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = Color.white;
        wallMat.name = "Runtime_WallMaterial";
        
        Material goalMat = new Material(Shader.Find("Standard"));
        goalMat.color = Color.green;
        goalMat.name = "Runtime_GoalMaterial";
        
        AssignMaterialsToMapGenerator(generator, "groundMaterials", new Material[] { groundMat });
        AssignMaterialsToMapGenerator(generator, "wallMaterials", new Material[] { wallMat });
        AssignMaterialToMapGenerator(generator, "goalZoneMaterial", goalMat);
        
        Log("Created basic runtime fallback materials");
    }

#if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/Complete OSM Scene Setup")]
    public static void CompleteOSMSceneSetupMenuItem()
    {
        var completer = FindFirstObjectByType<OSMSceneCompleter>();
        if (completer == null)
        {
            var tempObj = new GameObject("Temp_OSMSceneCompleter");
            completer = tempObj.AddComponent<OSMSceneCompleter>();
        }

        completer.CompleteOSMSceneSetup();

        if (completer.gameObject.name == "Temp_OSMSceneCompleter")
        {
            DestroyImmediate(completer.gameObject);
        }
    }
#endif
}
