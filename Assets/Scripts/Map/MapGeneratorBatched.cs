using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace RollABall.Map
{
    /// <summary>
    /// Performance-optimized MapGenerator with mesh batching
    /// Combines road segments and buildings by material to reduce Draw Calls
    /// </summary>
    public class MapGeneratorBatched : MonoBehaviour
    {
        [Header("Generation Prefabs")]
        [SerializeField] private GameObject roadPrefab;
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private GameObject areaPrefab;
        [SerializeField] private GameObject collectiblePrefab;
        [SerializeField] private GameObject goalZonePrefab;
        [SerializeField] private GameObject playerPrefab;
        
        [Header("Road Materials by Type")]
        [SerializeField] private Material roadMotorway;
        [SerializeField] private Material roadPrimary;
        [SerializeField] private Material roadSecondary;
        [SerializeField] private Material roadResidential;
        [SerializeField] private Material roadFootway;
        [SerializeField] private Material roadDefault;
        
        [Header("Building Materials by Type")]
        [SerializeField] private Material residentialMaterial;
        [SerializeField] private Material industrialMaterial;
        [SerializeField] private Material commercialMaterial;
        [SerializeField] private Material officeMaterial;
        [SerializeField] private Material defaultBuildingMaterial;
        
        [Header("Area Materials")]
        [SerializeField] private Material parkMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Material forestMaterial;
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material defaultAreaMaterial;
        
        [Header("Steampunk Decoration Prefabs")]
        [SerializeField] private GameObject gearPrefab;
        [SerializeField] private GameObject steamPipePrefab;
        [SerializeField] private GameObject chimneySmokeParticles;
        
        [Header("Batching Settings")]
        [SerializeField] private bool enableBatching = true;
        [SerializeField] private bool enableStaticBatching = true;
        [SerializeField] private bool separateColliders = true;
        [SerializeField] private bool batchAreas = true;
        
        [Header("Road Settings")]
        [SerializeField] private float roadHeightOffset = 0.05f;
        [SerializeField] private bool enableRoadColliders = false; // Usually better for gameplay
        [SerializeField] private bool enableFootwayColliders = false;
        
        [Header("Building Generation Settings")]
        [SerializeField] private float buildingHeightMultiplier = 1.0f;
        // TODO: Support per-building height variation based on OSM tags
        [SerializeField] private int collectiblesPerBuilding = 2;
        [SerializeField] private bool enableSteampunkEffects = true;
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private float minimumBuildingSize = 2.0f;
        // TODO: Implement polygonal building generation using OSM building outlines
        // [SerializeField] private bool enablePolygonalBuildings = true;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxBuildingsPerFrame = 5;
        [SerializeField] private int maxRoadSegmentsPerFrame = 10;
        
        // Generation state
        private Transform mapContainer;
        private OSMMapData currentMapData;
        private Vector3 playerSpawnPosition;
        private bool isGenerating = false;
        
        // Events
        public event System.Action<OSMMapData> OnMapGenerationStarted;
        public event System.Action<Vector3> OnPlayerSpawnPositionSet;
        public event System.Action OnMapGenerationCompleted;
        public event System.Action<string> OnGenerationError;
        
        // Material categories
        private enum RoadCategory { Motorway, Primary, Secondary, Residential, Footway, Default }
        private enum BuildingCategory { Residential, Industrial, Commercial, Office, Default }
        private enum AreaCategory { Park, Water, Forest, Grass, Default }

        // Batching Collections - organized by material for efficient combining
        private Dictionary<RoadCategory, List<CombineInstance>> roadMeshesByMaterial = new();
        private Dictionary<BuildingCategory, List<CombineInstance>> buildingMeshesByMaterial = new();
        private Dictionary<AreaCategory, List<CombineInstance>> areaMeshesByMaterial = new();
        
        // Separate collider collections for non-batched physics
        private List<ColliderData> roadColliders = new List<ColliderData>();
        private List<ColliderData> buildingColliders = new List<ColliderData>();
        
        // Container references for batched objects
        private Transform batchedRoadsContainer;
        private Transform batchedBuildingsContainer;
        private Transform batchedAreasContainer;
        private Transform collidersContainer;
        
        private void Awake()
        {
            CreateMapContainer();
            InitializeBatchingCollections();
        }
        
        /// <summary>
        /// Initialize collections for mesh batching by material type
        /// </summary>
        private void InitializeBatchingCollections()
        {
            // Road collections by material type
            roadMeshesByMaterial.Clear();
            foreach (RoadCategory cat in System.Enum.GetValues(typeof(RoadCategory)))
                roadMeshesByMaterial[cat] = new List<CombineInstance>();

            // Building collections by material type
            buildingMeshesByMaterial.Clear();
            foreach (BuildingCategory cat in System.Enum.GetValues(typeof(BuildingCategory)))
                buildingMeshesByMaterial[cat] = new List<CombineInstance>();

            // Area collections by material type
            if (batchAreas)
            {
                areaMeshesByMaterial.Clear();
                foreach (AreaCategory cat in System.Enum.GetValues(typeof(AreaCategory)))
                    areaMeshesByMaterial[cat] = new List<CombineInstance>();
            }
            
            // Clear collider collections
            roadColliders.Clear();
            buildingColliders.Clear();
        }
        
        /// <summary>
        /// Generate Unity world from OSM map data with performance optimization
        /// </summary>
        public void GenerateMap(OSMMapData mapData)
        {
            if (isGenerating)
            {
                Debug.LogWarning("[MapGeneratorBatched] Generation already in progress");
                return;
            }
            
            if (mapData == null || !mapData.IsValid())
            {
                OnGenerationError?.Invoke("Invalid map data provided");
                return;
            }
            
            currentMapData = mapData;
            StartCoroutine(GenerateMapCoroutine());
        }
        
        /// <summary>
        /// Clear existing map and generate new one
        /// </summary>
        public void RegenerateMap(OSMMapData mapData)
        {
            ClearExistingMap();
            GenerateMap(mapData);
        }
        
        /// <summary>
        /// Main generation coroutine with batching optimization
        /// </summary>
        private IEnumerator GenerateMapCoroutine()
        {
            isGenerating = true;
            OnMapGenerationStarted?.Invoke(currentMapData);
            
            Debug.Log("[MapGeneratorBatched] Starting batched map generation...");
            
            bool hasError = false;
            string errorMessage = "";
            
            try
            {
                // Clear existing content and prepare containers
                ClearExistingMap();
                CreateBatchingContainers();
                InitializeBatchingCollections();
            }
            catch (System.Exception e)
            {
                hasError = true;
                errorMessage = $"Failed to clear existing map: {e.Message}";
            }
            
            if (!hasError)
            {
                yield return null;
                
                // Generate ground plane
                yield return StartCoroutine(GenerateGroundPlane());
                
                // Generate road meshes (collect, don't instantiate yet)
                yield return StartCoroutine(CollectRoadMeshes());
                
                // Generate building meshes (collect, don't instantiate yet) 
                yield return StartCoroutine(CollectBuildingMeshes());
                
                // Generate area meshes (collect, don't instantiate yet)
                if (batchAreas)
                {
                    yield return StartCoroutine(CollectAreaMeshes());
                }
                else
                {
                    yield return StartCoroutine(GenerateAreas());
                }
                
                // Apply batching - combine all collected meshes
                if (enableBatching)
                {
                    yield return StartCoroutine(ApplyMeshBatching());
                }
                
                // Create separate colliders if needed
                if (separateColliders)
                {
                    yield return StartCoroutine(CreateSeparateColliders());
                }
                
                // Place dynamic objects (non-batched)
                yield return StartCoroutine(PlaceCollectibles());
                yield return StartCoroutine(PlaceGoalZone());
                
                // Set player spawn position
                SetPlayerSpawnPosition();
                
                // Add Steampunk atmosphere
                if (enableSteampunkEffects)
                {
                    yield return StartCoroutine(AddSteampunkAtmosphere());
                }
                
                Debug.Log("[MapGeneratorBatched] Batched map generation completed successfully");
                LogPerformanceStats();
                OnMapGenerationCompleted?.Invoke();
            }
            else
            {
                Debug.LogError($"[MapGeneratorBatched] Generation failed: {errorMessage}");
                OnGenerationError?.Invoke($"Generation failed: {errorMessage}");
            }
            
            isGenerating = false;
        }
        
        /// <summary>
        /// Create container objects for organized batching
        /// </summary>
        private void CreateBatchingContainers()
        {
            // Create main containers
            GameObject batchedRoadsGO = new GameObject("BatchedRoads");
            batchedRoadsContainer = batchedRoadsGO.transform;
            batchedRoadsContainer.SetParent(mapContainer);
            
            GameObject batchedBuildingsGO = new GameObject("BatchedBuildings");
            batchedBuildingsContainer = batchedBuildingsGO.transform;
            batchedBuildingsContainer.SetParent(mapContainer);
            
            if (batchAreas)
            {
                GameObject batchedAreasGO = new GameObject("BatchedAreas");
                batchedAreasContainer = batchedAreasGO.transform;
                batchedAreasContainer.SetParent(mapContainer);
            }
            
            if (separateColliders)
            {
                GameObject collidersGO = new GameObject("Colliders");
                collidersContainer = collidersGO.transform;
                collidersContainer.SetParent(mapContainer);
            }
        }
        
        /// <summary>
        /// Collect road meshes for batching instead of creating individual GameObjects
        /// </summary>
        private IEnumerator CollectRoadMeshes()
        {
            Debug.Log($"[MapGeneratorBatched] Collecting {currentMapData.roads.Count} roads for batching...");
            
            int processedSegments = 0;
            
            for (int roadIndex = 0; roadIndex < currentMapData.roads.Count; roadIndex++)
            {
                OSMWay road = currentMapData.roads[roadIndex];
                string highwayType = GetHighwayType(road);
                
                // Process each segment of the road
                for (int nodeIndex = 0; nodeIndex < road.nodes.Count - 1; nodeIndex++)
                {
                    OSMNode startNode = road.nodes[nodeIndex];
                    OSMNode endNode = road.nodes[nodeIndex + 1];
                    
                    // Create mesh data for this segment
                    var segmentMesh = CreateRoadSegmentMesh(startNode, endNode, highwayType, roadIndex, nodeIndex);
                    if (segmentMesh.HasValue)
                    {
                        // Add to appropriate material collection
                        RoadCategory materialKey = GetRoadMaterialKey(highwayType);
                        roadMeshesByMaterial[materialKey].Add(segmentMesh.Value);
                        
                        // Add collider data if needed
                        if ((enableRoadColliders && highwayType != "footway") || 
                            (enableFootwayColliders && highwayType == "footway"))
                        {
                            roadColliders.Add(CreateColliderData(segmentMesh.Value, "Road"));
                        }
                    }
                    
                    processedSegments++;
                    
                    // Yield periodically to prevent frame drops
                    if (processedSegments >= maxRoadSegmentsPerFrame)
                    {
                        processedSegments = 0;
                        yield return null;
                    }
                }
            }
            
            Debug.Log($"[MapGeneratorBatched] Collected {GetTotalMeshCount(roadMeshesByMaterial)} road meshes for batching");
            yield return null;
        }
        
        /// <summary>
        /// Create mesh data for a single road segment
        /// </summary>
        private CombineInstance? CreateRoadSegmentMesh(OSMNode startNode, OSMNode endNode, string highwayType, int roadIndex, int segmentIndex)
        {
            Vector3 startPos = currentMapData.LatLonToWorldPosition(startNode.lat, startNode.lon);
            Vector3 endPos = currentMapData.LatLonToWorldPosition(endNode.lat, endNode.lon);
            
            float segmentLength = Vector3.Distance(startPos, endPos);
            if (segmentLength < 0.1f) return null; // Skip very short segments
            
            startPos.y = roadHeightOffset;
            endPos.y = roadHeightOffset;
            
            float segmentWidth = GetRoadWidth(highwayType);
            
            // Use optimized mesh utilities
            return MeshUtilities.CreateRoadSegmentMesh(startPos, endPos, segmentWidth, 0.1f);
        }
        
        /// <summary>
        /// Collect building meshes for batching
        /// </summary>
        private IEnumerator CollectBuildingMeshes()
        {
            Debug.Log($"[MapGeneratorBatched] Collecting {currentMapData.buildings.Count} buildings for batching...");
            
            int processed = 0;
            for (int i = 0; i < currentMapData.buildings.Count; i++)
            {
                OSMBuilding building = currentMapData.buildings[i];
                building.CalculateHeight();
                
                var buildingMesh = CreateBuildingMesh(building);
                if (buildingMesh.HasValue)
                {
                    // Add to appropriate material collection
                    BuildingCategory materialKey = GetBuildingMaterialKey(building.buildingType);
                    buildingMeshesByMaterial[materialKey].Add(buildingMesh.Value);
                    
                    // Add collider data - buildings usually need colliders
                    buildingColliders.Add(CreateColliderData(buildingMesh.Value, "Building"));
                }
                
                processed++;
                if (processed >= maxBuildingsPerFrame)
                {
                    processed = 0;
                    yield return null;
                }
            }
            
            Debug.Log($"[MapGeneratorBatched] Collected {GetTotalMeshCount(buildingMeshesByMaterial)} building meshes for batching");
            yield return null;
        }
        
        /// <summary>
        /// Create mesh data for a building
        /// </summary>
        private CombineInstance? CreateBuildingMesh(OSMBuilding building)
        {
            if (building.nodes.Count < 3) return null;
            
            // Calculate building properties
            Vector3 center = CalculateBuildingCenter(building);
            Vector3 size = CalculateBuildingSize(building);
            Quaternion rotation = CalculateBuildingRotation(building);
            
            // Ensure minimum size
            size.x = Mathf.Max(size.x, minimumBuildingSize);
            size.z = Mathf.Max(size.z, minimumBuildingSize);
            size.y = building.height * buildingHeightMultiplier;
            
            // Use optimized mesh utilities
            return MeshUtilities.CreateBuildingMesh(center, size, rotation);
        }
        
        /// <summary>
        /// Collect area meshes for batching (if enabled)
        /// </summary>
        private IEnumerator CollectAreaMeshes()
        {
            Debug.Log($"[MapGeneratorBatched] Collecting {currentMapData.areas.Count} areas for batching...");
            
            for (int i = 0; i < currentMapData.areas.Count; i++)
            {
                OSMArea area = currentMapData.areas[i];
                if (area.nodes.Count < 3) continue;
                
                area.DetermineAreaType();
                
                var areaMesh = CreateAreaMesh(area);
                if (areaMesh.HasValue)
                {
                    AreaCategory materialKey = GetAreaMaterialKey(area.areaType);
                    areaMeshesByMaterial[materialKey].Add(areaMesh.Value);
                }
                
                if (i % 3 == 0)
                    yield return null;
            }
            
            Debug.Log($"[MapGeneratorBatched] Collected {GetTotalMeshCount(areaMeshesByMaterial)} area meshes for batching");
            yield return null;
        }
        
        /// <summary>
        /// Create mesh data for an area
        /// </summary>
        private CombineInstance? CreateAreaMesh(OSMArea area)
        {
            // Calculate area center and size
            Vector3 center = CalculateAreaCenter(area);
            Vector3 size = CalculateAreaSize(area);
            
            size.y = 0.02f; // Thin plane
            
            // Use optimized mesh utilities
            return MeshUtilities.CreateAreaMesh(center, size);
        }
        
        /// <summary>
        /// Apply mesh batching - combine all collected meshes by material
        /// </summary>
        private IEnumerator ApplyMeshBatching()
        {
            Debug.Log("[MapGeneratorBatched] Applying mesh batching...");
            // TODO: Consider performing batching in a job to avoid frame hitches
            
            // Batch roads by material
            yield return StartCoroutine(BatchMeshesByMaterial(roadMeshesByMaterial, batchedRoadsContainer, GetRoadMaterial, "AllRoads"));
            
            // Batch buildings by material  
            yield return StartCoroutine(BatchMeshesByMaterial(buildingMeshesByMaterial, batchedBuildingsContainer, GetBuildingMaterial, "AllBuildings"));
            
            // Batch areas by material (if enabled)
            if (batchAreas && batchedAreasContainer != null)
            {
                yield return StartCoroutine(BatchMeshesByMaterial(areaMeshesByMaterial, batchedAreasContainer, GetAreaMaterial, "AllAreas"));
            }
            
            Debug.Log("[MapGeneratorBatched] Mesh batching completed");
            yield return null;
        }
        
        /// <summary>
        /// Generic method to batch meshes by material type
        /// </summary>
        private IEnumerator BatchMeshesByMaterial<TKey>(
            Dictionary<TKey, List<CombineInstance>> meshCollections,
            Transform container,
            System.Func<TKey, Material> getMaterial,
            string namePrefix)
        {
            foreach (var kvp in meshCollections)
            {
                TKey materialKey = kvp.Key;
                List<CombineInstance> meshes = kvp.Value;
                
                if (meshes.Count == 0) continue;
                
                // Create combined mesh
                Mesh combinedMesh = new Mesh();
                combinedMesh.name = $"{namePrefix}_{materialKey}_Combined";
                
                try
                {
                    combinedMesh.CombineMeshes(meshes.ToArray(), true, true);
                    combinedMesh.RecalculateNormals();
                    combinedMesh.RecalculateBounds();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MapGeneratorBatched] Failed to combine meshes for {materialKey}: {e.Message}");
                    continue;
                }
                
                // Create GameObject for batched mesh
                GameObject batchedObject = new GameObject($"{namePrefix}_{materialKey}");
                batchedObject.transform.SetParent(container);
                
                // Add components
                MeshFilter meshFilter = batchedObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = batchedObject.AddComponent<MeshRenderer>();
                
                meshFilter.mesh = combinedMesh;
                meshRenderer.material = getMaterial(materialKey);
                
                // Mark as static for additional Unity batching
                if (enableStaticBatching)
                {
                    batchedObject.isStatic = true;
                }
                
                Debug.Log($"[MapGeneratorBatched] Batched {meshes.Count} {materialKey} meshes into single object");
                
                yield return null; // Yield after each material to spread the work
            }
        }
        
        /// <summary>
        /// Create separate colliders for physics interactions
        /// </summary>
        private IEnumerator CreateSeparateColliders()
        {
            Debug.Log("[MapGeneratorBatched] Creating separate colliders...");

            // TODO: Reuse collider objects via pooling to reduce GC pressure

            int colliderCount = 0;
            
            // Create road colliders
            foreach (var colliderData in roadColliders)
            {
                CreateColliderObject(colliderData, collidersContainer);
                colliderCount++;
                
                if (colliderCount % 10 == 0)
                    yield return null;
            }
            
            // Create building colliders
            foreach (var colliderData in buildingColliders)
            {
                CreateColliderObject(colliderData, collidersContainer);
                colliderCount++;
                
                if (colliderCount % 5 == 0)
                    yield return null;
            }
            
            Debug.Log($"[MapGeneratorBatched] Created {colliderCount} separate colliders");
            yield return null;
        }
        
        /// <summary>
        /// Create individual collider GameObject from collider data
        /// </summary>
        private void CreateColliderObject(ColliderData data, Transform parent)
        {
            GameObject colliderObj = new GameObject($"{data.name}_Collider");
            colliderObj.transform.SetParent(parent);
            
            // Apply transform from the original mesh
            colliderObj.transform.position = data.position;
            colliderObj.transform.rotation = data.rotation;
            colliderObj.transform.localScale = data.scale;
            
            // Add appropriate collider type
            if (data.mesh != null)
            {
                MeshCollider meshCollider = colliderObj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = data.mesh;
            }
            else
            {
                // Use BoxCollider for simple shapes
                BoxCollider boxCollider = colliderObj.AddComponent<BoxCollider>();
            }
            
            // Mark as static for performance
            if (enableStaticBatching)
            {
                colliderObj.isStatic = true;
            }
        }
        
        // Helper Methods
        
        // Removed: Using MeshUtilities instead of temporary GameObject creation
        
        private ColliderData CreateColliderData(CombineInstance combineInstance, string name)
        {
            // Extract transform data from the CombineInstance matrix
            Matrix4x4 matrix = combineInstance.transform;
            
            return new ColliderData
            {
                name = name,
                mesh = combineInstance.mesh,
                position = matrix.GetColumn(3), // Position from matrix
                rotation = matrix.rotation,
                scale = matrix.lossyScale
            };
        }
        
        private string GetHighwayType(OSMWay road)
        {
            if (road.tags.ContainsKey("highway"))
            {
                string highway = road.tags["highway"];
                return highway switch
                {
                    "motorway" or "trunk" => "motorway",
                    "primary" or "primary_link" => "primary", 
                    "secondary" or "secondary_link" => "secondary",
                    "residential" or "tertiary" or "unclassified" => "residential",
                    "footway" or "cycleway" or "path" or "pedestrian" => "footway",
                    _ => "default"
                };
            }
            return "default";
        }
        
        private float GetRoadWidth(string highwayType)
        {
            return highwayType switch
            {
                "motorway" => 5.5f,
                "primary" => 4.0f,
                "secondary" => 3.0f,
                "residential" => 2.0f,
                "footway" => 1.0f,
                _ => 2.0f
            };
        }
        
        private RoadCategory GetRoadMaterialKey(string highwayType)
        {
            return highwayType switch
            {
                "motorway" => RoadCategory.Motorway,
                "primary" => RoadCategory.Primary,
                "secondary" => RoadCategory.Secondary,
                "residential" => RoadCategory.Residential,
                "footway" => RoadCategory.Footway,
                _ => RoadCategory.Default
            };
        }

        private BuildingCategory GetBuildingMaterialKey(string buildingType)
        {
            return buildingType switch
            {
                "industrial" => BuildingCategory.Industrial,
                "commercial" => BuildingCategory.Commercial,
                "office" => BuildingCategory.Office,
                "residential" => BuildingCategory.Residential,
                _ => BuildingCategory.Default
            };
        }

        private AreaCategory GetAreaMaterialKey(string areaType)
        {
            return areaType switch
            {
                "park" => AreaCategory.Park,
                "water" => AreaCategory.Water,
                "forest" => AreaCategory.Forest,
                "grass" => AreaCategory.Grass,
                _ => AreaCategory.Default
            };
        }
        
        private Material GetRoadMaterial(RoadCategory materialKey)
        {
            return materialKey switch
            {
                RoadCategory.Motorway => roadMotorway ?? roadDefault,
                RoadCategory.Primary => roadPrimary ?? roadDefault,
                RoadCategory.Secondary => roadSecondary ?? roadDefault,
                RoadCategory.Residential => roadResidential ?? roadDefault,
                RoadCategory.Footway => roadFootway ?? roadDefault,
                _ => roadDefault
            };
        }

        private Material GetBuildingMaterial(BuildingCategory materialKey)
        {
            return materialKey switch
            {
                BuildingCategory.Residential => residentialMaterial ?? defaultBuildingMaterial,
                BuildingCategory.Industrial => industrialMaterial ?? defaultBuildingMaterial,
                BuildingCategory.Commercial => commercialMaterial ?? defaultBuildingMaterial,
                BuildingCategory.Office => officeMaterial ?? defaultBuildingMaterial,
                _ => defaultBuildingMaterial
            };
        }

        private Material GetAreaMaterial(AreaCategory materialKey)
        {
            return materialKey switch
            {
                AreaCategory.Park => parkMaterial ?? defaultAreaMaterial,
                AreaCategory.Water => waterMaterial ?? defaultAreaMaterial,
                AreaCategory.Forest => forestMaterial ?? defaultAreaMaterial,
                AreaCategory.Grass => grassMaterial ?? defaultAreaMaterial,
                _ => defaultAreaMaterial
            };
        }
        
        private Vector3 CalculateBuildingCenter(OSMBuilding building)
        {
            Vector3 center = Vector3.zero;
            foreach (OSMNode node in building.nodes)
            {
                center += currentMapData.LatLonToWorldPosition(node.lat, node.lon);
            }
            return center / building.nodes.Count;
        }
        
        private Vector3 CalculateBuildingSize(OSMBuilding building)
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            
            foreach (OSMNode node in building.nodes)
            {
                Vector3 worldPos = currentMapData.LatLonToWorldPosition(node.lat, node.lon);
                min = Vector3.Min(min, worldPos);
                max = Vector3.Max(max, worldPos);
            }
            
            return max - min;
        }
        
        private Quaternion CalculateBuildingRotation(OSMBuilding building)
        {
            if (building.nodes.Count >= 2)
            {
                Vector3 firstPos = currentMapData.LatLonToWorldPosition(building.nodes[0].lat, building.nodes[0].lon);
                Vector3 secondPos = currentMapData.LatLonToWorldPosition(building.nodes[1].lat, building.nodes[1].lon);
                Vector3 direction = (secondPos - firstPos).normalized;
                
                if (direction.magnitude > 0.1f)
                {
                    float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    return Quaternion.Euler(0, angle, 0);
                }
            }
            return Quaternion.identity;
        }
        
        private Vector3 CalculateAreaCenter(OSMArea area)
        {
            Vector3 center = Vector3.zero;
            int nodesToProcess = area.IsClosed() ? area.nodes.Count - 1 : area.nodes.Count;
            
            for (int i = 0; i < nodesToProcess; i++)
            {
                center += currentMapData.LatLonToWorldPosition(area.nodes[i].lat, area.nodes[i].lon);
            }
            return center / nodesToProcess;
        }
        
        private Vector3 CalculateAreaSize(OSMArea area)
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            
            int nodesToProcess = area.IsClosed() ? area.nodes.Count - 1 : area.nodes.Count;
            for (int i = 0; i < nodesToProcess; i++)
            {
                Vector3 worldPos = currentMapData.LatLonToWorldPosition(area.nodes[i].lat, area.nodes[i].lon);
                min = Vector3.Min(min, worldPos);
                max = Vector3.Max(max, worldPos);
            }
            
            Vector3 size = max - min;
            size.x = Mathf.Max(size.x, 2f);
            size.z = Mathf.Max(size.z, 2f);
            
            return size;
        }
        
        private int GetTotalMeshCount<TKey>(Dictionary<TKey, List<CombineInstance>> collections)
        {
            return collections.Values.Sum(list => list.Count);
        }
        
        private void LogPerformanceStats()
        {
            int totalRoadMeshes = GetTotalMeshCount(roadMeshesByMaterial);
            int totalBuildingMeshes = GetTotalMeshCount(buildingMeshesByMaterial);
            int totalAreaMeshes = batchAreas ? GetTotalMeshCount(areaMeshesByMaterial) : 0;
            
            int batchedRoadObjects = roadMeshesByMaterial.Count(kvp => kvp.Value.Count > 0);
            int batchedBuildingObjects = buildingMeshesByMaterial.Count(kvp => kvp.Value.Count > 0);
            int batchedAreaObjects = batchAreas ? areaMeshesByMaterial.Count(kvp => kvp.Value.Count > 0) : 0;
            
            Debug.Log($"[MapGeneratorBatched] Performance Stats:\n" +
                     $"Roads: {totalRoadMeshes} meshes → {batchedRoadObjects} batched objects\n" +
                     $"Buildings: {totalBuildingMeshes} meshes → {batchedBuildingObjects} batched objects\n" +
                     $"Areas: {totalAreaMeshes} meshes → {batchedAreaObjects} batched objects\n" +
                     $"Separate Colliders: {roadColliders.Count + buildingColliders.Count}\n" +
                     $"Draw Call Reduction: ~{(totalRoadMeshes + totalBuildingMeshes + totalAreaMeshes) - (batchedRoadObjects + batchedBuildingObjects + batchedAreaObjects)} fewer draw calls");
        }
        
        // Standard generation methods (simplified versions for non-batched objects)
        
        private IEnumerator GenerateGroundPlane()
        {
            Debug.Log("[MapGeneratorBatched] Generating ground plane...");
            
            Vector3 mapCenter = currentMapData.GetWorldCenter();
            float mapSize = Mathf.Max(
                (float)(currentMapData.bounds.GetWidth() * currentMapData.scaleMultiplier),
                (float)(currentMapData.bounds.GetHeight() * currentMapData.scaleMultiplier)
            );
            
            GameObject groundPlane = CreateGroundPlane(mapCenter, mapSize);
            groundPlane.transform.SetParent(mapContainer);
            
            if (enableStaticBatching)
            {
                groundPlane.isStatic = true;
            }
            
            yield return null;
        }
        
        private GameObject CreateGroundPlane(Vector3 center, float size)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = center;
            ground.transform.localScale = Vector3.one * (size / 10f);
            ground.name = "GroundPlane";
            ground.layer = groundLayer;
            return ground;
        }
        
        private IEnumerator GenerateAreas()
        {
            // Non-batched area generation (fallback when batching is disabled)
            Debug.Log($"[MapGeneratorBatched] Generating {currentMapData.areas.Count} areas (non-batched)...");
            
            Transform areaContainer = new GameObject("Areas").transform;
            areaContainer.SetParent(mapContainer);
            
            for (int i = 0; i < currentMapData.areas.Count; i++)
            {
                OSMArea area = currentMapData.areas[i];
                area.DetermineAreaType();
                
                Vector3 center = CalculateAreaCenter(area);
                Vector3 size = CalculateAreaSize(area);
                center.y = 0.01f;
                size.y = 0.02f;
                
                GameObject areaObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                areaObject.transform.position = center;
                areaObject.transform.localScale = size;
                areaObject.transform.SetParent(areaContainer);
                areaObject.name = $"Area_{area.areaType}_{area.id}";
                
                MeshRenderer renderer = areaObject.GetComponent<MeshRenderer>();
                renderer.material = GetAreaMaterial(area.areaType);
                
                if (enableStaticBatching)
                {
                    areaObject.isStatic = true;
                }
                
                if (i % 3 == 0)
                    yield return null;
            }
            
            yield return null;
        }
        
        private IEnumerator PlaceCollectibles()
        {
            if (collectiblePrefab == null)
            {
                Debug.LogWarning("[MapGeneratorBatched] No collectible prefab assigned");
                yield break;
            }
            
            Debug.Log("[MapGeneratorBatched] Placing collectibles...");
            
            Transform collectibleContainer = new GameObject("Collectibles").transform;
            collectibleContainer.SetParent(mapContainer);
            
            List<Vector3> collectiblePositions = GenerateCollectiblePositions();
            
            for (int i = 0; i < collectiblePositions.Count; i++)
            {
                GameObject collectible = Instantiate(collectiblePrefab, collectiblePositions[i], Quaternion.identity);
                collectible.transform.SetParent(collectibleContainer);
                collectible.name = $"Collectible_{i}";
                
                if (i % 5 == 0)
                    yield return null;
            }
            
            // Update UI if LevelManager exists
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.UpdateCollectibleCount();
            }
            
            yield return null;
        }
        
        private List<Vector3> GenerateCollectiblePositions()
        {
            List<Vector3> positions = new List<Vector3>();
            
            // Place collectibles near buildings
            foreach (OSMBuilding building in currentMapData.buildings)
            {
                for (int i = 0; i < collectiblesPerBuilding; i++)
                {
                    if (building.nodes.Count > 0)
                    {
                        OSMNode randomNode = building.nodes[Random.Range(0, building.nodes.Count)];
                        Vector3 buildingPos = currentMapData.LatLonToWorldPosition(randomNode.lat, randomNode.lon);
                        
                        Vector3 offset = new Vector3(
                            Random.Range(-5f, 5f),
                            1f,
                            Random.Range(-5f, 5f)
                        );
                        
                        positions.Add(buildingPos + offset);
                    }
                }
            }
            
            return positions;
        }
        
        private IEnumerator PlaceGoalZone()
        {
            if (goalZonePrefab == null)
            {
                Debug.LogWarning("[MapGeneratorBatched] No goal zone prefab assigned");
                yield break;
            }
            
            Debug.Log("[MapGeneratorBatched] Placing goal zone...");
            
            Vector3 goalPosition = FindOptimalGoalPosition();
            GameObject goalZone = Instantiate(goalZonePrefab, goalPosition, Quaternion.identity);
            goalZone.transform.SetParent(mapContainer);
            goalZone.name = "GoalZone";
            
            yield return null;
        }
        
        private Vector3 FindOptimalGoalPosition()
        {
            Vector3 goalPos = currentMapData.GetWorldCenter();
            
            if (currentMapData.buildings.Count > 0)
            {
                OSMBuilding largestBuilding = currentMapData.buildings[0];
                Vector3 largestCenter = CalculateBuildingCenter(largestBuilding);
                Vector3 largestSize = CalculateBuildingSize(largestBuilding);
                float largestArea = largestSize.x * largestSize.z;
                
                foreach (OSMBuilding building in currentMapData.buildings)
                {
                    Vector3 buildingSize = CalculateBuildingSize(building);
                    float area = buildingSize.x * buildingSize.z;
                    if (area > largestArea)
                    {
                        largestArea = area;
                        largestBuilding = building;
                        largestCenter = CalculateBuildingCenter(building);
                    }
                }
                
                goalPos = largestCenter;
            }
            
            goalPos.y = 0.5f;
            return goalPos;
        }
        
        private void SetPlayerSpawnPosition()
        {
            playerSpawnPosition = currentMapData.GetWorldCenter();
            
            if (currentMapData.roads.Count > 0)
            {
                OSMWay firstRoad = currentMapData.roads[0];
                if (firstRoad.nodes.Count > 0)
                {
                    playerSpawnPosition = currentMapData.LatLonToWorldPosition(firstRoad.nodes[0].lat, firstRoad.nodes[0].lon);
                }
            }
            
            playerSpawnPosition.y = 1f;
            OnPlayerSpawnPositionSet?.Invoke(playerSpawnPosition);
            
            if (playerPrefab != null)
            {
                GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
                if (existingPlayer != null)
                {
                    existingPlayer.transform.position = playerSpawnPosition;
                }
                else
                {
                    GameObject player = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
                    player.name = "Player";
                }
            }
        }
        
        private IEnumerator AddSteampunkAtmosphere()
        {
            Debug.Log("[MapGeneratorBatched] Adding Steampunk atmosphere...");
            
            RenderSettings.ambientLight = new Color(0.4f, 0.35f, 0.25f);
            RenderSettings.fogColor = new Color(0.5f, 0.4f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 80f;
            
            yield return null;
        }
        
        private void CreateMapContainer()
        {
            if (mapContainer == null)
            {
                GameObject containerGO = new GameObject("GeneratedMapBatched");
                mapContainer = containerGO.transform;
            }
        }
        
        private void ClearExistingMap()
        {
            if (mapContainer != null)
            {
                foreach (Transform child in mapContainer)
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }
            
            // Clear mesh utilities cache if needed
            MeshUtilities.ClearCache();
            
            InitializeBatchingCollections();
        }
        
        // Public Interface
        
        public Vector3 GetPlayerSpawnPosition() => playerSpawnPosition;
        public OSMMapData GetCurrentMapData() => currentMapData;
        public bool IsGenerating() => isGenerating;
        
        public string GetMapStatistics()
        {
            if (currentMapData == null)
                return "No map data available";
                
            int totalRoadMeshes = GetTotalMeshCount(roadMeshesByMaterial);
            int totalBuildingMeshes = GetTotalMeshCount(buildingMeshesByMaterial);
            int batchedObjects = roadMeshesByMaterial.Count(kvp => kvp.Value.Count > 0) + 
                               buildingMeshesByMaterial.Count(kvp => kvp.Value.Count > 0);
            
            return $"Batched Map Statistics:\n" +
                   $"- Roads: {currentMapData.roads.Count} ({totalRoadMeshes} segments)\n" +
                   $"- Buildings: {currentMapData.buildings.Count} ({totalBuildingMeshes} meshes)\n" +
                   $"- Areas: {currentMapData.areas.Count}\n" +
                   $"- POIs: {currentMapData.pointsOfInterest.Count}\n" +
                   $"- Batched Objects: {batchedObjects}\n" +
                   $"- Separate Colliders: {roadColliders.Count + buildingColliders.Count}\n" +
                   $"- Scale: {currentMapData.scaleMultiplier:F2}";
        }
    }
    
    /// <summary>
    /// Data structure for storing collider information separately from visual meshes
    /// </summary>
    [System.Serializable]
    public struct ColliderData
    {
        public string name;
        public Mesh mesh;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
}
