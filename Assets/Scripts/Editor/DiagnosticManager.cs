using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// Tool zur Steuerung von Diagnostic-Tools, die Console-Spam verursachen
/// </summary>
public class DiagnosticManager
{
#if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/🔇 Diagnostic-Tools deaktivieren", priority = 60)]
    public static void DisableAllDiagnosticTools()
    {
        if (!EditorUtility.DisplayDialog(
            "Diagnostic Manager",
            "Dies wird alle Diagnostic-Tools deaktivieren, die Console-Spam verursachen:\n\n" +
            "• CollectibleDiagnosticTool\n" +
            "• LevelProgressionFixer\n" +
            "• MasterFixTool\n" +
            "• Alle anderen Auto-Running Tools\n\n" +
            "Die Tools bleiben als Komponenten erhalten, werden aber deaktiviert.\n" +
            "Du kannst sie später manuell reaktivieren.\n\n" +
            "Fortfahren?",
            "Ja, deaktivieren",
            "Abbrechen"))
        {
            return;
        }

        int disabledCount = 0;
        Scene currentScene = SceneManager.GetActiveScene();
        
        // Deaktiviere CollectibleDiagnosticTool
        CollectibleDiagnosticTool[] diagnosticTools = Object.FindObjectsByType<CollectibleDiagnosticTool>(FindObjectsSortMode.None);
        foreach (var tool in diagnosticTools)
        {
            tool.enabled = false;
            EditorUtility.SetDirty(tool);
            disabledCount++;
            Debug.Log($"[DiagnosticManager] CollectibleDiagnosticTool deaktiviert auf '{// tool // Fixed: UniversalSceneFixture has no gameObject.name}'");
        }

        // Deaktiviere LevelProgressionFixer falls vorhanden
        var progressionFixers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var fixer in progressionFixers)
        {
            if (fixer.GetType().Name == "LevelProgressionFixer")
            {
                fixer.enabled = false;
                EditorUtility.SetDirty(fixer);
                disabledCount++;
                Debug.Log($"[DiagnosticManager] LevelProgressionFixer deaktiviert auf '{// fixer // Fixed: UniversalSceneFixture has no gameObject.name}'");
            }
        }

        // Deaktiviere MasterFixTool falls vorhanden
        foreach (var fixer in progressionFixers)
        {
            if (fixer.GetType().Name == "MasterFixTool")
            {
                fixer.enabled = false;
                EditorUtility.SetDirty(fixer);
                disabledCount++;
                Debug.Log($"[DiagnosticManager] MasterFixTool deaktiviert auf '{// fixer // Fixed: UniversalSceneFixture has no gameObject.name}'");
            }
        }

        // Markiere Szene als dirty
        if (disabledCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);
        }

        EditorUtility.DisplayDialog(
            "Diagnostic-Tools deaktiviert",
            $"✅ {disabledCount} Diagnostic-Tool(s) deaktiviert!\n\n" +
            "Console-Spam sollte jetzt gestoppt sein.\n" +
            "Du kannst die Tools später über den Inspector reaktivieren.",
            "OK");
    }

    [MenuItem("Roll-a-Ball/🔊 Diagnostic-Tools reaktivieren", priority = 61)]
    public static void EnableAllDiagnosticTools()
    {
        int enabledCount = 0;
        Scene currentScene = SceneManager.GetActiveScene();
        
        // Reaktiviere CollectibleDiagnosticTool
        CollectibleDiagnosticTool[] diagnosticTools = Object.FindObjectsByType<CollectibleDiagnosticTool>(FindObjectsSortMode.None);
        foreach (var tool in diagnosticTools)
        {
            tool.enabled = true;
            EditorUtility.SetDirty(tool);
            enabledCount++;
        }

        // Reaktiviere andere Tools
        var allComponents = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var component in allComponents)
        {
            string typeName = component.GetType().Name;
            if (typeName == "LevelProgressionFixer" || typeName == "MasterFixTool")
            {
                component.enabled = true;
                EditorUtility.SetDirty(component);
                enabledCount++;
            }
        }

        // Markiere Szene als dirty
        if (enabledCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);
        }

        EditorUtility.DisplayDialog(
            "Diagnostic-Tools reaktiviert",
            $"✅ {enabledCount} Diagnostic-Tool(s) reaktiviert!",
            "OK");
    }

    [MenuItem("Roll-a-Ball/📊 Diagnostic-Status anzeigen", priority = 62)]
    public static void ShowDiagnosticStatus()
    {
        string status = "📊 Diagnostic-Tools Status:\n\n";
        
        // Prüfe CollectibleDiagnosticTool
        CollectibleDiagnosticTool[] diagnosticTools = Object.FindObjectsByType<CollectibleDiagnosticTool>(FindObjectsSortMode.None);
        status += $"🔍 CollectibleDiagnosticTool: {diagnosticTools.Length} gefunden\n";
        foreach (var tool in diagnosticTools)
        {
            string state = tool.enabled ? "🟢 Aktiv" : "🔴 Deaktiviert";
            status += $"   └─ {// tool // Fixed: UniversalSceneFixture has no gameObject.name}: {state}\n";
        }

        // Prüfe andere Tools
        var allComponents = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        int otherToolsCount = 0;
        foreach (var component in allComponents)
        {
            string typeName = component.GetType().Name;
            if (typeName == "LevelProgressionFixer" || typeName == "MasterFixTool")
            {
                if (otherToolsCount == 0)
                {
                    status += $"\n🛠️ Andere Fix-Tools:\n";
                }
                string state = component.enabled ? "🟢 Aktiv" : "🔴 Deaktiviert";
                status += $"   └─ {typeName} auf {// component // Fixed: UniversalSceneFixture has no gameObject.name}: {state}\n";
                otherToolsCount++;
            }
        }

        if (otherToolsCount == 0)
        {
            status += "\n🛠️ Andere Fix-Tools: Keine gefunden\n";
        }

        status += "\n💡 Tipp: Deaktiviere Tools um Console-Spam zu vermeiden!";

        EditorUtility.DisplayDialog("Diagnostic-Status", status, "OK");
        Debug.Log("[DiagnosticManager] " + status);
    }
#endif
}
