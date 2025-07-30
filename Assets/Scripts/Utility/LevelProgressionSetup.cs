using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Utility class to setup LevelProgressionProfile assets
/// CLAUDE: CREATED to support configurable level progression
/// </summary>
public static class LevelProgressionSetup
{
    private const string DEFAULT_PROFILE_PATH = "Assets/ScriptableObjects/DefaultLevelProgression.asset";
    
    /// <summary>
    /// Create and setup default level progression profile
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void CreateDefaultLevelProgression()
    {
#if UNITY_EDITOR
        // Check if the ScriptableObjects directory exists
        string directoryPath = "Assets/ScriptableObjects";
        if (!AssetDatabase.IsValidFolder(directoryPath))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        
        // Create the asset
        LevelProgressionProfile profile = ScriptableObject.CreateInstance<LevelProgressionProfile>();
        
        // Setup default progression by calling the CreateDefaultProgression method directly
        var method = profile.GetType().GetMethod("CreateDefaultProgression", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(profile, null);
        }
        
        // Save the asset
        AssetDatabase.CreateAsset(profile, DEFAULT_PROFILE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[LevelProgressionSetup] Created default level progression at: {DEFAULT_PROFILE_PATH}");
        
        // Select the asset in project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = profile;
#endif
    }
    
    /// <summary>
    /// Find existing LevelProgressionProfile or create default one
    /// </summary>
    public static LevelProgressionProfile GetOrCreateDefaultProfile()
    {
#if UNITY_EDITOR
        // Try to load existing asset
        LevelProgressionProfile existing = AssetDatabase.LoadAssetAtPath<LevelProgressionProfile>(DEFAULT_PROFILE_PATH);
        if (existing != null)
            return existing;
        
        // Create new one if not found
        CreateDefaultLevelProgression();
        return AssetDatabase.LoadAssetAtPath<LevelProgressionProfile>(DEFAULT_PROFILE_PATH);
#else
        // In runtime, try to load from Resources
        LevelProgressionProfile profile = Resources.Load<LevelProgressionProfile>("DefaultLevelProgression");
        if (profile == null)
        {
            Debug.LogError("[LevelProgressionSetup] No LevelProgressionProfile found! Please assign one to LevelManager.");
        }
        return profile;
#endif
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Create menu item for easy access
    /// </summary>
    [MenuItem("Roll-a-Ball/Setup/Create Level Progression Profile")]
    private static void CreateLevelProgressionProfileMenu()
    {
        CreateDefaultLevelProgression();
    }
    
    /// <summary>
    /// Validate all LevelManagers have progression profiles assigned
    /// </summary>
    [MenuItem("Roll-a-Ball/Validate/Check Level Progression Setup")]
    private static void ValidateLevelProgressionSetup()
    {
        LevelManager[] managers = Object.FindObjectsByType<LevelManager>(FindObjectsSortMode.None);
        int managersWithoutProfile = 0;
        
        foreach (LevelManager manager in managers)
        {
            if (manager.GetType().GetField("progressionProfile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(manager) == null)
            {
                managersWithoutProfile++;
                Debug.LogWarning($"[LevelProgressionSetup] LevelManager in scene '{manager.gameObject.scene.name}' has no progression profile assigned!", manager);
            }
        }
        
        if (managersWithoutProfile == 0)
        {
            Debug.Log($"[LevelProgressionSetup] ✅ All {managers.Length} LevelManagers have progression profiles assigned.");
        }
        else
        {
            Debug.LogWarning($"[LevelProgressionSetup] ⚠️ {managersWithoutProfile}/{managers.Length} LevelManagers need progression profile assignment.");
        }
    }
#endif
}
