using UnityEngine;
using UnityEditor;
using System.IO;

namespace RollABall.Editor
{
    /// <summary>
    /// Sauberes und aktuelles Roll-a-Ball Editor Menu
    /// Entfernt alle obsoleten Funktionen und bietet nur noch relevante Tools
    /// </summary>
    public class CleanRollABallMenu : EditorWindow
    {
        private Vector2 scrollPosition;
        
        [MenuItem("Roll-a-Ball/🎮 Roll-a-Ball Control Panel", priority = 1)]
        public static void ShowWindow()
        {
            CleanRollABallMenu window = GetWindow<CleanRollABallMenu>("Roll-a-Ball Tools");
            window.minSize = new Vector2(320, 500);
            window.Show();
        }
        
        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Space(10);
            
            // Header
            EditorGUILayout.LabelField("🎱 Roll-a-Ball Development Tools", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Clean and Modern Editor Interface", EditorStyles.miniLabel);
            GUILayout.Space(10);
            
            // Quick Actions Section
            EditorGUILayout.LabelField("⚡ Quick Actions", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔧 Fix Current Scene", GUILayout.Height(30)))
            {
                ProjectCleanupAndFix.FixCurrentSceneOnly();
            }
            if (GUILayout.Button("🧹 Complete Cleanup", GUILayout.Height(30)))
            {
                ProjectCleanupAndFix.CompleteProjectCleanupAndFix();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Scene Navigation Section
            EditorGUILayout.LabelField("🎬 Scene Navigation", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Level 1"))
            {
                OpenScene("Assets/Scenes/Level1.unity");
            }
            if (GUILayout.Button("Level 2"))
            {
                OpenScene("Assets/Scenes/Level2.unity");
            }
            if (GUILayout.Button("Level 3"))
            {
                OpenScene("Assets/Scenes/Level3.unity");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generated Level"))
            {
                OpenScene("Assets/Scenes/GeneratedLevel.unity");
            }
            if (GUILayout.Button("Sample Scene"))
            {
                OpenScene("Assets/Scenes/SampleScene.unity");
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Level Generation Section
            EditorGUILayout.LabelField("🏗️ Level Generation", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            if (GUILayout.Button("📋 Create/Update Level Profiles"))
            {
                ProjectCleanupAndFix.CreateProperLevelProfiles();
                EditorUtility.DisplayDialog("Success", "Level Profiles created/updated successfully!", "OK");
            }
            
            if (GUILayout.Button("🎲 Test Level Generation"))
            {
                TestLevelGeneration();
            }
            
            GUILayout.Space(10);
            
            // Development Tools Section
            EditorGUILayout.LabelField("🔧 Development Tools", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            if (GUILayout.Button("📊 Project Status Report"))
            {
                ProjectCleanupAndFix.GenerateProjectStatusReport();
            }
            
            if (GUILayout.Button("🔍 Validate Current Scene"))
            {
                ValidateCurrentScene();
            }
            
            if (GUILayout.Button("🧼 Clear Console"))
            {
                ClearConsole();
            }
            
            GUILayout.Space(10);
            
            // Utilities Section
            EditorGUILayout.LabelField("🛠️ Utilities", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💾 Save All"))
            {
                SaveAllScenes();
            }
            if (GUILayout.Button("🔄 Refresh"))
            {
                AssetDatabase.Refresh();
                Debug.Log("Assets refreshed!");
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("📁 Open Project Folder"))
            {
                EditorUtility.RevealInFinder(Application.dataPath);
            }
            
            GUILayout.Space(20);
            
            // Current Status Display
            EditorGUILayout.LabelField("📈 Current Status", EditorStyles.boldLabel);
            DrawHorizontalLine();
            ShowCurrentStatus();
            
            GUILayout.Space(20);
            
            // Help Section
            EditorGUILayout.LabelField("❓ Help & Info", EditorStyles.boldLabel);
            DrawHorizontalLine();
            EditorGUILayout.HelpBox("Use 'Complete Cleanup' to fix all project issues at once.\\nUse 'Fix Current Scene' for scene-specific problems.", MessageType.Info);
            
            // IMPORTANT: End the scroll view that was started at the beginning of OnGUI
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHorizontalLine()
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(5);
        }
        
        private void OpenScene(string scenePath)
        {
            if (File.Exists(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"Opened scene: {scenePath}");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Scene not found: {scenePath}", "OK");
            }
        }
        
        private void TestLevelGeneration()
        {
            LevelGenerator levelGenerator = Object.FindAnyObjectByType<LevelGenerator>();
            if (levelGenerator == null)
            {
                EditorUtility.DisplayDialog("Error", "No LevelGenerator found in current scene!\\nPlease open a scene with a LevelGenerator or run 'Fix Current Scene' first.", "OK");
                return;
            }
            
            // Check if LevelProfile is assigned
            var serializedGenerator = new SerializedObject(levelGenerator);
            var profileProperty = serializedGenerator.FindProperty("levelProfile");
            
            if (profileProperty.objectReferenceValue == null)
            {
                // Try to assign default profile
                LevelProfile defaultProfile = Resources.Load<LevelProfile>("LevelProfiles/EasyProfile");
                if (defaultProfile != null)
                {
                    profileProperty.objectReferenceValue = defaultProfile;
                    serializedGenerator.ApplyModifiedProperties();
                    Debug.Log("Assigned default EasyProfile to LevelGenerator");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "No LevelProfile found!\\nPlease create Level Profiles first.", "OK");
                    return;
                }
            }
            
            // Trigger generation
            if (Application.isPlaying)
            {
                Debug.Log("Starting level generation...");
                levelGenerator.SendMessage("GenerateLevel", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "Level generation requires Play Mode.\\nPress Play and try again.", "OK");
            }
        }
        
        private void ValidateCurrentScene()
        {
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            
            report.AppendLine($"🔍 Validating Scene: {currentScene.name}");
            report.AppendLine("================================");
            
            bool hasIssues = false;
            
            // Check Player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                report.AppendLine("❌ Player object not found");
                hasIssues = true;
            }
            else
            {
                report.AppendLine("✅ Player object found");
                if (player.GetComponent<PlayerController>() == null)
                {
                    report.AppendLine("  ⚠️ PlayerController component missing");
                    hasIssues = true;
                }
            }
            
            // Check Camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                report.AppendLine("❌ Main Camera not found");
                hasIssues = true;
            }
            else
            {
                report.AppendLine("✅ Main Camera found");
            }
            
            // Check UI
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                report.AppendLine("❌ UI Canvas not found");
                hasIssues = true;
            }
            else
            {
                report.AppendLine("✅ UI Canvas found");
            }
            
            // Check Level Generator (if expected)
            LevelGenerator levelGenerator = Object.FindAnyObjectByType<LevelGenerator>();
            if (currentScene.name == "GeneratedLevel" && levelGenerator == null)
            {
                report.AppendLine("❌ LevelGenerator expected but not found");
                hasIssues = true;
            }
            else if (levelGenerator != null)
            {
                report.AppendLine("✅ LevelGenerator found");
            }
            
            // Check Collectibles
            CollectibleController[] collectibles = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
            report.AppendLine($"📦 Found {collectibles.Length} collectibles");
            
            report.AppendLine("================================");
            if (hasIssues)
            {
                report.AppendLine("⚠️ Issues found - consider running 'Fix Current Scene'");
            }
            else
            {
                report.AppendLine("🎉 Scene validation passed!");
            }
            
            Debug.Log(report.ToString());
            
            string dialogTitle = hasIssues ? "Issues Found" : "Validation Passed";
            string dialogMessage = hasIssues ? 
                "Scene validation found issues.\\nCheck Console for details.\\nRun 'Fix Current Scene' to resolve." :
                "Scene validation passed!\\nEverything looks good.";
            
            EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "OK");
        }
        
        private void SaveAllScenes()
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("💾 All scenes and assets saved!");
        }
        
        private void ClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method?.Invoke(new object(), null);
            Debug.Log("🧼 Console cleared!");
        }
        
        private void ShowCurrentStatus()
        {
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            EditorGUILayout.LabelField($"Scene: {currentScene.name}");
            
            // Quick status checks
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            EditorGUILayout.LabelField($"Player: {(player != null ? "✅" : "❌")}");
            
            Camera mainCamera = Camera.main;
            EditorGUILayout.LabelField($"Camera: {(mainCamera != null ? "✅" : "❌")}");
            
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            EditorGUILayout.LabelField($"UI: {(canvas != null ? "✅" : "❌")}");
            
            LevelGenerator levelGen = Object.FindAnyObjectByType<LevelGenerator>();
            if (currentScene.name == "GeneratedLevel")
            {
                EditorGUILayout.LabelField($"Generator: {(levelGen != null ? "✅" : "❌")}");
            }
            
            CollectibleController[] collectibles = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
            EditorGUILayout.LabelField($"Collectibles: {collectibles.Length}");
        }
    }
    
