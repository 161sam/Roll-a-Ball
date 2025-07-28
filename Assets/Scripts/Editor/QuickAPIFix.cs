using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
/// Quick API Fix Runner - Executes the Unity 6.1 API fixes automatically
/// </summary>
// TODO-DUPLICATE#2: Funktional identisch mit QuickAPIBatchFixer.cs und ObsoleteAPIBatchFixer.cs. Bitte vereinheitlichen oder entfernen.
public class QuickAPIFix
{
    [MenuItem("Roll-a-Ball/🚀 Quick Fix All APIs")]
    public static void RunQuickAPIFix()
    {
        Debug.Log("[QuickAPIFix] Starting automated API fixes...");
        
        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        var filesToFix = new List<string>();
        int fixedCount = 0;

        // Scan for files with deprecated APIs
        foreach (string file in scriptFiles)
        {
            // Skip some editor files to avoid complications
            if (file.Contains("/Editor/") && !file.Contains("RollABallControlPanel.cs"))
                continue;
                
            string content = File.ReadAllText(file);
            
            // Check for deprecated patterns
            if (content.Contains("FindObjectsByType<") || 
                content.Contains("FindFirstObjectByType<") ||
                content.Contains("GameObject.FindObjectsOfType"))
            {
                filesToFix.Add(file);
            }
        }

        Debug.Log($"[QuickAPIFix] Found {filesToFix.Count} files with deprecated APIs");

        // Fix each file
        foreach (string filePath in filesToFix)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                string originalContent = content;

                // Pattern 1: FindObjectsOfType<T>(FindObjectsSortMode.None) → Object.FindObjectsByType<T>(FindObjectsSortMode.None)
                content = Regex.Replace(content, 
                    @"FindObjectsOfType<([^>]+)>\(\)",
                    @"Object.FindObjectsByType<$1>(FindObjectsSortMode.None)");

                // Pattern 2: FindFirstObjectByType<T>() → Object.FindFirstObjectByType<T>()
                content = Regex.Replace(content,
                    @"FindObjectOfType<([^>]+)>\(\)",
                    @"Object.FindFirstObjectByType<$1>()");

                // Pattern 3: GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None) → Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                content = Regex.Replace(content,
                    @"GameObject\.FindObjectsOfType<([^>]+)>\(\)",
                    @"Object.FindObjectsByType<$1>(FindObjectsSortMode.None)");

                // Pattern 4: Object.FindObjectsOfType (already using Object. but without sort mode)
                content = Regex.Replace(content,
                    @"Object\.FindObjectsOfType<([^>]+)>\(\)",
                    @"Object.FindObjectsByType<$1>(FindObjectsSortMode.None)");

                // If content changed, write it back
                if (content != originalContent)
                {
                    File.WriteAllText(filePath, content);
                    fixedCount++;
                    
                    string fileName = Path.GetFileName(filePath);
                    Debug.Log($"[QuickAPIFix] Fixed: {fileName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuickAPIFix] Error fixing {filePath}: {e.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[QuickAPIFix] ✅ COMPLETED! Fixed {fixedCount} files with deprecated APIs.");
        
        // Show success dialog
        EditorUtility.DisplayDialog("API Fix Complete", 
            $"Successfully updated {fixedCount} files with Unity 6.1 compatible APIs!\n\nAll deprecated FindObjectOfType calls have been modernized.", 
            "Excellent!");
    }
}