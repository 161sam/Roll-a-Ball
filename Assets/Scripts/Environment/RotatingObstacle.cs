using UnityEngine;

namespace RollABall.Environment
{
    /// <summary>
    /// Rotierendes Steampunk-Hindernis (Zahnräder, Turbinen, etc.)
    /// Kann den Spieler abprallen lassen oder andere Effekte auslösen
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Environment/Rotating Obstacle")]
    public class RotatingObstacle : MonoBehaviour
    {
        [Header("Rotationseinstellungen")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 90f; // Grad pro Sekunde
        [SerializeField] private bool randomizeStartRotation = true;
        
        [Header("Rotation Variation")]
        [SerializeField] private bool useVariableSpeed = false;
        [SerializeField] private float speedVariationAmount = 0.3f;
        [SerializeField] private float speedVariationFrequency = 1f;
        
        [Header("Physik-Effekte")]
        [SerializeField] private bool applyForceToPlayer = true;
        [SerializeField] private float bounceForce = 10f;
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
        [SerializeField] private bool knockBackEffect = true;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip mechanicalWhirring;
        [SerializeField] private AudioClip playerHitSound;
        [SerializeField] private bool audioFollowsSpeed = true;
        
        [Header("Partikel-Effekte")]
        [SerializeField] private ParticleSystem sparksEffect;
        [SerializeField] private ParticleSystem steamEffect;
        [SerializeField] private bool playEffectsOnHit = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool highlightOnPlayerNear = true;
        [SerializeField] private float highlightDistance = 3f;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material highlightMaterial;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color gizmoColor = Color.red;
        
        // Private Variablen
        private float baseRotationSpeed;
        private float currentRotationSpeed;
        private Transform playerTransform;
        private Renderer obstacleRenderer;
        private bool playerNearby = false;
        private float speedVariationTimer = 0f;
        
        void Start()
        {
            InitializeObstacle();
        }
        
        /// <summary>
        /// Initialisiert das Hindernis und alle Komponenten
        /// </summary>
        private void InitializeObstacle()
        {
            // Basis-Rotationsgeschwindigkeit speichern
            baseRotationSpeed = rotationSpeed;
            currentRotationSpeed = rotationSpeed;
            
            // Zufällige Startrotation falls gewünscht
            if (randomizeStartRotation)
            {
                transform.rotation = Random.rotation;
            }
            
            // Audio-Setup
            SetupAudio();
            
            // Renderer für Highlighting
            obstacleRenderer = GetComponent<Renderer>();
            if (obstacleRenderer != null && normalMaterial == null)
            {
                normalMaterial = obstacleRenderer.material;
            }
            
            // Spieler finden
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            
            // Partikel-Setup
            SetupParticleEffects();
        }
        
        /// <summary>
        /// Konfiguriert Audio-Komponenten
        /// </summary>
        private void SetupAudio()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            if (audioSource != null && mechanicalWhirring != null)
            {
                audioSource.clip = mechanicalWhirring;
                audioSource.loop = true;
                audioSource.playOnAwake = true;
                audioSource.volume = 0.4f;
                audioSource.spatialBlend = 1f; // 3D Sound
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.maxDistance = 20f;
                
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
        }
        
        /// <summary>
        /// Konfiguriert Partikeleffekte
        /// </summary>
        private void SetupParticleEffects()
        {
            // Funken-Effekt Setup
            if (sparksEffect != null)
            {
                var main = sparksEffect.main;
                main.loop = true;
                main.startLifetime = 1f;
                main.startSpeed = 2f;
                main.maxParticles = 50;
                
                var emission = sparksEffect.emission;
                emission.rateOverTime = 10f;
            }
            
            // Steam-Effekt Setup
            if (steamEffect != null)
            {
                var main = steamEffect.main;
                main.loop = true;
                main.startLifetime = 2f;
                main.startSpeed = 1f;
                main.maxParticles = 100;
                
                var emission = steamEffect.emission;
                emission.rateOverTime = 20f;
            }
        }
        
        void Update()
        {
            HandleRotation();
            HandlePlayerProximity();
            HandleAudioPitching();
        }
        
        /// <summary>
        /// Behandelt die Rotationslogik mit optionaler Geschwindigkeitsvariation
        /// </summary>
        private void HandleRotation()
        {
            // Geschwindigkeitsvariation berechnen
            if (useVariableSpeed)
            {
                speedVariationTimer += Time.deltaTime * speedVariationFrequency;
                float variation = Mathf.Sin(speedVariationTimer) * speedVariationAmount;
                currentRotationSpeed = baseRotationSpeed * (1f + variation);
            }
            else
            {
                currentRotationSpeed = baseRotationSpeed;
            }
            
            // Rotation anwenden
            transform.Rotate(rotationAxis * currentRotationSpeed * Time.deltaTime, Space.Self);
        }
        
        /// <summary>
        /// Prüft Spielernähe für visuelle Effekte
        /// </summary>
        private void HandlePlayerProximity()
        {
            if (!highlightOnPlayerNear || playerTransform == null || obstacleRenderer == null)
                return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool wasNearby = playerNearby;
            playerNearby = distanceToPlayer <= highlightDistance;
            
            // Material wechseln bei Näheänderung
            if (playerNearby != wasNearby)
            {
                if (playerNearby && highlightMaterial != null)
                {
                    obstacleRenderer.material = highlightMaterial;
                }
                else if (normalMaterial != null)
                {
                    obstacleRenderer.material = normalMaterial;
                }
            }
        }
        
        /// <summary>
        /// Passt Audio-Pitch basierend auf Rotationsgeschwindigkeit an
        /// </summary>
        private void HandleAudioPitching()
        {
            if (audioSource != null && audioFollowsSpeed)
            {
                float pitchMultiplier = currentRotationSpeed / baseRotationSpeed;
                audioSource.pitch = Mathf.Clamp(pitchMultiplier, 0.5f, 2f);
            }
        }
        
        /// <summary>
        /// Behandelt Kollisionen mit dem Spieler
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && applyForceToPlayer)
            {
                HandlePlayerCollision(collision);
            }
        }
        
