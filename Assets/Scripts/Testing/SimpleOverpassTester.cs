using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using RollABall.Map;

namespace RollABall.Testing
{
    /// <summary>
    /// Simple live test for Overpass API
    /// </summary>
    public class SimpleOverpassTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        public double testLat = 51.3397; // Leipzig
        public double testLon = 12.3731; // Leipzig
        public float testRadius = 500.0f;
        
        [Header("Results")]
        public bool lastTestSuccessful = false;
        
        void Start()
        {
            Debug.Log("🌍 [SimpleOverpassTester] Ready for testing. Use Context Menu!");
        }
        
        [ContextMenu("Test API")]
        public void TestAPI()
        {
            Debug.Log("🚀 Starting simple API test...");
            StartCoroutine(RunAPITest());
        }
        
        IEnumerator RunAPITest()
        {
            // Validate coordinates
            if (!CoordinateValidator.IsValidCoordinate(testLat, testLon))
            {
                Debug.LogError("❌ Invalid coordinates!");
                lastTestSuccessful = false;
                yield break;
            }
            
            // Calculate bounding box
            OSMBounds bounds;
            try
            {
                bounds = CoordinateValidator.CalculateSafeBoundingBox(testLat, testLon, testRadius);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Bounding box error: {e.Message}");
                lastTestSuccessful = false;
                yield break;
            }
            
            Debug.Log($"✅ BoundingBox: [{bounds.minLat:F6}, {bounds.minLon:F6}, {bounds.maxLat:F6}, {bounds.maxLon:F6}]");
            
            // Build query
            string bbox = $"{bounds.minLat:F6},{bounds.minLon:F6},{bounds.maxLat:F6},{bounds.maxLon:F6}";
            string query = $@"[out:json][timeout:25];
(
  way[highway]({bbox});
  way[building]({bbox});
);
out geom;";
            
            Debug.Log($"📝 Query ready ({query.Length} chars)");
            
            // Execute API call
            yield return ExecuteQuery(query);
        }
        
        IEnumerator ExecuteQuery(string query)
        {
            string url = "https://overpass-api.de/api/interpreter";
            
            WWWForm form = new WWWForm();
            form.AddField("data", query);
            
            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.SetRequestHeader("User-Agent", "RollABallGame/1.0");
                request.timeout = 30;
                
                Debug.Log("🌐 Sending request to Overpass API...");
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    lastTestSuccessful = true;
                    
                    Debug.Log($"✅ SUCCESS! Received {response.Length} characters");
                    
                    // Count elements
                    int count = CountElements(response);
                    Debug.Log($"📊 Found ~{count} OSM elements");
                    
                    // Show snippet
                    string snippet = response.Length > 200 ? response.Substring(0, 200) + "..." : response;
                    Debug.Log($"📄 Response preview:\n{snippet}");
                    
                    Debug.Log("🎉 LIVE API TEST SUCCESS!");
                }
                else
                {
                    lastTestSuccessful = false;
                    Debug.LogError($"❌ API FAILED: {request.error}");
                    Debug.LogError($"Response Code: {request.responseCode}");
                    
                    if (!string.IsNullOrEmpty(request.downloadHandler.text))
                    {
                        Debug.LogError($"Error Response: {request.downloadHandler.text}");
                    }
                }
            }
        }
        
        int CountElements(string response)
        {
            int count = 0;
            int index = 0;
            while ((index = response.IndexOf("\"type\":", index)) != -1)
            {
                count++;
                index += 7;
            }
            return count;
        }
    }
}
