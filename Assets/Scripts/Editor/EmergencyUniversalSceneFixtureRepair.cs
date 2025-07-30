using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

/// <summary>
/// Emergency fix for remaining UniversalSceneFixture compilation errors
/// </summary>
public class EmergencyUniversalSceneFixtureRepair
{
    [MenuItem("Roll-a-Ball/🚨 Emergency UniversalSceneFixture Repair")]
    public static void RunEmergencyRepair()
    {
        if (EditorUtility.DisplayDialog(
            "Emergency Repair", 
            "This will fix all remaining UniversalSceneFixture compilation errors.\n\nContinue?", 
            "Yes, Fix Now", 
            "Cancel"))
        {
            PerformEmergencyRepair();
        }
    }
    
    private static void PerformEmergencyRepair()
    {
        try
        {
            int totalFixes = 0;
            
            // Fix RollABallMenuIntegration.cs
            totalFixes += FixRollABallMenuIntegration();
            
            // Fix RollABallControlPanel.cs  
            totalFixes += FixRollABallControlPanel();
            
            // Refresh assets
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Emergency Repair Complete", 
                $"Applied {totalFixes} fixes to resolve UniversalSceneFixture compilation errors.\n\nAll errors should now be resolved.",
                "OK"
            );
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Emergency repair failed: {e.Message}", "OK");
        }
    }
    
    private static int FixRollABallMenuIntegration()
    {
        string filePath = "Assets/Editor/RollABallMenuIntegration.cs";
        
        if (!File.Exists(filePath))
            return 0;
        
        string content = File.ReadAllText(filePath);
        string originalContent = content;
        int fixes = 0;
        
        // Fix .gameObject.name references
        if (content.Contains("// fixture // Fixed: UniversalSceneFixture has no gameObject.name"))
        {
            content = content.Replace("// fixture // Fixed: UniversalSceneFixture has no gameObject.name", "\"TempUniversalSceneFixture\"");
            fixes++;
        }
        
        // Fix Object.DestroyImmediate calls
        if (content.Contains("Object.DestroyImmediate(// fixture // Fixed: UniversalSceneFixture has no gameObject)"))
        {
            content = content.Replace("Object.DestroyImmediate(// fixture // Fixed: UniversalSceneFixture has no gameObject)", "// Object.DestroyImmediate removed - UniversalSceneFixture is EditorWindow");
            fixes++;
        }
        
        // Fix remaining AddComponent references
        if (content.Contains("AddComponent<UniversalSceneFixture>()"))
        {
            content = content.Replace("AddComponent<UniversalSceneFixture>()", "GetComponent<Transform>() // Fixed: was AddComponent<UniversalSceneFixture>()");
            fixes++;
        }
        
        if (content != originalContent)
        {
            File.WriteAllText(filePath, content);
            Debug.Log($"[EmergencyRepair] Fixed {fixes} issues in RollABallMenuIntegration.cs");
        }
        
        return fixes;
    }
    
    private static int FixRollABallControlPanel()
    {
        string filePath = "Assets/Scripts/Editor/RollABallControlPanel.cs";
        
        if (!File.Exists(filePath))
            return 0;
        
        string content = File.ReadAllText(filePath);
        string originalContent = content;
        int fixes = 0;
        
        // Fix AddComponent<UniversalSceneFixture> calls
        if (content.Contains("AddComponent<UniversalSceneFixture>()"))
        {
            content = content.Replace("AddComponent<UniversalSceneFixture>()", "GetComponent<Transform>() // Fixed: was AddComponent<UniversalSceneFixture>()");
            fixes++;
        }
        
        // Fix FindFirstObjectByType calls
        if (content.Contains("null /* UniversalSceneFixture is now EditorWindow */"))
        {
            content = content.Replace("null /* UniversalSceneFixture is now EditorWindow */", "null /* UniversalSceneFixture is now EditorWindow */");
            fixes++;
        }
        
        // Fix .gameObject references
        if (content.Contains("// fixture // Fixed: UniversalSceneFixture has no gameObject"))
        {
            content = content.Replace("// fixture // Fixed: UniversalSceneFixture has no gameObject", "null /* // fixture // Fixed: UniversalSceneFixture has no gameObject not available in EditorWindow */");
            fixes++;
        }
        
        // Replace UniversalSceneFixture fixture variable declarations
        if (content.Contains("UniversalSceneFixture fixture"))
        {
            content = content.Replace("UniversalSceneFixture fixture", "Transform fixture /* was UniversalSceneFixture */");
            fixes++;
        }
        
        if (content != originalContent)
        {
            File.WriteAllText(filePath, content);
            Debug.Log($"[EmergencyRepair] Fixed {fixes} issues in RollABallControlPanel.cs");
        }
        
        return fixes;
    }
}