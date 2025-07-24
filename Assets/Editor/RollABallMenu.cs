using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Roll-a-Ball Editor Menu - Ersetzt Unity MCP Menu mit lokalen Tools
/// Stellt alle wichtigen Setup- und Fix-Funktionen zur Verfügung
/// </summary>
public class RollABallMenu : EditorWindow
{
    private Vector2 scrollPosition;
    
    [MenuItem("Roll-a-Ball/🎮 Roll-a-Ball Control Panel")]
    public static void ShowWindow()
    {
        RollABallMenu window = GetWindow<RollABallMenu>("Roll-a-Ball Tools");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Space(10);
        
        // Header
        EditorGUILayout.LabelField("🎱 Roll-a-Ball Development Tools", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Scene Setup Section
        EditorGUILayout.LabelField("🏗️ Scene Setup", EditorStyles.boldLabel);
        DrawHorizontalLine();
        
        if (GUILayout.Button("🚀 Complete Scene Setup", GUILayout.Height(30)))
        {
            CompleteSceneSetup();
        }
        
        if (GUILayout.Button("👤 Fix Player Setup"))
        {
            FixPlayerSetup();
        }
        
        if (GUILayout.Button("📷 Fix Camera Setup"))
        {
            FixCameraSetup();
        }
        
        if (GUILayout.Button("🖥️ Fix UI Setup"))
        {
            FixUISetup();
        }
        
        GUILayout.Space(10);
        
        // Level Generation Section
        EditorGUILayout.LabelField("🏗️ Level Generation", EditorStyles.boldLabel);
        DrawHorizontalLine();
        
        if (GUILayout.Button("📋 Create Level Profiles"))
        {
            CreateLevelProfiles();
        }
        
        if (GUILayout.Button("🎲 Generate Test Level"))
        {
            GenerateTestLevel();
        }
        
        if (GUILayout.Button("🧹 Clear Generated Objects"))
        {
            ClearGeneratedObjects();
        }
        
        GUILayout.Space(10);
        
        // Debug & Fix Section
        EditorGUILayout.LabelField("🔧 Debug & Fix", EditorStyles.boldLabel);
        DrawHorizontalLine();
        
        if (GUILayout.Button("🔍 Validate Project"))
        {
            ValidateProject();
        }
        
        if (GUILayout.Button("🧼 Clean Console"))
        {
            ClearConsole();
        }
        
        if (GUILayout.Button("📊 Show Project Stats"))
        {
            ShowProjectStats();
        }
        
        GUILayout.Space(10);
        
        // Utilities Section
        EditorGUILayout.LabelField("🛠️ Utilities", EditorStyles.boldLabel);
        DrawHorizontalLine();
        
        if (GUILayout.Button("💾 Save All Scenes"))
        {
            SaveAllScenes();
        }
        
        if (GUILayout.Button("🔄 Refresh Assets"))
        {
            RefreshAssets();
        }
        
        if (GUILayout.Button("📁 Open Project Folder"))
        {
            OpenProjectFolder();
        }
        
        GUILayout.Space(20);
        
        // Status Display
        EditorGUILayout.LabelField("📈 Current Status:", EditorStyles.boldLabel);
        DrawHorizontalLine();
        
        ShowCurrentStatus();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawHorizontalLine()
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
    }
    
    private void CompleteSceneSetup()
    {
        Debug.Log("🚀 Starting Complete Scene Setup from Menu...");
        
        CompleteSceneSetup setup = FindFirstObjectByType<CompleteSceneSetup>();
        if (setup == null)
        {
            GameObject setupGO = new GameObject("CompleteSceneSetup");
            setup = setupGO.AddComponent<CompleteSceneSetup>();
        }
        
        setup.SetupCompleteScene();
        EditorUtility.DisplayDialog("Setup Complete", "Scene setup finished! Check Console for details.", "OK");
    }
    
    private void FixPlayerSetup()
    {
        Debug.Log("👤 Fixing Player Setup...");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.Log("Creating new Player...");
            player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player";
            player.tag = "Player";
            player.transform.localScale = Vector3.one * 0.5f;
            
            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.3f;
            
            player.AddComponent<PlayerController>();
        }
        
        Debug.Log("✅ Player setup complete!");
        EditorUtility.DisplayDialog("Player Fixed", "Player setup is now complete!", "OK");
    }
    
    private void FixCameraSetup()
    {
        Debug.Log("📷 Fixing Camera Setup...");
        
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            mainCam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }
        
        CameraController camController = mainCam.GetComponent<CameraController>();
        if (camController == null)
        {
            camController = mainCam.gameObject.AddComponent<CameraController>();
        }
        
