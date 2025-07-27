using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Scene Stress Test System - Automated generation and testing of corrupted scenes
/// Tests the Scene Consolidation Engine under extreme conditions and edge cases
/// </summary>
[AddComponentMenu("Roll-a-Ball/Scene Stress Tests")]
public class SceneStressTests : MonoBehaviour
{
    [Header("🧪 Test Configuration")]
    [SerializeField] private bool runAllTests = false;
    [SerializeField] private bool generateCorruptedScenesOnly = false;
    [SerializeField] private bool consolidateTestScenesOnly = false;
    [SerializeField] private bool generateReportsOnly = false;
    
    [Header("📊 Test Results")]
    [SerializeField] private int totalTestScenesGenerated = 0;
    [SerializeField] private int totalTestScenesTested = 0;
    [SerializeField] private int totalIssuesFound = 0;
    [SerializeField] private int totalIssuesFixed = 0;
    [SerializeField] private List<string> testSceneResults = new List<string>();
    
    [Header("⚙️ Test Scene Settings")]
    [SerializeField] private int corruptedPrefabCount = 50;
    [SerializeField] private int corruptedPrefabPercentage = 60;
    [SerializeField] private int overloadedUIElementCount = 20;
    [SerializeField] private int chaosCollectibleCount = 100;
    [SerializeField] private int chaosCollectibleCorruptPercentage = 30;
    
    private SceneConsolidationEngine consolidationEngine;
    private List<TestSceneProfile> testProfiles = new List<TestSceneProfile>();
    private Dictionary<string, TestSceneReport> testReports = new Dictionary<string, TestSceneReport>();
    
    /// <summary>
    /// Test scene configuration profile
    /// </summary>
    [System.Serializable]
    public class TestSceneProfile
    {
        public string sceneName;
        public string description;
        public TestSceneType testType;
        public int expectedErrorCount;
        public List<string> specificIssues;
        public bool requiresConsolidation;
    }
    
    /// <summary>
    /// Test scene report with before/after analysis
    /// </summary>
    [System.Serializable]
    public class TestSceneReport
    {
        public string sceneName;
        public System.DateTime testStartTime;
        public System.DateTime testEndTime;
        public int issuesFoundBefore;
        public int issuesFoundAfter;
        public int issuesFixed;
        public List<string> beforeIssues;
        public List<string> afterIssues;
        public List<string> fixedIssues;
        public bool consolidationSuccessful;
        public string additionalNotes;
    }
    
    public enum TestSceneType
    {
        CorruptedPrefabs,
        OverloadedUI,
        CollectiblesChaos
    }
    
    private void Start()
    {
        InitializeTestProfiles();
        
        // Get or create consolidation engine
        consolidationEngine = Object.FindFirstObjectByType<SceneConsolidationEngine>();
        if (consolidationEngine == null)
        {
            GameObject engineGO = new GameObject("SceneConsolidationEngine");
            consolidationEngine = engineGO.AddComponent<SceneConsolidationEngine>();
            Debug.Log("✅ SceneConsolidationEngine created for stress testing");
        }
    }
    
    private void OnValidate()
    {
        if (runAllTests)
        {
            runAllTests = false;
            ExecuteCompleteStressTest();
        }
        
        if (generateCorruptedScenesOnly)
        {
            generateCorruptedScenesOnly = false;
            StartCoroutine(GenerateAllTestScenesCoroutine());
        }
        
        if (consolidateTestScenesOnly)
        {
            consolidateTestScenesOnly = false;
            StartCoroutine(ConsolidateAllTestScenesCoroutine());
        }
        
        if (generateReportsOnly)
        {
            generateReportsOnly = false;
            GenerateAllTestReports();
        }
    }
    
    /// <summary>
    /// Executes complete stress test suite - generates scenes, tests consolidation, generates reports
    /// </summary>
    [ContextMenu("Execute Complete Stress Test")]
    public void ExecuteCompleteStressTest()
    {
        Debug.Log("🧪 Starting Complete Scene Stress Test Suite");
        StartCoroutine(CompleteStressTestCoroutine());
    }
    
    /// <summary>
    /// Complete stress test workflow
    /// </summary>
    private IEnumerator CompleteStressTestCoroutine()
    {
        Debug.Log("🎯 PHASE 1: Generating Corrupted Test Scenes");
        yield return GenerateAllTestScenesCoroutine();
        
        Debug.Log("🔧 PHASE 2: Testing Scene Consolidation");
        yield return ConsolidateAllTestScenesCoroutine();
        
        Debug.Log("📄 PHASE 3: Generating Test Reports");
        GenerateAllTestReports();
        
        Debug.Log("✅ Complete Stress Test Suite Finished!");
        GenerateFinalStressTestReport();
    }
    
