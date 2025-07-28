using UnityEngine;
using UnityEditor;
using System.Collections;

// TODO-REMOVE#7: Obsolete setup script – no longer needed and may corrupt current data
/// <summary>
/// Quick Scene Consolidation Executor - Easy access to Scene Consolidation Engine
/// Place this script on any GameObject and run from Inspector or menu
/// </summary>
[AddComponentMenu("Roll-a-Ball/Quick Scene Consolidator")]
public class QuickSceneConsolidator : MonoBehaviour
{
    [Header("🚀 Quick Actions")]
    [SerializeField] private bool consolidateCurrentScene = false;
    [SerializeField] private bool consolidateAllScenes = false;
    [SerializeField] private bool generateReport = false;
    
    [Header("📊 Status")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] private string lastOperation = "None";
    [SerializeField] private int lastFixCount = 0;
    
    private SceneConsolidationEngine engine;
    
    private void Start()
    {
        // Get or create the consolidation engine
        engine = Object.FindFirstObjectByType<SceneConsolidationEngine>();
        if (engine == null)
        {
            GameObject engineGO = new GameObject("SceneConsolidationEngine");
            engine = engineGO.AddComponent<SceneConsolidationEngine>();
        }
    }
    
    private void OnValidate()
    {
        if (consolidateCurrentScene)
        {
            consolidateCurrentScene = false;
            ExecuteCurrentSceneConsolidation();
        }
        
        if (consolidateAllScenes)
        {
            consolidateAllScenes = false;
            ExecuteAllScenesConsolidation();
        }
        
        if (generateReport)
        {
            generateReport = false;
            GenerateStatusReport();
        }
    }
    
    [ContextMenu("Consolidate Current Scene")]
    public void ExecuteCurrentSceneConsolidation()
    {
        if (isRunning) return;
        
        Debug.Log("🔧 Starting Current Scene Consolidation...");
        StartCoroutine(ConsolidateCurrentSceneCoroutine());
    }
    
    [ContextMenu("Consolidate All Scenes")]
    public void ExecuteAllScenesConsolidation()
    {
        if (isRunning) return;
        
        Debug.Log("🚀 Starting All Scenes Consolidation...");
        StartCoroutine(ConsolidateAllScenesCoroutine());
    }
    
    [ContextMenu("Generate Status Report")]
    public void GenerateStatusReport()
    {
        Debug.Log("📄 Generating Scene Consolidation Status Report...");
        
        string report = "# 🎯 Scene Consolidation Status Report\n\n";
        report += $"**Generated:** {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
        report += $"**Current Scene:** {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}\n";
        report += $"**Last Operation:** {lastOperation}\n";
        report += $"**Last Fix Count:** {lastFixCount}\n";
        report += $"**Engine Status:** {(engine != null ? "Available" : "Missing")}\n\n";
        
        report += "## 🔧 Available Operations\n";
        report += "- ✅ Current Scene Consolidation\n";
        report += "- ✅ All Scenes Consolidation\n";
        report += "- ✅ Automated Repair Report Generation\n\n";
        
        report += "## 📋 Usage Instructions\n";
        report += "1. **Current Scene Only:** Check 'consolidateCurrentScene' in Inspector\n";
        report += "2. **All Scenes:** Check 'consolidateAllScenes' in Inspector\n";
        report += "3. **Report:** Check 'generateReport' in Inspector\n\n";
        
        report += "**Status:** 🎉 Scene Consolidation System Ready!\n";
        
        Debug.Log(report);
    }
    
    private IEnumerator ConsolidateCurrentSceneCoroutine()
    {
        isRunning = true;
        lastOperation = "Current Scene Consolidation";
        
        if (engine != null)
        {
            yield return engine.ConsolidateCurrentScene();
            lastFixCount = engine.totalIssuesFixed;
            Debug.Log($"✅ Current Scene Consolidation Complete! Fixed {lastFixCount} issues.");
        }
        else
        {
            Debug.LogError("❌ SceneConsolidationEngine not found!");
        }
        
        isRunning = false;
    }
    
    private IEnumerator ConsolidateAllScenesCoroutine()
    {
        isRunning = true;
        lastOperation = "All Scenes Consolidation";
        
        if (engine != null)
        {
            yield return engine.ConsolidateAllScenesAsync();
            lastFixCount = engine.totalIssuesFixed;
            Debug.Log($"✅ All Scenes Consolidation Complete! Fixed {lastFixCount} issues across {engine.totalScenesProcessed} scenes.");
        }
        else
        {
            Debug.LogError("❌ SceneConsolidationEngine not found!");
        }
        
        isRunning = false;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Menu items for easy access to Scene Consolidation
/// </summary>
public class SceneConsolidationMenu
{
    [MenuItem("Roll-a-Ball/Scene Consolidation/Consolidate Current Scene")]
    public static void ConsolidateCurrentScene()
    {
        QuickSceneConsolidator consolidator = Object.FindFirstObjectByType<QuickSceneConsolidator>();
        if (consolidator == null)
        {
            GameObject go = new GameObject("QuickSceneConsolidator");
            consolidator = go.AddComponent<QuickSceneConsolidator>();
        }
        
        consolidator.ExecuteCurrentSceneConsolidation();
    }
    
    [MenuItem("Roll-a-Ball/Scene Consolidation/Consolidate All Scenes")]
    public static void ConsolidateAllScenes()
    {
        QuickSceneConsolidator consolidator = Object.FindFirstObjectByType<QuickSceneConsolidator>();
        if (consolidator == null)
        {
            GameObject go = new GameObject("QuickSceneConsolidator");
            consolidator = go.AddComponent<QuickSceneConsolidator>();
        }
        
        consolidator.ExecuteAllScenesConsolidation();
    }
    
    [MenuItem("Roll-a-Ball/Scene Consolidation/Generate Status Report")]
    public static void GenerateStatusReport()
    {
        QuickSceneConsolidator consolidator = Object.FindFirstObjectByType<QuickSceneConsolidator>();
        if (consolidator == null)
        {
            GameObject go = new GameObject("QuickSceneConsolidator");
            consolidator = go.AddComponent<QuickSceneConsolidator>();
        }
        
        consolidator.GenerateStatusReport();
    }
}
#endif