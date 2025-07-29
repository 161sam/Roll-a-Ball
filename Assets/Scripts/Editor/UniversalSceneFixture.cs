using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Universal Scene Fixture - Fixes common scene setup issues across all Roll-a-Ball scenes
/// Provides automated solutions for missing components, references, and configurations
/// </summary>
public class UniversalSceneFixture : MonoBehaviour
{
    [Header("Fix Configuration")]
    [SerializeField] private bool autoFixOnStart = false;
    [SerializeField] private bool verboseLogging = true;
    [SerializeField] private bool validateAfterFix = true;
    
    private List<string> fixLog = new List<string>();
    
    /// <summary>
    /// Fix the current scene - main entry point
    /// </summary>
    public void FixCurrentScene()
    {
        fixLog.Clear();
        LogFix("🔧 Starting Universal Scene Fixture");
        
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LogFix($"Fixing scene: {sceneName}");
        
        // Run all fix operations
        FixEssentialComponents();
        FixPlayerSetup();
        FixUI();
        FixCollectibles();
        FixLevelManager();
        FixGameManager();
        FixPrefabReferences();
        FixMaterials();
        FixTags();
        
        if (validateAfterFix)
        {
            ValidateScene();
        }
        
        LogFix("✅ Universal Scene Fixture completed");
        
        // Output summary
        if (verboseLogging)
        {
            foreach (string logEntry in fixLog)
            {
                Debug.Log($"[UniversalSceneFixture] {logEntry}");
            }
        }
        
        // Mark scene as dirty
        #if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        #endif
    }
    
    private void Start()
    {
        if (autoFixOnStart)
        {
            FixCurrentScene();
        }
    }
    
    #region Fix Operations
    
    private void FixEssentialComponents()
    {
        LogFix("Checking essential components...");
        
        // Ensure GameManager exists
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            GameObject gameManagerGO = new GameObject("GameManager");
            gameManager = gameManagerGO.AddComponent<GameManager>();
            LogFix("✅ Created GameManager");
        }
        
