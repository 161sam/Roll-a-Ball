using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Erkennt den Typ der aktuellen Szene und verhindert ungewollte Level-Generierung
/// </summary>
public static class SceneTypeDetector
{
    // Default lists used if no configuration asset is found.
    // Scene lists can be overridden at runtime via scene_config.json
    private static readonly string[] defaultProceduralScenes =
    {
        "GeneratedLevel",
        "Level_OSM",
        "MiniGame"
    };

    private static readonly string[] defaultStaticScenes =
    {
        "Level1",
        "Level2",
        "Level3"
    };

    private static SceneTypeConfig config;

    [System.Serializable]
    private class SceneTypeConfigData
    {
        public string[] proceduralScenes;
        public string[] staticScenes;
    }

    /// <summary>
    /// Szenen, in denen prozedurale Generierung erlaubt ist
    /// </summary>
    public static string[] ProceduralScenes
    {
        get
        {
            EnsureConfigLoaded();
            return config ? config.proceduralScenes : defaultProceduralScenes;
        }
    }

    /// <summary>
    /// Statische Szenen, die bereits manuell aufgebaut sind
    /// </summary>
    public static string[] StaticScenes
    {
        get
        {
            EnsureConfigLoaded();
            return config ? config.staticScenes : defaultStaticScenes;
        }
    }

    private static void EnsureConfigLoaded()
    {
        if (config == null)
        {
            config = Resources.Load<SceneTypeConfig>("DefaultSceneTypeConfig");
            LoadOverrideConfig();
        }
    }

    private static void LoadOverrideConfig()
    {
        string path = Path.Combine(Application.persistentDataPath, "scene_config.json");
        if (!File.Exists(path))
            return;

        try
        {
            string json = File.ReadAllText(path);
            SceneTypeConfigData data = JsonUtility.FromJson<SceneTypeConfigData>(json);
            if (data != null)
            {
                if (config == null)
                    config = ScriptableObject.CreateInstance<SceneTypeConfig>();
                if (data.proceduralScenes != null && data.proceduralScenes.Length > 0)
                    config.proceduralScenes = data.proceduralScenes;
                if (data.staticScenes != null && data.staticScenes.Length > 0)
                    config.staticScenes = data.staticScenes;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SceneTypeDetector] Failed to load override config: {e.Message}");
        }
    }

    /// <summary>
    /// Prüft, ob die aktuelle Szene prozedurale Generierung unterstützt
    /// </summary>
    public static bool IsProceduralScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        return IsProceduralScene(currentSceneName);
    }

    /// <summary>
    /// Prüft, ob eine bestimmte Szene prozedurale Generierung unterstützt
    /// </summary>
    public static bool IsProceduralScene(string sceneName)
    {
        foreach (string proceduralScene in ProceduralScenes)
        {
            if (sceneName.Equals(proceduralScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Prüft, ob die aktuelle Szene ein statisches Level ist
    /// </summary>
    public static bool IsStaticScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        return IsStaticScene(currentSceneName);
    }

    /// <summary>
    /// Prüft, ob eine bestimmte Szene ein statisches Level ist
    /// </summary>
    public static bool IsStaticScene(string sceneName)
    {
        foreach (string staticScene in StaticScenes)
        {
            if (sceneName.Equals(staticScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gibt den Typ der aktuellen Szene zurück
    /// </summary>
    public static SceneType GetCurrentSceneType()
    {
        if (IsProceduralScene())
            return SceneType.Procedural;
        else if (IsStaticScene()) 
            return SceneType.Static;
        else
            return SceneType.Unknown;
    }

    /// <summary>
    /// Loggt Informationen über die aktuelle Szene (für Debugging)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogSceneInfo()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneType sceneType = GetCurrentSceneType();
        
        Debug.Log($"[SceneTypeDetector] Aktuelle Szene: '{currentSceneName}' | Typ: {sceneType}");
        
        if (sceneType == SceneType.Procedural)
        {
            Debug.Log($"[SceneTypeDetector] ✅ Prozedurale Generierung erlaubt");
        }
        else if (sceneType == SceneType.Static)
        {
            Debug.Log($"[SceneTypeDetector] ❌ Statisches Level - keine Generierung erforderlich");
        }
        else
        {
            Debug.LogWarning($"[SceneTypeDetector] ⚠️ Unbekannter Szenentyp");
        }
    }
}

/// <summary>
/// Verschiedene Szenen-Typen im Spiel
/// </summary>
public enum SceneType
{
    Static,      // Manuell aufgebaute Level (Level1-3)
    Procedural,  // Generierte Level (GeneratedLevel, Level_OSM)
    Unknown      // Unbekannte oder Test-Szenen
}
