using System;
using UnityEngine;
using RollABall.Map;

namespace RollABall.Testing
{
    /// <summary>
    /// Immediate test runner for OSM coordinate validation
    /// </summary>
    public class OSMCoordinateTestRunner : MonoBehaviour
    {
        // Automatic tests disabled to prevent console spam and API errors
        // Use the Context Menu "Run Coordinate Tests" to manually run tests
        private void Start()
        {
            // Debug.Log("🔥 [OSMCoordinateTestRunner] Starting immediate coordinate validation tests...");
            // RunCoordinateTests(); // Commented out - use Context Menu instead
            Debug.Log("[OSMCoordinateTestRunner] Ready for manual testing via Context Menu");
        }
        
        [ContextMenu("Run Coordinate Tests")]
        public void RunCoordinateTests()
        {
            Debug.Log("=== KOORDINATEN-VALIDIERUNG TESTS ===");
            
            // Test 1: Normal coordinates (Leipzig)
            TestCoordinate("Leipzig", 51.3397, 12.3731, 500.0f);
            
            // Test 2: Northern region (moderate)
            TestCoordinate("Northern Region", 65.0, 120.0, 500.0f);
            
            // Test 3: Near dateline (but reasonable)
            TestCoordinate("Near Dateline", 60.0, 175.0, 500.0f);
            
            // Test 4: Southern hemisphere
            TestCoordinate("Sydney", -33.8688, 151.2093, 500.0f);
            
            // Test 5: Western hemisphere
            TestCoordinate("New York", 40.7128, -74.0060, 500.0f);
            
            Debug.Log("=== TESTS ABGESCHLOSSEN ===");
        }
        
        private void TestCoordinate(string name, double lat, double lon, float radius)
        {
            try
            {
                Debug.Log($"📍 Testing {name}: lat={lat:F4}, lon={lon:F4}, radius={radius}m");
                
                // Test the CoordinateValidator
                if (!CoordinateValidator.IsValidCoordinate(lat, lon))
                {
                    Debug.LogError($"❌ {name}: Invalid input coordinates");
                    return;
                }
                
                // Calculate safe bounding box
                var bounds = CoordinateValidator.CalculateSafeBoundingBox(lat, lon, radius);
                
                Debug.Log($"✅ {name}: BoundingBox calculated successfully");
                Debug.Log($"   minLat={bounds.minLat:F6}, maxLat={bounds.maxLat:F6}");
                Debug.Log($"   minLon={bounds.minLon:F6}, maxLon={bounds.maxLon:F6}");
                
                // Validate the bounding box
                if (CoordinateValidator.ValidateBoundingBox(bounds))
                {
                    Debug.Log($"✅ {name}: BoundingBox validation PASSED");
                    
                    // Show Overpass format
                    string overpassFormat = $"{bounds.minLat:F6},{bounds.minLon:F6},{bounds.maxLat:F6},{bounds.maxLon:F6}";
                    Debug.Log($"   Overpass format: {overpassFormat}");
                }
                else
                {
                    Debug.LogError($"❌ {name}: BoundingBox validation FAILED");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ {name}: EXCEPTION - {e.Message}");
            }
        }
        
        [ContextMenu("Test Overpass Query Format")]
        public void TestOverpassQueryFormat()
        {
            Debug.Log("=== OVERPASS QUERY FORMAT TEST ===");
            
            try
            {
                // Leipzig coordinates
                double lat = 51.3397;
                double lon = 12.3731;
                float radius = 500.0f;
                
                var bounds = CoordinateValidator.CalculateSafeBoundingBox(lat, lon, radius);
                
                // Create a simple Overpass query
                string query = $@"[out:json][timeout:25];
(
  way[highway]({bounds.minLat:F6},{bounds.minLon:F6},{bounds.maxLat:F6},{bounds.maxLon:F6});
  way[building]({bounds.minLat:F6},{bounds.minLon:F6},{bounds.maxLat:F6},{bounds.maxLon:F6});
);
out geom;";
                
                Debug.Log("✅ Sample Overpass Query:");
                Debug.Log(query);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Query generation failed: {e.Message}");
            }
        }
    }
}