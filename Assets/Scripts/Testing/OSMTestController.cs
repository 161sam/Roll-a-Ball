using UnityEngine;
using TMPro;
using UnityEngine.UI;
using RollABall.Map;

namespace RollABall.Testing
{
    /// <summary>
    /// Test script to verify OSM system functionality after repairs
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Testing/OSM Test Controller")]
    public class OSMTestController : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("Test Addresses")]
        [SerializeField] private string[] testAddresses = {
            "Leipzig, Germany",
            "Berlin, Germany",
            "Dresden, Germany"
        };
        
        private MapStartupController startupController;
        private AddressResolver addressResolver;
        private MapGenerator mapGenerator;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunSystemTests());
            }
        }
        
        private System.Collections.IEnumerator RunSystemTests()
        {
            LogTest("=== Starting OSM System Tests ===");
            
            // Wait a frame for everything to initialize
            yield return null;
            
            // Test 1: Component Discovery
            yield return StartCoroutine(TestComponentDiscovery());
            
            // Test 2: UI Element Discovery
            yield return StartCoroutine(TestUIElements());
            
            // Test 3: Position Validation
            yield return StartCoroutine(TestPositions());
            
            // Test 4: Basic Functionality
            yield return StartCoroutine(TestBasicFunctionality());
            
            LogTest("=== OSM System Tests Completed ===");
        }
        
        private System.Collections.IEnumerator TestComponentDiscovery()
        {
            LogTest("Test 1: Component Discovery");
            
            // Find core components
            startupController = FindFirstObjectByType<MapStartupController>();
            addressResolver = FindFirstObjectByType<AddressResolver>();
            mapGenerator = FindFirstObjectByType<MapGenerator>();
            
            // Validate components
            if (startupController != null)
                LogTest("✅ MapStartupController found");
            else
                LogTest("❌ MapStartupController NOT found");
                
            if (addressResolver != null)
                LogTest("✅ AddressResolver found");
            else
                LogTest("❌ AddressResolver NOT found");
                
            if (mapGenerator != null)
                LogTest("✅ MapGenerator found");
            else
                LogTest("❌ MapGenerator NOT found");
            
            // Check GameManager and LevelManager
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            
            if (gameManager != null)
                LogTest("✅ GameManager found");
            else
                LogTest("⚠️ GameManager NOT found (optional but recommended)");
                
            if (levelManager != null)
                LogTest("✅ LevelManager found");
            else
                LogTest("⚠️ LevelManager NOT found (optional but recommended)");
            
            yield return null;
        }
        
        private System.Collections.IEnumerator TestUIElements()
        {
            LogTest("Test 2: UI Element Discovery");
            
            // Find UI elements
            TMP_InputField addressInput = FindFirstObjectByType<TMP_InputField>();
            Button loadButton = GameObject.Find("LoadMapButton")?.GetComponent<Button>();
            TextMeshProUGUI statusText = GameObject.Find("LoadingText")?.GetComponent<TextMeshProUGUI>();
            GameObject loadingPanel = GameObject.Find("LoadingPanel");
            
            if (addressInput != null)
                LogTest($"✅ Address Input Field found: {addressInput.name}");
            else
                LogTest("❌ Address Input Field NOT found");
                
            if (loadButton != null)
                LogTest($"✅ Load Map Button found: {loadButton.name}");
            else
                LogTest("❌ Load Map Button NOT found");
                
            if (statusText != null)
                LogTest($"✅ Status Text found: {statusText.name}");
            else
                LogTest("⚠️ Status Text NOT found");
                
            if (loadingPanel != null)
                LogTest($"✅ Loading Panel found: {loadingPanel.name}");
            else
                LogTest("⚠️ Loading Panel NOT found");
            
            // Test button listeners
            if (loadButton != null)
            {
                int listenerCount = loadButton.onClick.GetPersistentEventCount();
                LogTest($"Load Map Button has {listenerCount} persistent listeners");
                
                if (listenerCount > 0)
                    LogTest("✅ Button has listeners configured");
                else
                    LogTest("⚠️ Button has no persistent listeners (runtime listeners may exist)");
            }
            
            yield return null;
        }
        
        private System.Collections.IEnumerator TestPositions()
        {
            LogTest("Test 3: Position Validation");
            
            // Check key objects for reasonable positions
            GameObject player = GameObject.Find("PlayerPrefab");
            GameObject collectible = GameObject.Find("CollectiblePrefab");
            GameObject goalZone = GameObject.Find("GoalZonePrefab");
            
            if (player != null)
            {
                Vector3 pos = player.transform.position;
                bool reasonable = IsPositionReasonable(pos);
                LogTest($"Player position: {pos} - {(reasonable ? "✅ Reasonable" : "❌ Extreme")}");
            }
            
            if (collectible != null)
            {
                Vector3 pos = collectible.transform.position;
                bool reasonable = IsPositionReasonable(pos);
                LogTest($"Collectible position: {pos} - {(reasonable ? "✅ Reasonable" : "❌ Extreme")}");
                
                // Check if collectible has required components
                CollectibleController controller = collectible.GetComponent<CollectibleController>();
                if (controller != null)
                    LogTest("✅ CollectibleController found on collectible");
                else
                    LogTest("❌ CollectibleController missing on collectible");
            }
            
            if (goalZone != null)
            {
                Vector3 pos = goalZone.transform.position;
                bool reasonable = IsPositionReasonable(pos);
                LogTest($"Goal Zone position: {pos} - {(reasonable ? "✅ Reasonable" : "❌ Extreme")}");
                
                // Check if goal zone has required components
                OSMGoalZoneTrigger trigger = goalZone.GetComponent<OSMGoalZoneTrigger>();
                if (trigger != null)
                    LogTest("✅ OSMGoalZoneTrigger found on goal zone");
                else
                    LogTest("❌ OSMGoalZoneTrigger missing on goal zone");
            }
            
            yield return null;
        }
        
        private System.Collections.IEnumerator TestBasicFunctionality()
        {
            LogTest("Test 4: Basic Functionality");
            
            if (startupController == null)
            {
                LogTest("❌ Cannot test functionality - MapStartupController not found");
                yield break;
            }
            
            // Test default address setting
            TMP_InputField addressInput = FindFirstObjectByType<TMP_InputField>();
            if (addressInput != null)
            {
                string defaultText = addressInput.text;
                LogTest($"Default address in input field: '{defaultText}'");
                
                if (!string.IsNullOrEmpty(defaultText))
                    LogTest("✅ Default address is set");
                else
                    LogTest("⚠️ No default address set");
            }
            
            // Test method availability using reflection
            var startupType = typeof(MapStartupController);
            
            var loadMapMethod = startupType.GetMethod("LoadMapFromInput");
            if (loadMapMethod != null)
                LogTest("✅ LoadMapFromInput method available");
            else
                LogTest("❌ LoadMapFromInput method not found");
                
            var loadAddressMethod = startupType.GetMethod("LoadMapFromAddress");
            if (loadAddressMethod != null)
                LogTest("✅ LoadMapFromAddress method available");
            else
                LogTest("❌ LoadMapFromAddress method not found");
            
            // Test event subscription
            bool hasEventSubscriptions = CheckEventSubscriptions();
            if (hasEventSubscriptions)
                LogTest("✅ Event system appears to be connected");
            else
                LogTest("⚠️ Event subscriptions may not be properly configured");
            
            yield return null;
        }
        
        private bool IsPositionReasonable(Vector3 position)
        {
            // Consider positions reasonable if they're within -100 to +100 in each axis
            // TODO: Replace magic value with configurable boundary constant
            return Mathf.Abs(position.x) <= 100f &&
                   Mathf.Abs(position.y) <= 100f &&
                   Mathf.Abs(position.z) <= 100f;
        }
        
        private bool CheckEventSubscriptions()
        {
            // This is a simplified check - in a real scenario we'd need to inspect the actual event subscriptions
            // For now, we just check if the components exist and assume they're connected properly
            return addressResolver != null && mapGenerator != null && startupController != null;
        }
        
        private void LogTest(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[OSMTest] {message}");
            }
        }
        
        [ContextMenu("Run Manual Test")]
        public void RunManualTest()
        {
            StartCoroutine(RunSystemTests());
        }
        
        [ContextMenu("Test Address Loading")]
        public void TestAddressLoading()
        {
            if (startupController != null && testAddresses.Length > 0)
            {
                string testAddress = testAddresses[Random.Range(0, testAddresses.Length)];
                LogTest($"Testing address loading with: {testAddress}");
                startupController.LoadMapFromAddress(testAddress);
            }
            else
            {
                LogTest("Cannot test address loading - missing components or test addresses");
            }
        }
        
        [ContextMenu("Test Leipzig Map")]
        public void TestLeipzigMap()
        {
            if (startupController != null)
            {
                LogTest("Testing Leipzig map loading...");
                startupController.LoadMapFromAddress("Leipzig, Germany");
            }
            else
            {
                LogTest("Cannot test Leipzig map - MapStartupController not found");
            }
        }
        
        [ContextMenu("Reset All Positions")]
        public void ResetAllPositions()
        {
            LogTest("Resetting all object positions...");
            
            GameObject player = GameObject.Find("PlayerPrefab");
            if (player != null) player.transform.position = new Vector3(0, 1, 0);
            
            GameObject collectible = GameObject.Find("CollectiblePrefab");
            if (collectible != null) collectible.transform.position = new Vector3(3, 1, 3);
            
            GameObject goalZone = GameObject.Find("GoalZonePrefab");
            if (goalZone != null) goalZone.transform.position = new Vector3(0, 0.5f, 8);
            
            LogTest("Position reset completed");
        }
    }
}
