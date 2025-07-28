using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace RollABall.Map
{
    /// <summary>
    /// ORIGINAL VERSION - Generates Unity GameObjects from OpenStreetMap data
    /// This is the backup version before segmented road generation was implemented
    /// Applies Steampunk theming and integrates with existing Roll-a-Ball systems
    /// </summary>
    public class MapGeneratorOriginal : MonoBehaviour
    {
        [Header("Generation Prefabs")]
        [SerializeField] private GameObject roadPrefab;
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private GameObject areaPrefab;
        [SerializeField] private GameObject collectiblePrefab;
        [SerializeField] private GameObject goalZonePrefab;
        [SerializeField] private GameObject playerPrefab;
        
        [Header("Steampunk Materials")]
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material buildingMaterial;
        [SerializeField] private Material parkMaterial;
        [SerializeField] private Material waterMaterial;
        
        [Header("Generation Settings")]
        [SerializeField] private float roadWidth = 2.0f;
        [SerializeField] private float buildingHeightMultiplier = 1.0f;
        [SerializeField] private int collectiblesPerBuilding = 2;
        [SerializeField] private bool enableSteampunkEffects = true;
        [SerializeField] private LayerMask groundLayer = 1;
        
        [Header("Performance Settings")]
        [SerializeField] private bool useBatching = true;
        [SerializeField] private int maxBuildingsPerFrame = 5;
        
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
        private List<CombineInstance> roadMeshes = new List<CombineInstance>();
        private List<CombineInstance> buildingMeshes = new List<CombineInstance>();
        
        private void Awake()
        {
            CreateMapContainer();
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
            
            Debug.Log("[MapGenerator] Starting map generation...");
            
            bool hasError = false;
            string errorMessage = "";
            
            try
            {
                // Clear existing content
                ClearExistingMap();
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
                
                // Generate roads
                yield return StartCoroutine(GenerateRoads());
                
                // Generate buildings
                yield return StartCoroutine(GenerateBuildings());
                
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
                
                Debug.Log("[MapGenerator] Map generation completed successfully");
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
        /// Generate road geometry from OSM way data
        /// </summary>
        private IEnumerator GenerateRoads()
        {
            Debug.Log($"[MapGenerator] Generating {currentMapData.roads.Count} roads...");
            
            Transform roadContainer = new GameObject("Roads").transform;
            roadContainer.SetParent(mapContainer);
            
            for (int i = 0; i < currentMapData.roads.Count; i++)
            {
                OSMWay road = currentMapData.roads[i];
                GenerateRoadFromWay(road, roadContainer);
                
                // Yield every few roads to prevent frame drops
                if (i % 3 == 0)
                    yield return null;
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Generate building geometry from OSM building data
        /// </summary>
        private IEnumerator GenerateBuildings()
        {
            Debug.Log($"[MapGenerator] Generating {currentMapData.buildings.Count} buildings...");
            
            Transform buildingContainer = new GameObject("Buildings").transform;
            buildingContainer.SetParent(mapContainer);
            
            int processed = 0;
            for (int i = 0; i < currentMapData.buildings.Count; i++)
            {
                OSMBuilding building = currentMapData.buildings[i];
                GenerateBuildingFromData(building, buildingContainer);
                
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
            
            // Update UI if LevelManager exists
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.UpdateCollectibleCount();
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
            // TODO-OPT#22: Shares logic with MapGenerator.PlaceGoalZone - consolidate into shared helper
            GameObject goalZone = Instantiate(goalZonePrefab, goalPosition, Quaternion.identity);
            goalZone.transform.SetParent(mapContainer);
            goalZone.name = "GoalZone";
            
            yield return null;
        }
        
        /// <summary>
        /// Generate road mesh from OSM way
        /// </summary>
        private void GenerateRoadFromWay(OSMWay road, Transform parent)
        {
            if (road.nodes.Count < 2) return;
            
            List<Vector3> roadPoints = new List<Vector3>();
            for (int i = 0; i < road.nodes.Count; i++)
            {
                Vector3 worldPos = currentMapData.LatLonToWorldPosition(road.nodes[i].lat, road.nodes[i].lon);
                roadPoints.Add(worldPos);
            }
            
            GameObject roadObject = CreateRoadMesh(roadPoints, roadWidth);
            roadObject.transform.SetParent(parent);
            roadObject.name = $"Road_{road.id}";
            
            // Apply road material
            if (roadMaterial != null)
            {
                roadObject.GetComponent<MeshRenderer>().material = roadMaterial;
            }
            
            // Add collider for physics
            roadObject.AddComponent<MeshCollider>();
        }
        
        /// <summary>
        /// Generate building mesh from OSM building data
        /// </summary>
        private void GenerateBuildingFromData(OSMBuilding building, Transform parent)
        {
            if (building.nodes.Count < 4) return; // Need at least 3 points + closing point
            
            List<Vector3> buildingPoints = new List<Vector3>();
            for (int i = 0; i < building.nodes.Count - 1; i++) // Skip last point (same as first)
            {
                Vector3 worldPos = currentMapData.LatLonToWorldPosition(building.nodes[i].lat, building.nodes[i].lon);
                buildingPoints.Add(worldPos);
            }
            
            GameObject buildingObject = CreateBuildingMesh(buildingPoints, building.height * buildingHeightMultiplier);
            buildingObject.transform.SetParent(parent);
            buildingObject.name = $"Building_{building.id}";
            
            // Apply building material
            if (buildingMaterial != null)
            {
                buildingObject.GetComponent<MeshRenderer>().material = buildingMaterial;
            }
            
            // Add collider
            buildingObject.AddComponent<MeshCollider>();
            
            // Add Steampunk elements
            if (enableSteampunkEffects)
            {
                AddSteampunkElementsToBuilding(buildingObject, building.buildingType);
            }
        }
        
        /// <summary>
        /// Generate area mesh (parks, water, etc.)
        /// </summary>
        private void GenerateAreaFromData(OSMArea area, Transform parent)
        {
            if (area.nodes.Count < 4) return;
            
            List<Vector3> areaPoints = new List<Vector3>();
            for (int i = 0; i < area.nodes.Count - 1; i++)
            {
                Vector3 worldPos = currentMapData.LatLonToWorldPosition(area.nodes[i].lat, area.nodes[i].lon);
                areaPoints.Add(worldPos);
            }
            
            GameObject areaObject = CreateAreaMesh(areaPoints);
            areaObject.transform.SetParent(parent);
            areaObject.name = $"Area_{area.areaType}_{area.id}";
            
            // Apply appropriate material based on area type
            Material areaMat = GetMaterialForAreaType(area.areaType);
            if (areaMat != null)
            {
                areaObject.GetComponent<MeshRenderer>().material = areaMat;
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
        
        private GameObject CreateRoadMesh(List<Vector3> points, float width)
        {
            // Simplified road mesh creation
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (points.Count >= 2)
            {
                Vector3 start = points[0];
                Vector3 end = points[points.Count - 1];
                Vector3 center = (start + end) / 2f;
                float length = Vector3.Distance(start, end);
                
                road.transform.position = center;
                road.transform.LookAt(end);
                road.transform.localScale = new Vector3(width, 0.1f, length);
            }
            return road;
        }
        
        private GameObject CreateBuildingMesh(List<Vector3> points, float height)
        {
            // Simplified building creation
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (points.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (Vector3 point in points)
                {
                    center += point;
                }
                center /= points.Count;
                center.y = height / 2f;
                
                building.transform.position = center;
                building.transform.localScale = new Vector3(5f, height, 5f);
            }
            return building;
        }
        
        private GameObject CreateAreaMesh(List<Vector3> points)
        {
            // Simplified area creation
            GameObject area = GameObject.CreatePrimitive(PrimitiveType.Plane);
            if (points.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (Vector3 point in points)
                {
                    center += point;
                }
                center /= points.Count;
                center.y = 0.1f;
                
                area.transform.position = center;
                area.transform.localScale = Vector3.one * 2f;
            }
            return area;
        }
        
        private Material GetMaterialForAreaType(string areaType)
        {
            return areaType switch
            {
                "park" or "grass" => parkMaterial,
                "water" => waterMaterial,
                _ => null
            };
        }
        
        private float CalculateBuildingArea(OSMBuilding building)
        {
            // Simplified area calculation
            return building.nodes.Count * 10f; // Rough approximation
        }
        
        private void AddSteampunkElementsToBuilding(GameObject building, string buildingType)
        {
            // Add steampunk decorations based on building type
            // This could include gears, pipes, steam emitters, etc.
            // For now, just add a simple particle effect for some buildings
            
            if (buildingType == "industrial" && Random.value < 0.3f)
            {
                // Add steam emitter
                CreateSteamEmitter(building.transform.position + Vector3.up * 5f);
            }
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
        }
        
        private IEnumerator ApplyMeshBatching()
        {
            Debug.Log("[MapGenerator] Applying mesh batching for performance...");
            // Mesh batching implementation would go here
            // For now, just yield to maintain the interface
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
            
            roadMeshes.Clear();
            buildingMeshes.Clear();
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
    }
}
