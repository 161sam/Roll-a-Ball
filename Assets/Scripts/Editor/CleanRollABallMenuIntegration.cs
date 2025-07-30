using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Clean Roll-a-Ball Menu Integration without UniversalSceneFixture dependencies
/// </summary>
public class CleanRollABallMenuIntegration : EditorWindow
{
    private Vector2 scrollPosition;
    
    [MenuItem("Roll-a-Ball/🔧 Clean Fix Dashboard")]
    public static void ShowWindow()
    {
        CleanRollABallMenuIntegration window = GetWindow<CleanRollABallMenuIntegration>("Clean Roll-a-Ball Fix Dashboard");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Header
        GUILayout.Label("🎱 Clean Roll-a-Ball Fix Dashboard", EditorStyles.largeLabel);
        GUILayout.Label("Compilation error-free fix tools", EditorStyles.helpBox);
        
        GUILayout.Space(10);
        
        // Quick Fix Section
        DrawQuickFixSection();
        
        GUILayout.Space(10);
        
        // Scene Navigation
        DrawSceneNavigationSection();
        
        GUILayout.Space(10);
        
        // Status Section
        DrawStatusSection();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawQuickFixSection()
    {
        GUILayout.Label("🚀 Quick Fix Tools", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("These tools fix common problems without compilation errors:", MessageType.Info);
        
        if (GUILayout.Button("🔧 Fix Current Scene", GUILayout.Height(30)))
        {
            FixCurrentSceneClean();
        }
        
        if (GUILayout.Button("🏷️ Setup Tags & Layers", GUILayout.Height(30)))
        {
            SetupTagsAndLayers();
        }
        
        if (GUILayout.Button("🎯 Fix Collectibles", GUILayout.Height(30)))
        {
            FixCollectibles();
        }
        
        if (GUILayout.Button("📱 Fix UI", GUILayout.Height(30)))
        {
            FixUIElements();
        }
    }
    
    private void DrawSceneNavigationSection()
    {
        GUILayout.Label("🎯 Scene Navigation", EditorStyles.boldLabel);
        
        string currentScene = SceneManager.GetActiveScene().name;
        EditorGUILayout.LabelField("Current Scene:", currentScene);
        
        GUILayout.Space(5);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Level1")) OpenScene("Level1");
        if (GUILayout.Button("Level2")) OpenScene("Level2");
        if (GUILayout.Button("Level3")) OpenScene("Level3");
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("GeneratedLevel")) OpenScene("GeneratedLevel");
        if (GUILayout.Button("Level_OSM")) OpenScene("Level_OSM");
        GUILayout.EndHorizontal();
    }
    
    private void DrawStatusSection()
    {
        GUILayout.Label("📊 Current Scene Status", EditorStyles.boldLabel);
        
        string sceneName = SceneManager.GetActiveScene().name;
        bool hasPlayer = GameObject.FindGameObjectWithTag("Player") != null;
        int collectibleCount = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None).Length;
        bool hasGameManager = Object.FindFirstObjectByType<GameManager>() != null;
        bool hasLevelManager = Object.FindFirstObjectByType<LevelManager>() != null;
        
        EditorGUILayout.LabelField("Scene:", sceneName);
        EditorGUILayout.LabelField("Player:", hasPlayer ? "✓ Found" : "✗ Missing");
        EditorGUILayout.LabelField("Collectibles:", collectibleCount.ToString());
        EditorGUILayout.LabelField("GameManager:", hasGameManager ? "✓ Found" : "✗ Missing");
        EditorGUILayout.LabelField("LevelManager:", hasLevelManager ? "✓ Found" : "✗ Missing");
    }
    
    // Implementation Methods
    
    private void FixCurrentSceneClean()
    {
        try 
        {
            // Basic scene fixes without UniversalSceneFixture dependencies
            FixEssentialComponents();
            FixPlayerSetup();
            FixUIElements();
            FixCollectibles();
            
            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            EditorUtility.DisplayDialog("Scene Fixed!", 
                $"Scene '{SceneManager.GetActiveScene().name}' has been fixed!\n\n" +
                "✓ Essential components checked\n" +
                "✓ Player setup verified\n" +
                "✓ UI elements fixed\n" +
                "✓ Collectibles configured", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Error fixing scene: {e.Message}", "OK");
        }
    }
    
    private void FixEssentialComponents()
    {
        // Ensure GameManager exists
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            GameObject gameManagerGO = new GameObject("GameManager");
            gameManager = gameManagerGO.AddComponent<GameManager>();
            Debug.Log("[CleanMenu] Created GameManager");
        }
        
        // Ensure LevelManager exists in non-OSM scenes
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Level_OSM")
        {
            LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
            if (levelManager == null)
            {
                GameObject levelManagerGO = new GameObject("LevelManager");
                levelManager = levelManagerGO.AddComponent<LevelManager>();
                Debug.Log("[CleanMenu] Created LevelManager");
            }
        }
    }
    
    private void FixPlayerSetup()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
            if (player != null && player.tag != "Player")
            {
                player.tag = "Player";
                Debug.Log("[CleanMenu] Fixed Player tag");
            }
        }
        
        if (player != null)
        {
            // Ensure player has essential components
            if (player.GetComponent<PlayerController>() == null)
            {
                player.AddComponent<PlayerController>();
                Debug.Log("[CleanMenu] Added PlayerController");
            }
            
            if (player.GetComponent<Rigidbody>() == null)
            {
                player.AddComponent<Rigidbody>();
                Debug.Log("[CleanMenu] Added Rigidbody");
            }
            
            if (player.GetComponent<SphereCollider>() == null)
            {
                player.AddComponent<SphereCollider>();
                Debug.Log("[CleanMenu] Added SphereCollider");
            }
        }
    }
    
    private void FixUIElements()
    {
        // Ensure Canvas exists
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("[CleanMenu] Created Canvas");
        }
        
        // Ensure EventSystem exists
        UnityEngine.EventSystems.EventSystem eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[CleanMenu] Created EventSystem");
        }
    }
    
    private void FixCollectibles()
    {
        CollectibleController[] collectibles = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        int fixedCount = 0;
        
        foreach (CollectibleController collectible in collectibles)
        {
            bool needsFix = false;
            
            if (collectible.tag != "Collectible")
            {
                collectible.tag = "Collectible";
                needsFix = true;
            }
            
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
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"[CleanMenu] Fixed {fixedCount} collectibles");
        }
    }
    
    private void SetupTagsAndLayers()
    {
        TagManager tagManager = Object.FindFirstObjectByType<TagManager>();
        if (tagManager == null)
        {
            GameObject tagGO = new GameObject("TempTagManager");
            tagManager = tagGO.AddComponent<TagManager>();
        }
        
        tagManager.CompleteTagSetup();
        
        if (tagManager.gameObject.name == "TempTagManager")
        {
            Object.DestroyImmediate(tagManager.gameObject);
        }
        
        Debug.Log("[CleanMenu] Tags and layers setup completed");
        EditorUtility.DisplayDialog("Tags Setup", "Tags and layers have been configured!", "OK");
    }
    
    private void OpenScene(string sceneName)
    {
        string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
        if (guids.Length > 0)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
        else
        {
            EditorUtility.DisplayDialog("Scene Not Found", $"Could not find scene: {sceneName}", "OK");
        }
    }
}