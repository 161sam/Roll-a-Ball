using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Comprehensive repair tool for the Generated Level
/// Fixes collectibles, materials, and validates all objects
/// </summary>
[AddComponentMenu("Tools/GeneratedLevelFixer")]
public class GeneratedLevelFixer : MonoBehaviour
{
    [Header("Fix Configuration")]
    [SerializeField] private bool autoFixOnStart = true;
    [SerializeField] private bool enableDebugLogging = true;
    
    [Header("Collectible Fixes")]
    [SerializeField] private bool fixCollectibleTriggers = true;
    [SerializeField] private bool fixCollectibleTags = true;
    [SerializeField] private bool fixCollectibleScripts = true;
    [SerializeField] private bool addMissingColliders = true;
    
    [Header("Material System")]
    [SerializeField] private bool fixGroundMaterials = true;
    [SerializeField] private bool createSteampunkMaterials = true;
    [SerializeField] private int materialSeed = 12345;
    
    [Header("Audio & Effects")]
    [SerializeField] private bool addCollectionSounds = true;
    [SerializeField] private bool addParticleEffects = true;
    
    [Header("Material References")]
    [SerializeField] private Material[] steampunkGroundMaterials;
    [SerializeField] private Material[] steampunkWallMaterials;
    [SerializeField] private Material collectibleMaterial;
    [SerializeField] private Material goalZoneMaterial;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip levelCompleteSound;
    
    // Fix statistics
    private int fixedCollectibles = 0;
    private int fixedMaterials = 0;
    private int missingComponents = 0;
    private List<string> fixResults = new List<string>();
    
    void Start()
    {
        if (autoFixOnStart)
        {
            StartCoroutine(FixGeneratedLevelAsync());
        }
    }
    
    [ContextMenu("Fix Generated Level")]
    public void FixGeneratedLevel()
    {
        StartCoroutine(FixGeneratedLevelAsync());
    }
    
    /// <summary>
    /// Main repair routine - fixes all issues in the Generated Level
    /// </summary>
    public IEnumerator FixGeneratedLevelAsync()
    {
        LogFix("🔧 Starting Generated Level repair...", true);
        
        // Reset statistics
        fixedCollectibles = 0;
        fixedMaterials = 0;
        missingComponents = 0;
        fixResults.Clear();
        
        // 1. Create Steampunk materials if needed
        if (createSteampunkMaterials)
        {
            yield return StartCoroutine(CreateSteampunkMaterials());
        }
        
        // 2. Fix all collectibles
        if (fixCollectibleTriggers || fixCollectibleTags || fixCollectibleScripts)
        {
            yield return StartCoroutine(FixAllCollectibles());
        }
        
        // 3. Fix ground materials
        if (fixGroundMaterials)
        {
            yield return StartCoroutine(FixGroundMaterialSystem());
        }
        
        // 4. Validate and fix scene objects
        yield return StartCoroutine(ValidateSceneObjects());
        
        // 5. Setup audio and effects
        if (addCollectionSounds || addParticleEffects)
        {
            yield return StartCoroutine(SetupAudioAndEffects());
        }
        
        // 6. Final validation
        yield return StartCoroutine(FinalSceneValidation());
        
        // 7. Generate repair report
        GenerateRepairReport();
        
        LogFix("✅ Generated Level repair completed!", true);
    }
    
    #region Material System
    
