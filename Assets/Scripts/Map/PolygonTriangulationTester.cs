using System.Collections.Generic;
using UnityEngine;

namespace RollABall.Map
{
    /// <summary>
    /// Test script for polygon triangulation functionality
    /// Validates the ear clipping algorithm with various polygon shapes
    /// </summary>
    [System.Serializable]
    public class PolygonTriangulationTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool visualizeResults = true;
        [SerializeField] private Material testMaterial;
        
        [Header("Test Results")]
        [SerializeField] private int testsRun = 0;
        [SerializeField] private int testsPassed = 0;
        [SerializeField] private int testsFailed = 0;
        
        private MapGenerator mapGenerator;
        
        private void Start()
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
            
            if (runTestsOnStart)
            {
                RunTriangulationTests();
            }
        }
        
        [ContextMenu("Run Triangulation Tests")]
        public void RunTriangulationTests()
        {
            Debug.Log("[PolygonTester] Starting triangulation tests...");
            
            testsRun = 0;
            testsPassed = 0;
            testsFailed = 0;
            
            // Test simple triangle
            TestTriangle();
            
            // Test simple rectangle
            TestRectangle();
            
            // Test complex polygon (L-shape)
            TestLShape();
            
            // Test concave polygon
            TestConcavePolygon();
            
            // Test degenerate cases
            TestDegenerateCases();
            
            // Report results
            Debug.Log($"[PolygonTester] Tests completed: {testsPassed}/{testsRun} passed, {testsFailed} failed");
        }
        
        private void TestTriangle()
        {
            Debug.Log("[PolygonTester] Testing triangle...");
            
            List<Vector3> trianglePoints = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(2, 0, 0),
                new Vector3(1, 0, 2)
            };
            
            GameObject result = CreateTestMesh(trianglePoints, "Triangle_Test");
            ValidateTest(result, "Triangle", 3);
        }
        
        private void TestRectangle()
        {
            Debug.Log("[PolygonTester] Testing rectangle...");
            
            List<Vector3> rectanglePoints = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(4, 0, 0),
                new Vector3(4, 0, 2),
                new Vector3(0, 0, 2)
            };
            
            GameObject result = CreateTestMesh(rectanglePoints, "Rectangle_Test");
            ValidateTest(result, "Rectangle", 4);
        }
        
        private void TestLShape()
        {
            Debug.Log("[PolygonTester] Testing L-shaped polygon...");
            
            List<Vector3> lShapePoints = new List<Vector3>
            {
                new Vector3(0, 0, 0),   // Bottom-left
                new Vector3(3, 0, 0),   // Bottom-right
                new Vector3(3, 0, 1),   // Inner corner bottom
                new Vector3(1, 0, 1),   // Inner corner left
                new Vector3(1, 0, 3),   // Top-right
                new Vector3(0, 0, 3)    // Top-left
            };
            
            GameObject result = CreateTestMesh(lShapePoints, "LShape_Test");
            ValidateTest(result, "L-Shape", 6);
        }
        
        private void TestConcavePolygon()
        {
            Debug.Log("[PolygonTester] Testing concave polygon...");
            
            List<Vector3> concavePoints = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(2, 0, 0),
                new Vector3(1, 0, 1),   // Concave point
                new Vector3(2, 0, 2),
                new Vector3(0, 0, 2)
            };
            
            GameObject result = CreateTestMesh(concavePoints, "Concave_Test");
            ValidateTest(result, "Concave", 5);
        }
        
        private void TestDegenerateCases()
        {
            Debug.Log("[PolygonTester] Testing degenerate cases...");
            
            // Test with too few points
            List<Vector3> twoPoints = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0)
            };
            
            GameObject result1 = CreateTestMesh(twoPoints, "TwoPoints_Test");
            ValidateTest(result1, "TwoPoints", 2, expectFailure: true);
            
            // Test with duplicate points
            List<Vector3> duplicatePoints = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 0),   // Duplicate
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 1)
            };
            
            GameObject result2 = CreateTestMesh(duplicatePoints, "Duplicates_Test");
            ValidateTest(result2, "Duplicates", 4);
        }
        
        private GameObject CreateTestMesh(List<Vector3> points, string testName)
        {
            try
            {
                if (mapGenerator == null)
                {
                    Debug.LogError("[PolygonTester] MapGenerator not found!");
                    return null;
                }
                
                // Use reflection to access private method for testing
                var method = typeof(MapGenerator).GetMethod("CreateAreaMesh", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method == null)
                {
                    Debug.LogError("[PolygonTester] CreateAreaMesh method not found!");
                    return null;
                }
                
                GameObject result = (GameObject)method.Invoke(mapGenerator, new object[] { points });
                
                if (result != null && visualizeResults)
                {
                    result.name = testName;
                    result.transform.SetParent(transform);
                    
                    // Apply test material if available
                    if (testMaterial != null)
                    {
                        MeshRenderer renderer = result.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            renderer.material = testMaterial;
                        }
                    }
                    
                    // Position tests in a grid
                    result.transform.position = new Vector3(testsRun * 6, 0, 0);
                }
                
                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PolygonTester] Test {testName} failed with exception: {e.Message}");
                return null;
            }
        }
        
        private void ValidateTest(GameObject result, string testName, int pointCount, bool expectFailure = false)
        {
            testsRun++;
            
            if (expectFailure)
            {
                if (result == null)
                {
                    testsPassed++;
                    Debug.Log($"[PolygonTester] ✓ {testName} correctly failed as expected");
                }
                else
                {
                    testsFailed++;
                    Debug.LogWarning($"[PolygonTester] ✗ {testName} should have failed but didn't");
                }
                return;
            }
            
            if (result == null)
            {
                testsFailed++;
                Debug.LogError($"[PolygonTester] ✗ {testName} failed - no result object created");
                return;
            }
            
            MeshFilter meshFilter = result.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                testsFailed++;
                Debug.LogError($"[PolygonTester] ✗ {testName} failed - no mesh generated");
                return;
            }
            
            Mesh mesh = meshFilter.sharedMesh;
            
            // Validate mesh properties
            if (mesh.vertices.Length < 3)
            {
                testsFailed++;
                Debug.LogError($"[PolygonTester] ✗ {testName} failed - insufficient vertices: {mesh.vertices.Length}");
                return;
            }
            
            if (mesh.triangles.Length < 3 || mesh.triangles.Length % 3 != 0)
            {
                testsFailed++;
                Debug.LogError($"[PolygonTester] ✗ {testName} failed - invalid triangle count: {mesh.triangles.Length}");
                return;
            }
            
            int triangleCount = mesh.triangles.Length / 3;
            int expectedTriangles = pointCount - 2; // For any polygon, triangles = vertices - 2
            
            if (triangleCount != expectedTriangles)
            {
                Debug.LogWarning($"[PolygonTester] ? {testName} warning - triangle count {triangleCount} != expected {expectedTriangles}");
            }
            
            testsPassed++;
            Debug.Log($"[PolygonTester] ✓ {testName} passed - {mesh.vertices.Length} vertices, {triangleCount} triangles");
        }
        
        [ContextMenu("Clear Test Results")]
        public void ClearTestResults()
        {
            // Remove all test objects
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }
            
            testsRun = 0;
            testsPassed = 0;
            testsFailed = 0;
            
            Debug.Log("[PolygonTester] Test results cleared");
        }
        
        /// <summary>
        /// Create a test OSM area for validation
        /// </summary>
        [ContextMenu("Test Real OSM Area")]
        public void TestRealOSMArea()
        {
            // Simulate a park area with irregular shape
            List<Vector3> parkPoints = new List<Vector3>
            {
                new Vector3(-2, 0, -2),
                new Vector3(3, 0, -1),
                new Vector3(4, 0, 1),
                new Vector3(2, 0, 3),
                new Vector3(-1, 0, 4),
                new Vector3(-3, 0, 2),
                new Vector3(-2, 0, 0)
            };
            
            GameObject parkTest = CreateTestMesh(parkPoints, "RealOSMPark_Test");
            ValidateTest(parkTest, "Real OSM Park", 7);
        }
    }
}
