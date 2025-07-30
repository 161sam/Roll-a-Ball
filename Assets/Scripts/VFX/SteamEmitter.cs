using UnityEngine;

namespace RollABall.VFX
{
    /// <summary>
    /// Steampunk-Atmosphäreneffekte: Dampf, Rauch, Funken und mechanische Geräusche
    /// Erzeugt immersive Umgebungseffekte für prozedural generierte Level
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/VFX/Steam Emitter")]
    public class SteamEmitter : MonoBehaviour
    {
        [Header("Emitter-Typ")]
        [SerializeField] private EmitterType emitterType = EmitterType.Steam;
        [SerializeField] private bool randomizeOnStart = true;
        
        [Header("Partikelsystem")]
        [SerializeField] private ParticleSystem steamParticles;
        [SerializeField] private ParticleSystem sparksParticles;
        [SerializeField] private ParticleSystem smokeParticles;
        
        [Header("Emission-Einstellungen")]
        [SerializeField] private float baseEmissionRate = 50f;
        [SerializeField] private bool useRandomBursts = true;
        [SerializeField] private float burstInterval = 3f;
        [SerializeField] private Vector2 burstCountRange = new Vector2(20, 100);
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] steamSounds;
        [SerializeField] private AudioClip[] mechanicalSounds;
        [SerializeField] private bool playRandomAudio = true;
        [SerializeField] private Vector2 audioIntervalRange = new Vector2(2f, 8f);
        
        [Header("Umgebungseinfluss")]
        [SerializeField] private bool affectPlayerMovement = false;
        [SerializeField] private float windForce = 2f;
        [SerializeField] private float effectRadius = 5f;
        [SerializeField] private LayerMask affectedLayers = -1;
        
