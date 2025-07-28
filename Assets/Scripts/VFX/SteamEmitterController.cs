using UnityEngine;

namespace RollABall.VFX
{
    /// <summary>
    /// Controls a steam emitter using profile settings.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/VFX/Steam Emitter Controller")]
    public class SteamEmitterController : MonoBehaviour
    {
        [SerializeField] private SteamEmitterProfile profile;
        [SerializeField] private ParticleSystem steamParticles;
        [SerializeField] private float playerEffectRadius = 3f;

        private ParticleSystem.EmissionModule emission;
        private Transform playerTransform;
        private Rigidbody playerRigidbody;
        private AudioSource audioSource;

        /// <summary>
        /// Initializes the emitter with a profile.
        /// </summary>
        public void Init(SteamEmitterProfile settings)
        {
            ApplyProfile(settings);
        }

        /// <summary>
        /// Apply settings from a SteamEmitterProfile.
        /// </summary>
        public void ApplyProfile(SteamEmitterProfile settings)
        {
            profile = settings;
            if (profile == null)
                return;

            if (!steamParticles)
                steamParticles = GetComponentInChildren<ParticleSystem>();

            if (steamParticles)
            {
                emission = steamParticles.emission;
                emission.rateOverTime = profile.BaseEmissionRate;
            }
        }

        /// <summary>
        /// Set steam audio clip.
        /// </summary>
        public void SetSteamSound(AudioClip clip)
        {
            if (!clip) return;
            EnsureAudioSource();
            audioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Set mechanical audio clip.
        /// </summary>
        public void SetMechanicalSound(AudioClip clip)
        {
            if (!clip) return;
            EnsureAudioSource();
            audioSource.PlayOneShot(clip);
        }

        private void Awake()
        {
            EnsurePlayer();
            EnsureAudioSource();
            if (profile != null)
                ApplyProfile(profile);
        }

        private void FixedUpdate()
        {
            // Steam emitter affects player movement
            if (profile == null || !profile.AffectPlayerMovement)
                return;
            if (!playerTransform)
                EnsurePlayer();
            if (!playerRigidbody) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= playerEffectRadius)
            {
                // Apply wind force
                Vector3 direction = transform.up;
                playerRigidbody.AddForce(direction * profile.WindForce, ForceMode.Force);
            }
        }

        private void EnsurePlayer()
        {
            if (playerTransform) return;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                playerTransform = player.transform;
                playerRigidbody = player.GetComponent<Rigidbody>();
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource) return;
            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }

        /// <summary>
        /// Returns true if this emitter influences player movement.
        /// </summary>
        public bool HasMovementInfluence => profile != null && profile.AffectPlayerMovement;
    }
}
