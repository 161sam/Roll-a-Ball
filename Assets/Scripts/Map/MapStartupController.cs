using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace RollABall.Map
{
    /// <summary>
    /// Handles OpenStreetMap integration and startup for Level_OSM scene
    /// Provides address input, map data loading, and fallback mechanisms
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Map/Map Startup Controller")]
    public class MapStartupController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField addressInputField;
    [SerializeField] private Button loadMapButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("Map Settings")]
    [SerializeField] private string defaultAddress = "Leipzig, Germany";
    [SerializeField] private int maxRetries = 3;
    
    [Header("Fallback")]
    [SerializeField] private GameObject fallbackLevelPrefab;
    [SerializeField] private bool useLeipzigFallback = true;
    
    // Leipzig coordinates (fallback)
    private readonly Vector2 leipzigCoords = new Vector2(51.3387f, 12.3799f);
    
    private bool isLoading = false;
    private int currentRetries = 0;
    
    public System.Action<bool> OnMapLoadCompleted; // success
    public System.Action<string> OnStatusUpdate; // status message
    
    private void Start()
    {
        InitializeUI();
        
        // Set default address
        if (addressInputField)
            addressInputField.text = defaultAddress;
        
        UpdateStatus("Ready to load map. Enter address and click Load Map.");
    }
    
    private void InitializeUI()
    {
        // Auto-find UI components if not assigned
        if (!addressInputField)
            addressInputField = Object.FindFirstObjectByType<TMP_InputField>();
        
        if (!loadMapButton)
            loadMapButton = Object.FindFirstObjectByType<Button>();
        
        if (!statusText)
            statusText = Object.FindFirstObjectByType<TextMeshProUGUI>();
        
        // Setup button listener
        if (loadMapButton)
        {
            loadMapButton.onClick.RemoveAllListeners();
            loadMapButton.onClick.AddListener(LoadMapFromInput);
        }
        
        // Setup input field listener
        if (addressInputField)
        {
            addressInputField.onEndEdit.RemoveAllListeners();
            addressInputField.onEndEdit.AddListener(OnAddressInputEnd);
        }
    }
    
    public void LoadMapFromInput()
    {
        if (isLoading) return;
        
        string address = addressInputField ? addressInputField.text.Trim() : defaultAddress;
        
        if (string.IsNullOrEmpty(address))
        {
            UpdateStatus("⚠️ Please enter a valid address.");
            return;
        }
        
        StartCoroutine(LoadMapCoroutine(address));
    }
    
    private void OnAddressInputEnd(string address)
    {
        // Auto-load on Enter key
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            LoadMapFromInput();
        }
    }
    
    private IEnumerator LoadMapCoroutine(string address)
    {
        isLoading = true;
        currentRetries = 0;
        
        SetLoadingState(true);
        UpdateStatus($"🌍 Loading map for: {address}");
        
        // Simulate OSM API call (placeholder for real implementation)
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
            UpdateStatus($"✅ Map loaded successfully: {address}");
            yield return StartCoroutine(GenerateMapFromAddress(address));
            OnMapLoadCompleted?.Invoke(true);
        }
        else
        {
            currentRetries++;
            if (currentRetries <= maxRetries)
            {
                UpdateStatus($"⚠️ Failed to load map. Retry {currentRetries}/{maxRetries}...");
                yield return new WaitForSeconds(1f);
                yield return StartCoroutine(SimulateOSMApiCall(address));
            }
            else
            {
                UpdateStatus("❌ Failed to load map. Using fallback level.");
                yield return StartCoroutine(LoadFallbackLevel());
                OnMapLoadCompleted?.Invoke(false);
            }
        }
    }
    
    private IEnumerator GenerateMapFromAddress(string address)
    {
        UpdateStatus("🔧 Generating level from map data...");
        
        // For Leipzig or as fallback, use specific coordinates
        Vector2 coords = address.ToLower().Contains("leipzig") ? leipzigCoords : GetCoordsFromAddress(address);
        
        // Generate a simple level based on coordinates
        yield return StartCoroutine(CreateLevelFromCoordinates(coords));
        
        UpdateStatus($"🎯 Level ready! Explore {address}");
        
        // Hide UI after successful generation
        yield return new WaitForSeconds(2f);
        HideMapUI();
    }
    
    private Vector2 GetCoordsFromAddress(string address)
    {
        // Placeholder: In real implementation, this would use Nominatim or similar geocoding API
        // For now, return Leipzig coordinates as fallback
        return leipzigCoords;
    }
    
    private IEnumerator CreateLevelFromCoordinates(Vector2 coordinates)
    {
        UpdateStatus("🏗️ Building terrain...");
        yield return new WaitForSeconds(0.5f);
        
        // Create a simple grid-based level inspired by coordinates
        CreateSimpleMapLevel(coordinates);
        
        UpdateStatus("📍 Placing collectibles...");
        yield return new WaitForSeconds(0.5f);
        
        // Place collectibles
        PlaceCollectiblesOnMap();
        
        UpdateStatus("🎮 Setting up player...");
        yield return new WaitForSeconds(0.3f);
        
        // Setup player start position
        SetupPlayerStart();
    }
    
    private void CreateSimpleMapLevel(Vector2 coords)
    {
        // Create a simple interpretation of map data
        // Use coordinate values to influence level generation
        int levelSize = Mathf.Clamp(8 + (int)(coords.x % 10), 8, 16);
        
        LevelGenerator generator = Object.FindFirstObjectByType<LevelGenerator>();
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
                Debug.LogWarning($"Could not use LevelGenerator: {e.Message}");
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
        // Create ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "OSM_Ground";
        ground.transform.localScale = Vector3.one * 2;
        
        // Create some walls
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
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player)
        {
            player.transform.position = new Vector3(0, 2, 0);
        }
    }
    
    private IEnumerator LoadFallbackLevel()
    {
        UpdateStatus("🔄 Loading fallback level...");
        
        if (useLeipzigFallback)
        {
            yield return StartCoroutine(GenerateMapFromAddress("Leipzig, Germany"));
        }
        else if (fallbackLevelPrefab)
        {
            Instantiate(fallbackLevelPrefab);
            UpdateStatus("📦 Fallback level loaded.");
        }
        else
        {
            CreateMinimalLevel();
            UpdateStatus("⚡ Minimal level created.");
        }
    }
    
    private void SetLoadingState(bool loading)
    {
        if (loadingPanel)
            loadingPanel.SetActive(loading);
        
        if (loadMapButton)
            loadMapButton.interactable = !loading;
        
        if (addressInputField)
            addressInputField.interactable = !loading;
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText)
            statusText.text = message;
        
        Debug.Log($"[MapStartup] {message}");
        OnStatusUpdate?.Invoke(message);
    }
    
    private void HideMapUI()
    {
        // Hide the input UI after successful map generation
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas)
        {
            canvas.gameObject.SetActive(false);
        }
    }
    
    // Public methods for external control
    public void LoadLeipzigMap()
    {
        if (addressInputField)
            addressInputField.text = "Leipzig, Germany";
        LoadMapFromInput();
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
        if (addressInputField)
            addressInputField.text = randomAddress;
        LoadMapFromInput();
    }
    
    public void ResetToDefault()
    {
        if (addressInputField)
            addressInputField.text = defaultAddress;
        UpdateStatus("Ready to load map. Enter address and click Load Map.");
    }
    
    // Additional methods expected by other scripts
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
    
    public void LoadMapFromAddress(string address)
    {
        if (addressInputField)
            addressInputField.text = address;
        LoadMapFromInput();
    }
    
    public void LoadMapFromCoordinates(Vector2 coordinates)
    {
        StartCoroutine(CreateLevelFromCoordinates(coordinates));
    }
    
    // Overload for LoadMapFromCoordinates with two parameters (lat, lon)
    public void LoadMapFromCoordinates(float latitude, float longitude)
    {
        Vector2 coordinates = new Vector2(latitude, longitude);
        LoadMapFromCoordinates(coordinates);
    }
    
    public void RegenerateCurrentMap()
    {
        // Regenerate the current map with same settings
        string currentAddress = addressInputField ? addressInputField.text : defaultAddress;
        if (string.IsNullOrEmpty(currentAddress))
            currentAddress = defaultAddress;
            
        UpdateStatus("🔄 Regenerating current map...");
        StartCoroutine(LoadMapCoroutine(currentAddress));
    }
    
    public void ResetController()
    {
        // Reset the controller to default state
        isLoading = false;
        currentRetries = 0;
        
        // Reset UI
        if (addressInputField)
            addressInputField.text = defaultAddress;
            
        SetLoadingState(false);
        UpdateStatus("🔄 Controller reset. Ready to load map.");
        
        // Show UI if hidden
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas && !canvas.gameObject.activeInHierarchy)
        {
            canvas.gameObject.SetActive(true);
        }
    }
}
}
