using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

namespace RollABall.Map
{
    /// <summary>
    /// Handles address geocoding and OSM data fetching
    /// Converts user-entered addresses to coordinates and retrieves map data
    /// </summary>
    public class AddressResolver : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string nominatimBaseUrl = "https://nominatim.openstreetmap.org";
        [SerializeField] private string overpassBaseUrl = "https://overpass-api.de/api/interpreter";
        // TODO: Move API URLs to a configuration asset for easier editing
        [SerializeField] private float searchRadius = 500.0f; // meters
        
        [Header("Request Settings")]
        [SerializeField] private float requestTimeout = 10.0f;
        [SerializeField] private string userAgent = "RollABallGame/1.0";
        
        [Header("Fallback Settings")]
        [SerializeField] private bool enableFallbackMode = true;
        [SerializeField] private double fallbackLat = 52.5217; // Berlin - Brandenburger Tor
        [SerializeField] private double fallbackLon = 13.4132;
        
        [Header("Debug Settings")]
        [SerializeField] private bool useSimpleQuery = false; // Use full query for production
        [SerializeField] private bool useHardcodedCoordinates = false; // Use real coordinates for production
        [SerializeField] private bool enableBoundingBoxValidation = true; // Extra validation
        
        // Events for communication with other systems
        public event Action<GeocodeResult> OnAddressResolved;
        public event Action<OSMMapData> OnMapDataLoaded;
        public event Action<string> OnError;
        public event Action<string> OnMapLoadErrorEvent;

        private Coroutine currentRequest;
        [SerializeField] private bool enableGeocodeCache = true;
        private readonly Dictionary<string, GeocodeResult> geocodeCache = new();
        
        /// <summary>
        /// Result of address geocoding
        /// </summary>
        [System.Serializable]
        public class GeocodeResult
        {
            public string displayName;
            public double lat;
            public double lon;
            public OSMBounds boundingBox;
            
            public bool IsValid()
            {
                return !string.IsNullOrEmpty(displayName) && boundingBox != null && boundingBox.IsValid();
            }
        }
        
        /// <summary>
        /// Resolve an address to coordinates and fetch surrounding map data
        /// </summary>
        public void ResolveAddressAndLoadMap(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                OnError?.Invoke("Bitte geben Sie eine gültige Adresse ein.");
                return;
            }
            
            // Stop any existing request
            if (currentRequest != null)
            {
                StopCoroutine(currentRequest);
            }
            
