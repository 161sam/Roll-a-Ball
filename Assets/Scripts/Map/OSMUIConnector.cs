using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using RollABall.Utility;

namespace RollABall.Map
{
    /// <summary>
    /// Fixes all UI-related issues in OSM levels
    /// Connects buttons, input fields, and ensures proper event handling
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/OSM UI Connector")]
    public class OSMUIConnector : MonoBehaviour
    {
        [Header("Auto-Connect Settings")]
        [SerializeField] private bool autoConnectOnStart = true;
        [SerializeField] private bool createMissingElements = true;
        [SerializeField] private bool debugMode = true;
        
        [Header("Manual Control")]
        [SerializeField] private bool reconnectNow = false;
        
        [Header("Default Addresses")]
        [SerializeField] private string[] defaultAddresses = {
            "Leipzig, Markt",
            "Berlin, Brandenburger Tor",
            "München, Marienplatz",
            "Hamburg, Speicherstadt"
        };

        [Header("Prefabs")]
        [SerializeField] private Button quickButtonPrefab;
        
        // UI References (auto-discovered)
        private TMP_InputField addressInputField;
        private Button loadMapButton;
        private Button useLocationButton;
        private Button[] suggestedLocationButtons;
        private TextMeshProUGUI statusText;
        private GameObject loadingPanel;
        private Slider progressBar;
        
        // Component references
        private MapStartupController mapController;
        private UIController uiController;

        private void Start()
        {
            if (autoConnectOnStart)
            {
                StartCoroutine(ConnectUIElementsDelayed());
            }
        }

        private void OnValidate()
        {
            if (reconnectNow)
            {
                reconnectNow = false;
                ConnectUIElements();
            }
        }

        /// <summary>
        /// Delayed connection to ensure all components are initialized
        /// </summary>
        private IEnumerator ConnectUIElementsDelayed()
        {
            yield return new WaitForEndOfFrame();
            ConnectUIElements();
        }

        /// <summary>
        /// Main UI connection function
        /// </summary>
        [ContextMenu("Connect UI Elements")]
        public void ConnectUIElements()
        {
            Log("Starting OSM UI connection process...");
            
            // Step 1: Find or create map controller
            SetupMapController();
            
            // Step 2: Find UI elements
            DiscoverUIElements();
            
            // Step 3: Create missing UI elements if needed
            if (createMissingElements)
            {
                CreateMissingUIElements();
            }
            
            // Step 4: Connect button events
            ConnectButtonEvents();
            
            // Step 5: Setup input field
            SetupInputField();
            
            // Step 6: Setup status displays
            SetupStatusDisplays();
            
            // Step 7: Create quick access buttons
            CreateQuickAccessButtons();
            
            Log("OSM UI connection completed!");
        }

        #region Setup Functions

        private void SetupMapController()
        {
            mapController = Utility.SceneObjectUtils.FindOrCreateComponent<MapStartupController>("MapStartupController");
            uiController = Utility.SceneObjectUtils.FindOrCreateComponent<UIController>("UIController");
        }

        private void DiscoverUIElements()
        {
            Log("Discovering UI elements...");
            
            // Find input field
            TMP_InputField[] inputFields = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
            foreach (var field in inputFields)
            {
                if (field.name.ToLower().Contains("address") || 
                    field.placeholder && field.placeholder.GetComponent<TextMeshProUGUI>()?.text.Contains("Adresse") == true)
                {
                    addressInputField = field;
                    Log($"Found address input field: {field.name}");
                    break;
                }
            }
            
            // Find buttons
            Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                string buttonName = button.name.ToLower();
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                string buttonTextContent = buttonText ? buttonText.text.ToLower() : "";
                
                if (buttonName.Contains("load") || buttonName.Contains("map") || 
                    buttonTextContent.Contains("laden") || buttonTextContent.Contains("map"))
                {
                    loadMapButton = button;
                    Log($"Found load map button: {button.name}");
                }
                else if (buttonName.Contains("location") || buttonName.Contains("current") ||
                         buttonTextContent.Contains("standort") || buttonTextContent.Contains("aktuell"))
                {
                    useLocationButton = button;
                    Log($"Found location button: {button.name}");
                }
            }
            
            // Find status text
            TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var text in texts)
            {
                if (text.name.ToLower().Contains("status") || 
                    text.name.ToLower().Contains("loading") ||
                    text.name.ToLower().Contains("info"))
                {
                    statusText = text;
                    Log($"Found status text: {text.name}");
                    break;
                }
            }
            