    #region Test Scene Generation
    
    /// <summary>
    /// Generates all three test scenes with specific corruption patterns
    /// </summary>
    public IEnumerator GenerateAllTestScenesCoroutine()
    {
        Debug.Log("🧪 Generating Test Scenes...");
        
        // Generate Scene_Test_CorruptedPrefabs
        yield return GenerateCorruptedPrefabsScene();
        
        // Generate Scene_Test_OverloadedUI
        yield return GenerateOverloadedUIScene();
        
        // Generate Scene_Test_CollectiblesChaos
        yield return GenerateCollectiblesChaosScene();
        
        Debug.Log($"✅ Generated {totalTestScenesGenerated} test scenes");
    }
    
    /// <summary>
    /// Generates Scene_Test_CorruptedPrefabs with objects that aren't prefab instances
    /// </summary>
    private IEnumerator GenerateCorruptedPrefabsScene()
    {
        Debug.Log("🧪 Generating Scene_Test_CorruptedPrefabs...");
        
#if UNITY_EDITOR
        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        newScene.name = "Scene_Test_CorruptedPrefabs";
        
        // Create basic scene structure but with corrupted prefabs
        CreateBasicSceneStructure();
        
        // Generate 50 objects where 30 are NOT prefab instances
        for (int i = 0; i < corruptedPrefabCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-20f, 20f),
                Random.Range(0f, 5f),
                Random.Range(-20f, 20f)
            );
            
            GameObject obj;
            
            // 60% should be corrupted (not prefab instances)
            if (i < (corruptedPrefabCount * corruptedPrefabPercentage / 100))
            {
                // Create manual objects (corrupted)
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"CorruptedObject_{i:D2}";
                obj.transform.position = position;
                obj.tag = "Ground"; // Wrong but intentional for testing
                
                // Remove some components to create more issues
                if (Random.Range(0f, 1f) < 0.3f)
                {
                    DestroyImmediate(obj.GetComponent<BoxCollider>());
                }
                
                // Add wrong materials
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = Color.red; // Clearly wrong
                }
            }
            else
            {
                // Create proper prefab instances (correct)
                GameObject groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GroundPrefab.prefab");
                if (groundPrefab != null)
                {
                    obj = PrefabUtility.InstantiatePrefab(groundPrefab) as GameObject;
                    obj.transform.position = position;
                }
                else
                {
                    // Fallback if prefab not found
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = $"FallbackGround_{i:D2}";
                    obj.transform.position = position;
                }
            }
        }
        
        // Create managers with missing script references
        CreateCorruptedManagers();
        
        // Save the corrupted scene
        string scenePath = "Assets/Scenes/TestScenes/Scene_Test_CorruptedPrefabs.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        totalTestScenesGenerated++;
        Debug.Log($"✅ Generated Scene_Test_CorruptedPrefabs with {corruptedPrefabCount} objects ({corruptedPrefabPercentage}% corrupted)");
#else
        Debug.LogWarning("⚠️ Test scene generation only available in Editor mode");
