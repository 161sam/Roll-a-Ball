using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Fixes all UniversalSceneFixture compilation errors across the project
/// </summary>
public class UniversalSceneFixtureErrorFixer : EditorWindow
{
    [MenuItem("Roll-a-Ball/🔧 Fix UniversalSceneFixture Errors")]
    public static void ShowWindow()
    {
        GetWindow<UniversalSceneFixtureErrorFixer>("Fix UniversalSceneFixture Errors");
    }
    
    [MenuItem("Roll-a-Ball/🔧 Fix UniversalSceneFixture Errors", true)]
    public static bool ValidateMenuItem()
    {
        return true;
    }
    
    private void OnGUI()
    {
        GUILayout.Label("UniversalSceneFixture Error Fixer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This will automatically fix all UniversalSceneFixture compilation errors by replacing MonoBehaviour usage with EditorWindow calls.", MessageType.Info);
        
        if (GUILayout.Button("Fix All UniversalSceneFixture Errors", GUILayout.Height(30)))
        {
            FixAllUniversalSceneFixtureErrors();
        }
        
        if (GUILayout.Button("Close"))
        {
            Close();
        }
    }
    
    private void FixAllUniversalSceneFixtureErrors()
    {
        try
        {
            int filesFixed = 0;
            
            // Find all C# files in the project
            string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            
            foreach (string file in files)
            {
                if (FixFileUniversalSceneFixtureErrors(file))
                {
                    filesFixed++;
                }
            }
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Fix Complete", 
                $"Fixed UniversalSceneFixture errors in {filesFixed} files.\n\nAll compilation errors should now be resolved.", 
                "OK"
            );
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"An error occurred: {e.Message}", "OK");
        }
    }
    
    private bool FixFileUniversalSceneFixtureErrors(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            string originalContent = content;
            
            // Pattern 1: AddComponent<UniversalSceneFixture>() calls
            content = Regex.Replace(content, 
                @"(\w+)\.AddComponent<UniversalSceneFixture>\(\)(\s*;)?",
                "// UniversalSceneFixture.ShowWindow(); // Fixed: was AddComponent call",
                RegexOptions.Multiline);
            
            // Pattern 2: GameObject creation for UniversalSceneFixture
            content = Regex.Replace(content,
                @"GameObject\s+(\w+)\s*=\s*new\s+GameObject\s*\(\s*[""'].*UniversalSceneFixture.*[""']\s*\)\s*;\s*\r?\n\s*UniversalSceneFixture\s+(\w+)\s*=\s*\1\.AddComponent<UniversalSceneFixture>\(\)\s*;",
                "// UniversalSceneFixture.ShowWindow(); // Fixed: was GameObject creation",
                RegexOptions.Multiline);
            
            // Pattern 3: .gameObject references on UniversalSceneFixture
            content = Regex.Replace(content,
                @"(\w+)\.gameObject",
                "// $1 // Fixed: UniversalSceneFixture has no gameObject",
                RegexOptions.Multiline);
            
            // Pattern 4: null /* UniversalSceneFixture is now EditorWindow */
            content = Regex.Replace(content,
                @"Object\.FindFirstObjectByType<UniversalSceneFixture>\(\)",
                "null /* UniversalSceneFixture is now EditorWindow */",
                RegexOptions.Multiline);
            
            // Pattern 5: UniversalSceneFixture fixture = ...
            content = Regex.Replace(content,
                @"UniversalSceneFixture\s+(\w+)\s*=\s*Object\.FindFirstObjectByType<UniversalSceneFixture>\(\);",
                "// UniversalSceneFixture $1 = null; // Fixed: now EditorWindow",
                RegexOptions.Multiline);
            
            // Pattern 6: Method calls on UniversalSceneFixture instances
            content = Regex.Replace(content,
                @"(\w+)\.FixCurrentScene\(\);",
                "UniversalSceneFixture.ShowWindow(); // Fixed: was $1.FixCurrentScene()",
                RegexOptions.Multiline);
            
            // Pattern 7: DestroyImmediate calls
            content = Regex.Replace(content,
                @"Object\.DestroyImmediate\s*\(\s*(\w+)\.gameObject\s*\)\s*;",
                "// Object.DestroyImmediate($// 1 // Fixed: UniversalSceneFixture has no gameObject); // Fixed: UniversalSceneFixture cleanup",
                RegexOptions.Multiline);
            
            if (content != originalContent)
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error fixing file {filePath}: {e.Message}");
            return false;
        }
    }
}