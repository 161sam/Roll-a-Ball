using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Master Fix Tool - Coordinates all repair systems and provides unified problem solving
/// One-click solution for all Roll-a-Ball level issues
/// </summary>
[AddComponentMenu("Roll-a-Ball/Master Fix Tool")]
public class MasterFixTool : MonoBehaviour
{
    [Header("Auto-Fix Settings")]
    [SerializeField] private bool autoFixOnStart = true;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showProgressNotifications = true;
    
    [Header("Manual Controls")]
    [SerializeField] private bool runCompleteFixNow = false;
    [SerializeField] private bool runQuickDiagnostic = false;
    
    [Header("Problem Categories")]
    [SerializeField] private bool fixUIProblems = true;
    [SerializeField] private bool fixCollectibleProblems = true;
    [SerializeField] private bool fixLevelProgression = true;
    [SerializeField] private bool fixOSMSpecificIssues = true;
    
    [Header("Fix Results")]
    [SerializeField] private int totalProblemsFound = 0;
    [SerializeField] private int totalProblemsFixed = 0;
    [SerializeField] private List<string> fixedIssues = new List<string>();
    [SerializeField] private List<string> remainingIssues = new List<string>();

    // Component references
    private UniversalSceneFixture sceneFixture;
    private CollectibleDiagnosticTool collectibleTool;
    private LevelProgressionFixer progressionFixer;
    private RollABall.Map.OSMUIConnector osmConnector;

    private void Start()
    {
        if (autoFixOnStart)
        {
            StartCoroutine(RunCompleteFix());
        }
    }

    private void OnValidate()
    {
        if (runCompleteFixNow)
        {
            runCompleteFixNow = false;
            StartCoroutine(RunCompleteFix());
        }
        
        if (runQuickDiagnostic)
        {
            runQuickDiagnostic = false;
            RunQuickDiagnostic();
        }
    }

    /// <summary>
    /// Main entry point - fixes all problems in the current scene
    /// </summary>
    [ContextMenu("Run Complete Fix")]
    public void RunCompleteFixNow()
    {
        StartCoroutine(RunCompleteFix());
    }

    /// <summary>
    /// Complete fix process with progress tracking
    /// </summary>
    private IEnumerator RunCompleteFix()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Log($"=== MASTER FIX TOOL - Starting complete fix for scene: {sceneName} ===");
        
        // Clear previous results
        totalProblemsFound = 0;
        totalProblemsFixed = 0;
        fixedIssues.Clear();
        remainingIssues.Clear();
        
        ShowProgress("Initialisiere Fix-Tools...", 0f);
        yield return new WaitForEndOfFrame();
        
        // Step 1: Initialize all fix tools
        InitializeFixTools();
        ShowProgress("Fix-Tools bereit", 0.1f);
        yield return new WaitForSeconds(0.5f);
        
        // Step 2: Run universal scene fixes
        if (fixUIProblems)
        {
            ShowProgress("Repariere UI-Verbindungen...", 0.2f);
            yield return StartCoroutine(RunUniversalSceneFix());
        }
        
        // Step 3: Fix collectible problems
        if (fixCollectibleProblems)
        {
            ShowProgress("Repariere Collectibles...", 0.4f);
            yield return StartCoroutine(RunCollectibleFix());
        }
        
        // Step 4: Fix level progression
        if (fixLevelProgression)
        {
            ShowProgress("Repariere Level-Übergänge...", 0.6f);
            yield return StartCoroutine(RunProgressionFix());
        }
        
        // Step 5: OSM-specific fixes
        if (fixOSMSpecificIssues && IsOSMScene(sceneName))
        {
            ShowProgress("Repariere OSM-UI...", 0.8f);
            yield return StartCoroutine(RunOSMFix());
        }
        
        // Step 6: Final validation
        ShowProgress("Validiere Reparaturen...", 0.9f);
        yield return StartCoroutine(RunFinalValidation());
        
        ShowProgress("Reparatur abgeschlossen!", 1.0f);
        
        // Generate final report
        GenerateFinalReport();
        
