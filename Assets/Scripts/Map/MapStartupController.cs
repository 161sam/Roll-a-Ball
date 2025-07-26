using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace RollABall.Map
{
    /// <summary>
    /// Orchestrates the map loading process and integrates with the existing Roll-a-Ball systems
    /// Handles the flow from address input to playable map generation
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Map/Map Startup Controller")]
    public class MapStartupController : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private AddressResolver addressResolver;
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UIController uiController;
        
        [Header("UI References")]
        [SerializeField] private GameObject addressInputPanel;
        [SerializeField] private TMP_InputField addressInputField;
        [SerializeField] private Button loadMapButton;
        [SerializeField] private Button useCurrentLocationButton;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private Slider loadingProgressBar;
        
        [Header("Fallback Configuration")]
        [SerializeField] private bool enableFallbackMode = true;
        [SerializeField] private double fallbackLat = 52.5217;
        [SerializeField] private double fallbackLon = 13.4132;
        
        [Header("Default Locations")]
        [SerializeField] private string[] suggestedAddresses = {
            "Leipzig, Markt",
            "Berlin, Brandenburger Tor",
            "München, Marienplatz",
            "Hamburg, Speicherstadt",
            "Köln, Dom"
        };
        
        // State management
        private bool isMapLoaded = false;
        private string lastLoadedAddress = "";
        private OSMMapData currentMapData;
        
        // Progress tracking
        private enum LoadingPhase
        {
            None,
            ResolvingAddress,
            LoadingMapData,
            GeneratingWorld,
            FinalizingSetup
        }
        
        private void Awake()
        {
            ValidateComponents();
            SetupUI();
        }
        
        private void Start()
        {
            ShowAddressInput();
        }
        
        private void OnEnable()
        {
            SubscribeToEvents();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        /// <summary>
        /// Validate that all required components are assigned
        /// </summary>
        private void ValidateComponents()
        {
            if (addressResolver == null)
            {
                addressResolver = FindFirstObjectByType<AddressResolver>();
                if (addressResolver == null)
                {
                    GameObject resolverGO = new GameObject("AddressResolver");
                    addressResolver = resolverGO.AddComponent<AddressResolver>();
                    Debug.Log("[MapStartupController] Created AddressResolver component");
                }
            }
            
            if (mapGenerator == null)
            {
                mapGenerator = FindFirstObjectByType<MapGenerator>();
                if (mapGenerator == null)
                {
                    GameObject generatorGO = new GameObject("MapGenerator");
                    mapGenerator = generatorGO.AddComponent<MapGenerator>();
                    Debug.Log("[MapStartupController] Created MapGenerator component");
                }
            }
            
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }
            
            if (uiController == null)
            {
                uiController = FindFirstObjectByType<UIController>();
            }
        }
        
        /// <summary>
        /// Setup UI elements and button listeners
        /// </summary>
        private void SetupUI()
        {
            if (loadMapButton != null)
            {
                loadMapButton.onClick.RemoveAllListeners();
                loadMapButton.onClick.AddListener(OnLoadMapButtonClicked);
            }
            
            if (useCurrentLocationButton != null)
            {
                useCurrentLocationButton.onClick.RemoveAllListeners();
                useCurrentLocationButton.onClick.AddListener(OnUseCurrentLocationClicked);
            }
            
            if (addressInputField != null)
            {
                addressInputField.onEndEdit.RemoveAllListeners();
                addressInputField.onEndEdit.AddListener(OnAddressInputEnded);
                
                // Set placeholder text
                if (addressInputField.placeholder is TextMeshProUGUI placeholder)
                {
                    placeholder.text = "z.B. Leipzig, Markt";
                }
            }
            
            // Initially hide loading panel
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Subscribe to events from address resolver and map generator
        /// </summary>
        private void SubscribeToEvents()
        {
            if (addressResolver != null)
            {
                addressResolver.OnAddressResolved += OnAddressResolved;
                addressResolver.OnMapDataLoaded += OnMapDataLoaded;
                addressResolver.OnError += OnAddressResolverError;
            }
            
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerationStarted += OnMapGenerationStarted;
                mapGenerator.OnPlayerSpawnPositionSet += OnPlayerSpawnPositionSet;
                mapGenerator.OnMapGenerationCompleted += OnMapGenerationCompleted;
                mapGenerator.OnGenerationError += OnMapGenerationError;
            }
        }
        
        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (addressResolver != null)
            {
                addressResolver.OnAddressResolved -= OnAddressResolved;
                addressResolver.OnMapDataLoaded -= OnMapDataLoaded;
                addressResolver.OnError -= OnAddressResolverError;
            }
            
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerationStarted -= OnMapGenerationStarted;
                mapGenerator.OnPlayerSpawnPositionSet -= OnPlayerSpawnPositionSet;
                mapGenerator.OnMapGenerationCompleted -= OnMapGenerationCompleted;
                mapGenerator.OnGenerationError -= OnMapGenerationError;
            }
        }
        
        /// <summary>
        /// Show the address input UI
        /// </summary>
        public void ShowAddressInput()
        {
            if (addressInputPanel != null)
            {
                addressInputPanel.SetActive(true);
            }
            
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            
            // Populate suggestions dropdown if available
            PopulateAddressSuggestions();
        }
        
        /// <summary>
        /// Load map from user-entered address
        /// </summary>
        public void LoadMapFromAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                ShowError("Bitte geben Sie eine gültige Adresse ein.");
                return;
            }
            
            lastLoadedAddress = address;
            ShowLoadingScreen("Adresse wird aufgelöst...");
            
            addressResolver.ResolveAddressAndLoadMap(address);
        }
        
        /// <summary>
        /// Load map from coordinates (for current location feature)
        /// </summary>
        public void LoadMapFromCoordinates(double lat, double lon)
        {
            lastLoadedAddress = $"Koordinaten: {lat:F4}, {lon:F4}";
            ShowLoadingScreen("Kartendaten werden geladen...");
            
            addressResolver.LoadMapFromCoordinates(lat, lon);
        }
        
        /// <summary>
        /// Load fallback map if everything else fails
        /// </summary>
        public void LoadFallbackMap()
        {
            if (!enableFallbackMode)
            {
                ShowError("Fallback-Modus ist deaktiviert. Keine Karte verfügbar.");
                return;
            }
            
            Debug.Log("[MapStartupController] Loading fallback map");
            LoadMapFromCoordinates(fallbackLat, fallbackLon);
        }
        
        /// <summary>
        /// Regenerate current map with different settings
        /// </summary>
        public void RegenerateCurrentMap()
        {
            if (currentMapData != null)
            {
                ShowLoadingScreen("Welt wird neu generiert...");
                mapGenerator.RegenerateMap(currentMapData);
            }
            else
            {
                ShowError("Keine Kartendaten zum Regenerieren verfügbar.");
            }
        }
        
        // UI Event Handlers
        private void OnLoadMapButtonClicked()
        {
            if (addressInputField != null)
            {
                LoadMapFromAddress(addressInputField.text);
            }
        }
        
        private void OnUseCurrentLocationClicked()
        {
            // For demo purposes, use Leipzig coordinates
            // In a real implementation, you would use Unity's Location Service
            LoadMapFromCoordinates(51.3387, 12.3799); // Leipzig
        }
        
        private void OnAddressInputEnded(string input)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                LoadMapFromAddress(input);
            }
        }
        
        // Event Handlers for Address Resolution
        private void OnAddressResolved(AddressResolver.GeocodeResult result)
        {
            Debug.Log($"[MapStartupController] Address resolved: {result.displayName}");
            UpdateLoadingProgress("Kartendaten werden geladen...", 0.3f);
        }
        
        private void OnMapDataLoaded(OSMMapData mapData)
        {
            Debug.Log("[MapStartupController] Map data loaded, starting world generation");
            currentMapData = mapData;
            UpdateLoadingProgress("3D-Welt wird generiert...", 0.6f);
            
            mapGenerator.GenerateMap(mapData);
        }
        
        private void OnAddressResolverError(string error)
        {
            Debug.LogWarning($"[MapStartupController] Address resolver error: {error}");
            
            if (enableFallbackMode)
            {
                ShowError($"{error}\nLade Fallback-Karte...");
                StartCoroutine(LoadFallbackAfterDelay(2f));
            }
            else
            {
                ShowError(error);
                ShowAddressInput();
            }
        }
        
        // Event Handlers for Map Generation
        private void OnMapGenerationStarted(OSMMapData mapData)
        {
            Debug.Log("[MapStartupController] Map generation started");
            UpdateLoadingProgress("Gebäude und Straßen werden erstellt...", 0.7f);
        }
        
        private void OnPlayerSpawnPositionSet(Vector3 spawnPosition)
        {
            Debug.Log($"[MapStartupController] Player spawn position set: {spawnPosition}");
            UpdateLoadingProgress("Spieler wird positioniert...", 0.9f);
        }
        
        private void OnMapGenerationCompleted()
        {
            Debug.Log("[MapStartupController] Map generation completed");
            UpdateLoadingProgress("Fertig!", 1f);
            
            StartCoroutine(FinalizeMapLoadingCoroutine());
        }
        
        private void OnMapGenerationError(string error)
        {
            Debug.LogError($"[MapStartupController] Map generation error: {error}");
            
            if (enableFallbackMode && !lastLoadedAddress.Contains("Fallback"))
            {
                ShowError($"Generierung fehlgeschlagen: {error}\nLade Fallback-Karte...");
                StartCoroutine(LoadFallbackAfterDelay(2f));
            }
            else
            {
                ShowError($"Kartengenerierung fehlgeschlagen: {error}");
                ShowAddressInput();
            }
        }
        
        /// <summary>
        /// Finalize the map loading process
        /// </summary>
        private IEnumerator FinalizeMapLoadingCoroutine()
        {
            yield return new WaitForSeconds(0.5f); // Brief pause to show completion
            
            // Hide loading screen
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            
            // Hide address input
            if (addressInputPanel != null)
            {
                addressInputPanel.SetActive(false);
            }
            
            // Initialize game systems
            InitializeGameSystems();
            
            // Update UI
            if (uiController != null)
            {
                uiController.ShowGameUI();
                uiController.UpdateLocationDisplay(lastLoadedAddress);
            }
            
            // Start the game
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
            
            isMapLoaded = true;
            Debug.Log("[MapStartupController] Map loading process completed successfully");
        }
        
        /// <summary>
        /// Initialize or reinitialize game systems for the new map
        /// </summary>
        private void InitializeGameSystems()
        {
            // Update camera if it exists
            CameraController cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    cameraController.SetTarget(player.transform);
                }
            }
            
            // Update game manager
            if (gameManager != null)
            {
                gameManager.ResetGame();
                gameManager.UpdateCollectibleCount();
            }
            
            // Setup audio
            AudioManager audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager != null)
            {
                audioManager.PlayAmbientMusic();
            }
        }
        
        /// <summary>
        /// Show loading screen with message
        /// </summary>
        private void ShowLoadingScreen(string message)
        {
            if (addressInputPanel != null)
            {
                addressInputPanel.SetActive(false);
            }
            
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }
            
            UpdateLoadingProgress(message, 0f);
        }
        
        /// <summary>
        /// Update loading progress display
        /// </summary>
        private void UpdateLoadingProgress(string message, float progress)
        {
            if (loadingText != null)
            {
                loadingText.text = message;
            }
            
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = progress;
            }
        }
        
        /// <summary>
        /// Show error message
        /// </summary>
        private void ShowError(string message)
        {
            Debug.LogError($"[MapStartupController] Error: {message}");
            
            if (loadingText != null)
            {
                loadingText.text = $"Fehler: {message}";
                loadingText.color = Color.red;
            }
            
            // Reset text color after a delay
            StartCoroutine(ResetErrorTextCoroutine());
        }
        
        /// <summary>
        /// Reset error text color
        /// </summary>
        private IEnumerator ResetErrorTextCoroutine()
        {
            yield return new WaitForSeconds(3f);
            if (loadingText != null)
            {
                loadingText.color = Color.white;
            }
        }
        
        /// <summary>
        /// Load fallback map after a delay
        /// </summary>
        private IEnumerator LoadFallbackAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadFallbackMap();
        }
        
        /// <summary>
        /// Populate address suggestions (could be extended with a dropdown)
        /// </summary>
        private void PopulateAddressSuggestions()
        {
            // This could be extended to create a dropdown with suggested addresses
            // For now, we just log the suggestions
            Debug.Log($"[MapStartupController] Available address suggestions: {string.Join(", ", suggestedAddresses)}");
        }
        
        // Public API for external access
        public bool IsMapLoaded => isMapLoaded;
        public string LastLoadedAddress => lastLoadedAddress;
        public OSMMapData CurrentMapData => currentMapData;
        public Vector3 GetPlayerSpawnPosition() => mapGenerator?.GetPlayerSpawnPosition() ?? Vector3.zero;
        
        /// <summary>
        /// Reset the map startup controller
        /// </summary>
        public void ResetController()
        {
            isMapLoaded = false;
            lastLoadedAddress = "";
            currentMapData = null;
            
            // Cancel any ongoing operations
            if (addressResolver != null)
            {
                addressResolver.CancelCurrentRequest();
            }
            
            ShowAddressInput();
        }
        
        /// <summary>
        /// Quick load a predefined location
        /// </summary>
        public void QuickLoadLocation(int suggestionIndex)
        {
            if (suggestionIndex >= 0 && suggestionIndex < suggestedAddresses.Length)
            {
                LoadMapFromAddress(suggestedAddresses[suggestionIndex]);
            }
        }
    }
}
