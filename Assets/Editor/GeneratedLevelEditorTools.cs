using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Editor menu items for quickly fixing Generated Level issues
/// </summary>
public class GeneratedLevelEditorTools
{
    [MenuItem("Roll-a-Ball/Fix Generated Level/Complete Repair")]
    public static void RunCompleteRepair()
    {
        Debug.Log("🚀 Starting complete Generated Level repair...");
        
        // Find or create GeneratedLevelFixer
        GeneratedLevelFixer fixer = Object.FindFirstObjectByType<GeneratedLevelFixer>();
        if (!fixer)
        {
            GameObject fixerGO = new GameObject("GeneratedLevelFixer");
            fixer = fixerGO.AddComponent<GeneratedLevelFixer>();
            Debug.Log("➕ Created GeneratedLevelFixer");
        }
        
        // Run the complete repair
        fixer.StartCoroutine(fixer.FixGeneratedLevelAsync());
        
        Debug.Log("✅ Complete repair initiated!");
    }
    
    [MenuItem("Roll-a-Ball/Fix Generated Level/Fix Collectibles Only")]
    public static void FixCollectiblesOnly()
    {
        Debug.Log("🎯 Fixing collectibles...");
        
        // Find all collectible objects and fix them
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int fixedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (IsCollectibleObject(obj))
            {
                FixCollectible(obj);
                fixedCount++;
            }
        }
        
        Debug.Log($"✅ Fixed {fixedCount} collectibles");
    }
    
    [MenuItem("Roll-a-Ball/Fix Generated Level/Fix Ground Materials")]
    public static void FixGroundMaterials()
    {
        Debug.Log("🎨 Fixing ground materials...");
        
        // Find or create GroundMaterialController
        GroundMaterialController materialController = Object.FindFirstObjectByType<GroundMaterialController>();
        if (!materialController)
        {
            GameObject controllerGO = new GameObject("GroundMaterialController");
            materialController = controllerGO.AddComponent<GroundMaterialController>();
            Debug.Log("➕ Created GroundMaterialController");
        }
        
        // Assign materials
        materialController.AssignMaterials();
        
        Debug.Log("✅ Ground materials fixed!");
    }
    
    [MenuItem("Roll-a-Ball/Fix Generated Level/Validate Scene")]
    public static void ValidateScene()
    {
        Debug.Log("🔍 Running scene validation...");
        
        // Find or create SceneValidator
        SceneValidator validator = Object.FindFirstObjectByType<SceneValidator>();
        if (!validator)
        {
            GameObject validatorGO = new GameObject("SceneValidator");
            validator = validatorGO.AddComponent<SceneValidator>();
            Debug.Log("➕ Created SceneValidator");
        }
        
        // Run validation
        validator.StartCoroutine(validator.ValidateSceneAsync());
        
        Debug.Log("✅ Scene validation initiated!");
    }
    
    [MenuItem("Roll-a-Ball/Fix Generated Level/Create Missing Tags")]
    public static void CreateMissingTags()
    {
        Debug.Log("🏷️ Creating missing tags...");
        
        string[] requiredTags = { "Player", "Collectible", "Finish", "Ground", "Wall" };
        
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        int addedTags = 0;
        
        foreach (string requiredTag in requiredTags)
        {
            bool tagExists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == requiredTag)
                {
                    tagExists = true;
                    break;
                }
            }
            
            if (!tagExists)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                tagsProp.GetArrayElementAtIndex(0).stringValue = requiredTag;
                addedTags++;
                Debug.Log($"➕ Added tag: {requiredTag}");
            }
        }
        
        if (addedTags > 0)
        {
            tagManager.ApplyModifiedProperties();
            Debug.Log($"✅ Added {addedTags} missing tags");
        }
        else
        {
            Debug.Log("✅ All required tags already exist");
        }
    }
    
    [MenuItem("Roll-a-Ball/Fix Generated Level/Setup Scene Managers")]
    public static void SetupSceneManagers()
    {
        Debug.Log("⚙️ Setting up scene managers...");
        
        int createdManagers = 0;
        
        // GameManager
        if (!Object.FindFirstObjectByType<GameManager>())
        {
            GameObject gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
            createdManagers++;
            Debug.Log("➕ Created GameManager");
        }
        
        // LevelManager
        if (!Object.FindFirstObjectByType<LevelManager>())
        {
            GameObject levelManagerGO = new GameObject("LevelManager");
            levelManagerGO.AddComponent<LevelManager>();
            createdManagers++;
            Debug.Log("➕ Created LevelManager");
        }
        
        // UIController
        if (!Object.FindFirstObjectByType<UIController>())
        {
            // Create Canvas first
            GameObject canvasGO = new GameObject("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add UIController
            canvasGO.AddComponent<UIController>();
            createdManagers++;
            Debug.Log("➕ Created UIController with Canvas");
        }
        
        Debug.Log($"✅ Setup complete - created {createdManagers} managers");
    }
    
    [MenuItem("Roll-a-Ball/Debug/Log Scene Statistics")]
    public static void LogSceneStatistics()
    {
        Debug.Log("📊 Scene Statistics:");
        
        // Count objects by type
        var collectibles = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        var gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        Debug.Log($"   GameObjects: {gameObjects.Length}");
        Debug.Log($"   Collectibles: {collectibles.Length}");
        Debug.Log($"   Renderers: {renderers.Length}");
        Debug.Log($"   AudioSources: {audioSources.Length}");
        
        // Check for Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log($"   Player: {(player ? "✅ Found" : "❌ Missing")}");
        
        // Check for essential managers
        bool hasGameManager = Object.FindFirstObjectByType<GameManager>();
        bool hasLevelManager = Object.FindFirstObjectByType<LevelManager>();
        bool hasUIController = Object.FindFirstObjectByType<UIController>();
        
        Debug.Log($"   GameManager: {(hasGameManager ? "✅" : "❌")}");
        Debug.Log($"   LevelManager: {(hasLevelManager ? "✅" : "❌")}");
        Debug.Log($"   UIController: {(hasUIController ? "✅" : "❌")}");
        
        Debug.Log("📊 Statistics complete!");
    }
    
    // Helper methods
    private static bool IsCollectibleObject(GameObject obj)
    {
        string name = obj.name.ToLower();
        return name.Contains("collectible") || name.Contains("pickup") || 
               obj.CompareTag("Collectible") || obj.GetComponent<CollectibleController>();
    }
    
    private static void FixCollectible(GameObject collectible)
    {
        // Add CollectibleController if missing
        CollectibleController controller = collectible.GetComponent<CollectibleController>();
        if (!controller)
        {
            controller = collectible.AddComponent<CollectibleController>();
        }
        
        // Add/fix collider
        Collider collider = collectible.GetComponent<Collider>();
        if (!collider)
        {
            SphereCollider sphere = collectible.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }
        else
        {
            collider.isTrigger = true;
        }
        
        // Fix tag
        if (!collectible.CompareTag("Collectible"))
        {
            collectible.tag = "Collectible";
        }
    }
}
