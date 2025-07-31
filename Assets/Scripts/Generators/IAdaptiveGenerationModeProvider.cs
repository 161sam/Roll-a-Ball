namespace RollABall.Generators
{
    /// <summary>
    /// Provides adaptive generation mode selection based on a seed.
    /// Implement on LevelProfile derivatives to override the default mode.
    /// </summary>
    public interface IAdaptiveGenerationModeProvider
    {
        /// <summary>
        /// Determine the generation mode for a given seed.
        /// </summary>
        /// <param name="seed">Randomization seed value.</param>
        /// <returns>Selected generation mode.</returns>
        LevelGenerationMode GetAdaptiveGenerationMode(int seed);
    }
}
