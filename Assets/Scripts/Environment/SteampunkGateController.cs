using UnityEngine;
using System.Collections;

namespace RollABall.Environment
{
    /// <summary>
    /// Interaktive Steampunk-Tore mit verschiedenen Aktivierungsmechanismen
    /// Unterstützt Button-Aktivierung, Collectible-basierte Öffnung und Timer
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Environment/Steampunk Gate Controller")]
    public class SteampunkGateController : MonoBehaviour
    {
        [Header("🚪 Tor-Einstellungen")]
        [SerializeField] private GateType gateType = GateType.CollectibleActivated;
        [SerializeField] private Transform gateModel; // Das bewegliche Tor-Modell
        [SerializeField] private bool startOpen = false;
        [SerializeField] private bool oneTimeUse = false;
        
        [Header("🎯 Aktivierungs-Bedingungen")]
        [SerializeField] private int requiredCollectibles = 3;
        [SerializeField] private bool requireAllCollectibles = false;
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private float activationRange = 3f;
        
        [Header("⚙️ Bewegungs-Animation")]
        [SerializeField] private Vector3 openPosition = Vector3.up * 3f;
        [SerializeField] private Vector3 closedPosition = Vector3.zero;
        [SerializeField] private float animationDuration = 2f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool useLocalSpace = true;
        
        [Header("🔘 Button-Aktivierung")]
        [SerializeField] private Transform buttonModel;
        [SerializeField] private float buttonPressDepth = 0.2f;
        [SerializeField] private float buttonResetTime = 3f;
        
        [Header("⏱️ Timer-Einstellungen")]
        [SerializeField] private float autoCloseDelay = 5f;
        [SerializeField] private bool repeatTimer = true;
        [SerializeField] private float timerInterval = 10f;
        
