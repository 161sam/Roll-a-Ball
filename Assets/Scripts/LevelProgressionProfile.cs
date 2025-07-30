using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject for configurable level progression
/// Eliminates hardcoded scene names and makes level sequence data-driven
/// CLAUDE: CREATED to address TODO-OPT#8 - configurable scene progression
/// </summary>
[CreateAssetMenu(fileName = "LevelProgressionProfile", menuName = "Roll-a-Ball/Level Progression Profile")]
public class LevelProgressionProfile : ScriptableObject
{
    [System.Serializable]
    public class LevelEntry
    {
        [Header("Level Info")]
        public string sceneName;
        public string displayName;
        public int levelIndex;
        
        [Header("Next Level")]
        public string nextSceneName;
        public bool isEndlessMode = false;
        
        [Header("Requirements")]
        public bool requiresPreviousCompletion = true;
        public int minimumScore = 0;
    }
    
    [Header("Level Sequence")]
    [SerializeField] private List<LevelEntry> levelSequence = new List<LevelEntry>();
    
    [Header("Special Modes")]
    [SerializeField] private string endlessModePrefab = "GeneratedLevel";
    [SerializeField] private string osmModePrefab = "Level_OSM"; 
    [SerializeField] private string mainMenuScene = "MainMenu";
    
    // Properties to access private fields (removes unused warnings)
    public string EndlessModePrefab => endlessModePrefab;
    public string OsmModePrefab => osmModePrefab;
    public string MainMenuScene => mainMenuScene;
    
    /// <summary>
    /// Get next scene name for given current scene
    /// </summary>
    public string GetNextScene(string currentScene)
    {
        foreach (LevelEntry entry in levelSequence)
        {
            if (entry.sceneName == currentScene)
            {
                return entry.nextSceneName;
            }
        }
        
        // Fallback: return empty string (no more levels)
        return "";
    }
    
    /// <summary>
    /// Get level info for scene name
    /// </summary>
    public LevelEntry GetLevelEntry(string sceneName)
    {
        foreach (LevelEntry entry in levelSequence)
        {
            if (entry.sceneName == sceneName)
                return entry;
        }
        return null;
    }
    
    /// <summary>
    /// Check if a level exists in the progression
    /// </summary>
    public bool HasLevel(string sceneName)
    {
        return GetLevelEntry(sceneName) != null;
    }
    
    /// <summary>
    /// Get all scene names in order
    /// </summary>
    public List<string> GetAllSceneNames()
    {
        List<string> names = new List<string>();
        foreach (LevelEntry entry in levelSequence)
        {
            names.Add(entry.sceneName);
        }
        return names;
    }
    
    /// <summary>
    /// Create default level progression for Roll-a-Ball
    /// </summary>
    [ContextMenu("Create Default Progression")]
    private void CreateDefaultProgression()
    {
        levelSequence.Clear();
        
        levelSequence.Add(new LevelEntry
        {
            sceneName = "Level1",
            displayName = "Tutorial - Movement",
            levelIndex = 1,
            nextSceneName = "Level2",
            requiresPreviousCompletion = false
        });
        
        levelSequence.Add(new LevelEntry
        {
            sceneName = "Level2", 
            displayName = "Advanced Controls",
            levelIndex = 2,
            nextSceneName = "Level3",
            requiresPreviousCompletion = true
        });
        
        levelSequence.Add(new LevelEntry
        {
            sceneName = "Level3",
            displayName = "Master Challenge", 
            levelIndex = 3,
            nextSceneName = "Level_OSM", // Transition to endless OSM mode
            requiresPreviousCompletion = true
        });
        
        levelSequence.Add(new LevelEntry
        {
            sceneName = "Level_OSM",
            displayName = "Endless: Real World Maps",
            levelIndex = 4,
            nextSceneName = "Level_OSM", // Loop back for endless
            isEndlessMode = true,
            requiresPreviousCompletion = true
        });
        
        levelSequence.Add(new LevelEntry
        {
            sceneName = "GeneratedLevel",
            displayName = "Endless: Procedural",
            levelIndex = 5,
            nextSceneName = "GeneratedLevel", // Loop back for endless
            isEndlessMode = true,
            requiresPreviousCompletion = true
        });
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    /// <summary>
    /// Validate progression setup
    /// </summary>
    public bool ValidateProgression()
    {
        if (levelSequence.Count == 0)
            return false;
            
        foreach (LevelEntry entry in levelSequence)
        {
            if (string.IsNullOrEmpty(entry.sceneName))
                return false;
        }
        
        return true;
    }
}
