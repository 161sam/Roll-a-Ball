using UnityEngine;
using System.Collections.Generic;

namespace RollABall.Utility
{
    /// <summary>
    /// Simple object pooling utility for prefabs.
    /// Optimizes performance by reusing GameObjects instead of constant instantiation/destruction.
    /// </summary>
    public static class PrefabPooler
    {
        private class Pool
        {
            public readonly GameObject prefab;
            public readonly Queue<GameObject> objects = new Queue<GameObject>();

            public Pool(GameObject prefab)
            {
                this.prefab = prefab;
            }
        }

        private static readonly Dictionary<GameObject, Pool> pools = new Dictionary<GameObject, Pool>();
        // TODO: Add maximum pool size to prevent uncontrolled growth

        /// <summary>
        /// Get a pooled instance of the given prefab.
        /// </summary>
        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!prefab) return null;
            
            if (!pools.TryGetValue(prefab, out Pool pool))
            {
                pool = new Pool(prefab);
                pools[prefab] = pool;
            }

            GameObject obj = pool.objects.Count > 0 ? pool.objects.Dequeue() : Object.Instantiate(prefab);
            obj.transform.SetParent(parent);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            
            PooledMarker marker = obj.GetComponent<PooledMarker>();
            if (!marker)
            {
                marker = obj.AddComponent<PooledMarker>();
            }
            marker.prefab = prefab;
            
            return obj;
        }

        /// <summary>
        /// Return an object to its pool.
        /// </summary>
        public static void Release(GameObject obj)
        {
            if (!obj) return;
            
            PooledMarker marker = obj.GetComponent<PooledMarker>();
            if (marker && marker.prefab && pools.TryGetValue(marker.prefab, out Pool pool))
            {
                obj.SetActive(false);
                obj.transform.SetParent(null);
                pool.objects.Enqueue(obj);
            }
            else
            {
                Object.Destroy(obj);
            }
        }

        /// <summary>
        /// Clear all pools and destroy pooled objects.
        /// </summary>
        public static void Clear()
        {
            foreach (var kvp in pools)
            {
                while (kvp.Value.objects.Count > 0)
                {
                    Object.Destroy(kvp.Value.objects.Dequeue());
                }
            }
            pools.Clear();
        }

        private class PooledMarker : MonoBehaviour
        {
            public GameObject prefab;
        }
    }
}
