using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Globalization;
using Newtonsoft.Json.Linq;
using RollABall.Map;

namespace RollABall.Map
{
    /// <summary>
    /// Enhanced MapStartupController that integrates with real OSM data
    /// Handles OpenStreetMap integration and startup for Level_OSM scene
    /// Provides address input, map data loading, and fallback mechanisms
    /// Combines real OSM integration with simulation/testing capabilities
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Map/Map Startup Controller")]
    public class MapStartupController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField addressInputField;
        [SerializeField] private Button loadMapButton;
        [SerializeField] private Button useCurrentLocationButton;
        [SerializeField] private Button regenerateMapButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingPanel;
        
        [Header("Map Settings")]
        [SerializeField] private string defaultAddress = "Leipzig, Germany";
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private float searchRadius = 500f;
        
        [Header("Endless Mode")]
        [SerializeField] private AddressList endlessAddressList;
        // TODO: Load endless mode addresses from AddressList asset
        private string[] endlessModeAddresses = {
            "Leipzig, Germany",
            "Berlin, Germany",
            "Munich, Germany",
            "Hamburg, Germany",
            "Dresden, Germany",
            "Cologne, Germany",
            "Frankfurt, Germany",
            "Stuttgart, Germany"
        };
        
        [Header("Fallback System")]
        [SerializeField] private GameObject fallbackLevelPrefab;
        [SerializeField] private bool useLeipzigFallback = true;
        [SerializeField] private bool enableSimulationMode = false; // For testing without real OSM
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;
        
        // Leipzig coordinates (fallback)
        [SerializeField] private Vector2 leipzigCoords = new Vector2(51.3387f, 12.3799f);
        
        // Component references
        private AddressResolver addressResolver;
        private MapGenerator mapGenerator;
        [SerializeField] private LevelGenerator levelGenerator;
        private GameManager gameManager;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private PlayerController player;
        
        // State
        private bool isLoading = false;
        private int currentRetries = 0;
        private string currentAddress = "";
        
        // Events
        public System.Action<bool> OnMapLoadCompleted; // success
        public System.Action<string> OnStatusUpdate; // status message
        
        private void Awake()
        {
            // Get component references
            addressResolver = GetComponent<AddressResolver>();
            mapGenerator = GetComponent<MapGenerator>();

            if (!gameManager)
                gameManager = FindFirstObjectByType<GameManager>();

            if (!levelManager)
                levelManager = FindFirstObjectByType<LevelManager>();

            if (!levelGenerator)
                levelGenerator = FindFirstObjectByType<LevelGenerator>();

            if (!player)
                player = FindFirstObjectByType<PlayerController>();

            // Load endless mode addresses from configuration
            if (endlessAddressList == null)
            {
                endlessAddressList = Resources.Load<AddressList>("AddressLists/EndlessAddresses");
            }
            if (endlessAddressList != null)
            {
                endlessModeAddresses = endlessAddressList.addresses;
            }
            
            // Check for real OSM components
            if (addressResolver == null && !enableSimulationMode)
            {
                LogDebug("WARNING: AddressResolver component not found! Enabling simulation mode as fallback.");
                enableSimulationMode = true;
            }
            
            if (mapGenerator == null && !enableSimulationMode)
            {
                LogDebug("WARNING: MapGenerator component not found! Enabling simulation mode as fallback.");
                enableSimulationMode = true;
            }
        }
        
        private void Start()
        {
            InitializeUI();
            
            // Only setup OSM event subscriptions if real components exist
            if (!enableSimulationMode)
            {
                SetupEventSubscriptions();
            }
            
            // Check if we're in auto-generate mode (endless mode)
            if (PlayerPrefs.GetInt("AutoGenerateOSMMode", 0) == 1)
            {
                StartAutoGenerateMode();
            }
            else
            {
                // Normal manual mode
                if (addressInputField)
                    addressInputField.text = defaultAddress;
                
                UpdateStatus("Ready to load map. Enter address and click Load Map.");
            }
        }
        
