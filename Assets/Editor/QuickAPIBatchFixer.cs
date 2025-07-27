using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace RollABall.Editor
{
    /// <summary>
    /// Simple batch fixer for deprecated Unity API calls
    /// Focuses on the most common warnings in Unity 6.1
    /// </summary>
    public class QuickAPIBatchFixer : EditorWindow
    {
        [MenuItem("Roll-a-Ball/Fix Tools/Quick API Batch Fix")]
        public static void FixAllDeprecatedAPIs()
        {
            int filesFixed = 0;
            int warningsFixed = 0;
            
            Debug.Log("[QuickAPIBatchFixer] Starting batch fix process...");
            
            // Get all C# files in the project
            string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            
            foreach (string file in files)
            {
                // Skip TextMesh Pro and other third-party files
                if (file.Contains("TextMesh Pro") || file.Contains("Packages"))
                    continue;
                
                try
                {
                    string content = File.ReadAllText(file);
                    string originalContent = content;
                    int fileWarnings = 0;
                    
                    // Fix FindFirstObjectByType<T>() → FindFirstObjectByType<T>()
                    var matches = Regex.Matches(content, @"FindObjectOfType<([^>]+)>\(\)");
                    content = Regex.Replace(content, @"FindObjectOfType<([^>]+)>\(\)", "FindFirstObjectByType<$1>()");
                    fileWarnings += matches.Count;
                    
                    // Fix FindObjectsByType<T>(FindObjectsSortMode.None) → FindObjectsByType<T>(FindObjectsSortMode.None)
                    matches = Regex.Matches(content, @"FindObjectsOfType<([^>]+)>\(\)");
                    content = Regex.Replace(content, @"FindObjectsOfType<([^>]+)>\(\)", "FindObjectsByType<$1>(FindObjectsSortMode.None)");
                    fileWarnings += matches.Count;
                    
                    // Fix Object.FindFirstObjectByType<T>() → Object.FindFirstObjectByType<T>()
                    matches = Regex.Matches(content, @"Object\.FindObjectOfType<([^>]+)>\(\)");
                    content = Regex.Replace(content, @"Object\.FindObjectOfType<([^>]+)>\(\)", "Object.FindFirstObjectByType<$1>()");
                    fileWarnings += matches.Count;
                    
                    // Fix Object.FindObjectsByType<T>(FindObjectsSortMode.None) → Object.FindObjectsByType<T>(FindObjectsSortMode.None)
                    matches = Regex.Matches(content, @"Object\.FindObjectsOfType<([^>]+)>\(\)");
                    content = Regex.Replace(content, @"Object\.FindObjectsOfType<([^>]+)>\(\)", "Object.FindObjectsByType<$1>(FindObjectsSortMode.None)");
                    fileWarnings += matches.Count;
                    
                    if (content != originalContent)
                    {
                        File.WriteAllText(file, content);
                        filesFixed++;
                        warningsFixed += fileWarnings;
                        Debug.Log($"[QuickAPIBatchFixer] Fixed {fileWarnings} warnings in {Path.GetFileName(file)}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[QuickAPIBatchFixer] Error processing {Path.GetFileName(file)}: {e.Message}");
                }
            }
            
            Debug.Log($"[QuickAPIBatchFixer] Batch fix complete! Fixed {warningsFixed} warnings in {filesFixed} files.");
            AssetDatabase.Refresh();
        }
    }
}
