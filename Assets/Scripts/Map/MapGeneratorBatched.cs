using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using RollABall.Utility;

namespace RollABall.Map
{
    /// <summary>
    /// Performance-optimized OSM map generator with mesh batching.
    /// Combines roads, buildings, and areas by material to reduce draw calls.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Map Generator Batched")]
    public class MapGeneratorBatched : MonoBehaviour
    {
        [Header("Prefab Configuration")]
        [SerializeField] private MapPrefabsConfig prefabsConfig;

        [Header("Generation Prefabs")]
        [SerializeField, HideInInspector] private GameObject roadPrefab;
        [SerializeField, HideInInspector] private GameObject buildingPrefab;
        [SerializeField, HideInInspector] private GameObject areaPrefab;
        [SerializeField, HideInInspector] private GameObject collectiblePrefab;
        [SerializeField, HideInInspector] private GameObject goalZonePrefab;
        [SerializeField, HideInInspector] private GameObject playerPrefab;

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
        [SerializeField, HideInInspector] private GameObject gearPrefab;
        [SerializeField, HideInInspector] private GameObject steamPipePrefab;
        [SerializeField, HideInInspector] private GameObject chimneySmokeParticles;

        [Header("Batching Settings")]
        [SerializeField] private bool enableBatching = true;
        [SerializeField] private bool enableStaticBatching = true;
        [SerializeField] private bool separateColliders = true;
        [SerializeField] private bool batchAreas = true;

        [Header("Road Settings")]
        [SerializeField] private float roadHeightOffset = 0.05f;
        [SerializeField] private bool enableRoadColliders = false;
        [SerializeField] private bool enableFootwayColliders = false;

        [Header("Building Generation Settings")]
        [SerializeField] private float buildingHeightMultiplier = 1.0f;
        [SerializeField] private int collectiblesPerBuilding = 2;
        [SerializeField] private bool enableSteampunkEffects = true;
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private float minimumBuildingSize = 2.0f;

        [Header("Performance Settings")]
        [SerializeField] private int maxBuildingsPerFrame = 5;
        [SerializeField] private int maxRoadSegmentsPerFrame = 10;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // Generation state
        private Transform mapContainer;
        private OSMMapData currentMapData;
        private Vector3 playerSpawnPosition;
        private bool isGenerating;

        // Events
        public event System.Action<OSMMapData> OnMapGenerationStarted;
        public event System.Action<Vector3> OnPlayerSpawnPositionSet;
        public event System.Action OnMapGenerationCompleted;
        public event System.Action<string> OnGenerationError;

        // Material categories
        private enum RoadCategory { Motorway, Primary, Secondary, Residential, Footway, Default }
        private enum BuildingCategory { Residential, Industrial, Commercial, Office, Default }
        private enum AreaCategory { Park, Water, Forest, Grass, Default }

        // Mesh collections for batching
        private Dictionary<RoadCategory, List<CombineInstance>> roadMeshesByMaterial = new();
        private Dictionary<BuildingCategory, List<CombineInstance>> buildingMeshesByMaterial = new();
        private Dictionary<AreaCategory, List<CombineInstance>> areaMeshesByMaterial = new();

        // Colliders
        private List<ColliderData> roadColliders = new();
        private List<ColliderData> buildingColliders = new();

        // Containers
        private Transform batchedRoadsContainer;
        private Transform batchedBuildingsContainer;
        private Transform batchedAreasContainer;
        private Transform collidersContainer;

        private void Awake()
        {
            ApplyPrefabConfig();
            CreateMapContainer();
            InitializeBatchingCollections();
        }

        private void ApplyPrefabConfig()
        {
            if (!prefabsConfig) return;

            roadPrefab = prefabsConfig.roadPrefab;
            buildingPrefab = prefabsConfig.buildingPrefab;
            areaPrefab = prefabsConfig.areaPrefab;
            collectiblePrefab = prefabsConfig.collectiblePrefab;
            goalZonePrefab = prefabsConfig.goalZonePrefab;
            playerPrefab = prefabsConfig.playerPrefab;

            gearPrefab = prefabsConfig.gearPrefab;
            steamPipePrefab = prefabsConfig.steamPipePrefab;
            chimneySmokeParticles = prefabsConfig.chimneySmokeParticles;
        }

