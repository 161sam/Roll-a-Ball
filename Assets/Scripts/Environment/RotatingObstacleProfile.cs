using UnityEngine;

/// <summary>
/// Configuration profile for RotatingObstacle parameters.
/// </summary>
[CreateAssetMenu(fileName = "RotatingObstacleProfile", menuName = "Roll-a-Ball/Rotating Obstacle Profile")]
public class RotatingObstacleProfile : ScriptableObject
{
    public float rotationSpeed = 45f;
    public Vector3 rotationAxis = Vector3.up;
    public bool clockwise = true;
    [Header("Speed Limits")]
    public float minRotationSpeed = 0f;
    public float maxRotationSpeed = 180f;
    [Header("Damage")]
    public float contactDamage = 1f;
}