    /// <summary>
    /// Schnelle Menu-Einträge für häufig verwendete Aktionen
    /// </summary>
    public static class RollABallQuickActions
    {
        [MenuItem("Roll-a-Ball/⚡ Quick Fix Current Scene", priority = 10)]
        public static void QuickFixCurrentScene()
        {
            ProjectCleanupAndFix.FixCurrentSceneOnly();
        }
        
        [MenuItem("Roll-a-Ball/🧹 Quick Complete Cleanup", priority = 11)]
        public static void QuickCompleteCleanup()
        {
            ProjectCleanupAndFix.CompleteProjectCleanupAndFix();
        }
        
        [MenuItem("Roll-a-Ball/🧼 Clear Console", priority = 50)]
        public static void QuickClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method?.Invoke(new object(), null);
        }
        
        [MenuItem("Roll-a-Ball/Scenes/Level 1", priority = 100)]
        public static void OpenLevel1() => OpenScene("Assets/Scenes/Level1.unity");
        
        [MenuItem("Roll-a-Ball/Scenes/Level 2", priority = 101)]
        public static void OpenLevel2() => OpenScene("Assets/Scenes/Level2.unity");
        
        [MenuItem("Roll-a-Ball/Scenes/Level 3", priority = 102)]
        public static void OpenLevel3() => OpenScene("Assets/Scenes/Level3.unity");
        
        [MenuItem("Roll-a-Ball/Scenes/Generated Level", priority = 103)]
        public static void OpenGeneratedLevel() => OpenScene("Assets/Scenes/GeneratedLevel.unity");
        
        private static void OpenScene(string scenePath)
        {
            if (File.Exists(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Scene not found: {scenePath}", "OK");
            }
        }
    }
}
