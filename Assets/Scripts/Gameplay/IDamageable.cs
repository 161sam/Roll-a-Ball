namespace RollABall.Gameplay
{
    /// <summary>
    /// Interface for objects that can receive damage.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to the object.
        /// </summary>
        /// <param name="amount">Amount of damage to apply.</param>
        void ApplyDamage(float amount);
    }
}