        private void InitializeBatchingCollections()
        {
            roadMeshesByMaterial.Clear();
            foreach (RoadCategory cat in System.Enum.GetValues(typeof(RoadCategory)))
                roadMeshesByMaterial[cat] = new List<CombineInstance>();

            buildingMeshesByMaterial.Clear();
            foreach (BuildingCategory cat in System.Enum.GetValues(typeof(BuildingCategory)))
                buildingMeshesByMaterial[cat] = new List<CombineInstance>();

            if (batchAreas)
            {
                areaMeshesByMaterial.Clear();
                foreach (AreaCategory cat in System.Enum.GetValues(typeof(AreaCategory)))
                    areaMeshesByMaterial[cat] = new List<CombineInstance>();
            }

            roadColliders.Clear();
            buildingColliders.Clear();
        }

        public void GenerateMap(OSMMapData mapData)
        {
            if (isGenerating) { Debug.LogWarning("[MapGen] Already generating."); return; }
            if (mapData == null || !mapData.IsValid()) { OnGenerationError?.Invoke("Invalid map data"); return; }

            currentMapData = mapData;
            StartCoroutine(GenerateMapCoroutine());
        }

        public void RegenerateMap(OSMMapData mapData)
        {
            ClearExistingMap();
            GenerateMap(mapData);
        }

        private IEnumerator GenerateMapCoroutine()
        {
            isGenerating = true;
            OnMapGenerationStarted?.Invoke(currentMapData);

            Log("Starting batched map generation");

            try
            {
                ClearExistingMap();
                CreateBatchingContainers();
                InitializeBatchingCollections();
            }
            catch (System.Exception e)
            {
                FailGeneration($"Failed to clear map: {e.Message}");
                yield break;
            }

            yield return GenerateGroundPlane();
            yield return CollectRoadMeshes();
            yield return CollectBuildingMeshes();
            if (batchAreas) yield return CollectAreaMeshes(); else yield return GenerateAreas();

            if (enableBatching) yield return ApplyMeshBatching();
            if (separateColliders) yield return CreateSeparateColliders();

            yield return PlaceCollectibles();
            yield return PlaceGoalZone();

            SetPlayerSpawnPosition();
            if (enableSteampunkEffects) yield return AddSteampunkAtmosphere();

            LogPerformanceStats();
            OnMapGenerationCompleted?.Invoke();
            isGenerating = false;
        }

        private void CreateBatchingContainers()
        {
            batchedRoadsContainer = new GameObject("BatchedRoads").transform;
            batchedRoadsContainer.SetParent(mapContainer);

            batchedBuildingsContainer = new GameObject("BatchedBuildings").transform;
            batchedBuildingsContainer.SetParent(mapContainer);

            if (batchAreas)
            {
                batchedAreasContainer = new GameObject("BatchedAreas").transform;
                batchedAreasContainer.SetParent(mapContainer);
            }

            if (separateColliders)
            {
                collidersContainer = new GameObject("Colliders").transform;
                collidersContainer.SetParent(mapContainer);
            }
        }
        private IEnumerator CollectRoadMeshes()
        {
            Log($"Collecting {currentMapData.roads.Count} roads");
            int processedSegments = 0;

            foreach (var road in currentMapData.roads)
            {
                string highwayType = GetHighwayType(road);

                for (int i = 0; i < road.nodes.Count - 1; i++)
                {
                    var segmentMesh = CreateRoadSegmentMesh(
                        road.nodes[i], road.nodes[i + 1], highwayType);

                    if (segmentMesh.HasValue)
                    {
                        var key = GetRoadMaterialKey(highwayType);
                        roadMeshesByMaterial[key].Add(segmentMesh.Value);

                        if ((enableRoadColliders && highwayType != "footway") ||
                            (enableFootwayColliders && highwayType == "footway"))
                        {
                            roadColliders.Add(CreateColliderData(segmentMesh.Value, "Road"));
                        }
                    }

                    processedSegments++;
                    if (processedSegments >= maxRoadSegmentsPerFrame)
                    {
                        processedSegments = 0;
                        yield return null;
                    }
                }
            }
        }

        private CombineInstance? CreateRoadSegmentMesh(OSMNode start, OSMNode end, string highwayType)
        {
            Vector3 startPos = currentMapData.LatLonToWorldPosition(start.lat, start.lon);
            Vector3 endPos = currentMapData.LatLonToWorldPosition(end.lat, end.lon);
            if (Vector3.Distance(startPos, endPos) < 0.1f) return null;

            startPos.y = roadHeightOffset;
            endPos.y = roadHeightOffset;
            float width = GetRoadWidth(highwayType);

            return MeshUtilities.CreateRoadSegmentMesh(startPos, endPos, width, 0.1f);
        }