            // Find loading panel
            GameObject[] panels = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var panel in panels)
            {
                if (panel.name.ToLower().Contains("loading") || 
                    panel.name.ToLower().Contains("progress"))
                {
                    loadingPanel = panel;
                    Log($"Found loading panel: {panel.name}");
                    break;
                }
            }
            
            // Find progress bar
            progressBar = FindFirstObjectByType<Slider>();
            if (progressBar)
            {
                Log($"Found progress bar: {progressBar.name}");
            }
        }

        private void CreateMissingUIElements()
        {
            Log("Creating missing UI elements...");
            
            // Create canvas if none exists
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas)
            {
                GameObject canvasGO = new GameObject("OSM_Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                Log("Created Canvas");
            }
            
            // Create input panel if missing
            if (!addressInputField)
            {
                addressInputField = CreateAddressInputField(canvas.transform);
            }
            
            // Create buttons if missing
            if (!loadMapButton)
            {
                loadMapButton = CreateButton(canvas.transform, "LoadMapButton", "Karte laden", new Vector2(0, -50));
            }
            
            if (!useLocationButton)
            {
                useLocationButton = CreateButton(canvas.transform, "UseLocationButton", "Leipzig verwenden", new Vector2(0, -100));
            }
            
            // Create status display if missing
            if (!statusText)
            {
                statusText = CreateStatusText(canvas.transform);
            }
        }

        #endregion

        #region UI Creation Functions

        private TMP_InputField CreateAddressInputField(Transform parent)
        {
            Log("Creating address input field...");
            
            GameObject inputGO = new GameObject("AddressInputField");
            inputGO.transform.SetParent(parent);
            
            RectTransform rect = inputGO.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(300, 30);
            
            Image background = inputGO.AddComponent<Image>();
            background.color = new Color(1, 1, 1, 0.8f);
            
            TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();
            
            // Create text area
            GameObject textAreaGO = new GameObject("TextArea");
            textAreaGO.transform.SetParent(inputGO.transform);
            RectTransform textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;
            
            // Create text component
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 14;
            text.color = Color.black;
            
            // Create placeholder
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform);
            RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholder.text = "z.B. Leipzig, Markt";
            placeholder.fontSize = 14;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            // Configure input field
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.targetGraphic = background;
            
            Log("Created address input field");
            return inputField;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            Log($"Creating button: {name}");
            
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent);
            
            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 40);
            
            Image background = buttonGO.AddComponent<Image>();
            background.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = background;
            
            // Create text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 14;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            Log($"Created button: {name}");
            return button;
        }

        private TextMeshProUGUI CreateStatusText(Transform parent)
        {
            Log("Creating status text...");
            
            GameObject textGO = new GameObject("StatusText");
            textGO.transform.SetParent(parent);
            
            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, -200);
            rect.sizeDelta = new Vector2(400, 50);
            
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Geben Sie eine Adresse ein...";
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            Log("Created status text");
            return text;
        }

        #endregion

        #region Event Connection

        private void ConnectButtonEvents()
        {
            Log("Connecting button events...");
            
            if (loadMapButton)
            {
                loadMapButton.onClick.RemoveAllListeners();
                loadMapButton.onClick.AddListener(OnLoadMapClicked);
                Log("Connected load map button");
            }
            
            if (useLocationButton)
            {
                useLocationButton.onClick.RemoveAllListeners();
                useLocationButton.onClick.AddListener(OnUseLocationClicked);
                Log("Connected use location button");
            }
        }

        private void SetupInputField()
        {
            if (!addressInputField) return;
            
            Log("Setting up input field events...");
            
            addressInputField.onEndEdit.RemoveAllListeners();
            addressInputField.onEndEdit.AddListener(OnAddressInputSubmitted);
            
            // Also listen for Enter key
            addressInputField.onSubmit.RemoveAllListeners();
            addressInputField.onSubmit.AddListener(OnAddressInputSubmitted);
            
            Log("Connected input field events");
        }

        private void SetupStatusDisplays()
        {
            if (!statusText) return;
            
            Log("Setting up status displays...");
            
            if (mapController)
            {
                // Subscribe to map controller events if available
                // This would require extending the MapStartupController with public events
                statusText.text = "Bereit für Adresseingabe";
            }
        }

        private void CreateQuickAccessButtons()
        {
            Log("Creating quick access buttons...");

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas || defaultAddresses.Length == 0) return;

            for (int i = 0; i < defaultAddresses.Length && i < 4; i++)
            {
                string address = defaultAddresses[i];
                Vector2 position = new Vector2(-200 + (i * 100), -150);

                Button quickButton;
                if (quickButtonPrefab)
                {
                    quickButton = Instantiate(quickButtonPrefab, canvas.transform);
                    quickButton.name = $"QuickButton_{i}";
                    RectTransform rect = quickButton.GetComponent<RectTransform>();
                    rect.anchoredPosition = position;
                    quickButton.GetComponentInChildren<TextMeshProUGUI>().text = address.Split(',')[0];
                }
                else
                {
                    quickButton = CreateButton(canvas.transform, $"QuickButton_{i}",
                        address.Split(',')[0], position);
                }
                    
                // Create closure for the address
                string capturedAddress = address;
                quickButton.onClick.AddListener(() => LoadSpecificAddress(capturedAddress));
                
                // Make button smaller
                quickButton.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 30);
                quickButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 10;
            }
            
            Log($"Created {defaultAddresses.Length} quick access buttons");
        }

        #endregion

        #region Event Handlers

        private void OnLoadMapClicked()
        {
            Log("Load map button clicked");
            
            if (addressInputField && !string.IsNullOrWhiteSpace(addressInputField.text))
            {
                LoadAddress(addressInputField.text);
            }
            else
            {
                UpdateStatus("Bitte geben Sie eine Adresse ein!");
            }
        }

        private void OnUseLocationClicked()
        {
            Log("Use location button clicked");
            
            // Use Leipzig as default location
            LoadCoordinates(51.3387, 12.3799, "Leipzig");
        }

        private void OnAddressInputSubmitted(string input)
        {
            Log($"Address input submitted: {input}");
            
            if (!string.IsNullOrWhiteSpace(input))
            {
                LoadAddress(input);
            }
        }

        private void LoadSpecificAddress(string address)
        {
            Log($"Quick loading address: {address}");
            
            if (addressInputField)
            {
                addressInputField.text = address;
            }
            
            LoadAddress(address);
        }

        #endregion

        #region Map Loading

        private void LoadAddress(string address)
        {
            Log($"Loading address: {address}");
            
            UpdateStatus($"Lade Karte für: {address}...");
            
            if (mapController)
            {
                mapController.LoadMapFromAddress(address);
            }
            else
            {
                LogWarning("No MapStartupController available!");
                UpdateStatus("Fehler: Kein MapController verfügbar");
                
                // Fallback: Load procedural level
                StartCoroutine(FallbackToProcedural());
            }
        }

        private void LoadCoordinates(double lat, double lon, string locationName)
        {
            Log($"Loading coordinates: {lat}, {lon} ({locationName})");
            
            UpdateStatus($"Lade Karte für: {locationName}...");
            
            if (mapController)
            {
                mapController.LoadMapFromCoordinates((float)lat, (float)lon);
            }
            else
            {
                LogWarning("No MapStartupController available!");
                StartCoroutine(FallbackToProcedural());
            }
        }

        private IEnumerator FallbackToProcedural()
        {
            UpdateStatus("Lade Ersatz-Level...");
            yield return new WaitForSeconds(1f);
            
            // Load a procedural level as fallback
            UnityEngine.SceneManagement.SceneManager.LoadScene("GeneratedLevel");
        }

        private void UpdateStatus(string message)
        {
            Log($"Status: {message}");
            
            if (statusText)
            {
                statusText.text = message;
            }
            
            if (uiController)
            {
                uiController.ShowNotification(message, 3f);
            }
        }

        #endregion

        #region Utility

        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[OSMUIConnector] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[OSMUIConnector] {message}");
        }

        /// <summary>
        /// Add this component to OSM scenes
        /// </summary>
        [ContextMenu("Add to Current Scene")]
        public static void AddToCurrentScene()
        {
            if (FindFirstObjectByType<OSMUIConnector>() == null)
            {
                GameObject connectorGO = new GameObject("OSMUIConnector");
                connectorGO.AddComponent<OSMUIConnector>();
                Debug.Log("Added OSMUIConnector to current scene");
            }
            else
            {
                Debug.Log("OSMUIConnector already exists in scene");
            }
        }

        #endregion
    }
}
