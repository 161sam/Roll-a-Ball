using UnityEngine;

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
        [SerializeField] private bool oneTimeUse = true;
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private ParticleSystem activationEffect;
        
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
        }

        /// <summary>
        /// Set the gate that this switch controls.
        /// </summary>
        public void SetGate(GateController gate)
        {
            connectedGate = gate;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (oneTimeUse && isActivated) return;
            
            if (other.CompareTag("Player"))
            {
                ActivateSwitch();
            }
        }

        private void ActivateSwitch()
        {
            isActivated = true;
            
            // Open connected gate
            if (connectedGate)
            {
                connectedGate.TriggerOpen();
            }
            
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
        }
        
        /// <summary>
        /// Check if switch has been activated.
        /// </summary>
        public bool IsActivated => isActivated;
    }
}
