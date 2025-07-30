using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central material management system for ground tiles
/// Ensures consistent, deterministic material assignment
/// </summary>
[AddComponentMenu("Environment/GroundMaterialController")]
public class GroundMaterialController : MonoBehaviour
{
    [Header("Material Configuration")]
    [SerializeField] private Material[] groundMaterials;
    [SerializeField] private bool useRandomMaterials = false;
    [SerializeField] private int materialSeed = 12345;
    [SerializeField] private float materialGroupSize = 4f; // Size of material groups
    
    [Header("Material Sources")]
    // TODO: Move material paths to a configuration ScriptableObject for easier updates
    [SerializeField] private string[] materialPaths = {
        "SteamGroundMaterial",
        "StandardGroundMaterial",
        "Assets/Material/Background"
    };
    
    [Header("Auto-Assignment")]
    [SerializeField] private bool assignOnStart = true;
    [SerializeField] private bool findGroundObjectsAutomatically = true;
    [SerializeField] private string[] groundObjectNames = { "ground", "floor", "platform", "tile" };
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugVisuals = false;
    [SerializeField] private bool logMaterialAssignments = false;
    
    // Private fields
    private System.Random materialRandom;
    private Dictionary<Vector2Int, Material> materialMap;
    private List<GameObject> managedGroundObjects;
    private Material[] availableMaterials;
    
    // Public properties
    public Material[] AvailableMaterials => availableMaterials;
    public int AssignedObjectCount => managedGroundObjects?.Count ?? 0;
    
    void Start()
    {
        if (assignOnStart)
        {
            AssignMaterials();
        }
    }
    
    /// <summary>
    /// Main entry point for material assignment
    /// </summary>
    [ContextMenu("Assign Materials")]
    public void AssignMaterials()
    {
        InitializeMaterialSystem();
        FindGroundObjects();
        ApplyMaterialsToGroundObjects();
        
        if (logMaterialAssignments)
        {
            Debug.Log($"[GroundMaterialController] Assigned materials to {managedGroundObjects.Count} objects");
        }
    }
    
    /// <summary>
    /// Initialize the material system
    /// </summary>
    private void InitializeMaterialSystem()
    {
        // TODO: Validate material sources in OnValidate() to warn about missing assets
        // Initialize random generator with seed
        materialRandom = new System.Random(materialSeed);
        materialMap = new Dictionary<Vector2Int, Material>();
        managedGroundObjects = new List<GameObject>();
        
        // Load available materials
        LoadAvailableMaterials();
        
        if (logMaterialAssignments)
        {
            Debug.Log($"[GroundMaterialController] Initialized with {availableMaterials.Length} materials");
        }
    }
    
    /// <summary>
    /// Load materials from various sources
    /// </summary>
    private void LoadAvailableMaterials()
    {
        List<Material> materials = new List<Material>();
        
        // 1. Use assigned materials first
        if (groundMaterials != null && groundMaterials.Length > 0)
        {
            materials.AddRange(groundMaterials.Where(m => m != null));
        }
        
        // 2. Try to load from Resources
        foreach (string path in materialPaths)
        {
            Material mat = Resources.Load<Material>(path);
            if (mat && !materials.Contains(mat))
            {
                materials.Add(mat);
            }
        }
        
        // 3. Try to find materials in the Material folder
        Material[] foundMaterials = Resources.LoadAll<Material>("Materials");
        foreach (Material mat in foundMaterials)
        {
            if (mat && !materials.Contains(mat) && IsGroundMaterial(mat))
            {
                materials.Add(mat);
            }
        }
        
        // 4. Fallback to creating basic materials
        if (materials.Count == 0)
        {
            materials.AddRange(CreateFallbackMaterials());
        }
        
        availableMaterials = materials.ToArray();
        
        if (logMaterialAssignments)
        {
            Debug.Log($"[GroundMaterialController] Loaded {availableMaterials.Length} materials: {string.Join(", ", availableMaterials.Select(m => m.name))}");
        }
    }
    
