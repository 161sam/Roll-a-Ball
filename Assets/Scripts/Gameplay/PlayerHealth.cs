using UnityEngine;

namespace RollABall.Gameplay
{
    /// <summary>
    /// Basic health component for the player.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Gameplay/Player Health")]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 3f;
        [SerializeField] private float currentHealth;

        /// <summary>
        /// Invoked whenever health changes. Parameters: current and max health.
        /// </summary>
        public System.Action<float, float> OnHealthChanged;

        private void Awake()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <inheritdoc />
        public void ApplyDamage(float amount)
        {
            if (amount <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0f)
            {
                Debug.Log($"[PlayerHealth] {name} died.", this);
            }
        }
    }
}
