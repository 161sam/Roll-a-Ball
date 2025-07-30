using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Tag Manager - Ensures all required tags exist in the project
/// Automatically creates missing tags needed for Roll-a-Ball functionality
/// </summary>
[AddComponentMenu("Roll-a-Ball/Tag Manager")]
public class TagManager : MonoBehaviour
{
    [Header("Required Tags")]
    // TODO: Load required tags from a central config file instead of hardcoding
    [SerializeField] private string[] requiredTags = {
        "Player",
        "Collectible",
        "Finish",
        "Ground",
        "Wall",
        "GoalZone",
        "Checkpoint",
        "Obstacle"
    };
    
    [Header("Required Layers")]
    [SerializeField] private string[] requiredLayers = {
        "Ground",
        "Collectibles",
        "Player",
        "UI",
        "Obstacles"
    };
    
    [Header("Settings")]
    [SerializeField] private bool autoCreateTagsOnStart = true;
    [SerializeField] private bool verboseLogging = true;
    
    [Header("Manual Controls")]
    [SerializeField] private bool createTagsNow = false;
    [SerializeField] private bool validateTagsNow = false;
    
    [Header("Status")]
    [SerializeField] private int tagsCreated = 0;
    [SerializeField] private int layersCreated = 0;
    [SerializeField] private List<string> missingTags = new List<string>();
    [SerializeField] private List<string> missingLayers = new List<string>();

    private void Start()
    {
        if (autoCreateTagsOnStart)
        {
            CreateMissingTags();
        }
    }

    private void OnValidate()
    {
        if (createTagsNow)
        {
            createTagsNow = false;
            CreateMissingTags();
        }
        
        if (validateTagsNow)
        {
            validateTagsNow = false;
            ValidateTags();
        }
    }

    /// <summary>
    /// Create all missing tags and layers
    /// </summary>
    [ContextMenu("Create Missing Tags")]
    public void CreateMissingTags()
    {
        Log("Starting tag and layer creation process...");
        
        tagsCreated = 0;
        layersCreated = 0;
        missingTags.Clear();
        missingLayers.Clear();
        
        #if UNITY_EDITOR
        CreateMissingTagsEditor();
        CreateMissingLayersEditor();
        #else
        Log("Tag creation only works in Unity Editor");
        ValidateTags();
        #endif
        
        Log($"Tag management completed. Created {tagsCreated} tags and {layersCreated} layers.");
    }

    /// <summary>
    /// Validate existing tags and layers
    /// </summary>
    [ContextMenu("Validate Tags")]
    public void ValidateTags()
    {
        Log("Validating tags and layers...");
        
        missingTags.Clear();
        missingLayers.Clear();
        
        // Check tags
        foreach (string tag in requiredTags)
        {
            if (!TagExists(tag))
            {
                missingTags.Add(tag);
                LogWarning($"Missing tag: {tag}");
            }
            else
            {
                Log($"Tag exists: {tag}");
            }
        }
        
        // Check layers
        foreach (string layer in requiredLayers)
        {
            if (!LayerExists(layer))
            {
                missingLayers.Add(layer);
                LogWarning($"Missing layer: {layer}");
            }
            else
            {
                Log($"Layer exists: {layer}");
            }
        }
        
        if (missingTags.Count == 0 && missingLayers.Count == 0)
        {
            Log("✓ All required tags and layers are present!");
        }
        else
        {
            LogWarning($"Missing {missingTags.Count} tags and {missingLayers.Count} layers");
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Create missing tags in editor
    /// </summary>
    private void CreateMissingTagsEditor()
    {
        Log("Creating missing tags...");
        
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        foreach (string tag in requiredTags)
        {
            if (!TagExists(tag))
            {
                // Add new tag
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTagProp.stringValue = tag;
                
                tagsCreated++;
                Log($"Created tag: {tag}");
            }
        }
        
        tagManager.ApplyModifiedProperties();
        
        if (tagsCreated > 0)
        {
            AssetDatabase.SaveAssets();
            Log($"Saved {tagsCreated} new tags to project");
        }
    }
    
    /// <summary>
    /// Create missing layers in editor
    /// </summary>
    private void CreateMissingLayersEditor()
    {
        Log("Creating missing layers...");
        
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        
        foreach (string layer in requiredLayers)
        {
            if (!LayerExists(layer))
            {
                // Find first empty layer slot (skip built-in layers 0-7)
                for (int i = 8; i < 32; i++)
                {
                    SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        layerProp.stringValue = layer;
                        layersCreated++;
                        Log($"Created layer: {layer} (index {i})");
                        break;
                    }
                }
            }
        }
        
        tagManager.ApplyModifiedProperties();
        
        if (layersCreated > 0)
        {
            AssetDatabase.SaveAssets();
            Log($"Saved {layersCreated} new layers to project");
        }
    }
    #endif

    /// <summary>
    /// Check if a tag exists
    /// </summary>
    private bool TagExists(string tag)
    {
        #if UNITY_EDITOR
        return System.Array.Exists(UnityEditorInternal.InternalEditorUtility.tags, t => t == tag);
        #else
        try
        {
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
        #endif
    }

    /// <summary>
    /// Check if a layer exists
    /// </summary>
    private bool LayerExists(string layer)
    {
        return LayerMask.NameToLayer(layer) != -1;
    }

    /// <summary>
    /// Apply tags to objects in the current scene
    /// </summary>
    [ContextMenu("Auto-Tag Objects in Scene")]
    public void AutoTagObjectsInScene()
    {
        Log("Auto-tagging objects in current scene...");
        
        int objectsTagged = 0;
        
        // Tag player objects
        GameObject[] playerObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in playerObjects)
        {
            if (obj.GetComponent<PlayerController>() && !obj.CompareTag("Player"))
            {
                if (TagExists("Player"))
                {
                    obj.tag = "Player";
                    objectsTagged++;
                    Log($"Tagged {obj.name} as Player");
                }
            }
        }
        
        // Tag collectible objects
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        foreach (CollectibleController collectible in collectibles)
        {
            if (!collectible.CompareTag("Collectible"))
            {
                if (TagExists("Collectible"))
                {
                    collectible.tag = "Collectible";
                    objectsTagged++;
                    Log($"Tagged {collectible.name} as Collectible");
                }
            }
        }
        
        // Tag goal zones
        GameObject[] potentialGoalZones = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in potentialGoalZones)
        {
            if (obj.name.ToLower().Contains("goal") || obj.name.ToLower().Contains("finish"))
            {
                if (TagExists("Finish") && !obj.CompareTag("Finish"))
                {
                    obj.tag = "Finish";
                    objectsTagged++;
                    Log($"Tagged {obj.name} as Finish");
                }
            }
        }
        
        // Tag ground objects
        foreach (GameObject obj in potentialGoalZones)
        {
            if ((obj.name.ToLower().Contains("ground") || obj.name.ToLower().Contains("floor") || obj.name.ToLower().Contains("platform")) 
                && !obj.CompareTag("Ground"))
            {
                if (TagExists("Ground"))
                {
                    obj.tag = "Ground";
                    objectsTagged++;
                    Log($"Tagged {obj.name} as Ground");
                }
            }
        }
        
        Log($"Auto-tagging completed. Tagged {objectsTagged} objects.");
    }

