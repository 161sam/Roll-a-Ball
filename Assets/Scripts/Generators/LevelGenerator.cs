using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using RollABall.Generators;

/// <summary>
/// Hauptklasse für die prozedurale Levelgenerierung - REFACTORED VERSION
/// Orchestriert modulare Komponenten für bessere Wartbarkeit
/// Generiert Level basierend auf LevelProfile-Konfigurationen
/// </summary>
[AddComponentMenu("Game/LevelGenerator")]
public class LevelGenerator : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private LevelProfile activeProfile;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool allowRegeneration = true;
    [SerializeField] private KeyCode regenerateKey = KeyCode.R;

    [Header("Prefab References")]
    [SerializeField] private GameObject groundPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject collectiblePrefab;
    [SerializeField] private GameObject goalZonePrefab;
    [SerializeField] private GameObject playerPrefab;

    [Header("Container Objects")]
    [SerializeField] private Transform levelContainer;
    [SerializeField] private Transform groundContainer;
    [SerializeField] private Transform wallContainer;
    [SerializeField] private Transform collectibleContainer;
    [SerializeField] private Transform effectsContainer;

    [Header("Debug & Visualization")]
    [SerializeField] private bool showGenerationDebug = false;
    [SerializeField] private bool showWalkableArea = false;
    [SerializeField] private Color walkableColor = Color.green;
    [SerializeField] private Color blockedColor = Color.red;
    [SerializeField] private Color mainPathColor = Color.cyan;
    [SerializeField] private Color platformCenterColor = Color.magenta;
    [SerializeField] private Color caSeedColor = Color.gray;
    [SerializeField] private Color caStartColor = Color.blue;

    #region Modular Components
    [Header("Modular Components (Auto-Initialized)")]
    [SerializeField] private LevelTerrainGenerator terrainGenerator;
    [SerializeField] private LevelCollectiblePlacer collectiblePlacer;
    [SerializeField] private LevelEffectManager effectManager;
    [SerializeField] private LevelObjectInstantiator objectInstantiator;
    #endregion

    #region Private Data
    // Core generation data
    private System.Random random;
    private int[,] levelGrid; // 0 = walkable, 1 = wall, 2 = collectible spawn, 3 = goal
    private Vector3 levelCenter;
    private Vector2Int playerSpawnPosition;
    private Vector2Int goalPosition;
    private LevelGenerationMode usedGenerationMode;
    private bool isGenerating = false;

    // Generation statistics
    private int generationAttempts = 0;
    private float lastGenerationTime = 0f;
    #endregion

    #region Events
    public System.Action<LevelProfile> OnLevelGenerationStarted;
    public System.Action<LevelProfile> OnLevelGenerationCompleted;
    public System.Action<string> OnGenerationError;
    #endregion

    #region Properties
    public LevelProfile ActiveProfile => activeProfile;
    public bool IsGenerating => isGenerating;
    public Vector3 LevelCenter => levelCenter;
    public Vector2Int PlayerSpawnPosition => playerSpawnPosition;
    public Vector2Int GoalPosition => goalPosition;
    
    // Modular Component Access
    public LevelTerrainGenerator TerrainGenerator => terrainGenerator;
    public LevelCollectiblePlacer CollectiblePlacer => collectiblePlacer;
    public LevelEffectManager EffectManager => effectManager;
    public LevelObjectInstantiator ObjectInstantiator => objectInstantiator;
    
    // Delegated Properties from Components
    public List<Vector2Int> WalkableTiles => terrainGenerator?.WalkableTiles ?? new List<Vector2Int>();
    public List<Vector2Int> MainPath => terrainGenerator?.MainPath ?? new List<Vector2Int>();
    public List<Vector2Int> PlatformCenters => terrainGenerator?.PlatformCenters ?? new List<Vector2Int>();
    public List<Vector2Int> CollectiblePositions => collectiblePlacer?.CollectiblePositions ?? new List<Vector2Int>();
    public List<Vector3> SteamEmitterPositions => effectManager?.SteamEmitterPositions ?? new List<Vector3>();
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        // MERGED: LOCAL scene detection logic preserved
        if (!SceneTypeDetector.IsProceduralScene())
        {
            if (showGenerationDebug)
            {
                Debug.Log($"[LevelGenerator] Deaktiviert in statischer Szene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'");
            }
            
            // Deaktiviere die Komponente in statischen Szenen
            this.enabled = false;
            return;
        }

        // Log Szenen-Info für Debugging
        SceneTypeDetector.LogSceneInfo();
        
        // Initialize modular components
        InitializeComponents();
        
        if (generateOnStart)
        {
            if (showGenerationDebug)
            {
                Debug.Log($"[LevelGenerator] Starte prozedurale Generierung in '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'");
            }
            GenerateLevel();
        }
    }

    void Update()
    {
        // Regeneration hotkey (nur im Debug-Modus)
        if (allowRegeneration && Input.GetKeyDown(regenerateKey) && !isGenerating)
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                RegenerateLevel();
            }
        }
    }
    #endregion

    #region Component Initialization
    /// <summary>
    /// Initialisiert die modularen Komponenten
    /// </summary>
    private void InitializeComponents()
    {
        // Erstelle Komponenten-Instanzen falls nicht vorhanden
        if (terrainGenerator == null)
            terrainGenerator = new LevelTerrainGenerator();
            
        if (collectiblePlacer == null)
            collectiblePlacer = new LevelCollectiblePlacer();
            
        if (effectManager == null)
            effectManager = new LevelEffectManager();

        // Subscribe to component events
        SetupComponentEvents();

        if (showGenerationDebug)
        {
            Debug.Log("[LevelGenerator] Modular components initialized");
        }
    }

    /// <summary>
    /// Richtet Event-Verbindungen zwischen Komponenten ein
    /// </summary>
    private void SetupComponentEvents()
    {
        // Terrain Generator Events
        if (terrainGenerator != null)
        {
            terrainGenerator.OnTerrainGenerationCompleted += OnTerrainCompleted;
            terrainGenerator.OnTerrainGenerationError += OnComponentError;
        }

        // Collectible Placer Events
        if (collectiblePlacer != null)
        {
            collectiblePlacer.OnCollectiblesPlaced += OnCollectiblesPlaced;
            collectiblePlacer.OnGoalZonePlaced += OnGoalZonePlaced;
            collectiblePlacer.OnPlacementError += OnComponentError;
        }

        // Effect Manager Events
        if (effectManager != null)
        {
            effectManager.OnEffectsApplied += OnEffectsApplied;
            effectManager.OnSteamEmittersPlaced += OnSteamEmittersPlaced;
            effectManager.OnEffectError += OnComponentError;
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Startet die Levelgenerierung mit dem aktuellen Profil
    /// </summary>
    public void GenerateLevel()
    {
        if (isGenerating)
        {
            Debug.LogWarning("Level generation already in progress!");
            return;
        }

        StartCoroutine(GenerateLevelCoroutine());
    }

    /// <summary>
    /// Startet die Levelgenerierung mit einem spezifischen Profil
    /// MERGED: Added difficulty modifier from REMOTE
    /// </summary>
    public void GenerateLevel(LevelProfile profile)
    {
        if (isGenerating)
        {
            Debug.LogWarning("Level generation already in progress!");
            return;
        }

        // MERGED: Difficulty scaling from REMOTE (with fallback)
        float modifier = 1f;
        if (GameManager.Instance != null)
        {
            // TODO: Verify GameManager.CalculateDifficultyModifier() exists
            try
            {
                var methodInfo = GameManager.Instance.GetType().GetMethod("CalculateDifficultyModifier");
                if (methodInfo != null)
                {
                    modifier = (float)methodInfo.Invoke(GameManager.Instance, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Difficulty modifier not available: {e.Message}");
            }
        }

        // TODO: Verify LevelProfile.CreateScaledProfile() exists
        try
        {
            var methodInfo = profile.GetType().GetMethod("CreateScaledProfile");
            if (methodInfo != null)
            {
                activeProfile = (LevelProfile)methodInfo.Invoke(profile, new object[] { modifier });
            }
            else
            {
                activeProfile = profile; // Fallback to original profile
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Scaled profile creation not available: {e.Message}");
            activeProfile = profile;
        }

        if (showGenerationDebug && modifier != 1f)
        {
            Debug.Log($"Difficulty modifier: {modifier:F2}");
            Debug.Log($"Scaled size: {activeProfile.LevelSize}, obstacles: {activeProfile.ObstacleDensity}, collectibles: {activeProfile.CollectibleCount}");
        }

        StartCoroutine(GenerateLevelCoroutine());
    }

    /// <summary>
    /// Regeneriert das Level mit neuen Zufallswerten
    /// </summary>
    public void RegenerateLevel()
    {
        if (!activeProfile)
        {
            Debug.LogError("No active profile set for regeneration!");
            return;
        }

        // FIX: Delay before regeneration to ensure containers clear completely
        StartCoroutine(DelayedRegenerate());
    }

    private IEnumerator DelayedRegenerate()
    {
        ClearLevel();
        // Short delay to allow Destroy operations to complete
        yield return null; // FIX: Delay before regeneration
        GenerateLevel();
    }

    /// <summary>
    /// Löscht das aktuelle Level vollständig
    /// MERGED: Extended clearing logic from REMOTE
    /// </summary>
    public void ClearLevel()
    {
        if (isGenerating)
        {
            StopAllCoroutines();
            isGenerating = false;
        }

        // Lösche alle generierten Objekte
        ClearContainer(groundContainer);
        ClearContainer(wallContainer);
        ClearContainer(collectibleContainer);
        ClearContainer(effectsContainer);

        // Reset core data
        levelGrid = null;
        usedGenerationMode = activeProfile ? activeProfile.GenerationMode : LevelGenerationMode.Simple;

        // Reset modular components
        terrainGenerator?.Reset();
        collectiblePlacer?.Reset();
        effectManager?.Reset();

        if (showGenerationDebug)
        {
            Debug.Log("[LevelGenerator] Level cleared and components reset");
        }
    }
    #endregion

    #region Level Generation Core
    /// <summary>
    /// Hauptroutine für die Levelgenerierung - REFACTORED
    /// Orchestriert die modularen Komponenten
    /// </summary>
    private IEnumerator GenerateLevelCoroutine()
    {
        isGenerating = true;
        float startTime = Time.realtimeSinceStartup;
        string errorMessage = "";

        // Validierung
        if (!ValidateSetup())
        {
            isGenerating = false;
            yield break;
        }

        // Event für Start der Generierung
        try
        {
            OnLevelGenerationStarted?.Invoke(activeProfile);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in OnLevelGenerationStarted event: {e.Message}");
        }

        // Generation steps (outside try-catch due to yield return)
        yield return InitializeGenerationCoroutine();
        yield return GenerateTerrainCoroutine();
        yield return PlaceCollectiblesCoroutine();
        yield return InstantiateLevelObjectsCoroutine();
        yield return ApplyMaterialsAndEffectsCoroutine();
        yield return PlaceInteractiveElementsCoroutine();

        // Post-processing (without yield)
        try
        {
            SetupPlayerSpawn();
            IntegrateWithGameSystems();

            // Erfolg
            lastGenerationTime = Time.realtimeSinceStartup - startTime;
            if (showGenerationDebug)
            {
                Debug.Log($"[LevelGenerator] Level generation completed in {lastGenerationTime:F2}s with {generationAttempts} attempts");
                LogGenerationSummary();
            }

            OnLevelGenerationCompleted?.Invoke(activeProfile);
        }
        catch (System.Exception e)
        {
            errorMessage = $"Level generation failed: {e.Message}";
            Debug.LogError(errorMessage);
            OnGenerationError?.Invoke(errorMessage);
        }

        isGenerating = false;
    }

    /// <summary>
    /// Validiert das Setup vor der Generierung
    /// </summary>
    private bool ValidateSetup()
    {
        if (!activeProfile)
        {
            Debug.LogError("No LevelProfile assigned!");
            return false;
        }

        if (!activeProfile.ValidateProfile())
        {
            Debug.LogError("Invalid LevelProfile configuration!");
            return false;
        }

        // Prefab validation
        if (!groundPrefab || !wallPrefab || !collectiblePrefab || !goalZonePrefab)
        {
            Debug.LogError("Missing prefab references!");
            return false;
        }

        // Component validation
        if (terrainGenerator == null || collectiblePlacer == null || effectManager == null)
        {
            Debug.LogError("Modular components not initialized!");
            return false;
        }

        return true;
    }
    #endregion

    #region Generation Steps
    /// <summary>
    /// Initialisierung für die Generierung
    /// </summary>
    private IEnumerator InitializeGenerationCoroutine()
    {
        generationAttempts = 0;

        // Initialize random with seed
        int seed = activeProfile.GetActualSeed();
        random = new System.Random(seed);

        // MERGED: Advanced mode selection from REMOTE (with fallback)
        try
        {
            var methodInfo = activeProfile.GetType().GetMethod("GetAdaptiveGenerationMode");
            if (methodInfo != null)
            {
                usedGenerationMode = (LevelGenerationMode)methodInfo.Invoke(activeProfile, new object[] { seed });
            }
            else
            {
                usedGenerationMode = activeProfile.GenerationMode;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Adaptive generation mode not available: {e.Message}");
            usedGenerationMode = activeProfile.GenerationMode;
        }

        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Generating level with seed: {seed} using mode {usedGenerationMode}");
        }

        // Setup containers
        SetupContainers();

        // Initialize level grid
        int size = activeProfile.LevelSize;
        levelGrid = new int[size, size];

        // Calculate level center
        levelCenter = new Vector3(
            (size - 1) * activeProfile.TileSize * 0.5f,
            0,
            (size - 1) * activeProfile.TileSize * 0.5f
        );

        yield return null;
    }

    /// <summary>
    /// Terrain-Generierung - delegiert an LevelTerrainGenerator
    /// </summary>
    private IEnumerator GenerateTerrainCoroutine()
    {
        // Initialize terrain generator
        terrainGenerator.Initialize(activeProfile, levelGrid, random, showGenerationDebug);

        // Generate terrain
        yield return terrainGenerator.GenerateTerrainCoroutine();

        // Update local references
        levelGrid = terrainGenerator.LevelGrid;

        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Terrain generation completed. Walkable tiles: {terrainGenerator.WalkableTiles.Count}");
        }
    }

    /// <summary>
    /// Collectible-Platzierung - delegiert an LevelCollectiblePlacer
    /// </summary>
    private IEnumerator PlaceCollectiblesCoroutine()
    {
        // Initialize collectible placer
        collectiblePlacer.Initialize(activeProfile, levelGrid, terrainGenerator.WalkableTiles, 
                                   terrainGenerator.MainPath, random, showGenerationDebug);

        // Place collectibles
        yield return collectiblePlacer.PlaceAllCollectibles();

        // Place goal zone
        yield return collectiblePlacer.PlaceGoalZoneCoroutine();

        // Apply to grid
        collectiblePlacer.ApplyToLevelGrid();

        // Update local references
        goalPosition = collectiblePlacer.GoalPosition;

        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Collectible placement completed. " +
                     $"Collectibles: {collectiblePlacer.PlacedCount}, Goal: {goalPosition}");
        }
    }

    /// <summary>
    /// Level-Objekte instantiieren
    /// </summary>
    private IEnumerator InstantiateLevelObjectsCoroutine()
    {
        int size = activeProfile.LevelSize;
        float tileSize = activeProfile.TileSize;
        int objectsCreated = 0;

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                Vector3 worldPos = new Vector3(x * tileSize, 0, z * tileSize);

                switch (levelGrid[x, z])
                {
                    case 0: // Walkable ground
                        CreateGroundTile(worldPos, x, z);
                        break;

                    case 1: // Wall
                        CreateWallTile(worldPos, x, z);
                        break;

                    case 2: // Collectible
                        CreateGroundTile(worldPos, x, z);
                        Vector3 collectiblePos = worldPos + Vector3.up * activeProfile.CollectibleSpawnHeight;
                        collectiblePlacer.CreateCollectibleObject(collectiblePos, collectiblePrefab, collectibleContainer);
                        break;

                    case 3: // Goal zone
                        // Create ground first, then goal zone slightly above
                        CreateGroundTile(worldPos, x, z);
                        Vector3 goalPos = worldPos + Vector3.up * 0.05f;
                        collectiblePlacer.CreateGoalZoneObject(goalPos, goalZonePrefab, levelContainer, activeProfile.GoalZoneMaterial);
                        break;
                }

                objectsCreated++;

                // Yield every 25 objects to prevent frame drops
                if (objectsCreated % 25 == 0)
                    yield return null;
            }
        }

        // MERGED: Moving platforms and obstacles (if available)
        yield return InstantiateAdvancedElementsCoroutine();

        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Instantiated {objectsCreated} level objects");
        }
    }

    /// <summary>
    /// Materialien und Effekte anwenden - delegiert an LevelEffectManager
    /// </summary>
    private IEnumerator ApplyMaterialsAndEffectsCoroutine()
    {
        // Initialize effect manager
        effectManager.Initialize(activeProfile, terrainGenerator.WalkableTiles, terrainGenerator.PlatformCenters,
                               usedGenerationMode, effectsContainer, random, showGenerationDebug);

        // Apply all materials and effects
        yield return effectManager.ApplyAllMaterialsAndEffects();

        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Materials and effects applied. " +
                     $"Effects: {effectManager.ActiveEffectsCount}, Steam: {effectManager.ActiveSteamEmittersCount}");
        }
    }

    /// <summary>
    /// Interactive Elements (Tore, Schalter, etc.)
    /// </summary>
    private IEnumerator PlaceInteractiveElementsCoroutine()
    {
        // TODO: Implement interactive gates and switches when LevelProfile supports it
        try
        {
            var enableInteractiveGates = activeProfile.GetType().GetProperty("EnableInteractiveGates");
            if (enableInteractiveGates != null && (bool)enableInteractiveGates.GetValue(activeProfile))
            {
                if (showGenerationDebug)
                {
                    Debug.Log("[LevelGenerator] Interactive gates feature detected but not fully implemented yet");
                }
                // TODO: Implement interactive gates placement
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LevelGenerator] Interactive elements check failed: {e.Message}");
        }

        yield return null;
    }

    /// <summary>
    /// Erweiterte Elemente (Moving Platforms, Rotating Obstacles)
    /// </summary>
    private IEnumerator InstantiateAdvancedElementsCoroutine()
    {
        float tileSize = activeProfile.TileSize;

        // MERGED: Moving platforms from REMOTE
        foreach (Vector2Int tile in terrainGenerator.MovingPlatformTiles)
        {
            try
            {
                var movingPlatformPrefabs = activeProfile.GetType().GetProperty("MovingPlatformPrefabs");
                if (movingPlatformPrefabs != null)
                {
                    var prefabs = movingPlatformPrefabs.GetValue(activeProfile) as GameObject[];
                    if (prefabs != null && prefabs.Length > 0)
                    {
                        GameObject prefab = prefabs[random.Next(prefabs.Length)];
                        if (prefab)
                        {
                            Vector3 pos = new Vector3(tile.x * tileSize, activeProfile.CollectibleSpawnHeight, tile.y * tileSize);
                            Instantiate(prefab, pos, Quaternion.identity, levelContainer);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelGenerator] Moving platform placement failed: {e.Message}");
            }
        }

        // MERGED: Rotating obstacles from REMOTE
        foreach (Vector2Int center in terrainGenerator.RotatingObstaclePlatforms)
        {
            try
            {
                var rotatingObstaclePrefabs = activeProfile.GetType().GetProperty("RotatingObstaclePrefabs");
                if (rotatingObstaclePrefabs != null)
                {
                    var prefabs = rotatingObstaclePrefabs.GetValue(activeProfile) as GameObject[];
                    if (prefabs != null && prefabs.Length > 0)
                    {
                        GameObject prefab = prefabs[random.Next(prefabs.Length)];
                        if (prefab)
                        {
                            Vector3 pos = new Vector3(center.x * tileSize, activeProfile.CollectibleSpawnHeight, center.y * tileSize);
                            Instantiate(prefab, pos, Quaternion.identity, levelContainer);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelGenerator] Rotating obstacle placement failed: {e.Message}");
            }
        }

        yield return null;
    }
    #endregion

    #region Object Creation
    /// <summary>
    /// Erstellt Ground Tile mit Material-Anwendung
    /// </summary>
    private void CreateGroundTile(Vector3 position, int gx = 0, int gz = 0)
    {
        GameObject ground = CreateOrPoolObject(groundPrefab, position, Quaternion.identity, groundContainer);
        
        // Apply material via effect manager
        effectManager.ApplyGroundMaterial(ground, gx, gz);

        // Apply slippery physics if enabled
        effectManager.ApplySlipperyPhysics(ground);
    }

    /// <summary>
    /// Erstellt Wall Tile mit Material-Anwendung
    /// </summary>
    private void CreateWallTile(Vector3 position, int gx = 0, int gz = 0)
    {
        GameObject wall = CreateOrPoolObject(wallPrefab, position, Quaternion.identity, wallContainer);
        
        // Apply material via effect manager
        effectManager.ApplyWallMaterial(wall, gx, gz);
    }

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
            Debug.LogWarning($"[LevelGenerator] Object pooling not available: {e.Message}");
        }

        // Fallback to instantiate
        if (obj == null)
        {
            obj = Instantiate(prefab, position, rotation, parent);
        }

        return obj;
    }
    #endregion

    #region Container Management
    /// <summary>
    /// Richtet Container-Objekte ein
    /// </summary>
    private void SetupContainers()
    {
        if (!levelContainer)
        {
            GameObject levelContainerGO = new GameObject("Generated Level");
            levelContainer = levelContainerGO.transform;
        }

        groundContainer = GetOrCreateContainer("Ground", levelContainer);
        wallContainer = GetOrCreateContainer("Walls", levelContainer);
        collectibleContainer = GetOrCreateContainer("Collectibles", levelContainer);
        effectsContainer = GetOrCreateContainer("Effects", levelContainer);
    }

    /// <summary>
    /// Holt oder erstellt einen Container
    /// </summary>
    private Transform GetOrCreateContainer(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing)
        {
            ClearContainer(existing);
            return existing;
        }

        GameObject container = new GameObject(name);
        container.transform.SetParent(parent);
        return container.transform;
    }

    /// <summary>
    /// MERGED: Object pooling with fallback to destroy
    /// </summary>
    private void ClearContainer(Transform container)
    {
        if (!container) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i).gameObject;
            
            // MERGED: Object pooling with fallback
            bool pooled = false;
            try
            {
                // Check if PrefabPooler exists and try to use it
                var poolerType = System.Type.GetType("PrefabPooler");
                if (poolerType != null)
                {
                    var releaseMethod = poolerType.GetMethod("Release");
                    if (releaseMethod != null)
                    {
                        releaseMethod.Invoke(null, new object[] { child });
                        pooled = true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelGenerator] Object pooling not available: {e.Message}");
            }

            // Fallback to destroy if pooling failed
            if (!pooled)
            {
                if (Application.isEditor)
                    DestroyImmediate(child); // FIX: Ensure immediate cleanup in Editor
                else
                    Destroy(child);
            }
        }
    }
    #endregion

    #region Post-Generation Setup
    /// <summary>
    /// Player-Spawn-Position einrichten
    /// </summary>
    private void SetupPlayerSpawn()
    {
        // Find a safe spawn position
        if (activeProfile.RandomizeSpawnPosition)
        {
            // Find a position with enough safe radius
            foreach (Vector2Int pos in WalkableTiles)
            {
                if (IsSafeSpawnPosition(pos))
                {
                    playerSpawnPosition = pos;
                    break;
                }
            }
        }
        else
        {
            // Use center or first walkable position
            playerSpawnPosition = WalkableTiles.Count > 0 ? WalkableTiles[0] : Vector2Int.zero;
        }

        // Position player if prefab is assigned
        if (playerPrefab)
        {
            Vector3 spawnWorldPos = new Vector3(
                playerSpawnPosition.x * activeProfile.TileSize,
                activeProfile.PlayerSpawnOffset.y,
                playerSpawnPosition.y * activeProfile.TileSize
            ) + activeProfile.PlayerSpawnOffset;

            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer)
            {
                existingPlayer.transform.position = spawnWorldPos;
                
                // Reset player velocity if it has a Rigidbody
                Rigidbody rb = existingPlayer.GetComponent<Rigidbody>();
                PhysicsUtils.ResetMotion(rb);
            }
            else
            {
                Instantiate(playerPrefab, spawnWorldPos, Quaternion.identity);
            }
        }

        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Player spawn set to {playerSpawnPosition} (world: {playerSpawnPosition.x * activeProfile.TileSize}, {playerSpawnPosition.y * activeProfile.TileSize})");
        }
    }

    /// <summary>
    /// Prüft ob eine Position sicher für Player-Spawn ist
    /// </summary>
    private bool IsSafeSpawnPosition(Vector2Int pos)
    {
        float safeRadius = activeProfile.SpawnSafeRadius / activeProfile.TileSize;
        
        foreach (Vector2Int collectiblePos in CollectiblePositions)
        {
            if (Vector2Int.Distance(pos, collectiblePos) < safeRadius)
                return false;
        }

        if (Vector2Int.Distance(pos, goalPosition) < safeRadius)
            return false;

        return true;
    }

    /// <summary>
    /// Integration mit Game-Systemen
    /// </summary>
    private void IntegrateWithGameSystems()
    {
        // Update LevelManager if present
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager)
        {
            // Configure level for the active profile
            if (levelManager.Config != null)
            {
                levelManager.Config.levelName = activeProfile.DisplayName;
                levelManager.Config.totalCollectibles = activeProfile.CollectibleCount;
                levelManager.Config.collectiblesRemaining = activeProfile.CollectibleCount;
                levelManager.Config.difficultyMultiplier = activeProfile.DifficultyLevel;
                levelManager.Config.themeColor = activeProfile.ThemeColor;
            }

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelGenerator] Updated LevelManager configuration");
            }
        }

        // Update UI if necessary
        UIController uiController = FindFirstObjectByType<UIController>();
        if (uiController)
        {
            // Could trigger UI refresh here
        }
    }
    #endregion

    #region Component Event Handlers
    /// <summary>
    /// Event-Handler für Terrain-Generierung abgeschlossen
    /// </summary>
    private void OnTerrainCompleted(Vector2Int playerSpawn, Vector2Int goalPos)
    {
        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Terrain generation completed. Player: {playerSpawn}, Goal: {goalPos}");
        }
    }

    /// <summary>
    /// Event-Handler für Collectibles platziert
    /// </summary>
    private void OnCollectiblesPlaced(int count)
    {
        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Collectibles placed: {count}");
        }
    }

    /// <summary>
    /// Event-Handler für Goal Zone platziert
    /// </summary>
    private void OnGoalZonePlaced(Vector2Int position)
    {
        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Goal zone placed at: {position}");
        }
    }

    /// <summary>
    /// Event-Handler für Effekte angewandt
    /// </summary>
    private void OnEffectsApplied(int effectCount)
    {
        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Effects applied: {effectCount}");
        }
    }

    /// <summary>
    /// Event-Handler für Steam Emitters platziert
    /// </summary>
    private void OnSteamEmittersPlaced(int count)
    {
        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] Steam emitters placed: {count}");
        }
    }

    /// <summary>
    /// Event-Handler für Komponenten-Fehler
    /// </summary>
    private void OnComponentError(string error)
    {
        Debug.LogWarning($"[LevelGenerator] Component error: {error}");
        OnGenerationError?.Invoke($"Component error: {error}");
    }
    #endregion

    #region Debug & Logging
    /// <summary>
    /// Loggt eine Zusammenfassung der Generierung
    /// </summary>
    private void LogGenerationSummary()
    {
        Debug.Log($"=== Level Generation Summary ===");
        Debug.Log($"Profile: {activeProfile.DisplayName} ({usedGenerationMode})");
        Debug.Log($"Size: {activeProfile.LevelSize}x{activeProfile.LevelSize}");
        Debug.Log($"Walkable Tiles: {WalkableTiles.Count}");
        Debug.Log($"Main Path: {MainPath.Count} tiles");
        Debug.Log($"Platform Centers: {PlatformCenters.Count}");
        Debug.Log($"Collectibles: {CollectiblePositions.Count}/{activeProfile.CollectibleCount}");
        Debug.Log($"Goal Position: {goalPosition}");
        Debug.Log($"Player Spawn: {playerSpawnPosition}");
        Debug.Log($"Steam Emitters: {SteamEmitterPositions.Count}");
        Debug.Log($"Effects: {effectManager?.ActiveEffectsCount ?? 0}");
        Debug.Log($"Generation Time: {lastGenerationTime:F2}s");
        Debug.Log($"Attempts: {generationAttempts}");

        // Component-specific debug info
        if (showGenerationDebug && terrainGenerator != null)
        {
            terrainGenerator.LogTerrainInfo();
        }
        if (showGenerationDebug && collectiblePlacer != null)
        {
            collectiblePlacer.LogPlacementInfo();
        }
        if (showGenerationDebug && effectManager != null)
        {
            effectManager.LogEffectInfo();
        }
    }
    #endregion

    #region Debug Visualization
    /// <summary>
    /// MERGED: Enhanced visualization with modular component data
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if ((!showWalkableArea && !showGenerationDebug) || levelGrid == null) return;

        int size = activeProfile ? activeProfile.LevelSize : 10;
        float tileSize = activeProfile ? activeProfile.TileSize : 2f;

        // Draw level grid
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                Vector3 worldPos = new Vector3(x * tileSize, 0, z * tileSize);
                
                switch (levelGrid[x, z])
                {
                    case 0: // Walkable
                        Gizmos.color = walkableColor;
                        Gizmos.DrawWireCube(worldPos, Vector3.one * tileSize * 0.9f);
                        break;
                    case 1: // Wall
                        Gizmos.color = blockedColor;
                        Gizmos.DrawCube(worldPos, Vector3.one * tileSize * 0.9f);
                        break;
                    case 2: // Collectible
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(worldPos + Vector3.up * 0.5f, 0.3f);
                        break;
                    case 3: // Goal
                        Gizmos.color = Color.green;
                        Gizmos.DrawCube(worldPos, Vector3.one * tileSize * 0.8f);
                        break;
                }
            }
        }

        // Draw spawn position
        if (playerSpawnPosition != Vector2Int.zero)
        {
            Vector3 spawnPos = new Vector3(
                playerSpawnPosition.x * tileSize,
                1f,
                playerSpawnPosition.y * tileSize
            );
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(spawnPos, 0.5f);
        }

        if (showGenerationDebug)
        {
            // Delegate additional visualization to components
            terrainGenerator?.DrawTerrainGizmos(tileSize, mainPathColor, platformCenterColor);
            effectManager?.DrawSteamEmitterGizmos(tileSize);
        }
    }
    #endregion

    #region Component Initialization
    /// <summary>
    /// Initialize all modular components
    /// </summary>
    private void InitializeComponents()
    {
        System.Random rng = new System.Random(activeProfile.UseTimeBasedSeed ? 
            (int)System.DateTime.Now.Ticks : activeProfile.GenerationSeed);
        
        // Initialize terrain generator
        if (terrainGenerator == null)
            terrainGenerator = new LevelTerrainGenerator();
        terrainGenerator.InitializeTerrain(activeProfile, rng);
        
        // Initialize collectible placer
        if (collectiblePlacer == null)
            collectiblePlacer = new LevelCollectiblePlacer();
        // collectiblePlacer.Initialize(...); // Would need proper setup
        
        // Initialize effect manager
        if (effectManager == null)
            effectManager = new LevelEffectManager();
        // effectManager.Initialize(...); // Would need proper setup
        
        // Initialize object instantiator
        if (objectInstantiator == null)
            objectInstantiator = new LevelObjectInstantiator();
        
        objectInstantiator.Initialize(
            activeProfile, terrainGenerator, 
            groundPrefab, wallPrefab, collectiblePrefab, goalZonePrefab,
            levelContainer, groundContainer, wallContainer, collectibleContainer, effectsContainer,
            rng, showGenerationDebug
        );
        
        if (showGenerationDebug)
        {
            Debug.Log($"[LevelGenerator] All components initialized for {activeProfile.DisplayName}");
        }
    }
    
    /// <summary>
    /// Subscribe to component events
    /// </summary>
    private void SubscribeToComponentEvents()
    {
        // Terrain events
        if (terrainGenerator != null)
        {
            terrainGenerator.OnTerrainGenerationCompleted += OnTerrainCompleted;
            terrainGenerator.OnTerrainGenerationError += OnComponentError;
        }
        
        // Object instantiator events
        if (objectInstantiator != null)
        {
            objectInstantiator.OnObjectsInstantiated += (count) => {
                if (showGenerationDebug)
                    Debug.Log($"[LevelGenerator] Objects instantiated: {count}");
            };
            objectInstantiator.OnDynamicElementsCreated += (count) => {
                if (showGenerationDebug)
                    Debug.Log($"[LevelGenerator] Dynamic elements created: {count}");
            };
            objectInstantiator.OnInstantiationError += OnComponentError;
        }
        
        // Effect manager events
        if (effectManager != null)
        {
            effectManager.OnEffectsApplied += OnEffectsApplied;
            effectManager.OnSteamEmittersPlaced += OnSteamEmittersPlaced;
            effectManager.OnEffectError += OnComponentError;
        }
        
        // Collectible placer events (when available)
        // collectiblePlacer events...
    }
    #endregion
}
