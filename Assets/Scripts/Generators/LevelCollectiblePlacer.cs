using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace RollABall.Generators
{
    /// <summary>
    /// Modulare Klasse für die Platzierung von Collectibles und Goal Zone
    /// Extrahiert aus LevelGenerator.cs für bessere Wartbarkeit
    /// Verantwortlichkeiten: Zone-basierte Collectible-Verteilung, Dead-End-Erkennung, Goal Zone Placement
    /// </summary>
    [System.Serializable]
    public class LevelCollectiblePlacer
    {
        #region Events
        public System.Action<int> OnCollectiblesPlaced;
        public System.Action<Vector2Int> OnGoalZonePlaced;
        public System.Action<string> OnPlacementError;
        #endregion

        #region Private Data
        private List<Vector2Int> collectiblePositions;
        private Vector2Int goalPosition;
        private System.Random random;
        
        // Dependencies (injected)
        private LevelProfile activeProfile;
        private int[,] levelGrid;
        private List<Vector2Int> walkableTiles;
        private List<Vector2Int> mainPath;
        private bool showGenerationDebug;
        #endregion

        #region Public Properties
        public List<Vector2Int> CollectiblePositions => collectiblePositions ?? new List<Vector2Int>();
        public Vector2Int GoalPosition => goalPosition;
        public int PlacedCount => collectiblePositions?.Count ?? 0;
        #endregion

        #region Initialization
        /// <summary>
        /// Initialisiert den Collectible Placer mit den erforderlichen Dependencies
        /// </summary>
        public void Initialize(LevelProfile profile, int[,] grid, List<Vector2Int> walkable, 
                             List<Vector2Int> path, System.Random rng, bool debug = false)
        {
            activeProfile = profile ?? throw new ArgumentNullException(nameof(profile));
            levelGrid = grid ?? throw new ArgumentNullException(nameof(grid));
            walkableTiles = walkable ?? throw new ArgumentNullException(nameof(walkable));
            mainPath = path ?? new List<Vector2Int>();
            random = rng ?? throw new ArgumentNullException(nameof(rng));
            showGenerationDebug = debug;

            // Initialize collections
            collectiblePositions = new List<Vector2Int>();
            goalPosition = Vector2Int.zero;

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelCollectiblePlacer] Initialized with {walkableTiles.Count} walkable tiles, " +
                         $"{mainPath.Count} main path tiles, target: {activeProfile.CollectibleCount} collectibles");
            }
        }

        /// <summary>
        /// Validiert ob der Placer korrekt initialisiert wurde
        /// </summary>
        public bool IsInitialized()
        {
            return activeProfile != null && levelGrid != null && walkableTiles != null && random != null;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Platziert alle Collectibles basierend auf dem aktiven Profil
        /// Coroutine für non-blocking Ausführung
        /// </summary>
        public IEnumerator PlaceAllCollectibles()
        {
            if (!IsInitialized())
            {
                OnPlacementError?.Invoke("LevelCollectiblePlacer not initialized");
                yield break;
            }

            collectiblePositions.Clear();
            
            yield return PlaceCollectiblesCoroutine();
            
            OnCollectiblesPlaced?.Invoke(collectiblePositions.Count);
        }

        /// <summary>
        /// Platziert die Goal Zone an optimaler Position
        /// Coroutine für non-blocking Ausführung
        /// </summary>
        public IEnumerator PlaceGoalZoneCoroutine()
        {
            if (!IsInitialized())
            {
                OnPlacementError?.Invoke("LevelCollectiblePlacer not initialized");
                yield break;
            }

            yield return PlaceGoalZoneInternal();
            
            OnGoalZonePlaced?.Invoke(goalPosition);
        }

        /// <summary>
        /// Erstellt Collectible GameObject an der angegebenen Position
        /// </summary>
        public GameObject CreateCollectibleObject(Vector3 position, GameObject prefab, Transform container)
        {
            if (!prefab)
            {
                Debug.LogError("[LevelCollectiblePlacer] Collectible prefab is null");
                return null;
            }

            return CreateCollectible(position, prefab, container);
        }

        /// <summary>
        /// Erstellt Goal Zone GameObject an der angegebenen Position
        /// </summary>
        public GameObject CreateGoalZoneObject(Vector3 position, GameObject prefab, Transform container, Material material = null)
        {
            if (!prefab)
            {
                Debug.LogError("[LevelCollectiblePlacer] Goal zone prefab is null");
                return null;
            }

            return CreateGoalZone(position, prefab, container, material);
        }

        /// <summary>
        /// Markiert Collectibles und Goal Zone im Level Grid
        /// </summary>
        public void ApplyToLevelGrid()
        {
            if (!IsInitialized()) return;

            // Mark collectible positions in grid
            foreach (Vector2Int pos in collectiblePositions)
            {
                if (IsValidGridPosition(pos))
                {
                    levelGrid[pos.x, pos.y] = 2; // Collectible marker
                }
            }

            // Mark goal position in grid
            if (IsValidGridPosition(goalPosition))
            {
                levelGrid[goalPosition.x, goalPosition.y] = 3; // Goal marker
            }
        }

        /// <summary>
        /// Zurücksetzen für neue Generierung
        /// </summary>
        public void Reset()
        {
            collectiblePositions?.Clear();
            goalPosition = Vector2Int.zero;
        }
        #endregion

        #region Collectible Placement Logic
        /// <summary>
        /// Hauptlogik für Collectible-Platzierung mit Zone-basierter Verteilung
        /// MERGED: Enhanced collectible placement from REMOTE
        /// </summary>
        private IEnumerator PlaceCollectiblesCoroutine()
        {
            int targetCount = activeProfile.CollectibleCount;
            int minDistance = activeProfile.MinCollectibleDistance;

            // Zone-based distribution for even spread
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
                int zone = CalculateZone(pos, half);
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

                    if (IsValidCollectiblePosition(candidate, minDistance))
                    {
                        collectiblePositions.Add(candidate);
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

                if (IsValidCollectiblePosition(candidate, minDistance))
                {
                    collectiblePositions.Add(candidate);
                }

                if (attempts % 50 == 0)
                    yield return null;
            }

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelCollectiblePlacer] Placed {collectiblePositions.Count}/{targetCount} collectibles in {attempts} attempts");
                LogZoneDistribution(zonePools, half);
            }
        }

        /// <summary>
        /// Berechnet die Zone (0-3) für eine Position
        /// </summary>
        private int CalculateZone(Vector2Int pos, int half)
        {
            int zone = 0;
            if (pos.x >= half) zone += 1;
            if (pos.y >= half) zone += 2;
            return zone;
        }

        /// <summary>
        /// Prüft ob eine Position für ein Collectible gültig ist
        /// </summary>
        private bool IsValidCollectiblePosition(Vector2Int candidate, int minDistance)
        {
            foreach (Vector2Int existing in collectiblePositions)
            {
                if (Vector2Int.Distance(candidate, existing) < minDistance)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Loggt die Verteilung der Collectibles über die Zonen (Debug)
        /// </summary>
        private void LogZoneDistribution(List<Vector2Int>[] zonePools, int half)
        {
            for (int z = 0; z < 4; z++)
            {
                int collectiblesInZone = collectiblePositions.Count(pos => CalculateZone(pos, half) == z);
                Debug.Log($"[LevelCollectiblePlacer] Zone {z}: {collectiblesInZone} collectibles, {zonePools[z].Count} candidates");
            }
        }
        #endregion

        #region Dead End Detection
        /// <summary>
        /// Findet Dead-End-Positionen für bevorzugte Collectible-Platzierung
        /// MERGED: Dead end detection from REMOTE
        /// </summary>
        private List<Vector2Int> FindDeadEnds()
        {
            List<Vector2Int> deadEnds = new List<Vector2Int>();
            int size = activeProfile.LevelSize;

            foreach (Vector2Int tile in walkableTiles)
            {
                if (mainPath.Contains(tile))
                    continue;

                int neighbors = CountWalkableNeighbors(tile, size);
                if (neighbors == 1)
                    deadEnds.Add(tile);
            }

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelCollectiblePlacer] Found {deadEnds.Count} dead ends for prioritized placement");
            }

            return deadEnds;
        }

        /// <summary>
        /// Zählt begehbare Nachbarn einer Position
        /// </summary>
        private int CountWalkableNeighbors(Vector2Int tile, int size)
        {
            int neighbors = 0;
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = tile + d;
                if (n.x >= 0 && n.x < size && n.y >= 0 && n.y < size)
                {
                    if (levelGrid[n.x, n.y] == 0) // Walkable
                        neighbors++;
                }
            }
            
            return neighbors;
        }
        #endregion

        #region Goal Zone Placement
        /// <summary>
        /// Platziert die Goal Zone an der optimalen Position
        /// </summary>
        private IEnumerator PlaceGoalZoneInternal()
        {
            // Find the position furthest from the center or spawn point
            Vector2Int centerPos = new Vector2Int(activeProfile.LevelSize / 2, activeProfile.LevelSize / 2);
            
            float maxDistance = 0f;
            Vector2Int bestPosition = centerPos;

            foreach (Vector2Int pos in walkableTiles)
            {
                // Skip positions with collectibles
                if (IsValidGridPosition(pos) && levelGrid[pos.x, pos.y] == 2) 
                    continue;

                float distance = Vector2Int.Distance(pos, centerPos);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bestPosition = pos;
                }
            }

            goalPosition = bestPosition;

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelCollectiblePlacer] Goal zone placed at {goalPosition} (distance: {maxDistance:F2} from center)");
            }

            yield return null;
        }
        #endregion

        #region Object Creation
        /// <summary>
        /// Erstellt ein Collectible GameObject
        /// </summary>
        private GameObject CreateCollectible(Vector3 position, GameObject prefab, Transform container)
        {
            GameObject collectible = CreateOrPoolObject(prefab, position, Quaternion.identity, container);
            
            // Configure collectible if it has a CollectibleController
            CollectibleController controller = collectible.GetComponent<CollectibleController>();
            if (controller)
            {
                // Could customize collectible data here based on profile
                // Example: controller.SetValue(activeProfile.CollectibleValue);
            }

            return collectible;
        }

        /// <summary>
        /// Erstellt ein Goal Zone GameObject
        /// </summary>
        private GameObject CreateGoalZone(Vector3 position, GameObject prefab, Transform container, Material material = null)
        {
            // NOTE: goalZonePrefab should include a slightly raised base to avoid Z-fighting
            GameObject goalZone = CreateOrPoolObject(prefab, position, Quaternion.identity, container);
            
            // Apply goal zone material if available
            if (material)
            {
                Renderer renderer = goalZone.GetComponent<Renderer>();
                if (renderer)
                    renderer.material = material;
            }

            return goalZone;
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
                Debug.LogWarning($"[LevelCollectiblePlacer] Object pooling not available: {e.Message}");
            }

            // Fallback to instantiate
            if (obj == null)
            {
                obj = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            }

            return obj;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Prüft ob eine Grid-Position gültig ist
        /// </summary>
        private bool IsValidGridPosition(Vector2Int pos)
        {
            int size = activeProfile.LevelSize;
            return pos.x >= 0 && pos.x < size && pos.y >= 0 && pos.y < size;
        }

        /// <summary>
        /// Konvertiert Grid-Position zu Weltkoordinaten
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos, float yOffset = 0f)
        {
            return new Vector3(
                gridPos.x * activeProfile.TileSize,
                yOffset,
                gridPos.y * activeProfile.TileSize
            );
        }

        /// <summary>
        /// Konvertiert Weltkoordinaten zu Grid-Position
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / activeProfile.TileSize),
                Mathf.RoundToInt(worldPos.z / activeProfile.TileSize)
            );
        }
        #endregion

        #region Debug Information
        /// <summary>
        /// Gibt Debug-Informationen über die Platzierung aus
        /// </summary>
        public void LogPlacementInfo()
        {
            if (!IsInitialized()) return;

            Debug.Log($"=== LevelCollectiblePlacer Debug Info ===");
            Debug.Log($"Collectibles: {collectiblePositions.Count}/{activeProfile.CollectibleCount}");
            Debug.Log($"Goal Position: {goalPosition}");
            Debug.Log($"Walkable Tiles: {walkableTiles.Count}");
            Debug.Log($"Main Path Tiles: {mainPath.Count}");

            // Zone distribution
            int half = activeProfile.LevelSize / 2;
            int[] zoneCount = new int[4];
            foreach (Vector2Int pos in collectiblePositions)
            {
                zoneCount[CalculateZone(pos, half)]++;
            }
            
            for (int i = 0; i < 4; i++)
            {
                Debug.Log($"Zone {i}: {zoneCount[i]} collectibles");
            }
        }
        #endregion
    }
}
