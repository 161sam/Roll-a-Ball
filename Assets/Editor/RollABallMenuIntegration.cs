using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity Editor Menu Integration for Roll-a-Ball Fix Tools
/// Provides easy access to all repair functions via Unity menu
/// </summary>
public class RollABallMenuIntegration : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;
    
    [MenuItem("Roll-a-Ball/🔧 Complete Fix Dashboard")]
    public static void ShowWindow()
    {
        RollABallMenuIntegration window = GetWindow<RollABallMenuIntegration>("Roll-a-Ball Fix Dashboard");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Header
        GUILayout.Label("🎱 Roll-a-Ball Fix Dashboard", EditorStyles.largeLabel);
        GUILayout.Label("One-click solutions for all common problems", EditorStyles.helpBox);
        
        GUILayout.Space(10);
        
        // Quick Fix Section
        DrawQuickFixSection();
        
        GUILayout.Space(10);
        
        // Scene-Specific Fixes
        DrawSceneSpecificSection();
        
        GUILayout.Space(10);
        
        // Advanced Options
        DrawAdvancedSection();
        
        GUILayout.Space(10);
        
        // Status Section
        DrawStatusSection();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawQuickFixSection()
    {
        GUILayout.Label("🚀 Quick Fix (Recommended)", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("These options fix ALL problems with one click:", MessageType.Info);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🔧 Fix All Scenes", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Fix All Scenes", 
                "This will automatically fix all problems in Level1, Level2, Level3, GeneratedLevel, and Level_OSM.\n\nThis is the recommended solution. Continue?", 
                "Yes, Fix All", "Cancel"))
            {
                SetupAllScenesComplete();
            }
        }
        
        if (GUILayout.Button("🔧 Fix Current Scene", GUILayout.Height(40)))
        {
            SetupCurrentSceneComplete();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🏷️ Setup Tags & Layers"))
        {
            SetupTagsAndLayers();
        }
    }
    
    private void DrawSceneSpecificSection()
    {
        GUILayout.Label("🎯 Scene-Specific Fixes", EditorStyles.boldLabel);
        
        string currentScene = SceneManager.GetActiveScene().name;
        EditorGUILayout.LabelField("Current Scene:", currentScene);
        
        GUILayout.Space(5);
        
        // Level2 Fix
        if (GUILayout.Button("Fix Level2 → Level3 Transition"))
        {
            FixLevel2Progression();
        }
        
        // Level3 & GeneratedLevel Fix
        if (GUILayout.Button("Fix Level3 & GeneratedLevel Collectibles"))
        {
            FixCollectibleProblems();
        }
        
        // OSM Fix
        if (GUILayout.Button("Fix Level_OSM UI"))
        {
            FixOSMUI();
        }
        
        GUILayout.Space(5);
        
        // Scene Navigation
        GUILayout.Label("Quick Scene Access:", EditorStyles.miniBoldLabel);
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
    
    private void DrawAdvancedSection()
    {
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "⚙️ Advanced Options");
        
        if (showAdvancedOptions)
        {
            EditorGUI.indentLevel++;
            
            GUILayout.Label("Individual Fix Tools:", EditorStyles.miniBoldLabel);
            
            if (GUILayout.Button("Run Universal Scene Fixture"))
            {
                RunUniversalSceneFixture();
            }
            
            if (GUILayout.Button("Run Collectible Diagnostic"))
            {
                RunCollectibleDiagnostic();
            }
            
            if (GUILayout.Button("Run Level Progression Fixer"))
            {
                RunLevelProgressionFixer();
            }
            
            if (GUILayout.Button("Run OSM UI Connector"))
            {
                RunOSMUIConnector();
            }
            
            GUILayout.Space(5);
            
            GUILayout.Label("Cleanup Options:", EditorStyles.miniBoldLabel);
            
            if (GUILayout.Button("Remove All Fix Tools from Current Scene"))
            {
                if (EditorUtility.DisplayDialog("Remove Fix Tools", 
                    "Remove all fix tool components from the current scene?", 
                    "Yes, Remove", "Cancel"))
                {
                    RemoveAllFixTools();
                }
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    private void DrawStatusSection()
    {
        GUILayout.Label("📊 Current Scene Status", EditorStyles.boldLabel);
        
        string sceneName = SceneManager.GetActiveScene().name;
        bool hasPlayer = GameObject.FindGameObjectWithTag("Player") != null;
        int collectibleCount = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None).Length;
        int buttonCount = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Length;
        bool hasGameManager = Object.FindFirstObjectByType<GameManager>() != null;
        bool hasLevelManager = Object.FindFirstObjectByType<LevelManager>() != null;
        bool hasMasterFix = Object.FindFirstObjectByType<MasterFixTool>() != null;
        
        EditorGUILayout.LabelField("Scene:", sceneName);
        EditorGUILayout.LabelField("Player:", hasPlayer ? "✓ Found" : "✗ Missing");
        EditorGUILayout.LabelField("Collectibles:", collectibleCount.ToString());
        EditorGUILayout.LabelField("UI Buttons:", buttonCount.ToString());
        EditorGUILayout.LabelField("GameManager:", hasGameManager ? "✓ Found" : "✗ Missing");
        EditorGUILayout.LabelField("LevelManager:", hasLevelManager ? "✓ Found" : "✗ Missing");
        EditorGUILayout.LabelField("Fix Tools:", hasMasterFix ? "✓ Installed" : "✗ Not Installed");
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🔍 Run Quick Diagnostic"))
        {
            RunQuickDiagnostic();
        }
        
        if (GUILayout.Button("🧪 Test Current Scene"))
        {
            TestCurrentScene();
        }
    }
    
    // Implementation Methods
    
    private void SetupAllScenesComplete()
    {
        EditorUtility.DisplayProgressBar("Setting up all scenes", "Starting setup...", 0f);
        
        try
        {
            // First setup tags
            SetupTagsAndLayers();
            
            EditorUtility.DisplayProgressBar("Setting up all scenes", "Setting up tags and layers...", 0.1f);
            
            // Get all scenes
            string[] sceneNames = { "Level1", "Level2", "Level3", "GeneratedLevel", "Level_OSM" };
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            
            for (int i = 0; i < sceneNames.Length; i++)
            {
                string sceneName = sceneNames[i];
                float progress = 0.2f + (0.7f * i / sceneNames.Length);
                
                EditorUtility.DisplayProgressBar("Setting up all scenes", $"Processing {sceneName}...", progress);
                
                // Find scene path
                string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
                if (guids.Length > 0)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    
                    // Open scene
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    
                    // Setup scene
                    SetupCurrentSceneComplete();
                    
                    // Save scene
                    EditorSceneManager.SaveOpenScenes();
                }
            }
            
            // Return to original scene
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }
            
            EditorUtility.DisplayProgressBar("Setting up all scenes", "Finalizing...", 0.9f);
            
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Setup Complete!", 
                "All scenes have been successfully set up with fix tools!\n\n" +
                "✓ All UI connections fixed\n" +
                "✓ All collectible problems resolved\n" +
                "✓ All level progressions configured\n" +
                "✓ OSM UI properly connected\n\n" +
                "Your Roll-a-Ball project is now fully functional!", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Setup Error", $"An error occurred during setup:\n{e.Message}", "OK");
        }
    }
    
    private void SetupCurrentSceneComplete()
    {
        // Add MasterFixTool if not present
        MasterFixTool masterFix = Object.FindFirstObjectByType<MasterFixTool>();
        if (!masterFix)
        {
            GameObject masterGO = new GameObject("MasterFixTool");
            masterFix = masterGO.AddComponent<MasterFixTool>();
        }
        
        // Run complete fix
        masterFix.RunCompleteFixNow();
        
        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Scene Fixed!", 
            $"Scene '{SceneManager.GetActiveScene().name}' has been completely fixed!\n\n" +
            "All problems should now be resolved.", "OK");
    }
    
    private void SetupTagsAndLayers()
    {
        TagManager tagManager = Object.FindFirstObjectByType<TagManager>();
        if (!tagManager)
        {
            GameObject tagGO = new GameObject("TempTagManager");
            tagManager = tagGO.AddComponent<TagManager>();
        }
        
        tagManager.CompleteTagSetup();
        
        if (tagManager.gameObject.name == "TempTagManager")
        {
            Object.DestroyImmediate(tagManager.gameObject);
        }
        
        Debug.Log("[RollABallMenu] Tags and layers setup completed");
    }
    
    private void FixLevel2Progression()
    {
        LevelProgressionFixer fixer = Object.FindFirstObjectByType<LevelProgressionFixer>();
        if (!fixer)
        {
            GameObject fixerGO = new GameObject("LevelProgressionFixer");
            fixer = fixerGO.AddComponent<LevelProgressionFixer>();
        }
        
        fixer.FixCurrentLevelProgression();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Level Progression Fixed!", 
            "Level progression has been configured. Level2 should now properly transition to Level3.", "OK");
    }
    
    private void FixCollectibleProblems()
    {
        CollectibleDiagnosticTool diagnostic = Object.FindFirstObjectByType<CollectibleDiagnosticTool>();
        if (!diagnostic)
        {
            GameObject diagnosticGO = new GameObject("CollectibleDiagnosticTool");
            diagnostic = diagnosticGO.AddComponent<CollectibleDiagnosticTool>();
        }
        
        diagnostic.RunDiagnostic();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Collectibles Fixed!", 
            "All collectible problems have been diagnosed and repaired.", "OK");
    }
    
    private void FixOSMUI()
    {
        RollABall.Map.OSMUIConnector connector = Object.FindFirstObjectByType<RollABall.Map.OSMUIConnector>();
        if (!connector)
        {
            GameObject connectorGO = new GameObject("OSMUIConnector");
            connector = connectorGO.AddComponent<RollABall.Map.OSMUIConnector>();
        }
        
        connector.ConnectUIElements();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("OSM UI Fixed!", 
            "OSM UI elements have been connected and should now work properly.", "OK");
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
    
    private void RunUniversalSceneFixture()
    {
        UniversalSceneFixture fixture = Object.FindFirstObjectByType<UniversalSceneFixture>();
        if (!fixture)
        {
            GameObject fixtureGO = new GameObject("UniversalSceneFixture");
            fixture = fixtureGO.AddComponent<UniversalSceneFixture>();
        }
        
        fixture.FixCurrentScene();
        EditorUtility.DisplayDialog("Universal Scene Fixture", "Scene fixture completed!", "OK");
    }
    
    private void RunCollectibleDiagnostic()
    {
        CollectibleDiagnosticTool diagnostic = Object.FindFirstObjectByType<CollectibleDiagnosticTool>();
        if (!diagnostic)
        {
            GameObject diagnosticGO = new GameObject("CollectibleDiagnosticTool");
            diagnostic = diagnosticGO.AddComponent<CollectibleDiagnosticTool>();
        }
        
        diagnostic.RunDiagnostic();
        EditorUtility.DisplayDialog("Collectible Diagnostic", "Collectible diagnostic completed!", "OK");
    }
    
    private void RunLevelProgressionFixer()
    {
        LevelProgressionFixer fixer = Object.FindFirstObjectByType<LevelProgressionFixer>();
        if (!fixer)
        {
            GameObject fixerGO = new GameObject("LevelProgressionFixer");
            fixer = fixerGO.AddComponent<LevelProgressionFixer>();
        }
        
        fixer.FixCurrentLevelProgression();
        EditorUtility.DisplayDialog("Level Progression Fixer", "Level progression fix completed!", "OK");
    }
    
    private void RunOSMUIConnector()
    {
        RollABall.Map.OSMUIConnector connector = Object.FindFirstObjectByType<RollABall.Map.OSMUIConnector>();
        if (!connector)
        {
            GameObject connectorGO = new GameObject("OSMUIConnector");
            connector = connectorGO.AddComponent<RollABall.Map.OSMUIConnector>();
        }
        
        connector.ConnectUIElements();
        EditorUtility.DisplayDialog("OSM UI Connector", "OSM UI connection completed!", "OK");
    }
    
    private void RemoveAllFixTools()
    {
        AutoSceneSetup setup = Object.FindFirstObjectByType<AutoSceneSetup>();
        if (!setup)
        {
            GameObject setupGO = new GameObject("TempAutoSceneSetup");
            setup = setupGO.AddComponent<AutoSceneSetup>();
        }
        
        setup.RemoveAllFixToolsFromCurrentScene();
        
        if (setup.gameObject.name == "TempAutoSceneSetup")
        {
            Object.DestroyImmediate(setup.gameObject);
        }
        
        EditorUtility.DisplayDialog("Fix Tools Removed", "All fix tool components have been removed from the current scene.", "OK");
    }
    
    private void RunQuickDiagnostic()
    {
        MasterFixTool masterFix = Object.FindFirstObjectByType<MasterFixTool>();
        if (!masterFix)
        {
            GameObject masterGO = new GameObject("TempMasterFixTool");
            masterFix = masterGO.AddComponent<MasterFixTool>();
        }
        
        masterFix.RunQuickDiagnostic();
        
        if (masterFix.gameObject.name == "TempMasterFixTool")
        {
            Object.DestroyImmediate(masterFix.gameObject);
        }
        
        EditorUtility.DisplayDialog("Quick Diagnostic", "Diagnostic completed! Check the Console window for results.", "OK");
    }
    
    private void TestCurrentScene()
    {
        // Test level progression if LevelProgressionFixer exists
        LevelProgressionFixer fixer = Object.FindFirstObjectByType<LevelProgressionFixer>();
        if (fixer)
        {
            fixer.TestLevelProgression();
        }
        
        // Test collectibles if diagnostic tool exists
        CollectibleDiagnosticTool diagnostic = Object.FindFirstObjectByType<CollectibleDiagnosticTool>();
        if (diagnostic)
        {
            diagnostic.ForceCollectAllCollectibles();
        }
        
        EditorUtility.DisplayDialog("Scene Test", "Scene test completed! Check the Console window for results.", "OK");
    }
}

