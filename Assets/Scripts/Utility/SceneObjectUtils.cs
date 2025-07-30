using UnityEngine;

namespace RollABall.Utility
{
    /// <summary>
    /// Helper methods for scene object management.
    /// </summary>
    public static class SceneObjectUtils
    {
        /// <summary>
        /// Find the first object of type T in the scene or create a new GameObject with that component.
        /// </summary>
        public static T FindOrCreateComponent<T>(string objectName = null) where T : Component
        {
            var existing = Object.FindFirstObjectByType<T>();
            if (existing)
                return existing;

            var go = new GameObject(string.IsNullOrEmpty(objectName) ? typeof(T).Name : objectName);
            return go.AddComponent<T>();
        }
    }
}