    /// <summary>
    /// Check if a material is suitable for ground use
    /// </summary>
    private bool IsGroundMaterial(Material material)
    {
        string name = material.name.ToLower();
        return name.Contains("ground") || name.Contains("floor") || 
               name.Contains("steam") || name.Contains("copper") ||
               name.Contains("metal") || name.Contains("stone");
    }
    
    /// <summary>
    /// Create fallback materials if none are found
    /// </summary>
    private Material[] CreateFallbackMaterials()
    {
        List<Material> fallbackMaterials = new List<Material>();
        
        // Create basic Steampunk-style materials
        Material copper = CreateMaterial("Steampunk_Copper", new Color(0.8f, 0.4f, 0.2f), 0.6f, 0.3f);
        Material brass = CreateMaterial("Steampunk_Brass", new Color(0.9f, 0.7f, 0.3f), 0.7f, 0.4f);
        Material iron = CreateMaterial("Steampunk_Iron", new Color(0.5f, 0.5f, 0.5f), 0.8f, 0.2f);
        Material bronze = CreateMaterial("Steampunk_Bronze", new Color(0.7f, 0.5f, 0.3f), 0.5f, 0.3f);
        
        fallbackMaterials.AddRange(new[] { copper, brass, iron, bronze });
        
        if (logMaterialAssignments)
        {
            Debug.Log("[GroundMaterialController] Created fallback Steampunk materials");
        }
        
        return fallbackMaterials.ToArray();
    }
    