    /// <summary>
    /// Creates consistent Steampunk materials with deterministic assignment
    /// </summary>
    private IEnumerator CreateSteampunkMaterials()
    {
        LogFix("🎨 Creating Steampunk material system...");
        
        // Create materials directory if it doesn't exist
        string materialPath = "Assets/Materials/Steampunk/";
        
        #if UNITY_EDITOR
        if (!UnityEditor.AssetDatabase.IsValidFolder(materialPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/Materials", "Steampunk");
        }
        #endif
        
        // Load or create ground materials
        steampunkGroundMaterials = LoadOrCreateGroundMaterials();
        steampunkWallMaterials = LoadOrCreateWallMaterials();
        collectibleMaterial = LoadOrCreateCollectibleMaterial();
        goalZoneMaterial = LoadOrCreateGoalZoneMaterial();
        
        LogFix($"📦 Created {steampunkGroundMaterials?.Length ?? 0} ground materials");
        
        yield return null;
    }
    
    private Material[] LoadOrCreateGroundMaterials()
    {
        List<Material> materials = new List<Material>();
        
        // Try to load existing materials first
        Material existingGround = Resources.Load<Material>("SteamGroundMaterial");
        if (existingGround)
        {
            materials.Add(existingGround);
        }
        
        // Load standard materials as fallback
        Material standardGround = Resources.Load<Material>("StandardGroundMaterial");
        if (standardGround)
        {
            materials.Add(standardGround);
        }
        
        // If no materials found, use defaults
        if (materials.Count == 0)
        {
            // Create basic materials programmatically
            Material copper = CreateBasicMaterial("Steampunk_Copper", new Color(0.8f, 0.4f, 0.2f));
            Material brass = CreateBasicMaterial("Steampunk_Brass", new Color(0.9f, 0.7f, 0.3f));
            Material iron = CreateBasicMaterial("Steampunk_Iron", new Color(0.5f, 0.5f, 0.5f));
            
            materials.AddRange(new[] { copper, brass, iron });
        }
        
        return materials.ToArray();
    }
    
    private Material[] LoadOrCreateWallMaterials()
    {
        List<Material> materials = new List<Material>();
        
        // Try to load existing wall materials
        Material existingWall = Resources.Load<Material>("SteamWallMaterial");
        if (existingWall)
        {
            materials.Add(existingWall);
        }
        
        Material standardWall = Resources.Load<Material>("StandardWallMaterial");
        if (standardWall)
        {
            materials.Add(standardWall);
        }
        
        if (materials.Count == 0)
        {
            // Create basic wall materials
            Material darkMetal = CreateBasicMaterial("Steampunk_DarkMetal", new Color(0.3f, 0.3f, 0.3f));
            materials.Add(darkMetal);
        }
        
        return materials.ToArray();
    }
    
    private Material LoadOrCreateCollectibleMaterial()
    {
        // Try to load existing
        Material existing = Resources.Load<Material>("CollectibleMaterial");
        if (existing) return existing;
        
        // Create emissive collectible material
        Material emissive = CreateBasicMaterial("Steampunk_Collectible", Color.yellow);
        emissive.EnableKeyword("_EMISSION");
        emissive.SetColor("_EmissionColor", Color.yellow * 0.5f);
        
        return emissive;
    }
    
    private Material LoadOrCreateGoalZoneMaterial()
    {
        // Try to load existing
        Material existing = Resources.Load<Material>("GoalZoneMaterial");
        if (existing) return existing;
        
        existing = Resources.Load<Material>("StandardGoalZoneMaterial");
        if (existing) return existing;
        
        // Create goal zone material
        return CreateBasicMaterial("Steampunk_GoalZone", Color.green);
    }
    
    private Material CreateBasicMaterial(string name, Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        mat.SetFloat("_Metallic", 0.5f);
        mat.SetFloat("_Smoothness", 0.3f);
        return mat;
    }
    
    #endregion
    
    #region Collectible Fixes
    
    /// <summary>
    /// Fixes all collectibles in the scene
    /// </summary>
    private IEnumerator FixAllCollectibles()
    {
        LogFix("🎯 Fixing collectibles...");
        
        // Find all potential collectible objects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> collectibleObjects = new List<GameObject>();
        
        // Find collectibles by various methods
        foreach (GameObject obj in allObjects)
        {
            if (IsCollectibleObject(obj))
            {
                collectibleObjects.Add(obj);
            }
        }
        
        LogFix($"🔍 Found {collectibleObjects.Count} potential collectibles");
        
        // Fix each collectible
        int index = 0;
        foreach (GameObject collectible in collectibleObjects)
        {
            FixSingleCollectible(collectible);
            fixedCollectibles++;
            
            // Yield every 10 objects
            if (++index % 10 == 0)
                yield return null;
        }
        
        LogFix($"✅ Fixed {fixedCollectibles} collectibles");
    }
    
    private bool IsCollectibleObject(GameObject obj)
    {
        // Check by name patterns
        string name = obj.name.ToLower();
        if (name.Contains("collectible") || name.Contains("pickup") || 
            name.Contains("coin") || name.Contains("kronkorken"))
        {
            return true;
        }
        
        // Check by tag
        if (obj.CompareTag("Collectible"))
        {
            return true;
        }
        
        // Check by component
        if (obj.GetComponent<CollectibleController>())
        {
            return true;
        }
        
        // Check if it's in the collectibles container
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (parent.name.ToLower().Contains("collectible"))
                return true;
            parent = parent.parent;
        }
        
        return false;
    }
    
