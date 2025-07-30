using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Utility-Klasse zum Erstellen der Standard-LevelProfile Assets
/// Wird nur im Editor verwendet
/// </summary>
public class LevelProfileCreator : MonoBehaviour
{
    [Header("Profile Creation")]
    [SerializeField] private bool createProfilesOnStart = false;
    
    #if UNITY_EDITOR
    [ContextMenu("Create All Level Profiles")]
    public void CreateAllProfiles()
    {
        CreateEasyProfile();
        CreateMediumProfile();
        CreateHardProfile();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("All LevelProfiles created successfully!");
    }
    
    private void CreateEasyProfile()
    {
        LevelProfile easy = ScriptableObject.CreateInstance<LevelProfile>();
        
        var easyFields = new System.Collections.Generic.Dictionary<string, object>
        {
            { "profileName", "Easy Profile" },
            { "displayName", "Einfach" },
            { "difficultyLevel", 1 },
            { "themeColor", Color.green },
            { "levelSize", 8 },
            { "tileSize", 2f },
            { "minWalkableArea", 80 },
            { "collectibleCount", 5 },
            { "minCollectibleDistance", 2 },
            { "collectibleSpawnHeight", 0.5f },
            { "obstacleDensity", 0.1f },
            { "enableMovingObstacles", false },
            { "movingObstacleChance", 0f },
            { "frictionVariance", 0.1f },
            { "enableSlipperyTiles", false },
            { "slipperyTileChance", 0f },
            { "enableParticleEffects", true },
            { "playerSpawnOffset", Vector3.up },
            { "randomizeSpawnPosition", false },
            { "spawnSafeRadius", 3f },
            { "useTimeBasedSeed", true },
            { "generationMode", LevelGenerationMode.Simple },
            { "pathComplexity", 0.3f }
        };

        foreach (var kvp in easyFields)
            SetPrivateField(easy, kvp.Key, kvp.Value);
        
        AssetDatabase.CreateAsset(easy, "Assets/ScriptableObjects/EasyProfile.asset");
    }
    
    private void CreateMediumProfile()
    {
        LevelProfile medium = ScriptableObject.CreateInstance<LevelProfile>();

        var mediumFields = new System.Collections.Generic.Dictionary<string, object>
        {
            { "profileName", "Medium Profile" },
            { "displayName", "Mittel" },
            { "difficultyLevel", 2 },
            { "themeColor", Color.yellow },
            { "levelSize", 12 },
            { "tileSize", 2f },
            { "minWalkableArea", 70 },
            { "collectibleCount", 8 },
            { "minCollectibleDistance", 2 },
            { "collectibleSpawnHeight", 0.5f },
            { "obstacleDensity", 0.25f },
            { "enableMovingObstacles", false },
            { "movingObstacleChance", 0.05f },
            { "frictionVariance", 0.2f },
            { "enableSlipperyTiles", true },
            { "slipperyTileChance", 0.1f },
            { "enableParticleEffects", true },
            { "playerSpawnOffset", Vector3.up },
            { "randomizeSpawnPosition", true },
            { "spawnSafeRadius", 3f },
            { "useTimeBasedSeed", true },
            { "generationMode", LevelGenerationMode.Maze },
            { "pathComplexity", 0.5f }
        };

        foreach (var kvp in mediumFields)
            SetPrivateField(medium, kvp.Key, kvp.Value);
        
        AssetDatabase.CreateAsset(medium, "Assets/ScriptableObjects/MediumProfile.asset");
    }
    
    private void CreateHardProfile()
    {
        LevelProfile hard = ScriptableObject.CreateInstance<LevelProfile>();

        var hardFields = new System.Collections.Generic.Dictionary<string, object>
        {
            { "profileName", "Hard Profile" },
            { "displayName", "Schwer" },
            { "difficultyLevel", 3 },
            { "themeColor", Color.red },
            { "levelSize", 16 },
            { "tileSize", 2f },
            { "minWalkableArea", 60 },
            { "collectibleCount", 12 },
            { "minCollectibleDistance", 1 },
            { "collectibleSpawnHeight", 0.5f },
            { "obstacleDensity", 0.4f },
            { "enableMovingObstacles", true },
            { "movingObstacleChance", 0.1f },
            { "frictionVariance", 0.3f },
            { "enableSlipperyTiles", true },
            { "slipperyTileChance", 0.2f },
            { "enableParticleEffects", true },
            { "playerSpawnOffset", Vector3.up },
            { "randomizeSpawnPosition", true },
            { "spawnSafeRadius", 2f },
            { "useTimeBasedSeed", true },
            { "generationMode", LevelGenerationMode.Maze },
            { "pathComplexity", 0.8f }
        };

        foreach (var kvp in hardFields)
            SetPrivateField(hard, kvp.Key, kvp.Value);
        
        AssetDatabase.CreateAsset(hard, "Assets/ScriptableObjects/HardProfile.asset");
    }
    
    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"Field '{fieldName}' not found in {obj.GetType().Name}");
        }
    }
    #endif
    
    void Start()
    {
        if (createProfilesOnStart)
        {
            #if UNITY_EDITOR
            CreateAllProfiles();
            #endif
        }
    }
}
