using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RollABall.Map
{
    /// <summary>
    /// Utility methods for simple pathfinding operations on <see cref="OSMMapData"/>.
    /// </summary>
    public static class MapPathfindingUtility
    {
        /// <summary>
        /// Find the road node farthest from the given origin using Dijkstra distance.
        /// </summary>
        /// <param name="mapData">OSM map data containing roads.</param>
        /// <param name="origin">World position to measure from.</param>
        /// <returns>World position of the farthest reachable road node.</returns>
        public static Vector3 GetFarthestRoadPosition(OSMMapData mapData, Vector3 origin)
        {
            if (mapData == null || mapData.roads.Count == 0)
                return origin;

            // Build node dictionary and adjacency
            Dictionary<long, OSMNode> nodes = new();
            Dictionary<long, List<long>> edges = new();
            foreach (var road in mapData.roads)
            {
                for (int i = 0; i < road.nodes.Count; i++)
                {
                    var node = road.nodes[i];
                    nodes[node.id] = node;
                    if (i < road.nodes.Count - 1)
                    {
                        long next = road.nodes[i + 1].id;
                        if (!edges.TryGetValue(node.id, out var list))
                        {
                            list = new List<long>();
                            edges[node.id] = list;
                        }
                        if (!list.Contains(next))
                            list.Add(next);
                        if (!edges.TryGetValue(next, out var rev))
                        {
                            rev = new List<long>();
                            edges[next] = rev;
                        }
                        if (!rev.Contains(node.id))
                            rev.Add(node.id);
                    }
                }
            }

            // Map node ids to world positions
            Dictionary<long, Vector3> worldPositions = nodes.ToDictionary(n => n.Key,
                n => mapData.LatLonToWorldPosition(n.Value.lat, n.Value.lon));

            // Determine closest start node to origin
            long startNode = nodes.First().Key;
            float minDist = float.MaxValue;
            foreach (var kvp in worldPositions)
            {
                float d = Vector3.Distance(origin, kvp.Value);
                if (d < minDist)
                {
                    minDist = d;
                    startNode = kvp.Key;
                }
            }

            // Dijkstra breadth-first search
            Dictionary<long, float> distances = new() { [startNode] = 0f };
            Queue<long> queue = new();
            queue.Enqueue(startNode);
            while (queue.Count > 0)
            {
                long current = queue.Dequeue();
                if (!edges.TryGetValue(current, out var neighbours))
                    continue;

                foreach (var n in neighbours)
                {
                    float newDist = distances[current] +
                        Vector3.Distance(worldPositions[current], worldPositions[n]);
                    if (!distances.ContainsKey(n) || newDist < distances[n])
                    {
                        distances[n] = newDist;
                        queue.Enqueue(n);
                    }
                }
            }

            if (distances.Count == 0)
                return origin;

            long farthestNode = startNode;
            float maxDist = 0f;
            foreach (var kvp in distances)
            {
                if (kvp.Value > maxDist)
                {
                    maxDist = kvp.Value;
                    farthestNode = kvp.Key;
                }
            }

            return worldPositions[farthestNode];
        }
    }
}
