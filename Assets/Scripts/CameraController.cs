using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Camera Position")]
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -15);
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Look Behavior")]
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookOffset = Vector3.up;

    [Header("Movement Constraints")]
    [SerializeField] private bool smoothMovement = true;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 30f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeIntensity = 0f;
    [SerializeField] private float shakeDuration = 0f;

    [Header("Height Constraints")]
    [SerializeField] private float minHeight = 2f;
    [SerializeField] private float maxHeight = 50f;

    // Private fields
    private Vector3 velocity;
    private Vector3 shakeOffset;
    private float shakeTimer;
    private Camera cam;

    void Start()
    {
        Initialize();
    }

    void LateUpdate()
    {
        // Retry finding player if not found yet
        if (!target && autoFindPlayer)
        {
            Initialize();
        }
        
        if (!target) return;

        UpdateCameraPosition();
        UpdateCameraRotation();
        UpdateCameraShake();
    }

    private void Initialize()
    {
        cam = GetComponent<Camera>();
        
        // Auto-find player if not assigned
        if (autoFindPlayer && !target)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player)
            {
                target = player.transform;
                Debug.Log("CameraController: Auto-found player target");
            }
            else
            {
                // Try alternative search methods
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj)
                {
                    target = playerObj.transform;
                    Debug.Log("CameraController: Found player by tag");
                }
                else
                {
                    // Look for any object with "Player" in the name
                    GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.name.ToLower().Contains("player"))
                        {
                            target = obj.transform;
                            Debug.Log($"CameraController: Found player by name: {obj.name}");
                            break;
                        }
                    }
                    
                    if (!target)
                    {
                        Debug.LogWarning("CameraController: No player found! Camera will wait for manual assignment.");
                    }
                }
            }
        }

        // Set initial position if target exists
        if (target)
        {
            Vector3 initialPos = target.position + offset;
            initialPos.y = Mathf.Clamp(initialPos.y, minHeight, maxHeight);
            transform.position = initialPos;
        }
    }

    private void UpdateCameraPosition()
    {
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Apply height constraints
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minHeight, maxHeight);
        
        // Apply distance constraints
        Vector3 directionToTarget = (target.position - desiredPosition).normalized;
        float distanceToTarget = Vector3.Distance(desiredPosition, target.position);
        
        if (distanceToTarget < minDistance)
        {
            desiredPosition = target.position - directionToTarget * minDistance;
        }
        else if (distanceToTarget > maxDistance)
        {
            desiredPosition = target.position - directionToTarget * maxDistance;
        }

        // Apply camera shake
        Vector3 finalPosition = desiredPosition + shakeOffset;

        // Move camera
        if (smoothMovement)
        {
            transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref velocity, 1f / followSpeed);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, finalPosition, followSpeed * Time.deltaTime);
        }
    }

    private void UpdateCameraRotation()
    {
        if (!lookAtTarget) return;

        Vector3 lookPosition = target.position + lookOffset;
        Vector3 direction = (lookPosition - transform.position).normalized;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateCameraShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            
            // Generate random shake offset
            shakeOffset = Random.insideUnitSphere * shakeIntensity;
            shakeOffset.z = 0; // Keep shake in screen space
            
            if (shakeTimer <= 0)
        {
                shakeOffset = Vector3.zero;
                shakeIntensity = 0f;
            }
        }
    }

    // ===== Public Methods =====
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    public void ShakeCamera(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    public void StopShake()
    {
        shakeTimer = 0f;
        shakeIntensity = 0f;
        shakeOffset = Vector3.zero;
    }

    public void SetFollowSpeed(float speed)
    {
        followSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(0.1f, speed);
    }

    // ===== Utility Methods =====

    public Vector3 GetWorldPointFromScreenPoint(Vector2 screenPoint)
    {
        if (!cam) return Vector3.zero;
        
        Ray ray = cam.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }
        
        return Vector3.zero;
    }

    public bool IsTargetVisible()
    {
        if (!target || !cam) return false;

        Vector3 screenPoint = cam.WorldToScreenPoint(target.position);
        return screenPoint.z > 0 && 
               screenPoint.x >= 0 && screenPoint.x <= cam.pixelWidth &&
               screenPoint.y >= 0 && screenPoint.y <= cam.pixelHeight;
    }

    // ===== Debug Visualization =====
    
    void OnDrawGizmosSelected()
    {
        if (!target) return;

        // Draw offset position
        Gizmos.color = Color.blue;
        Vector3 targetPos = target.position + offset;
        Gizmos.DrawWireCube(targetPos, Vector3.one * 0.5f);
        Gizmos.DrawLine(target.position, targetPos);

        // Draw distance constraints
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position, minDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, maxDistance);

        // Draw camera view direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);

        // Draw look target
        if (lookAtTarget)
        {
            Vector3 lookPos = target.position + lookOffset;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(lookPos, Vector3.one * 0.3f);
            Gizmos.DrawLine(transform.position, lookPos);
        }
    }

    // ===== Integration with Events =====

    void OnEnable()
    {
        // Subscribe to player events if available
        if (target)
        {
            PlayerController player = target.GetComponent<PlayerController>();
            if (player)
            {
                player.OnFlyingChanged += OnPlayerFlyingChanged;
            }
        }
    }

    void OnDisable()
    {
        // Unsubscribe from events
        if (target)
        {
            PlayerController player = target.GetComponent<PlayerController>();
            if (player)
            {
                player.OnFlyingChanged -= OnPlayerFlyingChanged;
            }
        }
    }

    private void OnPlayerFlyingChanged(bool isFlying)
    {
        // Adjust camera behavior when player is flying
        if (isFlying)
        {
            followSpeed = Mathf.Max(followSpeed, 3f); // Faster follow when flying
        }
        else
        {
            followSpeed = 5f; // Normal follow speed
        }
    }

    // ===== Camera Effects =====

    public void FocusOnTarget(float duration = 1f)
    {
        StartCoroutine(FocusCoroutine(duration));
    }

    private System.Collections.IEnumerator FocusCoroutine(float duration)
    {
        Vector3 startOffset = offset;
        Vector3 focusOffset = startOffset * 0.7f; // Get closer
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Smooth focus in and out
            float focusAmount = Mathf.Sin(t * Mathf.PI);
            offset = Vector3.Lerp(startOffset, focusOffset, focusAmount);
            
            yield return null;
        }

        offset = startOffset;
    }
}
