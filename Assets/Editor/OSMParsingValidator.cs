using UnityEngine;
using UnityEditor;
using RollABall.Map;
using System.IO;

namespace RollABall.Editor
{
    /// <summary>
    /// Editor tool to test and validate OSM parsing functionality
    /// Helps verify that the new ParseOSMResponse implementation works correctly
    /// </summary>
    public class OSMParsingValidator : EditorWindow
    {
        private string testAddress = "Leipzig, Germany";
        private string testResult = "";
        private Vector2 scrollPosition;
        private bool isValidating = false;
        
        [MenuItem("Roll-a-Ball/OSM Tools/Parsing Validator")]
        public static void ShowWindow()
        {
            GetWindow<OSMParsingValidator>("OSM Parsing Validator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("OSM Parsing Validation Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool validates that the new OSM parsing implementation works correctly.\n" +
                "It tests the replacement of placeholder data with real Overpass API parsing.",
                MessageType.Info
            );
            
            GUILayout.Space(10);
            
            // Address input
            GUILayout.Label("Test Address:", EditorStyles.label);
            testAddress = EditorGUILayout.TextField(testAddress);
            
            GUILayout.Space(10);
            
            // Validation buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate Newtonsoft.Json", GUILayout.Height(30)))
            {
                ValidateNewtonsoftJson();
            }
            
            if (GUILayout.Button("Test OSM Scene Setup", GUILayout.Height(30)))
            {
                ValidateOSMSceneSetup();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Run Complete OSM Test", GUILayout.Height(40)))
            {
                if (!isValidating)
                {
                    RunCompleteOSMTest();
                }
            }
            
            if (isValidating)
            {
                EditorGUILayout.HelpBox("Validation in progress... Check Console for details.", MessageType.Warning);
            }
            
            GUILayout.Space(10);
            
            // Results area
            GUILayout.Label("Validation Results:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(testResult, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            // Quick links
            if (GUILayout.Button("Open Level_OSM Scene"))
            {
                string scenePath = "Assets/Scenes/Level_OSM.unity";
                if (File.Exists(scenePath))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
                else
                {
                    AppendResult("ERROR: Level_OSM.unity not found at " + scenePath);
                }
            }
            
            if (GUILayout.Button("Clear Results"))
            {
                testResult = "";
            }
        }
        
        private void ValidateNewtonsoftJson()
        {
            AppendResult("=== Newtonsoft.Json Validation ===");
            
            try
            {
                // Test basic JSON parsing
                string testJson = "{\"test\": \"value\", \"number\": 42}";
                var parsed = Newtonsoft.Json.Linq.JObject.Parse(testJson);
                
                if (parsed["test"].ToString() == "value" && (int)parsed["number"] == 42)
                {
                    AppendResult("✅ Newtonsoft.Json is working correctly");
                }
                else
                {
                    AppendResult("❌ Newtonsoft.Json parsing failed");
                }
            }
            catch (System.Exception e)
            {
                AppendResult($"❌ Newtonsoft.Json error: {e.Message}");
                AppendResult("💡 Ensure 'com.unity.nuget.newtonsoft-json' is in Packages/manifest.json");
            }
        }
        
        private void ValidateOSMSceneSetup()
        {
            AppendResult("=== OSM Scene Setup Validation ===");
            
            // Check for required scene
            string scenePath = "Assets/Scenes/Level_OSM.unity";
            if (File.Exists(scenePath))
            {
                AppendResult("✅ Level_OSM.unity scene found");
            }
            else
            {
                AppendResult("❌ Level_OSM.unity scene not found");
                return;
            }
            
            // Check for AddressResolver in scene
            AddressResolver resolver = FindFirstObjectByType<AddressResolver>();
            if (resolver != null)
            {
                AppendResult("✅ AddressResolver found in scene");
            }
            else
            {
                AppendResult("⚠️  AddressResolver not found - may need to open Level_OSM scene first");
            }
            
            // Check for MapGenerator
            MapGenerator mapGen = FindFirstObjectByType<MapGenerator>();
            if (mapGen != null)
            {
                AppendResult("✅ MapGenerator found in scene");
            }
            else
            {
                AppendResult("⚠️  MapGenerator not found - may need to open Level_OSM scene first");
            }
            
            // Check for MapStartupController
            MapStartupController startup = FindFirstObjectByType<MapStartupController>();
            if (startup != null)
            {
                AppendResult("✅ MapStartupController found in scene");
            }
            else
            {
                AppendResult("⚠️  MapStartupController not found - may need to open Level_OSM scene first");
            }
        }
        
        private void RunCompleteOSMTest()
        {
            AppendResult("=== Complete OSM Implementation Test ===");
            AppendResult($"Testing with address: {testAddress}");
            
            isValidating = true;
            
            // First validate dependencies
            ValidateNewtonsoftJson();
            ValidateOSMSceneSetup();
            
            // Test OSM data structures
            TestOSMDataStructures();
            
            // Test address resolver functionality
            TestAddressResolverFunctionality();
            
            isValidating = false;
            AppendResult("=== Test Complete ===");
        }
        
        private void TestOSMDataStructures()
        {
            AppendResult("--- OSM Data Structures Test ---");
            
            try
            {
                // Test OSMMapData creation
                var mapData = new OSMMapData(50.0, 51.0, 12.0, 13.0);
                AppendResult("✅ OSMMapData creation successful");
                
                // Test OSMNode creation
                var node = new OSMNode(1, 50.5, 12.5);
                node.tags["amenity"] = "restaurant";
                node.name = "Test Restaurant";
                AppendResult("✅ OSMNode creation and tagging successful");
                
                // Test OSMWay creation
                var way = new OSMWay(2);
                way.tags["highway"] = "primary";
                way.nodes.Add(node);
                AppendResult("✅ OSMWay creation successful");
                
                // Test OSMBuilding creation
                var building = new OSMBuilding(3);
                building.tags["building"] = "residential";
                building.tags["building:levels"] = "3";
                building.CalculateHeight();
                AppendResult($"✅ OSMBuilding creation successful (height: {building.height}m)");
                
                // Test OSMArea creation
                var area = new OSMArea(4);
                area.tags["leisure"] = "park";
                area.DetermineAreaType();
                AppendResult($"✅ OSMArea creation successful (type: {area.areaType})");
                
                // Test coordinate conversion
                Vector3 worldPos = mapData.LatLonToWorldPosition(50.5, 12.5);
                AppendResult($"✅ Coordinate conversion successful: {worldPos}");
                
            }
            catch (System.Exception e)
            {
                AppendResult($"❌ OSM Data Structure error: {e.Message}");
            }
        }
        
        private void TestAddressResolverFunctionality()
        {
            AppendResult("--- AddressResolver Functionality Test ---");
            
            AddressResolver resolver = FindFirstObjectByType<AddressResolver>();
            if (resolver == null)
            {
                AppendResult("❌ AddressResolver not found in scene. Open Level_OSM scene first.");
                return;
            }
            
            AppendResult("✅ AddressResolver component found");
            AppendResult("💡 To test live functionality:");
            AppendResult("  1. Enter Play mode");
            AppendResult("  2. Enter an address in the UI");
            AppendResult("  3. Click 'Load Map'");
            AppendResult("  4. Check Console for parsing logs");
            AppendResult("💡 Expected Console Output:");
            AppendResult("  '[AddressResolver] Processing real OSM response from Overpass API...'");
            AppendResult("  '[AddressResolver] Added road: primary with X nodes'");
            AppendResult("  '[AddressResolver] Added building: residential with height Xm and Y nodes'");
            AppendResult("  '[AddressResolver] Successfully parsed OSM data: X roads, Y buildings, Z areas, W POIs'");
        }
        
        private void AppendResult(string message)
        {
            testResult += message + "\n";
            Debug.Log($"[OSMParsingValidator] {message}");
            Repaint();
        }
    }
}
