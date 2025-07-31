using UnityEngine;

namespace RollABall.Environment
{
    /// <summary>
    /// Bewegliche Steampunk-Plattform mit konfigurierbarer Bewegung
    /// Unterstützt lineare und sinus-basierte Bewegungsmuster
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Environment/Moving Platform")]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private MovingPlatformProfile profile;

        [Header("Bounce Settings")]
        [SerializeField] private float bounceFactor = 7.5625f;
        [SerializeField] private float bounceDiv = 2.75f;
        [SerializeField] private float bounceReturn1 = 0.75f;
        [SerializeField] private float bounceReturn2 = 0.9375f;
        [SerializeField] private float bounceReturn3 = 0.984375f;

        [Header("Bewegungseinstellungen")]
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 endPosition;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Bewegungstyp")]
        [SerializeField] private MovementType movementType = MovementType.Linear;
        [SerializeField] private bool useLocalSpace = true;
        
        [Header("Timing")]
        [SerializeField] private float pauseDuration = 1f;
        [SerializeField] private bool startMovingImmediately = true;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip mechanicalSound;
        [SerializeField] private bool playAudioOnMovement = true;
        
        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = Color.cyan;
        [SerializeField] private string passengerTag = "Player";
        
        // Private Variablen
        private Vector3 worldStartPosition;
        private Vector3 worldEndPosition;
        private float journeyTime = 0f;
        private bool movingToEnd = true;
        private bool isMoving = true;
        private float pauseTimer = 0f;
        private Rigidbody platformRigidbody;
        private Vector3 lastPosition;
        
        // Enums
        public enum MovementType
        {
            Linear,         // Gleichmäßige Bewegung
            Sine,          // Sinus-basierte Bewegung
            EaseInOut,     // Smooth Start/Stop
            Bounce         // Federeffekt an den Enden
        }
        
        void Start()
        {
            InitializePlatform();
        }
        
        /// <summary>
        /// Initialisiert die Plattform-Komponenten und Bewegungsparameter
        /// </summary>
        private void InitializePlatform()
        {
            if (profile)
            {
                startPosition = profile.startPosition;
                endPosition = profile.endPosition;
                moveSpeed = profile.moveSpeed;
                movementCurve = profile.movementCurve;
                movementType = profile.movementType;
                useLocalSpace = profile.useLocalSpace;
                pauseDuration = profile.pauseDuration;
                startMovingImmediately = profile.startMovingImmediately;
                mechanicalSound = profile.mechanicalSound;
                playAudioOnMovement = profile.playAudioOnMovement;
            }

            // Rigidbody setup für physikalische Interaktion
            platformRigidbody = GetComponent<Rigidbody>();
            if (platformRigidbody == null)
            {
                platformRigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            // Plattform ist kinematisch - bewegt sich nach Script, nicht nach Physik
            platformRigidbody.isKinematic = true;
            platformRigidbody.useGravity = false;
            
            // Audio-Setup
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null && playAudioOnMovement)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            if (audioSource != null)
            {
                audioSource.clip = mechanicalSound;
                audioSource.loop = true;
                audioSource.playOnAwake = false;
                audioSource.volume = 0.3f;
            }
            
            // Weltpositionen berechnen
            CalculateWorldPositions();
            
            // Startposition setzen
            if (useLocalSpace)
            {
                transform.localPosition = startPosition;
            }
            else
            {
                transform.position = worldStartPosition;
            }
            
            lastPosition = transform.position;
            
            // Bewegung starten falls gewünscht
            if (!startMovingImmediately)
            {
                isMoving = false;
            }
        }
        
        /// <summary>
        /// Berechnet die Weltpositionen basierend auf Local/World Space Einstellung
        /// </summary>
        private void CalculateWorldPositions()
        {
            if (useLocalSpace)
            {
                worldStartPosition = transform.parent != null 
                    ? transform.parent.TransformPoint(startPosition)
                    : startPosition;
                worldEndPosition = transform.parent != null 
                    ? transform.parent.TransformPoint(endPosition)
                    : endPosition;
            }
            else
            {
                worldStartPosition = startPosition;
                worldEndPosition = endPosition;
            }
        }
        
        void FixedUpdate()
        {
            if (!isMoving)
            {
                HandlePause();
                return;
            }
            
            MovePlatform();
            HandleAudio();
        }
        
        /// <summary>
        /// Behandelt Pausenlogik zwischen Bewegungszyklen
        /// </summary>
        private void HandlePause()
        {
            if (pauseDuration > 0)
            {
                pauseTimer += Time.fixedDeltaTime;
                if (pauseTimer >= pauseDuration)
                {
                    pauseTimer = 0f;
                    isMoving = true;
                }
            }
            else
            {
                isMoving = true;
            }
        }
        
