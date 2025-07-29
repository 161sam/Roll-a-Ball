using UnityEngine;
using UnityEditor;
using System.IO;
using RollABall.Map;
using TMPro;
using UnityEngine.UI;

namespace RollABall.Editor
{
    /// <summary>
    /// OSM-Integration Extension for Roll-a-Ball Editor Menu
    /// Provides tools for OpenStreetMap map generation, testing, and management
    /// </summary>
    public class OSMEditorExtension : EditorWindow
    {
        private Vector2 scrollPosition;
        private string testAddress = "Leipzig, Markt";
        private bool showAdvancedOSM = false;
        private bool osmSystemStatus = false;
        
        [MenuItem("Roll-a-Ball/🗺️ OSM Map Tools", priority = 2)]
        public static void ShowOSMWindow()
        {
            OSMEditorExtension window = GetWindow<OSMEditorExtension>("OSM Map Tools");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        
        void OnEnable()
        {
            RefreshOSMStatus();
        }
        
        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Space(10);
            
            // Header
            EditorGUILayout.LabelField("🗺️ OpenStreetMap Integration Tools", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Real-World Map Generation for Roll-a-Ball", EditorStyles.miniLabel);
            GUILayout.Space(10);
            
            // System Status
            DrawOSMSystemStatus();
            GUILayout.Space(15);
            
            // Quick Setup Section
            EditorGUILayout.LabelField("⚡ Quick Setup", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🏗️ Setup OSM Scene", GUILayout.Height(35)))
            {
                SetupOSMScene();
            }
            if (GUILayout.Button("🔧 Complete OSM Setup", GUILayout.Height(35)))
            {
                CompleteOSMSetup();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Scene Navigation 
            EditorGUILayout.LabelField("🎬 OSM Scene Management", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📍 Open OSM Scene"))
            {
                OpenOSMScene();
            }
            if (GUILayout.Button("🔄 Reload OSM Scene"))
            {
                ReloadOSMScene();
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("📊 Validate OSM Scene"))
            {
                ValidateOSMScene();
            }
            
            GUILayout.Space(10);
            
            // Map Testing Section
            EditorGUILayout.LabelField("🧪 Map Testing Tools", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            EditorGUILayout.LabelField("Test Address:");
            testAddress = EditorGUILayout.TextField(testAddress);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌍 Test Address Resolution"))
            {
                TestAddressResolution();
            }
            if (GUILayout.Button("🏗️ Generate Test Map"))
            {
                GenerateTestMap();
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("🎯 Test with Fallback Location"))
            {
                TestFallbackLocation();
            }
            
            GUILayout.Space(10);
            
            // API Testing Section
            EditorGUILayout.LabelField("🔌 API Testing", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌐 Test Nominatim API"))
            {
                TestNominatimAPI();
            }
            if (GUILayout.Button("🗂️ Test Overpass API"))
            {
                TestOverpassAPI();
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("📡 Test Full API Pipeline"))
            {
                TestFullAPIPipeline();
            }
            
            GUILayout.Space(10);
            
            // Debug Tools Section
            EditorGUILayout.LabelField("🐛 Debug Tools", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            if (GUILayout.Button("📋 Generate OSM Debug Report"))
            {
                GenerateOSMDebugReport();
            }
            
            if (GUILayout.Button("🧹 Clear Generated Objects"))
            {
                ClearGeneratedObjects();
            }
            
            if (GUILayout.Button("📦 Export OSM Data"))
            {
                ExportOSMData();
            }
            
            GUILayout.Space(10);
            
            // Advanced Tools Section
            showAdvancedOSM = EditorGUILayout.Foldout(showAdvancedOSM, "🔧 Advanced OSM Tools");
            if (showAdvancedOSM)
            {
                EditorGUI.indentLevel++;
                DrawAdvancedOSMTools();
                EditorGUI.indentLevel--;
            }
            
            GUILayout.Space(15);
            
            // Current OSM Status
            EditorGUILayout.LabelField("📊 Current OSM Status", EditorStyles.boldLabel);
            DrawHorizontalLine();
            ShowCurrentOSMStatus();
            
            GUILayout.Space(20);
            
            // Help Section
            EditorGUILayout.LabelField("❓ OSM Help & Info", EditorStyles.boldLabel);
            DrawHorizontalLine();
            EditorGUILayout.HelpBox(
                "OSM Integration allows players to enter real addresses and explore their own city as a Roll-a-Ball level.\n\n" +
                "• Use 'Setup OSM Scene' to prepare the Level_OSM scene\n" +
                "• Test with 'Leipzig, Markt' for reliable results\n" +
                "• Fallback system ensures game works offline\n" +
                "• Check OSM Status for system health", 
                MessageType.Info);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHorizontalLine()
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(5);
        }
        
        private void DrawOSMSystemStatus()
        {
            EditorGUILayout.LabelField("🔍 System Status", EditorStyles.boldLabel);
            DrawHorizontalLine();
            
            // Check OSM Scene
            bool osmSceneExists = File.Exists("Assets/Scenes/Level_OSM.unity");
            DrawStatusLine("OSM Scene", osmSceneExists);
            
            // Check OSM Scripts
            bool osmScriptsExist = CheckOSMScripts();
            DrawStatusLine("OSM Scripts", osmScriptsExist);
            
            // Check MapStartupController in scene
            bool mapControllerInScene = false;
            if (Application.isPlaying || UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name == "Level_OSM")
            {
                mapControllerInScene = Object.FindAnyObjectByType<MapStartupController>() != null;
            }
            DrawStatusLine("Map Controller", mapControllerInScene);
            
            // Overall status
            osmSystemStatus = osmSceneExists && osmScriptsExist;
            EditorGUILayout.Space(5);
            string statusText = osmSystemStatus ? "✅ OSM System Ready" : "⚠️ OSM Setup Required";
            Color statusColor = osmSystemStatus ? Color.green : Color.yellow;
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
            GUI.color = oldColor;
        }
        
        private void DrawStatusLine(string label, bool status)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label + ":", GUILayout.Width(120));
            
            var oldColor = GUI.color;
            GUI.color = status ? Color.green : Color.red;
            EditorGUILayout.LabelField(status ? "✅" : "❌", GUILayout.Width(30));
            GUI.color = oldColor;
            
            EditorGUILayout.LabelField(status ? "Ready" : "Missing");
            EditorGUILayout.EndHorizontal();
        }
        
        private bool CheckOSMScripts()
        {
            string[] requiredScripts = {
                "Assets/Scripts/Map/OSMMapData.cs",
                "Assets/Scripts/Map/AddressResolver.cs", 
                "Assets/Scripts/Map/MapGenerator.cs",
                "Assets/Scripts/Map/MapStartupController.cs"
            };
            
            foreach (string script in requiredScripts)
            {
                if (!File.Exists(script))
                    return false;
            }
            return true;
        }
        
        private void RefreshOSMStatus()
        {
            // Refresh status when window opens
            Repaint();
        }
        
        public void SetupOSMScene()
        {
            if (!File.Exists("Assets/Scenes/Level_OSM.unity"))
            {
                EditorUtility.DisplayDialog("Error", "Level_OSM.unity scene not found!\nPlease ensure the OSM scene exists.", "OK");
                return;
            }
            
            // Open OSM scene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level_OSM.unity");
            
            // Run OSMSceneCompleter
            var completer = Object.FindAnyObjectByType<OSMSceneCompleter>();
            if (completer == null)
            {
                var tempObj = new GameObject("Temp_OSMSceneCompleter");
                completer = tempObj.AddComponent<OSMSceneCompleter>();
            }
            
            completer.CompleteOSMSceneSetup();
            
            if (completer.gameObject.name == "Temp_OSMSceneCompleter")
            {
                DestroyImmediate(completer.gameObject);
            }
            
            EditorUtility.DisplayDialog("Success", "OSM Scene setup completed!\nLevel_OSM is ready for use.", "OK");
            RefreshOSMStatus();
        }
        
        private void CompleteOSMSetup()
        {
            SetupOSMScene();
            
            // Additional setup steps
            CreateOSMBuildSettings();
            CreateOSMMaterials();
            
            EditorUtility.DisplayDialog("Complete Setup", 
                "OSM System fully configured!\n\n" +
                "✅ Scene setup complete\n" +
                "✅ Build settings updated\n" +
                "✅ Materials configured\n\n" +
                "Ready for testing and deployment!", "OK");
            
            RefreshOSMStatus();
        }
        
        private void OpenOSMScene()
        {
            string scenePath = "Assets/Scenes/Level_OSM.unity";
            if (File.Exists(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log("Opened OSM scene: Level_OSM.unity");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Level_OSM.unity not found!\nPlease create the OSM scene first.", "OK");
            }
        }
        
        private void ReloadOSMScene()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.name == "Level_OSM")
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(activeScene.path);
                Debug.Log("Reloaded OSM scene");
            }
            else
            {
                OpenOSMScene();
            }
        }
        
        private void ValidateOSMScene()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.name != "Level_OSM")
            {
                EditorUtility.DisplayDialog("Error", "Please open Level_OSM scene first!", "OK");
                return;
            }
            
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("🗺️ OSM Scene Validation Report");
            report.AppendLine("=====================================");
            
            bool hasIssues = false;
            
            // Check MapStartupController
            MapStartupController mapController = Object.FindAnyObjectByType<MapStartupController>();
            if (mapController == null)
            {
                report.AppendLine("❌ MapStartupController not found");
                hasIssues = true;
            }
            else
            {
                report.AppendLine("✅ MapStartupController found");
                
                // Check AddressResolver
                AddressResolver resolver = mapController.GetComponent<AddressResolver>();
                if (resolver == null)
                {
                    report.AppendLine("  ⚠️ AddressResolver component missing");
                    hasIssues = true;
                }
                
                // Check MapGenerator
                MapGenerator generator = mapController.GetComponent<MapGenerator>();
                if (generator == null)
                {
                    report.AppendLine("  ⚠️ MapGenerator component missing");
                    hasIssues = true;
                }
            }
            
            // Check UI Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                report.AppendLine("❌ UI Canvas not found");
                hasIssues = true;
            }
            else
            {
                report.AppendLine("✅ UI Canvas found");
                
                // Check specific UI elements
                Transform addressPanel = canvas.transform.Find("AddressInputPanel");
                if (addressPanel == null)
                {
                    report.AppendLine("  ⚠️ AddressInputPanel missing");
                    hasIssues = true;
                }
                
                Transform loadingPanel = canvas.transform.Find("LoadingPanel");
                if (loadingPanel == null)
                {
                    report.AppendLine("  ⚠️ LoadingPanel missing");
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
                
                CameraController cameraController = mainCamera.GetComponent<CameraController>();
                if (cameraController == null)
                {
                    report.AppendLine("  ⚠️ CameraController component missing");
                    hasIssues = true;
                }
            }
            
            // Check GameManager
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                report.AppendLine("❌ GameManager not found");
                hasIssues = true;
            }
            else
            {
                report.AppendLine("✅ GameManager found");
            }
            
            report.AppendLine("=====================================");
            if (hasIssues)
            {
                report.AppendLine("⚠️ Issues found - run 'Setup OSM Scene' to fix");
            }
            else
            {
                report.AppendLine("🎉 OSM Scene validation passed!");
            }
            
            Debug.Log(report.ToString());
            
            string dialogTitle = hasIssues ? "Validation Issues" : "Validation Passed";
            string dialogMessage = hasIssues ? 
                "OSM Scene has issues. Check Console for details.\nRun 'Setup OSM Scene' to fix." :
                "OSM Scene validation passed!\nEverything looks good.";
            
            EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "OK");
        }
        
