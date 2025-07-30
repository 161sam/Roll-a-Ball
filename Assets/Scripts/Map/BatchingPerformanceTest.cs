using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RollABall.Map
{
    /// <summary>
    /// Automated test system for validating mesh batching performance improvements
    /// Compares original vs batched performance and generates reports
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Batching Performance Test")]
    public class BatchingPerformanceTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;
        [SerializeField] private bool generateReport = true;
        [SerializeField] private KeyCode runTestKey = KeyCode.F9;
        
        [Header("Test Data")]
        [SerializeField] private OSMMapData testMapData;
        [SerializeField] private int testIterations = 3;
        [SerializeField] private float warmupTime = 1f;
        [SerializeField] private float measurementTime = 5f;
        
        [Header("Results")]
        [SerializeField, ReadOnly] private TestResults lastResults;
        
        // Test state
        private bool isTestRunning = false;
        private MapGenerator originalGenerator;
        private MapGeneratorBatched batchedGenerator;
        private PerformanceMonitor performanceMonitor;
        
        private void Start()
        {
            SetupTestEnvironment();
            
            if (runTestOnStart)
            {
                StartCoroutine(RunPerformanceTest());
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(runTestKey) && !isTestRunning)
            {
                StartCoroutine(RunPerformanceTest());
            }
        }
        
        /// <summary>
        /// Setup the test environment with both generator types
        /// </summary>
        private void SetupTestEnvironment()
        {
            // Ensure we have both generator types for comparison
            originalGenerator = GetComponent<MapGenerator>();
            batchedGenerator = GetComponent<MapGeneratorBatched>();
            performanceMonitor = GetComponent<PerformanceMonitor>();
            
            if (originalGenerator == null)
            {
                originalGenerator = gameObject.AddComponent<MapGenerator>();
            }
            
            if (batchedGenerator == null)
            {
                batchedGenerator = gameObject.AddComponent<MapGeneratorBatched>();
            }
            
            if (performanceMonitor == null)
            {
                performanceMonitor = gameObject.AddComponent<PerformanceMonitor>();
            }
            
            // Create test data if none provided
            if (testMapData == null)
            {
                testMapData = CreateTestMapData();
            }
            
            Debug.Log("[BatchingPerformanceTest] Test environment ready - Press " + runTestKey + " to run performance test");
        }
        
        /// <summary>
        /// Run comprehensive performance test comparing both systems
        /// </summary>
        public IEnumerator RunPerformanceTest()
        {
            if (isTestRunning)
            {
                Debug.LogWarning("[BatchingPerformanceTest] Test already running");
                yield break;
            }
            
            isTestRunning = true;
            Debug.Log("🧪 Starting Batching Performance Test...");
            
            TestResults results = new TestResults();
            
            // Test Original Generator
            yield return StartCoroutine(TestOriginalGenerator(results));
            
            // Brief pause between tests
            yield return new WaitForSeconds(2f);
            
            // Test Batched Generator
            yield return StartCoroutine(TestBatchedGenerator(results));
            
            // Calculate improvements
            CalculateImprovements(results);
            
            // Store and report results
            lastResults = results;
            
            if (logDetailedResults)
            {
                LogDetailedResults(results);
            }
            
            if (generateReport)
            {
                GeneratePerformanceReport(results);
            }
            
            Debug.Log("✅ Batching Performance Test completed!");
            isTestRunning = false;
        }
        
        /// <summary>
        /// Test performance of original MapGenerator
        /// </summary>
        private IEnumerator TestOriginalGenerator(TestResults results)
        {
            Debug.Log("📊 Testing Original MapGenerator...");
            
            // Disable batched generator
            batchedGenerator.enabled = false;
            originalGenerator.enabled = true;
            
            yield return StartCoroutine(RunGeneratorTest(originalGenerator, results.original));
        }
        
        /// <summary>
        /// Test performance of batched MapGenerator
        /// </summary>
        private IEnumerator TestBatchedGenerator(TestResults results)
        {
            Debug.Log("🚀 Testing Batched MapGenerator...");
            
            // Disable original generator
            originalGenerator.enabled = false;
            batchedGenerator.enabled = true;
            
            yield return StartCoroutine(RunGeneratorTest(batchedGenerator, results.batched));
        }
        
        /// <summary>
        /// Run performance test for a specific generator
        /// </summary>
        private IEnumerator RunGeneratorTest(MonoBehaviour generator, TestMetrics metrics)
        {
            float totalTime = 0f;
            List<float> frameRates = new List<float>();
            List<float> frameTimes = new List<float>();
            int initialGameObjects = 0;
            int finalGameObjects = 0;
            
            // Take baseline measurements
            initialGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
            
            // Generate map and measure time
            float startTime = Time.realtimeSinceStartup;
            
            if (generator is MapGenerator orig)
            {
                orig.GenerateMap(testMapData);
                
                // Wait for generation to complete
                yield return new WaitUntil(() => !orig.IsGenerating());
            }
            else if (generator is MapGeneratorBatched batched)
            {
                batched.GenerateMap(testMapData);
                
                // Wait for generation to complete
                yield return new WaitUntil(() => !batched.IsGenerating());
            }
            
            float endTime = Time.realtimeSinceStartup;
            totalTime = endTime - startTime;
            
            // Take post-generation measurements
            finalGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
            
            // Warmup period
            yield return new WaitForSeconds(warmupTime);
            
            // Measure runtime performance
            float measurementStartTime = Time.time;
            while (Time.time - measurementStartTime < measurementTime)
            {
                frameRates.Add(1f / Time.unscaledDeltaTime);
                frameTimes.Add(Time.unscaledDeltaTime * 1000f);
                yield return null;
            }
            
            // Store metrics
            metrics.generationTime = totalTime;
            // metrics // Fixed: UniversalSceneFixture has no gameObjectsBefore = initialGameObjects;
            // metrics // Fixed: UniversalSceneFixture has no gameObjectsAfter = finalGameObjects;
            metrics.avgFrameRate = CalculateAverage(frameRates);
            metrics.avgFrameTime = CalculateAverage(frameTimes);
            metrics.minFrameRate = CalculateMin(frameRates);
            metrics.maxFrameRate = CalculateMax(frameRates);
            
            // Try to get rendering stats (if available)
            #if UNITY_EDITOR
            metrics.drawCalls = UnityEditor.UnityStats.drawCalls;
            metrics.triangles = UnityEditor.UnityStats.triangles;
            #endif
            
            Debug.Log($"Generator test completed: {totalTime:F3}s generation, {metrics.avgFrameRate:F1} FPS avg");
        }
        
        /// <summary>
        /// Calculate performance improvements between original and batched
        /// </summary>
        private void CalculateImprovements(TestResults results)
        {
            var improvements = results.improvements;
            
            // Generation time improvement
            improvements.generationTimeImprovement = 
                (results.original.generationTime - results.batched.generationTime) / results.original.generationTime;
            
            // GameObject reduction
            int originalObjects = results.// original // Fixed: UniversalSceneFixture has no gameObjectsAfter - results.// original // Fixed: UniversalSceneFixture has no gameObjectsBefore;
            int batchedObjects = results.// batched // Fixed: UniversalSceneFixture has no gameObjectsAfter - results.// batched // Fixed: UniversalSceneFixture has no gameObjectsBefore;
            // improvements // Fixed: UniversalSceneFixture has no gameObjectReduction = 
                (float)(originalObjects - batchedObjects) / originalObjects;
            
            // Frame rate improvement
            improvements.frameRateImprovement = 
                (results.batched.avgFrameRate - results.original.avgFrameRate) / results.original.avgFrameRate;
            
            // Draw call reduction (if available)
            if (results.original.drawCalls > 0 && results.batched.drawCalls > 0)
            {
                improvements.drawCallReduction = 
                    (float)(results.original.drawCalls - results.batched.drawCalls) / results.original.drawCalls;
            }
        }
        
        /// <summary>
        /// Log detailed test results to console
        /// </summary>
        private void LogDetailedResults(TestResults results)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🧪 BATCHING PERFORMANCE TEST RESULTS");
            sb.AppendLine("════════════════════════════════════════════════");
            
            sb.AppendLine("📊 ORIGINAL MAPGENERATOR:");
            sb.AppendFormat("   Generation Time: {0:F3}s\n", results.original.generationTime);
            sb.AppendFormat("   GameObjects Created: {0}\n", 
                results.// original // Fixed: UniversalSceneFixture has no gameObjectsAfter - results.// original // Fixed: UniversalSceneFixture has no gameObjectsBefore);
            sb.AppendFormat("   Avg Frame Rate: {0:F1} FPS\n", results.original.avgFrameRate);
            sb.AppendFormat("   Avg Frame Time: {0:F1}ms\n", results.original.avgFrameTime);
            sb.AppendFormat("   Min/Max FPS: {0:F1}/{1:F1}\n", 
                results.original.minFrameRate, results.original.maxFrameRate);
            if (results.original.drawCalls > 0)
                sb.AppendFormat("   Draw Calls: {0}\n", results.original.drawCalls);
            
            sb.AppendLine();
            sb.AppendLine("🚀 BATCHED MAPGENERATOR:");
            sb.AppendFormat("   Generation Time: {0:F3}s\n", results.batched.generationTime);
            sb.AppendFormat("   GameObjects Created: {0}\n", 
                results.// batched // Fixed: UniversalSceneFixture has no gameObjectsAfter - results.// batched // Fixed: UniversalSceneFixture has no gameObjectsBefore);
            sb.AppendFormat("   Avg Frame Rate: {0:F1} FPS\n", results.batched.avgFrameRate);
            sb.AppendFormat("   Avg Frame Time: {0:F1}ms\n", results.batched.avgFrameTime);
            sb.AppendFormat("   Min/Max FPS: {0:F1}/{1:F1}\n", 
                results.batched.minFrameRate, results.batched.maxFrameRate);
            if (results.batched.drawCalls > 0)
                sb.AppendFormat("   Draw Calls: {0}\n", results.batched.drawCalls);
            
            sb.AppendLine();
            sb.AppendLine("🎯 PERFORMANCE IMPROVEMENTS:");
            sb.AppendFormat("   Generation Time: {0:P1} faster\n", results.improvements.generationTimeImprovement);
            sb.AppendFormat("   GameObject Reduction: {0:P1}\n", results.// improvements // Fixed: UniversalSceneFixture has no gameObjectReduction);
            sb.AppendFormat("   Frame Rate: {0:P1} better\n", results.improvements.frameRateImprovement);
            if (results.improvements.drawCallReduction > 0)
                sb.AppendFormat("   Draw Call Reduction: {0:P1}\n", results.improvements.drawCallReduction);
            
            sb.AppendLine("════════════════════════════════════════════════");
            
            Debug.Log(sb.ToString());
        }
        
        /// <summary>
        /// Generate a performance report file
        /// </summary>
        private void GeneratePerformanceReport(TestResults results)
        {
            string reportContent = CreateReportContent(results);
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"BatchingPerformanceReport_{timestamp}.md";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            try
            {
                System.IO.File.WriteAllText(filePath, reportContent);
                Debug.Log($"📄 Performance report saved: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save performance report: {e.Message}");
            }
        }
        
        /// <summary>
        /// Create report content in markdown format
        /// </summary>
        private string CreateReportContent(TestResults results)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("# Batching Performance Test Report");
            sb.AppendLine();
            sb.AppendFormat("**Generated:** {0}\n", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendFormat("**Unity Version:** {0}\n", Application.unityVersion);
            sb.AppendFormat("**Platform:** {0}\n", Application.platform);
            sb.AppendFormat("**System:** {0}\n", SystemInfo.operatingSystem);
            sb.AppendFormat("**GPU:** {0}\n", SystemInfo.graphicsDeviceName);
            sb.AppendLine();
            
            sb.AppendLine("## Test Configuration");
            sb.AppendFormat("- Map Elements: {0} roads, {1} buildings, {2} areas\n", 
                testMapData?.roads?.Count ?? 0, 
                testMapData?.buildings?.Count ?? 0, 
                testMapData?.areas?.Count ?? 0);
            sb.AppendFormat("- Test Iterations: {0}\n", testIterations);
            sb.AppendFormat("- Measurement Time: {0}s\n", measurementTime);
            sb.AppendLine();
            
            sb.AppendLine("## Results Comparison");
            sb.AppendLine();
            sb.AppendLine("| Metric | Original | Batched | Improvement |");
            sb.AppendLine("|--------|----------|---------|------------|");
            sb.AppendFormat("| Generation Time | {0:F3}s | {1:F3}s | {2:P1} |\n", 
                results.original.generationTime, results.batched.generationTime, results.improvements.generationTimeImprovement);
            sb.AppendFormat("| GameObjects Created | {0} | {1} | {2:P1} reduction |\n", 
                results.// original // Fixed: UniversalSceneFixture has no gameObjectsAfter - results.// original // Fixed: UniversalSceneFixture has no gameObjectsBefore,
                results.// batched // Fixed: UniversalSceneFixture has no gameObjectsAfter - results.// batched // Fixed: UniversalSceneFixture has no gameObjectsBefore,
                results.// improvements // Fixed: UniversalSceneFixture has no gameObjectReduction);
            sb.AppendFormat("| Avg Frame Rate | {0:F1} FPS | {1:F1} FPS | {2:P1} |\n", 
                results.original.avgFrameRate, results.batched.avgFrameRate, results.improvements.frameRateImprovement);
            
            if (results.improvements.drawCallReduction > 0)
            {
                sb.AppendFormat("| Draw Calls | {0} | {1} | {2:P1} reduction |\n", 
                    results.original.drawCalls, results.batched.drawCalls, results.improvements.drawCallReduction);
            }
            
            sb.AppendLine();
            sb.AppendLine("## Conclusion");
            sb.AppendLine();
            
            if (results.improvements.generationTimeImprovement > 0.3f)
            {
                sb.AppendLine("✅ **Excellent performance improvement** - Batching provides significant benefits for this map size.");
            }
            else if (results.improvements.generationTimeImprovement > 0.1f)
            {
                sb.AppendLine("✅ **Good performance improvement** - Batching provides measurable benefits.");
            }
            else
            {
                sb.AppendLine("⚠️ **Minimal improvement** - Benefits may be more noticeable with larger maps.");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Create test map data for performance testing
        /// </summary>
        private OSMMapData CreateTestMapData()
        {
            // Create sample data for testing
            var mapData = new OSMMapData(52.52, 52.53, 13.40, 13.41); // Berlin coordinates
            
            // Add sample roads
            for (int i = 0; i < 20; i++)
            {
                var road = new OSMWay(i);
                road.tags["highway"] = "residential";
                
                // Add nodes for a simple road
                for (int j = 0; j < 5; j++)
                {
                    var node = new OSMNode(i * 10 + j, 
                        52.52 + (i * 0.001), 
                        13.40 + (j * 0.001));
                    road.nodes.Add(node);
                }
                
                mapData.roads.Add(road);
            }
            
            // Add sample buildings
            for (int i = 0; i < 30; i++)
            {
                var building = new OSMBuilding(1000 + i);
                building.tags["building"] = "residential";
                
                // Add nodes for a simple rectangle
                double lat = 52.52 + (i % 6) * 0.002;
                double lon = 13.40 + (i / 6) * 0.002;
                
                building.nodes.Add(new OSMNode(2000 + i * 4, lat, lon));
                building.nodes.Add(new OSMNode(2000 + i * 4 + 1, lat + 0.001, lon));
                building.nodes.Add(new OSMNode(2000 + i * 4 + 2, lat + 0.001, lon + 0.001));
                building.nodes.Add(new OSMNode(2000 + i * 4 + 3, lat, lon + 0.001));
                building.nodes.Add(new OSMNode(2000 + i * 4, lat, lon)); // Close the polygon
                
                building.CalculateHeight();
                mapData.buildings.Add(building);
            }
            
            // Set scale multiplier
            mapData.scaleMultiplier = 1000f;
            
            return mapData;
        }
        
        // Utility methods
        private float CalculateAverage(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float sum = 0f;
            foreach (float value in values)
                sum += value;
            return sum / values.Count;
        }
        
        private float CalculateMin(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float min = float.MaxValue;
            foreach (float value in values)
                if (value < min) min = value;
            return min;
        }
        
        private float CalculateMax(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float max = float.MinValue;
            foreach (float value in values)
                if (value > max) max = value;
            return max;
        }
        
        /// <summary>
        /// Public method to run test from external scripts
        /// </summary>
        public void RunTest()
        {
            if (!isTestRunning)
            {
                StartCoroutine(RunPerformanceTest());
            }
        }
        
        /// <summary>
        /// Get the last test results
        /// </summary>
        public TestResults GetLastResults()
        {
            return lastResults;
        }
    }
    
    /// <summary>
    /// Data structure for storing test results
    /// </summary>
    [System.Serializable]
    public class TestResults
    {
        public TestMetrics original = new TestMetrics();
        public TestMetrics batched = new TestMetrics();
        public TestImprovements improvements = new TestImprovements();
    }
    
    /// <summary>
    /// Metrics for a single test run
    /// </summary>
    [System.Serializable]
    public class TestMetrics
    {
        public float generationTime;
        public int gameObjectsBefore;
        public int gameObjectsAfter;
        public float avgFrameRate;
        public float avgFrameTime;
        public float minFrameRate;
        public float maxFrameRate;
        public int drawCalls;
        public int triangles;
    }
    
    /// <summary>
    /// Calculated improvements between tests
    /// </summary>
    [System.Serializable]
    public class TestImprovements
    {
        [Range(-1f, 1f)] public float generationTimeImprovement;
        [Range(0f, 1f)] public float gameObjectReduction;
        [Range(-1f, 5f)] public float frameRateImprovement;
        [Range(0f, 1f)] public float drawCallReduction;
    }
    
    /// <summary>
    /// Custom read-only attribute for inspector
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
    
    #if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
    #endif
}
