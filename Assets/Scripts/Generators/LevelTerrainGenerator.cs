using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace RollABall.Generators
{
    /// <summary>
    /// CLAUDE: REFACTORED from LevelGenerator.cs
    /// Handles terrain generation and grid-based level structure
    /// Responsible for: Grid systems, walkable paths, maze/platform layouts
    /// </summary>
    [System.Serializable]
    public class LevelTerrainGenerator
{
    [Header("Debug Visualization")]
    [SerializeField] private bool showTerrainDebug = false;
    [SerializeField] private Color walkableColor = Color.green;
    [SerializeField] private Color blockedColor = Color.red;
    [SerializeField] private Color mainPathColor = Color.cyan;
    [SerializeField] private Color platformCenterColor = Color.magenta;
    
    // Core terrain data
    private LevelProfile activeProfile;
    private System.Random random;
    private int[,] levelGrid; // 0 = walkable, 1 = wall, 2 = collectible spawn, 3 = goal
    private Vector3 levelCenter;
    private List<Vector2Int> walkableTiles;
    private List<Vector2Int> mainPath;
    private List<Vector2Int> platformCenters;
    private List<(Vector2Int from, Vector2Int to)> platformConnections;
    private List<Vector2Int> movingPlatformTiles;
    private List<Vector2Int> rotatingObstaclePlatforms;
    private List<RectInt> openRooms;
    private Vector2Int playerSpawnPosition;
    private LevelGenerationMode usedGenerationMode;
    
    // Generation statistics
    private int generationAttempts = 0;
    
    // Properties - Public API for other components
    public int[,] LevelGrid => levelGrid;
    public Vector3 LevelCenter => levelCenter;
    public List<Vector2Int> WalkableTiles => walkableTiles;
    public List<Vector2Int> MainPath => mainPath;
    public List<Vector2Int> PlatformCenters => platformCenters;
    public List<(Vector2Int from, Vector2Int to)> PlatformConnections => platformConnections;
    public List<Vector2Int> MovingPlatformTiles => movingPlatformTiles;
    public List<Vector2Int> RotatingObstaclePlatforms => rotatingObstaclePlatforms;
    public Vector2Int PlayerSpawnPosition => playerSpawnPosition;
    public LevelGenerationMode UsedGenerationMode => usedGenerationMode;
    
    /// <summary>
    /// Initialize terrain generation with the given profile and random seed
    /// </summary>
    public void InitializeTerrain(LevelProfile profile, System.Random randomGenerator)
    {
        random = randomGenerator;
        generationAttempts = 0;
        usedGenerationMode = DetermineGenerationMode(profile);
        
        if (showTerrainDebug)
        {
            Debug.Log($"[LevelTerrainGenerator] Initializing terrain with mode: {usedGenerationMode}");
        }
        
        // Initialize terrain data structures
        int size = profile.LevelSize;
        levelGrid = new int[size, size];
        walkableTiles = new List<Vector2Int>();
        mainPath = new List<Vector2Int>();
        platformCenters = new List<Vector2Int>();
        platformConnections = new List<(Vector2Int from, Vector2Int to)>();
        movingPlatformTiles = new List<Vector2Int>();
        rotatingObstaclePlatforms = new List<Vector2Int>();
        openRooms = new List<RectInt>();
        
        // Calculate level center
        levelCenter = new Vector3(
            (size - 1) * profile.TileSize * 0.5f,
            0,
            (size - 1) * profile.TileSize * 0.5f
        );
    }
    
    /// <summary>
    /// Generate the complete terrain structure
    /// </summary>
    public IEnumerator GenerateTerrain(LevelProfile profile)
    {
        yield return GenerateBaseStructure(profile);
        yield return GenerateWalkablePaths(profile);
        
        // Collect walkable tiles for other systems to use
        CollectWalkableTiles(profile);
        
        // Validate minimum walkable area
        ValidateWalkableArea(profile);
        
        if (showTerrainDebug)
        {
            Debug.Log($"[LevelTerrainGenerator] Terrain generation complete. Walkable tiles: {walkableTiles.Count}");
        }
    }
    
    /// <summary>
    /// Check if a position is walkable
    /// </summary>
    public bool IsPositionWalkable(Vector2Int position, LevelProfile profile)
    {
        int size = profile.LevelSize;
        if (position.x < 0 || position.x >= size || position.y < 0 || position.y >= size)
            return false;
            
        return levelGrid[position.x, position.y] == 0;
    }
    
    /// <summary>
    /// Mark a position with a specific type
    /// </summary>
    public void MarkPosition(Vector2Int position, int type)
    {
        if (levelGrid != null && position.x >= 0 && position.x < levelGrid.GetLength(0) 
            && position.y >= 0 && position.y < levelGrid.GetLength(1))
        {
            levelGrid[position.x, position.y] = type;
        }
    }
    
    /// <summary>
    /// Get a safe spawn position for the player
    /// </summary>
    public Vector2Int DeterminePlayerSpawn(LevelProfile profile)
    {
        if (profile.RandomizeSpawnPosition && walkableTiles.Count > 0)
        {
            // Find a position with enough safe radius
            foreach (Vector2Int pos in walkableTiles)
            {
                if (IsSafeSpawnPosition(pos, profile))
                {
                    playerSpawnPosition = pos;
                    return playerSpawnPosition;
                }
            }
        }
        
        // Fallback to first walkable position or center
        playerSpawnPosition = walkableTiles.Count > 0 ? walkableTiles[0] : Vector2Int.zero;
        return playerSpawnPosition;
    }
    
    #region Private Generation Methods
    
    private LevelGenerationMode DetermineGenerationMode(LevelProfile profile)
    {
        IAdaptiveGenerationModeProvider adaptive = profile as IAdaptiveGenerationModeProvider;
        if (adaptive != null)
        {
            return adaptive.GetAdaptiveGenerationMode(random.Next());
        }
        
        return profile.GenerationMode;
    }
    
    private IEnumerator GenerateBaseStructure(LevelProfile profile)
    {
        int size = profile.LevelSize;
        
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
    
    private IEnumerator GenerateWalkablePaths(LevelProfile profile)
    {
        float obstacleDensity = profile.ObstacleDensity;
        
        switch (usedGenerationMode)
        {
            case LevelGenerationMode.Simple:
                yield return GenerateSimpleLayout(profile, obstacleDensity);
                break;
            case LevelGenerationMode.Maze:
                yield return GenerateMazeLayout(profile);
                break;
            case LevelGenerationMode.Platforms:
                yield return GeneratePlatformLayout(profile);
                break;
            default:
                yield return GenerateSimpleLayout(profile, obstacleDensity);
                break;
        }
    }
    
    private IEnumerator GenerateSimpleLayout(LevelProfile profile, float obstacleDensity)
    {
        int size = profile.LevelSize;
        
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
        
        if (showTerrainDebug)
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
                    // Anti-clustering logic
                    if (!HasAdjacentWall(x, z, size))
                    {
                        levelGrid[x, z] = 1; // Wall
                    }
                }
            }
        }
        
        yield return null;
    }
    
    private IEnumerator GenerateMazeLayout(LevelProfile profile)
    {
        int size = profile.LevelSize;
        
        // Start with all walls (except perimeter)
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
        float complexityFactor = profile.PathComplexity;
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
    
    private IEnumerator GeneratePlatformLayout(LevelProfile profile)
    {
        int size = profile.LevelSize;
        
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
            Vector2Int next = GenerateNextPlatformPosition(current, end, size);
            
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
        yield return CreatePlatformIslands(profile);
        
        // Generate dynamic platform connections
        GeneratePlatformConnections(profile);
        
        yield return null;
    }
    
    private Vector2Int GenerateNextPlatformPosition(Vector2Int current, Vector2Int end, int size)
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
        
        return new Vector2Int(
            Mathf.Clamp(current.x + stepX, 1, size - 2),
            Mathf.Clamp(current.y + stepZ, 1, size - 2)
        );
    }
    
    private IEnumerator CreatePlatformIslands(LevelProfile profile)
    {
        int size = profile.LevelSize;
        
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
    }
    
    private void GeneratePlatformConnections(LevelProfile profile)
    {
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
                    
                    // Check for moving platforms feature
                    bool enableMovingPlatforms = HasProfileFeature(profile, "EnableMovingPlatforms");
                    
                    if (enableMovingPlatforms)
                    {
                        GenerateMovingPlatformTiles(platformCenters[i], platformCenters[j]);
                    }
                }
            }
        }
        
        // Generate rotating obstacle positions
        GenerateRotatingObstacles(profile);
    }
    
    private void GenerateMovingPlatformTiles(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> line = GetLine(from, to).ToList();
        for (int l = 0; l < line.Count; l++)
        {
            Vector2Int p = line[l];
            if (levelGrid[p.x, p.y] == 1)
                levelGrid[p.x, p.y] = 0;
            
            if (l % 2 == 0 && p != from && p != to)
                movingPlatformTiles.Add(p);
        }
    }
    
    private void GenerateRotatingObstacles(LevelProfile profile)
    {
        rotatingObstaclePlatforms.Clear();
        bool enableRotatingObstacles = HasProfileFeature(profile, "EnableRotatingObstacles");
        
        if (enableRotatingObstacles && platformCenters.Count > 0)
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
    
    #endregion
    
    #region Helper Methods
    
    private bool HasAdjacentWall(int x, int z, int size)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                int nx = x + dx;
                int nz = z + dz;
                if (nx < 1 || nx >= size - 1 || nz < 1 || nz >= size - 1)
                    continue;
                if (levelGrid[nx, nz] == 1)
                    return true;
            }
        }
        return false;
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
    
    private void CollectWalkableTiles(LevelProfile profile)
    {
        walkableTiles.Clear();
        int size = profile.LevelSize;
        
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
    
    private void ValidateWalkableArea(LevelProfile profile)
    {
        int size = profile.LevelSize;
        float walkablePercentage = (walkableTiles.Count / (float)(size * size)) * 100f;
        
        if (walkablePercentage < profile.MinWalkableArea)
        {
            if (showTerrainDebug)
            {
                Debug.LogWarning($"Walkable area too small ({walkablePercentage:F1}%), may need regeneration");
            }
            
            generationAttempts++;
        }
    }
    
    private bool IsSafeSpawnPosition(Vector2Int pos, LevelProfile profile)
    {
        float safeRadius = profile.SpawnSafeRadius / profile.TileSize;
        
        // Check distance from level boundaries
        if (pos.x < safeRadius || pos.y < safeRadius || 
            pos.x > profile.LevelSize - safeRadius || pos.y > profile.LevelSize - safeRadius)
            return false;
        
        // Additional safety checks can be added here
        return true;
    }
    
    private bool HasProfileFeature(LevelProfile profile, string featureName)
    {
        try
        {
            var prop = profile.GetType().GetProperty(featureName);
            return prop != null && (bool)prop.GetValue(profile);
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
    
    #region Debug Visualization
    
    void OnDrawGizmosSelected()
    {
        if (!showTerrainDebug || levelGrid == null) return;
        
        // Would need LevelProfile reference for tileSize - skip for now in modular version
        // This can be called from main LevelGenerator with proper parameters
    }
    
    /// <summary>
    /// Draw terrain debug information with provided parameters
    /// </summary>
    public void DrawTerrainDebugGizmos(LevelProfile profile)
    {
        if (!showTerrainDebug || levelGrid == null) return;
        
        int size = profile.LevelSize;
        float tileSize = profile.TileSize;
        
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
                }
            }
        }
        
        // Visualize main path
        if (mainPath != null)
        {
            Gizmos.color = mainPathColor;
            foreach (Vector2Int p in mainPath)
            {
                Vector3 pos = new Vector3(p.x * tileSize, 0.1f, p.y * tileSize);
                Gizmos.DrawCube(pos, Vector3.one * tileSize * 0.3f);
            }
        }
        
        // Visualize platform centers
        if (platformCenters != null)
        {
            Gizmos.color = platformCenterColor;
            foreach (Vector2Int c in platformCenters)
            {
                Vector3 pos = new Vector3(c.x * tileSize, 0.2f, c.y * tileSize);
                Gizmos.DrawSphere(pos, tileSize * 0.4f);
            }
        }
    }
    
    #endregion
    
    #region Events
    public System.Action<Vector2Int, Vector2Int> OnTerrainGenerationCompleted;
    public System.Action<string> OnTerrainGenerationError;
    #endregion
    
    #region Public API Methods
    /// <summary>
    /// Initialisiert den Terrain Generator
    /// </summary>
    public void Initialize(LevelProfile profile, int[,] grid, System.Random rng, bool debug = false)
    {
        activeProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        levelGrid = grid ?? throw new ArgumentNullException(nameof(grid));
        random = rng ?? throw new ArgumentNullException(nameof(rng));
        showTerrainDebug = debug;
        
        // Initialize collections
        walkableTiles = new List<Vector2Int>();
        mainPath = new List<Vector2Int>();
        platformCenters = new List<Vector2Int>();
        platformConnections = new List<(Vector2Int from, Vector2Int to)>();
        movingPlatformTiles = new List<Vector2Int>();
        rotatingObstaclePlatforms = new List<Vector2Int>();
        openRooms = new List<RectInt>();
        
        generationAttempts = 0;
        
        if (showTerrainDebug)
        {
            Debug.Log($"[LevelTerrainGenerator] Initialized for {profile.DisplayName}");
        }
    }
    
    /// <summary>
    /// Hauptmethode für Terrain-Generierung
    /// </summary>
    public IEnumerator GenerateTerrainCoroutine()
    {
        if (activeProfile == null)
        {
            OnTerrainGenerationError?.Invoke("No active profile set for terrain generation");
            yield break;
        }
        
        // Call with profile parameter
        yield return GenerateTerrain(activeProfile);
            
        // Set spawn position
        SetPlayerSpawnPosition();
        
        OnTerrainGenerationCompleted?.Invoke(playerSpawnPosition, new Vector2Int(0, 0));
    }
    
    /// <summary>
    /// Zurücksetzen für neue Generierung
    /// </summary>
    public void Reset()
    {
        walkableTiles?.Clear();
        mainPath?.Clear();
        platformCenters?.Clear();
        platformConnections?.Clear();
        movingPlatformTiles?.Clear();
        rotatingObstaclePlatforms?.Clear();
        openRooms?.Clear();
        generationAttempts = 0;
    }
    
    /// <summary>
    /// Loggt Terrain-Informationen für Debug
    /// </summary>
    public void LogTerrainInfo()
    {
        Debug.Log($"=== LevelTerrainGenerator Debug Info ===");
        Debug.Log($"Walkable Tiles: {walkableTiles?.Count ?? 0}");
        Debug.Log($"Main Path: {mainPath?.Count ?? 0} tiles");
        Debug.Log($"Platform Centers: {platformCenters?.Count ?? 0}");
        Debug.Log($"Moving Platform Tiles: {movingPlatformTiles?.Count ?? 0}");
        Debug.Log($"Rotating Obstacle Platforms: {rotatingObstaclePlatforms?.Count ?? 0}");
        Debug.Log($"Generation Attempts: {generationAttempts}");
        Debug.Log($"Used Generation Mode: {usedGenerationMode}");
    }
    
    /// <summary>
    /// Setzt die Player-Spawn-Position
    /// </summary>
    private void SetPlayerSpawnPosition()
    {
        if (walkableTiles != null && walkableTiles.Count > 0)
        {
            // Use first walkable position or center
            playerSpawnPosition = walkableTiles[0];
            
            if (showTerrainDebug)
            {
                Debug.Log($"[LevelTerrainGenerator] Player spawn set to {playerSpawnPosition}");
            }
        }
        else
        {
            playerSpawnPosition = new Vector2Int(1, 1); // Default fallback
        }
    }
    
    /// <summary>
    /// Zeichnet Terrain-Gizmos für Debug-Visualisierung
    /// </summary>
    public void DrawTerrainGizmos(float tileSize, Color mainPathColor, Color platformCenterColor)
    {
        // Main path visualization
        if (mainPath != null)
        {
            Gizmos.color = mainPathColor;
            foreach (Vector2Int p in mainPath)
            {
                Vector3 pos = new Vector3(p.x * tileSize, 0.1f, p.y * tileSize);
                Gizmos.DrawCube(pos, Vector3.one * tileSize * 0.3f);
            }
        }
        
        // Platform centers visualization
        if (platformCenters != null)
        {
            Gizmos.color = platformCenterColor;
            foreach (Vector2Int c in platformCenters)
            {
                Vector3 pos = new Vector3(c.x * tileSize, 0.2f, c.y * tileSize);
                Gizmos.DrawSphere(pos, tileSize * 0.4f);
            }
        }
    }
    #endregion
    }
}