    /// <summary>
    /// Create a material with specified properties
    /// </summary>
    private Material CreateMaterial(string name, Color albedo, float metallic, float smoothness)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = albedo;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }
    
    /// <summary>
    /// Find all ground objects in the scene
    /// </summary>
    private void FindGroundObjects()
    {
        managedGroundObjects.Clear();
        
        if (findGroundObjectsAutomatically)
        {
            // Find all GameObjects in the scene
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (GameObject obj in allObjects)
            {
                if (IsGroundObject(obj))
                {
                    managedGroundObjects.Add(obj);
                }
            }
        }
        else
        {
            // Only manage children of this object
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (IsGroundObject(child))
                {
                    managedGroundObjects.Add(child);
                }
            }
        }
        
        if (logMaterialAssignments)
        {
            Debug.Log($"[GroundMaterialController] Found {managedGroundObjects.Count} ground objects");
        }
    }
    
    /// <summary>
    /// Check if an object is a ground object
    /// </summary>
    private bool IsGroundObject(GameObject obj)
    {
        string name = obj.name.ToLower();
        
        // Check by name patterns
        foreach (string pattern in groundObjectNames)
        {
            if (name.Contains(pattern.ToLower()))
            {
                return true;
            }
        }
        
        // Check if it's in a ground container
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLower();
            if (parentName.Contains("ground") || parentName.Contains("floor"))
            {
                return true;
            }
            parent = parent.parent;
        }
        
        // Check if it has a renderer (must be visible to need a material)
        return obj.GetComponent<Renderer>() != null;
    }
    
    /// <summary>
    /// Apply materials to all ground objects using deterministic algorithm
    /// </summary>
    private void ApplyMaterialsToGroundObjects()
    {
        if (availableMaterials == null || availableMaterials.Length == 0)
        {
            Debug.LogWarning("[GroundMaterialController] No materials available for assignment!");
            return;
        }
        
        foreach (GameObject groundObj in managedGroundObjects)
        {
            Material assignedMaterial = GetMaterialForPosition(groundObj.transform.position);
            ApplyMaterialToObject(groundObj, assignedMaterial);
        }
    }
    
    /// <summary>
    /// Get material for a specific world position (deterministic)
    /// </summary>
    private Material GetMaterialForPosition(Vector3 worldPosition)
    {
        if (useRandomMaterials)
        {
            // Pure random assignment
            return availableMaterials[materialRandom.Next(availableMaterials.Length)];
        }
        else
        {
            // Deterministic grid-based assignment
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / materialGroupSize),
                Mathf.FloorToInt(worldPosition.z / materialGroupSize)
            );
            
            // Check if we already assigned a material to this grid position
            if (materialMap.ContainsKey(gridPos))
            {
                return materialMap[gridPos];
            }
            
            // Generate deterministic material index for this grid position
            int hash = gridPos.x * 73856093 ^ gridPos.y * 19349663; // Simple hash
            int materialIndex = Mathf.Abs(hash) % availableMaterials.Length;
            
            Material selectedMaterial = availableMaterials[materialIndex];
            materialMap[gridPos] = selectedMaterial;
            
            return selectedMaterial;
        }
    }
    
    /// <summary>
    /// Apply material to a specific object
    /// </summary>
    private void ApplyMaterialToObject(GameObject obj, Material material)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        // TODO: Cache renderer references to avoid repeated GetComponent calls
        if (renderer && renderer.material != material)
        {
            renderer.material = material;
            
            if (logMaterialAssignments)
            {
                Debug.Log($"[GroundMaterialController] Applied {material.name} to {obj.name}");
            }
        }
    }
    
    /// <summary>
    /// Reset all materials to the first available material
    /// </summary>
    [ContextMenu("Reset Materials")]
    public void ResetMaterials()
    {
        if (availableMaterials == null || availableMaterials.Length == 0)
        {
            Debug.LogWarning("[GroundMaterialController] No materials available for reset!");
            return;
        }
        
        Material defaultMaterial = availableMaterials[0];
        
        foreach (GameObject groundObj in managedGroundObjects)
        {
            ApplyMaterialToObject(groundObj, defaultMaterial);
        }
        
        materialMap.Clear();
        
        Debug.Log($"[GroundMaterialController] Reset {managedGroundObjects.Count} objects to {defaultMaterial.name}");
    }
    
    /// <summary>
    /// Refresh material assignments (useful after objects are added/removed)
    /// </summary>
    [ContextMenu("Refresh Assignments")]
    public void RefreshAssignments()
    {
        AssignMaterials();
    }
    
    /// <summary>
    /// Add a new ground object to be managed
    /// </summary>
    public void AddGroundObject(GameObject groundObj)
    {
        if (managedGroundObjects == null)
            managedGroundObjects = new List<GameObject>();
        
        if (!managedGroundObjects.Contains(groundObj))
        {
            managedGroundObjects.Add(groundObj);
            
            if (availableMaterials != null && availableMaterials.Length > 0)
            {
                Material material = GetMaterialForPosition(groundObj.transform.position);
                ApplyMaterialToObject(groundObj, material);
            }
        }
    }
    
    /// <summary>
    /// Remove a ground object from management
    /// </summary>
    public void RemoveGroundObject(GameObject groundObj)
    {
        if (managedGroundObjects != null)
        {
            managedGroundObjects.Remove(groundObj);
        }
    }
    
    /// <summary>
    /// Get statistics about material usage
    /// </summary>
    public Dictionary<Material, int> GetMaterialUsageStats()
    {
        Dictionary<Material, int> stats = new Dictionary<Material, int>();
        
        foreach (GameObject groundObj in managedGroundObjects)
        {
            Renderer renderer = groundObj.GetComponent<Renderer>();
            if (renderer && renderer.material)
            {
                Material mat = renderer.material;
                if (stats.ContainsKey(mat))
                    stats[mat]++;
                else
                    stats[mat] = 1;
            }
        }
        
        return stats;
    }
    
    #region Debug Visualization
    
    void OnDrawGizmosSelected()
    {
        if (!enableDebugVisuals || materialMap == null) return;
        // TODO: Disable or limit gizmo drawing in production builds
        
        // Draw material groups
        foreach (var kvp in materialMap)
        {
            Vector2Int gridPos = kvp.Key;
            Material material = kvp.Value;
            
            Vector3 worldPos = new Vector3(
                gridPos.x * materialGroupSize + materialGroupSize * 0.5f,
                0.1f,
                gridPos.y * materialGroupSize + materialGroupSize * 0.5f
            );
            
            // Use material color for visualization
            Gizmos.color = material.color;
            Gizmos.DrawWireCube(worldPos, new Vector3(materialGroupSize, 0.1f, materialGroupSize));
        }
    }
    
    #endregion
}
