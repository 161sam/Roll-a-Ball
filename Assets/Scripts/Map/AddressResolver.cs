using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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
        
        // Events for communication with other systems
        public event Action<GeocodeResult> OnAddressResolved;
        public event Action<OSMMapData> OnMapDataLoaded;
        public event Action<string> OnError;
        
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
                        OnError?.Invoke($"Adresse '{address}' konnte nicht gefunden werden.");
                    }
                }
                else
                {
                    Debug.LogError($"[AddressResolver] Geocoding failed: {request.error}");
                    OnError?.Invoke($"Fehler beim Auflösen der Adresse: {request.error}");
                }
            }
        }
        
        /// <summary>
        /// Load OSM map data for given coordinates
        /// </summary>
        private IEnumerator LoadMapDataCoroutine(double lat, double lon, float radius)
        {
            Debug.Log($"[AddressResolver] Loading map data for {lat}, {lon} with radius {radius}m");
            
            // Calculate bounding box
            double radiusInDegrees = radius / 111320.0; // Approximate conversion meters to degrees
            double minLat = lat - radiusInDegrees;
            double maxLat = lat + radiusInDegrees;
            double minLon = lon - radiusInDegrees;
            double maxLon = lon + radiusInDegrees;
            
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
                        Debug.Log($"[AddressResolver] Map data loaded successfully. Roads: {mapData.roads.Count}, Buildings: {mapData.buildings.Count}");
                        OnMapDataLoaded?.Invoke(mapData);
                    }
                    else
                    {
                        OnError?.Invoke("Keine Kartendaten für diese Region verfügbar.");
                    }
                }
                else
                {
                    Debug.LogError($"[AddressResolver] Map data request failed: {request.error}");
                    OnError?.Invoke($"Fehler beim Laden der Kartendaten: {request.error}");
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
        /// Parse OSM data response from Overpass API
        /// </summary>
        private OSMMapData ParseOSMResponse(string json, double minLat, double maxLat, double minLon, double maxLon)
        {
            try
            {
                OSMMapData mapData = new OSMMapData(minLat, maxLat, minLon, maxLon);
                
                // This is a simplified parser. In production, use a proper JSON library like Newtonsoft.Json
                // For now, we'll create some sample data based on the bounds
                
                Debug.Log($"[AddressResolver] Parsing OSM response (simplified). Creating sample data for bounds.");
                
                // Create sample roads in a grid pattern
                CreateSampleRoads(mapData);
                
                // Create sample buildings
                CreateSampleBuildings(mapData);
                
                // Set appropriate scale based on area size
                double areaWidth = maxLon - minLon;
                mapData.scaleMultiplier = (float)(1000.0 / areaWidth); // Scale to reasonable Unity size
                
                return mapData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressResolver] Error parsing OSM response: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Create sample road data (placeholder for real OSM parsing)
        /// </summary>
        private void CreateSampleRoads(OSMMapData mapData)
        {
            double centerLat = (mapData.bounds.minLat + mapData.bounds.maxLat) / 2.0;
            double centerLon = (mapData.bounds.minLon + mapData.bounds.maxLon) / 2.0;
            double span = (mapData.bounds.maxLat - mapData.bounds.minLat) / 4.0;
            
            // Create main horizontal road
            OSMWay mainRoad = new OSMWay(1001);
            mainRoad.tags["highway"] = "primary";
            mainRoad.nodes.Add(new OSMNode(1, centerLat, mapData.bounds.minLon));
            mainRoad.nodes.Add(new OSMNode(2, centerLat, mapData.bounds.maxLon));
            mapData.roads.Add(mainRoad);
            
            // Create main vertical road
            OSMWay crossRoad = new OSMWay(1002);
            crossRoad.tags["highway"] = "secondary";
            crossRoad.nodes.Add(new OSMNode(3, mapData.bounds.minLat, centerLon));
            crossRoad.nodes.Add(new OSMNode(4, mapData.bounds.maxLat, centerLon));
            mapData.roads.Add(crossRoad);
            
            // Create connecting roads
            for (int i = 0; i < 3; i++)
            {
                double roadLat = centerLat + (i - 1) * span;
                OSMWay road = new OSMWay(1003 + i);
                road.tags["highway"] = "residential";
                road.nodes.Add(new OSMNode(5 + i * 2, roadLat, centerLon - span));
                road.nodes.Add(new OSMNode(6 + i * 2, roadLat, centerLon + span));
                mapData.roads.Add(road);
            }
        }
        
        /// <summary>
        /// Create sample building data (placeholder for real OSM parsing)
        /// </summary>
        private void CreateSampleBuildings(OSMMapData mapData)
        {
            double centerLat = (mapData.bounds.minLat + mapData.bounds.maxLat) / 2.0;
            double centerLon = (mapData.bounds.minLon + mapData.bounds.maxLon) / 2.0;
            double span = (mapData.bounds.maxLat - mapData.bounds.minLat) / 6.0;
            
            string[] buildingTypes = { "residential", "commercial", "office", "industrial" };
            
            for (int i = 0; i < 8; i++)
            {
                OSMBuilding building = new OSMBuilding(2001 + i);
                building.tags["building"] = buildingTypes[i % buildingTypes.Length];
                
                // Create rectangular building
                double lat = centerLat + (i % 2 == 0 ? span : -span);
                double lon = centerLon + ((i / 2) - 1.5) * span;
                
                building.nodes.Add(new OSMNode(100 + i * 4, lat - span/3, lon - span/3));
                building.nodes.Add(new OSMNode(101 + i * 4, lat - span/3, lon + span/3));
                building.nodes.Add(new OSMNode(102 + i * 4, lat + span/3, lon + span/3));
                building.nodes.Add(new OSMNode(103 + i * 4, lat + span/3, lon - span/3));
                building.nodes.Add(new OSMNode(100 + i * 4, lat - span/3, lon - span/3)); // Close the shape
                
                building.CalculateHeight();
                mapData.buildings.Add(building);
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
