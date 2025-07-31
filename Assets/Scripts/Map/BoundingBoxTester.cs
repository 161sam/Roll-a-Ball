using System;
using UnityEngine;

namespace RollABall.Map
{
    /// <summary>
    /// Helper script to test bounding box calculations without API calls
    /// </summary>
    public class BoundingBoxTester : MonoBehaviour
    {
        [Header("Test Parameters")]
        [SerializeField] private double testLat = 51.3397;  // Leipzig
        [SerializeField] private double testLon = 12.3731;  // Leipzig
        [SerializeField] private float testRadius = 500.0f; // meters
        [SerializeField] private TestLocation[] customTestLocations;

        [Serializable]
        public class TestLocation
        {
            public string name;
            public double latitude;
            public double longitude;
        }
        
        [Header("Test Results")]
        [SerializeField] private bool lastTestPassed = false;
        [SerializeField] private string lastErrorMessage = "";
        
        [ContextMenu("Test Bounding Box Calculation")]
        public void TestBoundingBoxCalculation()
        {
            Debug.Log($"[BoundingBoxTester] === TESTING BOUNDING BOX CALCULATION ===");
            Debug.Log($"[BoundingBoxTester] Input: lat={testLat:F6}, lon={testLon:F6}, radius={testRadius}m");
            
            try
            {
                // Calculate bounding box with improved method
                var result = CalculateBoundingBox(testLat, testLon, testRadius);
                
                Debug.Log($"[BoundingBoxTester] Result:");
                Debug.Log($"  minLat={result.minLat:F6}, maxLat={result.maxLat:F6}");
                Debug.Log($"  minLon={result.minLon:F6}, maxLon={result.maxLon:F6}");
                Debug.Log($"  latSpan={result.maxLat - result.minLat:F6}°, lonSpan={result.maxLon - result.minLon:F6}°");
                
                // Validate result
                bool isValid = ValidateResult(result, testLat, testLon);
                lastTestPassed = isValid;
                lastErrorMessage = isValid ? "Test passed!" : "Test failed - see console for details";
                
                if (isValid)
                {
                    Debug.Log($"[BoundingBoxTester] ✅ TEST PASSED - Bounding box is valid!");
                    
                    // Show Overpass query format
                    string overpassBbox = $"{result.minLat:F6},{result.minLon:F6},{result.maxLat:F6},{result.maxLon:F6}";
                    Debug.Log($"[BoundingBoxTester] Overpass bbox format: {overpassBbox}");
                }
                else
                {
                    Debug.LogError($"[BoundingBoxTester] ❌ TEST FAILED");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BoundingBoxTester] ❌ EXCEPTION: {e.Message}");
                lastTestPassed = false;
                lastErrorMessage = e.Message;
            }
        }
        
        [ContextMenu("Test Multiple Locations")]
        public void TestMultipleLocations()
        {
            var testLocations = customTestLocations != null && customTestLocations.Length > 0
                ? customTestLocations
                : new[]
                {
                    new TestLocation { name = "Leipzig", latitude = 51.3397, longitude = 12.3731 },
                    new TestLocation { name = "Berlin", latitude = 52.5200, longitude = 13.4050 },
                    new TestLocation { name = "Hamburg", latitude = 53.5511, longitude = 9.9937 },
                    new TestLocation { name = "Munich", latitude = 48.1351, longitude = 11.5820 },
                    new TestLocation { name = "Reykjavik", latitude = 64.1466, longitude = -21.9426 },
                    new TestLocation { name = "Singapore", latitude = 1.3521, longitude = 103.8198 },
                    new TestLocation { name = "Sydney", latitude = -33.8688, longitude = 151.2093 }
                };
            
            Debug.Log($"[BoundingBoxTester] === TESTING MULTIPLE LOCATIONS ===");
            
            int passed = 0;
            int total = testLocations.Length;
            
            foreach (var location in testLocations)
            {
                string name = location.name;
                double lat = location.latitude;
                double lon = location.longitude;
                Debug.Log($"[BoundingBoxTester] Testing {name} ({lat:F4}, {lon:F4})");
                
                try
                {
                    var result = CalculateBoundingBox(lat, lon, 500.0f);
                    bool isValid = ValidateResult(result, lat, lon);
                    
                    if (isValid)
                    {
                        passed++;
                        Debug.Log($"  ✅ {name}: PASSED");
                    }
                    else
                    {
                        Debug.LogError($"  ❌ {name}: FAILED");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"  ❌ {name}: EXCEPTION - {e.Message}");
                }
            }
            
            Debug.Log($"[BoundingBoxTester] === RESULTS: {passed}/{total} tests passed ===");
        }
        
        private (double minLat, double maxLat, double minLon, double maxLon) CalculateBoundingBox(double lat, double lon, float radius)
        {
            var bounds = CoordinateValidator.CalculateSafeBoundingBox(lat, lon, radius);
            return (bounds.minLat, bounds.maxLat, bounds.minLon, bounds.maxLon);
        }
        
        private bool ValidateResult((double minLat, double maxLat, double minLon, double maxLon) result, double centerLat, double centerLon)
        {
            // Check if coordinates are within valid ranges
            if (result.minLat < -90.0 || result.maxLat > 90.0 || result.minLon < -180.0 || result.maxLon > 180.0)
            {
                Debug.LogError($"[BoundingBoxTester] Coordinates out of valid range: lat[{result.minLat:F6}, {result.maxLat:F6}], lon[{result.minLon:F6}, {result.maxLon:F6}]");
                return false;
            }
            
            // Check if bounding box makes sense
            if (result.minLat >= result.maxLat || result.minLon >= result.maxLon)
            {
                Debug.LogError($"[BoundingBoxTester] Invalid bounding box dimensions");
                return false;
            }
            
            // Check if center point is within bounding box
            if (centerLat < result.minLat || centerLat > result.maxLat || centerLon < result.minLon || centerLon > result.maxLon)
            {
                Debug.LogError($"[BoundingBoxTester] Center point outside bounding box");
                return false;
            }
            
            // Check if bounding box is reasonable size
            double latSpan = result.maxLat - result.minLat;
            double lonSpan = result.maxLon - result.minLon;
            
            if (latSpan > 10.0 || lonSpan > 10.0) // 10 degrees = ~1100km
            {
                Debug.LogWarning($"[BoundingBoxTester] Very large bounding box: {latSpan:F3}° lat, {lonSpan:F3}° lon");
            }
            
            return true;
        }
    }
}
