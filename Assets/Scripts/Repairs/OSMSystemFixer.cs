using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RollABall.Map;

namespace RollABall.Repairs
{
    /// <summary>
    /// Repairs the OSM system UI connections and resolves coordinate issues
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Repairs/OSM System Fixer")]
    public class OSMSystemFixer : MonoBehaviour
    {
        [Header("Fix Settings")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private bool debugLogging = true;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                FixOSMSystem();
            }
        }
        
        [ContextMenu("Fix OSM System")]
        public void FixOSMSystem()
        {
            LogDebug("Starting OSM System repair...");
            
            // 1. Fix UI connections
            FixUIConnections();
            
            // 2. Fix coordinate positions
            FixCoordinatePositions();
            
            // 3. Setup MapStartupController properly
            SetupMapStartupController();
            
            // 4. Ensure prefab references
            EnsurePrefabReferences();
            
            LogDebug("OSM System repair completed!");
        }
        
        private void FixUIConnections()
        {
            LogDebug("Fixing UI connections...");
            
            // Find components
            // TODO: Inject MapStartupController reference instead of using FindFirstObjectByType
            MapStartupController startupController = FindFirstObjectByType<MapStartupController>();
            if (startupController == null)
            {
                LogDebug("ERROR: MapStartupController not found!");
                return;
            }
            
            // Find UI elements
            TMP_InputField addressInput = FindUIElement<TMP_InputField>("AddressInputField");
            Button loadMapButton = FindUIElement<Button>("LoadMapButton");
            TextMeshProUGUI statusText = FindUIElement<TextMeshProUGUI>("LoadingText");
            GameObject loadingPanel = FindUIGameObject("LoadingPanel");
            
            if (addressInput == null || loadMapButton == null)
            {
                LogDebug("ERROR: Essential UI elements not found!");
                return;
            }
            
            // Use reflection to set private fields in MapStartupController
            // TODO: Expose these fields or provide public methods to remove reflection
            var startupControllerType = typeof(MapStartupController);
            
            // Set addressInputField
            var addressInputField = startupControllerType.GetField("addressInputField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (addressInputField != null)
            {
                addressInputField.SetValue(startupController, addressInput);
                LogDebug("Set addressInputField reference");
            }
            
            // Set loadMapButton
            var loadMapButtonField = startupControllerType.GetField("loadMapButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (loadMapButtonField != null)
            {
                loadMapButtonField.SetValue(startupController, loadMapButton);
                LogDebug("Set loadMapButton reference");
            }
            
            // Set statusText
            var statusTextField = startupControllerType.GetField("statusText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (statusTextField != null)
            {
                statusTextField.SetValue(startupController, statusText);
                LogDebug("Set statusText reference");
            }
            
            // Set loadingPanel
            var loadingPanelField = startupControllerType.GetField("loadingPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (loadingPanelField != null)
            {
                loadingPanelField.SetValue(startupController, loadingPanel);
                LogDebug("Set loadingPanel reference");
            }
            
            // Clear and set button listeners
            loadMapButton.onClick.RemoveAllListeners();
            loadMapButton.onClick.AddListener(() => {
                LogDebug("LoadMapButton clicked!");
                startupController.LoadMapFromInput();
            });
            
            // Setup input field listener
            addressInput.onEndEdit.RemoveAllListeners();
            addressInput.onEndEdit.AddListener((string value) => {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    LogDebug("Enter pressed in address input!");
                    startupController.LoadMapFromInput();
                }
            });
            
            LogDebug("UI connections fixed successfully!");
        }
        
        private void FixCoordinatePositions()
        {
            LogDebug("Fixing extreme coordinate positions...");
            
            // Find objects with extreme positions and reset them
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (GameObject obj in allObjects)
            {
                Vector3 pos = obj.transform.position;
                
                // Check for extreme positions (likely coordinate conversion errors)
                if (Mathf.Abs(pos.x) > 1000 || Mathf.Abs(pos.y) > 1000 || Mathf.Abs(pos.z) > 1000)
                {
                    // Reset to reasonable position
                    Vector3 newPos = Vector3.zero;
                    
                    // Adjust based on object type
                    if (obj.name.Contains("Player"))
                    {
                        newPos = new Vector3(0, 1, 0);
                    }
                    else if (obj.name.Contains("Collectible"))
                    {
                        newPos = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
                    }
                    else if (obj.name.Contains("GoalZone"))
                    {
                        newPos = new Vector3(0, 0.5f, 8);
                    }
                    
                    obj.transform.position = newPos;
                    LogDebug($"Reset position of {obj.name} from {pos} to {newPos}");
                }
            }
        }
        
        private void SetupMapStartupController()
        {
            LogDebug("Setting up MapStartupController...");
            
            MapStartupController startupController = FindFirstObjectByType<MapStartupController>();
            AddressResolver addressResolver = FindFirstObjectByType<AddressResolver>();
            MapGenerator mapGenerator = FindFirstObjectByType<MapGenerator>();
            
            if (startupController == null || addressResolver == null || mapGenerator == null)
            {
                LogDebug("ERROR: Missing core components!");
                return;
            }
            
            // Subscribe to AddressResolver events
            addressResolver.OnMapDataLoaded += (mapData) => {
                LogDebug("AddressResolver: Map data loaded, starting generation...");
                mapGenerator.GenerateMap(mapData);
            };
            
            addressResolver.OnError += (error) => {
                LogDebug($"AddressResolver Error: {error}");
            };
            
            // Subscribe to MapGenerator events  
            mapGenerator.OnMapGenerationCompleted += () => {
                LogDebug("MapGenerator: Generation completed!");
                
                // Start the game if GameManager exists
                GameManager gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.StartGame();
                }
            };
            
            mapGenerator.OnGenerationError += (error) => {
                LogDebug($"MapGenerator Error: {error}");
            };
            
            LogDebug("MapStartupController setup completed!");
        }
        
        private void EnsurePrefabReferences()
        {
            LogDebug("Ensuring prefab references...");
            
            MapGenerator mapGenerator = FindFirstObjectByType<MapGenerator>();
            if (mapGenerator == null)
            {
                LogDebug("ERROR: MapGenerator not found!");
                return;
            }
            
            // Check if prefabs are assigned using reflection
            // TODO: Refactor to use serialized fields instead of reflection
            var mapGeneratorType = typeof(MapGenerator);
            
            // List of prefab fields to check
            string[] prefabFields = {
                "collectiblePrefab", "goalZonePrefab", "playerPrefab",
                "roadPrefab", "buildingPrefab", "areaPrefab"
            };
            
            foreach (string fieldName in prefabFields)
            {
                var field = mapGeneratorType.GetField(fieldName, 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (field != null)
                {
                    GameObject prefab = field.GetValue(mapGenerator) as GameObject;
                    if (prefab == null)
                    {
                        // Try to find and assign from scene
                        GameObject scenePrefab = FindUIGameObject(fieldName.Replace("Prefab", ""));
                        if (scenePrefab != null)
                        {
                            field.SetValue(mapGenerator, scenePrefab);
                            LogDebug($"Assigned {fieldName} from scene object");
                        }
                        else
                        {
                            LogDebug($"WARNING: {fieldName} not assigned and no scene fallback found");
                        }
                    }
                }
            }
        }
        
        private T FindUIElement<T>(string name) where T : Component
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                return obj.GetComponent<T>();
            }
            
            // Try finding by component type
            T[] components = FindObjectsByType<T>(FindObjectsSortMode.None);
            if (components.Length > 0)
            {
                LogDebug($"Found {typeof(T).Name} component, but not by name. Using first found.");
                return components[0];
            }
            
            LogDebug($"ERROR: Could not find UI element: {name} of type {typeof(T).Name}");
            return null;
        }
        
        private GameObject FindUIGameObject(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                LogDebug($"WARNING: Could not find GameObject: {name}");
            }
            return obj;
        }
        
        private void LogDebug(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"[OSMSystemFixer] {message}");
            }
        }
        
        [ContextMenu("Test Address Input")]
        public void TestAddressInput()
        {
            MapStartupController startupController = FindFirstObjectByType<MapStartupController>();
            if (startupController != null)
            {
                startupController.LoadMapFromAddress("Leipzig, Germany");
                LogDebug("Test address loading initiated");
            }
        }
        
        [ContextMenu("Reset All Positions")]
        public void ResetAllPositions()
        {
            FixCoordinatePositions();
        }
    }
}