        // Ensure LevelManager exists in non-OSM scenes
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "Level_OSM")
        {
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager == null)
            {
                GameObject levelManagerGO = new GameObject("LevelManager");
                levelManager = levelManagerGO.AddComponent<LevelManager>();
                LogFix("✅ Created LevelManager");
            }
        }
        
        // Ensure UIController exists
        UIController uiController = FindFirstObjectByType<UIController>();
        if (uiController == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                uiController = canvas.gameObject.AddComponent<UIController>();
                LogFix("✅ Added UIController to Canvas");
            }
            else
            {
                GameObject uiGO = new GameObject("UIController");
                uiController = uiGO.AddComponent<UIController>();
                LogFix("✅ Created UIController GameObject");
            }
        }
    }
    
    private void FixPlayerSetup()
    {
        LogFix("Checking player setup...");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Try to find player by name
            player = GameObject.Find("Player");
            if (player != null && player.tag != "Player")
            {
                player.tag = "Player";
                LogFix("✅ Fixed Player tag");
            }
        }
        
        if (player != null)
        {
            // Ensure player has essential components
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = player.AddComponent<PlayerController>();
                LogFix("✅ Added PlayerController to Player");
            }
            
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = player.AddComponent<Rigidbody>();
                LogFix("✅ Added Rigidbody to Player");
            }
            
            SphereCollider collider = player.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = player.AddComponent<SphereCollider>();
                LogFix("✅ Added SphereCollider to Player");
            }
        }
        else
        {
            LogFix("⚠️ No Player found - may need manual setup");
        }
    }
    
    private void FixUI()
    {
        LogFix("Checking UI setup...");
        
        // Ensure Canvas exists
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            LogFix("✅ Created Canvas with CanvasScaler and GraphicRaycaster");
        }
        
        // Ensure EventSystem exists
        UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            LogFix("✅ Created EventSystem");
        }
        
        // Fix Canvas Scaler if missing
        if (canvas != null)
        {
            UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                LogFix("✅ Added CanvasScaler to Canvas");
            }
        }
    }
    
    private void FixCollectibles()
    {
        LogFix("Checking collectibles...");
        
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        int fixedCollectibles = 0;
        
        foreach (CollectibleController collectible in collectibles)
        {
            bool needsFix = false;
            
            // Ensure collectible has correct tag
            if (collectible.gameObject.tag != "Collectible")
            {
                collectible.gameObject.tag = "Collectible";
                needsFix = true;
            }
            
            // Ensure collectible has trigger collider
            Collider collider = collectible.GetComponent<Collider>();
            if (collider == null)
            {
                SphereCollider sphereCollider = collectible.gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                needsFix = true;
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = true;
                needsFix = true;
            }
            
            if (needsFix)
            {
                fixedCollectibles++;
            }
        }
        
        if (fixedCollectibles > 0)
        {
            LogFix($"✅ Fixed {fixedCollectibles} collectibles");
        }
        else if (collectibles.Length > 0)
        {
            LogFix($"✅ All {collectibles.Length} collectibles properly configured");
        }
        else
        {
            LogFix("⚠️ No collectibles found in scene");
        }
    }
    
    private void FixLevelManager()
    {
        LogFix("Checking LevelManager configuration...");
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            // Check if config exists
            if (levelManager.Config == null)
            {
                // Try to create a basic config
                LevelConfiguration config = new LevelConfiguration();
                config.levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                config.totalCollectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None).Length;
                config.collectiblesRemaining = config.totalCollectibles;
                
                // Use reflection to set config if possible
                try
                {
                    var configField = levelManager.GetType().GetField("config", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (configField != null)
                    {
                        configField.SetValue(levelManager, config);
                        LogFix("✅ Created basic LevelConfiguration");
                    }
                }
                catch (System.Exception e)
                {
                    LogFix($"⚠️ Could not set LevelConfiguration: {e.Message}");
                }
            }
            
            // Ensure collectibles are registered
            CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
            LogFix($"Found {collectibles.Length} collectibles for LevelManager");
        }
    }
    
    private void FixGameManager()
    {
        LogFix("Checking GameManager configuration...");
        
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            // Ensure GameManager is marked as DontDestroyOnLoad
            if (gameManager.transform.parent == null)
            {
                DontDestroyOnLoad(gameManager.gameObject);
                LogFix("✅ Set GameManager as DontDestroyOnLoad");
            }
        }
    }
    
    private void FixPrefabReferences()
    {
        LogFix("Checking prefab references...");
        
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator != null)
        {
            bool hasMissingPrefabs = false;
            
            // Use reflection to check prefab fields
            var fields = generator.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject) && field.Name.ToLower().Contains("prefab"))
                {
                    GameObject prefab = (GameObject)field.GetValue(generator);
                    if (prefab == null)
                    {
                        hasMissingPrefabs = true;
                        LogFix($"⚠️ Missing prefab reference: {field.Name}");
                    }
                }
            }
            
            if (!hasMissingPrefabs)
            {
                LogFix("✅ All prefab references valid");
            }
        }
    }
    
    private void FixMaterials()
    {
        LogFix("Checking materials...");
        
        // Look for objects without materials
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int fixedRenderers = 0;
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material == null || renderer.sharedMaterial == null)
            {
                // Try to assign a default material
                Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DefaultMaterial.mat");
                if (defaultMaterial == null)
                {
                    // Create a simple material
                    defaultMaterial = new Material(Shader.Find("Standard"));
                    defaultMaterial.color = Color.white;
                }
                
                renderer.material = defaultMaterial;
                fixedRenderers++;
            }
        }
        
        if (fixedRenderers > 0)
        {
            LogFix($"✅ Fixed {fixedRenderers} renderers with missing materials");
        }
    }
    
    private void FixTags()
    {
        LogFix("Checking tags...");
        
        // Ensure essential tags exist
        string[] requiredTags = { "Player", "Collectible", "Respawn", "Finish", "EditorOnly" };
        
        #if UNITY_EDITOR
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        bool addedTags = false;
        
        foreach (string tag in requiredTags)
        {
            bool tagExists = false;
            
            // Check if tag already exists
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag))
                {
                    tagExists = true;
                    break;
                }
            }
            
            // Add tag if it doesn't exist
            if (!tagExists)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(0);
                newTag.stringValue = tag;
                addedTags = true;
            }
        }
        
        if (addedTags)
        {
            tagManager.ApplyModifiedProperties();
            LogFix("✅ Added missing tags");
        }
        else
        {
            LogFix("✅ All required tags exist");
        }
        #endif
    }
    
    #endregion
    
    #region Validation
    
    private void ValidateScene()
    {
        LogFix("Validating scene after fixes...");
        
        int validations = 0;
        int failures = 0;
        
        // Validate essential GameObjects
        if (GameObject.FindGameObjectWithTag("Player") != null) 
            validations++; 
        else 
            failures++;
            
        if (FindFirstObjectByType<GameManager>() != null) 
            validations++; 
        else 
            failures++;
            
        if (FindFirstObjectByType<Canvas>() != null) 
            validations++; 
        else 
            failures++;
            
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) 
            validations++; 
        else 
            failures++;
        
        // Validate collectibles
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        bool collectiblesValid = true;
        
        foreach (CollectibleController collectible in collectibles)
        {
            if (collectible.gameObject.tag != "Collectible")
            {
                collectiblesValid = false;
                break;
            }
            
            Collider collider = collectible.GetComponent<Collider>();
            if (collider == null || !collider.isTrigger)
            {
                collectiblesValid = false;
                break;
            }
        }
        
        if (collectiblesValid) 
            validations++; 
        else 
            failures++;
        
        LogFix($"Validation Results: {validations} passed, {failures} failed");
        
        if (failures == 0)
        {
            LogFix("🎉 Scene validation completed successfully");
        }
        else
        {
            LogFix("⚠️ Some validation issues remain - manual intervention may be needed");
        }
    }
    
    #endregion
    
    #region Utility
    
    private void LogFix(string message)
    {
        fixLog.Add(message);
    }
    
    /// <summary>
    /// Get the fix log for external access
    /// </summary>
    public List<string> GetFixLog()
    {
        return new List<string>(fixLog);
    }
    
    /// <summary>
    /// Clear the fix log
    /// </summary>
    public void ClearFixLog()
    {
        fixLog.Clear();
    }
    
    #endregion
}
