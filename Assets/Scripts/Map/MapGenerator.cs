using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace RollABall.Map
{
    /// <summary>
    /// Enhanced MapGenerator with segment-based road generation
    /// Generates realistic road networks from OSM data with proper width and materials
    /// </summary>
    public class MapGenerator : MonoBehaviour
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
        
        [Header("Road Settings")]
        [SerializeField] private float roadHeightOffset = 0.05f; // Height above ground
        [SerializeField] private bool enableRoadColliders = true;
        [SerializeField] private bool enableFootwayColliders = false; // Footways typically don't block player
        
        [Header("Building Generation Settings")]
        [SerializeField] private float buildingHeightMultiplier = 1.0f;
        [SerializeField] private int collectiblesPerBuilding = 2;
        [SerializeField] private bool enableSteampunkEffects = true;
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private float minimumBuildingSize = 2.0f;
        [SerializeField] private bool enablePolygonalBuildings = true;
        
        [Header("Performance Settings")]
        [SerializeField] private bool useBatching = true;
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
        
        // Collections for batching
        private Dictionary<string, List<CombineInstance>> roadMeshesByType = new Dictionary<string, List<CombineInstance>>();
        private Dictionary<string, List<CombineInstance>> buildingMeshesByType = new Dictionary<string, List<CombineInstance>>();
        
        private void Awake()
        {
            CreateMapContainer();
            InitializeMeshCollections();
        }
        
        /// <summary>
        /// Initialize collections for mesh batching by type
        /// </summary>
        private void InitializeMeshCollections()
        {
            roadMeshesByType.Clear();
            roadMeshesByType["motorway"] = new List<CombineInstance>();
            roadMeshesByType["primary"] = new List<CombineInstance>();
            roadMeshesByType["secondary"] = new List<CombineInstance>();
            roadMeshesByType["residential"] = new List<CombineInstance>();
            roadMeshesByType["footway"] = new List<CombineInstance>();
            roadMeshesByType["default"] = new List<CombineInstance>();
            
            buildingMeshesByType.Clear();
            buildingMeshesByType["residential"] = new List<CombineInstance>();
            buildingMeshesByType["industrial"] = new List<CombineInstance>();
            buildingMeshesByType["commercial"] = new List<CombineInstance>();
            buildingMeshesByType["office"] = new List<CombineInstance>();
            buildingMeshesByType["default"] = new List<CombineInstance>();
        }
        
        /// <summary>
        /// Generate Unity world from OSM map data
        /// </summary>
        public void GenerateMap(OSMMapData mapData)
        {
            if (isGenerating)
            {
                Debug.LogWarning("[MapGenerator] Generation already in progress");
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
        /// Main generation coroutine - spreads work across frames
        /// </summary>
        private IEnumerator GenerateMapCoroutine()
        {
            isGenerating = true;
            OnMapGenerationStarted?.Invoke(currentMapData);
            
            Debug.Log("[MapGenerator] Starting segmented map generation...");
            
            bool hasError = false;
            string errorMessage = "";
            
            try
            {
                // Clear existing content
                ClearExistingMap();
                InitializeMeshCollections();
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
                
                // Generate segmented roads
                yield return StartCoroutine(GenerateSegmentedRoads());
                
                // Generate buildings with polygonal meshes
                yield return StartCoroutine(GeneratePolygonalBuildings());
                
                // Generate areas (parks, water, etc.)
                yield return StartCoroutine(GenerateAreas());
                
                // Place collectibles
                yield return StartCoroutine(PlaceCollectibles());
                
                // Place goal zone
                yield return StartCoroutine(PlaceGoalZone());
                
                // Set player spawn position
                SetPlayerSpawnPosition();
                
                // Apply batching for performance
                if (useBatching)
                {
                    yield return StartCoroutine(ApplyMeshBatching());
                }
                
                // Add Steampunk atmosphere
                if (enableSteampunkEffects)
                {
                    yield return StartCoroutine(AddSteampunkAtmosphere());
                }
                
                Debug.Log("[MapGenerator] Segmented map generation completed successfully");
                OnMapGenerationCompleted?.Invoke();
            }
            else
            {
                Debug.LogError($"[MapGenerator] Generation failed: {errorMessage}");
                OnGenerationError?.Invoke($"Generation failed: {errorMessage}");
            }
            
            isGenerating = false;
        }
        
        /// <summary>
        /// Generate base ground plane for the map
        /// </summary>
        private IEnumerator GenerateGroundPlane()
        {
            Debug.Log("[MapGenerator] Generating ground plane...");
            
            Vector3 mapCenter = currentMapData.GetWorldCenter();
            float mapSize = Mathf.Max(
                (float)(currentMapData.bounds.GetWidth() * currentMapData.scaleMultiplier),
                (float)(currentMapData.bounds.GetHeight() * currentMapData.scaleMultiplier)
            );
            
            GameObject groundPlane = CreateGroundPlane(mapCenter, mapSize);
            groundPlane.transform.SetParent(mapContainer);
            
            yield return null;
        }
        
        /// <summary>
        /// Generate segmented road geometry from OSM way data
        /// Processes each road segment individually with proper width and materials
        /// </summary>
        private IEnumerator GenerateSegmentedRoads()
        {
            Debug.Log($"[MapGenerator] Generating {currentMapData.roads.Count} roads with segments...");
            
            Transform roadContainer = new GameObject("Roads").transform;
            roadContainer.SetParent(mapContainer);
            
            int processedSegments = 0;
            
            for (int roadIndex = 0; roadIndex < currentMapData.roads.Count; roadIndex++)
            {
                OSMWay road = currentMapData.roads[roadIndex];
                
                // Extract highway type from tags
                string highwayType = GetHighwayType(road);
                
                // Generate segments for this road
                for (int nodeIndex = 0; nodeIndex < road.nodes.Count - 1; nodeIndex++)
                {
                    OSMNode startNode = road.nodes[nodeIndex];
                    OSMNode endNode = road.nodes[nodeIndex + 1];
                    
                    // Create segment mesh
                    GameObject segment = CreateRoadSegment(startNode, endNode, highwayType, roadIndex, nodeIndex);
                    if (segment != null)
                    {
                        segment.transform.SetParent(roadContainer);
                    }
                    
                    processedSegments++;
                    
                    // Yield every few segments to prevent frame drops
                    if (processedSegments >= maxRoadSegmentsPerFrame)
                    {
                        processedSegments = 0;
                        yield return null;
                    }
                }
            }
            
            Debug.Log($"[MapGenerator] Generated {processedSegments} road segments total");
            yield return null;
        }
        
        /// <summary>
        /// Create a single road segment between two nodes
        /// </summary>
        private GameObject CreateRoadSegment(OSMNode startNode, OSMNode endNode, string highwayType, int roadIndex, int segmentIndex)
        {
            // Convert OSM coordinates to Unity world positions
            Vector3 startPos = currentMapData.LatLonToWorldPosition(startNode.lat, startNode.lon);
            Vector3 endPos = currentMapData.LatLonToWorldPosition(endNode.lat, endNode.lon);
            
            // Skip segments that are too short (avoid visual noise)
            float segmentLength = Vector3.Distance(startPos, endPos);
            if (segmentLength < 0.1f) return null;
            
            // Calculate segment properties
            Vector3 segmentCenter = (startPos + endPos) / 2f;
            segmentCenter.y = roadHeightOffset; // Place slightly above ground
            
            Vector3 direction = (endPos - startPos).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            
            float segmentWidth = GetRoadWidth(highwayType);
            
            // Create segment GameObject
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"road_{roadIndex}_{segmentIndex}_{highwayType}";
            segment.transform.position = segmentCenter;
            segment.transform.rotation = rotation;
            segment.transform.localScale = new Vector3(segmentWidth, 0.1f, segmentLength);
            
            // Apply material
            Material roadMat = GetRoadMaterial(highwayType);
            if (roadMat != null)
            {
                segment.GetComponent<MeshRenderer>().material = roadMat;
            }
            
            // Configure collider
            ConfigureRoadCollider(segment, highwayType);
            
            return segment;
        }
        
        /// <summary>
        /// Get highway type from OSM way tags
        /// </summary>
        private string GetHighwayType(OSMWay road)
        {
            if (road.tags.ContainsKey("highway"))
            {
                string highway = road.tags["highway"];
                
                // Normalize highway types
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
        
        /// <summary>
        /// Get road width based on highway type
        /// </summary>
        private float GetRoadWidth(string highwayType)
        {
            return highwayType switch
            {
                "motorway" => 5.5f,    // Wide highways
                "primary" => 4.0f,     // Main roads
                "secondary" => 3.0f,   // Secondary roads
                "residential" => 2.0f, // Neighborhood streets
                "footway" => 1.0f,     // Pedestrian paths
                _ => 2.0f              // Default width
            };
        }
        
        /// <summary>
        /// Get material for road type
        /// </summary>
        private Material GetRoadMaterial(string highwayType)
        {
            return highwayType switch
            {
                "motorway" => roadMotorway ?? roadDefault,
                "primary" => roadPrimary ?? roadDefault,
                "secondary" => roadSecondary ?? roadDefault,
                "residential" => roadResidential ?? roadDefault,
                "footway" => roadFootway ?? roadDefault,
                _ => roadDefault
            };
        }
        
        /// <summary>
        /// Configure collider for road segment based on type
        /// </summary>
        private void ConfigureRoadCollider(GameObject segment, string highwayType)
        {
            if (!enableRoadColliders)
            {
                // Remove collider completely if disabled
                var collider = segment.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                        Destroy(collider);
                    else
                        DestroyImmediate(collider);
                }
                return;
            }
            
            // Special handling for footways
            if (highwayType == "footway" && !enableFootwayColliders)
            {
                var collider = segment.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                        Destroy(collider);
                    else
                        DestroyImmediate(collider);
                }
                return;
            }
            
            // Ensure the segment has a BoxCollider (more performance-friendly than MeshCollider for simple shapes)
            if (segment.GetComponent<BoxCollider>() == null)
            {
                segment.AddComponent<BoxCollider>();
            }
        }
        
        /// <summary>
        /// Generate polygonal building geometry from OSM building data with proper extrusion
        /// </summary>
        private IEnumerator GeneratePolygonalBuildings()
        {
            Debug.Log($"[MapGenerator] Generating {currentMapData.buildings.Count} polygonal buildings...");
            
            Transform buildingContainer = new GameObject("Buildings").transform;
            buildingContainer.SetParent(mapContainer);
            
            int processed = 0;
            for (int i = 0; i < currentMapData.buildings.Count; i++)
            {
                OSMBuilding building = currentMapData.buildings[i];
                
                // Ensure building has calculated height and type
                building.CalculateHeight();
                
                GameObject buildingObject = GenerateExtrudedBuildingMesh(building);
                if (buildingObject != null)
                {
                    buildingObject.transform.SetParent(buildingContainer);
                    buildingObject.name = $"Building_{building.id}_{building.buildingType}";
                    
                    // Add Steampunk elements for appropriate building types
                    if (enableSteampunkEffects && ShouldAddSteampunkElements(building.buildingType))
                    {
                        AddSteampunkElementsToBuilding(buildingObject, building);
                    }
                }
                
                processed++;
                if (processed >= maxBuildingsPerFrame)
                {
                    processed = 0;
                    yield return null;
                }
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Generate area objects (parks, water bodies, etc.)
        /// </summary>
        private IEnumerator GenerateAreas()
        {
            Debug.Log($"[MapGenerator] Generating {currentMapData.areas.Count} areas...");
            
            Transform areaContainer = new GameObject("Areas").transform;
            areaContainer.SetParent(mapContainer);
            
            for (int i = 0; i < currentMapData.areas.Count; i++)
            {
                OSMArea area = currentMapData.areas[i];
                GenerateAreaFromData(area, areaContainer);
                
                if (i % 2 == 0)
                    yield return null;
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Place collectible objects around the map
        /// </summary>
        private IEnumerator PlaceCollectibles()
        {
            if (collectiblePrefab == null)
            {
                Debug.LogWarning("[MapGenerator] No collectible prefab assigned");
                yield break;
            }
            
            Debug.Log("[MapGenerator] Placing collectibles...");
            
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
            
            // Add collectibles to LevelManager for proper tracking
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                // Get all created collectibles and add them to LevelManager
                CollectibleController[] collectibles = collectibleContainer.GetComponentsInChildren<CollectibleController>();
                foreach (CollectibleController collectible in collectibles)
                {
                    if (collectible != null)
                    {
                        // Set proper tag for goal zone detection
                        collectible.gameObject.tag = "Collectible";
                        
                        // Add to LevelManager for event tracking
                        levelManager.AddCollectible(collectible);
                    }
                }
                
                Debug.Log($"[MapGenerator] Added {collectibles.Length} collectibles to LevelManager");
            }
            else
            {
                Debug.LogWarning("[MapGenerator] No LevelManager found in scene. Collectibles won't be tracked properly.");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Place the goal zone at an appropriate location
        /// </summary>
        private IEnumerator PlaceGoalZone()
        {
            if (goalZonePrefab == null)
            {
                Debug.LogWarning("[MapGenerator] No goal zone prefab assigned");
                yield break;
            }
            
            Debug.Log("[MapGenerator] Placing goal zone...");
            
            Vector3 goalPosition = FindOptimalGoalPosition();
            GameObject goalZone = Instantiate(goalZonePrefab, goalPosition, Quaternion.identity);
            goalZone.transform.SetParent(mapContainer);
            goalZone.name = "GoalZone";
            goalZone.tag = "Finish"; // Set proper tag for LevelManager detection
            
            // Add OSM Goal Zone Trigger for automatic level completion
            OSMGoalZoneTrigger trigger = goalZone.GetComponent<OSMGoalZoneTrigger>();
            if (trigger == null)
            {
                trigger = goalZone.AddComponent<OSMGoalZoneTrigger>();
            }
            
            Debug.Log("[MapGenerator] Goal zone configured with trigger");
            
            yield return null;
        }
        
        /// <summary>
        /// Generate extruded building mesh from OSM building footprint
        /// </summary>
        private GameObject GenerateExtrudedBuildingMesh(OSMBuilding building)
        {
            if (building.nodes.Count < 4)
            {
                Debug.LogWarning($"[MapGenerator] Building {building.id} has insufficient points ({building.nodes.Count}), using fallback");
                return GenerateFallbackBuildingMesh(building);
            }
            
            // Convert OSM nodes to world positions
            List<Vector3> worldPositions = new List<Vector3>();
            for (int i = 0; i < building.nodes.Count - 1; i++) // Skip last node (same as first in closed ways)
            {
                Vector3 worldPos = currentMapData.LatLonToWorldPosition(building.nodes[i].lat, building.nodes[i].lon);
                worldPositions.Add(worldPos);
            }
            
            if (worldPositions.Count < 3)
            {
                return GenerateFallbackBuildingMesh(building);
            }
            
            try
            {
                if (enablePolygonalBuildings)
                {
                    return CreateExtrudedPolygonMesh(worldPositions, building);
                }
                else
                {
                    return GenerateFallbackBuildingMesh(building);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MapGenerator] Failed to create polygonal mesh for building {building.id}: {e.Message}. Using fallback.");
                return GenerateFallbackBuildingMesh(building);
            }
        }
        
        /// <summary>
        /// Generate polygon-based area mesh from OSM data (parks, water, forests, etc.)
        /// </summary>
        private void GenerateAreaFromData(OSMArea area, Transform parent)
        {
            if (area.nodes.Count < 3)
            {
                Debug.LogWarning($"[MapGenerator] Area {area.id} has insufficient nodes ({area.nodes.Count}), skipping");
                return;
            }
            
            // Ensure area type is determined
            if (string.IsNullOrEmpty(area.areaType))
            {
                area.DetermineAreaType();
            }
            
            // Convert OSM nodes to world positions
            List<Vector3> areaPoints = new List<Vector3>();
            int nodesToProcess = area.IsClosed() ? area.nodes.Count - 1 : area.nodes.Count;
            
            for (int i = 0; i < nodesToProcess; i++)
            {
                Vector3 worldPos = currentMapData.LatLonToWorldPosition(area.nodes[i].lat, area.nodes[i].lon);
                areaPoints.Add(worldPos);
            }
            
            // Create triangulated area mesh
            GameObject areaObject = CreateAreaMesh(areaPoints);
            if (areaObject == null)
            {
                Debug.LogWarning($"[MapGenerator] Failed to create area mesh for area {area.id}");
                return;
            }
            
            areaObject.transform.SetParent(parent);
            areaObject.name = $"Area_{area.areaType}_{area.id}";
            
            // Apply appropriate material based on area type
            Material areaMat = GetMaterialForAreaType(area.areaType);
            MeshRenderer renderer = areaObject.GetComponent<MeshRenderer>();
            if (renderer != null && areaMat != null)
            {
                renderer.material = areaMat;
            }
            
            // Add optional collider for water areas (can be used for gameplay mechanics)
            if (area.areaType == "water" || area.areaType == "river" || area.areaType == "lake")
            {
                AddWaterAreaCollider(areaObject);
            }
            
            Debug.Log($"[MapGenerator] Generated area: {area.areaType} with {areaPoints.Count} points");
        }
        
        /// <summary>
        /// Add optional trigger collider to water areas for gameplay mechanics
        /// </summary>
        private void AddWaterAreaCollider(GameObject waterObject)
        {
            if (waterObject == null) return;
            
            MeshFilter meshFilter = waterObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = waterObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.isTrigger = true; // Use as trigger for potential respawn mechanics
                meshCollider.tag = "Water"; // Tag for identification
                
                Debug.Log($"[MapGenerator] Added water collider to {waterObject.name}");
            }
        }
        
        /// <summary>
        /// Generate positions for collectibles
        /// </summary>
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
                        
                        // Offset randomly around the building
                        Vector3 offset = new Vector3(
                            Random.Range(-5f, 5f),
                            1f, // Above ground
                            Random.Range(-5f, 5f)
                        );
                        
                        positions.Add(buildingPos + offset);
                    }
                }
            }
            
            // Add some random positions along roads
            foreach (OSMWay road in currentMapData.roads)
            {
                if (road.nodes.Count > 1)
                {
                    OSMNode randomNode = road.nodes[Random.Range(0, road.nodes.Count)];
                    Vector3 roadPos = currentMapData.LatLonToWorldPosition(randomNode.lat, randomNode.lon);
                    roadPos.y = 1f;
                    positions.Add(roadPos);
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Find optimal position for goal zone
        /// </summary>
        private Vector3 FindOptimalGoalPosition()
        {
            // Place goal at the center of the largest building or at map center
            Vector3 goalPos = currentMapData.GetWorldCenter();
            
            if (currentMapData.buildings.Count > 0)
            {
                OSMBuilding largestBuilding = currentMapData.buildings[0];
                float largestArea = 0f;
                
                foreach (OSMBuilding building in currentMapData.buildings)
                {
                    float area = CalculateBuildingArea(building);
                    if (area > largestArea)
                    {
                        largestArea = area;
                        largestBuilding = building;
                    }
                }
                
                // Get center of largest building
                if (largestBuilding.nodes.Count > 0)
                {
                    OSMNode centerNode = largestBuilding.nodes[largestBuilding.nodes.Count / 2];
                    goalPos = currentMapData.LatLonToWorldPosition(centerNode.lat, centerNode.lon);
                }
            }
            
            goalPos.y = 0.5f; // Place slightly above ground
            return goalPos;
        }
        
        /// <summary>
        /// Set player spawn position
        /// </summary>
        private void SetPlayerSpawnPosition()
        {
            // Spawn player at a safe road position
            playerSpawnPosition = currentMapData.GetWorldCenter();
            
            if (currentMapData.roads.Count > 0)
            {
                OSMWay firstRoad = currentMapData.roads[0];
                if (firstRoad.nodes.Count > 0)
                {
                    playerSpawnPosition = currentMapData.LatLonToWorldPosition(firstRoad.nodes[0].lat, firstRoad.nodes[0].lon);
                }
            }
            
            playerSpawnPosition.y = 1f; // Above ground
            OnPlayerSpawnPositionSet?.Invoke(playerSpawnPosition);
            
            // Spawn player if prefab is assigned
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
        
        // Helper methods for mesh generation
        private GameObject CreateGroundPlane(Vector3 center, float size)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = center;
            ground.transform.localScale = Vector3.one * (size / 10f); // Plane is 10x10 by default
            ground.name = "GroundPlane";
            ground.layer = groundLayer;
            return ground;
        }
        
        /// <summary>
        /// Create extruded polygon mesh from building footprint points
        /// </summary>
        private GameObject CreateExtrudedPolygonMesh(List<Vector3> footprint, OSMBuilding building)
        {
            float height = building.height * buildingHeightMultiplier;
            
            // Triangulate the footprint for the roof and floor
            List<int> triangles = TriangulatePolygon(footprint);
            if (triangles.Count == 0)
            {
                throw new System.Exception("Failed to triangulate building footprint");
            }
            
            // Create mesh data
            List<Vector3> vertices = new List<Vector3>();
            List<int> meshTriangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            
            // Add floor vertices (y = 0)
            foreach (Vector3 point in footprint)
            {
                vertices.Add(new Vector3(point.x, 0, point.z));
                normals.Add(Vector3.down);
                uvs.Add(new Vector2(point.x * 0.1f, point.z * 0.1f));
            }
            
            // Add roof vertices (y = height)
            foreach (Vector3 point in footprint)
            {
                vertices.Add(new Vector3(point.x, height, point.z));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(point.x * 0.1f, point.z * 0.1f));
            }
            
            int roofVertexOffset = footprint.Count;
            
            // Add floor triangles (inverted winding for downward facing)
            for (int i = triangles.Count - 3; i >= 0; i -= 3)
            {
                meshTriangles.Add(triangles[i]);
                meshTriangles.Add(triangles[i + 1]);
                meshTriangles.Add(triangles[i + 2]);
            }
            
            // Add roof triangles
            foreach (int triangle in triangles)
            {
                meshTriangles.Add(triangle + roofVertexOffset);
            }
            
            // Generate walls
            for (int i = 0; i < footprint.Count; i++)
            {
                int nextIndex = (i + 1) % footprint.Count;
                
                // Current wall vertices
                int wallVertexStart = vertices.Count;
                
                // Bottom edge
                vertices.Add(new Vector3(footprint[i].x, 0, footprint[i].z));
                vertices.Add(new Vector3(footprint[nextIndex].x, 0, footprint[nextIndex].z));
                
                // Top edge
                vertices.Add(new Vector3(footprint[nextIndex].x, height, footprint[nextIndex].z));
                vertices.Add(new Vector3(footprint[i].x, height, footprint[i].z));
                
                // Calculate wall normal
                Vector3 wallDirection = footprint[nextIndex] - footprint[i];
                Vector3 wallNormal = Vector3.Cross(wallDirection, Vector3.up).normalized;
                
                // Add normals for wall
                for (int j = 0; j < 4; j++)
                {
                    normals.Add(wallNormal);
                }
                
                // Add UVs for wall
                float wallLength = Vector3.Distance(footprint[i], footprint[nextIndex]);
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(wallLength * 0.1f, 0));
                uvs.Add(new Vector2(wallLength * 0.1f, height * 0.1f));
                uvs.Add(new Vector2(0, height * 0.1f));
                
                // Add wall triangles
                meshTriangles.Add(wallVertexStart);
                meshTriangles.Add(wallVertexStart + 1);
                meshTriangles.Add(wallVertexStart + 2);
                
                meshTriangles.Add(wallVertexStart);
                meshTriangles.Add(wallVertexStart + 2);
                meshTriangles.Add(wallVertexStart + 3);
            }
            
            // Create the mesh
            Mesh buildingMesh = new Mesh();
            buildingMesh.name = $"Building_{building.id}_Mesh";
            buildingMesh.vertices = vertices.ToArray();
            buildingMesh.triangles = meshTriangles.ToArray();
            buildingMesh.normals = normals.ToArray();
            buildingMesh.uv = uvs.ToArray();
            
            // Create GameObject
            GameObject buildingObject = new GameObject();
            MeshFilter meshFilter = buildingObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = buildingObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = buildingObject.AddComponent<MeshCollider>();
            
            meshFilter.mesh = buildingMesh;
            meshRenderer.material = GetMaterialForBuildingType(building.buildingType);
            meshCollider.sharedMesh = buildingMesh;
            
            // Position building
            Vector3 buildingCenter = CalculateCentroid(footprint);
            buildingObject.transform.position = buildingCenter;
            
            return buildingObject;
        }
        
        /// <summary>
        /// Create polygon-based mesh for OSM areas (parks, water, forests)
        /// Uses triangulation to create authentic area shapes
        /// </summary>
        private GameObject CreateAreaMesh(List<Vector3> points)
        {
            if (points.Count < 3)
            {
                Debug.LogWarning("[MapGenerator] Cannot create area mesh with less than 3 points");
                return null;
            }
            
            try
            {
                return CreateTriangulatedAreaMesh(points);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MapGenerator] Failed to create triangulated area mesh: {e.Message}. Using fallback.");
                return CreateFallbackAreaMesh(points);
            }
        }
        
        /// <summary>
        /// Create triangulated area mesh with proper polygon shape
        /// </summary>
        private GameObject CreateTriangulatedAreaMesh(List<Vector3> points)
        {
            // Convert 3D points to 2D for triangulation (use x,z coordinates)
            List<Vector2> points2D = new List<Vector2>();
            foreach (Vector3 point in points)
            {
                points2D.Add(new Vector2(point.x, point.z));
            }
            
            // Remove duplicate points
            points2D = RemoveDuplicatePoints(points2D);
            if (points2D.Count < 3)
            {
                throw new System.Exception("Insufficient unique points after duplicate removal");
            }
            
            // Triangulate polygon
            List<int> triangles = TriangulatePolygonEarClipping(points2D);
            if (triangles.Count == 0)
            {
                throw new System.Exception("Triangulation failed");
            }
            
            // Create mesh data
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            
            // Calculate UV bounds for proper texture mapping
            Vector2 uvMin = Vector2.positiveInfinity;
            Vector2 uvMax = Vector2.negativeInfinity;
            
            foreach (Vector2 point in points2D)
            {
                uvMin = Vector2.Min(uvMin, point);
                uvMax = Vector2.Max(uvMax, point);
            }
            
            Vector2 uvSize = uvMax - uvMin;
            if (uvSize.x < 0.01f) uvSize.x = 1f;
            if (uvSize.y < 0.01f) uvSize.y = 1f;
            
            // Add vertices with slight elevation to avoid z-fighting
            foreach (Vector2 point in points2D)
            {
                vertices.Add(new Vector3(point.x, 0.01f, point.y));
                normals.Add(Vector3.up);
                
                // Map UVs to 0-1 range
                Vector2 uv = new Vector2(
                    (point.x - uvMin.x) / uvSize.x,
                    (point.y - uvMin.y) / uvSize.y
                );
                uvs.Add(uv);
            }
            
            // Create mesh
            Mesh areaMesh = new Mesh();
            areaMesh.name = "AreaMesh";
            areaMesh.vertices = vertices.ToArray();
            areaMesh.triangles = triangles.ToArray();
            areaMesh.normals = normals.ToArray();
            areaMesh.uv = uvs.ToArray();
            areaMesh.RecalculateBounds();
            
            // Create GameObject
            GameObject areaObject = new GameObject("AreaMesh");
            MeshFilter meshFilter = areaObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = areaObject.AddComponent<MeshRenderer>();
            
            meshFilter.mesh = areaMesh;
            
            // Position at polygon centroid
            Vector3 centroid = CalculateCentroid(points);
            centroid.y = 0.01f;
            areaObject.transform.position = centroid;
            
            return areaObject;
        }
        
        /// <summary>
        /// Fallback area mesh using bounding box approximation
        /// </summary>
        private GameObject CreateFallbackAreaMesh(List<Vector3> points)
        {
            Debug.Log("[MapGenerator] Using fallback area mesh (bounding box approximation)");
            
            Vector3 center = CalculateCentroid(points);
            center.y = 0.01f;
            
            // Calculate bounding box
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            
            foreach (Vector3 point in points)
            {
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
            
            Vector3 size = max - min;
            size.y = 0.02f; // Thin plane
            
            // Ensure minimum size
            size.x = Mathf.Max(size.x, 2f);
            size.z = Mathf.Max(size.z, 2f);
            
            GameObject area = GameObject.CreatePrimitive(PrimitiveType.Cube);
            area.transform.position = center;
            area.transform.localScale = size;
            area.name = "AreaMesh_Fallback";
            
            // Remove collider for fallback (keep it simple)
            var collider = area.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }
            
            return area;
        }
        
        /// <summary>
        /// Remove duplicate points that are too close together
        /// </summary>
        private List<Vector2> RemoveDuplicatePoints(List<Vector2> points)
        {
            List<Vector2> uniquePoints = new List<Vector2>();
            const float minDistance = 0.01f;
            
            foreach (Vector2 point in points)
            {
                bool isDuplicate = false;
                foreach (Vector2 existing in uniquePoints)
                {
                    if (Vector2.Distance(point, existing) < minDistance)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                
                if (!isDuplicate)
                {
                    uniquePoints.Add(point);
                }
            }
            
            return uniquePoints;
        }
        
        /// <summary>
        /// Get material for area type with comprehensive type support
        /// </summary>
        private Material GetMaterialForAreaType(string areaType)
        {
            if (string.IsNullOrEmpty(areaType))
                return defaultAreaMaterial;
                
            return areaType.ToLower() switch
            {
                // Park and recreation areas
                "park" or "recreation_ground" or "playground" => parkMaterial ?? defaultAreaMaterial,
                
                // Water bodies
                "water" or "river" or "lake" or "pond" or "reservoir" => waterMaterial ?? defaultAreaMaterial,
                
                // Forest and natural areas
                "forest" or "wood" or "scrub" or "heath" => forestMaterial ?? defaultAreaMaterial,
                
                // Grass and meadows
                "grass" or "meadow" or "grassland" => grassMaterial ?? parkMaterial ?? defaultAreaMaterial,
                
                // Commercial and residential
                "residential" => new Color(0.9f, 0.9f, 0.8f, 1f) != Color.white ? 
                    CreateMaterialFromColor(new Color(0.9f, 0.9f, 0.8f, 1f)) : defaultAreaMaterial,
                    
                "commercial" or "retail" => new Color(0.8f, 0.8f, 0.9f, 1f) != Color.white ? 
                    CreateMaterialFromColor(new Color(0.8f, 0.8f, 0.9f, 1f)) : defaultAreaMaterial,
                    
                "industrial" => new Color(0.7f, 0.7f, 0.7f, 1f) != Color.white ? 
                    CreateMaterialFromColor(new Color(0.7f, 0.7f, 0.7f, 1f)) : defaultAreaMaterial,
                
                // Default fallback
                _ => defaultAreaMaterial
            };
        }
        
        /// <summary>
        /// Create material from color for dynamic area types
        /// </summary>
        private Material CreateMaterialFromColor(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            mat.name = $"AreaMaterial_{color.r}_{color.g}_{color.b}";
            return mat;
        }
        
        private float CalculateBuildingArea(OSMBuilding building)
        {
            // Simplified area calculation
            return building.nodes.Count * 10f; // Rough approximation
        }
        
        /// <summary>
        /// Simple ear clipping triangulation for convex polygons
        /// </summary>
        private List<int> TriangulatePolygon(List<Vector3> points)
        {
            List<int> triangles = new List<int>();
            
            if (points.Count < 3) return triangles;
            
            // Simple fan triangulation from first vertex
            // This works well for convex polygons and many building shapes
            for (int i = 1; i < points.Count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }
            
            return triangles;
        }
        
        /// <summary>
        /// Advanced ear clipping triangulation for arbitrary polygons
        /// Handles both convex and concave polygons robustly
        /// </summary>
        private List<int> TriangulatePolygonEarClipping(List<Vector2> points)
        {
            List<int> triangles = new List<int>();
            
            if (points.Count < 3) 
            {
                Debug.LogWarning("[MapGenerator] Cannot triangulate polygon with less than 3 points");
                return triangles;
            }
            
            if (points.Count == 3)
            {
                // Triangle - direct triangulation
                triangles.AddRange(new int[] { 0, 1, 2 });
                return triangles;
            }
            
            // Create working list of vertex indices
            List<int> vertices = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                vertices.Add(i);
            }
            
            // Ensure counter-clockwise winding
            if (IsClockwise(points))
            {
                vertices.Reverse();
            }
            
            int maxIterations = points.Count * 2; // Prevent infinite loops
            int iterations = 0;
            
            // Ear clipping main loop
            while (vertices.Count > 3 && iterations < maxIterations)
            {
                bool earFound = false;
                
                for (int i = 0; i < vertices.Count; i++)
                {
                    int prevIndex = vertices[(i - 1 + vertices.Count) % vertices.Count];
                    int currIndex = vertices[i];
                    int nextIndex = vertices[(i + 1) % vertices.Count];
                    
                    Vector2 prev = points[prevIndex];
                    Vector2 curr = points[currIndex];
                    Vector2 next = points[nextIndex];
                    
                    // Check if this forms a valid ear
                    if (IsConvexVertex(prev, curr, next) && IsValidEar(points, vertices, prevIndex, currIndex, nextIndex))
                    {
                        // Add triangle
                        triangles.Add(prevIndex);
                        triangles.Add(currIndex);
                        triangles.Add(nextIndex);
                        
                        // Remove the ear vertex
                        vertices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }
                
                if (!earFound)
                {
                    Debug.LogWarning("[MapGenerator] Ear clipping failed - no valid ear found. Using fallback triangulation.");
                    break;
                }
                
                iterations++;
            }
            
            // Add final triangle
            if (vertices.Count == 3)
            {
                triangles.Add(vertices[0]);
                triangles.Add(vertices[1]);
                triangles.Add(vertices[2]);
            }
            
            // Fallback to fan triangulation if ear clipping failed
            if (triangles.Count == 0)
            {
                return TriangulatePolygonFan(points);
            }
            
            return triangles;
        }
        
        /// <summary>
        /// Fallback fan triangulation from center point
        /// </summary>
        private List<int> TriangulatePolygonFan(List<Vector2> points)
        {
            List<int> triangles = new List<int>();
            
            for (int i = 1; i < points.Count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }
            
            return triangles;
        }
        
        /// <summary>
        /// Check if polygon vertices are in clockwise order
        /// </summary>
        private bool IsClockwise(List<Vector2> points)
        {
            float area = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                area += (points[next].x - points[i].x) * (points[next].y + points[i].y);
            }
            return area > 0f;
        }
        
        /// <summary>
        /// Check if vertex forms a convex angle
        /// </summary>
        private bool IsConvexVertex(Vector2 prev, Vector2 curr, Vector2 next)
        {
            return CrossProduct(curr - prev, next - curr) > 0f;
        }
        
        /// <summary>
        /// Check if triangle is a valid ear (contains no other vertices)
        /// </summary>
        private bool IsValidEar(List<Vector2> points, List<int> vertices, int prevIndex, int currIndex, int nextIndex)
        {
            Vector2 prev = points[prevIndex];
            Vector2 curr = points[currIndex];
            Vector2 next = points[nextIndex];
            
            // Check if any other vertex lies inside this triangle
            for (int i = 0; i < vertices.Count; i++)
            {
                int vertexIndex = vertices[i];
                if (vertexIndex == prevIndex || vertexIndex == currIndex || vertexIndex == nextIndex)
                    continue;
                    
                if (IsPointInTriangle(points[vertexIndex], prev, curr, next))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if point is inside triangle using barycentric coordinates
        /// </summary>
        private bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(denominator) < 0.0001f) return false;
            
            float alpha = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) / denominator;
            float beta = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) / denominator;
            float gamma = 1f - alpha - beta;
            
            return alpha > 0f && beta > 0f && gamma > 0f;
        }
        
        /// <summary>
        /// Calculate 2D cross product
        /// </summary>
        private float CrossProduct(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
        
        /// <summary>
        /// Generate fallback building mesh using oriented bounding box
        /// </summary>
        private GameObject GenerateFallbackBuildingMesh(OSMBuilding building)
        {
            Debug.Log($"[MapGenerator] Using fallback mesh for building {building.id}");
            
            if (building.nodes.Count == 0)
            {
                return null;
            }
            
            // Calculate building center and bounding box
            Vector3 center = Vector3.zero;
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            
            foreach (OSMNode node in building.nodes)
            {
                Vector3 worldPos = currentMapData.LatLonToWorldPosition(node.lat, node.lon);
                center += worldPos;
                
                min = Vector3.Min(min, worldPos);
                max = Vector3.Max(max, worldPos);
            }
            
            center /= building.nodes.Count;
            center.y = (building.height * buildingHeightMultiplier) / 2f;
            
            // Calculate size ensuring minimum dimensions
            Vector3 size = max - min;
            size.x = Mathf.Max(size.x, minimumBuildingSize);
            size.z = Mathf.Max(size.z, minimumBuildingSize);
            size.y = building.height * buildingHeightMultiplier;
            
            // Create building object
            GameObject buildingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buildingObject.transform.position = center;
            buildingObject.transform.localScale = size;
            
            // Calculate rotation from longest edge if we have enough points
            if (building.nodes.Count >= 2)
            {
                Vector3 firstPos = currentMapData.LatLonToWorldPosition(building.nodes[0].lat, building.nodes[0].lon);
                Vector3 secondPos = currentMapData.LatLonToWorldPosition(building.nodes[1].lat, building.nodes[1].lon);
                Vector3 direction = (secondPos - firstPos).normalized;
                
                if (direction.magnitude > 0.1f)
                {
                    float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    buildingObject.transform.rotation = Quaternion.Euler(0, angle, 0);
                }
            }
            
            // Apply material and ensure collider
            MeshRenderer renderer = buildingObject.GetComponent<MeshRenderer>();
            renderer.material = GetMaterialForBuildingType(building.buildingType);
            
            // Replace BoxCollider with MeshCollider for consistency
            BoxCollider boxCollider = buildingObject.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                if (Application.isPlaying)
                    Destroy(boxCollider);
                else
                    DestroyImmediate(boxCollider);
            }
            
            MeshCollider meshCollider = buildingObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = buildingObject.GetComponent<MeshFilter>().sharedMesh;
            
            return buildingObject;
        }
        
        /// <summary>
        /// Get material for building type
        /// </summary>
        private Material GetMaterialForBuildingType(string buildingType)
        {
            return buildingType switch
            {
                "residential" => residentialMaterial ?? defaultBuildingMaterial,
                "industrial" => industrialMaterial ?? defaultBuildingMaterial,
                "commercial" => commercialMaterial ?? defaultBuildingMaterial,
                "office" => officeMaterial ?? defaultBuildingMaterial,
                "skyscraper" => officeMaterial ?? defaultBuildingMaterial,
                _ => defaultBuildingMaterial
            };
        }
        
        /// <summary>
        /// Determine if building type should get Steampunk decorations
        /// </summary>
        private bool ShouldAddSteampunkElements(string buildingType)
        {
            return buildingType switch
            {
                "industrial" => true,
                "factory" => true,
                "warehouse" => true,
                "commercial" => Random.value < 0.3f, // 30% chance for commercial
                _ => Random.value < 0.1f // 10% chance for others
            };
        }
        
        /// <summary>
        /// Add Steampunk decorative elements to building
        /// </summary>
        private void AddSteampunkElementsToBuilding(GameObject buildingObject, OSMBuilding building)
        {
            Vector3 buildingSize = buildingObject.GetComponent<MeshRenderer>().bounds.size;
            Vector3 roofCenter = buildingObject.transform.position;
            roofCenter.y += buildingSize.y / 2f;
            
            // Add steam emitter for industrial buildings
            if (building.buildingType == "industrial" && chimneySmokeParticles != null)
            {
                Vector3 chimneyPos = roofCenter + new Vector3(
                    Random.Range(-buildingSize.x * 0.3f, buildingSize.x * 0.3f),
                    0.5f,
                    Random.Range(-buildingSize.z * 0.3f, buildingSize.z * 0.3f)
                );
                
                GameObject steamEmitter = Instantiate(chimneySmokeParticles, chimneyPos, Quaternion.identity);
                steamEmitter.transform.SetParent(buildingObject.transform);
                steamEmitter.name = "ChimneySteam";
            }
            
            // Add gear decorations
            if (gearPrefab != null && Random.value < 0.5f)
            {
                int gearCount = Random.Range(1, 4);
                for (int i = 0; i < gearCount; i++)
                {
                    Vector3 gearPos = roofCenter + new Vector3(
                        Random.Range(-buildingSize.x * 0.4f, buildingSize.x * 0.4f),
                        Random.Range(-0.2f, 0.5f),
                        Random.Range(-buildingSize.z * 0.4f, buildingSize.z * 0.4f)
                    );
                    
                    GameObject gear = Instantiate(gearPrefab, gearPos, 
                        Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                    gear.transform.SetParent(buildingObject.transform);
                    gear.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);
                    gear.name = $"SteampunkGear_{i}";
                }
            }
            
            // Add pipe decorations
            if (steamPipePrefab != null && Random.value < 0.4f)
            {
                Vector3 pipePos = roofCenter + new Vector3(
                    Random.Range(-buildingSize.x * 0.3f, buildingSize.x * 0.3f),
                    -buildingSize.y * 0.3f,
                    buildingSize.z * 0.5f // Along the edge
                );
                
                GameObject pipe = Instantiate(steamPipePrefab, pipePos, 
                    Quaternion.Euler(0, Random.Range(0, 360), 0));
                pipe.transform.SetParent(buildingObject.transform);
                pipe.name = "SteampunkPipe";
            }
        }
        
        /// <summary>
        /// Calculate centroid of polygon
        /// </summary>
        private Vector3 CalculateCentroid(List<Vector3> points)
        {
            Vector3 centroid = Vector3.zero;
            foreach (Vector3 point in points)
            {
                centroid += point;
            }
            return centroid / points.Count;
        }
        
        private void CreateSteamEmitter(Vector3 position)
        {
            GameObject steamEmitter = new GameObject("SteamEmitter");
            steamEmitter.transform.position = position;
            
            ParticleSystem particles = steamEmitter.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            main.startLifetime = 3f;
            main.startSpeed = 2f;
            main.maxParticles = 50;
            
            var emission = particles.emission;
            emission.rateOverTime = 10f;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;

            steamEmitter.transform.SetParent(mapContainer);

            // TODO: Pool steam emitters to reduce allocations during regeneration
        }
        
        private IEnumerator ApplyMeshBatching()
        {
            Debug.Log("[MapGenerator] Applying mesh batching for performance...");
            
            // TODO: Implement mesh batching by road type
            // This would combine multiple road segments of the same type into single meshes
            // for better performance
            
            yield return null;
        }
        
        private IEnumerator AddSteampunkAtmosphere()
        {
            Debug.Log("[MapGenerator] Adding Steampunk atmosphere...");
            
            // Add ambient lighting adjustments
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
                GameObject containerGO = new GameObject("GeneratedMap");
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
            
            InitializeMeshCollections();
        }
        
        public Vector3 GetPlayerSpawnPosition()
        {
            return playerSpawnPosition;
        }
        
        public OSMMapData GetCurrentMapData()
        {
            return currentMapData;
        }
        
        public bool IsGenerating()
        {
            return isGenerating;
        }
        
        /// <summary>
        /// Get statistics about the current generated map
        /// </summary>
        public string GetMapStatistics()
        {
            if (currentMapData == null)
                return "No map data available";
                
            return $"Map Statistics:\n" +
                   $"- Roads: {currentMapData.roads.Count}\n" +
                   $"- Buildings: {currentMapData.buildings.Count}\n" +
                   $"- Areas: {currentMapData.areas.Count}\n" +
                   $"- POIs: {currentMapData.pointsOfInterest.Count}\n" +
                   $"- Scale: {currentMapData.scaleMultiplier:F2}";
        }
    }
}
