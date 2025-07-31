using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Scene Stress Testing System for Roll-a-Ball project
/// Tests level generation performance and stability
/// </summary>
[System.Serializable]
public class SceneStressTests : MonoBehaviour
{
    [Header("Stress Test Configuration")]
    [SerializeField] private int maxGenerationAttempts = 10;
    [SerializeField] private float testTimeoutSeconds = 30f;
    [SerializeField] private bool logDetailedResults = true;
    // Note: testAllProfiles removed as it was unused
    
    private int completedTests = 0;
    private int failedTests = 0;
    private System.Text.StringBuilder testResults;
    
    /// <summary>
    /// Execute complete stress test suite
    /// </summary>
    public void ExecuteCompleteStressTest()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(ExecuteStressTestCoroutine());
        }
        else
        {
            Debug.LogWarning("Stress tests should be run in Play Mode for accurate results");
            ExecuteEditorStressTest();
        }
    }
    
    /// <summary>
    /// Execute stress test in editor mode (limited functionality)
    /// </summary>
    public void ExecuteEditorStressTest()
    {
        testResults = new System.Text.StringBuilder();
        completedTests = 0;
        failedTests = 0;
        
        testResults.AppendLine("🧪 Scene Stress Test Results (Editor Mode)");
        testResults.AppendLine($"Started at: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        testResults.AppendLine();
        
        // Test 1: Check for LevelGenerator presence
        TestLevelGeneratorPresence();
        
        // Test 2: Validate scene setup
        TestSceneSetup();
        
        // Test 3: Check prefab references
        TestPrefabReferences();
        
        // Test 4: Validate UI connections
        TestUIConnections();
        
        // Generate final report
        GenerateFinalReport();
    }
    
    private IEnumerator ExecuteStressTestCoroutine()
    {
        testResults = new System.Text.StringBuilder();
        completedTests = 0;
        failedTests = 0;
        
        testResults.AppendLine("🧪 Scene Stress Test Results (Play Mode)");
        testResults.AppendLine($"Started at: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        testResults.AppendLine();
        
        // Runtime tests
        yield return StartCoroutine(TestLevelGeneration());
        yield return StartCoroutine(TestPerformanceStress());
        yield return StartCoroutine(TestMemoryUsage());
        
        GenerateFinalReport();
    }
    
    private void TestLevelGeneratorPresence()
    {
        testResults.AppendLine("### Test 1: LevelGenerator Presence");
        
        try
        {
            // TODO: Inject LevelGenerator reference instead of using FindFirstObjectByType
            LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            bool shouldHaveGenerator = sceneName == "GeneratedLevel" || sceneName == "Level_OSM";
            bool hasGenerator = generator != null;
            
            if (shouldHaveGenerator && hasGenerator)
            {
                testResults.AppendLine("✅ PASS: LevelGenerator found in procedural scene");
                completedTests++;
            }
            else if (!shouldHaveGenerator && !hasGenerator)
            {
                testResults.AppendLine("✅ PASS: No LevelGenerator in static scene (correct)");
                completedTests++;
            }
            else if (shouldHaveGenerator && !hasGenerator)
            {
                testResults.AppendLine("❌ FAIL: Missing LevelGenerator in procedural scene");
                failedTests++;
            }
            else
            {
                testResults.AppendLine("⚠️ WARNING: LevelGenerator found in static scene (may cause issues)");
                failedTests++;
            }
        }
        catch (System.Exception e)
        {
            testResults.AppendLine($"❌ ERROR: Exception in LevelGenerator test: {e.Message}");
            failedTests++;
        }
        
        testResults.AppendLine();
    }
    
    private void TestSceneSetup()
    {
        testResults.AppendLine("### Test 2: Scene Setup Validation");
        
        try
        {
            // Check for essential GameObjects
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            UIController uiController = FindFirstObjectByType<UIController>();
            
            testResults.AppendLine($"Player: {(player ? "✅ Found" : "❌ Missing")}");
            testResults.AppendLine($"GameManager: {(gameManager ? "✅ Found" : "❌ Missing")}");
            testResults.AppendLine($"LevelManager: {(levelManager ? "✅ Found" : "❌ Missing")}");
            testResults.AppendLine($"UIController: {(uiController ? "✅ Found" : "❌ Missing")}");
            
            int score = 0;
            if (player) score++;
            if (gameManager) score++;
            if (levelManager) score++;
            if (uiController) score++;
            
            if (score >= 3)
            {
                testResults.AppendLine("✅ PASS: Essential scene setup complete");
                completedTests++;
            }
            else
            {
                testResults.AppendLine($"❌ FAIL: Missing essential components (Score: {score}/4)");
                failedTests++;
            }
        }
        catch (System.Exception e)
        {
            testResults.AppendLine($"❌ ERROR: Exception in scene setup test: {e.Message}");
            failedTests++;
        }
        
        testResults.AppendLine();
    }
    
    private void TestPrefabReferences()
    {
        testResults.AppendLine("### Test 3: Prefab References");
        
        try
        {
            LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
            
            if (generator != null)
            {
                // Use reflection to check prefab fields
                var fields = generator.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                int prefabCount = 0;
                int validPrefabs = 0;
                
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(GameObject) && field.Name.ToLower().Contains("prefab"))
                    {
                        prefabCount++;
                        GameObject prefab = (GameObject)field.GetValue(generator);
                        if (prefab != null)
                        {
                            validPrefabs++;
                            testResults.AppendLine($"✅ {field.Name}: Valid");
                        }
                        else
                        {
                            testResults.AppendLine($"❌ {field.Name}: Missing");
                        }
                    }
                }
                
                if (validPrefabs == prefabCount && prefabCount > 0)
                {
                    testResults.AppendLine("✅ PASS: All prefab references valid");
                    completedTests++;
                }
                else
                {
                    testResults.AppendLine($"❌ FAIL: Missing prefab references ({validPrefabs}/{prefabCount})");
                    failedTests++;
                }
            }
            else
            {
                testResults.AppendLine("⚠️ SKIP: No LevelGenerator found for prefab testing");
            }
        }
        catch (System.Exception e)
        {
            testResults.AppendLine($"❌ ERROR: Exception in prefab test: {e.Message}");
            failedTests++;
        }
        
        testResults.AppendLine();
    }
    
    private void TestUIConnections()
    {
        testResults.AppendLine("### Test 4: UI Connections");
        
        try
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            UnityEngine.UI.Button[] buttons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
            TMPro.TextMeshProUGUI[] texts = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None);
            
            testResults.AppendLine($"Canvases: {canvases.Length}");
            testResults.AppendLine($"Buttons: {buttons.Length}");
            testResults.AppendLine($"UI Texts: {texts.Length}");
            
            // Check for common UI issues
            int workingButtons = 0;
            foreach (var button in buttons)
            {
                if (button.onClick.GetPersistentEventCount() > 0)
                {
                    workingButtons++;
                }
            }
            
            testResults.AppendLine($"Functional Buttons: {workingButtons}/{buttons.Length}");
            
            if (canvases.Length > 0 && (buttons.Length == 0 || workingButtons > 0))
            {
                testResults.AppendLine("✅ PASS: Basic UI setup detected");
                completedTests++;
            }
            else
            {
                testResults.AppendLine("❌ FAIL: UI setup issues detected");
                failedTests++;
            }
        }
        catch (System.Exception e)
        {
            testResults.AppendLine($"❌ ERROR: Exception in UI test: {e.Message}");
            failedTests++;
        }
        
        testResults.AppendLine();
    }
    
    private IEnumerator TestLevelGeneration()
    {
        testResults.AppendLine("### Test 5: Level Generation Performance");
        
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator == null)
        {
            testResults.AppendLine("⚠️ SKIP: No LevelGenerator found for performance testing");
            yield break;
        }
        
        float startTime = Time.realtimeSinceStartup;
        int successfulGenerations = 0;
        
        for (int i = 0; i < maxGenerationAttempts; i++)
        {
            float attemptStart = Time.realtimeSinceStartup;
            
            // Trigger regeneration outside try block
            generator.RegenerateLevel();
            
            // Wait for generation to complete
            yield return new WaitForSeconds(0.1f);
            
            // Wait for generator to finish - moved outside try block
            float timeout = Time.realtimeSinceStartup + testTimeoutSeconds;
            while (generator.IsGenerating && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            
            try
            {
                float attemptTime = Time.realtimeSinceStartup - attemptStart;
                
                if (!generator.IsGenerating)
                {
                    successfulGenerations++;
                    testResults.AppendLine($"✅ Generation {i + 1}: {attemptTime:F2}s");
                }
                else
                {
                    testResults.AppendLine($"❌ Generation {i + 1}: Timeout ({testTimeoutSeconds}s)");
                }
            }
            catch (System.Exception e)
            {
                testResults.AppendLine($"❌ Generation {i + 1}: Exception - {e.Message}");
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        float totalTime = Time.realtimeSinceStartup - startTime;
        float avgTime = totalTime / maxGenerationAttempts;
        
        testResults.AppendLine($"Successful: {successfulGenerations}/{maxGenerationAttempts}");
        testResults.AppendLine($"Average Time: {avgTime:F2}s");
        
        if (successfulGenerations >= maxGenerationAttempts * 0.8f)
        {
            testResults.AppendLine("✅ PASS: Level generation performance acceptable");
            completedTests++;
        }
        else
        {
            testResults.AppendLine("❌ FAIL: Too many generation failures or timeouts");
            failedTests++;
        }
        
        testResults.AppendLine();
    }
    
    private IEnumerator TestPerformanceStress()
    {
        testResults.AppendLine("### Test 6: Performance Stress Test");
        
        // Measure frame rate over time
        float[] frameRates = new float[60]; // 1 second of frame rate data
        
        for (int i = 0; i < frameRates.Length; i++)
        {
            try
            {
                frameRates[i] = 1f / Time.unscaledDeltaTime;
            }
            catch (System.Exception e)
            {
                testResults.AppendLine($"❌ ERROR: Exception measuring frame rate: {e.Message}");
                failedTests++;
                testResults.AppendLine();
                yield break;
            }
            yield return null;
        }
        
        float avgFrameRate = 0f;
        float minFrameRate = float.MaxValue;
        
        foreach (float rate in frameRates)
        {
            avgFrameRate += rate;
            if (rate < minFrameRate) minFrameRate = rate;
        }
        avgFrameRate /= frameRates.Length;
        
        testResults.AppendLine($"Average FPS: {avgFrameRate:F1}");
        testResults.AppendLine($"Minimum FPS: {minFrameRate:F1}");
        
        if (avgFrameRate > 30f && minFrameRate > 15f)
        {
            testResults.AppendLine("✅ PASS: Performance within acceptable range");
            completedTests++;
        }
        else
        {
            testResults.AppendLine("❌ FAIL: Performance below acceptable threshold");
            failedTests++;
        }
        
        testResults.AppendLine();
    }
    
    private IEnumerator TestMemoryUsage()
    {
        testResults.AppendLine("### Test 7: Memory Usage Test");
        
        long startMemory = System.GC.GetTotalMemory(false);
        
        // Trigger some operations outside try block
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator != null)
        {
            try
            {
                generator.RegenerateLevel();
            }
            catch (System.Exception e)
            {
                testResults.AppendLine($"❌ ERROR: Exception during level regeneration: {e.Message}");
                failedTests++;
                testResults.AppendLine();
                yield break;
            }
            
            // Wait outside try block
            yield return new WaitForSeconds(2f);
        }
        
        long endMemory = System.GC.GetTotalMemory(false);
        long memoryDiff = endMemory - startMemory;
        
        testResults.AppendLine($"Start Memory: {startMemory / 1024 / 1024:F1} MB");
        testResults.AppendLine($"End Memory: {endMemory / 1024 / 1024:F1} MB");
        testResults.AppendLine($"Memory Change: {memoryDiff / 1024 / 1024:F1} MB");
        
        if (memoryDiff < 50 * 1024 * 1024) // Less than 50MB increase
        {
            testResults.AppendLine("✅ PASS: Memory usage within acceptable range");
            completedTests++;
        }
        else
        {
            testResults.AppendLine("❌ FAIL: Excessive memory usage detected");
            failedTests++;
        }
        
        testResults.AppendLine();
    }
    
    private void GenerateFinalReport()
    {
        testResults.AppendLine("## 📊 Final Test Results");
        testResults.AppendLine($"Completed at: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        testResults.AppendLine($"Passed Tests: {completedTests}");
        testResults.AppendLine($"Failed Tests: {failedTests}");
        testResults.AppendLine($"Total Tests: {completedTests + failedTests}");
        
        float successRate = completedTests / (float)(completedTests + failedTests) * 100f;
        testResults.AppendLine($"Success Rate: {successRate:F1}%");
        
        if (successRate >= 80f)
        {
            testResults.AppendLine("🎉 Overall Result: PASS - Scene is in good condition");
        }
        else if (successRate >= 60f)
        {
            testResults.AppendLine("⚠️ Overall Result: WARNING - Some issues detected");
        }
        else
        {
            testResults.AppendLine("❌ Overall Result: FAIL - Significant issues need attention");
        }
        
        // Output results
        string report = testResults.ToString();
        Debug.Log(report);
        
        if (logDetailedResults)
        {
            // Try to save report to file
            try
            {
                string reportPath = System.IO.Path.Combine(Application.dataPath, "..", "Reports", "StressTestResults.md");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath));
                System.IO.File.WriteAllText(reportPath, report);
                Debug.Log($"📄 Detailed report saved: {reportPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not save report file: {e.Message}");
            }
        }
    }
}