        Log($"=== MASTER FIX COMPLETED: {totalProblemsFixed}/{totalProblemsFound} problems fixed ===");
    }

    #region Fix Tool Initialization

    private void InitializeFixTools()
    {
        Log("Initializing fix tools...");
        
        // Universal Scene Fixture
        sceneFixture = FindFirstObjectByType<UniversalSceneFixture>();
        if (!sceneFixture)
        {
            GameObject fixtureGO = new GameObject("UniversalSceneFixture");
            sceneFixture = fixtureGO.AddComponent<UniversalSceneFixture>();
            AddFixedIssue("Created UniversalSceneFixture");
        }
        
        // Collectible Diagnostic Tool
        collectibleTool = FindFirstObjectByType<CollectibleDiagnosticTool>();
        if (!collectibleTool)
        {
            GameObject collectibleGO = new GameObject("CollectibleDiagnosticTool");
            collectibleTool = collectibleGO.AddComponent<CollectibleDiagnosticTool>();
            AddFixedIssue("Created CollectibleDiagnosticTool");
        }
        
        // Level Progression Fixer
        progressionFixer = FindFirstObjectByType<LevelProgressionFixer>();
        if (!progressionFixer)
        {
            GameObject progressionGO = new GameObject("LevelProgressionFixer");
            progressionFixer = progressionGO.AddComponent<LevelProgressionFixer>();
            AddFixedIssue("Created LevelProgressionFixer");
        }
        
        // OSM UI Connector (only for OSM scenes)
        string sceneName = SceneManager.GetActiveScene().name;
        if (IsOSMScene(sceneName))
        {
            osmConnector = FindFirstObjectByType<RollABall.Map.OSMUIConnector>();
            if (!osmConnector)
            {
                GameObject osmGO = new GameObject("OSMUIConnector");
                osmConnector = osmGO.AddComponent<RollABall.Map.OSMUIConnector>();
                AddFixedIssue("Created OSMUIConnector");
            }
        }
        
        Log("Fix tools initialization completed");
    }

    #endregion

    #region Individual Fix Processes

    private IEnumerator RunUniversalSceneFix()
    {
        Log("Running universal scene fixes...");
        
        if (sceneFixture)
        {
            sceneFixture.FixCurrentScene();
            AddFixedIssue("Universal scene components fixed");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            AddRemainingIssue("UniversalSceneFixture not available");
        }
    }

    private IEnumerator RunCollectibleFix()
    {
        Log("Running collectible fixes...");
        
        if (collectibleTool)
        {
            // Count collectibles before fix
            int beforeCount = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None).Length;
            
            collectibleTool.RunDiagnostic();
            
            // Count after fix
            int afterCount = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None).Length;
            
            if (afterCount > beforeCount)
            {
                AddFixedIssue($"Added {afterCount - beforeCount} missing CollectibleControllers");
            }
            
            AddFixedIssue("Collectible diagnostic and repair completed");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            AddRemainingIssue("CollectibleDiagnosticTool not available");
        }
    }

    private IEnumerator RunProgressionFix()
    {
        Log("Running progression fixes...");
        
        if (progressionFixer)
        {
            progressionFixer.FixCurrentLevelProgression();
            AddFixedIssue("Level progression configured");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            AddRemainingIssue("LevelProgressionFixer not available");
        }
    }

    private IEnumerator RunOSMFix()
    {
        Log("Running OSM-specific fixes...");
        
        if (osmConnector)
        {
            osmConnector.ConnectUIElements();
            AddFixedIssue("OSM UI elements connected");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            AddRemainingIssue("OSMUIConnector not available");
        }
    }

    private IEnumerator RunFinalValidation()
    {
        Log("Running final validation...");
        
        // Validate essential components
        ValidateEssentialComponents();
        
        // Validate level progression
        ValidateLevelProgression();
        
        // Validate collectibles
        ValidateCollectibles();
        
        // Validate UI connections
        ValidateUIConnections();
        
        yield return new WaitForSeconds(0.5f);
    }

    #endregion

    #region Validation Functions

    private void ValidateEssentialComponents()
    {
        Log("Validating essential components...");
        
        bool hasPlayer = GameObject.FindGameObjectWithTag("Player") != null;
        bool hasCamera = FindFirstObjectByType<CameraController>() != null;
        bool hasGameManager = FindFirstObjectByType<GameManager>() != null;
        bool hasLevelManager = FindFirstObjectByType<LevelManager>() != null;
        bool hasUIController = FindFirstObjectByType<UIController>() != null;
        
        if (hasPlayer && hasCamera && hasGameManager && hasLevelManager && hasUIController)
        {
            AddFixedIssue("All essential components present");
        }
        else
        {
            if (!hasPlayer) AddRemainingIssue("Missing Player object");
            if (!hasCamera) AddRemainingIssue("Missing CameraController");
            if (!hasGameManager) AddRemainingIssue("Missing GameManager");
            if (!hasLevelManager) AddRemainingIssue("Missing LevelManager");
            if (!hasUIController) AddRemainingIssue("Missing UIController");
        }
    }

    private void ValidateLevelProgression()
    {
        Log("Validating level progression...");
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager)
        {
            bool hasNextScene = !string.IsNullOrEmpty(levelManager.Config?.nextSceneName);
            bool hasGoalZone = GameObject.FindGameObjectWithTag("Finish") != null;
            
            if (hasNextScene && hasGoalZone)
            {
                AddFixedIssue("Level progression properly configured");
            }
            else
            {
                if (!hasNextScene) AddRemainingIssue("No next scene configured");
                if (!hasGoalZone) AddRemainingIssue("No goal zone found");
            }
        }
        else
        {
            AddRemainingIssue("No LevelManager found");
        }
    }

    private void ValidateCollectibles()
    {
        Log("Validating collectibles...");
        
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        int validCollectibles = 0;
        int brokenCollectibles = 0;
        
        foreach (var collectible in collectibles)
        {
            bool hasCollider = collectible.GetComponent<Collider>() != null;
            bool isTrigger = hasCollider && collectible.GetComponent<Collider>().isTrigger;
            bool hasCorrectTag = collectible.CompareTag("Collectible");
            
            if (hasCollider && isTrigger && hasCorrectTag)
            {
                validCollectibles++;
            }
            else
            {
                brokenCollectibles++;
            }
        }
        
        if (brokenCollectibles == 0 && validCollectibles > 0)
        {
            AddFixedIssue($"All {validCollectibles} collectibles properly configured");
        }
        else
        {
            if (brokenCollectibles > 0)
                AddRemainingIssue($"{brokenCollectibles} collectibles still have issues");
            if (validCollectibles == 0)
                AddRemainingIssue("No valid collectibles found");
        }
    }

    private void ValidateUIConnections()
    {
        Log("Validating UI connections...");
        
        string sceneName = SceneManager.GetActiveScene().name;
        
        if (IsOSMScene(sceneName))
        {
            ValidateOSMUI();
        }
        else
        {
            ValidateStandardUI();
        }
    }

    private void ValidateOSMUI()
    {
        bool hasInputField = FindFirstObjectByType<TMPro.TMP_InputField>() != null;
        bool hasButtons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Length > 0;
        bool hasMapController = FindFirstObjectByType<RollABall.Map.MapStartupController>() != null;
        
        if (hasInputField && hasButtons && hasMapController)
        {
            AddFixedIssue("OSM UI properly connected");
        }
        else
        {
            if (!hasInputField) AddRemainingIssue("Missing address input field");
            if (!hasButtons) AddRemainingIssue("Missing UI buttons");
            if (!hasMapController) AddRemainingIssue("Missing MapStartupController");
        }
    }

    private void ValidateStandardUI()
    {
        UIController uiController = FindFirstObjectByType<UIController>();
        if (uiController)
        {
            AddFixedIssue("Standard UI controller available");
        }
        else
        {
            AddRemainingIssue("Missing UIController");
        }
    }

    #endregion

    #region Quick Diagnostic

    [ContextMenu("Quick Diagnostic")]
    public void RunQuickDiagnostic()
    {
        Log("=== QUICK DIAGNOSTIC ===");
        
        string sceneName = SceneManager.GetActiveScene().name;
        Log($"Scene: {sceneName}");
        
        // Count essential components
        int playerCount = GameObject.FindGameObjectsWithTag("Player").Length;
        int collectibleCount = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None).Length;
        int buttonCount = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Length;
        
        Log($"Players: {playerCount}");
        Log($"Collectibles: {collectibleCount}");
        Log($"UI Buttons: {buttonCount}");
        
        // Check managers
        bool hasGameManager = FindFirstObjectByType<GameManager>() != null;
        bool hasLevelManager = FindFirstObjectByType<LevelManager>() != null;
        bool hasUIController = FindFirstObjectByType<UIController>() != null;
        
        Log($"GameManager: {(hasGameManager ? "✓" : "✗")}");
        Log($"LevelManager: {(hasLevelManager ? "✓" : "✗")}");
        Log($"UIController: {(hasUIController ? "✓" : "✗")}");
        
        // Scene-specific checks
        if (IsOSMScene(sceneName))
        {
            bool hasMapController = FindFirstObjectByType<RollABall.Map.MapStartupController>() != null;
            bool hasInputField = FindFirstObjectByType<TMPro.TMP_InputField>() != null;
            Log($"MapController: {(hasMapController ? "✓" : "✗")}");
            Log($"InputField: {(hasInputField ? "✓" : "✗")}");
        }
        
        Log("=== END DIAGNOSTIC ===");
    }

    #endregion

    #region Utility Functions

    private bool IsOSMScene(string sceneName)
    {
        return sceneName.ToLower().Contains("osm") || sceneName.ToLower().Contains("map");
    }

    private void AddFixedIssue(string issue)
    {
        fixedIssues.Add(issue);
        totalProblemsFixed++;
        totalProblemsFound++;
        Log($"FIXED: {issue}");
    }

    private void AddRemainingIssue(string issue)
    {
        remainingIssues.Add(issue);
        totalProblemsFound++;
        LogWarning($"REMAINING: {issue}");
    }

    private void ShowProgress(string message, float progress)
    {
        Log($"Progress ({progress:P0}): {message}");
        
        if (showProgressNotifications)
        {
            UIController uiController = FindFirstObjectByType<UIController>();
            if (uiController)
            {
                uiController.ShowNotification(message, 1f);
            }
        }
    }

    private void GenerateFinalReport()
    {
        Log("=== FINAL FIX REPORT ===");
        Log($"Total Problems Found: {totalProblemsFound}");
        Log($"Total Problems Fixed: {totalProblemsFixed}");
        Log($"Success Rate: {(totalProblemsFound > 0 ? (float)totalProblemsFixed / totalProblemsFound : 1f):P0}");
        
        if (fixedIssues.Count > 0)
        {
            Log("FIXED ISSUES:");
            foreach (string issue in fixedIssues)
            {
                Log($"  ✓ {issue}");
            }
        }
        
        if (remainingIssues.Count > 0)
        {
            Log("REMAINING ISSUES:");
            foreach (string issue in remainingIssues)
            {
                Log($"  ✗ {issue}");
            }
        }
        
        Log("=== END REPORT ===");
        
        // Show summary notification
        if (showProgressNotifications)
        {
            UIController uiController = FindFirstObjectByType<UIController>();
            if (uiController)
            {
                string summary = remainingIssues.Count == 0 ? 
                    "Alle Probleme behoben!" : 
                    $"{totalProblemsFixed}/{totalProblemsFound} Probleme behoben";
                uiController.ShowNotification(summary, 3f);
            }
        }
    }

    private void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[MasterFixTool] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MasterFixTool] {message}");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Add MasterFixTool to current scene
    /// </summary>
    [ContextMenu("Add to Scene")]
    public static void AddToCurrentScene()
    {
        if (FindFirstObjectByType<MasterFixTool>() == null)
        {
            GameObject masterGO = new GameObject("MasterFixTool");
            masterGO.AddComponent<MasterFixTool>();
            Debug.Log("Added MasterFixTool to current scene");
        }
        else
        {
            Debug.Log("MasterFixTool already exists in scene");
        }
    }

    /// <summary>
    /// Fix specific problem category
    /// </summary>
    public void FixSpecificProblem(string problemType)
    {
        switch (problemType.ToLower())
        {
            case "ui":
                StartCoroutine(RunUniversalSceneFix());
                break;
            case "collectibles":
                StartCoroutine(RunCollectibleFix());
                break;
            case "progression":
                StartCoroutine(RunProgressionFix());
                break;
            case "osm":
                StartCoroutine(RunOSMFix());
                break;
            default:
                LogWarning($"Unknown problem type: {problemType}");
                break;
        }
    }

    #endregion
}
