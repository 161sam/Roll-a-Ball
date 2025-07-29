using UnityEngine;
using RollABall.Map;

namespace RollABall.Testing
{
    /// <summary>
    /// Simple test component for OSM system validation
    /// Attach to any GameObject in Level_OSM scene for testing
    /// </summary>
    public class OSMValidator : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool testOnStart = true;
        [SerializeField] private string testAddress = "Leipzig, Germany";
        
        void Start()
        {
            if (testOnStart)
            {
                ValidateOSMSystem();
            }
        }
        
        [ContextMenu("Validate OSM System")]
        public void ValidateOSMSystem()
        {
            Debug.Log("=== OSM SYSTEM VALIDATION ===");
            
            // Check if required components exist
            AddressResolver resolver = FindFirstObjectByType<AddressResolver>();
            if (resolver == null)
            {
                Debug.LogError("[OSMValidator] AddressResolver not found!");
                return;
            }
            
            MapGenerator generator = FindFirstObjectByType<MapGenerator>();
            if (generator == null)
            {
                Debug.LogError("[OSMValidator] MapGenerator not found!");
                return;
            }
            
            Debug.Log("[OSMValidator] ✓ Core components found");
            
            // Test coordinate validator
            TestCoordinateValidator();
            
            Debug.Log("=== OSM SYSTEM VALIDATION COMPLETE ===");
        }
        
        [ContextMenu("Test Address Loading")]
        public void TestAddressLoading()
        {
            AddressResolver resolver = FindFirstObjectByType<AddressResolver>();
            if (resolver == null)
            {
                Debug.LogError("[OSMValidator] AddressResolver not found!");
                return;
            }
            
            Debug.Log($"[OSMValidator] Testing address: {testAddress}");
            
            // Subscribe to events
            resolver.OnMapDataLoaded += OnMapDataLoaded;
            resolver.OnError += OnError;
            
            // Start loading
            resolver.ResolveAddressAndLoadMap(testAddress);
        }
        
        private void TestCoordinateValidator()
        {
            // Test normal coordinates
            bool valid = CoordinateValidator.IsValidCoordinate(51.3387, 12.3779);
            Debug.Log($"[OSMValidator] Leipzig coordinates valid: {valid}");
            
            // Test invalid coordinates
            bool invalid = CoordinateValidator.IsValidCoordinate(95.0, 0.0);
            Debug.Log($"[OSMValidator] Invalid coordinates rejected: {!invalid}");
            
            // Test bounding box calculation
            try
            {
                OSMBounds bounds = CoordinateValidator.CalculateSafeBoundingBox(51.3387, 12.3779, 500.0f);
                bool boundsValid = CoordinateValidator.ValidateBoundingBox(bounds);
                Debug.Log($"[OSMValidator] ✓ Bounding box calculation successful: {boundsValid}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OSMValidator] ✗ Bounding box calculation failed: {ex.Message}");
            }
        }
        
        private void OnMapDataLoaded(OSMMapData mapData)
        {
            Debug.Log($"[OSMValidator] ✓ SUCCESS! Map loaded: {mapData.roads.Count} roads, {mapData.buildings.Count} buildings, {mapData.areas.Count} areas");
            
            // Unsubscribe
            AddressResolver resolver = FindFirstObjectByType<AddressResolver>();
            if (resolver != null)
            {
                resolver.OnMapDataLoaded -= OnMapDataLoaded;
                resolver.OnError -= OnError;
            }
        }
        
        private void OnError(string error)
        {
            Debug.LogError($"[OSMValidator] ✗ Map loading failed: {error}");
            
            // Unsubscribe
            AddressResolver resolver = FindFirstObjectByType<AddressResolver>();
            if (resolver != null)
            {
                resolver.OnMapDataLoaded -= OnMapDataLoaded;
                resolver.OnError -= OnError;
            }
        }
    }
}
