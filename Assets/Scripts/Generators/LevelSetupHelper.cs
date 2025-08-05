#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Helper-Klasse zum automatischen Setup des prozeduralen Levelgenerierungssystems.
/// Wird beim ersten Unity-Start nach dem Hinzufügen der Scripts ausgeführt.
/// </summary>
[InitializeOnLoad]
public class LevelSetupHelper
{
    static LevelSetupHelper()
    {
        EditorApplication.delayCall += SetupProceduralSystem;
    }

    /// <summary>
    /// Richtet das prozedurale Levelgenerierungssystem ein, indem benötigte Assets und Szenen erstellt werden.
    /// </summary>
    [MenuItem("Roll-a-Ball/Setup Procedural Generation System")]
    public static void SetupProceduralSystem()
    {
        bool setupRequired = false;

        // 1. Überprüfe, ob ScriptableObject-Assets existieren
        string[] requiredProfiles = { "EasyProfile", "MediumProfile", "HardProfile" };
        
        foreach (string profileName in requiredProfiles)
        {
            string assetPath = $"Assets/ScriptableObjects/{profileName}.asset";
            if (!File.Exists(assetPath))
            {
                setupRequired = true;
                break;
            }
        }

        // 2. Überprüfe, ob GeneratedLevel-Szene existiert
        if (!File.Exists("Assets/Scenes/GeneratedLevel.unity"))
        {
            setupRequired = true;
        }

        if (setupRequired)
        {
            if (EditorUtility.DisplayDialog(
                "Procedural Level System Setup",
                "Das prozedurale Levelgenerierungssystem muss eingerichtet werden.\n\n" +
                "Dies wird folgende Aktionen durchführen:\n" +
                "• LevelProfile-Assets erstellen (Easy, Medium, Hard)\n" +
                "• GeneratedLevel-Szene erstellen\n" +
                "• System testen\n\n" +
                "Fortfahren?",
                "Ja, einrichten",
                "Später"))
            {
                CreateLevelProfileAssets();
                CreateGeneratedLevelScene();
                
                EditorUtility.DisplayDialog(
                    "Setup abgeschlossen",
                    "Das prozedurale Levelgenerierungssystem wurde erfolgreich eingerichtet!\n\n" +
                    "Sie können jetzt:\n" +
                    "• Die GeneratedLevel-Szene öffnen und testen\n" +
                    "• Die LevelProfile-Assets im ScriptableObjects-Ordner anpassen\n" +
                    "• Das System mit der R-Taste im Play-Modus regenerieren",
                    "OK");
            }
        }
    }

    private static void CreateLevelProfileAssets()
    {
        // Stelle sicher, dass das ScriptableObjects-Verzeichnis existiert
        string scriptableObjectsPath = "Assets/ScriptableObjects";
        if (!AssetDatabase.IsValidFolder(scriptableObjectsPath))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }

        // Erstelle Easy Profile
        CreateLevelProfile(
            "EasyProfile", 
            "Einfach", 
            1, 
            Color.green, 
            8, 5, 0.1f, 
            LevelGenerationMode.Simple, 
            0.3f, 
            false, 0f, 80
        );

        // Erstelle Medium Profile
        CreateLevelProfile(
            "MediumProfile", 
            "Mittel", 
            2, 
            Color.yellow, 
            12, 8, 0.25f, 
            LevelGenerationMode.Maze, 
            0.5f, 
            true, 0.1f, 70
        );

        // Erstelle Hard Profile  
        CreateLevelProfile(
            "HardProfile", 
            "Schwer", 
            3, 
            Color.red, 
            16, 12, 0.4f, 
            LevelGenerationMode.Maze, 
            0.8f, 
            true, 0.2f, 60
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LevelProfile-Assets wurden erfolgreich erstellt!");
    }

    private static void CreateLevelProfile(
        string fileName, 
        string displayName, 
        int difficulty, 
        Color themeColor, 
        int levelSize, 
        int collectibleCount, 
        float obstacleDensity,
        LevelGenerationMode generationMode,
        float pathComplexity,
        bool enableSlipperyTiles,
        float slipperyChance,
        int minWalkableArea)
    {
        LevelProfile profile = ScriptableObject.CreateInstance<LevelProfile>();
        
        // Verwende Reflection für private Felder
        var fields = typeof(LevelProfile).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var fieldValues = new System.Collections.Generic.Dictionary<string, object>
        {
            { "profileName", $"{fileName} Profile" },
            { "displayName", displayName },
            { "difficultyLevel", difficulty },
            { "themeColor", themeColor },
            { "levelSize", levelSize },
            { "tileSize", 2f },
            { "minWalkableArea", minWalkableArea },
            { "collectibleCount", collectibleCount },
            { "minCollectibleDistance", difficulty == 3 ? 1 : 2 },
            { "collectibleSpawnHeight", 0.5f },
            { "obstacleDensity", obstacleDensity },
            { "enableMovingObstacles", difficulty >= 3 },
            { "movingObstacleChance", difficulty >= 3 ? 0.1f : 0f },
            { "frictionVariance", 0.1f + (difficulty - 1) * 0.1f },
            { "enableSlipperyTiles", enableSlipperyTiles },
            { "slipperyTileChance", slipperyChance },
            { "enableParticleEffects", true },
            { "playerSpawnOffset", Vector3.up },
            { "randomizeSpawnPosition", difficulty > 1 },
            { "spawnSafeRadius", difficulty == 3 ? 2f : 3f },
            { "useTimeBasedSeed", true },
            { "generationMode", generationMode },
            { "pathComplexity", pathComplexity }
        };

        foreach (var kvp in fieldValues)
        {
            SetField(profile, kvp.Key, kvp.Value, fields);
        }

        string assetPath = $"Assets/ScriptableObjects/{fileName}.asset";
        AssetDatabase.CreateAsset(profile, assetPath);
    }

    private static void SetField(object obj, string fieldName, object value, System.Reflection.FieldInfo[] fields)
    {
        var field = System.Array.Find(fields, f => f.Name == fieldName);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"Field '{fieldName}' not found in {obj.GetType().Name}");
        }
    }

    private static void CreateGeneratedLevelScene()
    {
        // Erstelle neue Szene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Setup camera positioning
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(16f, 20f, -20f);
            mainCamera.transform.rotation = Quaternion.Euler(29f, 0f, 0f);
        }

        // Füge GameManager hinzu (falls noch nicht vorhanden)
        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();

        // Füge LevelManager hinzu
        GameObject levelManager = new GameObject("LevelManager");
        levelManager.AddComponent<LevelManager>();

        // Füge UIController hinzu (mit Canvas)
        GameObject canvas = new GameObject("Canvas");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject uiController = new GameObject("UIController");
        uiController.AddComponent<UIController>();

        // Füge LevelGenerator hinzu
        GameObject levelGenerator = new GameObject("LevelGenerator");
        LevelGenerator generatorComponent = levelGenerator.AddComponent<LevelGenerator>();

        // Füge EventSystem hinzu (für UI)
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Speichere die Szene
        string scenePath = "Assets/Scenes/GeneratedLevel.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        Debug.Log($"GeneratedLevel-Szene wurde erstellt: {scenePath}");
        
        // Öffne die Szene
        EditorSceneManager.OpenScene(scenePath);
    }
}
#endif