        [Header("Visuelle Dynamik")]
        [SerializeField] private bool useTemperatureVariation = true;
        [SerializeField] private AnimationCurve temperatureCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1f);
        [SerializeField] private float temperatureCycleDuration = 10f;
        
        [Header("Beleuchtung")]
        [SerializeField] private Light emitterLight;
        [SerializeField] private bool flickerLight = true;
        [SerializeField] private Vector2 flickerIntensityRange = new Vector2(0.5f, 2f);
        [SerializeField] private Color baseColor = Color.orange;
        
        [Header("Debug")]
        [SerializeField] private bool showEffectRadius = true;
        [SerializeField] private Color debugColor = Color.cyan;
        
        // Private Variablen
        private float burstTimer = 0f;
        private float audioTimer = 0f;
        private float temperatureTimer = 0f;
        private float nextAudioTime = 0f;
        private Transform playerTransform;
        private Rigidbody playerRigidbody;
        private ParticleSystem.EmissionModule steamEmission;
        private ParticleSystem.EmissionModule sparksEmission;
        private ParticleSystem.EmissionModule smokeEmission;
        private float baseLightIntensity;
        
        // Enums
        public enum EmitterType
        {
            Steam,          // Dampfpfeife
            Furnace,        // Ofen mit Funken
            Pipe,           // Rohrleitungen
            Engine,         // Maschinengeräusche
            Geyser,         // Geysir-ähnliche Ausbrüche
            Chimney         // Schornstein mit Rauch
        }
        
        void Start()
        {
            InitializeEmitter();
        }
        
        /// <summary>
        /// Initialisiert alle Komponenten des Steam Emitters
        /// </summary>
        private void InitializeEmitter()
        {
            // Spieler finden
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerRigidbody = player.GetComponent<Rigidbody>();
            }
            
            // Audio Setup
            SetupAudio();
            
            // Partikelsystem konfigurieren
            SetupParticleSystems();
            
            // Beleuchtung konfigurieren
            SetupLighting();
            
            // Zufällige Startwerte wenn gewünscht
            if (randomizeOnStart)
            {
                RandomizeSettings();
            }
            
            // Emitter-Typ spezifische Konfiguration
            ConfigureEmitterType();
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
            
            audioSource.spatialBlend = 1f; // 3D Audio
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = effectRadius * 2f;
            audioSource.volume = 0.3f;
            
            SetNextAudioTime();
        }
        
        /// <summary>
        /// Konfiguriert alle Partikelsysteme
        /// </summary>
        private void SetupParticleSystems()
        {
            // Steam Particles Setup
            if (steamParticles != null)
            {
                steamEmission = steamParticles.emission;
                ConfigureSteamParticles();
            }
            
            // Sparks Particles Setup
            if (sparksParticles != null)
            {
                sparksEmission = sparksParticles.emission;
                ConfigureSparksParticles();
            }
            
            // Smoke Particles Setup
            if (smokeParticles != null)
            {
                smokeEmission = smokeParticles.emission;
                ConfigureSmokeParticles();
            }
        }
        
        /// <summary>
        /// Konfiguriert Steam-Partikel
        /// </summary>
        private void ConfigureSteamParticles()
        {
            var main = steamParticles.main;
            main.startLifetime = 3f;
            main.startSpeed = 2f;
            main.startSize = 0.5f;
            main.startColor = new Color(1f, 1f, 1f, 0.7f);
            main.maxParticles = 100;
            
            var shape = steamParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            
            var velocityOverLifetime = steamParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(3f);
            
            steamEmission.rateOverTime = baseEmissionRate;
        }
        
        /// <summary>
        /// Konfiguriert Funken-Partikel
        /// </summary>
        private void ConfigureSparksParticles()
        {
            var main = sparksParticles.main;
            main.startLifetime = 1f;
            main.startSpeed = 5f;
            main.startSize = 0.1f;
            main.startColor = new Color(1f, 0.5f, 0f, 1f);
            main.maxParticles = 50;
            
            var emission = sparksParticles.emission;
            emission.rateOverTime = 0f; // Nur Bursts
            
            var shape = sparksParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.2f;
        }
        
        /// <summary>
        /// Konfiguriert Rauch-Partikel
        /// </summary>
        private void ConfigureSmokeParticles()
        {
            var main = smokeParticles.main;
            main.startLifetime = 5f;
            main.startSpeed = 1f;
            main.startSize = 1f;
            main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            main.maxParticles = 200;
            
            var sizeOverLifetime = smokeParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 
                AnimationCurve.Linear(0f, 0.5f, 1f, 2f));
                
            smokeEmission.rateOverTime = baseEmissionRate * 0.3f;
        }
        
        /// <summary>
        /// Konfiguriert Beleuchtung
        /// </summary>
        private void SetupLighting()
        {
            if (emitterLight != null)
            {
                baseLightIntensity = emitterLight.intensity;
                emitterLight.color = baseColor;
                emitterLight.range = effectRadius;
            }
        }
        
        /// <summary>
        /// Randomisiert Einstellungen für natürliche Variation
        /// </summary>
        private void RandomizeSettings()
        {
            burstInterval = Random.Range(2f, 6f);
            baseEmissionRate = Random.Range(30f, 80f);
            temperatureCycleDuration = Random.Range(8f, 15f);
            // TODO: Make random ranges configurable via serialized fields
            
            // Zufällige Startzeit für Audio
            audioTimer = Random.Range(0f, audioIntervalRange.y);
        }
        
        /// <summary>
        /// Konfiguriert emitterspezifische Eigenschaften
        /// </summary>
        private void ConfigureEmitterType()
        {
            switch (emitterType)
            {
                case EmitterType.Steam:
                    EnableSteamMode();
                    break;
                case EmitterType.Furnace:
                    EnableFurnaceMode();
                    break;
                case EmitterType.Pipe:
                    EnablePipeMode();
                    break;
                case EmitterType.Engine:
                    EnableEngineMode();
                    break;
                case EmitterType.Geyser:
                    EnableGeyserMode();
                    break;
                case EmitterType.Chimney:
                    EnableChimneyMode();
                    break;
            }
        }
        
        private void EnableSteamMode()
        {
            if (steamParticles != null) steamParticles.gameObject.SetActive(true);
            if (sparksParticles != null) sparksParticles.gameObject.SetActive(false);
            if (smokeParticles != null) smokeParticles.gameObject.SetActive(false);
        }
        
        private void EnableFurnaceMode()
        {
            if (steamParticles != null) steamParticles.gameObject.SetActive(true);
            if (sparksParticles != null) sparksParticles.gameObject.SetActive(true);
            if (smokeParticles != null) smokeParticles.gameObject.SetActive(false);
            useRandomBursts = true;
        }
        
        private void EnablePipeMode()
        {
            if (steamParticles != null) steamParticles.gameObject.SetActive(true);
            if (sparksParticles != null) sparksParticles.gameObject.SetActive(false);
            if (smokeParticles != null) smokeParticles.gameObject.SetActive(false);
            burstInterval = 0.5f; // Häufige kleine Stöße
        }
        
        private void EnableEngineMode()
        {
            if (steamParticles != null) steamParticles.gameObject.SetActive(true);
            if (sparksParticles != null) sparksParticles.gameObject.SetActive(true);
            if (smokeParticles != null) smokeParticles.gameObject.SetActive(true);
            affectPlayerMovement = true;
        }
        
        private void EnableGeyserMode()
        {
            if (steamParticles != null) steamParticles.gameObject.SetActive(true);
            if (sparksParticles != null) sparksParticles.gameObject.SetActive(false);
            if (smokeParticles != null) smokeParticles.gameObject.SetActive(false);
            burstInterval = 8f; // Seltene, große Ausbrüche
            burstCountRange = new Vector2(100, 300);
        }
        
        private void EnableChimneyMode()
        {
            if (steamParticles != null) steamParticles.gameObject.SetActive(false);
            if (sparksParticles != null) sparksParticles.gameObject.SetActive(false);
            if (smokeParticles != null) smokeParticles.gameObject.SetActive(true);
        }
        
        void Update()
        {
            HandleBursts();
            HandleAudio();
            HandleTemperatureVariation();
            HandleLightFlicker();
            HandlePlayerEffects();
        }
        
        /// <summary>
        /// Behandelt zufällige Partikel-Bursts
        /// </summary>
        private void HandleBursts()
        {
            if (!useRandomBursts) return;
            
            burstTimer += Time.deltaTime;
            
            if (burstTimer >= burstInterval)
            {
                TriggerBurst();
                burstTimer = 0f;
                burstInterval = Random.Range(2f, 6f); // Nächster Burst-Zeitpunkt
            }
        }
        
        /// <summary>
        /// Löst einen Partikel-Burst aus
        /// </summary>
        private void TriggerBurst()
        {
            int burstCount = (int)Random.Range(burstCountRange.x, burstCountRange.y);
            
            if (steamParticles != null && steamParticles.gameObject.activeInHierarchy)
            {
                steamParticles.Emit(burstCount);
            }
            
            if (sparksParticles != null && sparksParticles.gameObject.activeInHierarchy)
            {
                sparksParticles.Emit(burstCount / 5); // Weniger Funken
            }
        }
        
        /// <summary>
        /// Behandelt zufällige Audio-Wiedergabe
        /// </summary>
        private void HandleAudio()
        {
            if (!playRandomAudio || audioSource == null) return;
            
            audioTimer += Time.deltaTime;
            
            if (audioTimer >= nextAudioTime)
            {
                PlayRandomAudio();
                SetNextAudioTime();
                audioTimer = 0f;
            }
        }
        
        /// <summary>
        /// Spielt zufällige Audio-Clips ab
        /// </summary>
        private void PlayRandomAudio()
        {
            AudioClip[] soundArray = emitterType == EmitterType.Engine || emitterType == EmitterType.Furnace 
                ? mechanicalSounds : steamSounds;
                
            if (soundArray != null && soundArray.Length > 0)
            {
                AudioClip randomClip = soundArray[Random.Range(0, soundArray.Length)];
                if (randomClip != null)
                {
                    audioSource.PlayOneShot(randomClip, Random.Range(0.3f, 0.7f));
                }
            }
        }
        
        /// <summary>
        /// Setzt die nächste Audio-Zeit
        /// </summary>
        private void SetNextAudioTime()
        {
            nextAudioTime = Random.Range(audioIntervalRange.x, audioIntervalRange.y);
        }
        
        /// <summary>
        /// Behandelt Temperaturvariationen für dynamische Effekte
        /// </summary>
        private void HandleTemperatureVariation()
        {
            if (!useTemperatureVariation) return;
            
            temperatureTimer += Time.deltaTime;
            float normalizedTime = (temperatureTimer % temperatureCycleDuration) / temperatureCycleDuration;
            float temperatureFactor = temperatureCurve.Evaluate(normalizedTime);
            
            // Emission-Rate anpassen
            if (steamEmission.enabled)
            {
                steamEmission.rateOverTime = baseEmissionRate * temperatureFactor;
            }
            
            // Lichtintensität anpassen
            if (emitterLight != null && !flickerLight)
            {
                emitterLight.intensity = baseLightIntensity * temperatureFactor;
            }
        }
        
        /// <summary>
        /// Behandelt Licht-Flackern
        /// </summary>
        private void HandleLightFlicker()
        {
            if (!flickerLight || emitterLight == null) return;
            
            float flickerValue = Random.Range(flickerIntensityRange.x, flickerIntensityRange.y);
            emitterLight.intensity = baseLightIntensity * flickerValue;
        }
        
        /// <summary>
        /// Behandelt Effekte auf den Spieler
        /// </summary>
        private void HandlePlayerEffects()
        {
            if (!affectPlayerMovement || playerTransform == null || playerRigidbody == null)
                return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= effectRadius)
            {
                ApplyWindEffect(distanceToPlayer);
            }
        }
        
        /// <summary>
        /// Wendet Wind-Effekt auf den Spieler an
        /// </summary>
        private void ApplyWindEffect(float distance)
        {
            float effectStrength = 1f - (distance / effectRadius);
            Vector3 windDirection = transform.up; // Nach oben für Steam
            
            // Verschiedene Windrichtungen je nach Emitter-Typ
            switch (emitterType)
            {
                case EmitterType.Pipe:
                    windDirection = transform.forward;
                    break;
                case EmitterType.Geyser:
                    windDirection = Vector3.up;
                    break;
            }
            
            Vector3 windForceVector = windDirection * windForce * effectStrength * Time.deltaTime;
            playerRigidbody.AddForce(windForceVector, ForceMode.Force);
        }
        
        /// <summary>
        /// Aktiviert/Deaktiviert den Emitter
        /// </summary>
        public void SetActive(bool active)
        {
            enabled = active;
            
            if (steamParticles != null)
            {
                if (active) steamParticles.Play();
                else steamParticles.Stop();
            }
            
            if (sparksParticles != null)
            {
                if (active) sparksParticles.Play();
                else sparksParticles.Stop();
            }
            
            if (smokeParticles != null)
            {
                if (active) smokeParticles.Play();
                else smokeParticles.Stop();
            }
            
            if (emitterLight != null)
            {
                emitterLight.enabled = active;
            }
        }
        
        /// <summary>
        /// Ändert den Emitter-Typ zur Laufzeit
        /// </summary>
        public void ChangeEmitterType(EmitterType newType)
        {
            emitterType = newType;
            ConfigureEmitterType();
        }
        
        void OnDrawGizmos()
        {
            if (!showEffectRadius) return;
            
            Gizmos.color = debugColor;
            Gizmos.DrawWireSphere(transform.position, effectRadius);
            
            // Richtungsanzeige
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
        }
        
        void OnValidate()
        {
            baseEmissionRate = Mathf.Max(1f, baseEmissionRate);
            burstInterval = Mathf.Max(0.1f, burstInterval);
            effectRadius = Mathf.Max(0.1f, effectRadius);
            windForce = Mathf.Max(0f, windForce);
            temperatureCycleDuration = Mathf.Max(1f, temperatureCycleDuration);
        }
    }
}
