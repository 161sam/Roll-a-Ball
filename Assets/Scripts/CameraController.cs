using UnityEngine;

/// <summary>
/// Simple camera controller that follows a target smoothly.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 8, -12);
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Auto Target Settings")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Height Limits")]
    [SerializeField] private float minHeight = 2f;
    [SerializeField] private float maxHeight = 50f;

    [Header("Debug")]
    [SerializeField] private bool logTargetSearch = true;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("[CameraController] Camera component missing!");
            return;
        }

        if (autoFindPlayer && target == null)
        {
            FindPlayerTarget();
        }

        if (target != null)
        {
            Vector3 startPosition = target.position + offset;
            startPosition.y = Mathf.Clamp(startPosition.y, minHeight, maxHeight);
            transform.position = startPosition;
            transform.LookAt(target);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (autoFindPlayer)
            {
                FindPlayerTarget();
            }
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minHeight, maxHeight);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / followSpeed);

        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void FindPlayerTarget()
    {
        // TODO: Cache player reference to avoid repeated searches
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            target = player.transform;
            if (logTargetSearch)
                Debug.Log($"[CameraController] Found player by tag: {player.name}");
        }
        else
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                target = pc.transform;
                if (logTargetSearch)
                    Debug.Log($"[CameraController] Found player by component: {pc.name}");
            }
            else if (logTargetSearch)
            {
                Debug.LogWarning("[CameraController] No player found to follow.");
            }
        }
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

    public void SetOffset(Vector3 newOffset) => offset = newOffset;

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position + offset, 0.5f);
        }
    }
}
