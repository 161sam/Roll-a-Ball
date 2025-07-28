using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Hauptklasse für die prozedurale Levelgenerierung
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

    // Private generation data
    private System.Random random;
    private int[,] levelGrid; // 0 = walkable, 1 = wall, 2 = collectible spawn, 3 = goal
    private Vector3 levelCenter;
    private List<Vector2Int> walkableTiles;
    private List<Vector2Int> collectiblePositions;
    private Vector2Int playerSpawnPosition;
    private Vector2Int goalPosition;
    private bool isGenerating = false;

    // Generation statistics
    private int generationAttempts = 0;
    private float lastGenerationTime = 0f;

    // Events
    public System.Action<LevelProfile> OnLevelGenerationStarted;
    public System.Action<LevelProfile> OnLevelGenerationCompleted;
    public System.Action<string> OnGenerationError;

    // Properties
    public LevelProfile ActiveProfile => activeProfile;
    public bool IsGenerating => isGenerating;
    public Vector3 LevelCenter => levelCenter;
    public Vector2Int PlayerSpawnPosition => playerSpawnPosition;
    public Vector2Int GoalPosition => goalPosition;

    void Start()
    {
        if (generateOnStart)
        {
            StartCoroutine(GenerateLevelCoroutine());
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
    /// </summary>
    public void GenerateLevel(LevelProfile profile)
    {
        if (isGenerating)
        {
            Debug.LogWarning("Level generation already in progress!");
            return;
        }

        activeProfile = profile;
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

        // Reset level data
        levelGrid = null;
        walkableTiles?.Clear();
        collectiblePositions?.Clear();
    }

    #endregion

    #region Level Generation Core

    /// <summary>
    /// Hauptroutine für die Levelgenerierung
    /// </summary>
    private IEnumerator GenerateLevelCoroutine()
    {
        isGenerating = true;
        float startTime = Time.realtimeSinceStartup;
        bool hasError = false;
        string errorMessage = "";

        // Validierung ohne try-catch (da yield break verwendet wird)
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

        // Initialisierung
        try
        {
            InitializeGeneration();
        }
        catch (System.Exception e)
        {
            hasError = true;
            errorMessage = $"Initialization failed: {e.Message}";
        }

        // Generierungsschritte (außerhalb try-catch wegen yield return)
        if (!hasError)
        {
            yield return StartCoroutine(GenerateBaseStructure());
            yield return StartCoroutine(GenerateWalkablePaths());
            yield return StartCoroutine(PlaceCollectibles());
            yield return StartCoroutine(PlaceGoalZone());
            yield return StartCoroutine(InstantiateLevelObjects());
            yield return StartCoroutine(ApplyMaterialsAndEffects());
        }

        // Post-processing ohne yield
        if (!hasError)
        {
            try
            {
                SetupPlayerSpawn();
                IntegrateWithGameSystems();
                
                lastGenerationTime = Time.realtimeSinceStartup - startTime;
                if (showGenerationDebug)
                {
                    Debug.Log($"Level generation completed in {lastGenerationTime:F2}s with {generationAttempts} attempts");
                }
                
                OnLevelGenerationCompleted?.Invoke(activeProfile);
            }
            catch (System.Exception e)
            {
                hasError = true;
                errorMessage = $"Post-processing failed: {e.Message}";
            }
        }

        // Fehlerbehandlung
        if (hasError)
        {
            Debug.LogError($"Level generation failed: {errorMessage}");
            OnGenerationError?.Invoke(errorMessage);
        }

        isGenerating = false;
    }



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

        return true;
    }

    private void InitializeGeneration()
    {
        generationAttempts = 0;

        // Initialize random with seed
        int seed = activeProfile.GetActualSeed();
        random = new System.Random(seed);

        if (showGenerationDebug)
        {
            Debug.Log($"Generating level with seed: {seed}");
        }

        // Setup containers
        SetupContainers();

        // Initialize level grid
        int size = activeProfile.LevelSize;
        levelGrid = new int[size, size];
        walkableTiles = new List<Vector2Int>();
        collectiblePositions = new List<Vector2Int>();

        // Calculate level center
        levelCenter = new Vector3(
            (size - 1) * activeProfile.TileSize * 0.5f,
            0,
            (size - 1) * activeProfile.TileSize * 0.5f
        );
    }

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

    private void ClearContainer(Transform container)
    {
        if (!container) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i).gameObject;
            if (Application.isEditor)
                DestroyImmediate(child); // FIX: Ensure immediate cleanup in Editor
            else
                Destroy(child);
        }
    }

    #endregion

    #region Generation Steps

    private IEnumerator GenerateBaseStructure()
    {
        int size = activeProfile.LevelSize;

        // Initialize with walls around perimeter
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                if (x == 0 || x == size - 1 || z == 0 || z == size - 1)
                {
                    levelGrid[x, z] = 1; // Wall
                }
                else
                {
                    levelGrid[x, z] = 0; // Initially walkable
                }
            }
        }

        yield return null;
    }

    private IEnumerator GenerateWalkablePaths()
    {
        int size = activeProfile.LevelSize;
        float obstacleDensity = activeProfile.ObstacleDensity;

        switch (activeProfile.GenerationMode)
        {
            case LevelGenerationMode.Simple:
                yield return StartCoroutine(GenerateSimpleLayout(obstacleDensity));
                break;
            case LevelGenerationMode.Maze:
                yield return StartCoroutine(GenerateMazeLayout());
                break;
            case LevelGenerationMode.Platforms:
                yield return StartCoroutine(GeneratePlatformLayout());
                break;
            default:
                yield return StartCoroutine(GenerateSimpleLayout(obstacleDensity));
                break;
        }

        // Sammle begehbare Bereiche
        CollectWalkableTiles();

        // Validiere Mindestbegehbarkeit
        float walkablePercentage = (walkableTiles.Count / (float)(size * size)) * 100f;
        if (walkablePercentage < activeProfile.MinWalkableArea)
        {
            if (showGenerationDebug)
            {
                Debug.LogWarning($"Walkable area too small ({walkablePercentage:F1}%), regenerating...");
            }
            
            generationAttempts++;
            if (generationAttempts < 10)
            {
                yield return StartCoroutine(GenerateWalkablePaths()); // Retry
            }
        }
    }

    private IEnumerator GenerateSimpleLayout(float obstacleDensity)
    {
        int size = activeProfile.LevelSize;

        // Place random obstacles
        for (int x = 1; x < size - 1; x++)
        {
            for (int z = 1; z < size - 1; z++)
            {
                if (random.NextDouble() < obstacleDensity)
                {
                    levelGrid[x, z] = 1; // Wall
                }
            }
        }

        yield return null;
    }

    private IEnumerator GenerateMazeLayout()
    {
        int size = activeProfile.LevelSize;

        // Starte mit allen Wänden (außer Rand)
        for (int x = 1; x < size - 1; x++)
        {
            for (int z = 1; z < size - 1; z++)
            {
                levelGrid[x, z] = 1; // Wall
            }
        }

        // Recursive backtracking maze generation
        Vector2Int start = new Vector2Int(1, 1);
        levelGrid[start.x, start.y] = 0; // Make starting position walkable

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(start);

        int iterations = 0;
        const int maxIterations = 10000;

        while (stack.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Vector2Int current = stack.Peek();
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current, size);

            if (neighbors.Count > 0)
            {
                Vector2Int chosen = neighbors[random.Next(neighbors.Count)];
                
                // Remove wall between current and chosen
                Vector2Int between = current + ((chosen - current) / 2);
                levelGrid[between.x, between.y] = 0; // Walkable
                levelGrid[chosen.x, chosen.y] = 0;   // Walkable

                stack.Push(chosen);
            }
            else
            {
                stack.Pop();
            }

            // Yield periodically to avoid frame drops
            if (iterations % 100 == 0)
                yield return null;
        }

        // Apply path complexity - remove some walls to create multiple paths
        float complexityFactor = activeProfile.PathComplexity;
        int wallsToRemove = Mathf.RoundToInt(size * size * complexityFactor * 0.1f);

        for (int i = 0; i < wallsToRemove; i++)
        {
            int x = random.Next(1, size - 1);
            int z = random.Next(1, size - 1);
            if (levelGrid[x, z] == 1)
            {
                levelGrid[x, z] = 0; // Make walkable
            }
        }

        yield return null;
    }

    private IEnumerator GeneratePlatformLayout()
    {
        int size = activeProfile.LevelSize;

        // Create platform islands
        int platformCount = Mathf.RoundToInt(size * 0.3f);
        
        for (int i = 0; i < platformCount; i++)
        {
            int centerX = random.Next(2, size - 2);
            int centerZ = random.Next(2, size - 2);
            int platformSize = random.Next(2, 4);

            // Create platform
            for (int dx = -platformSize; dx <= platformSize; dx++)
            {
                for (int dz = -platformSize; dz <= platformSize; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;

                    if (x >= 1 && x < size - 1 && z >= 1 && z < size - 1)
                    {
                        if (Vector2.Distance(Vector2.zero, new Vector2(dx, dz)) <= platformSize)
                        {
                            levelGrid[x, z] = 0; // Walkable
                        }
                    }
                }
            }

            yield return null;
        }
    }

    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int pos, int size)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up * 2, Vector2Int.down * 2, Vector2Int.left * 2, Vector2Int.right * 2 };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = pos + dir;
            if (neighbor.x >= 1 && neighbor.x < size - 1 && neighbor.y >= 1 && neighbor.y < size - 1)
            {
                if (levelGrid[neighbor.x, neighbor.y] == 1) // Still a wall (unvisited)
                {
                    neighbors.Add(neighbor);
                }
            }
        }

        return neighbors;
    }

    private void CollectWalkableTiles()
    {
        walkableTiles.Clear();
        int size = activeProfile.LevelSize;

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                if (levelGrid[x, z] == 0)
                {
                    walkableTiles.Add(new Vector2Int(x, z));
                }
            }
        }
    }

    private IEnumerator PlaceCollectibles()
    {
        collectiblePositions.Clear();
        int targetCount = activeProfile.CollectibleCount;
        int minDistance = activeProfile.MinCollectibleDistance;

        List<Vector2Int> availablePositions = new List<Vector2Int>(walkableTiles);
        int attempts = 0;
        const int maxAttempts = 1000;

        while (collectiblePositions.Count < targetCount && attempts < maxAttempts && availablePositions.Count > 0)
        {
            attempts++;
            int index = random.Next(availablePositions.Count);
            Vector2Int candidate = availablePositions[index];

            // Check distance to existing collectibles
            bool validPosition = true;
            foreach (Vector2Int existing in collectiblePositions)
            {
                if (Vector2Int.Distance(candidate, existing) < minDistance)
                {
                    validPosition = false;
                    break;
                }
            }

            if (validPosition)
            {
                collectiblePositions.Add(candidate);
                levelGrid[candidate.x, candidate.y] = 2; // Mark as collectible position
            }

            availablePositions.RemoveAt(index);

            // Yield periodically
            if (attempts % 50 == 0)
                yield return null;
        }

        if (showGenerationDebug)
        {
            Debug.Log($"Placed {collectiblePositions.Count}/{targetCount} collectibles in {attempts} attempts");
        }
    }

    private IEnumerator PlaceGoalZone()
    {
        // Find the position furthest from the center or spawn point
        Vector2Int centerPos = new Vector2Int(activeProfile.LevelSize / 2, activeProfile.LevelSize / 2);
        
        float maxDistance = 0f;
        Vector2Int bestPosition = centerPos;

        foreach (Vector2Int pos in walkableTiles)
        {
            // Skip positions with collectibles
            if (levelGrid[pos.x, pos.y] == 2) continue;

            float distance = Vector2Int.Distance(pos, centerPos);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                bestPosition = pos;
            }
        }

        goalPosition = bestPosition;
        levelGrid[goalPosition.x, goalPosition.y] = 3; // Mark as goal position

        yield return null;
    }

    private IEnumerator InstantiateLevelObjects()
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
                        CreateGroundTile(worldPos);
                        break;

                    case 1: // Wall
                        CreateWallTile(worldPos);
                        break;

                    case 2: // Collectible
                        CreateGroundTile(worldPos);
                        CreateCollectible(worldPos + Vector3.up * activeProfile.CollectibleSpawnHeight);
                        break;

                    case 3: // Goal zone
                        // FIX: Avoid ground flackering on goal tile
                        // Only spawn the goal zone slightly above the ground
                        CreateGoalZone(worldPos + Vector3.up * 0.05f);
                        break;
                }

                objectsCreated++;

                // Yield every 25 objects to prevent frame drops
                if (objectsCreated % 25 == 0)
                    yield return null;
            }
        }

        if (showGenerationDebug)
        {
            Debug.Log($"Instantiated {objectsCreated} level objects");
        }
    }

    #endregion

    #region Object Creation
    // TODO: prepare object pooling for level prefabs (ground, walls, collectibles, goal zone)

    private void CreateGroundTile(Vector3 position)
    {
        GameObject ground = Instantiate(groundPrefab, position, Quaternion.identity, groundContainer);
        
        // Apply materials if available
        if (activeProfile.GroundMaterials != null && activeProfile.GroundMaterials.Length > 0)
        {
            Material material = activeProfile.GroundMaterials[random.Next(activeProfile.GroundMaterials.Length)];
            if (material)
            {
                Renderer renderer = ground.GetComponent<Renderer>();
                if (renderer)
                    renderer.material = material;
            }
        }

        // Apply slippery physics if enabled
        if (activeProfile.EnableSlipperyTiles && random.NextDouble() < activeProfile.SlipperyTileChance)
        {
            Collider collider = ground.GetComponent<Collider>();
            if (collider && activeProfile.SlipperyMaterial)
            {
                collider.material = activeProfile.SlipperyMaterial;
            }
        }
    }

    private void CreateWallTile(Vector3 position)
    {
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, wallContainer);
        
        // Apply materials if available
        if (activeProfile.WallMaterials != null && activeProfile.WallMaterials.Length > 0)
        {
            Material material = activeProfile.WallMaterials[random.Next(activeProfile.WallMaterials.Length)];
            if (material)
            {
                Renderer renderer = wall.GetComponent<Renderer>();
                if (renderer)
                    renderer.material = material;
            }
        }
    }

    private void CreateCollectible(Vector3 position)
    {
        GameObject collectible = Instantiate(collectiblePrefab, position, Quaternion.identity, collectibleContainer);
        
        // Configure collectible if it has a CollectibleController
        CollectibleController controller = collectible.GetComponent<CollectibleController>();
        if (controller)
        {
            // Could customize collectible data here based on profile
        }
    }

    private void CreateGoalZone(Vector3 position)
    {
        // NOTE: goalZonePrefab should include a slightly raised base to avoid Z-fighting
        GameObject goalZone = Instantiate(goalZonePrefab, position, Quaternion.identity, levelContainer);
        
        // Apply goal zone material if available
        if (activeProfile.GoalZoneMaterial)
        {
            Renderer renderer = goalZone.GetComponent<Renderer>();
            if (renderer)
                renderer.material = activeProfile.GoalZoneMaterial;
        }
    }

    #endregion

    #region Post-Generation Setup

    private IEnumerator ApplyMaterialsAndEffects()
    {
        // Apply particle effects if enabled
        if (activeProfile.EnableParticleEffects && activeProfile.DecorativeEffects != null)
        {
            int effectCount = Mathf.RoundToInt(walkableTiles.Count * 0.05f); // 5% of walkable tiles
            
            for (int i = 0; i < effectCount && i < activeProfile.DecorativeEffects.Length; i++)
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
                        Instantiate(effect, worldPos, Quaternion.identity, effectsContainer);
                    }
                }
            }
        }

        yield return null;
    }

    private void SetupPlayerSpawn()
    {
        // Find a safe spawn position
        if (activeProfile.RandomizeSpawnPosition)
        {
            // Find a position with enough safe radius
            foreach (Vector2Int pos in walkableTiles)
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
            playerSpawnPosition = walkableTiles.Count > 0 ? walkableTiles[0] : Vector2Int.zero;
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
                if (rb)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                Instantiate(playerPrefab, spawnWorldPos, Quaternion.identity);
            }
        }
    }

    private bool IsSafeSpawnPosition(Vector2Int pos)
    {
        float safeRadius = activeProfile.SpawnSafeRadius / activeProfile.TileSize;
        
        foreach (Vector2Int collectiblePos in collectiblePositions)
        {
            if (Vector2Int.Distance(pos, collectiblePos) < safeRadius)
                return false;
        }

        if (Vector2Int.Distance(pos, goalPosition) < safeRadius)
            return false;

        return true;
    }

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
        }

        // Update UI if necessary
        UIController uiController = FindFirstObjectByType<UIController>();
        if (uiController)
        {
            // Could trigger UI refresh here
        }
    }

    #endregion

    #region Debug & Visualization

    void OnDrawGizmosSelected()
    {
        if (!showWalkableArea || levelGrid == null) return;

        int size = activeProfile ? activeProfile.LevelSize : 10;
        float tileSize = activeProfile ? activeProfile.TileSize : 2f;

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
    }

    #endregion
}
