using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Automatic Scene Setup System - Deploys fix tools to all scenes automatically
/// One-click solution to add all necessary fix components to every scene
/// </summary>
[AddComponentMenu("Roll-a-Ball/Auto Scene Setup")]
public class AutoSceneSetup : MonoBehaviour
{
    [Header("Setup Configuration")]
    [SerializeField] private bool setupAllScenesOnStart = false;
    [SerializeField] private bool verboseLogging = true;
    [SerializeField] private bool backupScenesBeforeModification = true;
    
    [Header("Components to Deploy")]
    [SerializeField] private bool deployMasterFixTool = true;
    [SerializeField] private bool deployUniversalSceneFixture = true;
    [SerializeField] private bool deployCollectibleDiagnostic = true;
    [SerializeField] private bool deployProgressionFixer = true;
    [SerializeField] private bool deployOSMConnectorForOSMScenes = true;
    
    [Header("Scene Detection")]
    [SerializeField] private string[] sceneNames = {
        "Level1", "Level2", "Level3", "GeneratedLevel", "Level_OSM"
    };
    
    [Header("Manual Controls")]
    [SerializeField] private bool setupCurrentSceneNow = false;
    [SerializeField] private bool setupAllScenesNow = false;
    [SerializeField] private bool removeAllFixToolsFromCurrentScene = false;
    
    [Header("Status")]
    [SerializeField] private int scenesProcessed = 0;
    [SerializeField] private int componentsAdded = 0;
    [SerializeField] private List<string> processedScenes = new List<string>();

    private void Start()
    {
        if (setupAllScenesOnStart)
        {
            #if UNITY_EDITOR
            SetupAllScenes();
            #else
            Log("Auto scene setup only works in Unity Editor");
            #endif
        }
    }

    private void OnValidate()
    {
        if (setupCurrentSceneNow)
        {
            setupCurrentSceneNow = false;
            SetupCurrentScene();
        }
        
        if (setupAllScenesNow)
        {
            setupAllScenesNow = false;
            #if UNITY_EDITOR
            SetupAllScenes();
            #endif
        }
        
        if (removeAllFixToolsFromCurrentScene)
        {
            removeAllFixToolsFromCurrentScene = false;
            RemoveAllFixToolsFromCurrentScene();
        }
    }

    #region Public Methods

    /// <summary>
    /// Setup fix tools for the current scene
    /// </summary>
    [ContextMenu("Setup Current Scene")]
    public void SetupCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Log($"Setting up current scene: {sceneName}");
        
        int addedComponents = 0;
        
        // Add MasterFixTool
        if (deployMasterFixTool && !FindFirstObjectByType<MasterFixTool>())
        {
            GameObject masterGO = new GameObject("MasterFixTool");
            masterGO.AddComponent<MasterFixTool>();
            addedComponents++;
            Log("Added MasterFixTool");
        }
        
        // Add UniversalSceneFixture
        if (deployUniversalSceneFixture && !FindFirstObjectByType<UniversalSceneFixture>())
        {
            GameObject fixtureGO = new GameObject("UniversalSceneFixture");
            fixtureGO.AddComponent<UniversalSceneFixture>();
            addedComponents++;
            Log("Added UniversalSceneFixture");
        }
        
        // Add CollectibleDiagnosticTool
        if (deployCollectibleDiagnostic && !FindFirstObjectByType<CollectibleDiagnosticTool>())
        {
            GameObject diagnosticGO = new GameObject("CollectibleDiagnosticTool");
            diagnosticGO.AddComponent<CollectibleDiagnosticTool>();
            addedComponents++;
            Log("Added CollectibleDiagnosticTool");
        }
        
        // Add LevelProgressionFixer
        if (deployProgressionFixer && !FindFirstObjectByType<LevelProgressionFixer>())
        {
            GameObject progressionGO = new GameObject("LevelProgressionFixer");
            progressionGO.AddComponent<LevelProgressionFixer>();
            addedComponents++;
            Log("Added LevelProgressionFixer");
        }
        
        // Add OSMUIConnector for OSM scenes
        if (deployOSMConnectorForOSMScenes && IsOSMScene(sceneName) && 
            !FindFirstObjectByType<RollABall.Map.OSMUIConnector>())
        {
            GameObject osmGO = new GameObject("OSMUIConnector");
            osmGO.AddComponent<RollABall.Map.OSMUIConnector>();
            addedComponents++;
            Log("Added OSMUIConnector");
        }
        
        componentsAdded += addedComponents;
        Log($"Setup completed for {sceneName}. Added {addedComponents} components.");
        
