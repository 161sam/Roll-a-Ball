using System.Collections.Generic;
using UnityEngine;

namespace RollABall.Map
{
    /// <summary>
    /// ScriptableObject container for synthetic map test data used by MapGeneratorTester.
    /// </summary>
    [CreateAssetMenu(fileName = "MapGeneratorTestData", menuName = "Roll-a-Ball/Map Generator Test Data")]
    public class MapGeneratorTestData : ScriptableObject
    {
        [System.Serializable]
        public class RoadDefinition
        {
            public string roadType;
            public Vector2[] coordinates;
        }

        [Header("Map Bounds")]
        public OSMBounds bounds = new OSMBounds(51.33, 51.35, 12.37, 12.39);
        [Header("Scale Multiplier")]
        public float scaleMultiplier = 1000f;
        [Header("Road Definitions")]
        public List<RoadDefinition> roads = new();

        /// <summary>
        /// Convert this ScriptableObject into an OSMMapData instance.
        /// </summary>
        public OSMMapData ToOSMMapData()
        {
            var data = new OSMMapData(bounds.minLat, bounds.maxLat, bounds.minLon, bounds.maxLon)
            {
                scaleMultiplier = scaleMultiplier
            };
            foreach (var road in roads)
            {
                if (road.coordinates == null || road.coordinates.Length == 0)
                    continue;

                OSMWay way = new OSMWay(data.roads.Count + 1000)
                {
                    wayType = "highway"
                };
                way.tags.Add("highway", road.roadType);
                for (int i = 0; i < road.coordinates.Length; i++)
                {
                    var coord = road.coordinates[i];
                    OSMNode node = new OSMNode(i + (data.roads.Count * 100), coord.x, coord.y);
                    way.nodes.Add(node);
                }
                data.roads.Add(way);
            }
            return data;
        }
    }
}
