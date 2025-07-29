using UnityEngine;

/// <summary>
/// Common physics helper methods for Roll-a-Ball
/// CLAUDE: EXTENDED - Added more utility functions for consistent physics handling
/// </summary>
public static class PhysicsUtils
{
    /// <summary>
    /// Reset linear and angular velocity of a rigidbody if present.
    /// </summary>
    public static void ResetMotion(Rigidbody rb)
    {
        if (!rb) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    /// <summary>
    /// CLAUDE: ADDED - Ensure a component exists on a GameObject
    /// </summary>
    public static T EnsureComponent<T>(GameObject go) where T : Component
    {
        if (!go) return null;
        
        T component = go.GetComponent<T>();
        if (!component)
        {
            component = go.AddComponent<T>();
        }
        return component;
    }
    
    /// <summary>
    /// CLAUDE: ADDED - Safe velocity clamping for rigidbodies
    /// </summary>
    public static void ClampVelocity(Rigidbody rb, float maxSpeed)
    {
        if (!rb) return;
        
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
    
    /// <summary>
    /// CLAUDE: ADDED - Apply drag to horizontal movement only (for ball rolling)
    /// </summary>
    public static void ApplyHorizontalDrag(Rigidbody rb, float dragFactor)
    {
        if (!rb) return;
        
        Vector3 velocity = rb.linearVelocity;
        velocity.x *= dragFactor;
        velocity.z *= dragFactor;
        rb.linearVelocity = velocity;
    }
    
    /// <summary>
    /// CLAUDE: ADDED - Check if a point is within sphere radius (for ground checking)
    /// </summary>
    public static bool IsGrounded(Vector3 center, float radius, LayerMask groundLayer)
    {
        return Physics.CheckSphere(center, radius, groundLayer) ||
               Physics.Raycast(center + Vector3.up * 0.1f, Vector3.down, radius + 0.1f, groundLayer);
    }
    
    /// <summary>
    /// CLAUDE: ADDED - Safe force application with null checks
    /// </summary>
    public static void AddForceSafe(Rigidbody rb, Vector3 force, ForceMode mode = ForceMode.Force)
    {
        if (rb && !rb.isKinematic)
        {
            rb.AddForce(force, mode);
        }
    }
    
    /// <summary>
    /// CLAUDE: ADDED - Reset rigidbody and position in one call
    /// </summary>
    public static void ResetToPosition(Rigidbody rb, Vector3 position)
    {
        if (!rb) return;
        
        rb.transform.position = position;
        ResetMotion(rb);
    }
}
