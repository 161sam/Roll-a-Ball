using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RollABall.Map;

namespace RollABall.Editor
{
    /// <summary>
    /// Automated setup for Level_OSM.unity scene
    /// Creates all required components for OSM integration
    /// </summary>
    public class OSMAutoSetup : EditorWindow
    {
        [MenuItem("Roll-a-Ball/🔧 Auto-Setup OSM Scene", priority = 2)]
        public static void AutoSetupOSMScene()
        {
            Debug.Log("🔧 Starting Auto-Setup of OSM Scene...");
            
            // Open Level_OSM scene
            string scenePath = "Assets/Scenes/Level_OSM.unity";
            try
            {
                EditorSceneManager.OpenScene(scenePath);
                Debug.Log("✅ Opened Level_OSM.unity scene");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to open Level_OSM scene: {e.Message}");
                return;
            }
            
            // Setup all components
            SetupOSMComponents();
            SetupOSMUI();
            SetupOSMCamera();
            SetupOSMEnvironment();
            
            // Save scene
            EditorSceneManager.SaveOpenScenes();
            
            Debug.Log("🎉 OSM Scene Auto-Setup completed successfully!");
            EditorUtility.DisplayDialog("Auto-Setup Complete", 
                "Level_OSM.unity has been automatically configured!\n\n" +
                "✅ All OSM components added\n" +
                "✅ UI system created\n" +
                "✅ Camera configured\n" +
                "✅ Environment setup\n" +
                "✅ Scene saved\n\n" +
                "The OSM system is now ready for use!", "OK");
        }
        
        private static void SetupOSMComponents()
        {
            Debug.Log("🗺️ Setting up OSM Components...");
            
            // Create MapStartupController
            GameObject mapController = new GameObject("MapStartupController");
            mapController.AddComponent<MapStartupController>();
            mapController.AddComponent<AddressResolver>();
            mapController.AddComponent<MapGenerator>();
            
            // Create GameManager
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
            
            // Create LevelManager
            GameObject levelManager = new GameObject("LevelManager");
            levelManager.AddComponent<LevelManager>();
            
            Debug.Log("✅ OSM Components created");
        }
        
