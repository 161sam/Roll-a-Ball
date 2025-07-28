using UnityEngine;
using UnityEngine.UI;

// TODO-REMOVE#6: Obsolete setup script – no longer needed and may corrupt current data
/// <summary>
/// Complete Scene Setup - Erstellt eine vollständige Roll-a-Ball Szene
/// Kann sowohl zur Laufzeit als auch im Editor verwendet werden
/// </summary>
public class CompleteSceneSetup : MonoBehaviour
{
    [Header("Setup Configuration")]
    public bool setupOnStart = false;
    public bool includeUI = true;
    public bool includeLevelGenerator = true;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupCompleteScene();
        }
    }
    
    /// <summary>
    /// Hauptmethode zum Erstellen einer kompletten Szene
    /// </summary>
    public void SetupCompleteScene()
    {
        Debug.Log("🚀 Starting Complete Scene Setup...");
        
        SetupPlayer();
        SetupCamera();
        SetupGameManager();
        SetupLevelManager();
        
        if (includeUI)
        {
            SetupCompleteUI();
        }
        
        if (includeLevelGenerator)
        {
            SetupLevelGenerator();
        }
        
        SetupLighting();
        
        Debug.Log("✅ Complete Scene Setup finished!");
    }
    
    private void SetupPlayer()
    {
        Debug.Log("👤 Setting up Player...");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player";
            player.tag = "Player";
        }
        
        // Configure player properties
        player.transform.localScale = Vector3.one * 0.5f;
        player.transform.position = Vector3.zero;
        
        // Add/Configure Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = player.AddComponent<Rigidbody>();
        }
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.3f;
        
        // Add PlayerController
        if (player.GetComponent<PlayerController>() == null)
        {
            player.AddComponent<PlayerController>();
        }
        
        Debug.Log("✅ Player setup complete!");
    }
    
    private void SetupCamera()
    {
        Debug.Log("📷 Setting up Camera...");
        
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            mainCam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }
        
        // Position camera
        mainCam.transform.position = new Vector3(0, 10, -10);
        mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);
        
        // Add CameraController
        if (mainCam.GetComponent<CameraController>() == null)
        {
            mainCam.gameObject.AddComponent<CameraController>();
        }
        
        Debug.Log("✅ Camera setup complete!");
    }
    
    private void SetupGameManager()
    {
        Debug.Log("🎮 Setting up GameManager...");
        
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            GameObject gmGO = new GameObject("GameManager");
            gameManager = gmGO.AddComponent<GameManager>();
        }
        
        Debug.Log("✅ GameManager setup complete!");
    }
    
    private void SetupLevelManager()
    {
        Debug.Log("🏗️ Setting up LevelManager...");
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
        {
            GameObject lmGO = new GameObject("LevelManager");
            levelManager = lmGO.AddComponent<LevelManager>();
        }
        
        Debug.Log("✅ LevelManager setup complete!");
    }
    
    private void SetupCompleteUI()
    {
        Debug.Log("🖥️ Setting up UI...");
        
        // Create Canvas if it doesn't exist
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create EventSystem if it doesn't exist
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // Add UIController
        if (canvas.GetComponent<UIController>() == null)
        {
            canvas.gameObject.AddComponent<UIController>();
        }
        
        // Create basic UI elements
        CreateBasicUIElements(canvas);
        
        Debug.Log("✅ UI setup complete!");
    }
    
    private void CreateBasicUIElements(Canvas canvas)
    {
        // Score Text
        if (canvas.transform.Find("ScoreText") == null)
        {
            GameObject scoreTextGO = new GameObject("ScoreText");
            scoreTextGO.transform.SetParent(canvas.transform, false);
            
            Text scoreText = scoreTextGO.AddComponent<Text>();
            scoreText.text = "Score: 0";
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 24;
            scoreText.color = Color.white;
            
            RectTransform rt = scoreText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(100, -50);
            rt.sizeDelta = new Vector2(200, 50);
        }
        
        // Collectibles Text
        if (canvas.transform.Find("CollectiblesText") == null)
        {
            GameObject collectiblesTextGO = new GameObject("CollectiblesText");
            collectiblesTextGO.transform.SetParent(canvas.transform, false);
            
            Text collectiblesText = collectiblesTextGO.AddComponent<Text>();
            collectiblesText.text = "Collectibles: 0 / 0";
            collectiblesText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            collectiblesText.fontSize = 20;
            collectiblesText.color = Color.white;
            
            RectTransform rt = collectiblesText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(100, -100);
            rt.sizeDelta = new Vector2(250, 50);
        }
    }
    
    private void SetupLevelGenerator()
    {
        Debug.Log("🎲 Setting up LevelGenerator...");
        
        LevelGenerator levelGenerator = FindFirstObjectByType<LevelGenerator>();
        if (levelGenerator == null)
        {
            GameObject lgGO = new GameObject("LevelGenerator");
            levelGenerator = lgGO.AddComponent<LevelGenerator>();
        }
        
        Debug.Log("✅ LevelGenerator setup complete!");
    }
    
    private void SetupLighting()
    {
        Debug.Log("💡 Setting up Lighting...");
        
        // Ensure there's a directional light
        Light dirLight = FindFirstObjectByType<Light>();
        if (dirLight == null || dirLight.type != LightType.Directional)
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
        
        Debug.Log("✅ Lighting setup complete!");
    }
    
    /// <summary>
    /// Cleanup method to remove this setup component after use
    /// </summary>
    public void CleanupAfterSetup()
    {
        Debug.Log("🧹 Cleaning up CompleteSceneSetup component...");
        
        if (Application.isPlaying)
        {
            Destroy(this.gameObject);
        }
        else
        {
            DestroyImmediate(this.gameObject);
        }
    }
}
