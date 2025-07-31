using UnityEngine;
using System.Collections;

namespace RollABall.Environment
{
    /// <summary>
    /// Trigger component that opens a connected gate when the player enters.
    /// Creates puzzle elements in procedurally generated levels.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Environment/Switch Trigger")]
    public class SwitchTrigger : MonoBehaviour
    {
        [Header("Switch Configuration")]
        [SerializeField] private GateController connectedGate;
        [SerializeField] private SteampunkGateController steampunkGate;
        [SerializeField] private bool oneTimeUse = true;
        [SerializeField] private bool allowDeactivation = false;
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private ParticleSystem activationEffect;

        [Header("Activation Requirements")]
        [SerializeField] private bool requiresKeyItem = false;
        [SerializeField] private string requiredItemTag = "KeyItem";
        
        private bool isActivated = false;
        private AudioSource audioSource;

    private void Awake()
    {
        // Setup AudioSource for switch sounds
        audioSource = GetComponent<AudioSource>();
        if (!audioSource && activationSound)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
        if (connectedGate == null)
        {
            Debug.LogWarning($"[SwitchTrigger] No gate assigned on {name}", this);
        }
    }

        /// <summary>
        /// Set the gate that this switch controls.
        /// </summary>
        public void SetGate(MonoBehaviour gate)
        {
            connectedGate = gate as GateController;
            steampunkGate = gate as SteampunkGateController;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (oneTimeUse && isActivated) return;

            if (other.CompareTag("Player"))
            {
                if (!requiresKeyItem || other.transform.CompareTag(requiredItemTag))
                {
                    ActivateSwitch();
                }
            }
        }

    private void ActivateSwitch()
    {
        isActivated = true;
            
        // Open connected gate
        if (connectedGate)
            connectedGate.TriggerOpen();
        if (steampunkGate)
            steampunkGate.OpenGate();
            
            // Play activation sound
            if (audioSource && activationSound)
            {
                audioSource.PlayOneShot(activationSound);
            }
            
            // Trigger particle effect
            if (activationEffect)
            {
                activationEffect.Play();
            }
            
            // Visual feedback - change color if possible
            var renderer = GetComponent<Renderer>();
            if (renderer && renderer.material)
            {
                renderer.material.color = Color.green;
            }
            
        Debug.Log($"Switch {gameObject.name} activated!", this);

        if (!oneTimeUse && allowDeactivation)
        {
            StartCoroutine(ResetRoutine());
        }
    }

    private IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(1f);
        isActivated = false;
        if (connectedGate)
            connectedGate.TriggerClose();
        if (steampunkGate)
            steampunkGate.CloseGate();
        var renderer = GetComponent<Renderer>();
        if (renderer && renderer.material)
        {
            renderer.material.color = Color.white;
        }
    }
        
        /// <summary>
        /// Check if switch has been activated.
        /// </summary>
    public bool IsActivated => isActivated;

        private void OnValidate()
        {
            if (connectedGate == null && steampunkGate == null)
            {
                Debug.LogWarning($"[SwitchTrigger] Connected gate missing on {name}", this);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (connectedGate == null && steampunkGate == null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }
        }
#endif
    }
}
