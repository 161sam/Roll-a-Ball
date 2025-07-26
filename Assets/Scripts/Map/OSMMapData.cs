using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollABall.Map
{
    /// <summary>
    /// Data structure to represent OpenStreetMap data in Unity-compatible format
    /// Converts OSM coordinate system to Unity world coordinates
    /// </summary>
    [System.Serializable]
    public class OSMMapData
    {
        [Header("Map Bounds")]
        public OSMBounds bounds;
        
        [Header("Map Elements")]
        public List<OSMWay> roads = new List<OSMWay>();
        public List<OSMBuilding> buildings = new List<OSMBuilding>();
        public List<OSMArea> areas = new List<OSMArea>();
        public List<OSMNode> pointsOfInterest = new List<OSMNode>();
        
        [Header("Map Configuration")]
        public float scaleMultiplier = 1.0f;
        public Vector3 worldOffset = Vector3.zero;
        
        /// <summary>
        /// Initialize map data with coordinate bounds
        /// </summary>
        public OSMMapData(double minLat, double maxLat, double minLon, double maxLon)
        {
            bounds = new OSMBounds(minLat, maxLat, minLon, maxLon);
        }
        
        /// <summary>
        /// Convert latitude/longitude to Unity world position
        /// </summary>
        public Vector3 LatLonToWorldPosition(double lat, double lon)
        {
            // Normalize coordinates to 0-1 range within bounds
            float normalizedX = (float)((lon - bounds.minLon) / (bounds.maxLon - bounds.minLon));
            float normalizedZ = (float)((lat - bounds.minLat) / (bounds.maxLat - bounds.minLat));
            
            // Convert to Unity world coordinates with scaling
            float worldX = (normalizedX - 0.5f) * scaleMultiplier;
            float worldZ = (normalizedZ - 0.5f) * scaleMultiplier;
            
            return new Vector3(worldX, 0, worldZ) + worldOffset;
        }
        
        /// <summary>
        /// Get the center point of the map in world coordinates
        /// </summary>
        public Vector3 GetWorldCenter()
        {
            double centerLat = (bounds.minLat + bounds.maxLat) / 2.0;
            double centerLon = (bounds.minLon + bounds.maxLon) / 2.0;
            return LatLonToWorldPosition(centerLat, centerLon);
        }
        
        /// <summary>
        /// Validate that the map data is ready for generation
        /// </summary>
        public bool IsValid()
        {
            return bounds.IsValid() && (roads.Count > 0 || buildings.Count > 0);
        }
    }
    
    /// <summary>
    /// Geographic bounds for the map area
    /// </summary>
    [System.Serializable]
    public class OSMBounds
    {
        public double minLat, maxLat, minLon, maxLon;
        
        public OSMBounds(double minLat, double maxLat, double minLon, double maxLon)
        {
            this.minLat = minLat;
            this.maxLat = maxLat;
            this.minLon = minLon;
            this.maxLon = maxLon;
        }
        
        public bool IsValid()
        {
            return minLat < maxLat && minLon < maxLon;
        }
        
        public double GetWidth() => maxLon - minLon;
        public double GetHeight() => maxLat - minLat;
    }
    
    /// <summary>
    /// Represents a single coordinate point from OSM
    /// </summary>
    [System.Serializable]
    public class OSMNode
    {
        public long id;
        public double lat, lon;
        public Dictionary<string, string> tags = new Dictionary<string, string>();
        public string name;
        
        public OSMNode(long id, double lat, double lon)
        {
            this.id = id;
            this.lat = lat;
            this.lon = lon;
        }
        
        public bool HasTag(string key)
        {
            return tags.ContainsKey(key);
        }
        
        public string GetTag(string key, string defaultValue = "")
        {
            return tags.ContainsKey(key) ? tags[key] : defaultValue;
        }
    }
    
    /// <summary>
    /// Represents a way (path/road/building outline) from OSM
    /// </summary>
    [System.Serializable]
    public class OSMWay
    {
        public long id;
        public List<OSMNode> nodes = new List<OSMNode>();
        public Dictionary<string, string> tags = new Dictionary<string, string>();
        public string wayType; // "highway", "building", "area", etc.
        
        public OSMWay(long id)
        {
            this.id = id;
        }
        
        public bool IsClosed()
        {
            return nodes.Count > 2 && nodes[0].id == nodes[nodes.Count - 1].id;
        }
        
        public bool IsRoad()
        {
            return tags.ContainsKey("highway");
        }
        
        public bool IsBuilding()
        {
            return tags.ContainsKey("building");
        }
        
        public string GetTag(string key, string defaultValue = "")
        {
            return tags.ContainsKey(key) ? tags[key] : defaultValue;
        }
    }
    
    /// <summary>
    /// Specialized data structure for buildings with height information
    /// </summary>
    [System.Serializable]
    public class OSMBuilding : OSMWay
    {
        public float height = 3.0f; // Default building height in meters
        public int levels = 1;
        public string buildingType = "residential";
        
        public OSMBuilding(long id) : base(id)
        {
            wayType = "building";
        }
        
        /// <summary>
        /// Calculate building height from OSM tags
        /// </summary>
        public void CalculateHeight()
        {
            // Try to get height from tags
            if (tags.ContainsKey("height"))
            {
                if (float.TryParse(tags["height"].Replace("m", ""), out float parsedHeight))
                {
                    height = parsedHeight;
                    return;
                }
            }
            
            // Try to estimate from building levels
            if (tags.ContainsKey("building:levels"))
            {
                if (int.TryParse(tags["building:levels"], out int parsedLevels))
                {
                    levels = parsedLevels;
                    height = levels * 3.0f; // Assume 3m per level
                    return;
                }
            }
            
            // Set default based on building type
            buildingType = GetTag("building", "residential");
            height = buildingType switch
            {
                "skyscraper" => 50.0f,
                "office" => 15.0f,
                "commercial" => 6.0f,
                "industrial" => 8.0f,
                _ => 3.0f // residential default
            };
        }
    }
    
    /// <summary>
    /// Represents areas like parks, water bodies, etc.
    /// </summary>
    [System.Serializable]
    public class OSMArea : OSMWay
    {
        public string areaType; // "park", "water", "forest", etc.
        public Color materialColor = Color.green;
        
        public OSMArea(long id) : base(id)
        {
            wayType = "area";
        }
        
        /// <summary>
        /// Determine area type and appropriate material color
        /// </summary>
        public void DetermineAreaType()
        {
            if (tags.ContainsKey("leisure"))
            {
                areaType = tags["leisure"];
                materialColor = areaType switch
                {
                    "park" => Color.green,
                    "playground" => new Color(0.8f, 0.9f, 0.6f),
                    "sports_centre" => new Color(0.6f, 0.8f, 0.6f),
                    _ => Color.green
                };
            }
            else if (tags.ContainsKey("natural"))
            {
                areaType = tags["natural"];
                materialColor = areaType switch
                {
                    "water" => Color.blue,
                    "forest" => new Color(0.2f, 0.6f, 0.2f),
                    "grass" => Color.green,
                    _ => Color.gray
                };
            }
            else if (tags.ContainsKey("landuse"))
            {
                areaType = tags["landuse"];
                materialColor = areaType switch
                {
                    "residential" => new Color(0.9f, 0.9f, 0.8f),
                    "commercial" => new Color(0.8f, 0.8f, 0.9f),
                    "industrial" => new Color(0.7f, 0.7f, 0.7f),
                    _ => Color.gray
                };
            }
            else
            {
                areaType = "unknown";
                materialColor = Color.gray;
            }
        }
    }
}
