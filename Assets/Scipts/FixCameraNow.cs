using UnityEngine;

/// <summary>
/// ULTIMATIVE LÖSUNG: Einfach zu einem GameObject hinzufügen und Play drücken!
/// Repariert automatisch alle Kamera-Probleme in der aktuellen Szene
/// </summary>
public class FixCameraNow : MonoBehaviour
{
    [Header("Quick Fix Settings")]
    [SerializeField] private bool fixOnStart = true;
    [SerializeField] private bool addCameraController = true;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 12, -15);

    void Start()
    {
        if (fixOnStart)
        {
            FixEverything();
        }
    }

    [ContextMenu("Fix Camera NOW!")]
    public void FixEverything()
    {
        Debug.Log("🔧 FixCameraNow: Starting camera repair...");

        // 1. Main Camera erstellen/finden
        Camera mainCamera = EnsureMainCamera();
        
        // 2. AudioListener hinzufügen
        EnsureAudioListener(mainCamera.gameObject);
        
        // 3. CameraController hinzufügen (optional)
        if (addCameraController)
        {
            EnsureCameraController(mainCamera.gameObject);
        }
        
        // 4. Kamera positionieren
        PositionCamera(mainCamera);
        
        Debug.Log("✅ FixCameraNow: Camera fix complete! Game should be visible now.");
    }

    private Camera EnsureMainCamera()
    {
        // Suche nach existierender Main Camera
        Camera mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            // Suche nach beliebiger Kamera
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (allCameras.Length > 0)
            {
                mainCamera = allCameras[0];
                mainCamera.tag = "MainCamera";
                Debug.Log($"📷 Tagged existing camera '{mainCamera.name}' as MainCamera");
            }
        }

        if (mainCamera == null)
        {
            // Erstelle neue Main Camera
            GameObject cameraGO = new GameObject("Main Camera");
            mainCamera = cameraGO.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            
            // Standard Kamera-Settings
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.backgroundColor = Color.black;
            mainCamera.cullingMask = -1; // Render everything
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60f;
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;
            
            Debug.Log("📷 Created new Main Camera");
        }

        // Stelle sicher, dass die Kamera aktiviert ist
        mainCamera.enabled = true;
        mainCamera.gameObject.SetActive(true);

        return mainCamera;
    }

    private void EnsureAudioListener(GameObject cameraGO)
    {
        AudioListener listener = cameraGO.GetComponent<AudioListener>();
        if (listener == null)
        {
            // Entferne andere AudioListener um Konflikte zu vermeiden
            AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener oldListener in allListeners)
            {
                if (oldListener.gameObject != cameraGO)
                {
                    DestroyImmediate(oldListener);
                    Debug.Log($"🔊 Removed duplicate AudioListener from {oldListener.gameObject.name}");
                }
            }

            cameraGO.AddComponent<AudioListener>();
            Debug.Log("🔊 Added AudioListener to camera");
        }
    }

    private void EnsureCameraController(GameObject cameraGO)
    {
        CameraController controller = cameraGO.GetComponent<CameraController>();
        if (controller == null)
        {
            controller = cameraGO.AddComponent<CameraController>();
            Debug.Log("🎮 Added CameraController to camera");
        }
    }

    private void PositionCamera(Camera camera)
    {
        // Finde Player für bessere Positionierung
        GameObject player = FindPlayer();
        
        if (player != null)
        {
            // Positioniere relativ zum Player
            Vector3 playerPos = player.transform.position;
            camera.transform.position = playerPos + cameraOffset;
            camera.transform.LookAt(playerPos + Vector3.up);
            Debug.Log($"📍 Positioned camera relative to player: {camera.transform.position}");
        }
        else
        {
            // Fallback: Standard-Position
            camera.transform.position = new Vector3(10, 12, -15);
            camera.transform.LookAt(new Vector3(10, 0, 10));
            Debug.Log($"📍 Positioned camera at fallback location: {camera.transform.position}");
        }
    }

    private GameObject FindPlayer()
    {
        // Versuche verschiedene Wege, den Player zu finden
        GameObject player = null;

        // 1. Nach Tag suchen
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player;

        // 2. Nach PlayerController suchen
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null) return playerController.gameObject;

        // 3. Nach Namen suchen
        player = GameObject.Find("Player");
        if (player != null) return player;

        // 4. Nach Namen-Patterns suchen
        string[] playerNames = { "Ball", "PlayerBall", "RollaBall", "Sphere" };
        foreach (string name in playerNames)
        {
            player = GameObject.Find(name);
            if (player != null) return player;
        }

        return null;
    }

    /// <summary>
    /// Teste, ob die Kamera korrekt funktioniert
    /// </summary>
    [ContextMenu("Test Camera")]
    public void TestCamera()
    {
        Camera mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("❌ No Main Camera found!");
            return;
        }

        if (!mainCamera.enabled)
        {
            Debug.LogError("❌ Main Camera is disabled!");
            return;
        }

        if (mainCamera.cullingMask == 0)
        {
            Debug.LogWarning("⚠️ Main Camera culling mask is 0 - might not render anything!");
        }

        AudioListener listener = mainCamera.GetComponent<AudioListener>();
        if (listener == null)
        {
            Debug.LogWarning("⚠️ No AudioListener on Main Camera - no audio!");
        }

        Debug.Log($"✅ Camera test passed! Camera: {mainCamera.name} at {mainCamera.transform.position}");
    }

    /// <summary>
    /// Debug-Info ausgeben
    /// </summary>
    void Update()
    {
        // Einmalige Info beim ersten Frame
        if (Time.frameCount == 1)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Debug.Log($"🎬 Frame 1: Main Camera '{mainCamera.name}' is rendering");
            }
            else
            {
                Debug.LogError("🎬 Frame 1: Still no Main Camera found!");
            }
        }
    }
}