        /// <summary>
        /// Spezifische Logik für Spielerkollisionen
        /// </summary>
        private void HandlePlayerCollision(Collision collision)
        {
            Rigidbody playerRigidbody = collision.rigidbody;
            if (playerRigidbody == null) return;
            
            // Abstoßkraft berechnen
            Vector3 bounceDirection = CalculateBounceDirection(collision);
            
            if (knockBackEffect)
            {
                // Spieler wegschleudern
                playerRigidbody.AddForce(bounceDirection * bounceForce, forceMode);
            }
            
            // Audio-Effekt abspielen
            PlayHitSound();
            
            // Partikel-Effekte auslösen
            if (playEffectsOnHit)
            {
                TriggerHitEffects(collision.contacts[0].point);
            }
        }
        
        /// <summary>
        /// Berechnet die Richtung für den Abstoßeffekt
        /// </summary>
        private Vector3 CalculateBounceDirection(Collision collision)
        {
            Vector3 contactPoint = collision.contacts[0].point;
            Vector3 obstacleCenter = transform.position;
            
            // Richtung vom Hindernis weg
            Vector3 bounceDirection = (contactPoint - obstacleCenter).normalized;
            
            // Leichte Aufwärtskraft hinzufügen für dynamischeren Effekt
            bounceDirection.y = Mathf.Max(bounceDirection.y, 0.3f);
            
            return bounceDirection.normalized;
        }
        
        /// <summary>
        /// Spielt Aufprall-Sound ab
        /// </summary>
        private void PlayHitSound()
        {
            if (audioSource != null && playerHitSound != null)
            {
                audioSource.PlayOneShot(playerHitSound, 0.7f);
            }
        }
        
        /// <summary>
        /// Löst Partikeleffekte bei Kollision aus
        /// </summary>
        private void TriggerHitEffects(Vector3 contactPoint)
        {
            // Funken-Effekt
            if (sparksEffect != null)
            {
                sparksEffect.transform.position = contactPoint;
                sparksEffect.Emit(20);
            }
            
            // Zusätzlicher Steam-Burst
            if (steamEffect != null)
            {
                var burst = steamEffect.emission;
                steamEffect.Emit(30);
            }
        }
        
        /// <summary>
        /// Ändert die Rotationsgeschwindigkeit zur Laufzeit
        /// </summary>
        public void SetRotationSpeed(float newSpeed)
        {
            baseRotationSpeed = newSpeed;
            rotationSpeed = newSpeed;
        }
        
        /// <summary>
        /// Aktiviert/Deaktiviert die Rotation
        /// </summary>
        public void SetRotating(bool rotating)
        {
            enabled = rotating;
            
            if (audioSource != null)
            {
                if (rotating)
                {
                    audioSource.UnPause();
                }
                else
                {
                    audioSource.Pause();
                }
            }
        }
        
        /// <summary>
        /// Kehrt die Rotationsrichtung um
        /// </summary>
        public void ReverseRotation()
        {
            baseRotationSpeed = -baseRotationSpeed;
            rotationSpeed = -rotationSpeed;
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Hindernis-Position
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Rotationsachse visualisieren
            Gizmos.color = Color.yellow;
            Vector3 axisDirection = transform.TransformDirection(rotationAxis.normalized);
            Gizmos.DrawLine(transform.position - axisDirection * 2f, 
                           transform.position + axisDirection * 2f);
            
            // Highlight-Radius anzeigen
            if (highlightOnPlayerNear)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, highlightDistance);
            }
        }
        
        void OnValidate()
        {
            // Werte korrigieren
            rotationSpeed = Mathf.Clamp(rotationSpeed, -360f, 360f);
            bounceForce = Mathf.Max(0f, bounceForce);
            highlightDistance = Mathf.Max(0.1f, highlightDistance);
            speedVariationAmount = Mathf.Clamp01(speedVariationAmount);
            speedVariationFrequency = Mathf.Max(0.1f, speedVariationFrequency);
        }
    }
}