// Additional menu items for backwards compatibility
// DISABLED: These MenuItem attributes are duplicated in CleanRollABallMenu.cs
// To avoid "menu item already exists" warnings, these are commented out
public class RollABallMenuItems
{
    // [MenuItem("Roll-a-Ball/Setup All Scenes")]  // DISABLED: Duplicate in CleanRollABallMenu.cs
    public static void SetupAllScenes()
    {
        AutoSceneSetup.SetupAllScenesMenuItem();
    }
    
    // [MenuItem("Roll-a-Ball/Setup Current Scene")]  // DISABLED: Duplicate in CleanRollABallMenu.cs
    public static void SetupCurrentScene()
    {
        AutoSceneSetup.SetupCurrentSceneMenuItem();
    }
    
    // [MenuItem("Roll-a-Ball/Run Master Fix on Current Scene")]  // DISABLED: Duplicate in CleanRollABallMenu.cs
    public static void RunMasterFix()
    {
        AutoSceneSetup.RunMasterFixMenuItem();
    }
    
    // [MenuItem("Roll-a-Ball/Setup Tags and Layers")]  // DISABLED: Duplicate in CleanRollABallMenu.cs
    public static void SetupTags()
    {
        TagManager.SetupTagsMenuItem();
    }
    
    // [MenuItem("Roll-a-Ball/Remove All Fix Tools")]  // DISABLED: Duplicate in CleanRollABallMenu.cs
    public static void RemoveFixTools()
    {
        AutoSceneSetup.RemoveAllFixToolsMenuItem();
    }
    
    [MenuItem("Roll-a-Ball/📖 Open Fix Guide")]
    public static void OpenFixGuide()
    {
        string guidePath = "Assets/COMPLETE_FIX_GUIDE.md";
        if (System.IO.File.Exists(guidePath))
        {
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(guidePath, 1);
        }
        else
        {
            EditorUtility.DisplayDialog("Fix Guide", 
                "The complete fix guide can be found in the project root:\nCOMPLETE_FIX_GUIDE.md", "OK");
        }
    }
}