        [Header("🎵 Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip gateOpenSound;
        [SerializeField] private AudioClip gateCloseSound;
        [SerializeField] private AudioClip buttonPressSound;
        [SerializeField] private AudioClip mechanicalWhirring;
        
        [Header("💡 Visuelle Effekte")]
        [SerializeField] private ParticleSystem steamBurst;
        [SerializeField] private Light statusLight;
        [SerializeField] private Color openColor = Color.green;
        [SerializeField] private Color closedColor = Color.red;
        [SerializeField] private Color activatingColor = Color.yellow;
        
        [Header("🧲 Kollisions-Effekte")]
        [SerializeField] private bool blockPlayer = true;
        [SerializeField] private bool bouncePlayer = false;
        [SerializeField] private float bounceForce = 5f;
        
        [Header("🔧 Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool logStateChanges = true;
        
        // Private Variablen
        private bool isOpen;
        private bool isAnimating = false;
        private bool hasBeenUsed = false;
        private bool buttonPressed = false;
        private float timerCounter = 0f;
        private Coroutine currentAnimation;
        private Coroutine buttonResetCoroutine;
        private Coroutine autoCloseCoroutine;
        private Transform playerTransform;
        private GameManager gameManager;
        private LevelManager levelManager;
        private Vector3 originalGatePosition;
        private Vector3 originalButtonPosition;
        private Collider gateCollider;
        
        // Enums
        public enum GateType
        {
            ButtonActivated,        // Durch Button/Trigger aktiviert
            CollectibleActivated,   // Öffnet wenn X Collectibles gesammelt
            PlayerProximity,        // Öffnet bei Spielernähe
            TimerBased,            // Öffnet/schließt in Intervallen
            AllCollectibles,       // Öffnet nur wenn alle Collectibles gesammelt
            Sequential             // Teil einer Sequenz von Toren
        }
        
        void Start()
        {
            InitializeGate();
        }
        
        /// <summary>
        /// Initialisiert das Tor-System
        /// </summary>
        private void InitializeGate()
        {
            // Referenzen finden
            if (gateModel == null)
                gateModel = transform.Find("GateModel");
            
            if (buttonModel == null)
                buttonModel = transform.Find("Button");
                
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            
            // Audio konfigurieren
            ConfigureAudio();
            
            // Spieler und Manager finden
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
            
            gameManager = FindFirstObjectByType<GameManager>();
            levelManager = FindFirstObjectByType<LevelManager>();
            
            // Kollider setup
            gateCollider = gateModel?.GetComponent<Collider>();
            
            // Ursprungspositionen speichern
            if (gateModel != null)
            {
                originalGatePosition = useLocalSpace ? gateModel.localPosition : gateModel.position;
                
                // Öffnungs-/Schließpositionen relativ zur Ursprungsposition berechnen
                if (useLocalSpace)
                {
                    openPosition += originalGatePosition;
                    closedPosition += originalGatePosition;
                }
                else
                {
                    openPosition += gateModel.position;
                    closedPosition += gateModel.position;
                }
            }
            
            if (buttonModel != null)
            {
                originalButtonPosition = useLocalSpace ? buttonModel.localPosition : buttonModel.position;
            }
            
            // Startzustand setzen
            isOpen = startOpen;
            SetGatePosition(isOpen ? openPosition : closedPosition, immediate: true);
            UpdateVisualFeedback();
            
            if (logStateChanges)
                Debug.Log($"🚪 Tor '{name}' initialisiert. Typ: {gateType}, Startzustand: {(isOpen ? "Offen" : "Geschlossen")}");
        }
        
        /// <summary>
        /// Konfiguriert Audio-Einstellungen
        /// </summary>
        private void ConfigureAudio()
        {
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 15f;
            audioSource.volume = 0.5f;
        }
        
        void Update()
        {
            HandleGateLogic();
            HandleTimerLogic();
            UpdateVisualFeedback();
        }
        
        /// <summary>
        /// Hauptlogik für Tor-Aktivierung basierend auf Typ
        /// </summary>
        private void HandleGateLogic()
        {
            if (isAnimating || (oneTimeUse && hasBeenUsed))
                return;
            
            bool shouldOpen = false;
            
            switch (gateType)
            {
                case GateType.ButtonActivated:
                    shouldOpen = buttonPressed;
                    break;
                    
                case GateType.CollectibleActivated:
                    shouldOpen = CheckCollectibleRequirement();
                    break;
                    
                case GateType.PlayerProximity:
                    shouldOpen = CheckPlayerProximity();
                    break;
                    
                case GateType.AllCollectibles:
                    shouldOpen = CheckAllCollectiblesCollected();
                    break;
                    
                case GateType.Sequential:
                    shouldOpen = CheckSequentialRequirement();
                    break;
                    
                case GateType.TimerBased:
                    // Timer-basiert wird in HandleTimerLogic behandelt
                    break;
            }
            
            // Zustandsänderung durchführen
            if (shouldOpen && !isOpen)
            {
                OpenGate();
            }
            else if (!shouldOpen && isOpen && gateType != GateType.TimerBased)
            {
                CloseGate();
            }
        }
        
        /// <summary>
        /// Behandelt Timer-basierte Logik
        /// </summary>
        private void HandleTimerLogic()
        {
            if (gateType != GateType.TimerBased || isAnimating)
                return;
            
            timerCounter += Time.deltaTime;
            
            if (timerCounter >= timerInterval)
            {
                if (isOpen)
                {
                    CloseGate();
                }
                else
                {
                    OpenGate();
                }
                
                if (repeatTimer)
                {
                    timerCounter = 0f;
                }
            }
        }
        
        /// <summary>
        /// Prüft Collectible-Anforderungen
        /// </summary>
        private bool CheckCollectibleRequirement()
        {
            if (levelManager == null) return false;
            
            int collectedCount = levelManager.TotalCollectibles - levelManager.CollectiblesRemaining;
            
            if (requireAllCollectibles)
            {
                return levelManager.IsLevelCompleted;
            }
            else
            {
                return collectedCount >= requiredCollectibles;
            }
        }
        
        /// <summary>
        /// Prüft ob alle Collectibles gesammelt wurden
        /// </summary>
        private bool CheckAllCollectiblesCollected()
        {
            if (levelManager == null) return false;
            return levelManager.IsLevelCompleted;
        }
        
        /// <summary>
        /// Prüft Spielernähe
        /// </summary>
        private bool CheckPlayerProximity()
        {
            if (playerTransform == null) return false;
            
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            return distance <= activationRange;
        }
        
        /// <summary>
        /// Prüft sequenzielle Anforderungen (Platzhalter für komplexere Logik)
        /// </summary>
        private bool CheckSequentialRequirement()
        {
            // Hier könnte eine komplexere Sequenz-Logik implementiert werden
            // Z.B. Prüfung anderer Tore, Switches, etc.
            return CheckCollectibleRequirement();
        }
        
        /// <summary>
        /// Öffnet das Tor
        /// </summary>
        public void OpenGate()
        {
            if (isOpen || isAnimating || (oneTimeUse && hasBeenUsed))
                return;
            
            if (logStateChanges)
                Debug.Log($"🚪 Öffne Tor '{name}'");
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            
            currentAnimation = StartCoroutine(AnimateGate(openPosition, true));
            
            // Steam-Burst auslösen
            if (steamBurst != null)
            {
                steamBurst.Emit(50);
            }
            
            // Sound abspielen
            PlaySound(gateOpenSound);
            
            if (oneTimeUse)
                hasBeenUsed = true;
            
            // Auto-Close Timer starten
            if (autoCloseDelay > 0 && gateType != GateType.TimerBased)
            {
                if (autoCloseCoroutine != null)
                    StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
            }
        }
        
        /// <summary>
        /// Schließt das Tor
        /// </summary>
        public void CloseGate()
        {
            if (!isOpen || isAnimating)
                return;
            
            if (logStateChanges)
                Debug.Log($"🚪 Schließe Tor '{name}'");
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            
            currentAnimation = StartCoroutine(AnimateGate(closedPosition, false));
            
            // Sound abspielen
            PlaySound(gateCloseSound);
        }
        
        /// <summary>
        /// Animiert das Tor zwischen Positionen
        /// </summary>
        private IEnumerator AnimateGate(Vector3 targetPosition, bool opening)
        {
            if (gateModel == null) yield break;
            
            isAnimating = true;
            Vector3 startPosition = useLocalSpace ? gateModel.localPosition : gateModel.position;
            
            // Mechanisches Geräusch während Animation
            if (mechanicalWhirring != null)
            {
                audioSource.clip = mechanicalWhirring;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            float elapsedTime = 0f;
            
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / animationDuration;
                float curveValue = animationCurve.Evaluate(progress);
                
                Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
                
                if (useLocalSpace)
                {
                    gateModel.localPosition = currentPosition;
                }
                else
                {
                    gateModel.position = currentPosition;
                }
                
                yield return null;
            }
            
            // Endposition sicherstellen
            if (useLocalSpace)
            {
                gateModel.localPosition = targetPosition;
            }
            else
            {
                gateModel.position = targetPosition;
            }
            
            isOpen = opening;
            isAnimating = false;
            
            // Kollider aktivieren/deaktivieren
            if (gateCollider != null && blockPlayer)
            {
                gateCollider.enabled = !isOpen;
            }
            
            // Audio stoppen
            if (mechanicalWhirring != null)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            
            if (logStateChanges)
                Debug.Log($"🚪 Tor '{name}' Animation abgeschlossen. Zustand: {(isOpen ? "Offen" : "Geschlossen")}");
        }
        
        /// <summary>
        /// Automatisches Schließen nach Verzögerung
        /// </summary>
        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            
            if (isOpen && !isAnimating)
            {
                CloseGate();
            }
        }
        
        /// <summary>
        /// Setzt Tor-Position sofort (ohne Animation)
        /// </summary>
        private void SetGatePosition(Vector3 position, bool immediate = false)
        {
            if (gateModel == null) return;
            
            if (useLocalSpace)
            {
                gateModel.localPosition = position;
            }
            else
            {
                gateModel.position = position;
            }
            
            // Kollider entsprechend setzen
            if (gateCollider != null && blockPlayer)
            {
                gateCollider.enabled = !isOpen;
            }
        }
        
        /// <summary>
        /// Aktualisiert visuelle Feedback-Elemente
        /// </summary>
        private void UpdateVisualFeedback()
        {
            if (statusLight == null) return;
            
            Color targetColor;
            
            if (isAnimating)
            {
                targetColor = activatingColor;
            }
            else if (isOpen)
            {
                targetColor = openColor;
            }
            else
            {
                targetColor = closedColor;
            }
            
            statusLight.color = targetColor;
            
            // Flackerndes Licht während Animation
            if (isAnimating)
            {
                statusLight.intensity = 1f + Mathf.Sin(Time.time * 10f) * 0.3f;
            }
            else
            {
                statusLight.intensity = 1f;
            }
        }
        
        /// <summary>
        /// Spielt einen Sound ab
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, 0.7f);
            }
        }
        