        private IEnumerator CollectBuildingMeshes()
        {
            Log($"Collecting {currentMapData.buildings.Count} buildings");
            int processed = 0;

            foreach (var building in currentMapData.buildings)
            {
                building.CalculateHeight();
                var mesh = CreateBuildingMesh(building);

                if (mesh.HasValue)
                {
                    var key = GetBuildingMaterialKey(building.buildingType);
                    buildingMeshesByMaterial[key].Add(mesh.Value);
                    buildingColliders.Add(CreateColliderData(mesh.Value, "Building"));
                }

                processed++;
                if (processed >= maxBuildingsPerFrame)
                {
                    processed = 0;
                    yield return null;
                }
            }
        }

        private CombineInstance? CreateBuildingMesh(OSMBuilding b)
        {
            if (b.nodes.Count < 3) return null;
            Vector3 size = CalculateBuildingSize(b);
            size.x = Mathf.Max(size.x, minimumBuildingSize);
            size.z = Mathf.Max(size.z, minimumBuildingSize);
            size.y = b.height * buildingHeightMultiplier;

            return MeshUtilities.CreateBuildingMesh(
                CalculateBuildingCenter(b), size, CalculateBuildingRotation(b));
        }

        private IEnumerator CollectAreaMeshes()
        {
            Log($"Collecting {currentMapData.areas.Count} areas");
            int processed = 0;

            foreach (var area in currentMapData.areas)
            {
                if (area.nodes.Count < 3) continue;
                area.DetermineAreaType();

                var mesh = CreateAreaMesh(area);
                if (mesh.HasValue)
                {
                    var key = GetAreaMaterialKey(area.areaType);
                    areaMeshesByMaterial[key].Add(mesh.Value);
                }

                if (++processed % 3 == 0) yield return null;
            }
        }

        private CombineInstance? CreateAreaMesh(OSMArea a)
        {
            Vector3 center = CalculateAreaCenter(a);
            Vector3 size = CalculateAreaSize(a);
            size.y = 0.02f;
            return MeshUtilities.CreateAreaMesh(center, size);
        }

        private IEnumerator ApplyMeshBatching()
        {
            yield return BatchMeshesByMaterial(roadMeshesByMaterial, batchedRoadsContainer, GetRoadMaterial, "Roads");
            yield return BatchMeshesByMaterial(buildingMeshesByMaterial, batchedBuildingsContainer, GetBuildingMaterial, "Buildings");
            if (batchAreas) yield return BatchMeshesByMaterial(areaMeshesByMaterial, batchedAreasContainer, GetAreaMaterial, "Areas");
        }

        private IEnumerator BatchMeshesByMaterial<TKey>(
            Dictionary<TKey, List<CombineInstance>> collections,
            Transform container,
            System.Func<TKey, Material> getMat,
            string prefix)
        {
            foreach (var kvp in collections)
            {
                if (kvp.Value.Count == 0) continue;

                Mesh mesh = new Mesh();
                mesh.CombineMeshes(kvp.Value.ToArray(), true, true);
                mesh.RecalculateNormals();

                GameObject go = new GameObject($"{prefix}_{kvp.Key}");
                go.transform.SetParent(container);
                go.AddComponent<MeshFilter>().mesh = mesh;
                go.AddComponent<MeshRenderer>().material = getMat(kvp.Key);
                if (enableStaticBatching) go.isStatic = true;

                yield return null;
            }
        }

        private IEnumerator CreateSeparateColliders()
        {
            foreach (var data in roadColliders.Concat(buildingColliders))
            {
                GameObject obj = new GameObject($"{data.name}_Collider");
                obj.transform.SetParent(collidersContainer);
                obj.transform.SetPositionAndRotation(data.position, data.rotation);
                obj.transform.localScale = data.scale;

                if (data.mesh != null)
                    obj.AddComponent<MeshCollider>().sharedMesh = data.mesh;
                else
                    obj.AddComponent<BoxCollider>();

                if (enableStaticBatching) obj.isStatic = true;
                yield return null;
            }
        }

        private IEnumerator GenerateGroundPlane()
        {
            Vector3 center = currentMapData.GetWorldCenter();
            float size = Mathf.Max(
                (float)(currentMapData.bounds.GetWidth() * currentMapData.scaleMultiplier),
                (float)(currentMapData.bounds.GetHeight() * currentMapData.scaleMultiplier)
            );

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = center;
            ground.transform.localScale = Vector3.one * (size / 10f);
            ground.layer = groundLayer;
            ground.name = "GroundPlane";
            ground.transform.SetParent(mapContainer);
            if (enableStaticBatching) ground.isStatic = true;
            yield return null;
        }