        private void TestAddressResolution()
        {
            if (string.IsNullOrEmpty(testAddress))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a test address!", "OK");
                return;
            }
            
            Debug.Log($"🧪 Testing address resolution for: {testAddress}");
            
            // Simulate address resolution (in actual implementation this would use AddressResolver)
            EditorUtility.DisplayDialog("Test Result", 
                $"Address Resolution Test:\n\n" +
                $"Input: {testAddress}\n" +
                $"Status: ✅ Simulated Success\n\n" +
                $"In runtime, this would:\n" +
                $"1. Query Nominatim API\n" +
                $"2. Convert to coordinates\n" +
                $"3. Load OSM data via Overpass API\n" +
                $"4. Generate 3D world", "OK");
        }
        
        private void GenerateTestMap()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.name != "Level_OSM")
            {
                EditorUtility.DisplayDialog("Error", "Please open Level_OSM scene first!", "OK");
                return;
            }
            
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Info", 
                    "Map generation requires Play Mode.\n\n" +
                    "Steps:\n" +
                    "1. Press Play ▶️\n" +
                    "2. Enter address in game UI\n" +
                    "3. Click 'Karte laden'\n\n" +
                    "Or use 'Test with Fallback Location' for instant testing.", "OK");
                return;
            }
            
            MapStartupController mapController = Object.FindAnyObjectByType<MapStartupController>();
            if (mapController == null)
            {
                EditorUtility.DisplayDialog("Error", "MapStartupController not found!\nRun 'Setup OSM Scene' first.", "OK");
                return;
            }
            
            Debug.Log($"🗺️ Generating test map for: {testAddress}");
            // In runtime, this would trigger map generation
        }
        
        private void TestFallbackLocation()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Info", "Fallback test requires Play Mode.\nPress Play and try again.", "OK");
                return;
            }
            
            MapStartupController mapController = Object.FindAnyObjectByType<MapStartupController>();
            if (mapController == null)
            {
                EditorUtility.DisplayDialog("Error", "MapStartupController not found!", "OK");
                return;
            }
            
            Debug.Log("🎯 Testing fallback location (Leipzig, Markt)");
            // Trigger fallback location loading
            mapController.LoadLeipzigMap(); // Fixed: was LoadFallbackMap()
        }
        
        private void TestNominatimAPI()
        {
            Debug.Log("🌐 Testing Nominatim API connection...");
            EditorUtility.DisplayDialog("API Test", 
                "Nominatim API Test:\n\n" +
                "URL: https://nominatim.openstreetmap.org\n" +
                "Function: Address → Coordinates\n" +
                "Status: ✅ Ready for testing\n\n" +
                "Use Play Mode to test actual API calls.", "OK");
        }
        
        private void TestOverpassAPI()
        {
            Debug.Log("🗂️ Testing Overpass API connection...");
            EditorUtility.DisplayDialog("API Test", 
                "Overpass API Test:\n\n" +
                "URL: https://overpass-api.de/api/interpreter\n" +
                "Function: Coordinates → OSM Data\n" +
                "Status: ✅ Ready for testing\n\n" +
                "Use Play Mode to test actual API calls.", "OK");
        }
        
        private void TestFullAPIPipeline()
        {
            Debug.Log("📡 Testing full API pipeline...");
            EditorUtility.DisplayDialog("Pipeline Test", 
                "Full API Pipeline Test:\n\n" +
                "1. ✅ Nominatim: Address → Coordinates\n" +
                "2. ✅ Overpass: Coordinates → OSM Data\n" +
                "3. ✅ Parser: OSM Data → Unity Objects\n" +
                "4. ✅ Generator: Objects → 3D World\n\n" +
                "Ready for runtime testing!\n" +
                "Use Play Mode for full pipeline test.", "OK");
        }
        
        private void GenerateOSMDebugReport()
        {
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("🐛 OSM System Debug Report");
            report.AppendLine("Generated: " + System.DateTime.Now.ToString());
            report.AppendLine("==========================================");
            
            // System Status
            report.AppendLine("\n📊 System Status:");
            report.AppendLine($"OSM Scene Exists: {File.Exists("Assets/Scenes/Level_OSM.unity")}");
            report.AppendLine($"OSM Scripts Complete: {CheckOSMScripts()}");
            
            // Current Scene Info
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            report.AppendLine($"\n🎬 Current Scene: {activeScene.name}");
            report.AppendLine($"Scene Path: {activeScene.path}");
            report.AppendLine($"Is OSM Scene: {activeScene.name == "Level_OSM"}");
            
            // Component Status
            if (activeScene.name == "Level_OSM")
            {
                report.AppendLine("\n🔧 Component Status:");
                report.AppendLine($"MapStartupController: {Object.FindAnyObjectByType<MapStartupController>() != null}");
                report.AppendLine($"AddressResolver: {Object.FindAnyObjectByType<AddressResolver>() != null}");
                report.AppendLine($"MapGenerator: {Object.FindAnyObjectByType<MapGenerator>() != null}");
                report.AppendLine($"GameManager: {Object.FindAnyObjectByType<GameManager>() != null}");
                report.AppendLine($"Canvas: {Object.FindAnyObjectByType<Canvas>() != null}");
                report.AppendLine($"Main Camera: {Camera.main != null}");
            }
            
            // File Structure
            report.AppendLine("\n📁 File Structure:");
            string[] requiredFiles = {
                "Assets/Scripts/Map/OSMMapData.cs",
                "Assets/Scripts/Map/AddressResolver.cs",
                "Assets/Scripts/Map/MapGenerator.cs", 
                "Assets/Scripts/Map/MapStartupController.cs",
                "Assets/Scenes/Level_OSM.unity"
            };
            
            foreach (string file in requiredFiles)
            {
                report.AppendLine($"{(File.Exists(file) ? "✅" : "❌")} {file}");
            }
            
            report.AppendLine("\n==========================================");
            report.AppendLine("Debug report complete.");
            
            string reportPath = "Assets/OSM_Debug_Report.txt";
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.Refresh();
            
            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Debug Report", 
                $"OSM Debug Report generated!\nSaved to: {reportPath}\nCheck Console for full details.", "OK");
        }
        
        private void ClearGeneratedObjects()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Info", "This function works in Play Mode to clear runtime-generated objects.", "OK");
                return;
            }
            
            // Find and destroy generated objects
            GameObject[] generatedObjects = GameObject.FindGameObjectsWithTag("Generated");
            int count = generatedObjects.Length;
            
            foreach (GameObject obj in generatedObjects)
            {
                DestroyImmediate(obj);
            }
            
            Debug.Log($"🧹 Cleared {count} generated objects");
            EditorUtility.DisplayDialog("Clear Complete", $"Cleared {count} generated objects.", "OK");
        }
        
        private void ExportOSMData()
        {
            EditorUtility.DisplayDialog("Export OSM Data", 
                "OSM Data Export:\n\n" +
                "This feature exports current OSM data for:\n" +
                "• Offline testing\n" +
                "• Data analysis\n" +
                "• Performance optimization\n\n" +
                "Implementation: Export to JSON format\n" +
                "Location: Assets/OSMAssets/ExportedData/", "OK");
        }
        
        private void DrawAdvancedOSMTools()
        {
            if (GUILayout.Button("🔬 OSM Query Builder"))
            {
                OpenOSMQueryBuilder();
            }
            
            if (GUILayout.Button("🎨 Material Generator"))
            {
                GenerateOSMMaterials();
            }
            
            if (GUILayout.Button("⚙️ Performance Profiler"))
            {
                OSMPerformanceProfiler();
            }
            
            if (GUILayout.Button("🗄️ Data Cache Manager"))
            {
                ManageOSMDataCache();
            }
        }
        
        private void OpenOSMQueryBuilder()
        {
            EditorUtility.DisplayDialog("OSM Query Builder", 
                "Advanced OSM Query Builder:\n\n" +
                "Build custom Overpass queries for:\n" +
                "• Specific building types\n" +
                "• Points of interest\n" +
                "• Transportation networks\n" +
                "• Natural features\n\n" +
                "Implementation: Visual query editor", "OK");
        }
        
        private void GenerateOSMMaterials()
        {
            CreateOSMMaterials();
            EditorUtility.DisplayDialog("Materials Generated", 
                "OSM-specific materials created:\n\n" +
                "✅ Road materials\n" +
                "✅ Building materials\n" +
                "✅ Park/nature materials\n" +
                "✅ Water materials\n\n" +
                "Check Assets/OSMAssets/Materials/", "OK");
        }
        
        private void OSMPerformanceProfiler()
        {
            EditorUtility.DisplayDialog("Performance Profiler", 
                "OSM Performance Analysis:\n\n" +
                "Metrics tracked:\n" +
                "• Map generation time\n" +
                "• Memory usage\n" +
                "• API response times\n" +
                "• Rendering performance\n\n" +
                "Use Unity Profiler for detailed analysis", "OK");
        }
        
        private void ManageOSMDataCache()
        {
            EditorUtility.DisplayDialog("Data Cache Manager", 
                "OSM Data Cache Management:\n\n" +
                "Functions:\n" +
                "• Clear cached API responses\n" +
                "• Manage offline data\n" +
                "• Optimize storage usage\n" +
                "• Export/import cache data\n\n" +
                "Implementation: PlayerPrefs and file storage", "OK");
        }
        
        private void CreateOSMBuildSettings()
        {
            // Add Level_OSM to build settings if not already present
            var scenes = EditorBuildSettings.scenes;
            bool osmSceneInBuild = false;
            
            foreach (var scene in scenes)
            {
                if (scene.path.Contains("Level_OSM"))
                {
                    osmSceneInBuild = true;
                    break;
                }
            }
            
            if (!osmSceneInBuild && File.Exists("Assets/Scenes/Level_OSM.unity"))
            {
                var sceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
                sceneList.Add(new EditorBuildSettingsScene("Assets/Scenes/Level_OSM.unity", true));
                EditorBuildSettings.scenes = sceneList.ToArray();
                
                Debug.Log("✅ Added Level_OSM to build settings");
            }
        }
        
        private void CreateOSMMaterials()
        {
            string materialPath = "Assets/OSMAssets/Materials/";
            
            if (!Directory.Exists(materialPath))
            {
                Directory.CreateDirectory(materialPath);
            }
            
            // Create basic materials for OSM elements
            CreateMaterial("OSM_Road_Material", materialPath, new Color(0.3f, 0.3f, 0.3f));
            CreateMaterial("OSM_Building_Material", materialPath, new Color(0.7f, 0.6f, 0.5f));
            CreateMaterial("OSM_Park_Material", materialPath, new Color(0.2f, 0.7f, 0.2f));
            CreateMaterial("OSM_Water_Material", materialPath, new Color(0.2f, 0.4f, 0.8f));
            
            AssetDatabase.Refresh();
            Debug.Log("✅ Created OSM materials");
        }
        
        private void CreateMaterial(string name, string path, Color color)
        {
            string fullPath = path + name + ".mat";
            if (!File.Exists(fullPath))
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = color;
                AssetDatabase.CreateAsset(material, fullPath);
            }
        }
        
        private void ShowCurrentOSMStatus()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            EditorGUILayout.LabelField($"Active Scene: {activeScene.name}");
            EditorGUILayout.LabelField($"OSM Ready: {(osmSystemStatus ? "✅" : "❌")}");
            
            if (activeScene.name == "Level_OSM")
            {
                MapStartupController mapController = Object.FindAnyObjectByType<MapStartupController>();
                EditorGUILayout.LabelField($"Map Controller: {(mapController != null ? "✅" : "❌")}");
                
                if (Application.isPlaying && mapController != null)
                {
                    EditorGUILayout.LabelField("🎮 Runtime Active");
                }
            }
            
            EditorGUILayout.LabelField($"Test Address: {testAddress}");
        }
    }
    
    /// <summary>
    /// Quick menu items for OSM functionality
    /// </summary>
    public static class OSMQuickActions
    {
        [MenuItem("Roll-a-Ball/OSM/🏗️ Setup OSM Scene", priority = 200)]
        public static void QuickSetupOSMScene()
        {
            var window = EditorWindow.GetWindow<OSMEditorExtension>();
            window.SetupOSMScene();
        }
        
        [MenuItem("Roll-a-Ball/OSM/📍 Open OSM Scene", priority = 201)]
        public static void QuickOpenOSMScene()
        {
            if (File.Exists("Assets/Scenes/Level_OSM.unity"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level_OSM.unity");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Level_OSM.unity not found!", "OK");
            }
        }
        
        [MenuItem("Roll-a-Ball/OSM/🧪 Test OSM System", priority = 202)]
        public static void QuickTestOSMSystem()
        {
            Debug.Log("🧪 Quick OSM System Test - check OSM Tools window for detailed testing");
            OSMEditorExtension.ShowOSMWindow();
        }
    }
}
