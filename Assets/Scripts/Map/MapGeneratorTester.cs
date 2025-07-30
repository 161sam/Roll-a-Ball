using UnityEngine;
using RollABall.Map;
using System.Collections.Generic;

namespace RollABall.Testing
{
    /// <summary>
    /// Test script for the enhanced segmented road generation system
    /// Tests various road types, materials, and segment generation
    /// </summary>
    [System.Serializable]
    public class MapGeneratorTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool createVisualMarkers = true;
        
        [Header("Test Data")]
        [SerializeField] private MapGenerator mapGenerator;
        
        [Header("Test Results")]
        [SerializeField] private int totalRoadsGenerated = 0;
        [SerializeField] private int totalSegmentsGenerated = 0;
        [SerializeField] private Dictionary<string, int> segmentsByType = new Dictionary<string, int>();
        
        private void Start()
        {
            if (runTestOnStart)
            {
                StartTest();
            }
        }
        
        [ContextMenu("Start Segmented Road Test")]
        public void StartTest()
        {
            if (mapGenerator == null)
            {
                mapGenerator = FindFirstObjectByType<MapGenerator>();
                if (mapGenerator == null)
                {
                    Debug.LogError("[MapGeneratorTester] No MapGenerator found in scene!");
                    return;
                }
            }
            
            Debug.Log("[MapGeneratorTester] Starting segmented road generation test...");
            
            // Create test OSM data
            OSMMapData testData = CreateTestMapData();
            
            // Subscribe to events for monitoring
            SubscribeToEvents();
            
            // Generate test map
            mapGenerator.GenerateMap(testData);
        }
        
        /// <summary>
        /// Create synthetic test data with various road types
        /// </summary>
        // TODO: Move synthetic test data generation to a ScriptableObject for easier tweaking
        private OSMMapData CreateTestMapData()
        {
            Debug.Log("[MapGeneratorTester] Creating test OSM data...");
            
            // Create test bounds (small area around Leipzig for testing)
            OSMMapData testData = new OSMMapData(51.33, 51.35, 12.37, 12.39);
            testData.scaleMultiplier = 1000f; // Scale to make roads visible
            
            // Create test roads of different types
            CreateTestRoad(testData, "motorway", new Vector2[]
            {
                new Vector2(51.330f, 12.370f),
                new Vector2(51.332f, 12.375f),
                new Vector2(51.334f, 12.380f),
                new Vector2(51.336f, 12.385f)
            });
            
            CreateTestRoad(testData, "primary", new Vector2[]
            {
                new Vector2(51.340f, 12.370f),
                new Vector2(51.342f, 12.372f),
                new Vector2(51.344f, 12.374f),
                new Vector2(51.346f, 12.376f),
                new Vector2(51.348f, 12.378f)
            });
            
            CreateTestRoad(testData, "residential", new Vector2[]
            {
                new Vector2(51.335f, 12.375f),
                new Vector2(51.337f, 12.377f),
                new Vector2(51.339f, 12.379f)
            });
            
            CreateTestRoad(testData, "footway", new Vector2[]
            {
                new Vector2(51.332f, 12.380f),
                new Vector2(51.333f, 12.382f),
                new Vector2(51.334f, 12.384f),
                new Vector2(51.335f, 12.386f),
                new Vector2(51.336f, 12.388f),
                new Vector2(51.337f, 12.390f)
            });
            
            // Create a curved road to test segment generation
            CreateCurvedTestRoad(testData, "secondary");
            
            totalRoadsGenerated = testData.roads.Count;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[MapGeneratorTester] Created {totalRoadsGenerated} test roads");
            }
            
