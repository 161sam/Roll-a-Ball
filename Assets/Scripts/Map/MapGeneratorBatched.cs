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

        // ----------- Hilfsmethoden (gekürzt, da unverändert aus deiner Version) -----------

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

        private void FailGeneration(string msg)
        {
            Debug.LogError(msg);
            OnGenerationError?.Invoke(msg);
            isGenerating = false;
        }

        private void Log(string msg)
        {
            if (debugMode) Debug.Log("[MapGen] " + msg);
        }

        // ----------- Öffentliche Getter für Kompatibilität -----------

        public OSMMapData GetCurrentMapData()
        {
            return currentMapData;
        }

        public Vector3 GetPlayerSpawnPosition()
        {
            return playerSpawnPosition;
        }

        // ----------- ColliderData Struct -----------

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
