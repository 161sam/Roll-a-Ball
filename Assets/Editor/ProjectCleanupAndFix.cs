using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace RollABall.Editor
{
    public static class ProjectCleanupAndFix
    {
        [MenuItem("Roll-a-Ball/🧹 Complete Project Cleanup & Fix", priority = 0)]
        public static void CompleteProjectCleanupAndFix()
        {
            if (!EditorUtility.DisplayDialog("Project Cleanup", 
                "This will clean up duplicate files and fix all scene issues. This action cannot be undone.\n\nProceed?", 
                "Yes, Clean Up", "Cancel"))
            {
                return;
            }

            Debug.Log("=== Starting Complete Project Cleanup ===");
            
            // Step 1: Clean up duplicate scripts
            CleanupDuplicateScripts();
            
            // Step 2: Clean up ScriptableObject duplicates
            CleanupScriptableObjectDuplicates();
            
            // Step 3: Create proper LevelProfiles
            CreateProperLevelProfiles();
            
            // Step 4: Fix all scenes
            FixAllScenes();
            
            // Step 5: Clean up obsolete menu items
            RemoveObsoleteScripts();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("=== Project Cleanup Complete! ===");
            EditorUtility.DisplayDialog("Success", "Project cleanup completed successfully!\n\nAll duplicate files removed and scenes fixed.", "OK");
        }

        private static void CleanupDuplicateScripts()
        {
            Debug.Log("Cleaning up duplicate scripts...");
            
            List<string> filesToDelete = new List<string>
            {
                "Assets/Scripts/LevelProfile.cs",  // Keep the one in Generators/
                "Assets/Scripts/Editor/LevelProfileCreator.cs",  // Keep the one in Generators/
                "Assets/Scripts/ComprehensiveSceneFixer.cs",  // Was temporary
                "Assets/Scripts/AutoSceneFix.cs",  // Obsolete
                "Assets/Scripts/CompleteSceneSetup.cs",  // Obsolete
                "Assets/Scripts/FixCameraNow.cs",  // Obsolete
                "Assets/Scripts/ProjectCameraFixer.cs",  // Obsolete
                "Assets/Scripts/QuickCameraFix.cs",  // Obsolete
                "Assets/Scripts/CameraSetupHelper.cs",  // Obsolete
                "Assets/Scripts/CameraFollow.cs"  // Duplicate of CameraController
            };

            foreach (string filePath in filesToDelete)
            {
                if (File.Exists(filePath))
                {
                    AssetDatabase.DeleteAsset(filePath);
                    Debug.Log($"Deleted duplicate/obsolete script: {filePath}");
                }
            }
        }

        private static void CleanupScriptableObjectDuplicates()
        {
            Debug.Log("Cleaning up ScriptableObject duplicates...");
            
            // Remove duplicate directories
            List<string> foldersToDelete = new List<string>
            {
                "Assets/ScriptableObjects",
                "Assets/Data"
            };

            foreach (string folderPath in foldersToDelete)
            {
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.DeleteAsset(folderPath);
                    Debug.Log($"Deleted duplicate folder: {folderPath}");
                }
            }
        }

        public static void CreateProperLevelProfiles()
        {
            Debug.Log("Creating proper LevelProfiles...");
            
            string folderPath = "Assets/Resources/LevelProfiles";
            
            // Ensure directories exist
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "LevelProfiles");
            }

            // Create or update profiles
            CreateOrUpdateProfile("EasyProfile", "Easy", 8, 5, 0.1f, LevelGenerationMode.Simple, folderPath);
            CreateOrUpdateProfile("MediumProfile", "Medium", 12, 10, 0.2f, LevelGenerationMode.Maze, folderPath);
            CreateOrUpdateProfile("HardProfile", "Hard", 16, 15, 0.4f, LevelGenerationMode.Hybrid, folderPath);

            Debug.Log("LevelProfiles created successfully!");
        }

        private static void CreateOrUpdateProfile(string fileName, string profileName, int levelSize, 
            int collectibles, float obstacleDensity, LevelGenerationMode mode, string folderPath)
        {
            string fullPath = Path.Combine(folderPath, fileName + ".asset");
            
            LevelProfile profile = AssetDatabase.LoadAssetAtPath<LevelProfile>(fullPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<LevelProfile>();
                AssetDatabase.CreateAsset(profile, fullPath);
            }

            // Use SerializedObject to set private fields
            SerializedObject serializedProfile = new SerializedObject(profile);
            
            SetSerializedProperty(serializedProfile, "profileName", profileName);
            SetSerializedProperty(serializedProfile, "displayName", profileName + " Level");
            SetSerializedProperty(serializedProfile, "difficultyLevel", GetDifficultyLevel(profileName));
            SetSerializedProperty(serializedProfile, "levelSize", levelSize);
            SetSerializedProperty(serializedProfile, "collectibleCount", collectibles);
            SetSerializedProperty(serializedProfile, "obstacleDensity", obstacleDensity);
            SetSerializedProperty(serializedProfile, "generationMode", (int)mode);
            SetSerializedProperty(serializedProfile, "themeColor", GetThemeColor(profileName));
            
            serializedProfile.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(profile);
            Debug.Log($"Created/Updated {profileName} profile");
        }

        private static void SetSerializedProperty(SerializedObject obj, string propertyName, object value)
        {
            SerializedProperty prop = obj.FindProperty(propertyName);
            if (prop != null)
            {
                switch (value)
                {
                    case string s: prop.stringValue = s; break;
                    case int i: prop.intValue = i; break;
                    case float f: prop.floatValue = f; break;
                    case Color c: prop.colorValue = c; break;
                }
            }
        }

        private static int GetDifficultyLevel(string profileName)
        {
            switch (profileName.ToLower())
            {
                case "easy": return 1;
                case "medium": return 2;
                case "hard": return 3;
                default: return 1;
            }
        }

        private static Color GetThemeColor(string profileName)
        {
            switch (profileName.ToLower())
            {
                case "easy": return Color.green;
                case "medium": return Color.yellow;
                case "hard": return Color.red;
                default: return Color.white;
            }
        }

        private static void FixAllScenes()
        {
            Debug.Log("Fixing all scenes...");
            
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                
                // Skip example scenes
                if (sceneName.Contains("Sample") || sceneName.Contains("MiniGame"))
                    continue;
                
                Debug.Log($"Fixing scene: {sceneName}");
                FixIndividualScene(scenePath);
            }
        }

        private static void FixIndividualScene(string scenePath)
        {
            UnityEngine.SceneManagement.Scene currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEngine.SceneManagement.Scene sceneToFix = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            
            // Fix duplicate LevelManagers
            LevelManager[] levelManagers = Object.FindObjectsByType<LevelManager>(FindObjectsSortMode.None);
            for (int i = 1; i < levelManagers.Length; i++)
            {
                Object.DestroyImmediate(levelManagers[i].gameObject);
                Debug.Log($"Removed duplicate LevelManager from {sceneToFix.name}");
            }

            // Fix LevelGenerator profile assignment
            LevelGenerator levelGenerator = Object.FindFirstObjectByType<LevelGenerator>();
            if (levelGenerator != null)
            {
                LevelProfile defaultProfile = Resources.Load<LevelProfile>("LevelProfiles/EasyProfile");
                if (defaultProfile != null)
                {
                    SerializedObject serializedGenerator = new SerializedObject(levelGenerator);
                    SerializedProperty profileProperty = serializedGenerator.FindProperty("levelProfile");
                    if (profileProperty != null)
                    {
                        profileProperty.objectReferenceValue = defaultProfile;
                        serializedGenerator.ApplyModifiedProperties();
                        Debug.Log($"Assigned LevelProfile to LevelGenerator in {sceneToFix.name}");
                    }
                }
            }

            // Fix duplicate Player objects
            PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (players.Length > 1)
            {
                Debug.Log($"Found {players.Length} players in {sceneToFix.name}, removing duplicates");
                for (int i = 1; i < players.Length; i++)
                {
                    Object.DestroyImmediate(players[i].gameObject);
                }
            }

            // Ensure collectibles are properly tagged
            CollectibleController[] collectibles = Object.FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
            foreach (CollectibleController collectible in collectibles)
            {
                if (!collectible.CompareTag("Collectible"))
                {
                    collectible.tag = "Collectible";
                    Debug.Log($"Fixed collectible tag in {sceneToFix.name}");
                }
            }

            // Save scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sceneToFix);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sceneToFix);
            
            Debug.Log($"Fixed and saved scene: {sceneToFix.name}");
        }

        private static void RemoveObsoleteScripts()
        {
            Debug.Log("Removing obsolete editor scripts...");
            
            List<string> obsoleteEditorScripts = new List<string>
            {
                "Assets/Editor/BuildProfileSetup.cs",
                "Assets/Editor/SceneReferenceFixer.cs"
            };

            foreach (string scriptPath in obsoleteEditorScripts)
            {
                if (File.Exists(scriptPath))
                {
                    AssetDatabase.DeleteAsset(scriptPath);
                    Debug.Log($"Removed obsolete editor script: {scriptPath}");
                }
            }
        }

        [MenuItem("Roll-a-Ball/🎯 Fix Current Scene Only", priority = 10)]
        public static void FixCurrentSceneOnly()
        {
            UnityEngine.SceneManagement.Scene currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            
            if (!currentScene.IsValid())
            {
                EditorUtility.DisplayDialog("Error", "No valid scene is currently open.", "OK");
                return;
            }

            Debug.Log($"Fixing current scene: {currentScene.name}");
            FixIndividualScene(currentScene.path);
            
            EditorUtility.DisplayDialog("Success", $"Scene '{currentScene.name}' fixed successfully!", "OK");
        }

        [MenuItem("Roll-a-Ball/📊 Project Status Report", priority = 20)]
        public static void GenerateProjectStatusReport()
        {
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== ROLL-A-BALL PROJECT STATUS REPORT ===");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine();

            // Check for duplicate scripts
            report.AppendLine("SCRIPT STATUS:");
            CheckScriptStatus(report, "LevelProfile.cs", new[] { "Assets/Scripts/", "Assets/Scripts/Generators/" });
            CheckScriptStatus(report, "LevelProfileCreator.cs", new[] { "Assets/Scripts/Editor/", "Assets/Scripts/Generators/" });
            
            // Check ScriptableObjects
            report.AppendLine("\nSCRIPTABLEOBJECT STATUS:");
            CheckFolderStatus(report, "Resources/LevelProfiles", "Assets/Resources/LevelProfiles");
            CheckFolderStatus(report, "ScriptableObjects", "Assets/ScriptableObjects");
            CheckFolderStatus(report, "Data", "Assets/Data");

            // Check scenes
            report.AppendLine("\nSCENE STATUS:");
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                report.AppendLine($"  ✓ {sceneName}: {scenePath}");
            }

            Debug.Log(report.ToString());
            
            // Also write to file
            string reportPath = "Assets/PROJECT_STATUS_REPORT.txt";
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Report Generated", $"Project status report generated!\n\nCheck Console and:\n{reportPath}", "OK");
        }

        private static void CheckScriptStatus(System.Text.StringBuilder report, string fileName, string[] locations)
        {
            List<string> existingLocations = new List<string>();
            
            foreach (string location in locations)
            {
                string fullPath = location + fileName;
                if (File.Exists(fullPath))
                {
                    existingLocations.Add(fullPath);
                }
            }

            if (existingLocations.Count == 0)
            {
                report.AppendLine($"  ❌ {fileName}: NOT FOUND");
            }
            else if (existingLocations.Count == 1)
            {
                report.AppendLine($"  ✓ {fileName}: {existingLocations[0]}");
            }
            else
            {
                report.AppendLine($"  ⚠️  {fileName}: DUPLICATES FOUND:");
                foreach (string location in existingLocations)
                {
                    report.AppendLine($"    - {location}");
                }
            }
        }

        private static void CheckFolderStatus(System.Text.StringBuilder report, string folderName, string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                string[] assets = AssetDatabase.FindAssets("", new[] { folderPath });
                report.AppendLine($"  ✓ {folderName}: {assets.Length} assets in {folderPath}");
            }
            else
            {
                report.AppendLine($"  ❌ {folderName}: NOT FOUND at {folderPath}");
            }
        }

        /// <summary>
        /// Assigns missing prefab references to a LevelGenerator
        /// </summary>
        public static void AssignPrefabReferences(LevelGenerator generator)
        {
            if (generator == null)
            {
                Debug.LogError("LevelGenerator is null! Cannot assign prefab references.");
                return;
            }

            Debug.Log("🧩 Assigning prefab references to LevelGenerator...");
            
            SerializedObject serializedGenerator = new SerializedObject(generator);
            bool hasChanges = false;

            // Find and assign Ground Prefab
            SerializedProperty groundPrefabProp = serializedGenerator.FindProperty("groundPrefab");
            if (groundPrefabProp != null && groundPrefabProp.objectReferenceValue == null)
            {
                GameObject groundPrefab = FindPrefabByName("GroundPrefab", "Ground", "GroundTile");
                if (groundPrefab != null)
                {
                    groundPrefabProp.objectReferenceValue = groundPrefab;
                    hasChanges = true;
                    Debug.Log($"✅ Assigned Ground Prefab: {groundPrefab.name}");
                }
                else
                {
                    Debug.LogWarning("⚠️ Ground Prefab not found! Please assign manually.");
                }
            }

            // Find and assign Wall Prefab
            SerializedProperty wallPrefabProp = serializedGenerator.FindProperty("wallPrefab");
            if (wallPrefabProp != null && wallPrefabProp.objectReferenceValue == null)
            {
                GameObject wallPrefab = FindPrefabByName("WallPrefab", "Wall", "WallTile");
                if (wallPrefab != null)
                {
                    wallPrefabProp.objectReferenceValue = wallPrefab;
                    hasChanges = true;
                    Debug.Log($"✅ Assigned Wall Prefab: {wallPrefab.name}");
                }
                else
                {
                    Debug.LogWarning("⚠️ Wall Prefab not found! Please assign manually.");
                }
            }

            // Find and assign Collectible Prefab
            SerializedProperty collectiblePrefabProp = serializedGenerator.FindProperty("collectiblePrefab");
            if (collectiblePrefabProp != null && collectiblePrefabProp.objectReferenceValue == null)
            {
                GameObject collectiblePrefab = FindPrefabByName("CollectiblePrefab", "Collectible", "PickUp");
                if (collectiblePrefab != null)
                {
                    collectiblePrefabProp.objectReferenceValue = collectiblePrefab;
                    hasChanges = true;
                    Debug.Log($"✅ Assigned Collectible Prefab: {collectiblePrefab.name}");
                }
                else
                {
                    Debug.LogWarning("⚠️ Collectible Prefab not found! Please assign manually.");
                }
            }

            // Find and assign Goal Zone Prefab
            SerializedProperty goalZonePrefabProp = serializedGenerator.FindProperty("goalZonePrefab");
            if (goalZonePrefabProp != null && goalZonePrefabProp.objectReferenceValue == null)
            {
                GameObject goalZonePrefab = FindPrefabByName("GoalZonePrefab", "GoalZone", "Goal", "Finish");
                if (goalZonePrefab != null)
                {
                    goalZonePrefabProp.objectReferenceValue = goalZonePrefab;
                    hasChanges = true;
                    Debug.Log($"✅ Assigned Goal Zone Prefab: {goalZonePrefab.name}");
                }
                else
                {
                    Debug.LogWarning("⚠️ Goal Zone Prefab not found! Please assign manually.");
                }
            }

            // Find and assign Player Prefab
            SerializedProperty playerPrefabProp = serializedGenerator.FindProperty("playerPrefab");
            if (playerPrefabProp != null && playerPrefabProp.objectReferenceValue == null)
            {
                GameObject playerPrefab = FindPrefabByName("PlayerPrefab", "Player", "Ball");
                if (playerPrefab != null)
                {
                    playerPrefabProp.objectReferenceValue = playerPrefab;
                    hasChanges = true;
                    Debug.Log($"✅ Assigned Player Prefab: {playerPrefab.name}");
                }
                else
                {
                    Debug.LogWarning("⚠️ Player Prefab not found! Please assign manually.");
                }
            }

            // Apply changes if any were made
            if (hasChanges)
            {
                serializedGenerator.ApplyModifiedProperties();
                EditorUtility.SetDirty(generator);
                Debug.Log("🧩 Prefab references assignment completed!");
            }
            else
            {
                Debug.Log("🧩 All prefab references were already assigned.");
            }
        }

        /// <summary>
        /// Helper method to find a prefab by trying multiple potential names
        /// </summary>
        private static GameObject FindPrefabByName(params string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                // Search in Prefabs folder first
                string[] guids = AssetDatabase.FindAssets($"{name} t:Prefab", new[] { "Assets/Prefabs" });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        return prefab;
                }

                // Search in entire Assets folder if not found in Prefabs
                guids = AssetDatabase.FindAssets($"{name} t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        return prefab;
                }
            }

            return null;
        }
    }
}
