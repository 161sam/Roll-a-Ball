using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

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
    [SerializeField] private Color mainPathColor = Color.cyan;
    [SerializeField] private Color platformCenterColor = Color.magenta;

    // Private generation data
    private System.Random random;
    private int[,] levelGrid; // 0 = walkable, 1 = wall, 2 = collectible spawn, 3 = goal
    private Vector3 levelCenter;
    private List<Vector2Int> walkableTiles;
    private List<Vector2Int> collectiblePositions;
    private List<Vector2Int> mainPath;
    private List<Vector2Int> platformCenters;
    private List<(Vector2Int from, Vector2Int to)> platformConnections; // Dynamic bridges
    private List<Vector2Int> movingPlatformTiles;                       // Bridge spawn points
    private List<Vector2Int> rotatingObstaclePlatforms;                 // Platforms with obstacles
    private List<Vector3> steamEmitterPositions;                        // Steam emitter placement
    private List<(Vector3 switchPos, Vector3 gatePos)> interactiveGatePairs; // GateTrigger connection
    private Vector2Int playerSpawnPosition;
    private Vector2Int goalPosition;
    private bool isGenerating = false;

    private Material[] sectorGroundMaterials;   // Sector-based material assignment
    private Material[] sectorWallMaterials;     // Sector-based material assignment
    private bool useSectorMaterials = false;    // Sector-based material assignment

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
        platformConnections?.Clear();
        movingPlatformTiles?.Clear();
        rotatingObstaclePlatforms?.Clear();
        steamEmitterPositions?.Clear();
        interactiveGatePairs?.Clear();
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
            yield return StartCoroutine(PlaceInteractiveElements());
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

                if (showGenerationDebug)
                {
                    int count = FindObjectsOfType<RollABall.VFX.SteamEmitterController>().Count(e => e.HasMovementInfluence);
                    Debug.Log($"Steam emitters affecting player: {count}");
                }
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
        mainPath = new List<Vector2Int>();
        platformCenters = new List<Vector2Int>();
        platformConnections = new List<(Vector2Int from, Vector2Int to)>();
        movingPlatformTiles = new List<Vector2Int>();
        rotatingObstaclePlatforms = new List<Vector2Int>();
        steamEmitterPositions = new List<Vector3>();
        interactiveGatePairs = new List<(Vector3 switchPos, Vector3 gatePos)>();

        // Sector-based material assignment
        useSectorMaterials = activeProfile.LevelSize >= 16;
        if (useSectorMaterials)
        {
            sectorGroundMaterials = new Material[4];
            sectorWallMaterials = new Material[4];

            for (int i = 0; i < 4; i++)
            {
                if (activeProfile.GroundMaterials != null && activeProfile.GroundMaterials.Length > 0)
                    sectorGroundMaterials[i] = activeProfile.GroundMaterials[random.Next(activeProfile.GroundMaterials.Length)];

                if (activeProfile.WallMaterials != null && activeProfile.WallMaterials.Length > 0)
                    sectorWallMaterials[i] = activeProfile.WallMaterials[random.Next(activeProfile.WallMaterials.Length)];
            }
        }

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
            PrefabPooler.Release(child); // Object Pooling enabled for large levels
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

        // Ensure path from spawn to goal
        Vector2Int start = new Vector2Int(1, 1);
        Vector2Int end = new Vector2Int(size - 2, size - 2);
        mainPath.Clear();
        HashSet<Vector2Int> pathSet = new HashSet<Vector2Int>();
        foreach (Vector2Int p in GetLine(start, end))
        {
            mainPath.Add(p);
            pathSet.Add(p);
            levelGrid[p.x, p.y] = 0;
        }

        if (showGenerationDebug)
        {
            Debug.Log($"Generated main path with {mainPath.Count} tiles");
        }

        // Place random obstacles while keeping the main path clear
        for (int x = 1; x < size - 1; x++)
        {
            for (int z = 1; z < size - 1; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);

                // Prevent walls on main path
                if (pathSet.Contains(pos))
                    continue;

                if (random.NextDouble() < obstacleDensity)
                {
                    bool adjacentWall = false;
                    for (int dx = -1; dx <= 1 && !adjacentWall; dx++)
                    {
                        for (int dz = -1; dz <= 1 && !adjacentWall; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 1 || nx >= size - 1 || nz < 1 || nz >= size - 1)
                                continue;
                            if (levelGrid[nx, nz] == 1)
                                adjacentWall = true;
                        }
                    }

                    if (!adjacentWall)
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
        // Start with all interior cells blocked
        for (int x = 1; x < size - 1; x++)
        {
            for (int z = 1; z < size - 1; z++)
            {
                levelGrid[x, z] = 1; // Wall
            }
        }

        mainPath.Clear();
        platformCenters.Clear();

        Vector2Int start = new Vector2Int(1, 1);
        Vector2Int end = new Vector2Int(size - 2, size - 2);

        // Build connecting path of platform centers
        Vector2Int current = start;
        platformCenters.Add(current);

        while (Mathf.Abs(current.x - end.x) > 3 || Mathf.Abs(current.y - end.y) > 3)
        {
            int stepX = Mathf.Clamp(end.x - current.x, -2, 2);
            int stepZ = Mathf.Clamp(end.y - current.y, -2, 2);

            // Randomize within jump distance
            stepX += random.Next(-1, 2);
            stepZ += random.Next(-1, 2);

            stepX = Mathf.Clamp(stepX, -3, 3);
            stepZ = Mathf.Clamp(stepZ, -3, 3);

            // Ensure Manhattan distance <= 3
            if (Mathf.Abs(stepX) + Mathf.Abs(stepZ) > 3)
            {
                if (Mathf.Abs(stepX) > Mathf.Abs(stepZ))
                    stepX = stepX > 0 ? 3 - Mathf.Abs(stepZ) : -(3 - Mathf.Abs(stepZ));
                else
                    stepZ = stepZ > 0 ? 3 - Mathf.Abs(stepX) : -(3 - Mathf.Abs(stepX));
            }

            Vector2Int next = new Vector2Int(
                Mathf.Clamp(current.x + stepX, 1, size - 2),
                Mathf.Clamp(current.y + stepZ, 1, size - 2));

            if (next == current)
                break;

            platformCenters.Add(next);

            // Ensure platform path connectivity
            foreach (Vector2Int p in GetLine(current, next))
            {
                mainPath.Add(p);
                levelGrid[p.x, p.y] = 0;
            }

            current = next;

            yield return null;
        }

        platformCenters.Add(end);
        foreach (Vector2Int p in GetLine(current, end))
        {
            mainPath.Add(p);
            levelGrid[p.x, p.y] = 0;
        }

        // Create platform islands at each center
        foreach (Vector2Int center in platformCenters)
        {
            int platformSize = random.Next(2, 4);
            for (int dx = -platformSize; dx <= platformSize; dx++)
            {
                for (int dz = -platformSize; dz <= platformSize; dz++)
                {
                    int x = center.x + dx;
                    int z = center.y + dz;

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

        // Dynamic platform bridge generation
        platformConnections.Clear();
        movingPlatformTiles.Clear();
        for (int i = 0; i < platformCenters.Count; i++)
        {
            for (int j = i + 1; j < platformCenters.Count; j++)
            {
                float dist = Vector2Int.Distance(platformCenters[i], platformCenters[j]);
                if (dist > 2f && dist < 6f)
                {
                    platformConnections.Add((platformCenters[i], platformCenters[j]));

                    List<Vector2Int> line = GetLine(platformCenters[i], platformCenters[j]).ToList();
                    for (int l = 0; l < line.Count; l++)
                    {
                        Vector2Int p = line[l];
                        if (levelGrid[p.x, p.y] == 1)
                            levelGrid[p.x, p.y] = 0;

                        if (activeProfile.EnableMovingPlatforms && l % 2 == 0 && p != platformCenters[i] && p != platformCenters[j])
                            movingPlatformTiles.Add(p);
                    }
                }
            }
        }

        // Rotating obstacle placement
        rotatingObstaclePlatforms.Clear();
        if (activeProfile.EnableRotatingObstacles)
        {
            int obstacleCount = Mathf.Max(1, Mathf.RoundToInt(platformCenters.Count * 0.1f));
            for (int i = 0; i < obstacleCount; i++)
            {
                Vector2Int c = platformCenters[random.Next(platformCenters.Count)];
                if (!rotatingObstaclePlatforms.Contains(c))
                    rotatingObstaclePlatforms.Add(c);
            }
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

    private IEnumerable<Vector2Int> GetLine(Vector2Int start, Vector2Int end)
    {
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;
        int dx = Mathf.Abs(x1 - x0);
        int dy = -Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            yield return new Vector2Int(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
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

        int size = activeProfile.LevelSize;
        int half = size / 2;

        // Potential positions excluding the guaranteed main path
        HashSet<Vector2Int> mainPathSet = new HashSet<Vector2Int>(mainPath);
        List<Vector2Int> deadEnds = FindDeadEnds();
        List<Vector2Int> otherTiles = new List<Vector2Int>();

        foreach (Vector2Int tile in walkableTiles)
        {
            if (mainPathSet.Contains(tile))
                continue;

            if (!deadEnds.Contains(tile))
                otherTiles.Add(tile);
        }

        // Prioritize dead ends for placement
        List<Vector2Int> allCandidates = new List<Vector2Int>(deadEnds);
        allCandidates.AddRange(otherTiles);

        // Split candidates into 4 zones for even distribution
        List<Vector2Int>[] zonePools = new List<Vector2Int>[4];
        for (int i = 0; i < 4; i++)
            zonePools[i] = new List<Vector2Int>();

        foreach (Vector2Int pos in allCandidates)
        {
            int zone = 0;
            if (pos.x >= half) zone += 1;
            if (pos.y >= half) zone += 2;
            zonePools[zone].Add(pos);
        }

        int attempts = 0;
        const int maxAttempts = 1000;

        // Try to place at least one collectible per zone if possible
        for (int z = 0; z < 4 && collectiblePositions.Count < targetCount; z++)
        {
            List<Vector2Int> pool = zonePools[z];
            while (pool.Count > 0 && collectiblePositions.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;
                int index = random.Next(pool.Count);
                Vector2Int candidate = pool[index];
                pool.RemoveAt(index);

                bool valid = true;
                foreach (Vector2Int existing in collectiblePositions)
                {
                    if (Vector2Int.Distance(candidate, existing) < minDistance)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    collectiblePositions.Add(candidate);
                    levelGrid[candidate.x, candidate.y] = 2; // Mark collectible
                    break; // only one per zone in first pass
                }

                if (attempts % 50 == 0)
                    yield return null;
            }
        }

        // Combine remaining positions from all zones
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        foreach (var pool in zonePools)
            availablePositions.AddRange(pool);

        // Fill remaining collectibles
        while (collectiblePositions.Count < targetCount && attempts < maxAttempts && availablePositions.Count > 0)
        {
            attempts++;
            int index = random.Next(availablePositions.Count);
            Vector2Int candidate = availablePositions[index];
            availablePositions.RemoveAt(index);

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
                levelGrid[candidate.x, candidate.y] = 2;
            }

            if (attempts % 50 == 0)
                yield return null;
        }

        if (showGenerationDebug)
        {
            Debug.Log($"Placed {collectiblePositions.Count}/{targetCount} collectibles in {attempts} attempts");
        }
    }

    // Collectible placement based on dead ends and distribution zones
    private List<Vector2Int> FindDeadEnds()
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        int size = activeProfile.LevelSize;

        foreach (Vector2Int tile in walkableTiles)
        {
            if (mainPath.Contains(tile))
                continue;

            int neighbors = 0;
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = tile + d;
                if (n.x >= 0 && n.x < size && n.y >= 0 && n.y < size)
                {
                    if (levelGrid[n.x, n.y] == 0)
                        neighbors++;
                }
            }
            if (neighbors == 1)
                deadEnds.Add(tile);
        }

        return deadEnds;
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
                        CreateGroundTile(worldPos, x, z); // Object Pooling enabled for large levels
                        break;

                    case 1: // Wall
                        CreateWallTile(worldPos, x, z); // Object Pooling enabled for large levels
                        break;

                    case 2: // Collectible
                        CreateGroundTile(worldPos, x, z); // Object Pooling enabled for large levels
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

        // Dynamic platform bridge generation
        foreach (Vector2Int tile in movingPlatformTiles)
        {
            if (activeProfile.MovingPlatformPrefabs != null && activeProfile.MovingPlatformPrefabs.Length > 0)
            {
                GameObject prefab = activeProfile.MovingPlatformPrefabs[random.Next(activeProfile.MovingPlatformPrefabs.Length)];
                if (prefab)
                {
                    Vector3 pos = new Vector3(tile.x * tileSize, activeProfile.CollectibleSpawnHeight, tile.y * tileSize);
                    Instantiate(prefab, pos, Quaternion.identity, levelContainer);
                }
            }
        }

        // Rotating obstacle placement
        foreach (Vector2Int center in rotatingObstaclePlatforms)
        {
            if (activeProfile.RotatingObstaclePrefabs != null && activeProfile.RotatingObstaclePrefabs.Length > 0)
            {
                GameObject prefab = activeProfile.RotatingObstaclePrefabs[random.Next(activeProfile.RotatingObstaclePrefabs.Length)];
                if (prefab)
                {
                    Vector3 pos = new Vector3(center.x * tileSize, activeProfile.CollectibleSpawnHeight, center.y * tileSize);
                    Instantiate(prefab, pos, Quaternion.identity, levelContainer);
                }
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

    private void CreateGroundTile(Vector3 position, int gx, int gz)
    {
        GameObject ground = PrefabPooler.Get(groundPrefab, position, Quaternion.identity, groundContainer); // Object Pooling enabled for large levels
        
        // Apply materials if available
        if (activeProfile.GroundMaterials != null && activeProfile.GroundMaterials.Length > 0)
        {
            Material material = null;
            if (useSectorMaterials)
            {
                int sector = GetSector(gx, gz);
                material = sectorGroundMaterials[sector]; // Sector-based material assignment
            }
            else
            {
                material = activeProfile.GroundMaterials[random.Next(activeProfile.GroundMaterials.Length)];
            }
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

    private void CreateWallTile(Vector3 position, int gx, int gz)
    {
        GameObject wall = PrefabPooler.Get(wallPrefab, position, Quaternion.identity, wallContainer); // Object Pooling enabled for large levels
        
        // Apply materials if available
        if (activeProfile.WallMaterials != null && activeProfile.WallMaterials.Length > 0)
        {
            Material material = null;
            if (useSectorMaterials)
            {
                int sector = GetSector(gx, gz);
                material = sectorWallMaterials[sector]; // Sector-based material assignment
            }
            else
            {
                material = activeProfile.WallMaterials[random.Next(activeProfile.WallMaterials.Length)];
            }
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
        GameObject collectible = PrefabPooler.Get(collectiblePrefab, position, Quaternion.identity, collectibleContainer); // Object Pooling enabled for large levels
        
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
        GameObject goalZone = PrefabPooler.Get(goalZonePrefab, position, Quaternion.identity, levelContainer); // Object Pooling enabled for large levels
        
        // Apply goal zone material if available
        if (activeProfile.GoalZoneMaterial)
        {
            Renderer renderer = goalZone.GetComponent<Renderer>();
            if (renderer)
                renderer.material = activeProfile.GoalZoneMaterial;
        }
    }

    private int GetSector(int x, int z)
    {
        int half = activeProfile.LevelSize / 2;
        bool right = x >= half;
        bool top = z >= half;
        if (top)
            return right ? 3 : 2;
        return right ? 1 : 0;
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

        // Steam emitter integration for atmosphere
        if (activeProfile.GenerationMode == LevelGenerationMode.Platforms &&
            activeProfile.EnableSteamEmitters &&
            activeProfile.SteamEmitterPrefabs != null &&
            activeProfile.SteamEmitterPrefabs.Length > 0)
        {
            float density = Mathf.Clamp01(activeProfile.SteamEmitterDensity);
            steamEmitterPositions.Clear();

            foreach (Vector2Int center in platformCenters)
            {
                if (random.NextDouble() <= density)
                {
                    GameObject prefab = activeProfile.SteamEmitterPrefabs[random.Next(activeProfile.SteamEmitterPrefabs.Length)];
                    if (prefab)
                    {
                        Vector3 pos = new Vector3(
                            center.x * activeProfile.TileSize,
                            activeProfile.CollectibleSpawnHeight + 0.5f,
                            center.y * activeProfile.TileSize);

                        GameObject emitter = PrefabPooler.Get(prefab, pos, Quaternion.identity, effectsContainer); // Object Pooling enabled for large levels
                        steamEmitterPositions.Add(pos);

                        // Apply steam settings if a compatible controller exists
                        if (activeProfile.SteamSettings != null)
                        {
                            var controller = emitter.GetComponent<RollABall.VFX.SteamEmitterController>();
                            if (controller)
                            {
                                controller.Init(activeProfile.SteamSettings);

                                if (activeProfile.SteamSettings.SteamSounds != null && activeProfile.SteamSettings.SteamSounds.Length > 0)
                                {
                                    AudioClip steamClip = activeProfile.SteamSettings.SteamSounds[random.Next(activeProfile.SteamSettings.SteamSounds.Length)];
                                    controller.SetSteamSound(steamClip);
                                }

                                if (activeProfile.SteamSettings.MechanicalSounds != null && activeProfile.SteamSettings.MechanicalSounds.Length > 0)
                                {
                                    AudioClip mechClip = activeProfile.SteamSettings.MechanicalSounds[random.Next(activeProfile.SteamSettings.MechanicalSounds.Length)];
                                    controller.SetMechanicalSound(mechClip);
                                }
                            }
                        }
                    }
                }
            }
        }

        yield return null;
    }

    // Place interactive gates and switches
    private IEnumerator PlaceInteractiveElements()
    {
        if (!activeProfile.EnableInteractiveGates ||
            activeProfile.InteractiveGatePrefabs == null ||
            activeProfile.InteractiveGatePrefabs.Length == 0)
        {
            yield break;
        }

        int pairCount = Mathf.Max(1, Mathf.RoundToInt(walkableTiles.Count * activeProfile.InteractiveGateDensity));
        float tileSize = activeProfile.TileSize;

        // Place a gate near the goal that opens after collecting all items
        if (walkableTiles.Count > 0)
        {
            Vector2Int goalGateTile = goalPosition;
            List<Vector2Int> nearGoal = new List<Vector2Int>();
            foreach (Vector2Int pos in walkableTiles)
            {
                if (Vector2Int.Distance(pos, goalPosition) <= 2)
                    nearGoal.Add(pos);
            }
            if (nearGoal.Count > 0)
                goalGateTile = nearGoal[random.Next(nearGoal.Count)];

            Vector3 gatePos = new Vector3(goalGateTile.x * tileSize, 0, goalGateTile.y * tileSize);
            GameObject gatePrefab = activeProfile.InteractiveGatePrefabs[random.Next(activeProfile.InteractiveGatePrefabs.Length)];
            GameObject gateObj = Instantiate(gatePrefab, gatePos, Quaternion.identity, levelContainer);
            var gateCtrl = gateObj.GetComponent<RollABall.Environment.GateController>();
            if (!gateCtrl)
                gateCtrl = gateObj.AddComponent<RollABall.Environment.GateController>();

            gateCtrl.RequiresAllCollectibles = true; // Gate requires all collectibles to be opened
            gateCtrl.RequiresSwitchTrigger = false;

            if (showGenerationDebug)
                Debug.Log($"Placed goal gate at {gatePos}");
        }

        for (int i = 0; i < pairCount; i++)
        {
            if (walkableTiles.Count < 2)
                break;

            Vector2Int switchTile = walkableTiles[random.Next(walkableTiles.Count)];
            Vector2Int gateTile = walkableTiles[random.Next(walkableTiles.Count)];

            int attempts = 0;
            int minDist = Mathf.Max(2, activeProfile.LevelSize / 3);
            while (Vector2Int.Distance(switchTile, gateTile) < minDist && attempts < 20)
            {
                gateTile = walkableTiles[random.Next(walkableTiles.Count)];
                attempts++;
            }

            Vector3 gatePos = new Vector3(gateTile.x * tileSize, 0, gateTile.y * tileSize);
            GameObject gatePrefab = activeProfile.InteractiveGatePrefabs[random.Next(activeProfile.InteractiveGatePrefabs.Length)];
            GameObject gateObj = Instantiate(gatePrefab, gatePos, Quaternion.identity, levelContainer);
            var gateCtrl = gateObj.GetComponent<RollABall.Environment.GateController>();
            if (!gateCtrl)
                gateCtrl = gateObj.AddComponent<RollABall.Environment.GateController>();

            Vector3 switchPos = new Vector3(switchTile.x * tileSize, 0, switchTile.y * tileSize);
            GameObject switchObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            switchObj.transform.SetParent(levelContainer);
            switchObj.transform.position = switchPos;
            switchObj.transform.localScale = Vector3.one * (tileSize * 0.3f);
            Collider c = switchObj.GetComponent<Collider>();
            if (c is CapsuleCollider capsule)
                capsule.isTrigger = true;
            else if (c)
                c.isTrigger = true;

            var trigger = switchObj.AddComponent<RollABall.Environment.SwitchTrigger>();
            trigger.SetGate(gateCtrl);

            interactiveGatePairs.Add((switchPos, gatePos));

            if (i % 5 == 0)
                yield return null;
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
        if ((!showWalkableArea && !showGenerationDebug) || levelGrid == null) return;

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

        // Visualize main path
        if (showGenerationDebug && mainPath != null)
        {
            Gizmos.color = mainPathColor;
            foreach (Vector2Int p in mainPath)
            {
                Vector3 pos = new Vector3(p.x * tileSize, 0.1f, p.y * tileSize);
                Gizmos.DrawCube(pos, Vector3.one * tileSize * 0.3f);
            }
        }

        // Visualize platform centers
        if (showGenerationDebug && platformCenters != null)
        {
            Gizmos.color = platformCenterColor;
            foreach (Vector2Int c in platformCenters)
            {
                Vector3 pos = new Vector3(c.x * tileSize, 0.2f, c.y * tileSize);
                Gizmos.DrawSphere(pos, tileSize * 0.4f);
            }
        }

        // Visualize dynamic bridges
        if (showGenerationDebug && platformConnections != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var conn in platformConnections)
            {
                Vector3 from = new Vector3(conn.from.x * tileSize, 0.5f, conn.from.y * tileSize);
                Vector3 to = new Vector3(conn.to.x * tileSize, 0.5f, conn.to.y * tileSize);
                Gizmos.DrawLine(from, to);
            }
        }

        // Mark platforms with rotating obstacles
        if (showGenerationDebug && rotatingObstaclePlatforms != null)
        {
            Gizmos.color = Color.red;
            foreach (Vector2Int c in rotatingObstaclePlatforms)
            {
                Vector3 pos = new Vector3(c.x * tileSize, 1f, c.y * tileSize);
                Gizmos.DrawCube(pos, Vector3.one * tileSize * 0.3f);
            }
        }

        // Steam emitter integration for atmosphere
        if (showGenerationDebug && steamEmitterPositions != null)
        {
            Gizmos.color = Color.white;
            foreach (Vector3 pos in steamEmitterPositions)
            {
                Gizmos.DrawSphere(pos, tileSize * 0.2f);
            }
        }

        // Interactive gate connections
        if (showGenerationDebug && interactiveGatePairs != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var pair in interactiveGatePairs)
            {
                Gizmos.DrawLine(pair.switchPos + Vector3.up * 0.1f, pair.gatePos + Vector3.up * 0.1f);
                Gizmos.DrawSphere(pair.switchPos + Vector3.up * 0.2f, tileSize * 0.15f);
            }
        }
    }

    #endregion
}
