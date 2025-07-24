using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
#endif

/// <summary>
/// Repariert alle Szenen im Projekt, die keine Main Camera haben
/// Löst das "No Cameras rendering" Problem projektweise
/// </summary>
public class ProjectCameraFixer : MonoBehaviour
{
    #if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/Fix All Scenes - Add Missing Cameras")]
    public static void FixAllScenesInProject()
    {
        if (EditorUtility.DisplayDialog(
            "Fix All Scenes",
            "Dies wird alle Szenen im Projekt öffnen und fehlende Main Cameras hinzufügen.\n\n" +
            "Gefundene Szenen werden automatisch repariert:\n" +
            "• Level1.unity\n" +
            "• Level2.unity\n" +
            "• Level3.unity\n" +
            "• GeneratedLevel.unity\n" +
            "• Weitere Szenen im Assets/Scenes/ Ordner\n\n" +
            "Fortfahren?",
            "Ja, alle reparieren",
            "Abbrechen"))
        {
            FixAllScenes();
        }
    }

    [MenuItem("Roll-a-Ball/Fix Current Scene - Add Camera")]
    public static void FixCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.IsValid())
        {
            FixSceneCamera(currentScene.path);
            Debug.Log($"✅ Current scene '{currentScene.name}' camera fixed!");
        }
        else
        {
            Debug.LogError("No valid scene loaded!");
        }
    }

    private static void FixAllScenes()
    {
        // Speichere aktuell geöffnete Szene
        Scene originalScene = SceneManager.GetActiveScene();
        string originalScenePath = originalScene.path;

        // Finde alle .unity Dateien im Projekt
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        int fixedScenes = 0;
        int totalScenes = sceneGuids.Length;

        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            try
            {
                EditorUtility.DisplayProgressBar(
                    "Fixing Scenes", 
                    $"Processing {sceneName}...", 
                    (float)fixedScenes / totalScenes);

                if (FixSceneCamera(scenePath))
                {
                    fixedScenes++;
                    Debug.Log($"✅ Fixed camera in scene: {sceneName}");
                }
                else
                {
                    Debug.Log($"ℹ️ Scene '{sceneName}' already has a camera");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to fix scene '{sceneName}': {e.Message}");
            }
        }

        EditorUtility.ClearProgressBar();

        // Kehre zur ursprünglichen Szene zurück
        if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        // Zeige Ergebnis
        EditorUtility.DisplayDialog(
            "Camera Fix Complete",
            $"Camera fix completed!\n\n" +
            $"• Processed: {totalScenes} scenes\n" +
            $"• Fixed: {fixedScenes} scenes\n" +
            $"• Already OK: {totalScenes - fixedScenes} scenes\n\n" +
            "All scenes should now have working cameras.",
            "OK");

        Debug.Log($"🎉 ProjectCameraFixer: Fixed {fixedScenes} out of {totalScenes} scenes");
    }

    private static bool FixSceneCamera(string scenePath)
    {
        // Öffne die Szene
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool wasFixed = false;

        // Überprüfe, ob Main Camera existiert
        Camera mainCamera = Camera.main;
        GameObject cameraGO = null;

        if (mainCamera == null)
        {
            // Suche nach beliebiger Kamera
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 0)
            {
                mainCamera = cameras[0];
                mainCamera.tag = "MainCamera";
                Debug.Log($"Tagged existing camera as MainCamera in {scene.name}");
            }
        }

        if (mainCamera == null)
        {
            // Erstelle neue Main Camera
            cameraGO = new GameObject("Main Camera");
            mainCamera = cameraGO.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            wasFixed = true;

            // Standard Kamera-Einstellungen
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.backgroundColor = new Color(0.192f, 0.301f, 0.475f); // Unity default
            mainCamera.cullingMask = -1;
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60f;
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;

            Debug.Log($"Created Main Camera in {scene.name}");
        }
        else
        {
            cameraGO = mainCamera.gameObject;
        }

        // AudioListener hinzufügen falls nicht vorhanden
        AudioListener audioListener = cameraGO.GetComponent<AudioListener>();
        if (audioListener == null)
        {
            cameraGO.AddComponent<AudioListener>();
            wasFixed = true;
            Debug.Log($"Added AudioListener to camera in {scene.name}");
        }

        // CameraController hinzufügen für Level1-3
        string sceneName = scene.name.ToLower();
        if (sceneName.Contains("level") || sceneName.Contains("generated"))
        {
            CameraController cameraController = cameraGO.GetComponent<CameraController>();
            if (cameraController == null)
            {
                cameraController = cameraGO.AddComponent<CameraController>();
                wasFixed = true;
                Debug.Log($"Added CameraController to camera in {scene.name}");
            }
        }

        // Positioniere Kamera sinnvoll
        PositionCameraForScene(mainCamera, scene);

        // Speichere Szene falls geändert
        if (wasFixed)
        {
            EditorSceneManager.SaveScene(scene);
        }

        return wasFixed;
    }

    private static void PositionCameraForScene(Camera camera, Scene scene)
    {
        Vector3 cameraPosition = new Vector3(0, 10, -15);
        Vector3 lookAtPosition = Vector3.zero;

        // Szenen-spezifische Positionierung
        string sceneName = scene.name.ToLower();

        if (sceneName.Contains("level1") || sceneName == "level1")
        {
            // Level 1: Einfache Übersicht
            cameraPosition = new Vector3(5, 8, -12);
            lookAtPosition = new Vector3(5, 0, 5);
        }
        else if (sceneName.Contains("level2") || sceneName == "level2")
        {
            // Level 2: Leicht erhöht
            cameraPosition = new Vector3(8, 12, -15);
            lookAtPosition = new Vector3(8, 0, 8);
        }
        else if (sceneName.Contains("level3") || sceneName == "level3")
        {
            // Level 3: Höhere Sicht für komplexeres Level
            cameraPosition = new Vector3(10, 15, -18);
            lookAtPosition = new Vector3(10, 0, 10);
        }
        else if (sceneName.Contains("generated"))
        {
            // Generiertes Level: Mittig über dem Spielfeld
            cameraPosition = new Vector3(16, 20, -20); // Für 16x16 Grid
            lookAtPosition = new Vector3(16, 0, 16);
        }
        else
        {
            // Standard für andere Szenen
            cameraPosition = new Vector3(0, 10, -15);
            lookAtPosition = Vector3.zero;
        }

        // Setze Position und Rotation
        camera.transform.position = cameraPosition;
        camera.transform.LookAt(lookAtPosition);

        Debug.Log($"Positioned camera in {scene.name} at {cameraPosition}");
    }
    #endif

    /// <summary>
    /// Runtime-Version für Testing (ohne Editor)
    /// </summary>
    void Start()
    {
        // Überprüfe aktuelle Szene zur Laufzeit
        CheckCurrentSceneCamera();
    }

    private void CheckCurrentSceneCamera()
    {
        Camera mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogWarning("❌ No Main Camera found in current scene!");
            
            // Versuche automatische Reparatur zur Laufzeit
            CreateRuntimeCamera();
        }
        else
        {
            Debug.Log($"✅ Main Camera found: {mainCamera.name}");
        }
    }

    private void CreateRuntimeCamera()
    {
        GameObject cameraGO = new GameObject("Runtime Main Camera");
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraGO.AddComponent<AudioListener>();

        // Standard-Positionierung
        cameraGO.transform.position = new Vector3(10, 15, -15);
        cameraGO.transform.LookAt(Vector3.zero);

        // CameraController hinzufügen
        CameraController controller = cameraGO.AddComponent<CameraController>();

        Debug.Log("✅ Runtime camera created successfully!");
    }

    /// <summary>
    /// Überprüft alle Szenen und gibt einen Report aus
    /// </summary>
    #if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/Validate All Scenes")]
    public static void ValidateAllScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        int scenesWithCamera = 0;
        int scenesWithoutCamera = 0;
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        
        report.AppendLine("📋 Scene Camera Validation Report:");
        report.AppendLine("=====================================");

        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            // Lade Szene temporär (ohne GUI zu ändern)
            Scene originalScene = SceneManager.GetActiveScene();
            Scene tempScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(tempScene);

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                scenesWithCamera++;
                report.AppendLine($"✅ {sceneName} - Has Main Camera: {mainCamera.name}");
            }
            else
            {
                scenesWithoutCamera++;
                report.AppendLine($"❌ {sceneName} - Missing Main Camera!");
            }

            EditorSceneManager.CloseScene(tempScene, true);
            SceneManager.SetActiveScene(originalScene);
        }

        report.AppendLine("=====================================");
        report.AppendLine($"📊 Summary:");
        report.AppendLine($"   Scenes with camera: {scenesWithCamera}");
        report.AppendLine($"   Scenes missing camera: {scenesWithoutCamera}");
        report.AppendLine($"   Total scenes: {sceneGuids.Length}");

        if (scenesWithoutCamera > 0)
        {
            report.AppendLine($"\n⚠️  Action needed: Run 'Fix All Scenes' to repair {scenesWithoutCamera} scenes");
        }
        else
        {
            report.AppendLine($"\n🎉 Perfect! All scenes have cameras.");
        }

        Debug.Log(report.ToString());

        // Zeige Report im Editor
        EditorUtility.DisplayDialog(
            "Scene Validation Complete",
            $"Validation complete!\n\n" +
            $"Scenes with camera: {scenesWithCamera}\n" +
            $"Scenes missing camera: {scenesWithoutCamera}\n\n" +
            $"Check Console for detailed report.",
            "OK");
    }
    #endif
}