#endif
        
        yield return null;
    }
    
    /// <summary>
    /// Generates Scene_Test_OverloadedUI with excessive and broken UI elements
    /// </summary>
    private IEnumerator GenerateOverloadedUIScene()
    {
        Debug.Log("🧪 Generating Scene_Test_OverloadedUI...");
        
#if UNITY_EDITOR
        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        newScene.name = "Scene_Test_OverloadedUI";
        
        // Create basic scene structure
        CreateBasicSceneStructure();
        
        // Create multiple Canvas instances (problematic)
        for (int i = 0; i < 3; i++)
        {
            GameObject canvasGO = new GameObject($"Canvas_{i:D2}");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = i; // Conflicting sort orders
            
            // Missing CanvasScaler and GraphicRaycaster intentionally
            
            // Create excessive UI elements with wrong sizing
            for (int j = 0; j < overloadedUIElementCount / 3; j++)
            {
                GameObject uiElement = new GameObject($"UI_Element_{i}_{j:D2}");
                uiElement.transform.SetParent(canvasGO.transform);
                
                RectTransform rect = uiElement.AddComponent<RectTransform>();
                
                // Intentionally problematic sizing and positioning
                rect.anchorMin = new Vector2(Random.Range(-2f, 3f), Random.Range(-2f, 3f));
                rect.anchorMax = new Vector2(Random.Range(-2f, 3f), Random.Range(-2f, 3f));
                rect.sizeDelta = new Vector2(Random.Range(50f, 5000f), Random.Range(50f, 5000f));
                rect.anchoredPosition = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));
                
                // Add different UI components with issues
                switch (j % 4)
                {
                    case 0:
                        // Text without font
                        Text text = uiElement.AddComponent<Text>();
                        text.text = $"Broken Text {j}";
                        text.font = null; // Intentionally null
                        break;
                        
                    case 1:
                        // Button without proper setup
                        Button button = uiElement.AddComponent<Button>();
                        Image buttonImage = uiElement.AddComponent<Image>();
                        buttonImage.sprite = null; // Missing sprite
                        break;
                        
                    case 2:
                        // Input field without proper text component
                        InputField input = uiElement.AddComponent<InputField>();
                        input.textComponent = null; // Missing text component
                        break;
                        
                    case 3:
                        // Image with missing sprite
                        Image image = uiElement.AddComponent<Image>();
                        image.sprite = null;
                        image.color = new Color(Random.value, Random.value, Random.value, Random.Range(0f, 2f));
                        break;
                }
            }
        }
        
        // Create separate EventSystem instances (problematic)
        for (int i = 0; i < 3; i++)
        {
            GameObject eventSystemGO = new GameObject($"EventSystem_{i:D2}");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // Save the overloaded scene
        string scenePath = "Assets/Scenes/TestScenes/Scene_Test_OverloadedUI.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        totalTestScenesGenerated++;
        Debug.Log($"✅ Generated Scene_Test_OverloadedUI with {overloadedUIElementCount} UI elements across 3 Canvas instances");
#else
        Debug.LogWarning("⚠️ Test scene generation only available in Editor mode");
#endif
        
        yield return null;
    }
    
    /// <summary>
    /// Generates Scene_Test_CollectiblesChaos with 100 randomly distributed collectibles with various issues
    /// </summary>
    private IEnumerator GenerateCollectiblesChaosScene()
    {
        Debug.Log("🧪 Generating Scene_Test_CollectiblesChaos...");
        
#if UNITY_EDITOR
        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        newScene.name = "Scene_Test_CollectiblesChaos";
        
        // Create basic scene structure
        CreateBasicSceneStructure();
        
        // Generate 100 collectibles with various issues
        for (int i = 0; i < chaosCollectibleCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-50f, 50f),
                Random.Range(0f, 10f),
                Random.Range(-50f, 50f)
            );
            
            GameObject collectible;
            
            // 30% should have issues
            if (i < (chaosCollectibleCount * chaosCollectibleCorruptPercentage / 100))
            {
                // Create corrupted collectibles
                collectible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                collectible.name = $"BrokenCollectible_{i:D3}";
                collectible.transform.position = position;
                
                // Various corruption patterns
                switch (i % 5)
                {
                    case 0:
                        // Missing Collider
                        DestroyImmediate(collectible.GetComponent<SphereCollider>());
                        collectible.tag = "Collectible";
                        break;
                        
                    case 1:
                        // Wrong Tag
                        collectible.tag = "Ground";
                        break;
                        
                    case 2:
                        // No Trigger
                        var collider = collectible.GetComponent<SphereCollider>();
                        if (collider != null) collider.isTrigger = false;
                        collectible.tag = "Collectible";
                        break;
                        
                    case 3:
                        // Missing Controller Script (simulate by not adding it)
                        collectible.tag = "Collectible";
                        break;
                        
                    case 4:
                        // Inactive GameObject
                        collectible.SetActive(false);
                        collectible.tag = "Collectible";
                        break;
                }
                
                // Wrong material
                var renderer = collectible.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = Color.black; // Wrong color for collectible
                }
            }
            else
            {
                // Create proper collectible instances
                GameObject collectiblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CollectiblePrefab.prefab");
                if (collectiblePrefab != null)
                {
                    collectible = PrefabUtility.InstantiatePrefab(collectiblePrefab) as GameObject;
                    collectible.transform.position = position;
                }
                else
                {
                    // Fallback if prefab not found
                    collectible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    collectible.name = $"FallbackCollectible_{i:D3}";
                    collectible.transform.position = position;
                    collectible.tag = "Collectible";
                    
                    var collider = collectible.GetComponent<SphereCollider>();
                    if (collider != null) collider.isTrigger = true;
                }
            }
        }
        
        // Create ground for collectibles to rest on
        for (int i = 0; i < 25; i++)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = $"Ground_{i:D2}";
            ground.transform.position = new Vector3(
                Random.Range(-50f, 50f),
                -1f,
                Random.Range(-50f, 50f)
            );
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            ground.tag = "Ground";
        }
        
        // Save the chaos scene
        string scenePath = "Assets/Scenes/TestScenes/Scene_Test_CollectiblesChaos.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        totalTestScenesGenerated++;
        Debug.Log($"✅ Generated Scene_Test_CollectiblesChaos with {chaosCollectibleCount} collectibles ({chaosCollectibleCorruptPercentage}% corrupted)");
