using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Ziel")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Offset-Profile")]
    [SerializeField] private Vector3 normalOffset = new Vector3(0, 6, -8);
    [SerializeField] private Vector3 sprintOffset = new Vector3(0, 7, -10);
    [SerializeField] private Vector3 slideOffset = new Vector3(0, 4, -6);
    [SerializeField] private Vector3 flyOffset = new Vector3(0, 10, -12);

    [Header("Follow-Verhalten")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float offsetLerpSpeed = 5f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private bool smoothRotation = true;

    [Header("Dynamik bei Höhe")]
    [SerializeField] private float heightZoomFactor = 0.2f;
    [SerializeField] private float maxYZoom = 10f;
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Velocity-basierte Anpassungen")]
    [SerializeField] private bool enableVelocityOffset = true;
    [SerializeField] private float velocityOffsetFactor = 2f;
    [SerializeField] private float maxVelocityOffset = 5f;
    [SerializeField] private float velocityOffsetSmoothing = 2f;

    [Header("Kollisionsvermeidung")]
    [SerializeField] private bool enableCollisionAvoidance = true;
    [SerializeField] private LayerMask obstacleLayer = 1;
    [SerializeField] private float collisionBuffer = 0.5f;
    [SerializeField] private float collisionCheckRadius = 0.3f;

    [Header("Kamera-Shake")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float landingShakeStrength = 0.2f;
    [SerializeField] private float landingShakeDuration = 0.3f;

    [Header("FOV-Anpassungen")]
    [SerializeField] private bool enableDynamicFOV = true;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 70f;
    [SerializeField] private float flyFOV = 75f;
    [SerializeField] private float fovChangeSpeed = 2f;

    [Header("Look-Ahead")]
    [SerializeField] private bool enableLookAhead = true;
    [SerializeField] private float lookAheadDistance = 3f;
    [SerializeField] private float lookAheadSmoothing = 5f;

    // Private fields
    private PlayerController playerController;
    private Camera cam;
    private Vector3 currentOffset;
    private Vector3 velocityOffset;
    private Vector3 shakeOffset;
    private Vector3 lookAheadOffset;
    private float targetFOV;
    private bool wasGrounded = true;
    private Coroutine shakeCoroutine;

    // Optimization
    private float lastObstacleCheck;
    private const float OBSTACLE_CHECK_INTERVAL = 0.1f;

    void Start()
    {
        InitializeComponents();
        InitializeValues();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeComponents()
    {
        cam = GetComponent<Camera>();
        if (!cam)
        {
            Debug.LogError("CameraFollow: Camera component missing!");
            return;
        }

        if (autoFindPlayer && !target)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player)
                target = player.transform;
        }

        if (target)
            playerController = target.GetComponent<PlayerController>();
    }

    private void InitializeValues()
    {
        currentOffset = normalOffset;
        targetFOV = normalFOV;
        
        if (cam)
            cam.fieldOfView = normalFOV;

        velocityOffset = Vector3.zero;
        shakeOffset = Vector3.zero;
        lookAheadOffset = Vector3.zero;
    }

    private void SubscribeToEvents()
    {
        if (!playerController) return;

        playerController.OnGroundedChanged += OnGroundedChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (!playerController) return;

        playerController.OnGroundedChanged -= OnGroundedChanged;
    }

    void LateUpdate()
    {
        if (!target || !playerController)
            return;

        UpdateCameraOffset();
        UpdateVelocityOffset();
        UpdateLookAheadOffset();
        UpdateCameraPosition();
        UpdateCameraRotation();
        UpdateFOV();
    }

    private void UpdateCameraOffset()
    {
        Vector3 targetOffset = DetermineTargetOffset();
        
        // Höhenabhängiger Zusatz
        float yExtra = CalculateHeightOffset();
        Vector3 heightOffset = new Vector3(0, yExtra, -yExtra * 0.5f);
        
        Vector3 finalOffset = targetOffset + heightOffset;
        
        // Sanfte Offset-Übergänge
        currentOffset = Vector3.Lerp(currentOffset, finalOffset, offsetLerpSpeed * Time.deltaTime);
    }

    private Vector3 DetermineTargetOffset()
    {
        // Prioritäten: Flug > Rutsch > Sprint > Normal
        if (playerController.IsFlying)
        {
            targetFOV = flyFOV;
            return flyOffset;
        }
        else if (playerController.IsSliding)
        {
            targetFOV = normalFOV;
            return slideOffset;
        }
        else if (playerController.IsSprinting)
        {
            targetFOV = sprintFOV;
            return sprintOffset;
        }
        else
        {
            targetFOV = normalFOV;
            return normalOffset;
        }
    }

    private float CalculateHeightOffset()
    {
        float normalizedHeight = Mathf.Clamp01(target.position.y / maxYZoom);
        float curveValue = heightCurve.Evaluate(normalizedHeight);
        return Mathf.Clamp(target.position.y * heightZoomFactor * curveValue, 0, maxYZoom);
    }

    private void UpdateVelocityOffset()
    {
        if (!enableVelocityOffset) return;

        Vector3 velocity = playerController.Velocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        
        // Velocity-basierter Offset in Bewegungsrichtung
        Vector3 targetVelocityOffset = Vector3.zero;
        if (horizontalVelocity.magnitude > 1f)
        {
            Vector3 velocityDirection = horizontalVelocity.normalized;
            float velocityMagnitude = Mathf.Clamp(horizontalVelocity.magnitude, 0, maxVelocityOffset);
            targetVelocityOffset = velocityDirection * velocityMagnitude * velocityOffsetFactor;
        }

        velocityOffset = Vector3.Lerp(velocityOffset, targetVelocityOffset, 
            velocityOffsetSmoothing * Time.deltaTime);
    }

    private void UpdateLookAheadOffset()
    {
        if (!enableLookAhead) return;

        Vector3 targetLookAhead = Vector3.zero;
        Vector3 velocity = playerController.Velocity;
        
        if (velocity.magnitude > 0.5f)
        {
            Vector3 lookDirection = new Vector3(velocity.x, 0, velocity.z).normalized;
            targetLookAhead = lookDirection * lookAheadDistance;
        }

        lookAheadOffset = Vector3.Lerp(lookAheadOffset, targetLookAhead, 
            lookAheadSmoothing * Time.deltaTime);
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = target.position + lookAheadOffset;
        Vector3 totalOffset = currentOffset + velocityOffset + shakeOffset;
        Vector3 desiredPosition = targetPosition + totalOffset;

        // Kollisionsvermeidung
        if (enableCollisionAvoidance && Time.time - lastObstacleCheck > OBSTACLE_CHECK_INTERVAL)
        {
            desiredPosition = AvoidObstacles(targetPosition, desiredPosition);
            lastObstacleCheck = Time.time;
        }

        // Sanfte Positionsanpassung
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, 
            followSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    private void UpdateCameraRotation()
    {
        Vector3 lookTarget = target.position + lookAheadOffset;
        
        if (smoothRotation)
        {
            Vector3 direction = (lookTarget - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 
                rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.LookAt(lookTarget);
        }
    }

    private void UpdateFOV()
    {
        if (!enableDynamicFOV || !cam) return;

        float currentFOV = cam.fieldOfView;
        cam.fieldOfView = Mathf.Lerp(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
    }

    private Vector3 AvoidObstacles(Vector3 targetPos, Vector3 desiredPos)
    {
        Vector3 direction = (desiredPos - targetPos).normalized;
        float distance = Vector3.Distance(targetPos, desiredPos);
        
        if (Physics.SphereCast(targetPos, collisionCheckRadius, direction, out RaycastHit hit, 
            distance, obstacleLayer))
        {
            // Obstacle detected, move camera closer
            Vector3 safePosition = hit.point - direction * collisionBuffer;
            return safePosition;
        }

        return desiredPos;
    }

    private void OnGroundedChanged(bool grounded)
    {
        if (!wasGrounded && grounded && enableShake)
        {
            // Player just landed
            TriggerCameraShake(landingShakeStrength, landingShakeDuration);
        }
        
        wasGrounded = grounded;
    }

    public void TriggerCameraShake(float strength, float duration)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        
        shakeCoroutine = StartCoroutine(CameraShakeCoroutine(strength, duration));
    }

    private IEnumerator CameraShakeCoroutine(float strength, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration);
            float currentStrength = strength * t;
            
            shakeOffset = Random.insideUnitSphere * currentStrength;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        shakeOffset = Vector3.zero;
        shakeCoroutine = null;
    }

    // Public methods for external control
    public void SetTarget(Transform newTarget)
    {
        if (playerController)
            UnsubscribeFromEvents();
        
        target = newTarget;
        
        if (target)
        {
            playerController = target.GetComponent<PlayerController>();
            SubscribeToEvents();
        }
    }

    public void SetCustomOffset(Vector3 offset, float lerpSpeed = -1f)
    {
        currentOffset = offset;
        if (lerpSpeed > 0)
            offsetLerpSpeed = lerpSpeed;
    }

    public void ResetToTarget()
    {
        if (!target) return;
        
        transform.position = target.position + currentOffset;
        transform.LookAt(target);
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (!target) return;

        // Current camera target
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, 0.5f);
        
        // Look ahead position
        if (enableLookAhead)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position + lookAheadOffset, 0.3f);
        }
        
        // Collision check
        if (enableCollisionAvoidance)
        {
            Gizmos.color = Color.red;
            Vector3 direction = (transform.position - target.position).normalized;
            float distance = Vector3.Distance(target.position, transform.position);
            Gizmos.DrawWireSphere(target.position + direction * distance, collisionCheckRadius);
        }
    }
}