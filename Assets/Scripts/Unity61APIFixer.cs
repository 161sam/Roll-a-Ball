using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Unity 6.1 API Compatibility Fixer
/// Automatically updates deprecated FindObject APIs to modern Unity 6.1 syntax
/// </summary>
public class Unity61APIFixer : EditorWindow
{
    private Vector2 scrollPosition;
    private string[] filesToFix;
    private int fixedCount = 0;
    private bool scanComplete = false;

    [MenuItem("Roll-a-Ball/🔧 Fix Unity 6.1 APIs")]
    public static void ShowWindow()
    {
        GetWindow<Unity61APIFixer>("Unity 6.1 API Fixer");
    }

    void OnGUI()
    {
        GUILayout.Label("Unity 6.1 API Compatibility Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Dieser Tool aktualisiert alle veralteten FindObject-APIs in Ihrem Projekt:\n" +
            "• FindObjectsOfType → Object.FindObjectsByType\n" +
            "• FindObjectOfType → Object.FindFirstObjectByType\n" +
            "• GameObject.FindObjectsOfType → Object.FindObjectsByType", 
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("🔍 SCAN PROJECT", GUILayout.Height(40)))
        {
            ScanProject();
        }

        if (GUILayout.Button("🚀 FIX ALL APIS", GUILayout.Height(40)))
        {
            FixAllAPIs();
        }

        GUILayout.Space(10);

        if (scanComplete && filesToFix != null)
        {
            GUILayout.Label($"Gefundene Dateien mit veralteten APIs: {filesToFix.Length}", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (string file in filesToFix)
            {
                GUILayout.Label(file);
            }
            EditorGUILayout.EndScrollView();
        }

        if (fixedCount > 0)
        {
            EditorGUILayout.HelpBox($"✅ {fixedCount} Dateien erfolgreich aktualisiert!", MessageType.Info);
        }
    }

    private void ScanProject()
    {
        Debug.Log("[Unity61APIFixer] Scanning project for deprecated APIs...");
        
        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        var filesToFix_List = new System.Collections.Generic.List<string>();

        foreach (string file in scriptFiles)
        {
            if (file.Contains("/Editor/") || file.Contains("\\Editor\\"))
                continue; // Skip some editor files that might be intentionally using old APIs
                
            string content = File.ReadAllText(file);
            
            // Check for deprecated patterns
            if (content.Contains("FindObjectsOfType<") || 
                content.Contains("FindObjectOfType<") ||
                content.Contains("GameObject.FindObjectsOfType"))
            {
                string relativePath = file.Replace(Application.dataPath, "Assets");
                filesToFix_List.Add(relativePath);
            }
        }

        filesToFix = filesToFix_List.ToArray();
        scanComplete = true;
        
        Debug.Log($"[Unity61APIFixer] Found {filesToFix.Length} files with deprecated APIs");
        Repaint();
    }

    private void FixAllAPIs()
    {
        if (filesToFix == null || filesToFix.Length == 0)
        {
            ScanProject();
        }

        fixedCount = 0;
        Debug.Log("[Unity61APIFixer] Starting API fixes...");

        foreach (string file in filesToFix)
        {
            string fullPath = file.Replace("Assets", Application.dataPath);
            FixFileAPIs(fullPath);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Unity61APIFixer] Completed! Fixed {fixedCount} files.");
        Repaint();
    }

    private void FixFileAPIs(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            string originalContent = content;

            // Pattern 1: FindObjectsOfType<T>() → Object.FindObjectsByType<T>(FindObjectsSortMode.None)
            content = Regex.Replace(content, 
                @"FindObjectsOfType<([^>]+)>\(\)",
                @"Object.FindObjectsByType<$1>(FindObjectsSortMode.None)");

            // Pattern 2: FindObjectOfType<T>() → Object.FindFirstObjectByType<T>()
            content = Regex.Replace(content,
                @"FindObjectOfType<([^>]+)>\(\)",
                @"Object.FindFirstObjectByType<$1>()");

            // Pattern 3: GameObject.FindObjectsOfType<GameObject>() → Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            content = Regex.Replace(content,
                @"GameObject\.FindObjectsOfType<([^>]+)>\(\)",
                @"Object.FindObjectsByType<$1>(FindObjectsSortMode.None)");

            // Additional specific patterns that might occur
            
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
                Debug.Log($"[Unity61APIFixer] Fixed: {fileName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Unity61APIFixer] Error fixing {filePath}: {e.Message}");
        }
    }
}