        private void InitializeUI()
        {
            LogDebug("Initializing UI components...");

            // Auto-find UI components if not assigned using shared utility
            if (!addressInputField)
                addressInputField = Utility.SceneObjectUtils.FindOrCreateComponent<TMP_InputField>("AddressInputField");

            if (!loadMapButton)
                loadMapButton = Utility.SceneObjectUtils.FindOrCreateComponent<Button>("LoadMapButton");

            if (!statusText)
                statusText = Utility.SceneObjectUtils.FindOrCreateComponent<TextMeshProUGUI>("StatusText");

            if (!loadingPanel)
                loadingPanel = Utility.SceneObjectUtils.FindOrCreateComponent<RectTransform>("LoadingPanel").gameObject;
            
            // Setup button listeners
            if (loadMapButton)
            {
                loadMapButton.onClick.RemoveAllListeners();
                loadMapButton.onClick.AddListener(LoadMapFromInput);
                LogDebug("LoadMapButton listener added");
            }
            
            if (useCurrentLocationButton)
            {
                useCurrentLocationButton.onClick.RemoveAllListeners();
                useCurrentLocationButton.onClick.AddListener(UseCurrentLocation);
                LogDebug("UseCurrentLocationButton listener added");
            }
            
            if (regenerateMapButton)
            {
                regenerateMapButton.onClick.RemoveAllListeners();
                regenerateMapButton.onClick.AddListener(RegenerateCurrentMap);
                LogDebug("RegenerateMapButton listener added");
            }
            
            // Setup input field listener
            if (addressInputField)
            {
                addressInputField.onEndEdit.RemoveAllListeners();
                addressInputField.onEndEdit.AddListener(OnAddressInputEnd);
                LogDebug("AddressInputField listener added");
            }
            
            LogDebug("UI initialization completed");
        }
        
        private void SetupEventSubscriptions()
        {
            LogDebug("Setting up event subscriptions...");
            
            if (addressResolver != null)
            {
                // Subscribe to AddressResolver events
                addressResolver.OnMapDataLoaded += OnMapDataLoaded;
                addressResolver.OnError += OnAddressResolverError;
                addressResolver.OnMapLoadErrorEvent += OnMapLoadError;
                LogDebug("AddressResolver events subscribed");
            }
            
            if (mapGenerator != null)
            {
                // Subscribe to MapGenerator events
                mapGenerator.OnMapGenerationStarted += OnMapGenerationStarted;
                mapGenerator.OnMapGenerationCompleted += OnMapGenerationCompleted;
                mapGenerator.OnGenerationError += OnMapGenerationError;
                LogDebug("MapGenerator events subscribed");
            }
        }
        
        public void LoadMapFromInput()
        {
            if (isLoading)
            {
                LogDebug("Already loading, ignoring request");
                return;
            }
            
            string address = addressInputField ? addressInputField.text.Trim() : defaultAddress;
            
            if (string.IsNullOrEmpty(address))
            {
                UpdateStatus("Please enter a valid address.");
                return;
            }
            
            LoadMapFromAddress(address);
        }
        
        public void LoadMapFromAddress(string address)
        {
            if (isLoading)
            {
                LogDebug("Already loading, ignoring request");
                return;
            }

            ClearExistingLevel();

            currentAddress = address;
            currentRetries = 0;
            
            LogDebug($"Loading map for address: {address}");
            UpdateStatus($"Loading map for: {address}");
            SetLoadingState(true);
            
            // Use real OSM system or simulation based on mode
            if (enableSimulationMode)
            {
                StartCoroutine(LoadMapCoroutineSimulation(address));
            }
            else
            {
                // Use real AddressResolver
                if (addressResolver != null)
                {
                    // TODO: Execute address resolution asynchronously to keep UI responsive
                    addressResolver.ResolveAddressAndLoadMap(address);
                }
                else
                {
                    LogDebug("ERROR: AddressResolver not available! Falling back to simulation.");
                    StartCoroutine(LoadMapCoroutineSimulation(address));
                }
            }
        }
        