        private IEnumerator GenerateAreas()
        {
            Transform container = new GameObject("Areas").transform;
            container.SetParent(mapContainer);

            foreach (var area in currentMapData.areas)
            {
                area.DetermineAreaType();
                Vector3 center = CalculateAreaCenter(area);
                Vector3 size = CalculateAreaSize(area);

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(container);
                cube.transform.position = center;
                cube.transform.localScale = size;
                cube.GetComponent<MeshRenderer>().material = GetAreaMaterial(GetAreaMaterialKey(area.areaType));
                if (enableStaticBatching) cube.isStatic = true;

                yield return null;
            }
        }

        private IEnumerator PlaceCollectibles()
        {
            if (!collectiblePrefab) yield break;
            Transform container = new GameObject("Collectibles").transform;
            container.SetParent(mapContainer);

            foreach (var pos in GenerateCollectiblePositions())
            {
                Instantiate(collectiblePrefab, pos, Quaternion.identity, container);
                yield return null;
            }

            FindFirstObjectByType<LevelManager>()?.RescanCollectibles();
        }

        private List<Vector3> GenerateCollectiblePositions()
        {
            List<Vector3> list = new();
            foreach (var b in currentMapData.buildings)
            {
                for (int i = 0; i < collectiblesPerBuilding; i++)
                {
                    var node = b.nodes[Random.Range(0, b.nodes.Count)];
                    Vector3 pos = currentMapData.LatLonToWorldPosition(node.lat, node.lon);
                    pos += new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
                    list.Add(pos);
                }
            }
            return list;
        }

        private IEnumerator PlaceGoalZone()
        {
            if (!goalZonePrefab) yield break;
            Vector3 pos = FindOptimalGoalPosition();
            Instantiate(goalZonePrefab, pos, Quaternion.identity, mapContainer).name = "GoalZone";
            yield return null;
        }

        private Vector3 FindOptimalGoalPosition()
        {
            Vector3 pos = currentMapData.GetWorldCenter();
            if (currentMapData.buildings.Count > 0)
                pos = CalculateBuildingCenter(currentMapData.buildings.OrderByDescending(b => CalculateBuildingSize(b).x * CalculateBuildingSize(b).z).First());
            pos.y = 0.5f;
            return pos;
        }

        private void SetPlayerSpawnPosition()
        {
            playerSpawnPosition = currentMapData.GetWorldCenter();
            if (currentMapData.roads.Count > 0)
                playerSpawnPosition = currentMapData.LatLonToWorldPosition(currentMapData.roads[0].nodes[0].lat, currentMapData.roads[0].nodes[0].lon);

            playerSpawnPosition.y = 1f;
            OnPlayerSpawnPositionSet?.Invoke(playerSpawnPosition);

            GameObject player = GameObject.FindGameObjectWithTag("Player") ??
                                Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            player.transform.position = playerSpawnPosition;
        }

