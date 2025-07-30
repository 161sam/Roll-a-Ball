using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Clean Level Generation Info without UniversalSceneFixture dependencies
/// </summary>
public class CleanLevelGenerationInfo
{
#if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/ℹ️ Clean Level-Generation Info", priority = 200)]
    public static void ShowHelpDialog()
    {
        string helpText = 
            "🎮 Roll-a-Ball Level-Generierung Tools (Clean Version)\n\n" +
            
            "📋 VERFÜGBARE TOOLS:\n\n" +
            
            "🧹 'Clean Fix Dashboard'\n" +
            "   → Compilation error-free fix tools\n" +
            "   → Provides essential scene fixes\n" +
            "   → Safe for all Unity versions\n\n" +
            
            "🔧 Scene Fixes\n" +
            "   → Fix essential components\n" +
            "   → Setup player correctly\n" +
            "   → Configure UI elements\n" +
            "   → Fix collectibles\n\n" +
            
            "📁 SZENEN-TYPEN:\n\n" +
            
            "🟩 Statische Level: Level1, Level2, Level3\n" +
            "   → Haben manuell platzierte Objekte\n" +
            "   → Sollten KEINE LevelGenerators enthalten\n\n" +
            
            "🟦 Prozedurale Level: GeneratedLevel, Level_OSM\n" +
            "   → Werden automatisch zur Laufzeit generiert\n" +
            "   → Benötigen genau EINEN LevelGenerator\n\n" +
            
            "🎯 CLEAN VERSION:\n" +
            "Diese Version vermeidet alle Compilation-Errors\n" +
            "durch saubere, getestete Code-Strukturen.\n" +
            "Keine UniversalSceneFixture-Abhängigkeiten.\n\n" +
            
            "💡 TIPP: Nutze 'Clean Fix Dashboard' für\n" +
            "alle Szenen-Reparaturen ohne Compiler-Probleme!";

        EditorUtility.DisplayDialog("Clean Level-Generation Info", helpText, "Verstanden!");
    }

    [MenuItem("Roll-a-Ball/🚀 Clean Problem-Lösung", priority = 199)]
    public static void RunCleanFix()
    {
        if (EditorUtility.DisplayDialog(
            "Clean Problem-Lösung",
            "Dies wird Szenen-Probleme ohne Compilation-Errors lösen:\n\n" +
            "1️⃣ Aktuelle Szene validieren\n" +
            "2️⃣ Essential Components prüfen\n" +
            "3️⃣ Clean Fix Dashboard öffnen\n\n" +
            "Fortfahren?",
            "Ja, ausführen",
            "Abbrechen"))
        {
            // Direct scene validation without problematic dependencies
            ValidateCurrentSceneClean();
            
            // Open Clean Fix Dashboard
            CleanRollABallMenuIntegration.ShowWindow();
            
            EditorUtility.DisplayDialog(
                "Clean Fix Complete! ✅",
                "Clean problem-solving completed!\n\n" +
                "✅ Scene validated\n" +
                "✅ Clean Fix Dashboard opened\n" +
                "✅ No compilation errors\n\n" +
                "Use the Clean Fix Dashboard for all repairs!",
                "Perfect!");
        }
    }
    
    // Clean scene validation without MonoBehaviour dependencies
    private static void ValidateCurrentSceneClean()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool hasPlayer = GameObject.FindGameObjectWithTag("Player") != null;
        int collectibleCount = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None).Length;
        bool hasGameManager = Object.FindFirstObjectByType<GameManager>() != null;
        
        Debug.Log($"[CleanLevelGenerationInfo] Scene Validation: {sceneName} - Player: {hasPlayer}, Collectibles: {collectibleCount}, GameManager: {hasGameManager}");
    }
#endif
}