        private static void SetupOSMUI()
        {
            Debug.Log("🖥️ Setting up OSM UI...");
            
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Create Address Input Panel
            GameObject addressPanel = new GameObject("AddressInputPanel");
            addressPanel.transform.SetParent(canvasObj.transform);
            RectTransform addressRect = addressPanel.AddComponent<RectTransform>();
            Image addressImage = addressPanel.AddComponent<Image>();
            addressImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Position address panel (top of screen)
            addressRect.anchorMin = new Vector2(0.1f, 0.7f);
            addressRect.anchorMax = new Vector2(0.9f, 0.95f);
            addressRect.offsetMin = Vector2.zero;
            addressRect.offsetMax = Vector2.zero;
            
            // Create Address Input Field
            GameObject inputField = new GameObject("AddressInputField");
            inputField.transform.SetParent(addressPanel.transform);
            RectTransform inputRect = inputField.AddComponent<RectTransform>();
            Image inputImage = inputField.AddComponent<Image>();
            inputImage.color = Color.white;
            TMP_InputField inputComponent = inputField.AddComponent<TMP_InputField>();
            
            // Position input field
            inputRect.anchorMin = new Vector2(0.05f, 0.2f);
            inputRect.anchorMax = new Vector2(0.65f, 0.8f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            
            // Create input text
            GameObject inputText = new GameObject("Text");
            inputText.transform.SetParent(inputField.transform);
            RectTransform textRect = inputText.AddComponent<RectTransform>();
            TextMeshProUGUI textComponent = inputText.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.color = Color.black;
            textComponent.fontSize = 16;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            inputComponent.textComponent = textComponent;
            
            // Create placeholder
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(inputField.transform);
            RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Gib eine Adresse ein (z.B. Leipzig, Markt)";
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.fontSize = 16;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 5);
            placeholderRect.offsetMax = new Vector2(-10, -5);
            inputComponent.placeholder = placeholderText;
            
            // Create Load Button
            GameObject loadButton = new GameObject("LoadMapButton");
            loadButton.transform.SetParent(addressPanel.transform);
            RectTransform buttonRect = loadButton.AddComponent<RectTransform>();
            Button buttonComponent = loadButton.AddComponent<Button>();
            Image buttonImage = loadButton.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
            
            // Position button
            buttonRect.anchorMin = new Vector2(0.7f, 0.2f);
            buttonRect.anchorMax = new Vector2(0.95f, 0.8f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            // Create button text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(loadButton.transform);
            RectTransform buttonTextRect = buttonText.AddComponent<RectTransform>();
            TextMeshProUGUI buttonTextComponent = buttonText.AddComponent<TextMeshProUGUI>();
            buttonTextComponent.text = "Karte laden";
            buttonTextComponent.color = Color.white;
            buttonTextComponent.fontSize = 14;
            buttonTextComponent.alignment = TextAlignmentOptions.Center;
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            // Create Loading Panel (initially hidden)
            GameObject loadingPanel = new GameObject("LoadingPanel");
            loadingPanel.transform.SetParent(canvasObj.transform);
            RectTransform loadingRect = loadingPanel.AddComponent<RectTransform>();
            Image loadingImage = loadingPanel.AddComponent<Image>();
            loadingImage.color = new Color(0, 0, 0, 0.8f);
            loadingPanel.SetActive(false);
            
            // Full screen loading panel
            loadingRect.anchorMin = Vector2.zero;
            loadingRect.anchorMax = Vector2.one;
            loadingRect.offsetMin = Vector2.zero;
            loadingRect.offsetMax = Vector2.zero;
            
            // Create loading text
            GameObject loadingText = new GameObject("LoadingText");
            loadingText.transform.SetParent(loadingPanel.transform);
            RectTransform loadingTextRect = loadingText.AddComponent<RectTransform>();
            TextMeshProUGUI loadingTextComponent = loadingText.AddComponent<TextMeshProUGUI>();
            loadingTextComponent.text = "Lade Karte aus OpenStreetMap...";
            loadingTextComponent.color = Color.white;
            loadingTextComponent.fontSize = 24;
            loadingTextComponent.alignment = TextAlignmentOptions.Center;
            loadingTextRect.anchorMin = new Vector2(0.2f, 0.4f);
            loadingTextRect.anchorMax = new Vector2(0.8f, 0.6f);
            loadingTextRect.offsetMin = Vector2.zero;
            loadingTextRect.offsetMax = Vector2.zero;
            
            // Create Game UI Panel for standard Roll-a-Ball UI
            GameObject gameUIPanel = new GameObject("GameUIPanel");
            gameUIPanel.transform.SetParent(canvasObj.transform);
            RectTransform gameUIRect = gameUIPanel.AddComponent<RectTransform>();
            gameUIRect.anchorMin = new Vector2(0, 0);
            gameUIRect.anchorMax = new Vector2(1, 0.2f);
            gameUIRect.offsetMin = Vector2.zero;
            gameUIRect.offsetMax = Vector2.zero;
            
            // Create Collectible Counter
            GameObject collectibleText = new GameObject("CollectibleText");
            collectibleText.transform.SetParent(gameUIPanel.transform);
            RectTransform collectibleRect = collectibleText.AddComponent<RectTransform>();
            TextMeshProUGUI collectibleComponent = collectibleText.AddComponent<TextMeshProUGUI>();
            collectibleComponent.text = "Collectibles: 0 / 0";
            collectibleComponent.color = Color.white;
            collectibleComponent.fontSize = 18;
            collectibleRect.anchorMin = new Vector2(0.05f, 0.5f);
            collectibleRect.anchorMax = new Vector2(0.5f, 1f);
            collectibleRect.offsetMin = Vector2.zero;
            collectibleRect.offsetMax = Vector2.zero;
            
            // Create Fly Energy Bar
            GameObject flyText = new GameObject("FlyText");
            flyText.transform.SetParent(gameUIPanel.transform);
            RectTransform flyTextRect = flyText.AddComponent<RectTransform>();
            TextMeshProUGUI flyTextComponent = flyText.AddComponent<TextMeshProUGUI>();
            flyTextComponent.text = "Fly Energy:";
            flyTextComponent.color = Color.white;
            flyTextComponent.fontSize = 16;
            flyTextRect.anchorMin = new Vector2(0.55f, 0.6f);
            flyTextRect.anchorMax = new Vector2(0.8f, 1f);
            flyTextRect.offsetMin = Vector2.zero;
            flyTextRect.offsetMax = Vector2.zero;
            
            GameObject flyBar = new GameObject("FlyBar");
            flyBar.transform.SetParent(gameUIPanel.transform);
            RectTransform flyBarRect = flyBar.AddComponent<RectTransform>();
            Slider flyBarComponent = flyBar.AddComponent<Slider>();
            Image flyBarImage = flyBar.AddComponent<Image>();
            flyBarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            flyBarRect.anchorMin = new Vector2(0.55f, 0.2f);
            flyBarRect.anchorMax = new Vector2(0.95f, 0.5f);
            flyBarRect.offsetMin = Vector2.zero;
            flyBarRect.offsetMax = Vector2.zero;
            
            // Setup slider components
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(flyBar.transform);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f, 1f); // Blue energy color
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            flyBarComponent.fillRect = fillRect;
            flyBarComponent.value = 1f; // Start with full energy
            
            Debug.Log("✅ OSM UI created");
        }
        
        private static void SetupOSMCamera()
        {
            Debug.Log("📷 Setting up OSM Camera...");
            
            // Create Main Camera
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera camera = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            cameraObj.AddComponent<CameraController>();
            
            // Position camera for OSM overview
            cameraObj.transform.position = new Vector3(0, 15, -10);
            cameraObj.transform.rotation = Quaternion.Euler(35, 0, 0);
            
            // Camera settings optimized for OSM
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.2f, 0.4f, 0.6f, 1f);
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 2000f; // Extended for large OSM areas
            
            Debug.Log("✅ OSM Camera created");
        }
        
        private static void SetupOSMEnvironment()
        {
            Debug.Log("🌍 Setting up OSM Environment...");
            
            // Create Ground Plane as fallback
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FallbackGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(20, 1, 20); // Large ground plane
            
            // Create ground material
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                Material groundMat = new Material(Shader.Find("Standard"));
                groundMat.color = new Color(0.4f, 0.6f, 0.4f, 1f); // Green ground
                groundMat.SetFloat("_Metallic", 0f);
                groundMat.SetFloat("_Glossiness", 0.2f);
                groundRenderer.material = groundMat;
            }
            
            // Create Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.position = new Vector3(0, 10, 0);
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Setup ambient lighting for OSM
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1.0f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            RenderSettings.fog = false; // Disable fog for better OSM visibility
            
            // Create spawn point for player
            GameObject spawnPoint = new GameObject("PlayerSpawnPoint");
            spawnPoint.transform.position = new Vector3(0, 1, 0);
            spawnPoint.tag = "Respawn";
            
            Debug.Log("✅ OSM Environment setup complete");
        }
    }
}
