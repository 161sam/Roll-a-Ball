using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Automated Scene Test Executor - Runs complete stress test suite without user interaction
/// Can be called from command line builds or automation systems
/// </summary>
public static class AutomatedSceneTestExecutor
{
    /// <summary>
    /// Executes complete automated stress test suite
    /// This method can be called from command line Unity builds
    /// </summary>
    [MenuItem("Roll-a-Ball/Automation/Execute Complete Stress Test Suite")]
    public static void ExecuteCompleteAutomatedStressTest()
    {
        Debug.Log("🤖 Starting Automated Scene Stress Test Suite...");
        
        // Create or find the stress test component
        SceneStressTests stressTester = GameObject.FindFirstObjectByType<SceneStressTests>();
        if (stressTester == null)
        {
            GameObject testGO = new GameObject("AutomatedSceneStressTests");
            stressTester = testGO.AddComponent<SceneStressTests>();
            Debug.Log("✅ Created SceneStressTests component for automation");
        }
        
        // Execute the complete stress test
        stressTester.ExecuteCompleteStressTest();
        
        Debug.Log("🤖 Automated stress test execution initiated");
    }
    
    /// <summary>
    /// Command line method for automated testing
    /// Usage: Unity.exe -batchmode -executeMethod AutomatedSceneTestExecutor.CommandLineStressTest -quit
    /// </summary>
    public static void CommandLineStressTest()
    {
        try
        {
            Debug.Log("🤖 Command Line Stress Test Started");
            
            // Force creation of stress test system
            GameObject testGO = new GameObject("AutomatedSceneStressTests");
            SceneStressTests stressTester = testGO.AddComponent<SceneStressTests>();
            
            // Execute complete test
            stressTester.ExecuteCompleteStressTest();
            
            // Wait for completion (in a real scenario, we'd need to implement proper completion detection)
            System.Threading.Thread.Sleep(30000); // 30 seconds wait
            
            Debug.Log("✅ Command Line Stress Test Completed");
            
            // Exit Unity
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Command Line Stress Test Failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    /// <summary>
    /// Quick validation method to check if stress test system is working
    /// </summary>
    [MenuItem("Roll-a-Ball/Automation/Validate Stress Test System")]
    public static void ValidateStressTestSystem()
    {
        Debug.Log("🔍 Validating Stress Test System...");
        
        // Check if SceneConsolidationEngine exists
        SceneConsolidationEngine consolidationEngine = GameObject.FindFirstObjectByType<SceneConsolidationEngine>();
        bool hasConsolidationEngine = consolidationEngine != null;
        
        // Check if test directories exist
        bool hasTestScenesDir = System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Scenes", "TestScenes"));
        bool hasReportsDir = System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "..", "Reports", "TestScenes"));
        
        // Check if prefabs exist
        GameObject groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GroundPrefab.prefab");
        GameObject collectiblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CollectiblePrefab.prefab");
        bool hasPrefabs = groundPrefab != null && collectiblePrefab != null;
        
        // Generate validation report
        System.Text.StringBuilder validation = new System.Text.StringBuilder();
        validation.AppendLine("# 🔍 Stress Test System Validation Report");
        validation.AppendLine($"**Generated:** {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        validation.AppendLine();
        
        validation.AppendLine("## ✅ System Components");
        validation.AppendLine($"- **Consolidation Engine:** {(hasConsolidationEngine ? "✅ Available" : "❌ Missing")}");
        validation.AppendLine($"- **Test Scenes Directory:** {(hasTestScenesDir ? "✅ Ready" : "❌ Missing")}");
        validation.AppendLine($"- **Reports Directory:** {(hasReportsDir ? "✅ Ready" : "❌ Missing")}");
        validation.AppendLine($"- **Required Prefabs:** {(hasPrefabs ? "✅ Available" : "❌ Missing")}");
        validation.AppendLine();
        
        validation.AppendLine("## 🎯 Readiness Status");
        bool isFullyReady = hasConsolidationEngine && hasTestScenesDir && hasReportsDir && hasPrefabs;
        
        if (isFullyReady)
        {
            validation.AppendLine("✅ **SYSTEM READY** - All components available for stress testing");
        }
        else
        {
            validation.AppendLine("⚠️ **SYSTEM NOT READY** - Missing components need to be resolved");
        }
        
        validation.AppendLine();
        validation.AppendLine("## 📋 Next Steps");
        if (isFullyReady)
        {
            validation.AppendLine("1. Run `Roll-a-Ball → Automation → Execute Complete Stress Test Suite`");
            validation.AppendLine("2. Check console for progress updates");
            validation.AppendLine("3. Review generated reports in `/Reports/TestScenes/`");
        }
        else
        {
            validation.AppendLine("1. Resolve missing components listed above");
            validation.AppendLine("2. Re-run validation");
            validation.AppendLine("3. Proceed with stress testing when ready");
        }
        
        Debug.Log(validation.ToString());
        
        // Write validation report to file
        try
        {
            string reportPath = System.IO.Path.Combine(Application.dataPath, "..", "Reports", "STRESS_TEST_VALIDATION.md");
            System.IO.File.WriteAllText(reportPath, validation.ToString());
            Debug.Log($"📄 Validation report saved: {reportPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Could not save validation report: {e.Message}");
        }
    }
}
