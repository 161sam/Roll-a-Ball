using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using RollABall.Utility;
using RollABall.Environment;

namespace RollABall.Generators
{
    /// <summary>
    /// Modulare Klasse für Object-Instantiierung mit Object Pooling
    /// Zuständig für: Ground/Wall Tiles, Collectibles, Interactive Elements, Steampunk Objects
    /// Nutzt PrefabPooler für Performance-Optimierung bei großen Leveln
    /// </summary>
    [System.Serializable]
    public class LevelObjectInstantiator
    {
        #region Events
        public System.Action<int> OnObjectsInstantiated;
        public System.Action<int> OnDynamicElementsCreated;
        public System.Action<string> OnInstantiationError;
        #endregion

        #region Private Data
        private LevelProfile activeProfile;
        private LevelTerrainGenerator terrainGen;
        private System.Random random;
        private Transform levelContainer;
        private Transform groundContainer;
        private Transform wallContainer;
        private Transform collectibleContainer;
        private Transform effectsContainer;
        private bool showGenerationDebug;
        private bool useObjectPooling;

        // Prefab references
        private GameObject groundPrefab;
        private GameObject wallPrefab;
        private GameObject collectiblePrefab;
        private GameObject goalZonePrefab;

        // Sector-based materials (for performance)
        private Material[] sectorGroundMaterials;
        private Material[] sectorWallMaterials;
        private bool useSectorMaterials = false;

        // Statistics
        private int objectsCreated = 0;
        #endregion

