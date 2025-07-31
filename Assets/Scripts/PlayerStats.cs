using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Roll-a-Ball/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Movement")]
    public float moveForce = 10f;
    public float maxSpeed = 8f;
    public float sprintMultiplier = 1.5f;
    public float airControlMultiplier = 0.5f;
    public float ballDrag = 0.98f;

    [Header("Slide")]
    public float slideImpulseMultiplier = 0.8f;
    public float slideDuration = 0.5f;

    [Header("Jumping")]
    public float jumpForce = 8f;
    public float groundCheckRadius = 0.6f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = 1;
    public float coyoteTime = 0.2f;

    [Header("Double Jump")]
    public bool enableDoubleJump = true;
    public float doubleJumpForce = 6f;

    [Header("Flying")]
    public float flyForce = 12f;
    public float maxFlyHeight = 25f;
    public float flyHorizontalDamping = 0.9f;

    [Header("Fly Energy")]
    public float maxFlyEnergy = 3f;
    public float flyDepletionRate = 1f;
    public float flyRegenRate = 1.5f;
    public float flyRegenDelay = 1f;
}