        /// <summary>
        /// Hauptbewegungslogik der Plattform
        /// </summary>
        private void MovePlatform()
        {
            // Bewegungsfortschritt berechnen
            float distance = Vector3.Distance(worldStartPosition, worldEndPosition);
            float journeyFraction = (journeyTime * moveSpeed) / distance;
            
            // Bewegungstyp anwenden
            float adjustedFraction = ApplyMovementType(journeyFraction);
            
            // Zielposition berechnen
            Vector3 targetPosition = movingToEnd 
                ? Vector3.Lerp(worldStartPosition, worldEndPosition, adjustedFraction)
                : Vector3.Lerp(worldEndPosition, worldStartPosition, adjustedFraction);
            
            // Plattform bewegen (kinematisch)
            Vector3 movement = targetPosition - transform.position;
            platformRigidbody.MovePosition(targetPosition);
            
            // Bewegungszeit aktualisieren
            journeyTime += Time.fixedDeltaTime;
            
            // Prüfen ob Ziel erreicht wurde
            if (journeyFraction >= 1f)
            {
                // Richtung umkehren
                movingToEnd = !movingToEnd;
                journeyTime = 0f;
                
                // Pause einlegen falls konfiguriert
                if (pauseDuration > 0)
                {
                    isMoving = false;
                    pauseTimer = 0f;
                }
            }
            
            lastPosition = transform.position;
        }
        
        /// <summary>
        /// Wendet den gewählten Bewegungstyp auf die Bewegungsfraktion an
        /// </summary>
        private float ApplyMovementType(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            
            switch (movementType)
            {
                case MovementType.Linear:
                    return fraction;
                    
                case MovementType.Sine:
                    return Mathf.Sin(fraction * Mathf.PI * 0.5f);
                    
                case MovementType.EaseInOut:
                    return movementCurve.Evaluate(fraction);
                    
                case MovementType.Bounce:
                    return BounceEaseOut(fraction);
                    
                default:
                    return fraction;
            }
        }
        
        /// <summary>
        /// Implementiert einen Bounce-Effekt für organischere Bewegung
        /// </summary>
        private float BounceEaseOut(float t)
        {
            float stage1 = 1f / bounceDiv;
            float stage2 = 2f / bounceDiv;
            float stage3 = 2.5f / bounceDiv;
            float stage4 = 2.625f / bounceDiv;

            if (t < stage1)
            {
                return bounceFactor * t * t;
            }
            else if (t < stage2)
            {
                return bounceFactor * (t -= (1.5f / bounceDiv)) * t + bounceReturn1;
            }
            else if (t < stage3)
            {
                return bounceFactor * (t -= (2.25f / bounceDiv)) * t + bounceReturn2;
            }
            else
            {
                return bounceFactor * (t -= stage4) * t + bounceReturn3;
            }
        }
        
        /// <summary>
        /// Behandelt Audio-Wiedergabe basierend auf Bewegungsstatus
        /// </summary>
        private void HandleAudio()
        {
            if (audioSource != null && playAudioOnMovement)
            {
                if (isMoving && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }
                else if (!isMoving && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }
        
        /// <summary>
        /// Startet oder stoppt die Plattformbewegung
        /// </summary>
        public void SetMoving(bool moving)
        {
            isMoving = moving;
            if (!moving && audioSource != null)
            {
                audioSource.Stop();
            }
        }
        
        /// <summary>
        /// Setzt die Bewegungsgeschwindigkeit zur Laufzeit
        /// </summary>
        public void SetMoveSpeed(float newSpeed)
        {
            moveSpeed = Mathf.Max(0.1f, newSpeed);
        }
        
        /// <summary>
        /// Kehrt die aktuelle Bewegungsrichtung um
        /// </summary>
        public void ReverseDirection()
        {
            movingToEnd = !movingToEnd;
            journeyTime = 0f;
        }
        
        void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Positionen aktualisieren
            CalculateWorldPositions();
            
            // Start- und Endpunkte zeichnen
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(worldStartPosition, 0.5f);
            Gizmos.DrawWireSphere(worldEndPosition, 0.5f);
            
            // Bewegungslinie zeichnen
            Gizmos.DrawLine(worldStartPosition, worldEndPosition);
            
            // Aktuelle Position hervorheben
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
            
            // Bewegungsrichtung anzeigen
            Vector3 direction = (worldEndPosition - worldStartPosition).normalized;
            Vector3 arrowHead = worldStartPosition + direction * Vector3.Distance(worldStartPosition, worldEndPosition) * 0.8f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(arrowHead, arrowHead + direction * 0.5f);
        }
        
        void OnValidate()
        {
            // Automatische Korrektur ungültiger Werte
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            pauseDuration = Mathf.Max(0f, pauseDuration);
            bounceFactor = Mathf.Max(0f, bounceFactor);
            bounceDiv = Mathf.Max(0.1f, bounceDiv);
            if (startPosition == endPosition)
            {
                Debug.LogWarning($"[MovingPlatform] Start and End positions are identical on {name}", this);
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            // Spieler zur Plattform "heften" für realistisches Verhalten
            if (other.CompareTag(passengerTag))
            {
                // CharacterController sollte nicht geparentet werden
                if (!other.GetComponent<CharacterController>())
                {
                    other.transform.SetParent(transform);
                }
            }
        }
        
        void OnTriggerExit(Collider other)
        {
            // Spieler von Plattform lösen
            if (other.CompareTag(passengerTag))
            {
                other.transform.SetParent(null);
            }
        }
    }
}
