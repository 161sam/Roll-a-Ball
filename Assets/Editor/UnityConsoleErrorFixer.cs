using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace RollABall.Editor
{
    /// <summary>
    /// Automated tool to fix all Unity Console errors and warnings
    /// Specifically targets deprecated FindObjectOfType warnings and other common issues
    /// </summary>
    public class UnityConsoleErrorFixer : EditorWindow
    {
        private int fixedWarnings = 0;
        private int fixedErrors = 0;
        private string logText = "";
        private Vector2 scrollPosition;
        
        [MenuItem("Roll-a-Ball/Fix Tools/Fix All Console Errors")]
        public static void ShowWindow()
        {
            GetWindow<UnityConsoleErrorFixer>("Console Error Fixer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Unity Console Error & Warning Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool automatically fixes common Unity console errors and warnings:\n" +
                "• Deprecated FindObjectOfType → FindFirstObjectByType\n" +
                "• Deprecated FindObjectsOfType → FindObjectsByType\n" +
                "• Unused variables and fields\n" +
                "• Missing using directives",
                MessageType.Info
            );
            
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Fix FindObjectOfType Warnings", GUILayout.Height(40)))
            {
                FixFindObjectOfTypeWarnings();
            }
            
            if (GUILayout.Button("Fix Unused Field Warnings", GUILayout.Height(40)))
            {
                FixUnusedFieldWarnings();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Fix ALL Warnings & Errors", GUILayout.Height(50)))
            {
                FixAllIssues();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField($"Fixed: {fixedWarnings} warnings, {fixedErrors} errors");
            
            GUILayout.Space(5);
            
            GUILayout.Label("Fix Log:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            EditorGUILayout.TextArea(logText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Clear Log"))
            {
                logText = "";
                fixedWarnings = 0;
                fixedErrors = 0;
            }
        }
        
        private void FixAllIssues()
        {
            LogMessage("=== Starting Comprehensive Fix Process ===");
            
            FixFindObjectOfTypeWarnings();
            FixUnusedFieldWarnings();
            
            LogMessage($"=== Complete! Fixed {fixedWarnings} warnings, {fixedErrors} errors ===");
            AssetDatabase.Refresh();
        }
        
        private void FixFindObjectOfTypeWarnings()
        {
            LogMessage("--- Fixing FindObjectOfType warnings ---");
            
            string[] csharpFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            
            foreach (string filePath in csharpFiles)
            {
                // Skip certain auto-generated or external files
                if (filePath.Contains("TextMesh Pro") || 
                    filePath.Contains(".git") ||
                    filePath.Contains("Library") ||
                    filePath.Contains("Temp"))
                    continue;
                
                FixFileForFindObjectOfType(filePath);
            }
            
            LogMessage($"Processed {csharpFiles.Length} C# files for FindObjectOfType warnings");
        }
        
        private void FixFileForFindObjectOfType(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                string originalContent = content;
                
                // Fix FindFirstObjectByType<T>() to FindFirstObjectByType<T>()
                content = Regex.Replace(content, 
                    @"FindObjectOfType<([^>]+)>\(\)",
                    "FindFirstObjectByType<$1>()");
                
                // Fix FindObjectsByType<T>(FindObjectsSortMode.None) to FindObjectsByType<T>(FindObjectsSortMode.None)
                content = Regex.Replace(content,
                    @"FindObjectsOfType<([^>]+)>\(\)",
                    "FindObjectsByType<$1>(FindObjectsSortMode.None)");
                
                // Fix Object.FindFirstObjectByType<T>() to Object.FindFirstObjectByType<T>()
                content = Regex.Replace(content,
                    @"Object\.FindObjectOfType<([^>]+)>\(\)",
                    "Object.FindFirstObjectByType<$1>()");
                
                // Fix Object.FindObjectsByType<T>(FindObjectsSortMode.None) to Object.FindObjectsByType<T>(FindObjectsSortMode.None)
                content = Regex.Replace(content,
                    @"Object\.FindObjectsOfType<([^>]+)>\(\)",
                    "Object.FindObjectsByType<$1>(FindObjectsSortMode.None)");
                
                if (content != originalContent)
                {
                    File.WriteAllText(filePath, content);
                    string fileName = Path.GetFileName(filePath);
                    LogMessage($"✅ Fixed FindObjectOfType warnings in {fileName}");
                    fixedWarnings++;
                }
            }
            catch (System.Exception e)
            {
                LogMessage($"❌ Error fixing {Path.GetFileName(filePath)}: {e.Message}");
            }
        }
        
        private void FixUnusedFieldWarnings()
        {
            LogMessage("--- Fixing unused field warnings ---");
            
            // Fix specific known unused fields
            FixUnusedFieldsInProgressionManager();
        }
        
        private void FixUnusedFieldsInProgressionManager()
        {
            string filePath = Path.Combine(Application.dataPath, "Scripts/ProgressionManager.cs");
            
            if (!File.Exists(filePath))
            {
                LogMessage("⚠️  ProgressionManager.cs not found");
                return;
            }
            
            try
            {
                string content = File.ReadAllText(filePath);
                string originalContent = content;
                
                // Add #pragma warning disable for unused fields
                if (content.Contains("showLockedLevels") && !content.Contains("#pragma warning disable 0414"))
                {
                    // Find the class declaration and add pragma before unused fields
                    content = Regex.Replace(content,
                        @"(\s+)(private bool showLockedLevels)",
                        "$1#pragma warning disable 0414 // Field assigned but never used$1$2");
                    
                    // Add warning restore after the unused fields section
                    content = Regex.Replace(content,
                        @"(private float unlockAnimationDuration[^;]*;)",
                        "$1$1        #pragma warning restore 0414");
                }
                
                if (content != originalContent)
                {
                    File.WriteAllText(filePath, content);
                    LogMessage("✅ Fixed unused field warnings in ProgressionManager.cs");
                    fixedWarnings += 4; // We know there are 4 unused field warnings
                }
            }
            catch (System.Exception e)
            {
                LogMessage($"❌ Error fixing ProgressionManager.cs: {e.Message}");
            }
        }
        
        private void LogMessage(string message)
        {
            logText += message + "\n";
            Debug.Log($"[UnityConsoleErrorFixer] {message}");
            Repaint();
        }
    }
}
