using UnityEngine;
using System.Collections.Generic;

namespace RollABall.Utility
{
    /// <summary>
    /// Simple pooling utility for collider GameObjects. This reduces
    /// allocations when many temporary colliders are created at runtime.
    /// </summary>
    public static class ColliderPooler
    {
        private static readonly Queue<GameObject> meshPool = new();
        private static readonly Queue<GameObject> boxPool = new();

        /// <summary>
        /// Get a pooled collider object. If <paramref name="useMeshCollider"/> is
        /// true a MeshCollider is provided, otherwise a BoxCollider.
        /// </summary>
        public static GameObject Get(bool useMeshCollider, Transform parent, string name)
        {
            Queue<GameObject> pool = useMeshCollider ? meshPool : boxPool;
            GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateNew(useMeshCollider);

            obj.name = name;
            obj.transform.SetParent(parent);
            obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// Return a collider object to its pool.
        /// </summary>
        public static void Release(GameObject obj)
        {
            if (!obj) return;
            obj.SetActive(false);
            obj.transform.SetParent(null);

            if (obj.TryGetComponent<MeshCollider>(out var mesh))
            {
                mesh.sharedMesh = null;
                meshPool.Enqueue(obj);
            }
            else if (obj.TryGetComponent<BoxCollider>(out _))
            {
                boxPool.Enqueue(obj);
            }
            else
            {
                Object.Destroy(obj);
            }
        }

        /// <summary>
        /// Clear all pooled objects.
        /// </summary>
        public static void Clear()
        {
            while (meshPool.Count > 0) Object.Destroy(meshPool.Dequeue());
            while (boxPool.Count > 0) Object.Destroy(boxPool.Dequeue());
        }

        private static GameObject CreateNew(bool mesh)
        {
            var obj = new GameObject("PooledCollider");
            if (mesh)
            {
                obj.AddComponent<MeshCollider>();
            }
            else
            {
                obj.AddComponent<BoxCollider>();
            }
            obj.SetActive(false);
            return obj;
        }
    }
}