            return testData;
        }
        
        /// <summary>
        /// Create a test road with specified type and coordinates
        /// </summary>
        private void CreateTestRoad(OSMMapData mapData, string roadType, Vector2[] coordinates)
        {
            OSMWay road = new OSMWay(mapData.roads.Count + 1000);
            road.tags.Add("highway", roadType);
            road.wayType = "highway";
            
            for (int i = 0; i < coordinates.Length; i++)
            {
                OSMNode node = new OSMNode(i + (mapData.roads.Count * 100), coordinates[i].x, coordinates[i].y);
                road.nodes.Add(node);
            }
            
            mapData.roads.Add(road);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[MapGeneratorTester] Created {roadType} road with {coordinates.Length} nodes");
            }
        }
        
        /// <summary>
        /// Create a curved test road to verify segment generation works with curves
        /// </summary>
        private void CreateCurvedTestRoad(OSMMapData mapData, string roadType)
        {
            List<Vector2> curvePoints = new List<Vector2>();
            
            // Generate a smooth curve with many points
            for (float t = 0; t <= 1; t += 0.1f)
            {
                float lat = 51.340f + (0.01f * Mathf.Sin(t * Mathf.PI * 2));
                float lon = 12.380f + (0.01f * t);
                curvePoints.Add(new Vector2(lat, lon));
            }
            
            CreateTestRoad(mapData, roadType, curvePoints.ToArray());
            
            if (enableDebugLogs)
            {
                Debug.Log($"[MapGeneratorTester] Created curved {roadType} road with {curvePoints.Count} points");
            }
        }
        
        /// <summary>
        /// Subscribe to MapGenerator events for testing feedback
        /// </summary>
        private void SubscribeToEvents()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerationStarted += OnGenerationStarted;
                mapGenerator.OnMapGenerationCompleted += OnGenerationCompleted;
                mapGenerator.OnGenerationError += OnGenerationError;
                mapGenerator.OnPlayerSpawnPositionSet += OnPlayerSpawnSet;
            }
        }
        
        private void OnGenerationStarted(OSMMapData mapData)
        {
            Debug.Log($"[MapGeneratorTester] Map generation started with {mapData.roads.Count} roads");
            
            // Reset counters
            totalSegmentsGenerated = 0;
            segmentsByType.Clear();
        }
        
        private void OnGenerationCompleted()
        {
            Debug.Log("[MapGeneratorTester] Map generation completed!");
            
            // Count generated segments
            CountGeneratedSegments();
            
            // Create visual markers if enabled
            if (createVisualMarkers)
            {
                CreateSegmentMarkers();
            }
            
            // Log test results
            LogTestResults();
        }
        
        private void OnGenerationError(string error)
        {
            Debug.LogError($"[MapGeneratorTester] Generation error: {error}");
        }
        
        private void OnPlayerSpawnSet(Vector3 spawnPosition)
        {
            Debug.Log($"[MapGeneratorTester] Player spawn position set to: {spawnPosition}");
        }
        
        /// <summary>
        /// Count the generated road segments in the scene
        /// </summary>
        private void CountGeneratedSegments()
        {
            GameObject roadContainer = GameObject.Find("GeneratedMap/Roads");
            if (roadContainer != null)
            {
                totalSegmentsGenerated = roadContainer.transform.childCount;
                
                // Count by type
                foreach (Transform child in roadContainer.transform)
                {
                    string segmentName = child.name;
                    if (segmentName.Contains("_"))
                    {
                        string[] parts = segmentName.Split('_');
                        if (parts.Length >= 4)
                        {
                            string roadType = parts[3];
                            if (segmentsByType.ContainsKey(roadType))
                            {
                                segmentsByType[roadType]++;
                            }
                            else
                            {
                                segmentsByType[roadType] = 1;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Create visual markers to highlight segment endpoints
        /// </summary>
        private void CreateSegmentMarkers()
        {
            GameObject markerContainer = new GameObject("SegmentMarkers");
            
            GameObject roadContainer = GameObject.Find("GeneratedMap/Roads");
            if (roadContainer != null)
            {
                foreach (Transform roadSegment in roadContainer.transform)
                {
                    // Create a small sphere at each segment center
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.transform.position = roadSegment.position + Vector3.up * 0.5f;
                    marker.transform.localScale = Vector3.one * 0.2f;
                    marker.transform.SetParent(markerContainer.transform);
                    marker.name = $"Marker_{roadSegment.name}";
                    
                    // Color code by road type
                    MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
                    if (roadSegment.name.Contains("motorway"))
                        renderer.material.color = Color.red;
                    else if (roadSegment.name.Contains("primary"))
                        renderer.material.color = Color.blue;
                    else if (roadSegment.name.Contains("residential"))
                        renderer.material.color = Color.green;
                    else if (roadSegment.name.Contains("footway"))
                        renderer.material.color = Color.yellow;
                    else
                        renderer.material.color = Color.white;
                }
            }
        }
        
        /// <summary>
        /// Log comprehensive test results
        /// </summary>
        private void LogTestResults()
        {
            Debug.Log("=== SEGMENTED ROAD GENERATION TEST RESULTS ===");
            Debug.Log($"Total roads created: {totalRoadsGenerated}");
            Debug.Log($"Total segments generated: {totalSegmentsGenerated}");
            
            foreach (var kvp in segmentsByType)
            {
                Debug.Log($"Road type '{kvp.Key}': {kvp.Value} segments");
            }
            
            // Validation checks
            bool testPassed = true;
            
            if (totalSegmentsGenerated == 0)
            {
                Debug.LogError("TEST FAILED: No road segments were generated!");
                testPassed = false;
            }
            
            if (segmentsByType.Count == 0)
            {
                Debug.LogError("TEST FAILED: No road types were detected!");
                testPassed = false;
            }
            
            // Check if different road types were generated
            string[] expectedTypes = { "motorway", "primary", "residential", "footway", "secondary" };
            foreach (string expectedType in expectedTypes)
            {
                if (!segmentsByType.ContainsKey(expectedType))
                {
                    Debug.LogWarning($"Road type '{expectedType}' was not generated");
                }
            }
            
            if (testPassed)
            {
                Debug.Log("✅ SEGMENTED ROAD GENERATION TEST PASSED!");
            }
            else
            {
                Debug.Log("❌ SEGMENTED ROAD GENERATION TEST FAILED!");
            }
        }
        
        /// <summary>
        /// Manual test for specific road type
        /// </summary>
        [ContextMenu("Test Motorway Generation")]
        public void TestMotorwayGeneration()
        {
            OSMMapData testData = new OSMMapData(51.33, 51.35, 12.37, 12.39);
            testData.scaleMultiplier = 1000f;
            
            CreateTestRoad(testData, "motorway", new Vector2[]
            {
                new Vector2(51.330f, 12.370f),
                new Vector2(51.335f, 12.375f),
                new Vector2(51.340f, 12.380f)
            });
            
            mapGenerator.GenerateMap(testData);
        }
        
        [ContextMenu("Test Footway Generation")]
        public void TestFootwayGeneration()
        {
            OSMMapData testData = new OSMMapData(51.33, 51.35, 12.37, 12.39);
            testData.scaleMultiplier = 1000f;
            
            CreateTestRoad(testData, "footway", new Vector2[]
            {
                new Vector2(51.330f, 12.370f),
                new Vector2(51.331f, 12.371f),
                new Vector2(51.332f, 12.372f),
                new Vector2(51.333f, 12.373f)
            });
            
            mapGenerator.GenerateMap(testData);
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerationStarted -= OnGenerationStarted;
                mapGenerator.OnMapGenerationCompleted -= OnGenerationCompleted;
                mapGenerator.OnGenerationError -= OnGenerationError;
                mapGenerator.OnPlayerSpawnPositionSet -= OnPlayerSpawnSet;
            }
        }
    }
}
