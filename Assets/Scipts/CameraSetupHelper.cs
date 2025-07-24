using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Automatisches Setup für eine funktionierende Kamera in prozeduralen Leveln
/// Löst das "No Cameras rendering" Problem
/// </summary>
public class CameraSetupHelper : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnAwake = true;
    [SerializeField] private bool findExistingCamera = true;
    
    [Header("Camera Configuration")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0, 10, -15);
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool lookAtTarget = true;

    void Awake()
    {
        if (setupOnAwake)
        {
            SetupCamera();
        }
    }

    /// <summary>
    /// Erstellt oder konfiguriert eine Main Camera für das Spiel
    /// </summary>
    public void SetupCamera()
    {
        Camera mainCamera = null;
        
        // 1. Versuche existierende Main Camera zu finden
        if (findExistingCamera)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Suche nach beliebiger Kamera in der Szene
                Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cameras.Length > 0)
                {
                    mainCamera = cameras[0];
                    mainCamera.tag = "MainCamera"; // Setze MainCamera Tag
                    Debug.Log($"CameraSetupHelper: Existing camera '{mainCamera.name}' tagged as MainCamera");
                }
            }
        }

        // 2. Erstelle neue Kamera falls keine existiert
        if (mainCamera == null)
        {
            GameObject cameraGO = new GameObject("Main Camera");
            mainCamera = cameraGO.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            
            // Standard Kamera-Einstellungen
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.backgroundColor = Color.black;
            mainCamera.cullingMask = -1; // Render everything
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60f;
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;
            
            // Audio Listener hinzufügen
            if (cameraGO.GetComponent<AudioListener>() == null)
            {
                cameraGO.AddComponent<AudioListener>();
            }
            
            Debug.Log("CameraSetupHelper: Created new Main Camera");
        }

        // 3. CameraController hinzufügen/konfigurieren
        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = mainCamera.gameObject.AddComponent<CameraController>();
            Debug.Log("CameraSetupHelper: Added CameraController to camera");
        }

        // 4. Kamera für prozedurales Level konfigurieren
        ConfigureCameraForProceduralLevel(mainCamera, cameraController);

        // 5. Kamera-Position setzen
        PositionCameraForLevel(mainCamera);
    }

    private void ConfigureCameraForProceduralLevel(Camera camera, CameraController controller)
    {
        // CameraController über Reflection konfigurieren (da Felder private sind)
        var controllerType = typeof(CameraController);
        
        // Setze Offset
        SetPrivateField(controller, "offset", defaultOffset);
        SetPrivateField(controller, "followSpeed", followSpeed);
        SetPrivateField(controller, "lookAtTarget", lookAtTarget);
        SetPrivateField(controller, "autoFindPlayer", true);
        SetPrivateField(controller, "smoothMovement", true);
        SetPrivateField(controller, "minHeight", 2f);
        SetPrivateField(controller, "maxHeight", 50f);
        SetPrivateField(controller, "minDistance", 5f);
        SetPrivateField(controller, "maxDistance", 30f);

        Debug.Log("CameraSetupHelper: Configured CameraController for procedural level");
    }

    private void PositionCameraForLevel(Camera camera)
    {
        // Finde LevelGenerator für Level-Center
        LevelGenerator levelGenerator = FindFirstObjectByType<LevelGenerator>();
        Vector3 levelCenter = Vector3.zero;
        
        if (levelGenerator != null && levelGenerator.ActiveProfile != null)
        {
            // Berechne Level-Center basierend auf LevelProfile
            LevelProfile profile = levelGenerator.ActiveProfile;
            float levelSize = profile.LevelSize * profile.TileSize;
            levelCenter = new Vector3(levelSize * 0.5f, 0, levelSize * 0.5f);
        }

        // Finde Player für Target
        PlayerController player = FindFirstObjectByType<PlayerController>();
        GameObject playerGO = null;
        
        if (player != null)
        {
            playerGO = player.gameObject;
        }
        else
        {
            // Suche nach GameObject mit "Player" Tag
            playerGO = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerGO != null)
        {
            // Positioniere Kamera relativ zum Player
            Vector3 cameraPos = playerGO.transform.position + defaultOffset;
            camera.transform.position = cameraPos;
            
            // Schaue zum Player
            if (lookAtTarget)
            {
                camera.transform.LookAt(playerGO.transform.position + Vector3.up);
            }
            
            Debug.Log($"CameraSetupHelper: Positioned camera relative to player at {cameraPos}");
        }
        else
        {
            // Fallback: Positioniere über Level-Center
            Vector3 fallbackPos = levelCenter + defaultOffset;
            camera.transform.position = fallbackPos;
            camera.transform.LookAt(levelCenter);
            
            Debug.Log($"CameraSetupHelper: Positioned camera over level center at {fallbackPos}");
        }
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"CameraSetupHelper: Field '{fieldName}' not found in {obj.GetType().Name}");
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Setup Camera Now")]
    public void SetupCameraEditor()
    {
        SetupCamera();
        EditorUtility.SetDirty(this);
        Debug.Log("CameraSetupHelper: Manual camera setup completed");
    }

    [MenuItem("Roll-a-Ball/Fix Camera Setup")]
    public static void FixCameraSetupStatic()
    {
        CameraSetupHelper helper = FindFirstObjectByType<CameraSetupHelper>();
        if (helper == null)
        {
            GameObject helperGO = new GameObject("CameraSetupHelper");
            helper = helperGO.AddComponent<CameraSetupHelper>();
        }
        
        helper.SetupCamera();
        Debug.Log("CameraSetupHelper: Fixed camera setup via menu");
    }
    #endif

    /// <summary>
    /// Überprüft, ob eine funktionierende Kamera vorhanden ist
    /// </summary>
    public bool ValidateCameraSetup()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("CameraSetupHelper: No Main Camera found!");
            return false;
        }

        if (!mainCamera.enabled)
        {
            Debug.LogError("CameraSetupHelper: Main Camera is disabled!");
            return false;
        }

        if (mainCamera.cullingMask == 0)
        {
            Debug.LogWarning("CameraSetupHelper: Main Camera culling mask is empty!");
            return false;
        }

        Debug.Log("CameraSetupHelper: Camera setup is valid");
        return true;
    }

    void Start()
    {
        // Validiere Setup nach der Initialisierung
        Invoke(nameof(ValidateCameraSetup), 0.5f);
    }
}