    private void FixSingleCollectible(GameObject collectible)
    {
        bool wasFixed = false;
        
        // 1. Fix or add CollectibleController
        if (fixCollectibleScripts)
        {
            CollectibleController controller = collectible.GetComponent<CollectibleController>();
            if (!controller)
            {
                controller = collectible.AddComponent<CollectibleController>();
                LogFix($"➕ Added CollectibleController to {collectible.name}");
                wasFixed = true;
            }
        }
        
        // 2. Fix or add Collider (Trigger)
        if (addMissingColliders)
        {
            Collider collider = collectible.GetComponent<Collider>();
            if (!collider)
            {
                SphereCollider sphere = collectible.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.5f;
                LogFix($"➕ Added SphereCollider (trigger) to {collectible.name}");
                wasFixed = true;
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = true;
                LogFix($"🔧 Fixed trigger on {collectible.name}");
                wasFixed = true;
            }
        }
        
        // 3. Fix tags
        if (fixCollectibleTags)
        {
            if (!collectible.CompareTag("Collectible"))
            {
                // Ensure Collectible tag exists
                #if UNITY_EDITOR
                SerializedObject tagManager = new UnityEditor.SerializedObject(
                    UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                UnityEditor.SerializedProperty tagsProp = tagManager.FindProperty("tags");
                
                bool tagExists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Collectible")
                    {
                        tagExists = true;
                        break;
                    }
                }
                
                if (!tagExists)
                {
                    tagsProp.InsertArrayElementAtIndex(0);
                    tagsProp.GetArrayElementAtIndex(0).stringValue = "Collectible";
                    tagManager.ApplyModifiedProperties();
                    LogFix("➕ Added 'Collectible' tag to project");
                }
                #endif
                
                collectible.tag = "Collectible";
                LogFix($"🏷️ Fixed tag on {collectible.name}");
                wasFixed = true;
            }
        }
        
        // 4. Apply collectible material
        if (collectibleMaterial)
        {
            Renderer renderer = collectible.GetComponent<Renderer>();
            if (renderer && renderer.material != collectibleMaterial)
            {
                renderer.material = collectibleMaterial;
                LogFix($"🎨 Applied collectible material to {collectible.name}");
                wasFixed = true;
            }
        }
        
        // 5. Add audio source if missing
        if (addCollectionSounds)
        {
            AudioSource audioSource = collectible.GetComponent<AudioSource>();
            if (!audioSource)
            {
                audioSource = collectible.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.maxDistance = 20f;
                
                if (collectSound)
                    audioSource.clip = collectSound;
                
                LogFix($"🔊 Added AudioSource to {collectible.name}");
                wasFixed = true;
            }
        }
        
        if (wasFixed)
        {
            fixResults.Add($"Fixed collectible: {collectible.name}");
        }
    }
    
    #endregion
    
    #region Ground Material System
    
    /// <summary>
    /// Fixes ground materials with deterministic assignment
    /// </summary>
    private IEnumerator FixGroundMaterialSystem()
    {
        LogFix("🌍 Fixing ground material system...");
        
        if (steampunkGroundMaterials == null || steampunkGroundMaterials.Length == 0)
        {
            LogFix("⚠️ No ground materials available, skipping material fixes");
            yield break;
        }
        
        // Find all ground objects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> groundObjects = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (IsGroundObject(obj))
            {
                groundObjects.Add(obj);
            }
        }
        
        LogFix($"🔍 Found {groundObjects.Count} ground objects");
        
        // Apply materials using deterministic seed
        System.Random materialRandom = new System.Random(materialSeed);
        
        // Group ground objects by position for consistent material assignment
        var positionGroups = groundObjects
            .GroupBy(g => new Vector2Int(
                Mathf.RoundToInt(g.transform.position.x / 2f),
                Mathf.RoundToInt(g.transform.position.z / 2f)
            ))
            .ToList();
        
