using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace RollABall.Editor
{
    public class ObsoleteAPIBatchFixer
    {
        [MenuItem("Roll-a-Ball/⚡ Batch Fix Obsolete APIs", priority = 5)]
        public static void BatchFixObsoleteAPIs()
        {
            Debug.Log("⚡ Starting batch fix of obsolete APIs...");
            
            string scriptsPath = Path.Combine(Application.dataPath, "Scripts");
            string[] allScripts = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
            
            int totalFiles = 0;
            int fixedFiles = 0;
            
            foreach (string scriptPath in allScripts)
            {
                totalFiles++;
                string content = File.ReadAllText(scriptPath);
                string originalContent = content;
                
                // Fix FindObjectOfType -> Object.FindFirstObjectByType
                content = Regex.Replace(content, @"\bFindObjectOfType<", "Object.FindFirstObjectByType<");
                content = Regex.Replace(content, @"Object\.FindObjectOfType<", "Object.FindFirstObjectByType<");
                
                // Fix FindObjectsOfType -> Object.FindObjectsByType  
                content = Regex.Replace(content, @"\bFindObjectsOfType<", "Object.FindObjectsByType<");
                content = Regex.Replace(content, @"Object\.FindObjectsOfType<", "Object.FindObjectsByType<");
                
                // Add sorting parameter for FindObjectsByType calls that don't have it
                content = Regex.Replace(content, @"Object\.FindObjectsByType<([^>]+)>\(\)", 
                    "Object.FindObjectsByType<$1>(FindObjectsSortMode.None)");
                
                if (content != originalContent)
                {
                    File.WriteAllText(scriptPath, content);
                    fixedFiles++;
                    Debug.Log($"✅ Fixed APIs in: {Path.GetFileName(scriptPath)}");
                }
            }
            
            Debug.Log($"🎯 Batch fix complete! Fixed {fixedFiles}/{totalFiles} files");
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Batch Fix Complete", 
                $"Fixed obsolete APIs in {fixedFiles} out of {totalFiles} files.\n\nUnity will now refresh assets.", "OK");
        }
    }
}