        // Run initial fix
        MasterFixTool masterFix = FindFirstObjectByType<MasterFixTool>();
        if (masterFix)
        {
            masterFix.RunCompleteFixNow();
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Setup fix tools for all scenes (Editor only)
    /// </summary>
    [ContextMenu("Setup All Scenes")]
    public void SetupAllScenes()
    {
        Log("Starting automatic setup for all scenes...");
        
        // Reset counters
        scenesProcessed = 0;
        componentsAdded = 0;
        processedScenes.Clear();
        
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        
        // Find all scenes in the project
        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
        
        foreach (string guid in sceneGUIDs)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            // Skip if not in our target list
            if (!System.Array.Exists(sceneNames, name => name.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            
            Log($"Processing scene: {sceneName}");
            
            // Backup scene if requested
            if (backupScenesBeforeModification)
            {
                BackupScene(scenePath);
            }
            
            // Open scene
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            // Setup the scene
            SetupCurrentScene();
            
            // Save scene
            EditorSceneManager.SaveOpenScenes();
            
            processedScenes.Add(sceneName);
            scenesProcessed++;
        }
        
        // Return to original scene
        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }
        
        Log($"Automatic setup completed! Processed {scenesProcessed} scenes, added {componentsAdded} components total.");
        
        // Show completion dialog
        EditorUtility.DisplayDialog("Auto Scene Setup Complete", 
            $"Successfully processed {scenesProcessed} scenes.\n" +
            $"Added {componentsAdded} fix components.\n" +
            $"All scenes are now ready with fix tools!", "OK");
    }
    
    private void BackupScene(string scenePath)
    {
        string backupPath = scenePath.Replace(".unity", "_backup.unity");
        AssetDatabase.CopyAsset(scenePath, backupPath);
        Log($"Created backup: {backupPath}");
    }
    #endif

    /// <summary>
    /// Remove all fix tools from current scene
    /// </summary>
    [ContextMenu("Remove All Fix Tools")]
    public void RemoveAllFixToolsFromCurrentScene()
    {
        Log("Removing all fix tools from current scene...");
        
        int removedComponents = 0;
        
        // Remove MasterFixTool
        MasterFixTool[] masterTools = FindObjectsByType<MasterFixTool>(FindObjectsSortMode.None);
        foreach (var tool in masterTools)
        {
            DestroyImmediate(tool.gameObject);
            removedComponents++;
        }
        
        // Remove UniversalSceneFixture
        UniversalSceneFixture[] fixtures = FindObjectsByType<UniversalSceneFixture>(FindObjectsSortMode.None);
        foreach (var fixture in fixtures)
        {
            DestroyImmediate(fixture.gameObject);
            removedComponents++;
        }
        
        // Remove CollectibleDiagnosticTool
        CollectibleDiagnosticTool[] diagnostics = FindObjectsByType<CollectibleDiagnosticTool>(FindObjectsSortMode.None);
        foreach (var diagnostic in diagnostics)
        {
            DestroyImmediate(diagnostic.gameObject);
            removedComponents++;
        }
        
        // Remove LevelProgressionFixer
        LevelProgressionFixer[] progressionFixers = FindObjectsByType<LevelProgressionFixer>(FindObjectsSortMode.None);
        foreach (var fixer in progressionFixers)
        {
            DestroyImmediate(fixer.gameObject);
            removedComponents++;
        }
        
        // Remove OSMUIConnector
        RollABall.Map.OSMUIConnector[] osmConnectors = FindObjectsByType<RollABall.Map.OSMUIConnector>(FindObjectsSortMode.None);
        foreach (var connector in osmConnectors)
        {
            DestroyImmediate(connector.gameObject);
            removedComponents++;
        }
        
        Log($"Removed {removedComponents} fix tool components from scene");
    }

    #endregion

    #region Utility Methods

    private bool IsOSMScene(string sceneName)
    {
        return sceneName.ToLower().Contains("osm") || sceneName.ToLower().Contains("map");
    }

    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[AutoSceneSetup] {message}");
        }
    }

    #endregion

    #region Editor Integration

    #if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/Setup All Scenes")]
    public static void SetupAllScenesMenuItem()
    {
        // Create temporary AutoSceneSetup if none exists
        AutoSceneSetup setup = FindFirstObjectByType<AutoSceneSetup>();
        if (!setup)
        {
            GameObject setupGO = new GameObject("TempAutoSceneSetup");
            setup = setupGO.AddComponent<AutoSceneSetup>();
        }
        
        setup.SetupAllScenes();
        
        // Clean up temporary object
        if (setup.gameObject.name == "TempAutoSceneSetup")
        {
            DestroyImmediate(setup.gameObject);
        }
    }
    
    [MenuItem("Roll-a-Ball/Setup Current Scene")]
    public static void SetupCurrentSceneMenuItem()
    {
        AutoSceneSetup setup = FindFirstObjectByType<AutoSceneSetup>();
        if (!setup)
        {
            GameObject setupGO = new GameObject("TempAutoSceneSetup");
            setup = setupGO.AddComponent<AutoSceneSetup>();
        }
        
        setup.SetupCurrentScene();
        
        if (setup.gameObject.name == "TempAutoSceneSetup")
        {
            DestroyImmediate(setup.gameObject);
        }
    }
    
    [MenuItem("Roll-a-Ball/Run Master Fix on Current Scene")]
    public static void RunMasterFixMenuItem()
    {
        MasterFixTool masterFix = FindFirstObjectByType<MasterFixTool>();
        if (masterFix)
        {
            masterFix.RunCompleteFixNow();
        }
        else
        {
            SetupCurrentSceneMenuItem();
            masterFix = FindFirstObjectByType<MasterFixTool>();
            if (masterFix)
            {
                masterFix.RunCompleteFixNow();
            }
        }
    }
    
    [MenuItem("Roll-a-Ball/Remove All Fix Tools")]
    public static void RemoveAllFixToolsMenuItem()
    {
        AutoSceneSetup setup = FindFirstObjectByType<AutoSceneSetup>();
        if (!setup)
        {
            GameObject setupGO = new GameObject("TempAutoSceneSetup");
            setup = setupGO.AddComponent<AutoSceneSetup>();
        }
        
        setup.RemoveAllFixToolsFromCurrentScene();
        
        if (setup.gameObject.name == "TempAutoSceneSetup")
        {
            DestroyImmediate(setup.gameObject);
        }
    }
    #endif

    #endregion
}
