using UnityEngine;

namespace RollABall.Map
{
    /// <summary>
    /// Configuration asset controlling scale of generated maps.
    /// Allows adjusting overall Unity scale without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "MapScaleConfig", menuName = "Roll-a-Ball/Map Scale Config")]
    public class MapScaleConfig : ScriptableObject
    {
        [Tooltip("Target size in Unity units for the loaded map area")]
        public float unityScale = 1000f;
    }
}

