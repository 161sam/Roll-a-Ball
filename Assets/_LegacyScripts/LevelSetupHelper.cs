using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
#endif
// TODO-REMOVE#1: Obsolete setup script – no longer needed and may corrupt current data
// TODO-DUPLICATE#1: Funktional identisch mit LevelProfileCreator.cs. Bitte vereinheitlichen oder entfernen.

/// <summary>
/// Helper-Klasse zum automatischen Setup des prozeduralen Levelgenerierungssystems
/// Wird beim ersten Unity-Start nach dem Hinzufügen der Scripts ausgeführt
/// </summary>
[InitializeOnLoad]
public class LevelSetupHelper
{
    static LevelSetupHelper()
    {
        #if UNITY_EDITOR
        EditorApplication.delayCall += SetupProcedualSystem;
        #endif
    }

    #if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/Setup Procedural Generation System")]
    public static void SetupProcedualSystem()
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

        // TODO-OPT#18: Many SetField calls below could be driven by a dictionary
        // to avoid repeating nearly identical reflection assignments
        
        SetField(profile, "profileName", $"{fileName} Profile", fields);
        SetField(profile, "displayName", displayName, fields);
        SetField(profile, "difficultyLevel", difficulty, fields);
        SetField(profile, "themeColor", themeColor, fields);
        SetField(profile, "levelSize", levelSize, fields);
        SetField(profile, "tileSize", 2f, fields);
        SetField(profile, "minWalkableArea", minWalkableArea, fields);
        SetField(profile, "collectibleCount", collectibleCount, fields);
        SetField(profile, "minCollectibleDistance", difficulty == 3 ? 1 : 2, fields);
        SetField(profile, "collectibleSpawnHeight", 0.5f, fields);
        SetField(profile, "obstacleDensity", obstacleDensity, fields);
        SetField(profile, "enableMovingObstacles", difficulty >= 3, fields);
        SetField(profile, "movingObstacleChance", difficulty >= 3 ? 0.1f : 0f, fields);
        SetField(profile, "frictionVariance", 0.1f + (difficulty - 1) * 0.1f, fields);
        SetField(profile, "enableSlipperyTiles", enableSlipperyTiles, fields);
        SetField(profile, "slipperyTileChance", slipperyChance, fields);
        SetField(profile, "enableParticleEffects", true, fields);
        SetField(profile, "playerSpawnOffset", Vector3.up, fields);
        SetField(profile, "randomizeSpawnPosition", difficulty > 1, fields);
        SetField(profile, "spawnSafeRadius", difficulty == 3 ? 2f : 3f, fields);
        SetField(profile, "useTimeBasedSeed", true, fields);
        SetField(profile, "generationMode", generationMode, fields);
        SetField(profile, "pathComplexity", pathComplexity, fields);

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
    #endif
}
