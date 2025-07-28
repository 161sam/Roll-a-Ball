using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using RollABall.Map;

namespace RollABall.Testing
{
    /// <summary>
    /// Live test for Overpass API with improved coordinate validation
    /// </summary>
    public class LiveOverpassAPITester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private string overpassUrl = "https://overpass-api.de/api/interpreter";
        [SerializeField] private double testLat = 51.3397; // Leipzig
        [SerializeField] private double testLon = 12.3731; // Leipzig
        [SerializeField] private float testRadius = 500.0f;
        
        [Header("Results")]
        [SerializeField] private bool lastTestSuccessful = false;
        [SerializeField] private string lastResponse = "";
        
        private void Start()
        {
            Debug.Log("🌍 [LiveOverpassAPITester] Live API Test bereit. Verwende Context Menu 'Test Live API'");
        }
        
        [ContextMenu("Test Live API")]
        public void TestLiveAPI()
        {
            Debug.Log("🚀 [LiveOverpassAPITester] Starting LIVE Overpass API test...");
            StartCoroutine(PerformLiveAPITest());
        }
        
        private IEnumerator PerformLiveAPITest()
        {
            try
            {
                Debug.Log($"📍 Testing coordinates: {testLat:F6}, {testLon:F6} with radius {testRadius}m");
                
                // Step 1: Validate coordinates using our improved validator
                if (!CoordinateValidator.IsValidCoordinate(testLat, testLon))
                {
                    Debug.LogError("❌ Input coordinates are invalid!");
                    yield break;
                }
                
                // Step 2: Calculate safe bounding box
                var bounds = CoordinateValidator.CalculateSafeBoundingBox(testLat, testLon, testRadius);
                
                Debug.Log($"✅ BoundingBox calculated: [{bounds.minLat:F6}, {bounds.minLon:F6}, {bounds.maxLat:F6}, {bounds.maxLon:F6}]");
                
                // Step 3: Build Overpass query
                string query = BuildTestQuery(bounds);
                Debug.Log($"📝 Query generated ({query.Length} characters)");
                
                // Step 4: Execute API call
                yield return StartCoroutine(ExecuteOverpassQuery(query));
                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception during API test: {e.Message}");
                lastTestSuccessful = false;
            }
        }
        
        private string BuildTestQuery(OSMBounds bounds)
        {
            // Build a simple but valid Overpass QL query  
            string bbox = $"{bounds.minLat:F6},{bounds.minLon:F6},{bounds.maxLat:F6},{bounds.maxLon:F6}";
            
            return $@"[out:json][timeout:25];
(
  way[highway]({bbox});
  way[building]({bbox});
);
out geom;";
        }
        
        private IEnumerator ExecuteOverpassQuery(string query)
        {
            Debug.Log("🌐 Executing Overpass API request...");
            
            // Create POST request with form data (proper Overpass format)
            WWWForm form = new WWWForm();
            form.AddField("data", query);
            
            using (UnityWebRequest request = UnityWebRequest.Post(overpassUrl, form))
            {
                // Set proper headers
                request.SetRequestHeader("User-Agent", "RollABallGame/1.0 (Testing)");
                request.timeout = 30; // 30 seconds timeout
                
                // Send request
                var operation = request.SendWebRequest();
                
                // Wait for completion with progress updates
                float startTime = Time.time;
                while (!operation.isDone)
                {
                    float elapsed = Time.time - startTime;
                    if (elapsed > 1.0f && elapsed % 5.0f < 0.1f) // Every 5 seconds
                    {
                        Debug.Log($"⏳ API call in progress... ({elapsed:F1}s elapsed)");
                    }
                    yield return null;
                }
                
                // Process result
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    lastResponse = response;
                    lastTestSuccessful = true;
                    
                    Debug.Log($"✅ SUCCESS! Received {response.Length} characters from Overpass API");
                    
                    // Log response snippet for debugging
                    string snippet = response.Length > 300 ? response.Substring(0, 300) + "..." : response;
                    Debug.Log($"📄 Response snippet:\n{snippet}");
                    
                    // Try to parse and count elements
                    int elementCount = CountElementsInResponse(response);
                    Debug.Log($"📊 Found approximately {elementCount} OSM elements in response");
                    
                    Debug.Log("🎉 LIVE API TEST COMPLETED SUCCESSFULLY!");
                }
                else
                {
                    lastTestSuccessful = false;
                    string error = request.error;
                    string responseText = request.downloadHandler?.text ?? "No response text";
                    
                    Debug.LogError($"❌ API REQUEST FAILED!");
                    Debug.LogError($"   Error: {error}");
                    Debug.LogError($"   Response Code: {request.responseCode}");
                    Debug.LogError($"   Response Text: {responseText}");
                    
                    // Try to extract specific error from response
                    if (responseText.Contains("only allowed values are floats"))
                    {
                        Debug.LogError("🔧 COORDINATE ERROR DETECTED - This is the exact issue we're fixing!");
                    }
                }
            }
        }
        
        private int CountElementsInResponse(string response)
        {
            try
            {
                // Simple element counting by looking for "type" fields
                int count = 0;
                int index = 0;
                while ((index = response.IndexOf("\"type\":", index)) != -1)
                {
                    count++;
                    index += 7;
                }
                return count;
            }
            catch
            {
                return -1; // Unknown count
            }
        }
        
        [ContextMenu("Test Multiple Locations Live")]
        public void TestMultipleLocationsLive()
        {
            Debug.Log("🌍 [LiveOverpassAPITester] Testing multiple locations...");
            StartCoroutine(TestMultipleLocationsCoroutine());
        }
        
        private IEnumerator TestMultipleLocationsCoroutine()
        {
            var locations = new[]
            {
                ("Leipzig", 51.3397, 12.3731),
                ("Berlin", 52.5200, 13.4050),
                ("Hamburg", 53.5511, 9.9937)
            };
            
            int successful = 0;
            
            foreach (var (name, lat, lon) in locations)
            {
                Debug.Log($"📍 Testing {name}...");
                
                // Temporarily set test coordinates
                double originalLat = testLat;
                double originalLon = testLon;
                testLat = lat;
                testLon = lon;
                
                // Run test
                yield return StartCoroutine(PerformLiveAPITest());
                
                if (lastTestSuccessful)
                {
                    successful++;
                    Debug.Log($"✅ {name}: SUCCESS");
                }
                else
                {
                    Debug.LogError($"❌ {name}: FAILED");
                }
                
                // Restore original coordinates
                testLat = originalLat;
                testLon = originalLon;
                
                // Wait between requests to be polite to the API
                yield return new WaitForSeconds(2.0f);
            }
            
            Debug.Log($"🎯 FINAL RESULTS: {successful}/{locations.Length} locations tested successfully");
        }
    }
}
