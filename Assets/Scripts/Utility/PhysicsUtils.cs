using UnityEngine;

/// <summary>
/// Common physics helper methods.
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
}
