using UnityEngine;

namespace RollABall.Environment
{
    /// <summary>
    /// Simple gate controller that can be opened via script.
    /// Used for puzzle elements in procedurally generated levels.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Environment/Gate Controller")]
    public class GateController : MonoBehaviour
    {
        [Header("Gate Configuration")]
        [SerializeField] private GameObject gateObject;
        [SerializeField] private bool disableVisuals = true;
        [SerializeField] private bool disableCollider = true;
        [SerializeField] private AudioClip openSound;
        
        private bool isOpened = false;
        private AudioSource audioSource;

        private void Awake()
        {
            if (!gateObject)
                gateObject = gameObject;
                
            // Setup AudioSource for gate sounds
            audioSource = GetComponent<AudioSource>();
            if (!audioSource && openSound)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }

        /// <summary>
        /// Opens the gate by disabling visuals and collider.
        /// </summary>
        public void TriggerOpen()
        {
            if (isOpened) return;
            
            isOpened = true;

            if (gateObject)
            {
                if (disableCollider)
                {
                    var col = gateObject.GetComponent<Collider>();
                    if (col) col.enabled = false;
                }
                
                if (disableVisuals)
                {
                    var rend = gateObject.GetComponent<Renderer>();
                    if (rend) rend.enabled = false;
                }
            }
            
            // Play open sound if available
            if (audioSource && openSound)
            {
                audioSource.PlayOneShot(openSound);
            }
            
            Debug.Log($"Gate {gameObject.name} opened!", this);
        }
        
        /// <summary>
        /// Check if gate is currently opened.
        /// </summary>
        public bool IsOpened => isOpened;
    }
}