        /// <summary>
        /// Button-Aktivierung (kann von Triggern aufgerufen werden)
        /// </summary>
        public void ActivateButton()
        {
            if (gateType != GateType.ButtonActivated || buttonPressed)
                return;
            
            buttonPressed = true;
            PlaySound(buttonPressSound);
            
            // Button-Animation
            if (buttonModel != null)
            {
                Vector3 pressedPosition = originalButtonPosition - Vector3.up * buttonPressDepth;
                if (useLocalSpace)
                {
                    buttonModel.localPosition = pressedPosition;
                }
                else
                {
                    buttonModel.position = pressedPosition;
                }
            }
            
            // Button-Reset Timer
            if (buttonResetCoroutine != null)
                StopCoroutine(buttonResetCoroutine);
            buttonResetCoroutine = StartCoroutine(ResetButtonAfterDelay());
            
            if (logStateChanges)
                Debug.Log($"🔘 Button von Tor '{name}' aktiviert");
        }
        
        /// <summary>
        /// Setzt Button nach Verzögerung zurück
        /// </summary>
        private IEnumerator ResetButtonAfterDelay()
        {
            yield return new WaitForSeconds(buttonResetTime);
            
            buttonPressed = false;
            
            // Button-Position zurücksetzen
            if (buttonModel != null)
            {
                if (useLocalSpace)
                {
                    buttonModel.localPosition = originalButtonPosition;
                }
                else
                {
                    buttonModel.position = originalButtonPosition;
                }
            }
            
            if (logStateChanges)
                Debug.Log($"🔘 Button von Tor '{name}' zurückgesetzt");
        }
        
