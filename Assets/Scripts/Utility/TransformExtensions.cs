using UnityEngine;

namespace RollABall.Utility
{
    /// <summary>
    /// Extension methods for <see cref="Transform"/> instances.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Builds the full hierarchy path of the transform within the scene.
        /// </summary>
        /// <param name="transform">Transform to evaluate.</param>
        /// <returns>Slash separated path from the scene root to the object.</returns>
        public static string GetHierarchyPath(this Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
