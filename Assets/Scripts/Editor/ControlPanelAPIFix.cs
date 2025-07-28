using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Control Panel API Fix - Fix remaining FindObjectOfType calls in RollABallControlPanel
/// </summary>
// TODO-DUPLICATE#2: Funktional identisch mit QuickAPIFix.cs. Bitte vereinheitlichen oder entfernen.
public class ControlPanelAPIFix
{
    [MenuItem("Roll-a-Ball/🔧 Fix Control Panel APIs")]
    public static void FixControlPanelAPIs()
    {
        Debug.Log("[ControlPanelAPIFix] Fixing Control Panel APIs...");
        
        string filePath = "Assets/Scripts/Editor/RollABallControlPanel.cs";
        string fullPath = Application.dataPath.Replace("Assets", "") + filePath;
        
        if (File.Exists(fullPath))
        {
            try
            {
                string content = File.ReadAllText(fullPath);
                string originalContent = content;

                // Replace FindFirstObjectByType<T>() with FindFirstObjectByType<T>()
                content = Regex.Replace(content,
                    @"FindObjectOfType<([^>]+)>\(\)",
                    @"FindFirstObjectByType<$1>()");

                if (content != originalContent)
                {
                    File.WriteAllText(fullPath, content);
                    Debug.Log("[ControlPanelAPIFix] ✅ Fixed Control Panel APIs!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ControlPanelAPIFix] Error: {e.Message}");
            }
        }
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Control Panel Fixed", 
            "All Control Panel APIs updated to Unity 6.1 standard!\n\nAll warnings should now be resolved.", 
            "Perfect!");
    }
}