            currentRequest = StartCoroutine(ResolveAddressCoroutine(address));
        }
        
        /// <summary>
        /// Load map data directly from coordinates
        /// </summary>
        public void LoadMapFromCoordinates(double lat, double lon, float radius = 500.0f)
        {
            if (currentRequest != null)
            {
                StopCoroutine(currentRequest);
            }
            
            currentRequest = StartCoroutine(LoadMapDataCoroutine(lat, lon, radius));
        }
        
        /// <summary>
        /// Geocoding coroutine - converts address to coordinates
        /// </summary>
        private IEnumerator ResolveAddressCoroutine(string address)
        {
            Debug.Log($"[AddressResolver] Resolving address: {address}");

            if (enableGeocodeCache && geocodeCache.TryGetValue(address, out GeocodeResult cached))
            {
                Debug.Log($"[AddressResolver] Using cached geocode for {address}");
                OnAddressResolved?.Invoke(cached);
                yield return LoadMapDataCoroutine(cached.lat, cached.lon, searchRadius);
                yield break;
            }

            // Build Nominatim search URL
            string encodedAddress = UnityWebRequest.EscapeURL(address);
            string geocodeUrl = $"{nominatimBaseUrl}/search?q={encodedAddress}&format=json&limit=1&addressdetails=1&extratags=1";
            
            using (UnityWebRequest request = UnityWebRequest.Get(geocodeUrl))
            {
                // Set headers
                request.SetRequestHeader("User-Agent", userAgent);
                request.timeout = (int)requestTimeout;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    GeocodeResult result = ParseGeocodeResponse(response);
                    
                    if (result != null && result.IsValid())
                    {
                        Debug.Log($"[AddressResolver] Address resolved: {result.displayName} at {result.lat}, {result.lon}");

                        if (enableGeocodeCache)
                            geocodeCache[address] = result;

                        OnAddressResolved?.Invoke(result);

                        // Now fetch map data for this location
                        yield return LoadMapDataCoroutine(result.lat, result.lon, searchRadius);
                    }
                    else
                    {
                        HandleGeocodeError($"Adresse '{address}' konnte nicht gefunden werden.");
                    }
                }
                else
                {
                    Debug.LogError($"[AddressResolver] Geocoding failed: {request.error}");
                    HandleGeocodeError($"Fehler beim Auflösen der Adresse: {request.error}");
                }
            }
        }
        
        /// <summary>
        /// Load OSM map data for given coordinates with robust coordinate validation
        /// </summary>
        private IEnumerator LoadMapDataCoroutine(double lat, double lon, float radius)
        {
            Debug.Log($"[AddressResolver] === NEW REQUEST STARTING ===");
            Debug.Log($"[AddressResolver] Loading map data for {lat}, {lon} with radius {radius}m");
            Debug.Log($"[AddressResolver] useHardcodedCoordinates: {useHardcodedCoordinates}");
            Debug.Log($"[AddressResolver] useSimpleQuery: {useSimpleQuery}");
            
            // TESTING: Use completely hardcoded coordinates if enabled
            if (useHardcodedCoordinates)
            {
                Debug.Log("[AddressResolver] Using HARDCODED coordinates for testing");
                // Leipzig city center with very small, safe bounding box
                double hardMinLat = 51.330;
                double hardMaxLat = 51.340;
                double hardMinLon = 12.370;
                double hardMaxLon = 12.380;
                
                Debug.Log($"[AddressResolver] HARDCODED bounding box: {hardMinLat},{hardMinLon},{hardMaxLat},{hardMaxLon}");
                
                // Build query with hardcoded safe coordinates
                string hardcodedQuery = BuildHardcodedTestQuery(hardMinLat, hardMaxLat, hardMinLon, hardMaxLon);
                yield return StartCoroutine(ExecuteOverpassQuery(hardcodedQuery, hardMinLat, hardMaxLat, hardMinLon, hardMaxLon));
                yield break;
            }
            
            // Validate coordinates and calculate bounding box (outside of coroutine context)
            OSMBounds validatedBounds = ValidateAndCalculateBounds(lat, lon, radius);
            
            if (validatedBounds == null)
            {
                // Error already handled in ValidateAndCalculateBounds
                yield break;
            }
            
            Debug.Log($"[AddressResolver] Validated bounding box: lat[{validatedBounds.minLat:F6}, {validatedBounds.maxLat:F6}], lon[{validatedBounds.minLon:F6}, {validatedBounds.maxLon:F6}]");
            
            // Build query with validated coordinates
            string query = useSimpleQuery ? 
                BuildSimpleOverpassQuery(validatedBounds.minLat, validatedBounds.maxLat, validatedBounds.minLon, validatedBounds.maxLon) : 
                BuildOverpassQuery(validatedBounds.minLat, validatedBounds.maxLat, validatedBounds.minLon, validatedBounds.maxLon);
            
            yield return StartCoroutine(ExecuteOverpassQuery(query, validatedBounds.minLat, validatedBounds.maxLat, validatedBounds.minLon, validatedBounds.maxLon));
        }
        
        /// <summary>
        /// Validates coordinates and calculates safe bounding box (non-coroutine method)
        /// </summary>
        private OSMBounds ValidateAndCalculateBounds(double lat, double lon, float radius)
        {
            try
            {
                // Check if bounding box validation is enabled
                if (!enableBoundingBoxValidation)
                {
                    Debug.LogWarning("[AddressResolver] BoundingBox validation is DISABLED - using basic calculation");
                    // Use simple calculation without validation (legacy mode)
                    return CalculateBasicBounds(lat, lon, radius);
                }
                
                // Validate and clamp input coordinates using new validator
                if (!CoordinateValidator.IsValidCoordinate(lat, lon))
                {
                    Debug.LogError($"[AddressResolver] Invalid input coordinates: lat={lat}, lon={lon}");
                    HandleMapDataError("Ungültige Koordinaten.");
                    return null;
                }
                
                // Use the robust coordinate validator to calculate safe bounding box
                OSMBounds bounds = CoordinateValidator.CalculateSafeBoundingBox(lat, lon, radius);
                
                // Final validation of the calculated bounding box
                if (!CoordinateValidator.ValidateBoundingBox(bounds))
                {
                    Debug.LogError($"[AddressResolver] Bounding box validation failed after calculation");
                    HandleMapDataError("Konnte keine gültige Bounding Box berechnen.");
                    return null;
                }
                
                return bounds;
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"[AddressResolver] Coordinate validation error: {ex.Message}");
                HandleMapDataError($"Koordinaten-Fehler: {ex.Message}");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError($"[AddressResolver] Bounding box calculation error: {ex.Message}");
                HandleMapDataError($"Bounding Box Fehler: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressResolver] Unexpected error in coordinate processing: {ex.Message}");
                HandleMapDataError($"Unerwarteter Fehler bei Koordinaten-Verarbeitung: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Execute the Overpass API query
        /// </summary>
        private IEnumerator ExecuteOverpassQuery(string overpassQuery, double minLat, double maxLat, double minLon, double maxLon)
        {
            Debug.Log($"[AddressResolver] Executing Overpass query");
            // TODO: Throttle requests and cache results to avoid API rate limits
            Debug.Log($"[AddressResolver] Query:\n{overpassQuery}");
            
            // Use proper form data format for Overpass API
            WWWForm form = new WWWForm();
            form.AddField("data", overpassQuery);
            
            using (UnityWebRequest request = UnityWebRequest.Post(overpassBaseUrl, form))
            {
                request.SetRequestHeader("User-Agent", userAgent);
                request.timeout = (int)(requestTimeout * 2);
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    Debug.Log($"[AddressResolver] SUCCESS! Received response of {response.Length} characters");
                    
                    // Log a snippet of the response for debugging
                    string responseSnippet = response.Length > 200 ? response.Substring(0, 200) + "..." : response;
                    Debug.Log($"[AddressResolver] Response snippet: {responseSnippet}");
                    
                    OSMMapData mapData = ParseOSMResponse(response, minLat, maxLat, minLon, maxLon);
                    
                    if (mapData != null && mapData.IsValid())
                    {
                        Debug.Log($"[AddressResolver] Map data loaded successfully. Roads: {mapData.roads.Count}, Buildings: {mapData.buildings.Count}, Areas: {mapData.areas.Count}, POIs: {mapData.pointsOfInterest.Count}");
                        OnMapDataLoaded?.Invoke(mapData);
                    }
                    else
                    {
                        Debug.LogWarning("[AddressResolver] No valid map data found, creating fallback data");
                        mapData = CreateFallbackMapData(minLat, maxLat, minLon, maxLon);
                        OnMapDataLoaded?.Invoke(mapData);
                    }
                }
                else
                {
                    Debug.LogError($"[AddressResolver] Map data request failed: {request.error}");
                    Debug.LogError($"[AddressResolver] Response Code: {request.responseCode}");
                    
                    // Log the COMPLETE error response for debugging
                    if (!string.IsNullOrEmpty(request.downloadHandler.text))
                    {
                        string fullErrorResponse = request.downloadHandler.text;
                        Debug.LogError($"[AddressResolver] FULL ERROR RESPONSE:\n{fullErrorResponse}");
                        
                        // Try to extract the actual error message from XML
                        if (fullErrorResponse.Contains("<p>") && fullErrorResponse.Contains("</p>"))
                        {
                            int startIdx = fullErrorResponse.IndexOf("<p>") + 3;
                            int endIdx = fullErrorResponse.IndexOf("</p>", startIdx);
                            if (endIdx > startIdx)
                            {
                                string errorMsg = fullErrorResponse.Substring(startIdx, endIdx - startIdx);
                                Debug.LogError($"[AddressResolver] Extracted error: {errorMsg}");
                            }
                        }
                    }
                    
                    HandleMapDataError($"Fehler beim Laden der Kartendaten: {request.error}");
                }
            }
        }
        
        /// <summary>
        /// Build a hardcoded test query with known-good coordinates
        /// </summary>
        private string BuildHardcodedTestQuery(double minLat, double maxLat, double minLon, double maxLon)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("[out:json][timeout:25];");
            query.AppendLine("(");
            
            // Use exactly these coordinates: Leipzig city center, very small area
            string bbox = $"{minLat:F3},{minLon:F3},{maxLat:F3},{maxLon:F3}";
            
            // Just get buildings in this tiny area
            query.AppendLine($"  way[building]({bbox});");
            
            query.AppendLine(");");
            query.AppendLine("out geom;");
            
            return query.ToString();
        }
        
        /// <summary>
        /// Parse geocoding response from Nominatim
        /// </summary>
        private GeocodeResult ParseGeocodeResponse(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json) || json == "[]")
                {
                    return null;
                }
                
                // Use Newtonsoft.Json for better parsing
                JArray results = JArray.Parse(json);
                if (results.Count == 0)
                {
                    return null;
                }
                
                JObject firstResult = results[0] as JObject;
                GeocodeResult result = new GeocodeResult();
                
                result.displayName = firstResult["display_name"]?.ToString() ?? "";
                result.lat = (double)(firstResult["lat"] ?? 0.0);
                result.lon = (double)(firstResult["lon"] ?? 0.0);
                
                // Create bounding box
                double radius = searchRadius / 111320.0;
                result.boundingBox = new OSMBounds(
                    result.lat - radius, result.lat + radius,
                    result.lon - radius, result.lon + radius
                );
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressResolver] Error parsing geocode response: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Build a simple Overpass QL query for testing
        /// </summary>
        private string BuildSimpleOverpassQuery(double minLat, double maxLat, double minLon, double maxLon)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("[out:json][timeout:25];");
            query.AppendLine("(");
            
            // Format: (minLat,minLon,maxLat,maxLon) 
            string bbox = $"{minLat:F6},{minLon:F6},{maxLat:F6},{maxLon:F6}";
            
            // Just get a few buildings for testing
            query.AppendLine($"  way[building]({bbox});");
            
            query.AppendLine(");");
            query.AppendLine("out geom;");
            
            return query.ToString();
        }
        
        /// <summary>
        /// Build improved Overpass QL query for map data with proper syntax
        /// </summary>
        private string BuildOverpassQuery(double minLat, double maxLat, double minLon, double maxLon)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("[out:json][timeout:25];");
            query.AppendLine("(");
            
            // Format: (minLat,minLon,maxLat,maxLon)
            string bbox = $"{minLat:F6},{minLon:F6},{maxLat:F6},{maxLon:F6}";
            
            // Major roads and highways only to avoid too much data - FIXED REGEX
            query.AppendLine($"  way[highway~\"^(motorway|trunk|primary|secondary|tertiary|residential|pedestrian|footway)$\"]({bbox});");
            
            // Buildings
            query.AppendLine($"  way[building]({bbox});");
            
            // Areas (parks, water, etc.) - limit to major ones - FIXED REGEX
            query.AppendLine($"  way[leisure~\"^(park|playground|sports_centre)$\"]({bbox});");
            query.AppendLine($"  way[natural~\"^(water|forest|grass)$\"]({bbox});");
            query.AppendLine($"  way[landuse~\"^(residential|commercial|industrial)$\"]({bbox});");
            
            // Important POIs only - FIXED REGEX
            query.AppendLine($"  node[amenity~\"^(restaurant|cafe|school|hospital|bank|fuel|parking)$\"]({bbox});");
            
            query.AppendLine(");");
            query.AppendLine("out geom;");
            
            return query.ToString();
        }
        
        /// <summary>
        /// Create fallback map data when OSM data is not available
        /// </summary>
        private OSMMapData CreateFallbackMapData(double minLat, double maxLat, double minLon, double maxLon)
        {
            Debug.Log("[AddressResolver] Creating fallback map data");
            
            OSMMapData mapData = new OSMMapData(minLat, maxLat, minLon, maxLon);
            
            double centerLat = (minLat + maxLat) / 2.0;
            double centerLon = (minLon + maxLon) / 2.0;
            
            // Create a simple fallback road network
            for (int i = 0; i < 3; i++)
            {
                OSMWay road = new OSMWay(i + 1);
                road.wayType = "highway";
                road.tags["highway"] = "residential";
                
                double latOffset = (maxLat - minLat) * 0.3 * (i - 1);
                
                road.nodes.Add(new OSMNode(i * 10 + 1, centerLat + latOffset, minLon + (maxLon - minLon) * 0.1));
                road.nodes.Add(new OSMNode(i * 10 + 2, centerLat + latOffset, maxLon - (maxLon - minLon) * 0.1));
                
                mapData.roads.Add(road);
            }
            
            // Create some fallback buildings
            for (int i = 0; i < 5; i++)
            {
                OSMBuilding building = new OSMBuilding(i + 100);
                building.buildingType = "residential";
                building.height = 3.0f + i;
                
                double buildingLat = centerLat + ((i - 2) * 0.0005);
                double buildingLon = centerLon + ((i % 2 == 0 ? 1 : -1) * 0.0003);
                
                // Create a simple rectangular building
                building.nodes.Add(new OSMNode(i * 10 + 100, buildingLat - 0.0001, buildingLon - 0.0001));
                building.nodes.Add(new OSMNode(i * 10 + 101, buildingLat + 0.0001, buildingLon - 0.0001));
                building.nodes.Add(new OSMNode(i * 10 + 102, buildingLat + 0.0001, buildingLon + 0.0001));
                building.nodes.Add(new OSMNode(i * 10 + 103, buildingLat - 0.0001, buildingLon + 0.0001));
                building.nodes.Add(new OSMNode(i * 10 + 100, buildingLat - 0.0001, buildingLon + 0.0001)); // Close the way
                
                mapData.buildings.Add(building);
            }
            
            return mapData;
        }
        
        /// <summary>
        /// Parse OSM data response from Overpass API using Newtonsoft.Json
        /// This replaces the placeholder implementation with real OSM data processing
        /// </summary>
        private OSMMapData ParseOSMResponse(string json, double minLat, double maxLat, double minLon, double maxLon)
        {
            try
            {
                Debug.Log($"[AddressResolver] Parsing real OSM response from Overpass API...");
                
                // Create map data container
                OSMMapData mapData = new OSMMapData(minLat, maxLat, minLon, maxLon);
                
                // Parse JSON using Newtonsoft.Json
                JObject overpassData = JObject.Parse(json);
                JArray elements = overpassData["elements"] as JArray;
                
                if (elements == null || elements.Count == 0)
                {
                    Debug.LogWarning("[AddressResolver] No elements found in Overpass response");
                    return null;
                }
                
                // Dictionary to store nodes by ID for way processing
                Dictionary<long, OSMNode> nodesById = new Dictionary<long, OSMNode>();
                
                Debug.Log($"[AddressResolver] Processing {elements.Count} elements from Overpass API");
                
                // First pass: Process all nodes
                foreach (JToken element in elements)
                {
                    string type = element["type"]?.ToString();
                    
                    if (type == "node")
                    {
                        ProcessOSMNode(element, nodesById, mapData);
                    }
                }
                
                // Second pass: Process ways (need nodes to be processed first)
                foreach (JToken element in elements)
                {
                    string type = element["type"]?.ToString();
                    
                    if (type == "way")
                    {
                        ProcessOSMWay(element, nodesById, mapData);
                    }
                    // Note: Relations are not processed in this version but could be added here
                }
                
                // Validate and set appropriate scale based on area size
                double areaWidth = maxLon - minLon;
                mapData.scaleMultiplier = (float)(1000.0 / (areaWidth * 111320.0)); // Scale to reasonable Unity size
                
                Debug.Log($"[AddressResolver] Successfully parsed OSM data: {mapData.roads.Count} roads, {mapData.buildings.Count} buildings, {mapData.areas.Count} areas, {mapData.pointsOfInterest.Count} POIs");
                
                return mapData;
            }
            catch (Newtonsoft.Json.JsonException jsonEx)
            {
                Debug.LogError($"[AddressResolver] JSON parsing error: {jsonEx.Message}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressResolver] Error parsing OSM response: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Process a single OSM node from JSON data
        /// </summary>
        private void ProcessOSMNode(JToken nodeElement, Dictionary<long, OSMNode> nodesById, OSMMapData mapData)
        {
            try
            {
                long id = (long)(nodeElement["id"] ?? 0);
                double lat = (double)(nodeElement["lat"] ?? 0.0);
                double lon = (double)(nodeElement["lon"] ?? 0.0);
                
                if (id == 0) return;
                
                OSMNode node = new OSMNode(id, lat, lon);
                
                // Process tags
                JObject tags = nodeElement["tags"] as JObject;
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        node.tags[tag.Key] = tag.Value?.ToString() ?? "";
                    }
                    
                    // Set name if available
                    if (node.tags.ContainsKey("name"))
                    {
                        node.name = node.tags["name"];
                    }
                    
                    // Check if this node is a point of interest
                    if (node.HasTag("amenity") || node.HasTag("shop"))
                    {
                        mapData.pointsOfInterest.Add(node);
                        Debug.Log($"[AddressResolver] Added POI: {node.name ?? node.GetTag("amenity", node.GetTag("shop", "unknown"))} at {lat}, {lon}");
                    }
                }
                
                // Store node for way processing
                nodesById[id] = node;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AddressResolver] Error processing node: {e.Message}");
            }
        }
        
        /// <summary>
        /// Process a single OSM way from JSON data
        /// </summary>
        private void ProcessOSMWay(JToken wayElement, Dictionary<long, OSMNode> nodesById, OSMMapData mapData)
        {
            try
            {
                long id = (long)(wayElement["id"] ?? 0);
                if (id == 0) return;
                
                // Get node references
                JArray nodeRefs = wayElement["nodes"] as JArray;
                if (nodeRefs == null || nodeRefs.Count < 2) return;
                
                // Process tags to determine way type
                JObject tags = wayElement["tags"] as JObject;
                if (tags == null) return;
                
                Dictionary<string, string> wayTags = new Dictionary<string, string>();
                foreach (var tag in tags)
                {
                    wayTags[tag.Key] = tag.Value?.ToString() ?? "";
                }
                
                // Determine way type and create appropriate object
                if (wayTags.ContainsKey("highway"))
                {
                    ProcessRoadWay(id, nodeRefs, wayTags, nodesById, mapData);
                }
                else if (wayTags.ContainsKey("building"))
                {
                    ProcessBuildingWay(id, nodeRefs, wayTags, nodesById, mapData);
                }
                else if (wayTags.ContainsKey("leisure") || wayTags.ContainsKey("natural") || wayTags.ContainsKey("landuse"))
                {
                    ProcessAreaWay(id, nodeRefs, wayTags, nodesById, mapData);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AddressResolver] Error processing way: {e.Message}");
            }
        }
        
        /// <summary>
        /// Process a way as a road/highway
        /// </summary>
        private void ProcessRoadWay(long id, JArray nodeRefs, Dictionary<string, string> tags, Dictionary<long, OSMNode> nodesById, OSMMapData mapData)
        {
            OSMWay road = new OSMWay(id);
            road.tags = tags;
            road.wayType = "highway";
            
            // Add nodes to the road
            foreach (var nodeRef in nodeRefs)
            {
                long nodeId = (long)nodeRef;
                if (nodesById.ContainsKey(nodeId))
                {
                    road.nodes.Add(nodesById[nodeId]);
                }
            }
            
            if (road.nodes.Count >= 2)
            {
                mapData.roads.Add(road);
                Debug.Log($"[AddressResolver] Added road: {road.GetTag("highway", "unknown")} with {road.nodes.Count} nodes");
            }
        }
        
        /// <summary>
        /// Process a way as a building
        /// </summary>
        private void ProcessBuildingWay(long id, JArray nodeRefs, Dictionary<string, string> tags, Dictionary<long, OSMNode> nodesById, OSMMapData mapData)
        {
            OSMBuilding building = new OSMBuilding(id);
            building.tags = tags;
            
            // Add nodes to the building
            foreach (var nodeRef in nodeRefs)
            {
                long nodeId = (long)nodeRef;
                if (nodesById.ContainsKey(nodeId))
                {
                    building.nodes.Add(nodesById[nodeId]);
                }
            }
            
            if (building.nodes.Count >= 3) // Need at least 3 nodes for a building
            {
                building.CalculateHeight();
                mapData.buildings.Add(building);
                Debug.Log($"[AddressResolver] Added building: {building.buildingType} with height {building.height}m and {building.nodes.Count} nodes");
            }
        }
        
        /// <summary>
        /// Process a way as an area (park, water, etc.)
        /// </summary>
        private void ProcessAreaWay(long id, JArray nodeRefs, Dictionary<string, string> tags, Dictionary<long, OSMNode> nodesById, OSMMapData mapData)
        {
            OSMArea area = new OSMArea(id);
            area.tags = tags;
            
            // Add nodes to the area
            foreach (var nodeRef in nodeRefs)
            {
                long nodeId = (long)nodeRef;
                if (nodesById.ContainsKey(nodeId))
                {
                    area.nodes.Add(nodesById[nodeId]);
                }
            }
            
            if (area.nodes.Count >= 3) // Need at least 3 nodes for an area
            {
                area.DetermineAreaType();
                mapData.areas.Add(area);
                Debug.Log($"[AddressResolver] Added area: {area.areaType} with {area.nodes.Count} nodes");
            }
        }
        
        /// <summary>
        /// Handle geocode errors with fallback support
        /// </summary>
        private void HandleGeocodeError(string errorMessage)
        {
            if (enableFallbackMode)
            {
                Debug.Log($"[AddressResolver] Geocode error, using fallback location: {errorMessage}");
                OnError?.Invoke(errorMessage + " Lade Fallback-Karte...");
                StartCoroutine(LoadMapDataCoroutine(fallbackLat, fallbackLon, searchRadius));
            }
            else
            {
                OnError?.Invoke(errorMessage);
            }
        }
        
        /// <summary>
        /// Handle map data errors with fallback support
        /// </summary>
        private void HandleMapDataError(string errorMessage)
        {
            if (enableFallbackMode)
            {
                Debug.Log($"[AddressResolver] Map data error, using fallback location: {errorMessage}");
                OnMapLoadErrorEvent?.Invoke(errorMessage + " Lade Fallback-Karte...");
                StartCoroutine(LoadMapDataCoroutine(fallbackLat, fallbackLon, searchRadius));
            }
            else
            {
                OnMapLoadErrorEvent?.Invoke(errorMessage);
            }
        }
        
        /// <summary>
        /// Calculate basic bounding box without advanced validation (legacy mode)
        /// </summary>
        private OSMBounds CalculateBasicBounds(double lat, double lon, float radius)
        {
            double radiusInDegrees = radius / 111320.0;
            double latRadiusInDegrees = radiusInDegrees;
            double lonRadiusInDegrees = radiusInDegrees / Math.Cos(lat * Math.PI / 180.0);
            
            double minLat = lat - latRadiusInDegrees;
            double maxLat = lat + latRadiusInDegrees;
            double minLon = lon - lonRadiusInDegrees;
            double maxLon = lon + lonRadiusInDegrees;
            
            return new OSMBounds(minLat, maxLat, minLon, maxLon);
        }
        
        /// <summary>
        /// Validate bounding box coordinates
        /// </summary>
        private bool ValidateBoundingBox(double minLat, double maxLat, double minLon, double maxLon, double centerLat, double centerLon)
        {
            // Check if coordinates are within valid ranges
            if (minLat < -90.0 || maxLat > 90.0 || minLon < -180.0 || maxLon > 180.0)
            {
                Debug.LogError($"[AddressResolver] Coordinates out of valid range: lat[{minLat:F6}, {maxLat:F6}], lon[{minLon:F6}, {maxLon:F6}]");
                return false;
            }
            
            // Check if bounding box makes sense
            if (minLat >= maxLat || minLon >= maxLon)
            {
                Debug.LogError($"[AddressResolver] Invalid bounding box dimensions");
                return false;
            }
            
            // Check if center point is within bounding box
            if (centerLat < minLat || centerLat > maxLat || centerLon < minLon || centerLon > maxLon)
            {
                Debug.LogError($"[AddressResolver] Center point outside bounding box");
                return false;
            }
            
            // Check if bounding box is reasonable size (not too large)
            double latSpan = maxLat - minLat;
            double lonSpan = maxLon - minLon;
            
            if (latSpan > 10.0 || lonSpan > 10.0) // 10 degrees = ~1100km
            {
                Debug.LogWarning($"[AddressResolver] Very large bounding box: {latSpan:F3}° lat, {lonSpan:F3}° lon");
                // Don't fail, just warn
            }
            
            Debug.Log($"[AddressResolver] Bounding box validation passed");
            return true;
        }
        
        /// <summary>
        /// Stop any ongoing requests
        /// </summary>
        public void CancelCurrentRequest()
        {
            if (currentRequest != null)
            {
                StopCoroutine(currentRequest);
                currentRequest = null;
            }
        }
        
        private void OnDestroy()
        {
            CancelCurrentRequest();
        }
    }
}