        int groupIndex = 0;
        foreach (var group in positionGroups)
        {
            Material selectedMaterial = steampunkGroundMaterials[groupIndex % steampunkGroundMaterials.Length];
            
            foreach (GameObject groundObj in group)
            {
                Renderer renderer = groundObj.GetComponent<Renderer>();
                if (renderer && renderer.material != selectedMaterial)
                {
                    renderer.material = selectedMaterial;
                    fixedMaterials++;
                }
            }
            
            groupIndex++;
            
            // Yield every 20 groups
            if (groupIndex % 20 == 0)
                yield return null;
        }
        
        LogFix($"✅ Fixed {fixedMaterials} ground materials");
    }
    
    private bool IsGroundObject(GameObject obj)
    {
        string name = obj.name.ToLower();
        if (name.Contains("ground") || name.Contains("floor") || name.Contains("platform"))
        {
            return true;
        }
        
        // Check if it's in the ground container
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (parent.name.ToLower().Contains("ground"))
                return true;
            parent = parent.parent;
        }
        
        return false;
    }
    
    #endregion
    
    #region Scene Validation
    
    /// <summary>
    /// Validates all objects in the scene for missing components
    /// </summary>
    private IEnumerator ValidateSceneObjects()
    {
        LogFix("🔍 Validating scene objects...");
        
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int checkedObjects = 0;
        
        foreach (GameObject obj in allObjects)
        {
            ValidateSingleObject(obj);
            checkedObjects++;
            
            // Yield every 50 objects
            if (checkedObjects % 50 == 0)
                yield return null;
        }
        
        LogFix($"🔍 Validated {checkedObjects} objects, found {missingComponents} issues");
    }
    
    private void ValidateSingleObject(GameObject obj)
    {
        // Check for missing scripts
        Component[] components = obj.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp == null)
            {
                LogFix($"⚠️ Missing script on {obj.name}", false);
                missingComponents++;
            }
        }
        
        // Check specific object types
        if (obj.name.ToLower().Contains("player"))
        {
            ValidatePlayer(obj);
        }
        else if (obj.name.ToLower().Contains("goalzone") || obj.name.ToLower().Contains("goal"))
        {
            ValidateGoalZone(obj);
        }
    }
    
    private void ValidatePlayer(GameObject player)
    {
        // Ensure player has correct tag
        if (!player.CompareTag("Player"))
        {
            player.tag = "Player";
            LogFix($"🏷️ Fixed Player tag on {player.name}");
        }
        
        // Check for essential components
        if (!player.GetComponent<PlayerController>())
        {
            LogFix($"⚠️ Player missing PlayerController: {player.name}", false);
            missingComponents++;
        }
        
        if (!player.GetComponent<Rigidbody>())
        {
            LogFix($"⚠️ Player missing Rigidbody: {player.name}", false);
            missingComponents++;
        }
    }
    
    private void ValidateGoalZone(GameObject goalZone)
    {
        // Ensure goal zone has correct tag
        if (!goalZone.CompareTag("Finish"))
        {
            goalZone.tag = "Finish";
            LogFix($"🏷️ Fixed Finish tag on {goalZone.name}");
        }
        
        // Apply goal zone material
        if (goalZoneMaterial)
        {
            Renderer renderer = goalZone.GetComponent<Renderer>();
            if (renderer && renderer.material != goalZoneMaterial)
            {
                renderer.material = goalZoneMaterial;
                LogFix($"🎨 Applied goal zone material to {goalZone.name}");
            }
        }
    }
    
    #endregion
    
    #region Audio and Effects
    
    /// <summary>
    /// Sets up audio and particle effects
    /// </summary>
    private IEnumerator SetupAudioAndEffects()
    {
        LogFix("🔊 Setting up audio and effects...");
        
        // Setup global audio manager if needed
        AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        if (!audioManager)
        {
            GameObject audioManagerGO = new GameObject("AudioManager");
            audioManager = audioManagerGO.AddComponent<AudioManager>();
            LogFix("➕ Created AudioManager");
        }
        
        // TODO: Add particle effects setup
        
        yield return null;
    }
    
    #endregion
    
    #region Final Validation
    
    /// <summary>
    /// Final validation and integration with game systems
    /// </summary>
    private IEnumerator FinalSceneValidation()
    {
        LogFix("🔍 Final scene validation...");
        
        // Ensure LevelManager is aware of all collectibles
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager)
        {
            // Force LevelManager to re-scan collectibles
            if (enableDebugLogging)
            {
                LogFix("🔄 Refreshing LevelManager collectible count");
            }
            
            levelManager.UpdateCollectibleCount();
        }
        
        // Ensure GameManager integration
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager)
        {
            gameManager.UpdateCollectibleCount();
        }
        
        // Validate UI connections
        UIController uiController = FindFirstObjectByType<UIController>();
        if (uiController)
        {
            // Could trigger UI refresh here
        }
        
        yield return null;
        
        LogFix("✅ Final validation completed");
    }
    
    #endregion
    
    #region Reporting
    
    /// <summary>
    /// Generates a comprehensive repair report
    /// </summary>
    private void GenerateRepairReport()
    {
        string reportPath = "/home/saschi/Games/Roll-a-Ball/Reports/SceneReport_GeneratedLevel.md";
        
        string report = GenerateMarkdownReport();
        
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath));
            System.IO.File.WriteAllText(reportPath, report);
            LogFix($"📄 Repair report saved to: {reportPath}");
        }
        catch (System.Exception e)
        {
            LogFix($"❌ Failed to save report: {e.Message}", false);
        }
    }
    
    private string GenerateMarkdownReport()
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("# Generated Level Repair Report");
        report.AppendLine($"**Generated:** {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Scene:** {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        report.AppendLine();
        
        // Summary
        report.AppendLine("## Summary");
        report.AppendLine($"- **Collectibles Fixed:** {fixedCollectibles}");
        report.AppendLine($"- **Materials Fixed:** {fixedMaterials}");
        report.AppendLine($"- **Missing Components:** {missingComponents}");
        report.AppendLine($"- **Total Issues Addressed:** {fixResults.Count}");
        report.AppendLine();
        
        // Configuration
        report.AppendLine("## Fix Configuration");
        report.AppendLine($"- Fix Collectible Triggers: {fixCollectibleTriggers}");
        report.AppendLine($"- Fix Collectible Tags: {fixCollectibleTags}");
        report.AppendLine($"- Fix Collectible Scripts: {fixCollectibleScripts}");
        report.AppendLine($"- Add Missing Colliders: {addMissingColliders}");
        report.AppendLine($"- Fix Ground Materials: {fixGroundMaterials}");
        report.AppendLine($"- Material Seed: {materialSeed}");
        report.AppendLine();
        
        // Detailed Results
        if (fixResults.Count > 0)
        {
            report.AppendLine("## Detailed Fix Results");
            foreach (string result in fixResults)
            {
                report.AppendLine($"- {result}");
            }
            report.AppendLine();
        }
        
        // Materials Info
        if (steampunkGroundMaterials != null && steampunkGroundMaterials.Length > 0)
        {
            report.AppendLine("## Material System");
            report.AppendLine($"- Ground Materials: {steampunkGroundMaterials.Length}");
            report.AppendLine($"- Wall Materials: {steampunkWallMaterials?.Length ?? 0}");
            report.AppendLine($"- Collectible Material: {(collectibleMaterial ? "✅" : "❌")}");
            report.AppendLine($"- Goal Zone Material: {(goalZoneMaterial ? "✅" : "❌")}");
            report.AppendLine();
        }
        
        // Recommendations
        report.AppendLine("## Recommendations");
        if (missingComponents > 0)
        {
            report.AppendLine("- ⚠️ Review objects with missing components");
        }
        if (fixedCollectibles == 0)
        {
            report.AppendLine("- ℹ️ No collectibles needed fixing - good!");
        }
        if (fixedMaterials == 0)
        {
            report.AppendLine("- ℹ️ No materials needed fixing - good!");
        }
        
        report.AppendLine();
        report.AppendLine("## Status");
        report.AppendLine("✅ **Generated Level repair completed successfully!**");
        
        return report.ToString();
    }
    
    #endregion
    
    #region Utilities
    
    private void LogFix(string message, bool isImportant = false)
    {
        if (!enableDebugLogging && !isImportant) return;
        
        Debug.Log($"[GeneratedLevelFixer] {message}");
    }
    
    #endregion
}
