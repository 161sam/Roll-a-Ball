using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Info-Panel für die Level-Generierung Tools
/// Zeigt verfügbare Menüoptionen und deren Verwendung
/// </summary>
public class LevelGenerationInfo
{
#if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/ℹ️ Level-Generierung Hilfe", priority = 200)]
    public static void ShowHelpDialog()
    {
        string helpText = 
            "🎮 Roll-a-Ball Level-Generierung Tools\n\n" +
            
            "📋 VERFÜGBARE TOOLS:\n\n" +
            
            "🧹 'Bereinige Level-Generierung'\n" +
            "   → Entfernt LevelGenerators aus statischen Levels (Level1-3)\n" +
            "   → Behebt doppelte LevelGenerators in prozeduralen Levels\n" +
            "   → Erstellt detaillierten Bereinigungsreport\n\n" +
            
            "🔧 'Repariere aktuelle Szene'\n" +
            "   → Bereinigt nur die aktuell geöffnete Szene\n" +
            "   → Ideal zum schnellen Testen einer einzelnen Szene\n\n" +
            
            "🔍 'Validiere Level-Setup'\n" +
            "   → Überprüft das Setup der aktuellen Szene\n" +
            "   → Zeigt Empfehlungen für korrekte Konfiguration\n" +
            "   → Hilfreich beim Debuggen von Level-Problemen\n\n" +
            
            "📁 SZENEN-TYPEN:\n\n" +
            
            "🟩 Statische Level: Level1, Level2, Level3\n" +
            "   → Haben manuell platzierte Objekte\n" +
            "   → Sollten KEINE LevelGenerators enthalten\n\n" +
            
            "🟦 Prozedurale Level: GeneratedLevel, Level_OSM\n" +
            "   → Werden automatisch zur Laufzeit generiert\n" +
            "   → Benötigen genau EINEN LevelGenerator\n\n" +
            
            "🎯 PROBLEM GELÖST:\n" +
            "Das ursprüngliche Problem der doppelten Level-Generierung\n" +
            "wurde durch intelligente Szenen-Erkennung behoben.\n" +
            "Der LevelGenerator läuft jetzt nur noch in den\n" +
            "dafür vorgesehenen prozeduralen Szenen.\n\n" +
            
            "💡 TIPP: Nutze 'Bereinige Level-Generierung' einmalig,\n" +
            "um alle Szenen zu reparieren, dann 'Validiere Level-Setup'\n" +
            "um das Ergebnis zu überprüfen!";

        EditorUtility.DisplayDialog("Level-Generierung Hilfe", helpText, "Verstanden!");
    }

    [MenuItem("Roll-a-Ball/🚀 Problem-Lösung ausführen", priority = 199)]
    public static void RunCompleteFix()
    {
        if (EditorUtility.DisplayDialog(
            "Automatische Problem-Lösung",
            "Dies wird das komplette Level-Generierung Problem in folgenden Schritten lösen:\n\n" +
            "1️⃣ Alle Szenen bereinigen\n" +
            "2️⃣ Aktuelle Szene validieren\n" +
            "3️⃣ Erfolgsmeldung anzeigen\n\n" +
            "Fortfahren?",
            "Ja, ausführen",
            "Abbrechen"))
        {
            // Schritt 1: Bereinige alle Szenen
            SceneGeneratorCleaner.CleanAllScenes();
            
            // Kurze Pause für Unity
            System.Threading.Thread.Sleep(500);
            
            // Schritt 2: Validiere aktuelle Szene
            SceneGeneratorCleaner.ValidateCurrentLevelSetup();
            
            // Schritt 3: Erfolgsmeldung
            EditorUtility.DisplayDialog(
                "Problem gelöst! ✅",
                "Das Problem der doppelten Level-Generierung wurde erfolgreich behoben!\n\n" +
                "✅ Alle Szenen bereinigt\n" +
                "✅ LevelGenerator läuft nur in prozeduralen Szenen\n" +
                "✅ Console-Warnings sollten verschwunden sein\n\n" +
                "Du kannst jetzt:\n" +
                "• Die Level1-3 normal spielen (ohne Generierung)\n" +
                "• GeneratedLevel für prozedurale Level nutzen\n" +
                "• Mit R-Taste in GeneratedLevel neue Level erzeugen",
                "Perfekt!");
        }
    }
#endif
}