        #region Public Properties
        public int ObjectsCreated => objectsCreated;
        public bool UseObjectPooling => useObjectPooling;
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the Object Instantiator with required dependencies
        /// </summary>
        public void Initialize(LevelProfile profile, LevelTerrainGenerator terrain, 
                             GameObject ground, GameObject wall, GameObject collectible, GameObject goalZone,
                             Transform levelCont, Transform groundCont, Transform wallCont, 
                             Transform collectibleCont, Transform effectsCont,
                             System.Random rng, bool debug = false)
        {
            activeProfile = profile ?? throw new System.ArgumentNullException(nameof(profile));
            terrainGen = terrain ?? throw new System.ArgumentNullException(nameof(terrain));
            groundPrefab = ground ?? throw new System.ArgumentNullException(nameof(ground));
            wallPrefab = wall ?? throw new System.ArgumentNullException(nameof(wall));
            collectiblePrefab = collectible ?? throw new System.ArgumentNullException(nameof(collectible));
            goalZonePrefab = goalZone ?? throw new System.ArgumentNullException(nameof(goalZone));
            
            levelContainer = levelCont ?? throw new System.ArgumentNullException(nameof(levelCont));
            groundContainer = groundCont ?? throw new System.ArgumentNullException(nameof(groundCont));
            wallContainer = wallCont ?? throw new System.ArgumentNullException(nameof(wallCont));
            collectibleContainer = collectibleCont ?? throw new System.ArgumentNullException(nameof(collectibleCont));
            effectsContainer = effectsCont ?? throw new System.ArgumentNullException(nameof(effectsCont));
            
            random = rng ?? throw new System.ArgumentNullException(nameof(rng));
            showGenerationDebug = debug;

            // Enable object pooling for large levels (performance optimization)
            useObjectPooling = activeProfile.LevelSize >= 16;

            // Setup sector-based materials for large levels
            SetupSectorMaterials();

            objectsCreated = 0;

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelObjectInstantiator] Initialized. Object Pooling: {useObjectPooling}, " +
                         $"Sector Materials: {useSectorMaterials}, Level Size: {activeProfile.LevelSize}");
            }
        }

        private void SetupSectorMaterials()
        {
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

                if (showGenerationDebug)
                {
                    Debug.Log($"[LevelObjectInstantiator] Sector materials initialized for {activeProfile.LevelSize}x{activeProfile.LevelSize} level");
                }
            }
        }
        #endregion

        #region Level Object Creation
        /// <summary>
        /// Instantiate all level objects based on terrain grid
        /// </summary>
        public IEnumerator InstantiateLevelObjects()
        {
            if (terrainGen?.LevelGrid == null)
            {
                OnInstantiationError?.Invoke("No terrain grid available for instantiation");
                yield break;
            }

            objectsCreated = 0;
            int size = activeProfile.LevelSize;
            float tileSize = activeProfile.TileSize;
            int[,] levelGrid = terrainGen.LevelGrid;

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelObjectInstantiator] Starting object instantiation for {size}x{size} grid");
            }

            // Clear existing objects (use pooling if enabled)
            ClearExistingObjects();

            // Instantiate terrain objects
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

                        case 2: // Collectible spawn (create ground + collectible)
                            CreateGroundTile(worldPos, x, z);
                            CreateCollectible(worldPos + Vector3.up * activeProfile.CollectibleSpawnHeight);
                            break;

                        case 3: // Goal zone
                            CreateGoalZone(worldPos);
                            break;
                    }

                    objectsCreated++;

                    // Yield periodically to prevent frame drops
                    if (objectsCreated % 50 == 0)
                        yield return null;
                }
            }

            // Create dynamic elements (moving platforms, gates, etc.)
            yield return CreateDynamicElements();

            OnObjectsInstantiated?.Invoke(objectsCreated);

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelObjectInstantiator] Instantiated {objectsCreated} objects. " +
                         $"Dynamic elements: {terrainGen.MovingPlatformTiles.Count + terrainGen.RotatingObstaclePlatforms.Count}");
            }
        }

        private void ClearExistingObjects()
        {
            // Clear container hierarchies efficiently
            ClearContainer(groundContainer);
            ClearContainer(wallContainer);
            ClearContainer(collectibleContainer);
            ClearContainer(effectsContainer);
        }

        private void ClearContainer(Transform container)
        {
            if (!container) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i).gameObject;
                
                if (useObjectPooling)
                {
                    PrefabPooler.Release(child);
                }
                else
                {
                    if (Application.isEditor)
                        Object.DestroyImmediate(child);
                    else
                        Object.Destroy(child);
                }
            }
        }
        #endregion

        #region Object Creation Methods
        private void CreateGroundTile(Vector3 position, int gx, int gz)
        {
            GameObject ground = InstantiateObject(groundPrefab, position, Quaternion.identity, groundContainer);
            
            // Apply materials if available
            if (activeProfile.GroundMaterials != null && activeProfile.GroundMaterials.Length > 0)
            {
                Material material = GetGroundMaterial(gx, gz);
                ApplyMaterial(ground, material);
            }
        }

        private void CreateWallTile(Vector3 position, int gx, int gz)
        {
            GameObject wall = InstantiateObject(wallPrefab, position, Quaternion.identity, wallContainer);
            
            // Apply materials if available
            if (activeProfile.WallMaterials != null && activeProfile.WallMaterials.Length > 0)
            {
                Material material = GetWallMaterial(gx, gz);
                ApplyMaterial(wall, material);
            }
        }

        private void CreateCollectible(Vector3 position)
        {
            GameObject collectible = InstantiateObject(collectiblePrefab, position, Quaternion.identity, collectibleContainer);
            
            // Configure collectible if it has a CollectibleController
            CollectibleController controller = collectible.GetComponent<CollectibleController>();
            if (controller)
            {
                // Additional collectible configuration can be added here
            }
        }

        private void CreateGoalZone(Vector3 position)
        {
            GameObject goalZone = InstantiateObject(goalZonePrefab, position, Quaternion.identity, levelContainer);
            
            // Apply goal zone material if available
            if (activeProfile.GoalZoneMaterial)
            {
                ApplyMaterial(goalZone, activeProfile.GoalZoneMaterial);
            }
        }
        #endregion

        #region Dynamic Elements Creation
        /// <summary>
        /// Create dynamic elements like moving platforms, rotating obstacles, gates
        /// </summary>
        private IEnumerator CreateDynamicElements()
        {
            int dynamicObjectsCreated = 0;

            // Create moving platforms
            if (activeProfile.EnableMovingPlatforms && terrainGen.MovingPlatformTiles.Count > 0)
            {
                yield return CreateMovingPlatforms();
                dynamicObjectsCreated += terrainGen.MovingPlatformTiles.Count;
            }

            // Create rotating obstacles
            if (activeProfile.EnableRotatingObstacles && terrainGen.RotatingObstaclePlatforms.Count > 0)
            {
                yield return CreateRotatingObstacles();
                dynamicObjectsCreated += terrainGen.RotatingObstaclePlatforms.Count;
            }

            // Create interactive gates
            if (activeProfile.EnableInteractiveGates)
            {
                yield return CreateInteractiveGates();
                dynamicObjectsCreated += GetInteractiveGateCount();
            }

            OnDynamicElementsCreated?.Invoke(dynamicObjectsCreated);
        }

        private IEnumerator CreateMovingPlatforms()
        {
            if (activeProfile.MovingPlatformPrefabs == null || activeProfile.MovingPlatformPrefabs.Length == 0)
                yield break;

            float tileSize = activeProfile.TileSize;
            int objectsCreated = 0;

            foreach (Vector2Int tile in terrainGen.MovingPlatformTiles)
            {
                GameObject prefab = activeProfile.MovingPlatformPrefabs[random.Next(activeProfile.MovingPlatformPrefabs.Length)];
                if (prefab)
                {
                    Vector3 pos = new Vector3(tile.x * tileSize, activeProfile.CollectibleSpawnHeight, tile.y * tileSize);
                    InstantiateObject(prefab, pos, Quaternion.identity, effectsContainer);
                    objectsCreated++;

                    if (objectsCreated % 10 == 0)
                        yield return null;
                }
            }

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelObjectInstantiator] Created {objectsCreated} moving platforms");
            }
        }

        private IEnumerator CreateRotatingObstacles()
        {
            if (activeProfile.RotatingObstaclePrefabs == null || activeProfile.RotatingObstaclePrefabs.Length == 0)
                yield break;

            float tileSize = activeProfile.TileSize;
            int objectsCreated = 0;

            foreach (Vector2Int center in terrainGen.RotatingObstaclePlatforms)
            {
                GameObject prefab = activeProfile.RotatingObstaclePrefabs[random.Next(activeProfile.RotatingObstaclePrefabs.Length)];
                if (prefab)
                {
                    Vector3 pos = new Vector3(center.x * tileSize, activeProfile.CollectibleSpawnHeight, center.y * tileSize);
                    InstantiateObject(prefab, pos, Quaternion.identity, effectsContainer);
                    objectsCreated++;

                    if (objectsCreated % 5 == 0)
                        yield return null;
                }
            }

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelObjectInstantiator] Created {objectsCreated} rotating obstacles");
            }
        }

        private IEnumerator CreateInteractiveGates()
        {
            if (activeProfile.InteractiveGatePrefabs == null || activeProfile.InteractiveGatePrefabs.Length == 0)
                yield break;

            List<Vector2Int> walkableTiles = terrainGen.WalkableTiles;
            if (walkableTiles.Count < 2)
                yield break;

            int pairCount = Mathf.Max(1, Mathf.RoundToInt(walkableTiles.Count * activeProfile.InteractiveGateDensity));
            float tileSize = activeProfile.TileSize;
            int gatesCreated = 0;

            for (int i = 0; i < pairCount; i++)
            {
                if (walkableTiles.Count < 2)
                    break;

                Vector2Int switchTile = walkableTiles[random.Next(walkableTiles.Count)];
                Vector2Int gateTile = walkableTiles[random.Next(walkableTiles.Count)];

                // Ensure minimum distance between switch and gate
                int attempts = 0;
                int minDist = Mathf.Max(2, activeProfile.LevelSize / 3);
                while (Vector2Int.Distance(switchTile, gateTile) < minDist && attempts < 20)
                {
                    gateTile = walkableTiles[random.Next(walkableTiles.Count)];
                    attempts++;
                }

                // Create gate
                Vector3 gatePos = new Vector3(gateTile.x * tileSize, 0, gateTile.y * tileSize);
                GameObject gatePrefab = activeProfile.InteractiveGatePrefabs[random.Next(activeProfile.InteractiveGatePrefabs.Length)];
                GameObject gateObj = InstantiateObject(gatePrefab, gatePos, Quaternion.identity, levelContainer);
                
                var gateCtrl = gateObj.GetComponent<GateController>();
                if (!gateCtrl)
                    gateCtrl = gateObj.AddComponent<GateController>();

                // Create switch
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

                var trigger = switchObj.AddComponent<SwitchTrigger>();
                trigger.SetGate(gateCtrl);

                gatesCreated++;

                if (i % 5 == 0)
                    yield return null;
            }

            if (showGenerationDebug)
            {
                Debug.Log($"[LevelObjectInstantiator] Created {gatesCreated} interactive gate pairs");
            }
        }

        private int GetInteractiveGateCount()
        {
            List<Vector2Int> walkableTiles = terrainGen.WalkableTiles;
            if (walkableTiles.Count < 2)
                return 0;

            return Mathf.Max(1, Mathf.RoundToInt(walkableTiles.Count * activeProfile.InteractiveGateDensity));
        }
        #endregion

        #region Helper Methods
        private GameObject InstantiateObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (useObjectPooling)
            {
                return PrefabPooler.Get(prefab, position, rotation, parent);
            }
            else
            {
                return Object.Instantiate(prefab, position, rotation, parent);
            }
        }

        private Material GetGroundMaterial(int gx, int gz)
        {
            if (useSectorMaterials)
            {
                int sector = GetSector(gx, gz);
                return sectorGroundMaterials[sector];
            }
            else
            {
                return activeProfile.GroundMaterials[random.Next(activeProfile.GroundMaterials.Length)];
            }
        }

        private Material GetWallMaterial(int gx, int gz)
        {
            if (useSectorMaterials)
            {
                int sector = GetSector(gx, gz);
                return sectorWallMaterials[sector];
            }
            else
            {
                return activeProfile.WallMaterials[random.Next(activeProfile.WallMaterials.Length)];
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

        private void ApplyMaterial(GameObject obj, Material material)
        {
            if (material && obj)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer)
                {
                    renderer.material = material;
                }
            }
        }
        #endregion

        #region Debug & Cleanup
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Reset()
        {
            objectsCreated = 0;
            
            if (useObjectPooling)
            {
                // Clear pools if needed
                // PrefabPooler.Clear(); // Only if you want to completely clear pools
            }
        }

        /// <summary>
        /// Log instantiation statistics
        /// </summary>
        public void LogInstantiationInfo()
        {
            Debug.Log($"=== LevelObjectInstantiator Debug Info ===");
            Debug.Log($"Objects Created: {objectsCreated}");
            Debug.Log($"Object Pooling: {useObjectPooling}");
            Debug.Log($"Sector Materials: {useSectorMaterials}");
            Debug.Log($"Moving Platforms: {terrainGen?.MovingPlatformTiles?.Count ?? 0}");
            Debug.Log($"Rotating Obstacles: {terrainGen?.RotatingObstaclePlatforms?.Count ?? 0}");
        }
        #endregion
    }
}
