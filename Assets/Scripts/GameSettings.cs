using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Roll-a-Ball/Game Settings", order = 0)]
public class GameSettings : ScriptableObject
{
    [Header("General")] 
    public bool debugMode = false;
    public float gameTimeScale = 1f;
    public float fallDeathHeight = -50f;
    public bool enableRespawn = true;
    public float baseRespawnDelay = 2f;

    [System.Serializable]
    public struct DifficultyRespawn
    {
        public LevelDifficulty difficulty;
        public float respawnDelay;
    }

    [Header("Difficulty Overrides")]
    public DifficultyRespawn[] difficultyRespawnDelays;

    public float GetRespawnDelay(LevelDifficulty difficulty)
    {
        if (difficultyRespawnDelays != null)
        {
            foreach (var entry in difficultyRespawnDelays)
            {
                if (entry.difficulty == difficulty)
                    return Mathf.Max(0f, entry.respawnDelay);
            }
        }
        return baseRespawnDelay;
    }
}