        /// <summary>
        /// Behandelt Kollisionen mit dem Spieler
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag(requiredTag))
                return;
            
            // Button-Aktivierung bei Kollision
            if (gateType == GateType.ButtonActivated)
            {
                ActivateButton();
            }
            
            // Bounce-Effekt
            if (bouncePlayer && !isOpen)
            {
                Rigidbody playerRigidbody = collision.rigidbody;
                if (playerRigidbody != null)
                {
                    Vector3 bounceDirection = (collision.transform.position - transform.position).normalized;
                    bounceDirection.y = 0.3f; // Leichte Aufwärtskraft
                    playerRigidbody.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
                }
            }
        }
        
        /// <summary>
        /// Setzt das Tor manuell auf einen bestimmten Zustand
        /// </summary>
        public void SetGateState(bool open)
        {
            if (open && !isOpen)
            {
                OpenGate();
            }
            else if (!open && isOpen)
            {
                CloseGate();
            }
        }
        
        /// <summary>
        /// Gibt den aktuellen Zustand des Tors zurück
        /// </summary>
        public bool IsOpen()
        {
            return isOpen;
        }
        
        /// <summary>
        /// Prüft ob das Tor gerade animiert wird
        /// </summary>
        public bool IsAnimating()
        {
            return isAnimating;
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Tor-Position
            Gizmos.color = isOpen ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
            
            // Aktivierungsreichweite für Proximity-Typ
            if (gateType == GateType.PlayerProximity)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, activationRange);
            }
            
            // Bewegungsbereich visualisieren
            if (gateModel != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(closedPosition, openPosition);
                Gizmos.DrawWireSphere(closedPosition, 0.2f);
                Gizmos.DrawWireSphere(openPosition, 0.2f);
            }
        }
        
        void OnValidate()
        {
            animationDuration = Mathf.Max(0.1f, animationDuration);
            activationRange = Mathf.Max(0.1f, activationRange);
            requiredCollectibles = Mathf.Max(0, requiredCollectibles);
            buttonResetTime = Mathf.Max(0.1f, buttonResetTime);
            autoCloseDelay = Mathf.Max(0f, autoCloseDelay);
            timerInterval = Mathf.Max(1f, timerInterval);
            bounceForce = Mathf.Max(0f, bounceForce);
        }
    }
}
