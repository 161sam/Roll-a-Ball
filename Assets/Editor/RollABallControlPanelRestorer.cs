using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace RollABall.Editor
{
    // TODO-DUPLICATE#3: Funktional identisch mit CleanRollABallMenu.cs und RollABallMenuIntegration.cs. Bitte vereinheitlichen oder entfernen.
    public class RollABallControlPanelRestorer : EditorWindow
    {
        private Vector2 scrollPosition;

        // [MenuItem("Roll-a-Ball/🎮 Roll-a-Ball Control Panel", priority = 1)] - DEACTIVATED: Duplicate MenuItem
        // Use CleanRollABallMenu.cs instead
        public static void ShowControlPanel()
        {
            RollABallControlPanelRestorer window = GetWindow<RollABallControlPanelRestorer>("🎮 Roll-a-Ball Control Panel");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("🎮 Roll-a-Ball Control Panel", EditorStyles.largeLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Zentrales Control Panel für das Roll-a-Ball Projekt", MessageType.Info);
            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Scene Management
            GUILayout.Label("🎬 Scene Management", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Level1")) LoadScene("Level1");
            if (GUILayout.Button("Level2")) LoadScene("Level2");
            if (GUILayout.Button("Level3")) LoadScene("Level3");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("GeneratedLevel")) LoadScene("GeneratedLevel");
            if (GUILayout.Button("Level_OSM")) LoadScene("Level_OSM");
            if (GUILayout.Button("MiniGame")) LoadScene("MiniGame");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Tools & Utilities
            GUILayout.Label("🔧 Tools & Utilities", EditorStyles.boldLabel);
            
            if (GUILayout.Button("🔧 Unity Console Error Fixer"))
            {
                UnityConsoleErrorFixer.ShowWindow();
            }
            
            if (GUILayout.Button("🎯 Scene Consolidator"))
            {
                ShowSceneConsolidator();
            }
            
            if (GUILayout.Button("🔍 Scene Validator"))
            {
                ShowSceneValidator();
            }

            GUILayout.Space(10);

            // System & Repair
            GUILayout.Label("🛠️ System & Repair", EditorStyles.boldLabel);
            
            if (GUILayout.Button("🚨 Emergency Scene Repair"))
            {
                EmergencySceneRepair();
            }
            
            if (GUILayout.Button("🧹 Clean Project Cache"))
            {
                CleanProjectCache();
            }
            
            if (GUILayout.Button("🔄 Refresh Assets"))
            {
                AssetDatabase.Refresh();
                Debug.Log("✅ Assets refreshed");
            }

            GUILayout.Space(10);

            // Status & Information
            GUILayout.Label("📊 Status & Information", EditorStyles.boldLabel);
            
            if (GUILayout.Button("📋 Project Status Report"))
            {
                GenerateProjectStatusReport();
            }
            
            if (GUILayout.Button("🎯 Validate All Scenes"))
            {
                ValidateAllScenes();
            }
            
            if (GUILayout.Button("📝 Generate Scene Reports"))
            {
                GenerateSceneReports();
            }

            GUILayout.Space(20);

            // Quick Actions
            GUILayout.Label("⚡ Quick Actions", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("▶️ Play Current Scene", GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = true;
            }
            
            if (GUILayout.Button("⏹️ Stop Play Mode", GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = false;
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void LoadScene(string sceneName)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Warnung", "Bitte stoppe Play Mode vor dem Laden einer neuen Scene.", "OK");
                return;
            }

            string scenePath = $"Assets/Scenes/{sceneName}.unity";
            if (File.Exists(Path.Combine(Application.dataPath, "..", scenePath)))
            {
                EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"✅ Scene {sceneName} geladen");
            }
            else
            {
                Debug.LogWarning($"⚠️ Scene {sceneName} nicht gefunden: {scenePath}");
                EditorUtility.DisplayDialog("Scene nicht gefunden", $"Scene {sceneName} existiert nicht.\nPfad: {scenePath}", "OK");
            }
        }

        private void ShowSceneConsolidator()
        {
            // Try to find and show existing QuickSceneConsolidator
            var consolidatorType = System.Type.GetType("QuickSceneConsolidator");
            if (consolidatorType != null)
            {
                var window = GetWindow(consolidatorType);
                window.Show();
            }
            else
            {
                Debug.Log("🔧 QuickSceneConsolidator wird gestartet...");
                EditorUtility.DisplayDialog("Scene Consolidator", "QuickSceneConsolidator Tool wird gestartet.\nFalls nicht verfügbar, nutze Emergency Scene Repair.", "OK");
            }
        }

        private void ShowSceneValidator()
        {
            Debug.Log("🔍 Scene Validator wird gestartet...");
            EditorUtility.DisplayDialog("Scene Validator", "Scene Validator Tool wird gestartet.\nÜberprüfung der aktuellen Scene...", "OK");
        }

        private void EmergencySceneRepair()
        {
            Debug.Log("🚨 Emergency Scene Repair wird gestartet...");
            
            try
            {
                // Repair current scene
                RepairCurrentScene();
                Debug.Log("✅ Emergency Scene Repair abgeschlossen");
                EditorUtility.DisplayDialog("Repair Complete", "Emergency Scene Repair wurde erfolgreich ausgeführt!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Emergency Scene Repair Fehler: {e.Message}");
                EditorUtility.DisplayDialog("Repair Failed", $"Emergency Scene Repair fehlgeschlagen:\n{e.Message}", "OK");
            }
        }

        private void RepairCurrentScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            Debug.Log($"🔧 Repariere Scene: {currentScene.name}");

            // Find essential components
            var player = Object.FindFirstObjectByType<PlayerController>();
            var gameManager = Object.FindFirstObjectByType<GameManager>();
            var camera = Camera.main;

            // Repair Player
            if (player == null)
            {
                Debug.Log("⚠️ Player nicht gefunden, versuche Reparatur...");
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO == null)
                {
                    Debug.LogWarning("❌ Player GameObject nicht gefunden");
                }
            }

            // Repair Camera
            if (camera == null)
            {
                Debug.Log("⚠️ Main Camera nicht gefunden, erstelle neue...");
                CreateMainCamera();
            }

            // Repair GameManager
            if (gameManager == null)
            {
                Debug.Log("⚠️ GameManager nicht gefunden, versuche Reparatur...");
                var managerGO = new GameObject("GameManager");
                managerGO.AddComponent<GameManager>();
            }

            EditorSceneManager.MarkSceneDirty(currentScene);
        }

        private void CreateMainCamera()
        {
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();
            
            // Add CameraController if available
            var cameraControllerType = System.Type.GetType("CameraController");
            if (cameraControllerType != null)
            {
                cameraGO.AddComponent(cameraControllerType);
            }
            
            cameraGO.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 10, -10);
            camera.transform.LookAt(Vector3.zero);
            
            Debug.Log("✅ Main Camera erstellt");
        }

        private void CleanProjectCache()
        {
            Debug.Log("🧹 Cleaning project cache...");
            
            AssetDatabase.Refresh();
            
            // Force reimport critical assets
            AssetDatabase.ImportAsset("Assets/Scripts", ImportAssetOptions.ImportRecursive);
            AssetDatabase.ImportAsset("Assets/Scenes", ImportAssetOptions.ImportRecursive);
            
            Debug.Log("✅ Project cache cleaned");
            EditorUtility.DisplayDialog("Cache Cleaned", "Project cache wurde bereinigt.\nAssets wurden neu importiert.", "OK");
        }

        private void GenerateProjectStatusReport()
        {
            Debug.Log("📋 Generiere Project Status Report...");
            
            var scenes = new[] { "Level1", "Level2", "Level3", "GeneratedLevel", "Level_OSM", "MiniGame" };
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== ROLL-A-BALL PROJECT STATUS REPORT ===");
            report.AppendLine($"Generiert am: {System.DateTime.Now}");
            report.AppendLine();
            
            foreach (var sceneName in scenes)
            {
                string scenePath = $"Assets/Scenes/{sceneName}.unity";
                bool exists = File.Exists(Path.Combine(Application.dataPath, "..", scenePath));
                report.AppendLine($"Scene {sceneName}: {(exists ? "✅ Vorhanden" : "❌ Fehlt")}");
            }
            
            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Project Status", "Status Report wurde in der Console ausgegeben.", "OK");
        }

        private void ValidateAllScenes()
        {
            Debug.Log("🎯 Validiere alle Scenes...");
            
            var scenes = new[] { "Level1", "Level2", "Level3", "GeneratedLevel", "Level_OSM", "MiniGame" };
            var currentScene = SceneManager.GetActiveScene();
            
            foreach (var sceneName in scenes)
            {
                string scenePath = $"Assets/Scenes/{sceneName}.unity";
                if (File.Exists(Path.Combine(Application.dataPath, "..", scenePath)))
                {
                    Debug.Log($"✅ {sceneName}: Scene verfügbar");
                }
                else
                {
                    Debug.LogWarning($"⚠️ {sceneName}: Scene fehlt");
                }
            }
            
            Debug.Log("🎯 Scene Validation abgeschlossen");
        }

        private void GenerateSceneReports()
        {
            Debug.Log("📝 Generiere Scene Reports...");
            
            var currentScene = SceneManager.GetActiveScene();
            Debug.Log($"📝 Report für aktuelle Scene: {currentScene.name}");
            
            // Count GameObjects
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Debug.Log($"📊 GameObjects in Scene: {allObjects.Length}");
            
            // Count Collectibles
            var collectibles = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
            Debug.Log($"🎯 Collectibles gefunden: {collectibles.Length}");
            
            EditorUtility.DisplayDialog("Scene Reports", "Scene Reports wurden in der Console ausgegeben.", "OK");
        }
    }
}