    /// <summary>
    /// Set up layer assignments for objects
    /// </summary>
    [ContextMenu("Auto-Assign Layers")]
    public void AutoAssignLayers()
    {
        Log("Auto-assigning layers to objects...");
        
        int layersAssigned = 0;
        
        // Assign player layer
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            foreach (GameObject player in playerObjects)
            {
                if (player.layer != playerLayer)
                {
                    player.layer = playerLayer;
                    layersAssigned++;
                    Log($"Assigned Player layer to {player.name}");
                }
            }
        }
        
        // Assign collectibles layer
        GameObject[] collectibleObjects = GameObject.FindGameObjectsWithTag("Collectible");
        int collectiblesLayer = LayerMask.NameToLayer("Collectibles");
        if (collectiblesLayer != -1)
        {
            foreach (GameObject collectible in collectibleObjects)
            {
                if (collectible.layer != collectiblesLayer)
                {
                    collectible.layer = collectiblesLayer;
                    layersAssigned++;
                    Log($"Assigned Collectibles layer to {collectible.name}");
                }
            }
        }
        
        // Assign ground layer
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer != -1)
        {
            foreach (GameObject ground in groundObjects)
            {
                if (ground.layer != groundLayer)
                {
                    ground.layer = groundLayer;
                    layersAssigned++;
                    Log($"Assigned Ground layer to {ground.name}");
                }
            }
        }
        
        Log($"Layer assignment completed. Assigned {layersAssigned} layers.");
    }

    /// <summary>
    /// Complete tag and layer setup for the scene
    /// </summary>
    [ContextMenu("Complete Tag Setup")]
    public void CompleteTagSetup()
    {
        CreateMissingTags();
        AutoTagObjectsInScene();
        AutoAssignLayers();
        ValidateTags();
        
        Log("Complete tag setup finished!");
    }

    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[TagManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[TagManager] {message}");
    }

    #region Editor Integration

    #if UNITY_EDITOR
    [MenuItem("Roll-a-Ball/Setup Tags and Layers")]
    public static void SetupTagsMenuItem()
    {
        TagManager tagManager = FindFirstObjectByType<TagManager>();
        if (!tagManager)
        {
            GameObject tagGO = new GameObject("TempTagManager");
            tagManager = tagGO.AddComponent<TagManager>();
        }
        
        tagManager.CompleteTagSetup();
        
        if (tagManager.gameObject.name == "TempTagManager")
        {
            DestroyImmediate(tagManager.gameObject);
        }
        
        EditorUtility.DisplayDialog("Tag Setup Complete", 
            "All required tags and layers have been created and assigned!", "OK");
    }
    #endif

    #endregion

    /// <summary>
    /// Add TagManager to current scene
    /// </summary>
    public static void AddToCurrentScene()
    {
        if (FindFirstObjectByType<TagManager>() == null)
        {
            GameObject tagGO = new GameObject("TagManager");
            tagGO.AddComponent<TagManager>();
            Debug.Log("Added TagManager to current scene");
        }
        else
        {
            Debug.Log("TagManager already exists in scene");
        }
    }
}