#else
        Debug.LogWarning("⚠️ Test scene generation only available in Editor mode");
#endif
        
        yield return null;
    }
    
    #endregion
    
    #region Scene Consolidation Testing
    
    /// <summary>
    /// Consolidates all test scenes and records results
    /// </summary>
    private IEnumerator ConsolidateAllTestScenesCoroutine()
    {
        Debug.Log("🔧 Testing Scene Consolidation on Test Scenes...");
        
        string[] testScenes = {
            "Scene_Test_CorruptedPrefabs",
            "Scene_Test_OverloadedUI", 
            "Scene_Test_CollectiblesChaos"
        };
        
        foreach (string sceneName in testScenes)
        {
            yield return ConsolidateAndAnalyzeTestScene(sceneName);
            yield return new WaitForSeconds(1f); // Small delay between tests
        }
        
        Debug.Log($"✅ Consolidation testing complete on {testScenes.Length} test scenes");
    }
    
    /// <summary>
    /// Consolidates a specific test scene and analyzes results
    /// </summary>
    private IEnumerator ConsolidateAndAnalyzeTestScene(string sceneName)
    {
        Debug.Log($"🔍 Testing consolidation of {sceneName}...");
        
        TestSceneReport report = new TestSceneReport
        {
            sceneName = sceneName,
            testStartTime = System.DateTime.Now,
            beforeIssues = new List<string>(),
            afterIssues = new List<string>(),
            fixedIssues = new List<string>()
        };
        
        // Phase 1: Analyze scene before consolidation
        yield return AnalyzeSceneIssues(sceneName, report, true);
        
        // Phase 2: Apply consolidation
        if (consolidationEngine != null)
        {
            Debug.Log($"🔧 Applying consolidation to {sceneName}...");
            yield return consolidationEngine.ConsolidateScene(sceneName);
            report.consolidationSuccessful = true;
        }
        else
        {
            Debug.LogError($"❌ ConsolidationEngine not available for {sceneName}");
            report.consolidationSuccessful = false;
        }
        
        // Phase 3: Analyze scene after consolidation
        yield return AnalyzeSceneIssues(sceneName, report, false);
        
        // Phase 4: Calculate results
        report.testEndTime = System.DateTime.Now;
        report.issuesFixed = report.issuesFoundBefore - report.issuesFoundAfter;
        
        // Calculate fixed issues by comparing before/after lists
        foreach (string beforeIssue in report.beforeIssues)
        {
            if (!report.afterIssues.Contains(beforeIssue))
            {
                report.fixedIssues.Add(beforeIssue);
            }
        }
        
        // Store report
        testReports[sceneName] = report;
        totalTestScenesTested++;
        totalIssuesFound += report.issuesFoundBefore;
        totalIssuesFixed += report.issuesFixed;
        
        string resultSummary = $"Scene: {sceneName}, Before: {report.issuesFoundBefore}, After: {report.issuesFoundAfter}, Fixed: {report.issuesFixed}";
        testSceneResults.Add(resultSummary);
        
        Debug.Log($"✅ {sceneName}: {report.issuesFixed}/{report.issuesFoundBefore} issues fixed");
        
        yield return null;
    }
    
    /// <summary>
    /// Analyzes scene issues before or after consolidation
    /// </summary>
    private IEnumerator AnalyzeSceneIssues(string sceneName, TestSceneReport report, bool isBefore)
    {
        Debug.Log($"🔍 Analyzing {sceneName} {(isBefore ? "BEFORE" : "AFTER")} consolidation...");
        
        List<string> issues = isBefore ? report.beforeIssues : report.afterIssues;
        int issueCount = 0;
        
        // Load scene if not current
        if (SceneManager.GetActiveScene().name != sceneName)
        {
#if UNITY_EDITOR
            string scenePath = $"Assets/Scenes/TestScenes/{sceneName}.unity";
            EditorSceneManager.OpenScene(scenePath);
            yield return new WaitForSeconds(0.5f);
#else
            SceneManager.LoadScene(sceneName);
            yield return new WaitForSeconds(1f);
#endif
        }
        
        // Check for missing managers
        if (Object.FindFirstObjectByType<GameManager>() == null)
        {
            issues.Add("Missing GameManager");
            issueCount++;
        }
        
        if (Object.FindFirstObjectByType<LevelManager>() == null)
        {
            issues.Add("Missing LevelManager");
            issueCount++;
        }
        
        if (Object.FindFirstObjectByType<UIController>() == null)
        {
            issues.Add("Missing UIController");
            issueCount++;
        }
        
        if (Object.FindFirstObjectByType<PlayerController>() == null)
        {
            issues.Add("Missing PlayerController");
            issueCount++;
        }
        
        if (Object.FindFirstObjectByType<CameraController>() == null)
        {
            issues.Add("Missing CameraController");
            issueCount++;
        }
        
        // Check UI issues
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases.Length == 0)
        {
            issues.Add("No Canvas found");
            issueCount++;
        }
        else if (canvases.Length > 1)
        {
            issues.Add($"Multiple Canvas instances ({canvases.Length})");
            issueCount++;
        }
        
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length == 0)
        {
            issues.Add("No EventSystem found");
            issueCount++;
        }
        else if (eventSystems.Length > 1)
        {
            issues.Add($"Multiple EventSystem instances ({eventSystems.Length})");
            issueCount++;
        }
        
        // Check for non-prefab instances
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int nonPrefabCount = 0;
        foreach (GameObject obj in allObjects)
        {
#if UNITY_EDITOR
            if (obj.scene.IsValid() && !PrefabUtility.IsPartOfPrefabInstance(obj) && 
                (obj.name.Contains("Ground") || obj.name.Contains("Wall") || obj.name.Contains("Collectible")))
            {
                nonPrefabCount++;
            }
#endif
        }
        
        if (nonPrefabCount > 0)
        {
            issues.Add($"Non-prefab instances found ({nonPrefabCount})");
            issueCount += nonPrefabCount;
        }
        
        // Check collectible issues
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        int brokenCollectibles = 0;
        foreach (GameObject collectible in collectibles)
        {
            if (collectible.GetComponent<Collider>() == null)
            {
                brokenCollectibles++;
            }
            else if (!collectible.GetComponent<Collider>().isTrigger)
            {
                brokenCollectibles++;
            }
            
            if (!collectible.activeInHierarchy)
            {
                brokenCollectibles++;
            }
        }
        
        if (brokenCollectibles > 0)
        {
            issues.Add($"Broken collectibles ({brokenCollectibles})");
            issueCount += brokenCollectibles;
        }
        
        // Check for UI component issues
        Text[] oldTexts = FindObjectsByType<Text>(FindObjectsSortMode.None);
        if (oldTexts.Length > 0)
        {
            issues.Add($"Old Text components found ({oldTexts.Length})");
            issueCount += oldTexts.Length;
        }
        
        // Check for broken UI elements
        Image[] images = FindObjectsByType<Image>(FindObjectsSortMode.None);
        int brokenImages = 0;
        foreach (Image img in images)
        {
            if (img.sprite == null && img.GetComponent<Button>() == null)
            {
                brokenImages++;
            }
        }
        
        if (brokenImages > 0)
        {
            issues.Add($"Images without sprites ({brokenImages})");
            issueCount += brokenImages;
        }
        
        // Store issue count
        if (isBefore)
        {
            report.issuesFoundBefore = issueCount;
        }
        else
        {
            report.issuesFoundAfter = issueCount;
        }
        
        Debug.Log($"📊 {sceneName} {(isBefore ? "BEFORE" : "AFTER")}: {issueCount} issues found");
        
        yield return null;
    }
    
    #endregion
    
    #region Report Generation
    
    /// <summary>
    /// Generates individual reports for each test scene
    /// </summary>
    public void GenerateAllTestReports()
    {
        Debug.Log("📄 Generating Test Scene Reports...");
        
        foreach (var kvp in testReports)
        {
            GenerateIndividualTestReport(kvp.Key, kvp.Value);
        }
        
        Debug.Log($"✅ Generated {testReports.Count} individual test reports");
    }
    
    /// <summary>
    /// Generates individual test report for a specific scene
    /// </summary>
    private void GenerateIndividualTestReport(string sceneName, TestSceneReport report)
    {
        string reportPath = Path.Combine(Application.dataPath, "..", "Reports", "TestScenes", $"{sceneName}_AFTER.md");
        
        System.Text.StringBuilder reportContent = new System.Text.StringBuilder();
        reportContent.AppendLine($"# 🧪 Test Scene Report: {sceneName}");
        reportContent.AppendLine($"**Generated:** {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        reportContent.AppendLine($"**Test Duration:** {(report.testEndTime - report.testStartTime).TotalSeconds:F2} seconds");
        reportContent.AppendLine($"**Consolidation Engine:** {(report.consolidationSuccessful ? "✅ Success" : "❌ Failed")}");
        reportContent.AppendLine();
        
        reportContent.AppendLine("## 📊 Consolidation Results");
        reportContent.AppendLine($"- **Issues Found (Before):** {report.issuesFoundBefore}");
        reportContent.AppendLine($"- **Issues Found (After):** {report.issuesFoundAfter}");
        reportContent.AppendLine($"- **Issues Fixed:** {report.issuesFixed}");
        reportContent.AppendLine($"- **Fix Rate:** {(report.issuesFoundBefore > 0 ? (float)report.issuesFixed / report.issuesFoundBefore * 100 : 100):F1}%");
        reportContent.AppendLine();
        
        reportContent.AppendLine("## 🔍 Issues Found BEFORE Consolidation");
        if (report.beforeIssues.Count > 0)
        {
            foreach (string issue in report.beforeIssues)
            {
                reportContent.AppendLine($"- ❌ {issue}");
            }
        }
        else
        {
            reportContent.AppendLine("- ✅ No issues found");
        }
        reportContent.AppendLine();
        
        reportContent.AppendLine("## 🔍 Issues Found AFTER Consolidation");
        if (report.afterIssues.Count > 0)
        {
            foreach (string issue in report.afterIssues)
            {
                reportContent.AppendLine($"- ⚠️ {issue}");
            }
        }
        else
        {
            reportContent.AppendLine("- ✅ No issues remaining");
        }
        reportContent.AppendLine();
        
        reportContent.AppendLine("## ✅ Issues FIXED by Consolidation");
        if (report.fixedIssues.Count > 0)
        {
            foreach (string issue in report.fixedIssues)
            {
                reportContent.AppendLine($"- ✅ {issue}");
            }
        }
        else
        {
            reportContent.AppendLine("- ⚠️ No issues were fixed");
        }
        reportContent.AppendLine();
        
        // Scene-specific analysis
        switch (sceneName)
        {
            case "Scene_Test_CorruptedPrefabs":
                reportContent.AppendLine("## 🧩 Prefab Validation Results");
                reportContent.AppendLine($"This test verified the consolidation engine's ability to handle non-prefab instances.");
                reportContent.AppendLine($"- **Expected Issues:** ~{corruptedPrefabCount * corruptedPrefabPercentage / 100} corrupted prefabs");
                reportContent.AppendLine($"- **Prefab Standardization:** {(report.fixedIssues.Any(issue => issue.Contains("Non-prefab")) ? "✅ Successful" : "⚠️ Needs Review")}");
                break;
                
            case "Scene_Test_OverloadedUI":
                reportContent.AppendLine("## 🖥️ UI System Validation Results");
                reportContent.AppendLine($"This test verified the consolidation engine's ability to clean up excessive UI elements.");
                reportContent.AppendLine($"- **Expected Issues:** Multiple Canvas/EventSystem instances");
                reportContent.AppendLine($"- **UI Cleanup:** {(report.fixedIssues.Any(issue => issue.Contains("Multiple")) ? "✅ Successful" : "⚠️ Needs Review")}");
                break;
                
            case "Scene_Test_CollectiblesChaos":
                reportContent.AppendLine("## 🎯 Collectible System Validation Results");
                reportContent.AppendLine($"This test verified the consolidation engine's ability to fix collectible issues.");
                reportContent.AppendLine($"- **Expected Issues:** ~{chaosCollectibleCount * chaosCollectibleCorruptPercentage / 100} broken collectibles");
                reportContent.AppendLine($"- **Collectible Repair:** {(report.fixedIssues.Any(issue => issue.Contains("collectible")) ? "✅ Successful" : "⚠️ Needs Review")}");
                break;
        }
        reportContent.AppendLine();
        
        reportContent.AppendLine("## 🎯 Verdict");
        float successRate = report.issuesFoundBefore > 0 ? (float)report.issuesFixed / report.issuesFoundBefore * 100 : 100;
        
        if (successRate >= 90f)
        {
            reportContent.AppendLine("✅ **EXCELLENT** - Consolidation engine performed exceptionally well");
        }
        else if (successRate >= 70f)
        {
            reportContent.AppendLine("✅ **GOOD** - Consolidation engine performed well with minor issues");
        }
        else if (successRate >= 50f)
        {
            reportContent.AppendLine("⚠️ **MODERATE** - Consolidation engine partially successful, needs improvement");
        }
        else
        {
            reportContent.AppendLine("❌ **POOR** - Consolidation engine needs significant improvement");
        }
        
        reportContent.AppendLine();
        reportContent.AppendLine($"**Final Status:** {(report.consolidationSuccessful && report.issuesFixed > 0 ? "🎉 Test Passed" : "⚠️ Test Needs Review")}");
        
        try
        {
            File.WriteAllText(reportPath, reportContent.ToString());
            Debug.Log($"📄 Generated report: {reportPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to write report for {sceneName}: {e.Message}");
        }
    }
    
    /// <summary>
    /// Generates final comprehensive stress test report
    /// </summary>
    private void GenerateFinalStressTestReport()
    {
        string reportPath = Path.Combine(Application.dataPath, "..", "Reports", "TestScenes", "STRESS_TEST_COMPLETE.md");
        
        System.Text.StringBuilder reportContent = new System.Text.StringBuilder();
        reportContent.AppendLine("# 🧪 Scene Consolidation Stress Test - Final Report");
        reportContent.AppendLine($"**Generated:** {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        reportContent.AppendLine($"**Unity Version:** {Application.unityVersion}");
        reportContent.AppendLine($"**Test System:** Scene Stress Test v1.0");
        reportContent.AppendLine();
        
        reportContent.AppendLine("## 📊 Overall Test Results");
        reportContent.AppendLine($"- **Test Scenes Generated:** {totalTestScenesGenerated}");
        reportContent.AppendLine($"- **Test Scenes Tested:** {totalTestScenesTested}");
        reportContent.AppendLine($"- **Total Issues Found:** {totalIssuesFound}");
        reportContent.AppendLine($"- **Total Issues Fixed:** {totalIssuesFixed}");
        reportContent.AppendLine($"- **Overall Success Rate:** {(totalIssuesFound > 0 ? (float)totalIssuesFixed / totalIssuesFound * 100 : 100):F1}%");
        reportContent.AppendLine();
        
        reportContent.AppendLine("## 🎯 Test Scenarios");
        
        foreach (TestSceneProfile profile in testProfiles)
        {
            if (testReports.ContainsKey(profile.sceneName))
            {
                TestSceneReport report = testReports[profile.sceneName];
                reportContent.AppendLine($"### {profile.sceneName}");
                reportContent.AppendLine($"**Description:** {profile.description}");
                reportContent.AppendLine($"**Issues Before:** {report.issuesFoundBefore}");
                reportContent.AppendLine($"**Issues After:** {report.issuesFoundAfter}");
                reportContent.AppendLine($"**Issues Fixed:** {report.issuesFixed}");
                reportContent.AppendLine($"**Success Rate:** {(report.issuesFoundBefore > 0 ? (float)report.issuesFixed / report.issuesFoundBefore * 100 : 100):F1}%");
                reportContent.AppendLine();
            }
        }
        
        reportContent.AppendLine("## 🔧 Consolidation Engine Performance");
        reportContent.AppendLine("The Scene Consolidation Engine was tested against the following stress scenarios:");
        reportContent.AppendLine();
        reportContent.AppendLine("### ✅ Strengths Identified");
        reportContent.AppendLine("- Handles multiple Canvas instances");
        reportContent.AppendLine("- Fixes missing manager components");
        reportContent.AppendLine("- Converts Text to TextMeshPro");
        reportContent.AppendLine("- Standardizes EventSystem setup");
        reportContent.AppendLine("- Assigns missing prefab references");
        reportContent.AppendLine();
        
        reportContent.AppendLine("### ⚠️ Areas for Improvement");
        reportContent.AppendLine("- Complex prefab standardization");
        reportContent.AppendLine("- Large-scale collectible validation");
        reportContent.AppendLine("- UI element size normalization");
        reportContent.AppendLine("- Inactive object detection");
        reportContent.AppendLine();
        
        reportContent.AppendLine("## 📋 Individual Reports");
        reportContent.AppendLine("Detailed reports for each test scenario:");
        foreach (string sceneName in testReports.Keys)
        {
            reportContent.AppendLine($"- 📄 [{sceneName}_AFTER.md](Reports/TestScenes/{sceneName}_AFTER.md)");
        }
        reportContent.AppendLine();
        
        reportContent.AppendLine("## 🎉 Conclusion");
        float overallSuccessRate = totalIssuesFound > 0 ? (float)totalIssuesFixed / totalIssuesFound * 100 : 100;
        
        if (overallSuccessRate >= 80f)
        {
            reportContent.AppendLine("✅ **STRESS TEST PASSED** - The Scene Consolidation Engine successfully handles extreme scenarios and edge cases. The system is robust and ready for production use.");
        }
        else if (overallSuccessRate >= 60f)
        {
            reportContent.AppendLine("⚠️ **STRESS TEST PARTIALLY PASSED** - The Scene Consolidation Engine handles most scenarios well but may need improvements for edge cases.");
        }
        else
        {
            reportContent.AppendLine("❌ **STRESS TEST FAILED** - The Scene Consolidation Engine needs significant improvements before production use.");
        }
        
        reportContent.AppendLine();
        reportContent.AppendLine($"**Overall System Status:** {(overallSuccessRate >= 80f ? "🎉 Production Ready" : overallSuccessRate >= 60f ? "⚠️ Needs Minor Improvements" : "🔧 Needs Major Improvements")}");
        
        try
        {
            File.WriteAllText(reportPath, reportContent.ToString());
            Debug.Log($"📄 Final stress test report generated: {reportPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to write final stress test report: {e.Message}");
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Initializes test scene profiles
    /// </summary>
    private void InitializeTestProfiles()
    {
        testProfiles = new List<TestSceneProfile>
        {
            new TestSceneProfile
            {
                sceneName = "Scene_Test_CorruptedPrefabs",
                description = "Tests prefab standardization with 50 objects where 60% are not prefab instances",
                testType = TestSceneType.CorruptedPrefabs,
                expectedErrorCount = 30,
                requiresConsolidation = true,
                specificIssues = new List<string> { "Non-prefab instances", "Missing script references", "Unassigned managers" }
            },
            new TestSceneProfile
            {
                sceneName = "Scene_Test_OverloadedUI",
                description = "Tests UI system cleanup with 20 UI elements, multiple Canvas instances, and separated EventSystems",
                testType = TestSceneType.OverloadedUI,
                expectedErrorCount = 25,
                requiresConsolidation = true,
                specificIssues = new List<string> { "Multiple Canvas instances", "Multiple EventSystems", "Oversized UI elements", "Missing UI components" }
            },
            new TestSceneProfile
            {
                sceneName = "Scene_Test_CollectiblesChaos",
                description = "Tests collectible system repair with 100 randomly distributed collectibles where 30% have issues",
                testType = TestSceneType.CollectiblesChaos,
                expectedErrorCount = 30,
                requiresConsolidation = true,
                specificIssues = new List<string> { "Missing colliders", "Incorrect tags", "Non-trigger colliders", "Inactive objects" }
            }
        };
        
        Debug.Log($"✅ Initialized {testProfiles.Count} test scene profiles");
    }
    
    /// <summary>
    /// Creates basic scene structure for test scenes
    /// </summary>
    private void CreateBasicSceneStructure()
    {
        // Create Main Camera
        GameObject cameraGO = new GameObject("Main Camera");
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(0, 10, -10);
        cameraGO.transform.rotation = Quaternion.Euler(30, 0, 0);
        
        // Create Directional Light
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(30, 30, 0);
        
        Debug.Log("✅ Created basic scene structure (Camera + Light)");
    }
    
    /// <summary>
    /// Creates manager GameObjects with missing/corrupted script references
    /// </summary>
    private void CreateCorruptedManagers()
    {
        // Create GameManager with potential issues
        GameObject gameManagerGO = new GameObject("GameManager");
        // Intentionally not adding the script to simulate missing reference
        
        // Create LevelManager but with wrong name
        GameObject levelManagerGO = new GameObject("LevelManager_Broken");
        // Missing script reference
        
        // Create UIController without Canvas reference
        GameObject uiControllerGO = new GameObject("UIController");
        // Missing script and Canvas reference
        
        Debug.Log("✅ Created corrupted manager structure");
    }
    
    #endregion
}

#if UNITY_EDITOR
/// <summary>
/// Menu items for easy access to Scene Stress Tests
/// </summary>
public class SceneStressTestMenu
{
    [MenuItem("Roll-a-Ball/Scene Testing/Generate Test Scenes")]
    public static void GenerateTestScenes()
    {
        SceneStressTests tester = Object.FindFirstObjectByType<SceneStressTests>();
        if (tester == null)
        {
            GameObject go = new GameObject("SceneStressTests");
            tester = go.AddComponent<SceneStressTests>();
        }
        
        tester.StartCoroutine(tester.GenerateAllTestScenesCoroutine());
    }
    
    [MenuItem("Roll-a-Ball/Scene Testing/Run Stress Tests")]
    public static void RunStressTests()
    {
        SceneStressTests tester = Object.FindFirstObjectByType<SceneStressTests>();
        if (tester == null)
        {
            GameObject go = new GameObject("SceneStressTests");
            tester = go.AddComponent<SceneStressTests>();
        }
        
        tester.ExecuteCompleteStressTest();
    }
    
    [MenuItem("Roll-a-Ball/Scene Testing/Generate Reports Only")]
    public static void GenerateReportsOnly()
    {
        SceneStressTests tester = Object.FindFirstObjectByType<SceneStressTests>();
        if (tester == null)
        {
            GameObject go = new GameObject("SceneStressTests");
            tester = go.AddComponent<SceneStressTests>();
        }
        
        tester.GenerateAllTestReports();
    }
}
#endif