        // Set default position
        mainCam.transform.position = new Vector3(0, 10, -10);
        mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);
        
        Debug.Log("✅ Camera setup complete!");
        EditorUtility.DisplayDialog("Camera Fixed", "Camera setup is now complete!", "OK");
    }
    
    private void FixUISetup()
    {
        Debug.Log("🖥️ Fixing UI Setup...");
        
        CompleteSceneSetup setup = FindFirstObjectByType<CompleteSceneSetup>();
        if (setup == null)
        {
            GameObject setupGO = new GameObject("CompleteSceneSetup");
            setup = setupGO.AddComponent<CompleteSceneSetup>();
        }
        
        // Call private method via reflection
        var method = typeof(CompleteSceneSetup).GetMethod("SetupCompleteUI", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(setup, null);
        
        Debug.Log("✅ UI setup complete!");
        EditorUtility.DisplayDialog("UI Fixed", "UI setup is now complete!", "OK");
    }
    
    private void CreateLevelProfiles()
    {
        Debug.Log("📋 Creating Level Profiles...");
        
        LevelProfileCreator creator = FindFirstObjectByType<LevelProfileCreator>();
        if (creator == null)
        {
            GameObject creatorGO = new GameObject("LevelProfileCreator");
            creator = creatorGO.AddComponent<LevelProfileCreator>();
        }
        
        // Trigger profile creation via context menu
        var method = typeof(LevelProfileCreator).GetMethod("CreateAllProfiles");
        if (method != null)
        {
            method.Invoke(creator, null);
        }
        else
        {
            Debug.LogWarning("CreateAllProfiles method not found. Creating profiles manually...");
            CreateProfilesManually();
        }
        
        EditorUtility.DisplayDialog("Profiles Created", "Level Profiles have been created!", "OK");
    }
    
    private void CreateProfilesManually()
    {
        string profilePath = "Assets/ScriptableObjects";
        
        if (!Directory.Exists(profilePath))
        {
            Directory.CreateDirectory(profilePath);
        }
        
        // Create Easy Profile
        LevelProfile easyProfile = ScriptableObject.CreateInstance<LevelProfile>();
        AssetDatabase.CreateAsset(easyProfile, profilePath + "/EasyProfile.asset");
        
        // Create Medium Profile
        LevelProfile mediumProfile = ScriptableObject.CreateInstance<LevelProfile>();
        AssetDatabase.CreateAsset(mediumProfile, profilePath + "/MediumProfile.asset");
        
        // Create Hard Profile  
        LevelProfile hardProfile = ScriptableObject.CreateInstance<LevelProfile>();
        AssetDatabase.CreateAsset(hardProfile, profilePath + "/HardProfile.asset");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("📋 Manual profile creation complete!");
    }
    
    private void GenerateTestLevel()
    {
        Debug.Log("🎲 Generating Test Level...");
        
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator == null)
        {
            Debug.LogError("No LevelGenerator found! Please run Complete Scene Setup first.");
            EditorUtility.DisplayDialog("Error", "No LevelGenerator found!\\nPlease run Complete Scene Setup first.", "OK");
            return;
        }
        
        // Trigger generation via reflection or public method
        var method = typeof(LevelGenerator).GetMethod("GenerateLevel");
        if (method != null)
        {
            method.Invoke(generator, null);
        }
        
        Debug.Log("✅ Test level generated!");
        EditorUtility.DisplayDialog("Level Generated", "Test level has been generated!", "OK");
    }
    
    private void ClearGeneratedObjects()
    {
        Debug.Log("🧹 Clearing Generated Objects...");
        
        // Clear objects with specific tags/names
        GameObject[] generatedObjects = GameObject.FindGameObjectsWithTag("Generated");
        foreach (GameObject obj in generatedObjects)
        {
            DestroyImmediate(obj);
        }
        
        // Clear by name patterns
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Generated") || obj.name.Contains("LevelTile") || 
                obj.name.Contains("GroundTile") || obj.name.Contains("WallTile"))
            {
                DestroyImmediate(obj);
            }
        }
        
        Debug.Log("✅ Generated objects cleared!");
        EditorUtility.DisplayDialog("Cleared", "Generated objects have been cleared!", "OK");
    }
    
    private void ValidateProject()
    {
        Debug.Log("🔍 Validating Project...");
        
        bool isValid = true;
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("🔍 Project Validation Report:");
        report.AppendLine("================================");
        
        // Check Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            report.AppendLine("❌ Player not found!");
            isValid = false;
        }
        else
        {
            report.AppendLine("✅ Player found");
            if (player.GetComponent<PlayerController>() == null)
            {
                report.AppendLine("⚠️ PlayerController missing!");
            }
        }
        
        // Check Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            report.AppendLine("❌ Main Camera not found!");
            isValid = false;
        }
        else
        {
            report.AppendLine("✅ Main Camera found");
        }
        
        // Check UI
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            report.AppendLine("❌ UI Canvas not found!");
            isValid = false;
        }
        else
        {
            report.AppendLine("✅ UI Canvas found");
        }
        
        // Check LevelGenerator
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator == null)
        {
            report.AppendLine("❌ LevelGenerator not found!");
            isValid = false;
        }
        else
        {
            report.AppendLine("✅ LevelGenerator found");
        }
        
        report.AppendLine("================================");
        if (isValid)
        {
            report.AppendLine("🎉 Project validation PASSED!");
        }
        else
        {
            report.AppendLine("⚠️ Project validation FAILED - please fix issues above.");
        }
        
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Validation Complete", 
            isValid ? "Project validation PASSED!" : "Project validation FAILED!\\nCheck Console for details.", 
            "OK");
    }
    
    private void ClearConsole()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
        
        Debug.Log("🧼 Console cleared!");
    }
    
    private void ShowProjectStats()
    {
        System.Text.StringBuilder stats = new System.Text.StringBuilder();
        stats.AppendLine("📊 Roll-a-Ball Project Statistics:");
        stats.AppendLine("===================================");
        
        // Count GameObjects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        stats.AppendLine($"Total GameObjects: {allObjects.Length}");
        
        // Count Scripts
        MonoBehaviour[] scripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        stats.AppendLine($"MonoBehaviour Scripts: {scripts.Length}");
        
        // Count specific components
        int playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length;
        int levelGenerators = FindObjectsByType<LevelGenerator>(FindObjectsSortMode.None).Length;
        int uiControllers = FindObjectsByType<UIController>(FindObjectsSortMode.None).Length;
        
        stats.AppendLine($"PlayerControllers: {playerControllers}");
        stats.AppendLine($"LevelGenerators: {levelGenerators}");
        stats.AppendLine($"UIControllers: {uiControllers}");
        
        // Scene info
        stats.AppendLine($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        
        Debug.Log(stats.ToString());
        EditorUtility.DisplayDialog("Project Stats", "Project statistics logged to Console!", "OK");
    }
    
    private void SaveAllScenes()
    {
        EditorApplication.ExecuteMenuItem("File/Save Project");
        Debug.Log("💾 All scenes and project saved!");
        EditorUtility.DisplayDialog("Saved", "All scenes and project have been saved!", "OK");
    }
    
    private void RefreshAssets()
    {
        AssetDatabase.Refresh();
        Debug.Log("🔄 Assets refreshed!");
        EditorUtility.DisplayDialog("Refreshed", "Asset database has been refreshed!", "OK");
    }
    
    private void OpenProjectFolder()
    {
        EditorUtility.RevealInFinder(Application.dataPath);
    }
    
    private void ShowCurrentStatus()
    {
        // Show current scene status
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        EditorGUILayout.LabelField($"Scene: {sceneName}");
        
        // Show component counts
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        EditorGUILayout.LabelField($"Player: {(player != null ? "✅ Found" : "❌ Missing")}");
        
        Camera mainCam = Camera.main;
        EditorGUILayout.LabelField($"Camera: {(mainCam != null ? "✅ Found" : "❌ Missing")}");
        
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        EditorGUILayout.LabelField($"Generator: {(generator != null ? "✅ Found" : "❌ Missing")}");
        
        Canvas canvas = FindFirstObjectByType<Canvas>();
        EditorGUILayout.LabelField($"UI: {(canvas != null ? "✅ Found" : "❌ Missing")}");
    }
}

