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
        [SerializeField] private float searchRadius = 500.0f; // meters
        
        [Header("Request Settings")]
        [SerializeField] private float requestTimeout = 10.0f;
        [SerializeField] private string userAgent = "RollABallGame/1.0";
        
        [Header("Fallback Settings")]
        [SerializeField] private bool enableFallbackMode = true;
        [SerializeField] private double fallbackLat = 52.5217; // Berlin - Brandenburger Tor
        [SerializeField] private double fallbackLon = 13.4132;
        
        // Events for communication with other systems
        public event Action<GeocodeResult> OnAddressResolved;
        public event Action<OSMMapData> OnMapDataLoaded;
        public event Action<string> OnError;
        public event Action<string> OnMapLoadErrorEvent;
        
        private Coroutine currentRequest;
        
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
        /// Load OSM map data for given coordinates
        /// </summary>
        private IEnumerator LoadMapDataCoroutine(double lat, double lon, float radius)
        {
            Debug.Log($"[AddressResolver] Loading map data for {lat}, {lon} with radius {radius}m");
            
            // Calculate bounding box with proper lat/lon correction
            double radiusInDegrees = radius / 111320.0; // Approximate conversion meters to degrees
            double latRadiusInDegrees = radiusInDegrees;
            double lonRadiusInDegrees = radiusInDegrees / Math.Cos(lat * Math.PI / 180.0); // Correct for latitude
            
            double minLat = lat - latRadiusInDegrees;
            double maxLat = lat + latRadiusInDegrees;
            double minLon = lon - lonRadiusInDegrees;
            double maxLon = lon + lonRadiusInDegrees;
            
            // Build Overpass query
            string overpassQuery = BuildOverpassQuery(minLat, maxLat, minLon, maxLon);
            
            using (UnityWebRequest request = UnityWebRequest.Post(overpassBaseUrl, overpassQuery, "application/x-www-form-urlencoded"))
            {
                request.SetRequestHeader("User-Agent", userAgent);
                request.timeout = (int)(requestTimeout * 2); // Map data requests may take longer
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    OSMMapData mapData = ParseOSMResponse(response, minLat, maxLat, minLon, maxLon);
                    
                    if (mapData != null && mapData.IsValid())
                    {
                        Debug.Log($"[AddressResolver] Map data loaded successfully. Roads: {mapData.roads.Count}, Buildings: {mapData.buildings.Count}, Areas: {mapData.areas.Count}, POIs: {mapData.pointsOfInterest.Count}");
                        OnMapDataLoaded?.Invoke(mapData);
                    }
                    else
                    {
                        HandleMapDataError("Keine Kartendaten für diese Region verfügbar.");
                    }
                }
                else
                {
                    Debug.LogError($"[AddressResolver] Map data request failed: {request.error}");
                    HandleMapDataError($"Fehler beim Laden der Kartendaten: {request.error}");
                }
            }
        }
        
        /// <summary>
        /// Parse geocoding response from Nominatim
        /// </summary>
        private GeocodeResult ParseGeocodeResponse(string json)
        {
            try
            {
                // Simple JSON parsing for geocoding result
                // In a production environment, you might want to use a proper JSON library
                if (json.StartsWith("[") && json.Length > 10)
                {
                    // Remove array brackets and get first result
                    json = json.Substring(1, json.Length - 2);
                    
                    GeocodeResult result = new GeocodeResult();
                    
                    // Extract display name
                    int displayStart = json.IndexOf("\"display_name\":\"") + 16;
                    int displayEnd = json.IndexOf("\",", displayStart);
                    if (displayStart > 15 && displayEnd > displayStart)
                    {
                        result.displayName = json.Substring(displayStart, displayEnd - displayStart);
                    }
                    
                    // Extract latitude
                    int latStart = json.IndexOf("\"lat\":\"") + 7;
                    int latEnd = json.IndexOf("\"", latStart);
                    if (latStart > 6 && latEnd > latStart)
                    {
                        double.TryParse(json.Substring(latStart, latEnd - latStart), out result.lat);
                    }
                    
                    // Extract longitude
                    int lonStart = json.IndexOf("\"lon\":\"") + 7;
                    int lonEnd = json.IndexOf("\"", lonStart);
                    if (lonStart > 6 && lonEnd > lonStart)
                    {
                        double.TryParse(json.Substring(lonStart, lonEnd - lonStart), out result.lon);
                    }
                    
                    // Create bounding box
                    double radius = searchRadius / 111320.0;
                    result.boundingBox = new OSMBounds(
                        result.lat - radius, result.lat + radius,
                        result.lon - radius, result.lon + radius
                    );
                    
                    return result;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressResolver] Error parsing geocode response: {e.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Build Overpass QL query for map data
        /// </summary>
        private string BuildOverpassQuery(double minLat, double maxLat, double minLon, double maxLon)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("[out:json][timeout:25];");
            query.AppendLine("(");
            
            // Roads/highways
            query.AppendLine($"  way[\"highway\"]({minLat},{minLon},{maxLat},{maxLon});");
            
            // Buildings
            query.AppendLine($"  way[\"building\"]({minLat},{minLon},{maxLat},{maxLon});");
            
            // Areas (parks, water, etc.)
            query.AppendLine($"  way[\"leisure\"]({minLat},{minLon},{maxLat},{maxLon});");
            query.AppendLine($"  way[\"natural\"]({minLat},{minLon},{maxLat},{maxLon});");
            query.AppendLine($"  way[\"landuse\"]({minLat},{minLon},{maxLat},{maxLon});");
            
            // Points of interest
            query.AppendLine($"  node[\"amenity\"]({minLat},{minLon},{maxLat},{maxLon});");
            query.AppendLine($"  node[\"shop\"]({minLat},{minLon},{maxLat},{maxLon});");
            
            query.AppendLine(");");
            query.AppendLine("out geom;");
            
            return query.ToString();
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
            catch (System.Exception jsonEx) when (jsonEx.GetType().Name == "JsonException")
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
