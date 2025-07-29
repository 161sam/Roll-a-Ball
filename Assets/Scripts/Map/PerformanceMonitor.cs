using UnityEngine;
using UnityEngine.Profiling;
using System.Text;

namespace RollABall.Map
{
    /// <summary>
    /// Performance monitoring tool to track the impact of mesh batching
    /// Compares original vs batched generation performance
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Performance Monitor")]
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Monitoring Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool showOnScreen = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        
        [Header("Display Settings")]
        [SerializeField] private Font displayFont;
        [SerializeField] private int fontSize = 14;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
        
        // Performance metrics
        private int frameRate;
        private float frameTime;
        private long totalMemory;
        private long allocatedMemory;
        private int drawCalls;
        private int triangles;
        private int vertices;
        
        // Map generation metrics
        private int totalGameObjects;
        private int batchedObjects;
        private int separateColliders;
        private float lastGenerationTime;
        
        // Display state
        private bool isVisible = true;
        private GUIStyle textStyle;
        private GUIStyle backgroundStyle;
        private StringBuilder displayText = new StringBuilder();
        
        // References
        private MapGeneratorBatched mapGenerator;
        
        private void Start()
        {
            InitializeMonitoring();
            FindMapGenerator();
        }
        
        private void Update()
        {
            if (enableProfiling)
            {
                UpdatePerformanceMetrics();
            }
            
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
            }
        }
        
        private void OnGUI()
        {
            if (!showOnScreen || !isVisible) return;
            
            if (textStyle == null)
            {
                InitializeGUIStyles();
            }
            
            UpdateDisplayText();
            
            // Calculate display area
            Vector2 textSize = textStyle.CalcSize(new GUIContent(displayText.ToString()));
            Rect backgroundRect = new Rect(10, 10, textSize.x + 20, textSize.y + 20);
            Rect textRect = new Rect(20, 20, textSize.x, textSize.y);
            
            // Draw background
            GUI.Box(backgroundRect, "", backgroundStyle);
            
            // Draw text
            GUI.Label(textRect, displayText.ToString(), textStyle);
        }
        
        private void InitializeMonitoring()
        {
            // Enable Unity profiler if available
            #if UNITY_EDITOR
            if (enableProfiling)
            {
                Profiler.enabled = true;
            }
            #endif
            
            Debug.Log("[PerformanceMonitor] Initialized - Press " + toggleKey + " to toggle display");
        }
        
        private void InitializeGUIStyles()
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.font = displayFont ? displayFont : GUI.skin.font;
            textStyle.fontSize = fontSize;
            textStyle.normal.textColor = textColor;
            textStyle.wordWrap = false;
            
            backgroundStyle = new GUIStyle(GUI.skin.box);
            backgroundStyle.normal.background = CreateColorTexture(backgroundColor);
        }
        
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
        
        private void FindMapGenerator()
        {
            mapGenerator = FindFirstObjectByType<MapGeneratorBatched>();
            if (mapGenerator == null)
            {
                Debug.LogWarning("[PerformanceMonitor] No MapGeneratorBatched found in scene");
            }
            else
            {
                // Subscribe to generation events
                mapGenerator.OnMapGenerationStarted += OnMapGenerationStarted;
                mapGenerator.OnMapGenerationCompleted += OnMapGenerationCompleted;
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            // Frame metrics
            frameRate = Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
            frameTime = Time.unscaledDeltaTime * 1000f;
            
            // Memory metrics
            totalMemory = Profiler.GetTotalAllocatedMemoryLong();
            allocatedMemory = Profiler.GetTotalReservedMemoryLong();
            
            // Rendering metrics
            #if UNITY_EDITOR
            drawCalls = UnityStats.drawCalls;
            triangles = UnityStats.triangles;
            vertices = UnityStats.vertices;
            #endif
            
            // Map metrics
            UpdateMapMetrics();
        }
        
        private void UpdateMapMetrics()
        {
            // Count total GameObjects in scene
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            totalGameObjects = allObjects.Length;
            
            // Count batched objects
            batchedObjects = 0;
            separateColliders = 0;
            
            Transform batchedContainer = GameObject.Find("GeneratedMapBatched")?.transform;
            if (batchedContainer != null)
            {
                Transform batchedRoads = batchedContainer.Find("BatchedRoads");
                Transform batchedBuildings = batchedContainer.Find("BatchedBuildings");
                Transform batchedAreas = batchedContainer.Find("BatchedAreas");
                Transform colliders = batchedContainer.Find("Colliders");
                
                if (batchedRoads != null) batchedObjects += batchedRoads.childCount;
                if (batchedBuildings != null) batchedObjects += batchedBuildings.childCount;
                if (batchedAreas != null) batchedObjects += batchedAreas.childCount;
                if (colliders != null) separateColliders = colliders.childCount;
            }
        }
        
        private void UpdateDisplayText()
        {
            displayText.Clear();
            
            displayText.AppendLine("🎮 ROLL-A-BALL PERFORMANCE MONITOR");
            displayText.AppendLine("═══════════════════════════════════");
            
            // Frame Performance
            displayText.AppendLine("📊 FRAME PERFORMANCE:");
            displayText.AppendFormat("   FPS: {0} | Frame Time: {1:F1}ms\n", frameRate, frameTime);
            
            // Memory
            displayText.AppendLine("💾 MEMORY:");
            displayText.AppendFormat("   Total: {0:F1}MB | Reserved: {1:F1}MB\n", 
                totalMemory / (1024f * 1024f), allocatedMemory / (1024f * 1024f));
            
            // Rendering
            displayText.AppendLine("🎨 RENDERING:");
            displayText.AppendFormat("   Draw Calls: {0} | Triangles: {1:N0}\n", drawCalls, triangles);
            displayText.AppendFormat("   Vertices: {0:N0}\n", vertices);
            
            // Map Generation
            displayText.AppendLine("🗺️ MAP GENERATION:");
            displayText.AppendFormat("   Total GameObjects: {0}\n", totalGameObjects);
            displayText.AppendFormat("   Batched Objects: {0}\n", batchedObjects);
            displayText.AppendFormat("   Separate Colliders: {0}\n", separateColliders);
            
            if (lastGenerationTime > 0)
            {
                displayText.AppendFormat("   Last Generation: {0:F1}ms\n", lastGenerationTime);
            }
            
            // Map Data
            if (mapGenerator != null && mapGenerator.GetCurrentMapData() != null)
            {
                displayText.AppendLine("📍 MAP DATA:");
                var mapData = mapGenerator.GetCurrentMapData();
                displayText.AppendFormat("   Roads: {0} | Buildings: {1}\n", 
                    mapData.roads.Count, mapData.buildings.Count);
                displayText.AppendFormat("   Areas: {0} | POIs: {1}\n", 
                    mapData.areas.Count, mapData.pointsOfInterest.Count);
            }
            
            // Performance Rating
            displayText.AppendLine("⭐ PERFORMANCE RATING:");
            string rating = GetPerformanceRating();
            displayText.AppendFormat("   {0}\n", rating);
            
            displayText.AppendLine("═══════════════════════════════════");
            displayText.AppendFormat("Press {0} to toggle | Batching: ON\n", toggleKey);
        }
        
        private string GetPerformanceRating()
        {
            if (frameRate >= 60)
                return "🟢 EXCELLENT (60+ FPS)";
            else if (frameRate >= 45)
                return "🟡 GOOD (45-59 FPS)";
            else if (frameRate >= 30)
                return "🟠 FAIR (30-44 FPS)";
            else
                return "🔴 POOR (<30 FPS)";
        }
        
        private void OnMapGenerationStarted(OSMMapData mapData)
        {
            var startTime = Time.realtimeSinceStartup;
            
            if (logToConsole)
            {
                Debug.Log($"[PerformanceMonitor] Map generation started - Data: {mapData.roads.Count} roads, {mapData.buildings.Count} buildings");
            }
        }
        
        private void OnMapGenerationCompleted()
        {
            lastGenerationTime = Time.realtimeSinceStartup * 1000f; // Convert to ms
            
            if (logToConsole)
            {
                Debug.Log($"[PerformanceMonitor] Map generation completed in {lastGenerationTime:F1}ms");
                LogDetailedStats();
            }
        }
        
        private void LogDetailedStats()
        {
            if (mapGenerator == null) return;
            
            StringBuilder stats = new StringBuilder();
            stats.AppendLine("🎯 DETAILED PERFORMANCE STATS:");
            stats.AppendLine("════════════════════════════════════════");
            
            // Before/After comparison (estimated)
            var mapData = mapGenerator.GetCurrentMapData();
            if (mapData != null)
            {
                int totalRoadSegments = 0;
                foreach (var road in mapData.roads)
                {
                    totalRoadSegments += Mathf.Max(1, road.nodes.Count - 1);
                }
                
                int totalBuildingMeshes = mapData.buildings.Count;
                int totalAreaMeshes = mapData.areas.Count;
                
                int estimatedOriginalObjects = totalRoadSegments + totalBuildingMeshes + totalAreaMeshes;
                int actualBatchedObjects = batchedObjects;
                int drawCallReduction = estimatedOriginalObjects - actualBatchedObjects;
                
                stats.AppendLine("📊 BATCHING IMPACT:");
                stats.AppendFormat("   Original Objects (estimated): {0}\n", estimatedOriginalObjects);
                stats.AppendFormat("   Batched Objects (actual): {0}\n", actualBatchedObjects);
                stats.AppendFormat("   Draw Call Reduction: {0} ({1:P1})\n", 
                    drawCallReduction, (float)drawCallReduction / estimatedOriginalObjects);
                
                stats.AppendLine("🏗️ MESH DATA:");
                stats.AppendFormat("   Road Segments: {0}\n", totalRoadSegments);
                stats.AppendFormat("   Building Meshes: {0}\n", totalBuildingMeshes);
                stats.AppendFormat("   Area Meshes: {0}\n", totalAreaMeshes);
                stats.AppendFormat("   Separate Colliders: {0}\n", separateColliders);
            }
            
            stats.AppendLine("⚡ CURRENT PERFORMANCE:");
            stats.AppendFormat("   FPS: {0} | Frame Time: {1:F1}ms\n", frameRate, frameTime);
            stats.AppendFormat("   Draw Calls: {0} | Triangles: {1:N0}\n", drawCalls, triangles);
            stats.AppendFormat("   Memory: {0:F1}MB\n", totalMemory / (1024f * 1024f));
            
            Debug.Log(stats.ToString());
        }
        
        /// <summary>
        /// Public method to trigger performance snapshot
        /// </summary>
        public void TakePerformanceSnapshot()
        {
            UpdatePerformanceMetrics();
            LogDetailedStats();
        }
        
        /// <summary>
        /// Compare performance before and after batching
        /// </summary>
        public void ComparePerformance(int originalObjects, float originalGenerationTime)
        {
            StringBuilder comparison = new StringBuilder();
            comparison.AppendLine("🔄 PERFORMANCE COMPARISON:");
            comparison.AppendLine("═══════════════════════════════════");
            
            comparison.AppendFormat("Original Objects: {0}\n", originalObjects);
            comparison.AppendFormat("Batched Objects: {0}\n", batchedObjects);
            comparison.AppendFormat("Reduction: {0} ({1:P1})\n", 
                originalObjects - batchedObjects, 
                (float)(originalObjects - batchedObjects) / originalObjects);
            
            comparison.AppendFormat("Original Generation Time: {0:F1}ms\n", originalGenerationTime);
            comparison.AppendFormat("Batched Generation Time: {0:F1}ms\n", lastGenerationTime);
            
            float timeImprovement = (originalGenerationTime - lastGenerationTime) / originalGenerationTime;
            comparison.AppendFormat("Time Improvement: {0:P1}\n", timeImprovement);
            
            Debug.Log(comparison.ToString());
        }
        
        private void OnDestroy()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerationStarted -= OnMapGenerationStarted;
                mapGenerator.OnMapGenerationCompleted -= OnMapGenerationCompleted;
            }
        }
    }
}

/// <summary>
/// Unity Statistics wrapper for accessing rendering stats
/// </summary>
#if UNITY_EDITOR
public static class UnityStats
{
    public static int drawCalls => UnityEditor.UnityStats.drawCalls;
    public static int triangles => UnityEditor.UnityStats.triangles;
    public static int vertices => UnityEditor.UnityStats.vertices;
}
#else
public static class UnityStats
{
    public static int drawCalls => 0; // Not available in builds
    public static int triangles => 0;
    public static int vertices => 0;
}
#endif
