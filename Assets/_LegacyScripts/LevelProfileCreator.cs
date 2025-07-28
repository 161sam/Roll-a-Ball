using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
// TODO-REMOVE#2: Obsolete setup script – no longer needed and may corrupt current data
// TODO-DUPLICATE#1: Funktional identisch mit LevelSetupHelper.cs. Bitte vereinheitlichen oder entfernen.

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
        
        // Set private fields via reflection (since they're private)
        // TODO-OPT#19: The long list of SetPrivateField calls repeats for each profile
        // consider a configuration struct or dictionary-driven assignment
        SetPrivateField(easy, "profileName", "Easy Profile");
        SetPrivateField(easy, "displayName", "Einfach");
        SetPrivateField(easy, "difficultyLevel", 1);
        SetPrivateField(easy, "themeColor", Color.green);
        SetPrivateField(easy, "levelSize", 8);
        SetPrivateField(easy, "tileSize", 2f);
        SetPrivateField(easy, "minWalkableArea", 80);
        SetPrivateField(easy, "collectibleCount", 5);
        SetPrivateField(easy, "minCollectibleDistance", 2);
        SetPrivateField(easy, "collectibleSpawnHeight", 0.5f);
        SetPrivateField(easy, "obstacleDensity", 0.1f);
        SetPrivateField(easy, "enableMovingObstacles", false);
        SetPrivateField(easy, "movingObstacleChance", 0f);
        SetPrivateField(easy, "frictionVariance", 0.1f);
        SetPrivateField(easy, "enableSlipperyTiles", false);
        SetPrivateField(easy, "slipperyTileChance", 0f);
        SetPrivateField(easy, "enableParticleEffects", true);
        SetPrivateField(easy, "playerSpawnOffset", Vector3.up);
        SetPrivateField(easy, "randomizeSpawnPosition", false);
        SetPrivateField(easy, "spawnSafeRadius", 3f);
        SetPrivateField(easy, "useTimeBasedSeed", true);
        SetPrivateField(easy, "generationMode", LevelGenerationMode.Simple);
        SetPrivateField(easy, "pathComplexity", 0.3f);
        
        AssetDatabase.CreateAsset(easy, "Assets/ScriptableObjects/EasyProfile.asset");
    }
    
    private void CreateMediumProfile()
    {
        LevelProfile medium = ScriptableObject.CreateInstance<LevelProfile>();
        
        SetPrivateField(medium, "profileName", "Medium Profile");
        SetPrivateField(medium, "displayName", "Mittel");
        SetPrivateField(medium, "difficultyLevel", 2);
        SetPrivateField(medium, "themeColor", Color.yellow);
        SetPrivateField(medium, "levelSize", 12);
        SetPrivateField(medium, "tileSize", 2f);
        SetPrivateField(medium, "minWalkableArea", 70);
        SetPrivateField(medium, "collectibleCount", 8);
        SetPrivateField(medium, "minCollectibleDistance", 2);
        SetPrivateField(medium, "collectibleSpawnHeight", 0.5f);
        SetPrivateField(medium, "obstacleDensity", 0.25f);
        SetPrivateField(medium, "enableMovingObstacles", false);
        SetPrivateField(medium, "movingObstacleChance", 0.05f);
        SetPrivateField(medium, "frictionVariance", 0.2f);
        SetPrivateField(medium, "enableSlipperyTiles", true);
        SetPrivateField(medium, "slipperyTileChance", 0.1f);
        SetPrivateField(medium, "enableParticleEffects", true);
        SetPrivateField(medium, "playerSpawnOffset", Vector3.up);
        SetPrivateField(medium, "randomizeSpawnPosition", true);
        SetPrivateField(medium, "spawnSafeRadius", 3f);
        SetPrivateField(medium, "useTimeBasedSeed", true);
        SetPrivateField(medium, "generationMode", LevelGenerationMode.Maze);
        SetPrivateField(medium, "pathComplexity", 0.5f);
        
        AssetDatabase.CreateAsset(medium, "Assets/ScriptableObjects/MediumProfile.asset");
    }
    
    private void CreateHardProfile()
    {
        LevelProfile hard = ScriptableObject.CreateInstance<LevelProfile>();
        
        SetPrivateField(hard, "profileName", "Hard Profile");
        SetPrivateField(hard, "displayName", "Schwer");
        SetPrivateField(hard, "difficultyLevel", 3);
        SetPrivateField(hard, "themeColor", Color.red);
        SetPrivateField(hard, "levelSize", 16);
        SetPrivateField(hard, "tileSize", 2f);
        SetPrivateField(hard, "minWalkableArea", 60);
        SetPrivateField(hard, "collectibleCount", 12);
        SetPrivateField(hard, "minCollectibleDistance", 1);
        SetPrivateField(hard, "collectibleSpawnHeight", 0.5f);
        SetPrivateField(hard, "obstacleDensity", 0.4f);
        SetPrivateField(hard, "enableMovingObstacles", true);
        SetPrivateField(hard, "movingObstacleChance", 0.1f);
        SetPrivateField(hard, "frictionVariance", 0.3f);
        SetPrivateField(hard, "enableSlipperyTiles", true);
        SetPrivateField(hard, "slipperyTileChance", 0.2f);
        SetPrivateField(hard, "enableParticleEffects", true);
        SetPrivateField(hard, "playerSpawnOffset", Vector3.up);
        SetPrivateField(hard, "randomizeSpawnPosition", true);
        SetPrivateField(hard, "spawnSafeRadius", 2f);
        SetPrivateField(hard, "useTimeBasedSeed", true);
        SetPrivateField(hard, "generationMode", LevelGenerationMode.Maze);
        SetPrivateField(hard, "pathComplexity", 0.8f);
        
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
