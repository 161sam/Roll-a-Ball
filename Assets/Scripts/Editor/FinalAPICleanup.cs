using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Final API Cleanup - Fix remaining Object.FindObjectOfType calls
/// </summary>
// TODO-DUPLICATE#2: Funktional identisch mit QuickAPIFix.cs. Bitte vereinheitlichen oder entfernen.
public class FinalAPICleanup
{
    [MenuItem("Roll-a-Ball/🎯 Final API Cleanup")]
    public static void RunFinalCleanup()
    {
        Debug.Log("[FinalAPICleanup] Starting final API cleanup...");
        
        string[] filesToCheck = {
            "Assets/Scripts/SceneStressTests.cs",
            "Assets/Scripts/QuickSceneConsolidator.cs",
            "Assets/Scripts/SceneConsolidationEngine.cs",
            "Assets/Scripts/Map/OSMSceneCompleter.cs"
        };
        
        int fixedCount = 0;
        
        foreach (string filePath in filesToCheck)
        {
            string fullPath = Application.dataPath.Replace("Assets", "") + filePath;
            
            if (File.Exists(fullPath))
            {
                try
                {
                    string content = File.ReadAllText(fullPath);
                    string originalContent = content;

                    // Replace Object.FindFirstObjectByType<T>() with Object.FindFirstObjectByType<T>()
                    content = Regex.Replace(content,
                        @"Object\.FindObjectOfType<([^>]+)>\(\)",
                        @"Object.FindFirstObjectByType<$1>()");

                    if (content != originalContent)
                    {
                        File.WriteAllText(fullPath, content);
                        fixedCount++;
                        Debug.Log($"[FinalAPICleanup] Fixed: {Path.GetFileName(fullPath)}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[FinalAPICleanup] Error fixing {filePath}: {e.Message}");
                }
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"[FinalAPICleanup] ✅ COMPLETED! Fixed {fixedCount} more files.");
        
        EditorUtility.DisplayDialog("Final Cleanup Complete", 
            $"Successfully cleaned up {fixedCount} additional files!\n\nAll Unity 6.1 API warnings should now be resolved.", 
            "Perfect!");
    }
}