        public void LoadMapFromCoordinates(float latitude, float longitude)
        {
            if (isLoading)
            {
                LogDebug("Already loading, ignoring request");
                return;
            }

            ClearExistingLevel();
            
            LogDebug($"Loading map for coordinates: {latitude}, {longitude}");
            UpdateStatus($"Loading map for coordinates: {latitude:F4}, {longitude:F4}");
            SetLoadingState(true);
            
            // Use real OSM system or simulation based on mode
            if (enableSimulationMode)
            {
                Vector2 coords = new Vector2(latitude, longitude);
                // TODO: Cache generated maps to avoid repeated downloads
                StartCoroutine(CreateLevelFromCoordinates(coords));
            }
            else
            {
                // Use real AddressResolver
                if (addressResolver != null)
                {
                    addressResolver.LoadMapFromCoordinates(latitude, longitude, searchRadius);
                }
                else
                {
                    LogDebug("ERROR: AddressResolver not available! Falling back to simulation.");
                    Vector2 coords = new Vector2(latitude, longitude);
                    StartCoroutine(CreateLevelFromCoordinates(coords));
                }
            }
        }
        
        // Overload for backward compatibility
        public void LoadMapFromCoordinates(Vector2 coordinates)
        {
            LoadMapFromCoordinates(coordinates.x, coordinates.y);
        }
        
        public void UseCurrentLocation()
        {
            LogDebug("Using current location (fallback to Leipzig for now)");
            // TODO: Implement real GPS location when needed
            // For now, use Leipzig as demo location
            LoadMapFromCoordinates(51.3387f, 12.3799f);
        }
        
        public void RegenerateCurrentMap()
        {
            if (string.IsNullOrEmpty(currentAddress))
            {
                LogDebug("No current address to regenerate");
                UpdateStatus("No map to regenerate. Load a map first.");
                return;
            }
            
            LogDebug($"Regenerating map for: {currentAddress}");
            UpdateStatus($"Regenerating map for: {currentAddress}");
            
            LoadMapFromAddress(currentAddress);
        }
        
