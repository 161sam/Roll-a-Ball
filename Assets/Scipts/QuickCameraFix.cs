using UnityEngine;

/// <summary>
/// Schnelle Kamera-Fix für \"No Cameras rendering\" Problem
/// Einfach als Script zu einem GameObject hinzufügen und Play drücken
/// </summary>
public class QuickCameraFix : MonoBehaviour
{
    void Start()
    {
        FixCameraNow();
    }

    public void FixCameraNow()
    {
        // 1. Überprüfe, ob Main Camera existiert
        Camera mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.Log("QuickCameraFix: No Main Camera found, creating one...");
            
            // Erstelle neue Main Camera
            GameObject cameraGO = new GameObject("Main Camera");
            mainCamera = cameraGO.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            
            // Audio Listener hinzufügen
            cameraGO.AddComponent<AudioListener>();
            
            // Standard Position (über dem Spielfeld)
            cameraGO.transform.position = new Vector3(10, 15, -10);
            cameraGO.transform.LookAt(Vector3.zero);
            
            Debug.Log("QuickCameraFix: Main Camera created and positioned!");
        }
        else
        {
            Debug.Log("QuickCameraFix: Main Camera already exists");
        }
        
        // 2. Aktiviere die Kamera falls deaktiviert
        if (!mainCamera.enabled)
        {
            mainCamera.enabled = true;
            Debug.Log("QuickCameraFix: Main Camera enabled");
        }
        
        // 3. Überprüfe Audio Listener
        AudioListener listener = mainCamera.GetComponent<AudioListener>();
        if (listener == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
            Debug.Log("QuickCameraFix: Audio Listener added");
        }
        
        // 4. Setze Standard-Kamera-Einstellungen
        mainCamera.clearFlags = CameraClearFlags.Skybox;
        mainCamera.cullingMask = -1; // Render everything
        mainCamera.fieldOfView = 60f;
        mainCamera.nearClipPlane = 0.3f;
        mainCamera.farClipPlane = 1000f;
        
        // 5. Füge CameraController hinzu falls nicht vorhanden
        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = mainCamera.gameObject.AddComponent<CameraController>();
            Debug.Log("QuickCameraFix: CameraController added");
        }
        
        // 6. Positioniere Kamera für aktuelles Level
        PositionCameraForCurrentLevel(mainCamera);
        
        Debug.Log("✅ QuickCameraFix: Camera setup complete! You should now see the game.");
    }
    
    private void PositionCameraForCurrentLevel(Camera camera)
    {
        // Finde Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController)
                player = playerController.gameObject;
        }
        
        if (player != null)
        {
            // Positioniere Kamera relativ zum Player
            Vector3 offset = new Vector3(0, 10, -15);
            camera.transform.position = player.transform.position + offset;
            camera.transform.LookAt(player.transform.position + Vector3.up);
            Debug.Log($"QuickCameraFix: Camera positioned relative to player at {player.transform.position}");
        }
        else
        {
            // Fallback: Standard-Position
            camera.transform.position = new Vector3(10, 15, -10);
            camera.transform.LookAt(Vector3.zero);
            Debug.Log("QuickCameraFix: Camera positioned at fallback location");
        }
    }
}
