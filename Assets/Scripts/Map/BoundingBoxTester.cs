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
            // TODO: Expose test locations via inspector to allow custom cases
            var testLocations = new[]
            {
                ("Leipzig", 51.3397, 12.3731),
                ("Berlin", 52.5200, 13.4050),
                ("Hamburg", 53.5511, 9.9937),
                ("Munich", 48.1351, 11.5820),
                ("Reykjavik", 64.1466, -21.9426), // High latitude test
                ("Singapore", 1.3521, 103.8198), // Near equator
                ("Sydney", -33.8688, 151.2093)   // Southern hemisphere
            };
            
            Debug.Log($"[BoundingBoxTester] === TESTING MULTIPLE LOCATIONS ===");
            
            int passed = 0;
            int total = testLocations.Length;
            
            foreach (var (name, lat, lon) in testLocations)
            {
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
        
        // TODO: Move bounding box calculation to a shared utility class for reuse
        private (double minLat, double maxLat, double minLon, double maxLon) CalculateBoundingBox(double lat, double lon, float radius)
        {
            // Same algorithm as in AddressResolver (improved version)
            double radiusInDegrees = radius / 111320.0; // meters to degrees (at equator)
            double latRadius = radiusInDegrees;
            
            // Correct longitude calculation based on latitude (cosine correction)
            double latRadians = lat * Math.PI / 180.0;
            double cosLat = Math.Cos(latRadians);
            double lonRadius = cosLat > 0.001 ? radiusInDegrees / cosLat : radiusInDegrees; // Avoid division by zero
            
            double minLat = lat - latRadius;
            double maxLat = lat + latRadius;
            double minLon = lon - lonRadius;
            double maxLon = lon + lonRadius;
            
            // STRICT bounds checking with proper limits
            minLat = Math.Max(minLat, -89.9);
            maxLat = Math.Min(maxLat, 89.9);
            minLon = Math.Max(minLon, -179.9);
            maxLon = Math.Min(maxLon, 179.9);
            
            // Additional safety: ensure bounding box doesn't wrap around
            if (maxLon - minLon > 180.0)
            {
                Debug.LogWarning($"[BoundingBoxTester] Bounding box too wide, reducing radius");
                double safeRadius = 90.0 * cosLat; // Max safe radius in degrees
                lonRadius = Math.Min(lonRadius, safeRadius);
                minLon = lon - lonRadius;
                maxLon = lon + lonRadius;
                minLon = Math.Max(minLon, -179.9);
                maxLon = Math.Min(maxLon, 179.9);
            }
            
            return (minLat, maxLat, minLon, maxLon);
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