        private void OnAddressInputEnd(string address)
        {
            // Auto-load on Enter key
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                LoadMapFromInput();
            }
        }
        
        // SIMULATION MODE METHODS (From Remote)
        private IEnumerator LoadMapCoroutineSimulation(string address)
        {
            LogDebug($"Using simulation mode for address: {address}");
            
            isLoading = true;
            currentRetries = 0;
            
            SetLoadingState(true);
            UpdateStatus($"[SIMULATION] Loading map for: {address}");
            
            // Simulate OSM API call
            yield return StartCoroutine(SimulateOSMApiCall(address));
            
            SetLoadingState(false);
            isLoading = false;
        }
        
        private IEnumerator SimulateOSMApiCall(string address)
        {
            // Simulate network delay
            float delay = Random.Range(1f, 3f);
            yield return new WaitForSeconds(delay);
            
            // Simulate success/failure (80% success rate for demo)
            bool success = Random.Range(0f, 1f) < 0.8f || useLeipzigFallback;
            
            if (success)
            {
                UpdateStatus($"[SIMULATION] Map loaded successfully: {address}");
                yield return StartCoroutine(GenerateMapFromAddress(address));
                OnMapLoadCompleted?.Invoke(true);
            }
            else
            {
                currentRetries++;
                if (currentRetries <= maxRetries)
                {
                    UpdateStatus($"[SIMULATION] Failed to load map. Retry {currentRetries}/{maxRetries}...");
                    yield return new WaitForSeconds(1f);
                    yield return StartCoroutine(SimulateOSMApiCall(address));
                }
                else
                {
                    UpdateStatus("[SIMULATION] Failed to load map. Using fallback level.");
                    yield return StartCoroutine(LoadFallbackLevel());
                    OnMapLoadCompleted?.Invoke(false);
                }
            }
        }
        
        private IEnumerator GenerateMapFromAddress(string address)
        {
            UpdateStatus("Generating level from map data...");

            // For Leipzig or as fallback, use specific coordinates
            Vector2 coords = leipzigCoords;
            if (address.ToLower().Contains("leipzig"))
            {
                coords = leipzigCoords;
            }
            else
            {
                bool done = false;
                yield return StartCoroutine(GetCoordsFromAddress(address, result => { coords = result; done = true; }));
                if (!done)
                    coords = leipzigCoords;
            }
            
            // Generate a simple level based on coordinates
            yield return StartCoroutine(CreateLevelFromCoordinates(coords));
            
            UpdateStatus($"Level ready! Explore {address}");
            
            // Hide UI after successful generation
            yield return new WaitForSeconds(2f);
            HideMapUI();
            
            // Start the game
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }
        
        private IEnumerator GetCoordsFromAddress(string address, System.Action<Vector2> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                onCompleted?.Invoke(leipzigCoords);
                yield break;
            }

            string url = $"https://nominatim.openstreetmap.org/search?q={UnityWebRequest.EscapeURL(address)}&format=json&limit=1";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", "RollABallGame/1.0");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        JArray array = JArray.Parse(request.downloadHandler.text);
                        if (array.Count > 0)
                        {
                            float lat = float.Parse(array[0]["lat"].ToString(), CultureInfo.InvariantCulture);
                            float lon = float.Parse(array[0]["lon"].ToString(), CultureInfo.InvariantCulture);
                            onCompleted?.Invoke(new Vector2(lat, lon));
                            yield break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[MapStartupController] Geocode parse error: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[MapStartupController] Geocode request failed: {request.error}");
                }
            }

            // Fallback if request failed
            onCompleted?.Invoke(leipzigCoords);
        }
        
        private IEnumerator CreateLevelFromCoordinates(Vector2 coordinates)
        {
            UpdateStatus("Building terrain...");
            yield return new WaitForSeconds(0.5f);
            
            // Create a simple grid-based level inspired by coordinates
            CreateSimpleMapLevel(coordinates);
            
            UpdateStatus("Placing collectibles...");
            yield return new WaitForSeconds(0.5f);
            
            // Place collectibles
            PlaceCollectiblesOnMap();
            
            UpdateStatus("Setting up player...");
            yield return new WaitForSeconds(0.3f);
            
            // Setup player start position
            SetupPlayerStart();
            
            UpdateStatus("Level ready! Start exploring!");
            SetLoadingState(false);
            
            // Hide UI after successful generation
            yield return new WaitForSeconds(2f);
            HideMapUI();
        }
        
        private void CreateSimpleMapLevel(Vector2 coords)
        {
            // Create a simple interpretation of map data
            // Use coordinate values to influence level generation
            int levelSize = Mathf.Clamp(8 + (int)(coords.x % 10), 8, 16);
            
            LevelGenerator generator = levelGenerator ? levelGenerator : FindFirstObjectByType<LevelGenerator>();
            if (generator != null)
            {
                // Use coordinates to create a unique seed
                int seed = (int)(coords.x * 1000 + coords.y * 1000);
                
                // Try to set generation parameters
                try
                {
                    var seedMethod = generator.GetType().GetMethod("SetGenerationSeed");
                    if (seedMethod != null)
                    {
                        seedMethod.Invoke(generator, new object[] { seed });
                    }
                    
                    var generateMethod = generator.GetType().GetMethod("GenerateLevel");
                    if (generateMethod != null)
                    {
                        generateMethod.Invoke(generator, new object[] { null });
                    }
                }
                catch (System.Exception e)
                {
                    LogDebug($"Could not use LevelGenerator: {e.Message}");
                    CreateFallbackLevel();
                }
            }
            else
            {
                CreateFallbackLevel();
            }
        }
        
        private void CreateFallbackLevel()
        {
            // Create a simple manual level if no generator is available
            if (fallbackLevelPrefab)
            {
                Instantiate(fallbackLevelPrefab);
            }
            else
            {
                // Create minimal level manually
                CreateMinimalLevel();
            }
        }
        
        private void CreateMinimalLevel()
        {
            // TODO-OPT#59: Use prefab for fallback level if available
            if (fallbackLevelPrefab)
            {
                Instantiate(fallbackLevelPrefab);
                return;
            }

            // Manual creation as last resort
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "OSM_Ground";
            ground.transform.localScale = Vector3.one * 2;

            for (int i = 0; i < 4; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"OSM_Wall_{i}";
                wall.transform.position = new Vector3(
                    Mathf.Cos(i * Mathf.PI * 0.5f) * 8,
                    1,
                    Mathf.Sin(i * Mathf.PI * 0.5f) * 8
                );
                wall.transform.localScale = new Vector3(2, 2, 2);
            }
        }
        
        private void PlaceCollectiblesOnMap()
        {
            // Place collectibles randomly on the generated level
            GameObject collectiblePrefab = Resources.Load<GameObject>("CollectiblePrefab");
            if (collectiblePrefab)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 position = new Vector3(
                        Random.Range(-8f, 8f),
                        2f,
                        Random.Range(-8f, 8f)
                    );
                    
                    Instantiate(collectiblePrefab, position, Quaternion.identity);
                }
            }
        }
        
        private void SetupPlayerStart()
        {
            if (!player)
                player = FindFirstObjectByType<PlayerController>();

            if (player)
            {
                player.transform.position = new Vector3(0, 2, 0);
            }
        }
        
        private IEnumerator LoadFallbackLevel()
        {
            UpdateStatus("Loading fallback level...");
            
            if (useLeipzigFallback)
            {
                yield return StartCoroutine(GenerateMapFromAddress("Leipzig, Germany"));
            }
            else if (fallbackLevelPrefab)
            {
                Instantiate(fallbackLevelPrefab);
                UpdateStatus("Fallback level loaded.");
            }
            else
            {
                CreateMinimalLevel();
                UpdateStatus("Minimal level created.");
            }
        }
        
        // REAL OSM EVENT HANDLERS (From Local)
        private void OnMapDataLoaded(OSMMapData mapData)
        {
            LogDebug($"Map data loaded successfully. Roads: {mapData.roads.Count}, Buildings: {mapData.buildings.Count}");
            UpdateStatus("Generating level from map data...");
            
            // The mapGenerator should automatically receive this via its own event subscription
            // But we can also trigger it manually if needed
            if (mapGenerator != null)
            {
                mapGenerator.GenerateMap(mapData);
            }
            else
            {
                LogDebug("ERROR: MapGenerator not available!");
                UpdateStatus("System error: MapGenerator not found");
                SetLoadingState(false);
            }
        }
        
        private void OnAddressResolverError(string error)
        {
            LogDebug($"AddressResolver error: {error}");
            UpdateStatus($"Error: {error}");
            
            currentRetries++;
            if (currentRetries <= maxRetries)
            {
                LogDebug($"Retrying... ({currentRetries}/{maxRetries})");
                StartCoroutine(RetryAfterDelay(1f));
            }
            else
            {
                LogDebug("Max retries reached, falling back to simulation mode");
                enableSimulationMode = true;
                LoadMapFromAddress(currentAddress);
            }
        }
        
        private void OnMapLoadError(string error)
        {
            LogDebug($"Map load error: {error}");
            UpdateStatus($"Warning: {error}");
        }
        
        // Event handlers for MapGenerator
        private void OnMapGenerationStarted(OSMMapData mapData)
        {
            LogDebug("Map generation started");
            UpdateStatus("Building terrain and placing objects...");
        }
        
        private void OnMapGenerationCompleted()
        {
            LogDebug("Map generation completed successfully!");
            UpdateStatus("Level ready! Start exploring!");
            
            SetLoadingState(false);
            
            // Hide UI after successful generation
            StartCoroutine(HideUIAfterDelay(2f));
            
            // Start the game
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
            
            OnMapLoadCompleted?.Invoke(true);
        }
        
        private void OnMapGenerationError(string error)
        {
            LogDebug($"Map generation error: {error}");
            UpdateStatus($"Generation failed: {error}");
            
            // Fallback to simulation mode on error
            LogDebug("Falling back to simulation mode due to generation error");
            enableSimulationMode = true;
            LoadMapFromAddress(currentAddress);
        }
        
        private IEnumerator RetryAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (!string.IsNullOrEmpty(currentAddress))
            {
                UpdateStatus($"Retrying {currentAddress}... ({currentRetries}/{maxRetries})");
                
                if (addressResolver != null && !enableSimulationMode)
                {
                    addressResolver.ResolveAddressAndLoadMap(currentAddress);
                }
                else
                {
                    StartCoroutine(LoadMapCoroutineSimulation(currentAddress));
                }
            }
        }
        
        private IEnumerator HideUIAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideMapUI();
        }
        
        private void SetLoadingState(bool loading)
        {
            isLoading = loading;
            
            if (loadingPanel)
                loadingPanel.SetActive(loading);
            
            if (loadMapButton)
                loadMapButton.interactable = !loading;
            
            if (addressInputField)
                addressInputField.interactable = !loading;
            
            if (useCurrentLocationButton)
                useCurrentLocationButton.interactable = !loading;
            
            if (regenerateMapButton)
                regenerateMapButton.interactable = !loading;
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText)
                statusText.text = message;

            LogDebug($"Status: {message}");
            OnStatusUpdate?.Invoke(message);
        }

        // TODO-OPT#92: Replace destructive cleanup with pooled map objects
        private void ClearExistingLevel()
        {
            GameObject existingMap = GameObject.Find("GeneratedMap");
            if (existingMap)
            {
                Destroy(existingMap);
            }

            GameObject[] fallbackObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in fallbackObjects)
            {
                if (obj.name.StartsWith("OSM_"))
                {
                    Destroy(obj);
                }
            }
        }
        
        private void HideMapUI()
        {
            // Hide the input UI after successful map generation
            GameObject inputPanel = GameObject.Find("AddressInputPanel");
            if (inputPanel)
            {
                inputPanel.SetActive(false);
                LogDebug("Hidden AddressInputPanel");
            }

            if (addressInputField)
                addressInputField.gameObject.SetActive(false);

            if (loadMapButton)
                loadMapButton.gameObject.SetActive(false);

            if (useCurrentLocationButton)
                useCurrentLocationButton.gameObject.SetActive(false);

            if (regenerateMapButton)
                regenerateMapButton.gameObject.SetActive(false);

            if (loadingPanel)
                loadingPanel.SetActive(false);

            if (statusText)
                statusText.gameObject.SetActive(false);

            // Also try the canvas approach (from remote)
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas && inputPanel == null)
            {
                canvas.gameObject.SetActive(false);
                LogDebug("Hidden entire Canvas");
            }
            
            // Keep game UI visible
            GameObject gameUIPanel = GameObject.Find("GameUIPanel");
            if (gameUIPanel)
            {
                gameUIPanel.SetActive(true);
                LogDebug("Ensured GameUIPanel is visible");
            }
        }
        
        private void ShowMapUI()
        {
            // Show the input UI
            GameObject inputPanel = GameObject.Find("AddressInputPanel");
            if (inputPanel)
            {
                inputPanel.SetActive(true);
                LogDebug("Shown AddressInputPanel");
            }

            if (addressInputField)
                addressInputField.gameObject.SetActive(true);

            if (loadMapButton)
                loadMapButton.gameObject.SetActive(true);

            if (useCurrentLocationButton)
                useCurrentLocationButton.gameObject.SetActive(true);

            if (regenerateMapButton)
                regenerateMapButton.gameObject.SetActive(true);

            if (loadingPanel)
                loadingPanel.SetActive(false);

            if (statusText)
                statusText.gameObject.SetActive(true);

            // Also try the canvas approach
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas && !canvas.gameObject.activeInHierarchy)
            {
                canvas.gameObject.SetActive(true);
                LogDebug("Shown Canvas");
            }
        }
        
        /// <summary>
        /// Start automatic generation mode for endless OSM levels
        /// </summary>
        private void StartAutoGenerateMode()
        {
            LogDebug("Starting auto-generate mode for endless levels");
            
            // Hide UI components for automatic mode
            HideMapUI();
            
            // Get location index from PlayerPrefs
            int locationIndex = PlayerPrefs.GetInt("OSMLocationIndex", 0);
            
            // Cycle through available addresses
            if (endlessModeAddresses.Length == 0)
            {
                LogDebug("No endless mode addresses configured. Using default.");
                LoadMapFromAddress(defaultAddress);
                return;
            }
            
            // Use modulo to cycle through addresses infinitely
            string selectedAddress = endlessModeAddresses[locationIndex % endlessModeAddresses.Length];
            
            UpdateStatus($"Endless Mode Level {locationIndex + 1}: Loading {selectedAddress}...");
            
            // Auto-load the map
            LoadMapFromAddress(selectedAddress);
            
            // Increment for next time
            PlayerPrefs.SetInt("OSMLocationIndex", locationIndex + 1);
        }
        
        // Public methods for external control
        public void LoadLeipzigMap()
        {
            LoadMapFromAddress("Leipzig, Germany");
        }
        
        public void LoadRandomMap()
        {
            string[] sampleAddresses = {
                "Berlin, Germany",
                "Munich, Germany", 
                "Hamburg, Germany",
                "Dresden, Germany",
                "Leipzig, Germany"
            };
            
            string randomAddress = sampleAddresses[Random.Range(0, sampleAddresses.Length)];
            LoadMapFromAddress(randomAddress);
        }
        
        public void ResetToDefault()
        {
            if (addressInputField)
                addressInputField.text = defaultAddress;
            UpdateStatus("Ready to load map. Enter address and click Load Map.");
            ShowMapUI();
        }
        
        public void ResetController()
        {
            // Reset the controller to default state
            isLoading = false;
            currentRetries = 0;
            currentAddress = "";
            
            // Reset UI
            if (addressInputField)
                addressInputField.text = defaultAddress;
                
            SetLoadingState(false);
            UpdateStatus("Controller reset. Ready to load map.");
            
            // Show UI if hidden
            ShowMapUI();
        }
        
        // Additional methods expected by other scripts (from remote)
        public void LoadFallbackMap()
        {
            if (useLeipzigFallback)
            {
                LoadLeipzigMap();
            }
            else
            {
                StartCoroutine(LoadFallbackLevel());
            }
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                string modeIndicator = enableSimulationMode ? "[SIMULATION]" : "[REAL OSM]";
                Debug.Log($"[MapStartupController] {modeIndicator} {message}");
            }
        }
        
        // Clean up event subscriptions
        private void OnDestroy()
        {
            if (addressResolver != null)
            {
                addressResolver.OnMapDataLoaded -= OnMapDataLoaded;
                addressResolver.OnError -= OnAddressResolverError;
                addressResolver.OnMapLoadErrorEvent -= OnMapLoadError;
            }
            
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerationStarted -= OnMapGenerationStarted;
                mapGenerator.OnMapGenerationCompleted -= OnMapGenerationCompleted;
                mapGenerator.OnGenerationError -= OnMapGenerationError;
            }
        }
    }
}