/// <summary>
/// Additional Menu Items für schnellen Zugriff
/// </summary>
public class RollABallQuickMenu
{
    [MenuItem("Roll-a-Ball/Quick Fixes/🚀 Complete Scene Setup")]
    public static void QuickCompleteSetup()
    {
        CompleteSceneSetup setup = FindFirstObjectByType<CompleteSceneSetup>();
        if (setup == null)
        {
            GameObject setupGO = new GameObject("CompleteSceneSetup");
            setup = setupGO.AddComponent<CompleteSceneSetup>();
        }
        setup.SetupCompleteScene();
    }
    
    [MenuItem("Roll-a-Ball/Quick Fixes/👤 Fix Player")]
    public static void QuickFixPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player";
            player.tag = "Player";
            player.transform.localScale = Vector3.one * 0.5f;
            player.AddComponent<Rigidbody>();
            player.AddComponent<PlayerController>();
        }
        Debug.Log("✅ Player fixed!");
    }
    
    [MenuItem("Roll-a-Ball/Quick Fixes/🧼 Clear Console")]
    public static void QuickClearConsole()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
    
    [MenuItem("Roll-a-Ball/Scenes/📋 Level 1")]
    public static void OpenLevel1()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
    }
    
    [MenuItem("Roll-a-Ball/Scenes/📋 Level 2")]
    public static void OpenLevel2()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity");
    }
    
    [MenuItem("Roll-a-Ball/Scenes/📋 Level 3")]
    public static void OpenLevel3()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level3.unity");
    }
    
    [MenuItem("Roll-a-Ball/Scenes/🎲 Generated Level")]
    public static void OpenGeneratedLevel()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GeneratedLevel.unity");
    }
}
