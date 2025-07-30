using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace RollABall.Generators
{
    /// <summary>
    /// Modulare Klasse für Material-Anwendung und Steampunk-Atmosphäre
    /// Extrahiert aus LevelGenerator.cs für bessere Wartbarkeit
    /// Verantwortlichkeiten: Materialien, Partikeleffekte, Steam Emitter, Atmosphäre
    /// </summary>
    [System.Serializable]
    public class LevelEffectManager
    {
        #region Events
        public System.Action<int> OnEffectsApplied;
        public System.Action<int> OnSteamEmittersPlaced;
        public System.Action<string> OnEffectError;
        #endregion

        #region Private Data
        private List<Vector3> steamEmitterPositions;
        private List<GameObject> activeEffects;
        private List<GameObject> activeSteamEmitters;
        private System.Random random;
        
        // Dependencies (injected)
        private LevelProfile activeProfile;
        private List<Vector2Int> walkableTiles;
        private List<Vector2Int> platformCenters;
        private LevelGenerationMode usedGenerationMode;
        private Transform effectsContainer;
        private bool showGenerationDebug;
        
        // Sector-based material assignment
        private Material[] sectorGroundMaterials;
        private Material[] sectorWallMaterials;
        private bool useSectorMaterials;
        #endregion

        #region Public Properties
        public List<Vector3> SteamEmitterPositions => steamEmitterPositions ?? new List<Vector3>();
        public int ActiveEffectsCount => activeEffects?.Count ?? 0;
        public int ActiveSteamEmittersCount => activeSteamEmitters?.Count ?? 0;
        public bool UseSectorMaterials => useSectorMaterials;
        #endregion

        #region Initialization
        /// <summary>
        /// Initialisiert den Effect Manager mit den erforderlichen Dependencies
        /// </summary>
        public void Initialize(LevelProfile profile, List<Vector2Int> walkable, List<Vector2Int> platforms,
                             LevelGenerationMode mode, Transform container, System.Random rng, bool debug = false)
        {
            activeProfile = profile ?? throw new ArgumentNullException(nameof(profile));
            walkableTiles = walkable ?? throw new ArgumentNullException(nameof(walkable));
            platformCenters = platforms ?? new List<Vector2Int>();
            usedGenerationMode = mode;
            effectsContainer = container ?? throw new ArgumentNullException(nameof(container));
            random = rng ?? throw new ArgumentNullException(nameof(rng));
            showGenerationDebug = debug;

            // Initialize collections
            steamEmitterPositions = new List<Vector3>();
            activeEffects = new List<GameObject>();
            activeSteamEmitters = new List<GameObject>();

            // Setup sector-based materials
            InitializeSectorMaterials();

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelEffectManager] Initialized for {mode} mode with {walkableTiles.Count} walkable tiles, " +
                         $"{platformCenters.Count} platform centers, sector materials: {useSectorMaterials}");
            }
        }

        /// <summary>
        /// Validiert ob der Effect Manager korrekt initialisiert wurde
        /// </summary>
        public bool IsInitialized()
        {
            bool isValid = activeProfile != null && 
                          walkableTiles != null && 
                          effectsContainer != null && 
                          random != null;
            
            if (!isValid && showGenerationDebug)
            {
                Debug.LogWarning($"[LevelEffectManager] Not properly initialized. " +
                               $"Profile: {activeProfile != null}, " +
                               $"WalkableTiles: {walkableTiles != null}, " +
                               $"Container: {effectsContainer != null}, " +
                               $"Random: {random != null}");
            }
            
            return isValid;
        }

        /// <summary>
        /// Initialisiert sector-basierte Material-Zuweisung
        /// </summary>
        private void InitializeSectorMaterials()
        {
            useSectorMaterials = activeProfile != null && activeProfile.LevelSize >= 16;
            
            if (useSectorMaterials)
            {
                sectorGroundMaterials = new Material[4];
                sectorWallMaterials = new Material[4];

                for (int i = 0; i < 4; i++)
                {
                    if (activeProfile.GroundMaterials != null && activeProfile.GroundMaterials.Length > 0)
                    {
                        sectorGroundMaterials[i] = activeProfile.GroundMaterials[random.Next(activeProfile.GroundMaterials.Length)];
                    }
                    // Note: sectorGroundMaterials[i] remains null if no materials available

                    if (activeProfile.WallMaterials != null && activeProfile.WallMaterials.Length > 0)
                    {
                        sectorWallMaterials[i] = activeProfile.WallMaterials[random.Next(activeProfile.WallMaterials.Length)];
                    }
                    // Note: sectorWallMaterials[i] remains null if no materials available
                }

                if (showGenerationDebug)
                {
                    Debug.Log($"[LevelEffectManager] Initialized sector materials for 4 zones. " +
                             $"Ground materials available: {activeProfile.GroundMaterials?.Length ?? 0}, " +
                             $"Wall materials available: {activeProfile.WallMaterials?.Length ?? 0}");
                }
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Wendet alle Materialien und Effekte an
        /// Hauptmethode - entspricht ApplyMaterialsAndEffects() aus LevelGenerator
        /// </summary>
        public IEnumerator ApplyAllMaterialsAndEffects()
        {
            if (!IsInitialized())
            {
                OnEffectError?.Invoke("LevelEffectManager not initialized");
                yield break;
            }

            int totalEffects = 0;

            // Apply decorative particle effects
            yield return ApplyDecorativeEffectsCoroutine();
            totalEffects += activeEffects.Count;

            // Apply steam emitter effects
            yield return ApplySteamEmittersCoroutine();
            totalEffects += activeSteamEmitters.Count;

            // Apply ambient atmosphere
            yield return ApplyAmbientAtmosphereCoroutine();

            OnEffectsApplied?.Invoke(totalEffects);

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelEffectManager] Applied {totalEffects} total effects " +
                         $"({activeEffects.Count} decorative, {activeSteamEmitters.Count} steam emitters)");
            }
        }

        /// <summary>
        /// Wendet Material auf ein GameObject an (sector-based oder zufällig)
        /// </summary>
        public void ApplyTileMaterial(GameObject obj, Material[] materials, int gx = 0, int gz = 0)
        {
            if (materials == null || materials.Length == 0) return;

            Material material = useSectorMaterials
                ? GetSectorMaterial(materials, gx, gz)
                : materials[random.Next(materials.Length)];

            ApplyMaterialToObject(obj, material);
        }

        /// <summary>
        /// Wendet Ground Material auf ein GameObject an
        /// </summary>
        public void ApplyGroundMaterial(GameObject obj, int gx = 0, int gz = 0)
        {
            if (obj == null) return;
            
            if (useSectorMaterials && sectorGroundMaterials != null)
            {
                int sector = CalculateSector(gx, gz);
                if (sector >= 0 && sector < sectorGroundMaterials.Length && sectorGroundMaterials[sector] != null)
                {
                    ApplyMaterialToObject(obj, sectorGroundMaterials[sector]);
                }
            }
            else if (activeProfile != null && activeProfile.GroundMaterials != null && activeProfile.GroundMaterials.Length > 0)
            {
                Material material = activeProfile.GroundMaterials[random.Next(activeProfile.GroundMaterials.Length)];
                ApplyMaterialToObject(obj, material);
            }
        }

        /// <summary>
        /// Wendet Wall Material auf ein GameObject an
        /// </summary>
        public void ApplyWallMaterial(GameObject obj, int gx = 0, int gz = 0)
        {
            if (obj == null) return;
            
            if (useSectorMaterials && sectorWallMaterials != null)
            {
                int sector = CalculateSector(gx, gz);
                if (sector >= 0 && sector < sectorWallMaterials.Length && sectorWallMaterials[sector] != null)
                {
                    ApplyMaterialToObject(obj, sectorWallMaterials[sector]);
                }
            }
            else if (activeProfile != null && activeProfile.WallMaterials != null && activeProfile.WallMaterials.Length > 0)
            {
                Material material = activeProfile.WallMaterials[random.Next(activeProfile.WallMaterials.Length)];
                ApplyMaterialToObject(obj, material);
            }
        }

        /// <summary>
        /// Wendet rutschige Physik-Materialien an falls aktiviert
        /// </summary>
        public void ApplySlipperyPhysics(GameObject obj)
        {
            if (obj == null || activeProfile == null ||
                !activeProfile.EnableSlipperyTiles || activeProfile.SlipperyMaterial == null)
                return;

            if (random.NextDouble() < activeProfile.SlipperyTileChance)
            {
                Collider collider = obj.GetComponent<Collider>();
                if (collider)
                {
                    collider.material = activeProfile.SlipperyMaterial;
                    
                    if (showGenerationDebug)
                    {
                        Debug.Log($"[LevelEffectManager] Applied slippery material to {obj.name}");
                    }
                }
            }
        }

        /// <summary>
        /// Zurücksetzen für neue Generierung
        /// </summary>
        public void Reset()
        {
            ClearAllEffects();
            steamEmitterPositions?.Clear();
            activeEffects?.Clear();
            activeSteamEmitters?.Clear();
        }
        #endregion

        #region Decorative Effects
        /// <summary>
        /// Wendet dekorative Partikeleffekte an
        /// MERGED: Enhanced with particle effects from REMOTE
        /// </summary>
        private IEnumerator ApplyDecorativeEffectsCoroutine()
        {
            if (!activeProfile.EnableParticleEffects || activeProfile.DecorativeEffects == null)
            {
                yield break;
            }

            int effectCount = Mathf.RoundToInt(walkableTiles.Count * 0.05f); // 5% of walkable tiles
            effectCount = Mathf.Min(effectCount, activeProfile.DecorativeEffects.Length);

            for (int i = 0; i < effectCount; i++)
            {
                if (walkableTiles.Count > 0)
                {
                    Vector2Int tilePos = walkableTiles[random.Next(walkableTiles.Count)];
                    Vector3 worldPos = new Vector3(
                        tilePos.x * activeProfile.TileSize,
                        activeProfile.CollectibleSpawnHeight,
                        tilePos.y * activeProfile.TileSize
                    );

                    GameObject effect = activeProfile.DecorativeEffects[random.Next(activeProfile.DecorativeEffects.Length)];
                    if (effect)
                    {
                        GameObject effectInstance = CreateOrPoolObject(effect, worldPos, Quaternion.identity, effectsContainer);
                        activeEffects.Add(effectInstance);

                        // Optional: Randomize rotation for variety
                        effectInstance.transform.rotation = Quaternion.Euler(0, random.Next(0, 360), 0);
                    }
                }

                // Yield every 5 effects to prevent frame drops
                if (i % 5 == 0)
                    yield return null;
            }

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelEffectManager] Placed {activeEffects.Count} decorative effects");
            }
        }
        #endregion

        #region Steam Emitters
        /// <summary>
        /// Wendet Steam Emitter Effekte an
        /// MERGED: Steam emitter integration from REMOTE
        /// </summary>
        private IEnumerator ApplySteamEmittersCoroutine()
        {
            if (usedGenerationMode != LevelGenerationMode.Platforms || platformCenters.Count == 0)
            {
                yield break;
            }

            // Get steam emitter properties
            var enableSteamEmitters = activeProfile.GetType().GetProperty("EnableSteamEmitters");
            var steamEmitterPrefabs = activeProfile.GetType().GetProperty("SteamEmitterPrefabs");
            var steamEmitterDensity = activeProfile.GetType().GetProperty("SteamEmitterDensity");
            
            bool canUseSteam = enableSteamEmitters != null && steamEmitterPrefabs != null && steamEmitterDensity != null;
            
            if (!canUseSteam)
            {
                yield break;
            }

            bool steamEnabled = false;
            GameObject[] prefabs = null;
            float density = 0f;
            
            try
            {
            steamEnabled = (bool)enableSteamEmitters.GetValue(activeProfile);
            prefabs = steamEmitterPrefabs.GetValue(activeProfile) as GameObject[];
            density = (float)steamEmitterDensity.GetValue(activeProfile);
            }
            catch (Exception e)
            {
            OnEffectError?.Invoke($"Steam emitter setup failed: {e.Message}");
            Debug.LogWarning($"[LevelEffectManager] Steam emitter setup failed: {e.Message}");
            steamEnabled = false; // Set to false to exit early
            }
            
            if (!steamEnabled)
            {
            yield break;
            }
            
            if (prefabs != null && prefabs.Length > 0)
            {
            steamEmitterPositions.Clear();
            
            foreach (Vector2Int center in platformCenters)
            {
            if (random.NextDouble() <= density)
            {
                    GameObject prefab = prefabs[random.Next(prefabs.Length)];
            if (prefab)
            {
            Vector3 pos = new Vector3(
                    center.x * activeProfile.TileSize,
                            activeProfile.CollectibleSpawnHeight + 0.5f,
                    center.y * activeProfile.TileSize);

                        GameObject emitter = CreateOrPoolObject(prefab, pos, Quaternion.identity, effectsContainer);
                    steamEmitterPositions.Add(pos);
                        activeSteamEmitters.Add(emitter);

                    // Apply steam settings if available
                ApplySteamEmitterSettings(emitter);
            }
            }

                    yield return null; // Yield after each platform check
                }

            OnSteamEmittersPlaced?.Invoke(activeSteamEmitters.Count);

                if (showGenerationDebug)
            {
                Debug.Log($"[LevelEffectManager] Placed {activeSteamEmitters.Count} steam emitters " +
                         $"on {platformCenters.Count} platforms (density: {density:F2})");
            }
        }
        }

        /// <summary>
        /// Wendet spezielle Einstellungen auf Steam Emitter an
        /// </summary>
        private void ApplySteamEmitterSettings(GameObject emitter)
        {
            try
            {
                // Look for SteamEmitterController or similar components
                var steamController = emitter.GetComponent<MonoBehaviour>();
                if (steamController != null)
                {
                    // Apply settings via reflection if available
                    var steamIntensityProp = steamController.GetType().GetProperty("Intensity");
                    if (steamIntensityProp != null)
                    {
                        float intensity = 0.5f + (float)random.NextDouble() * 0.5f; // Random 0.5-1.0
                        steamIntensityProp.SetValue(steamController, intensity);
                    }

                    var steamColorProp = steamController.GetType().GetProperty("SteamColor");
                    if (steamColorProp != null && activeProfile.ThemeColor != Color.clear)
                    {
                        // Tint steam with theme color
                        Color steamColor = Color.Lerp(Color.white, activeProfile.ThemeColor, 0.3f);
                        steamColorProp.SetValue(steamController, steamColor);
                    }
                }

                // Apply audio settings if AudioSource present
                AudioSource audioSource = emitter.GetComponent<AudioSource>();
                if (audioSource)
                {
                    audioSource.volume = 0.3f + (float)random.NextDouble() * 0.4f; // Random 0.3-0.7
                    audioSource.pitch = 0.8f + (float)random.NextDouble() * 0.4f; // Random 0.8-1.2
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelEffectManager] Failed to apply steam emitter settings: {e.Message}");
            }
        }
        #endregion

        #region Ambient Atmosphere
        /// <summary>
        /// Wendet Ambient-Atmosphäre an (Beleuchtung, Nebel, Ambient-Sound)
        /// </summary>
        private IEnumerator ApplyAmbientAtmosphereCoroutine()
        {
            // Apply fog settings for steampunk atmosphere
            ApplySteampunkFog();
            
            // Apply ambient lighting
            ApplySteampunkLighting();
            
            // Apply ambient sound if available
            ApplyAmbientSound();

            yield return null;
        }

        /// <summary>
        /// Wendet Steampunk-Nebel-Einstellungen an
        /// </summary>
        private void ApplySteampunkFog()
        {
            try
            {
                if (activeProfile.ThemeColor != Color.clear)
                {
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = Color.Lerp(activeProfile.ThemeColor, Color.gray, 0.7f);
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    RenderSettings.fogDensity = 0.01f;

                    if (showGenerationDebug)
                    {
                        Debug.Log($"[LevelEffectManager] Applied steampunk fog with color {RenderSettings.fogColor}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelEffectManager] Failed to apply fog settings: {e.Message}");
            }
        }

        /// <summary>
        /// Wendet Steampunk-Beleuchtung an
        /// </summary>
        private void ApplySteampunkLighting()
        {
            try
            {
                if (activeProfile.ThemeColor != Color.clear)
                {
                    Color ambientColor = Color.Lerp(activeProfile.ThemeColor, new Color(0.2f, 0.15f, 0.1f), 0.5f);
                    RenderSettings.ambientLight = ambientColor;

                    if (showGenerationDebug)
                    {
                        Debug.Log($"[LevelEffectManager] Applied steampunk ambient lighting: {ambientColor}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelEffectManager] Failed to apply lighting settings: {e.Message}");
            }
        }

        /// <summary>
        /// Wendet Ambient-Sound für Steampunk-Atmosphäre an
        /// </summary>
        private void ApplyAmbientSound()
        {
            try
            {
                // Look for existing ambient audio or create one
                GameObject ambientAudio = GameObject.Find("AmbientAudio");
                if (ambientAudio == null)
                {
                    ambientAudio = new GameObject("AmbientAudio");
                    ambientAudio.transform.SetParent(effectsContainer);
                }

                AudioSource audioSource = ambientAudio.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = ambientAudio.AddComponent<AudioSource>();
                }

                // Configure ambient audio settings
                audioSource.volume = 0.2f;
                audioSource.loop = true;
                audioSource.playOnAwake = true;
                audioSource.spatialBlend = 0f; // 2D sound

                if (showGenerationDebug)
                {
                    Debug.Log($"[LevelEffectManager] Configured ambient audio settings");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelEffectManager] Failed to apply ambient sound: {e.Message}");
            }
        }
        #endregion

        #region Material Utilities
        /// <summary>
        /// Berechnet den Sektor (0-3) für eine Grid-Position
        /// </summary>
        private int CalculateSector(int gx, int gz)
        {
            int half = activeProfile.LevelSize / 2;
            bool right = gx >= half;
            bool top = gz >= half;
            if (top)
                return right ? 3 : 2;
            return right ? 1 : 0;
        }

        /// <summary>
        /// Holt das sector-basierte Material
        /// </summary>
        private Material GetSectorMaterial(Material[] materials, int gx, int gz)
        {
            if (useSectorMaterials)
            {
                int sector = CalculateSector(gx, gz);
                return materials == sectorGroundMaterials ? sectorGroundMaterials[sector] : sectorWallMaterials[sector];
            }
            else
            {
                return materials[random.Next(materials.Length)];
            }
        }

        /// <summary>
        /// Wendet ein Material auf ein GameObject an
        /// </summary>
        private void ApplyMaterialToObject(GameObject obj, Material material)
        {
            if (material && obj)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer)
                    renderer.material = material;
            }
        }
        #endregion

        #region Object Creation & Management
        /// <summary>
        /// Object pooling mit Fallback auf Instantiate
        /// MERGED: Object pooling with fallback
        /// </summary>
        private GameObject CreateOrPoolObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject obj = null;
            
            // Try object pooling first
            try
            {
                var poolerType = System.Type.GetType("PrefabPooler");
                if (poolerType != null)
                {
                    var getMethod = poolerType.GetMethod("Get", new Type[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Transform) });
                    if (getMethod != null)
                    {
                        obj = (GameObject)getMethod.Invoke(null, new object[] { prefab, position, rotation, parent });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelEffectManager] Object pooling not available: {e.Message}");
            }

            // Fallback to instantiate
            if (obj == null)
            {
                obj = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            }

            return obj;
        }

        /// <summary>
        /// Löscht alle aktiven Effekte
        /// </summary>
        private void ClearAllEffects()
        {
            ClearEffectsList(activeEffects);
            ClearEffectsList(activeSteamEmitters);
        }

        /// <summary>
        /// Löscht eine Liste von Effekt-GameObjects
        /// </summary>
        private void ClearEffectsList(List<GameObject> effects)
        {
            if (effects == null) return;

            foreach (GameObject effect in effects)
            {
                if (effect)
                {
                    // Try pooling first, fallback to destroy
                    bool pooled = false;
                    try
                    {
                        var poolerType = System.Type.GetType("PrefabPooler");
                        if (poolerType != null)
                        {
                            var releaseMethod = poolerType.GetMethod("Release");
                            if (releaseMethod != null)
                            {
                                releaseMethod.Invoke(null, new object[] { effect });
                                pooled = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[LevelEffectManager] Object pooling release failed: {e.Message}");
                    }

                    if (!pooled)
                    {
                        if (Application.isEditor)
                            UnityEngine.Object.DestroyImmediate(effect);
                        else
                            UnityEngine.Object.Destroy(effect);
                    }
                }
            }

            effects.Clear();
        }
        #endregion

        #region Debug Information
        /// <summary>
        /// Gibt Debug-Informationen über die angewandten Effekte aus
        /// </summary>
        public void LogEffectInfo()
        {
            if (!IsInitialized()) return;

            Debug.Log($"=== LevelEffectManager Debug Info ===");
            Debug.Log($"Generation Mode: {usedGenerationMode}");
            Debug.Log($"Active Effects: {activeEffects.Count}");
            Debug.Log($"Steam Emitters: {activeSteamEmitters.Count}");
            Debug.Log($"Steam Emitter Positions: {steamEmitterPositions.Count}");
            Debug.Log($"Use Sector Materials: {useSectorMaterials}");
            Debug.Log($"Particle Effects Enabled: {activeProfile.EnableParticleEffects}");
            Debug.Log($"Platform Centers: {platformCenters.Count}");

            if (useSectorMaterials)
            {
                Debug.Log($"Sector Ground Materials: {(sectorGroundMaterials?.Length ?? 0)}");
                Debug.Log($"Sector Wall Materials: {(sectorWallMaterials?.Length ?? 0)}");
            }
        }

        /// <summary>
        /// Visualisiert Steam Emitter Positionen in Scene View
        /// </summary>
        public void DrawSteamEmitterGizmos(float tileSize)
        {
            if (steamEmitterPositions == null) return;

            Gizmos.color = Color.white;
            foreach (Vector3 pos in steamEmitterPositions)
            {
                Gizmos.DrawSphere(pos, tileSize * 0.2f);
                // Draw steam effect as line upward
                Gizmos.DrawLine(pos, pos + Vector3.up * tileSize);
            }
        }
        #endregion
    }
}