        private IEnumerator AddSteampunkAtmosphere()
        {
            RenderSettings.ambientLight = new Color(0.4f, 0.35f, 0.25f);
            RenderSettings.fogColor = new Color(0.5f, 0.4f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 80f;
            yield return null;
        }

        private void CreateMapContainer()
        {
            if (!mapContainer)
                mapContainer = new GameObject("GeneratedMapBatched").transform;
        }

        private void ClearExistingMap()
        {
            foreach (Transform child in mapContainer)
                Destroy(child.gameObject);

            roadColliders.Clear();
            buildingColliders.Clear();
            InitializeBatchingCollections();
        }

        // === Utility & Helpers ===
        private string GetHighwayType(OSMWay r) => r.tags.TryGetValue("highway", out var h) ? h : "default";
        private float GetRoadWidth(string type) => type switch
        {
            "motorway" => 5.5f,
            "primary" => 4f,
            "secondary" => 3f,
            "residential" => 2f,
            "footway" => 1f,
            _ => 2f
        };
        private RoadCategory GetRoadMaterialKey(string type) => type switch
        {
            "motorway" => RoadCategory.Motorway,
            "primary" => RoadCategory.Primary,
            "secondary" => RoadCategory.Secondary,
            "residential" => RoadCategory.Residential,
            "footway" => RoadCategory.Footway,
            _ => RoadCategory.Default
        };
        private BuildingCategory GetBuildingMaterialKey(string type) => type switch
        {
            "industrial" => BuildingCategory.Industrial,
            "commercial" => BuildingCategory.Commercial,
            "office" => BuildingCategory.Office,
            "residential" => BuildingCategory.Residential,
            _ => BuildingCategory.Default
        };
        private AreaCategory GetAreaMaterialKey(string type) => type switch
        {
            "park" => AreaCategory.Park,
            "water" => AreaCategory.Water,
            "forest" => AreaCategory.Forest,
            "grass" => AreaCategory.Grass,
            _ => AreaCategory.Default
        };
        private Material GetRoadMaterial(RoadCategory key) => key switch
        {
            RoadCategory.Motorway => roadMotorway ?? roadDefault,
            RoadCategory.Primary => roadPrimary ?? roadDefault,
            RoadCategory.Secondary => roadSecondary ?? roadDefault,
            RoadCategory.Residential => roadResidential ?? roadDefault,
            RoadCategory.Footway => roadFootway ?? roadDefault,
            _ => roadDefault
        };
        private Material GetBuildingMaterial(BuildingCategory key) => key switch
        {
            BuildingCategory.Residential => residentialMaterial ?? defaultBuildingMaterial,
            BuildingCategory.Industrial => industrialMaterial ?? defaultBuildingMaterial,
            BuildingCategory.Commercial => commercialMaterial ?? defaultBuildingMaterial,
            BuildingCategory.Office => officeMaterial ?? defaultBuildingMaterial,
            _ => defaultBuildingMaterial
        };
        private Material GetAreaMaterial(AreaCategory key) => key switch
        {
            AreaCategory.Park => parkMaterial ?? defaultAreaMaterial,
            AreaCategory.Water => waterMaterial ?? defaultAreaMaterial,
            AreaCategory.Forest => forestMaterial ?? defaultAreaMaterial,
            AreaCategory.Grass => grassMaterial ?? defaultAreaMaterial,
            _ => defaultAreaMaterial
        };

        private Vector3 CalculateBuildingCenter(OSMBuilding b) => b.nodes.Select(n => currentMapData.LatLonToWorldPosition(n.lat, n.lon)).Aggregate(Vector3.zero, (sum, v) => sum + v) / b.nodes.Count;
        private Vector3 CalculateBuildingSize(OSMBuilding b)
        {
            var pts = b.nodes.Select(n => currentMapData.LatLonToWorldPosition(n.lat, n.lon)).ToList();
            Vector3 min = pts.Aggregate(Vector3.positiveInfinity, Vector3.Min);
            Vector3 max = pts.Aggregate(Vector3.negativeInfinity, Vector3.Max);
            return max - min;
        }
        private Quaternion CalculateBuildingRotation(OSMBuilding b)
        {
            if (b.nodes.Count < 2) return Quaternion.identity;
            var p1 = currentMapData.LatLonToWorldPosition(b.nodes[0].lat, b.nodes[0].lon);
            var p2 = currentMapData.LatLonToWorldPosition(b.nodes[1].lat, b.nodes[1].lon);
            var dir = (p2 - p1).normalized;
            return dir.magnitude > 0.1f ? Quaternion.Euler(0, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 0) : Quaternion.identity;
        }
        private Vector3 CalculateAreaCenter(OSMArea a)
        {
            int count = a.IsClosed() ? a.nodes.Count - 1 : a.nodes.Count;
            return a.nodes.Take(count).Select(n => currentMapData.LatLonToWorldPosition(n.lat, n.lon)).Aggregate(Vector3.zero, (sum, v) => sum + v) / count;
        }
        private Vector3 CalculateAreaSize(OSMArea a)
        {
            int count = a.IsClosed() ? a.nodes.Count - 1 : a.nodes.Count;
            var pts = a.nodes.Take(count).Select(n => currentMapData.LatLonToWorldPosition(n.lat, n.lon)).ToList();
            Vector3 min = pts.Aggregate(Vector3.positiveInfinity, Vector3.Min);
            Vector3 max = pts.Aggregate(Vector3.negativeInfinity, Vector3.Max);
            Vector3 size = max - min;
            size.x = Mathf.Max(size.x, 2f);
            size.z = Mathf.Max(size.z, 2f);
            return size;
        }

        private ColliderData CreateColliderData(CombineInstance ci, string name) =>
            new ColliderData { name = name, mesh = ci.mesh, position = ci.transform.GetColumn(3), rotation = ci.transform.rotation, scale = ci.transform.lossyScale };

        private void LogPerformanceStats()
        {
            Log($"Road meshes: {roadMeshesByMaterial.Sum(k => k.Value.Count)} | Building meshes: {buildingMeshesByMaterial.Sum(k => k.Value.Count)}");
        }
        private void FailGeneration(string msg)
        {
            Debug.LogError(msg);
            OnGenerationError?.Invoke(msg);
            isGenerating = false;
        }
        private void Log(string msg) { if (debugMode) Debug.Log("[MapGen] " + msg); }

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
}
