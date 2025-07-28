using System;
using UnityEngine;

namespace RollABall.Map
{
    /// <summary>
    /// Utility class for validating and correcting geographic coordinates
    /// Ensures all coordinates are within valid ranges for OSM APIs
    /// </summary>
    public static class CoordinateValidator
    {
        // Strict OSM/Overpass API limits
        public const double MIN_LATITUDE = -90.0;
        public const double MAX_LATITUDE = 90.0;
        public const double MIN_LONGITUDE = -180.0;
        public const double MAX_LONGITUDE = 180.0;
        
        // Safe limits to avoid edge cases
        public const double SAFE_MIN_LATITUDE = -89.9;
        public const double SAFE_MAX_LATITUDE = 89.9;
        public const double SAFE_MIN_LONGITUDE = -179.9;
        public const double SAFE_MAX_LONGITUDE = 179.9;
        
        // Maximum reasonable radius for OSM queries (in meters)
        public const double MAX_SAFE_RADIUS = 10000.0; // 10km
        
        /// <summary>
        /// Validates a single coordinate pair
        /// </summary>
        public static bool IsValidCoordinate(double lat, double lon)
        {
            return lat >= MIN_LATITUDE && lat <= MAX_LATITUDE &&
                   lon >= MIN_LONGITUDE && lon <= MAX_LONGITUDE;
        }
        
        /// <summary>
        /// Clamps coordinates to safe ranges for API usage
        /// </summary>
        public static (double lat, double lon) ClampCoordinate(double lat, double lon)
        {
            double clampedLat = Math.Max(SAFE_MIN_LATITUDE, Math.Min(SAFE_MAX_LATITUDE, lat));
            double clampedLon = Math.Max(SAFE_MIN_LONGITUDE, Math.Min(SAFE_MAX_LONGITUDE, lon));
            
            if (Math.Abs(clampedLat - lat) > 0.001 || Math.Abs(clampedLon - lon) > 0.001)
            {
                Debug.LogWarning($"[CoordinateValidator] Coordinates clamped: ({lat:F6}, {lon:F6}) -> ({clampedLat:F6}, {clampedLon:F6})");
            }
            
            return (clampedLat, clampedLon);
        }
        
        /// <summary>
        /// Calculates a safe bounding box around a center point
        /// </summary>
        public static OSMBounds CalculateSafeBoundingBox(double centerLat, double centerLon, double radiusMeters)
        {
            // Validate input coordinates
            if (!IsValidCoordinate(centerLat, centerLon))
            {
                Debug.LogError($"[CoordinateValidator] Invalid center coordinates: {centerLat}, {centerLon}");
                throw new ArgumentException("Center coordinates are outside valid range");
            }
            
            // Clamp radius to reasonable bounds
            double safeRadius = Math.Min(radiusMeters, MAX_SAFE_RADIUS);
            if (safeRadius != radiusMeters)
            {
                Debug.LogWarning($"[CoordinateValidator] Radius clamped from {radiusMeters}m to {safeRadius}m");
            }
            
            // Convert radius to degrees
            double radiusInDegrees = safeRadius / 111320.0; // meters to degrees at equator
            
            // Calculate latitude bounds (straightforward)
            double minLat = centerLat - radiusInDegrees;
            double maxLat = centerLat + radiusInDegrees;
            
            // Calculate longitude bounds with cosine correction
            double latRadians = centerLat * Math.PI / 180.0;
            double cosLat = Math.Cos(latRadians);
            
            // Handle pole proximity (avoid division by zero)
            double lonRadius;
            if (Math.Abs(centerLat) > 89.0) // Very close to poles
            {
                Debug.LogWarning($"[CoordinateValidator] Near pole at lat {centerLat:F6}, using full longitude range");
                // Near poles, use maximum longitude range
                lonRadius = 180.0;
            }
            else if (cosLat < 0.001) // Avoid division by very small numbers
            {
                lonRadius = radiusInDegrees / 0.001; // Use minimum safe cosine value
            }
            else
            {
                lonRadius = radiusInDegrees / cosLat;
            }
            
            double minLon = centerLon - lonRadius;
            double maxLon = centerLon + lonRadius;
            
            // Strict coordinate clamping to API limits
            minLat = Math.Max(MIN_LATITUDE, minLat);
            maxLat = Math.Min(MAX_LATITUDE, maxLat);
            minLon = Math.Max(MIN_LONGITUDE, minLon);
            maxLon = Math.Min(MAX_LONGITUDE, maxLon);
            
            // Handle longitude wraparound near dateline
            if (maxLon - minLon > 360.0)
            {
                Debug.LogWarning($"[CoordinateValidator] Longitude span too large, limiting to full range");
                minLon = MIN_LONGITUDE;
                maxLon = MAX_LONGITUDE;
            }
            
            // Final validation
            if (minLat >= maxLat || minLon >= maxLon)
            {
                Debug.LogError($"[CoordinateValidator] Invalid bounding box calculated: lat[{minLat:F6}, {maxLat:F6}], lon[{minLon:F6}, {maxLon:F6}]");
                throw new InvalidOperationException("Unable to calculate valid bounding box");
            }
            
            var bounds = new OSMBounds(minLat, maxLat, minLon, maxLon);
            
            Debug.Log($"[CoordinateValidator] Safe bounding box: lat[{minLat:F6}, {maxLat:F6}], lon[{minLon:F6}, {maxLon:F6}]");
            
            return bounds;
        }
        
        /// <summary>
        /// Validates a complete bounding box for API usage
        /// </summary>
        public static bool ValidateBoundingBox(OSMBounds bounds)
        {
            if (bounds == null || !bounds.IsValid())
            {
                return false;
            }
            
            // Check coordinate ranges
            if (bounds.minLat < MIN_LATITUDE || bounds.maxLat > MAX_LATITUDE ||
                bounds.minLon < MIN_LONGITUDE || bounds.maxLon > MAX_LONGITUDE)
            {
                Debug.LogError($"[CoordinateValidator] Bounding box coordinates out of range: " +
                             $"lat[{bounds.minLat:F6}, {bounds.maxLat:F6}], lon[{bounds.minLon:F6}, {bounds.maxLon:F6}]");
                return false;
            }
            
            // Check dimensions
            if (bounds.minLat >= bounds.maxLat || bounds.minLon >= bounds.maxLon)
            {
                Debug.LogError($"[CoordinateValidator] Invalid bounding box dimensions");
                return false;
            }
            
            // Check reasonable size (not too large)
            double latSpan = bounds.maxLat - bounds.minLat;
            double lonSpan = bounds.maxLon - bounds.minLon;
            
            if (latSpan > 10.0 || lonSpan > 10.0) // 10 degrees ≈ 1100km
            {
                Debug.LogWarning($"[CoordinateValidator] Very large bounding box: {latSpan:F3}° lat, {lonSpan:F3}° lon");
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets a safe fallback location (Leipzig city center)
        /// </summary>
        public static (double lat, double lon) GetFallbackLocation()
        {
            return (51.3387, 12.3779); // Leipzig Marktplatz
        }
        
        /// <summary>
        /// Determines if coordinates are near the international dateline
        /// </summary>
        public static bool IsNearDateline(double lon, double threshold = 10.0)
        {
            return Math.Abs(Math.Abs(lon) - 180.0) < threshold;
        }
        
        /// <summary>
        /// Determines if coordinates are near the poles
        /// </summary>
        public static bool IsNearPoles(double lat, double threshold = 5.0)
        {
            return Math.Abs(Math.Abs(lat) - 90.0) < threshold;
        }
    }
}
