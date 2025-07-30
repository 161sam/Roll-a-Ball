using UnityEngine;

namespace RollABall.VFX
{
    /// <summary>
    /// Simple rotating gear component for Steampunk decoration
    /// </summary>
    public class RotatingGear : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 30f; // degrees per second
        [SerializeField] private Vector2 speedVariationRange = new Vector2(-5f, 5f);
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private bool randomizeDirection = true;
        
        private float direction = 1f;
        
        private void Start()
        {
            // Randomize rotation direction for variety
            if (randomizeDirection)
            {
                direction = Random.value > 0.5f ? 1f : -1f;
            }
            
            // Add configurable random variation to rotation speed
            rotationSpeed += Random.Range(speedVariationRange.x, speedVariationRange.y);
        }
        
        private void Update()
        {
            // Rotate the gear continuously
            transform.Rotate(rotationAxis * rotationSpeed * direction * Time.deltaTime);
        }
    }
}