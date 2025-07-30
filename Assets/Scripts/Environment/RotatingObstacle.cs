using UnityEngine;

/// <summary>
/// Simple rotating obstacle for Level2 difficulty enhancement
/// Rotates continuously around Y-axis to create movement challenge
/// </summary>
[AddComponentMenu("Roll-a-Ball/Environment/Rotating Obstacle")]
public class RotatingObstacle : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private RotatingObstacleProfile profile;
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 45f; // degrees per second
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // Y-axis rotation
    [SerializeField] private bool clockwise = true;
    [SerializeField] private float minRotationSpeed = 0f;
    [SerializeField] private float maxRotationSpeed = 180f;
    // parameters can be overridden via profile
    
    [Header("Visual")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.red;

    private void Awake()
    {
        if (profile)
        {
            rotationSpeed = profile.rotationSpeed;
            rotationAxis = profile.rotationAxis;
            clockwise = profile.clockwise;
            minRotationSpeed = profile.minRotationSpeed;
            maxRotationSpeed = profile.maxRotationSpeed;
        }
    }
    
    private void Update()
    {
        // Calculate rotation direction
        float direction = clockwise ? 1f : -1f;

        // Apply rotation
        transform.Rotate(rotationAxis, rotationSpeed * direction * Time.deltaTime, Space.Self);
    }

    private void OnValidate()
    {
        if (profile && (minRotationSpeed != profile.minRotationSpeed || maxRotationSpeed != profile.maxRotationSpeed))
        {
            minRotationSpeed = profile.minRotationSpeed;
            maxRotationSpeed = profile.maxRotationSpeed;
        }

        rotationSpeed = Mathf.Clamp(rotationSpeed, minRotationSpeed, maxRotationSpeed);
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Draw rotation direction indicator
        Vector3 forward = transform.forward * (transform.localScale.z * 0.6f);
        Gizmos.DrawRay(transform.position, forward);
    }
    
        private void OnTriggerEnter(Collider other)
        {
            // Optional: Add sound effect when player touches obstacle
            if (other.CompareTag("Player"))
            {
                if (AudioManager.Instance)
                {
                    AudioManager.Instance.PlaySound("MetalClank");
                }
                // Damage system not yet implemented but can be hooked up here
            }
        }
}
