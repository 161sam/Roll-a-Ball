using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fixes all collectible-related issues across scenes
/// Ensures proper colliders, tags, events, and component connections
/// </summary>
[AddComponentMenu("Roll-a-Ball/Collectible Diagnostic Tool")]
public class CollectibleDiagnosticTool : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    [SerializeField] private bool autoRunOnStart = true;
    [SerializeField] private bool enableAutoRepair = true;
    [SerializeField] private bool verboseLogging = true;
    
    [Header("Repair Options")]
    [SerializeField] private bool fixMissingColliders = true;
    [SerializeField] private bool fixIncorrectTags = true;
    [SerializeField] private bool reconnectEventListeners = true;
    [SerializeField] private bool resetCollectedStates = true;
    
    [Header("Manual Controls")]
    [SerializeField] private bool runDiagnosticNow = false;
    [SerializeField] private bool forceCollectAll = false;
    
    // Diagnostic results
    private List<CollectibleController> foundCollectibles = new List<CollectibleController>();
    private List<GameObject> brokenCollectibles = new List<GameObject>();
    private int repairedCount = 0;

    private void Start()
    {
        if (autoRunOnStart)
        {
            RunDiagnostic();
        }
    }

    private void OnValidate()
    {
        if (runDiagnosticNow)
        {
            runDiagnosticNow = false;
            RunDiagnostic();
        }
        
        if (forceCollectAll)
        {
            forceCollectAll = false;
            ForceCollectAllCollectibles();
        }
    }

    /// <summary>
    /// Main diagnostic and repair function
    /// </summary>
    [ContextMenu("Run Collectible Diagnostic")]
    public void RunDiagnostic()
    {
        Log("=== COLLECTIBLE DIAGNOSTIC TOOL ===");
        
        // Clear previous results
        foundCollectibles.Clear();
        brokenCollectibles.Clear();
        repairedCount = 0;
        
        // Step 1: Find all collectibles
        FindAllCollectibles();
        
        // Step 2: Diagnose each collectible
        DiagnoseCollectibles();
        
        // Step 3: Find objects that should be collectibles but aren't
        FindPotentialCollectibles();
        
        // Step 4: Validate level manager connections
        ValidateLevelManagerConnections();
        
        // Step 5: Test collection functionality
        TestCollectionFunctionality();
        
        // Final report
        GenerateReport();
    }

    #region Diagnostic Steps
    
    private void FindAllCollectibles()
    {
        Log("Step 1: Finding all CollectibleController components...");
        
        CollectibleController[] controllers = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        foundCollectibles.AddRange(controllers);
        
        Log($"Found {foundCollectibles.Count} CollectibleController components");
        
        // Also check for objects tagged as Collectible
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("Collectible");
        Log($"Found {taggedObjects.Length} objects with 'Collectible' tag");
        
        foreach (GameObject obj in taggedObjects)
        {
            // Skip prefabs and tool objects
            if (obj.name.Contains("Prefab") || obj.name.Contains("Tool") || obj.name.Contains("Manager"))
            {
                continue; // Skip prefabs and management objects
            }
            
            if (!obj.GetComponent<CollectibleController>())
            {
                LogWarning($"Object '{obj.name}' has Collectible tag but no CollectibleController!");
                brokenCollectibles.Add(obj);
            }
        }
    }
    
    private void DiagnoseCollectibles()
    {
        Log("Step 2: Diagnosing each collectible...");
        
        foreach (CollectibleController collectible in foundCollectibles)
        {
            if (collectible == null) continue;
            
            bool needsRepair = false;
            List<string> issues = new List<string>();
            
            // Check collider
            Collider collider = collectible.GetComponent<Collider>();
            if (!collider)
            {
                issues.Add("Missing Collider");
                needsRepair = true;
            }
            else if (!collider.isTrigger)
            {
                issues.Add("Collider is not a trigger");
                needsRepair = true;
            }
            
            // Check tag
            if (!collectible.CompareTag("Collectible"))
            {
                issues.Add($"Wrong tag: '{collectible.tag}' should be 'Collectible'");
                needsRepair = true;
            }
            
            // Check if already collected
            if (collectible.IsCollected)
            {
                issues.Add("Already collected (should be reset)");
                needsRepair = true;
            }
            
            // Check for required components
            if (!collectible.GetComponent<Renderer>())
            {
                issues.Add("Missing Renderer component");
            }
            
            // Report issues
            if (issues.Count > 0)
            {
                LogWarning($"Collectible '{collectible.name}' issues: {string.Join(", ", issues)}");
                
                if (enableAutoRepair && needsRepair)
                {
                    RepairCollectible(collectible, issues);
                }
            }
            else
            {
                Log($"Collectible '{collectible.name}' is OK");
            }
        }
    }
    
    private void FindPotentialCollectibles()
    {
        Log("Step 3: Looking for potential collectibles without controllers...");
        
        // Look for objects with common collectible names
        string[] collectibleNames = { "collectible", "pickup", "item", "coin", "gem", "power", "kronkorken", "sicherheitsnadel" };
        
        // Exclude these types of objects (tools, managers, UI, prefabs, etc.)
        string[] excludedTerms = { "tool", "diagnostic", "manager", "controller", "fixer", "ui", "canvas", "button", "text", "panel", "system", "generator", "helper", "setup", "prefab" };
        
        foreach (string name in collectibleNames)
        {
            GameObject[] potentialCollectibles = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => {
                    string objName = go.name.ToLower();
                    // Must contain collectible name
                    if (!objName.Contains(name.ToLower())) return false;
                    // Must not have CollectibleController already
                    if (go.GetComponent<CollectibleController>()) return false;
                    // Must not contain excluded terms
                    foreach (string excluded in excludedTerms)
                    {
                        if (objName.Contains(excluded.ToLower())) return false;
                    }
                    // Must have a renderer (actual visual object)
                    if (!go.GetComponent<Renderer>()) return false;
                    // Must not be a UI object
                    if (go.GetComponentInParent<Canvas>()) return false;
                    return true;
                })
                .ToArray();
                
            foreach (GameObject potential in potentialCollectibles)
            {
                LogWarning($"Potential collectible without controller: '{potential.name}'");
                
                if (enableAutoRepair)
                {
                    AddCollectibleController(potential);
                }
            }
        }
    }
    
    private void ValidateLevelManagerConnections()
    {
        Log("Step 4: Validating LevelManager connections...");
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (!levelManager)
        {
            LogWarning("No LevelManager found in scene!");
            return;
        }
        
        // Force level manager to update its collectible list
        levelManager.UpdateCollectibleCount();
        
        int totalCollectibles = levelManager.TotalCollectibles;
        int remaining = levelManager.CollectiblesRemaining;
        
        Log($"LevelManager reports: {remaining}/{totalCollectibles} collectibles remaining");
        
        if (totalCollectibles != foundCollectibles.Count)
        {
            LogWarning($"Mismatch: Found {foundCollectibles.Count} collectibles, but LevelManager reports {totalCollectibles}");
        }
    }
    
    private void TestCollectionFunctionality()
    {
        Log("Step 5: Testing collection functionality...");
        
        // Check if player exists and can collect
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (!player)
        {
            LogWarning("No PlayerController found - cannot test collection!");
            return;
        }
        
        Log($"Player found at position: {player.transform.position}");
        
        // Check distances to collectibles
        foreach (CollectibleController collectible in foundCollectibles)
        {
            if (collectible == null) continue;
            
            float distance = Vector3.Distance(player.transform.position, collectible.transform.position);
            if (distance < 2f)
            {
                Log($"Collectible '{collectible.name}' is within collection range ({distance:F2}m)");
            }
        }
    }
    
    #endregion
    
    #region Repair Functions
    
    private void RepairCollectible(CollectibleController collectible, List<string> issues)
    {
        Log($"Repairing collectible '{collectible.name}'...");
        
        foreach (string issue in issues)
        {
            if (issue.Contains("Missing Collider") && fixMissingColliders)
            {
                SphereCollider collider = // collectible // Fixed: UniversalSceneFixture has no gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.5f;
                Log($"Added SphereCollider to '{collectible.name}'");
                repairedCount++;
            }
            else if (issue.Contains("not a trigger") && fixMissingColliders)
            {
                Collider collider = collectible.GetComponent<Collider>();
                collider.isTrigger = true;
                Log($"Set trigger flag on '{collectible.name}'");
                repairedCount++;
            }
            else if (issue.Contains("Wrong tag") && fixIncorrectTags)
            {
                try
                {
                    collectible.tag = "Collectible";
                    Log($"Fixed tag on '{collectible.name}'");
                    repairedCount++;
                }
                catch (UnityException)
                {
                    LogWarning($"Cannot set Collectible tag - tag doesn't exist in project settings");
                }
            }
            else if (issue.Contains("Already collected") && resetCollectedStates)
            {
                collectible.ResetCollectible();
                Log($"Reset collected state for '{collectible.name}'");
                repairedCount++;
            }
        }
        
        // Reconnect event listeners
        if (reconnectEventListeners)
        {
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager)
            {
                levelManager.AddCollectible(collectible);
                Log($"Reconnected '{collectible.name}' to LevelManager");
            }
        }
    }
    
    private void AddCollectibleController(GameObject target)
    {
        Log($"Adding CollectibleController to '{target.name}'...");
        
        CollectibleController controller = target.AddComponent<CollectibleController>();
        
        // Set up basic configuration
        if (!target.GetComponent<Collider>())
        {
            SphereCollider collider = target.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }
        
        // Set tag
        try
        {
            target.tag = "Collectible";
        }
        catch (UnityException)
        {
            LogWarning("Collectible tag not found in project settings");
        }
        
        foundCollectibles.Add(controller);
        repairedCount++;
        
        Log($"Successfully added CollectibleController to '{target.name}'");
    }
    
    #endregion
    
    #region Utility Functions
    
    /// <summary>
    /// Force collect all collectibles (for testing)
    /// </summary>
    [ContextMenu("Force Collect All")]
    public void ForceCollectAllCollectibles()
    {
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        
        foreach (CollectibleController collectible in collectibles)
        {
            if (!collectible.IsCollected)
            {
                collectible.ForceCollect();
            }
        }
        
        Log($"Force collected {collectibles.Length} collectibles");
    }
    
    /// <summary>
    /// Reset all collectibles in scene
    /// </summary>
    [ContextMenu("Reset All Collectibles")]
    public void ResetAllCollectibles()
    {
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        
        foreach (CollectibleController collectible in collectibles)
        {
            collectible.ResetCollectible();
        }
        
        Log($"Reset {collectibles.Length} collectibles");
    }
    
    private void GenerateReport()
    {
        Log("=== DIAGNOSTIC REPORT ===");
        Log($"Found {foundCollectibles.Count} collectibles");
        Log($"Found {brokenCollectibles.Count} broken collectible objects");
        Log($"Repaired {repairedCount} issues");
        
        if (brokenCollectibles.Count > 0)
        {
            LogWarning($"Broken collectibles: {string.Join(", ", brokenCollectibles.Select(go => go.name))}");
        }
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager)
        {
            Log($"LevelManager: {levelManager.CollectiblesRemaining}/{levelManager.TotalCollectibles} remaining");
        }
        
        Log("=== END REPORT ===");
    }
    
    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[CollectibleDiagnostic] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[CollectibleDiagnostic] {message}");
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (foundCollectibles == null || foundCollectibles.Count == 0) return;
        
        // Draw collectible positions
        Gizmos.color = Color.yellow;
        foreach (CollectibleController collectible in foundCollectibles)
        {
            if (collectible != null)
            {
                if (collectible.IsCollected)
                {
                    Gizmos.color = Color.gray;
                }
                else
                {
                    Gizmos.color = Color.yellow;
                }
                
                Gizmos.DrawWireSphere(collectible.transform.position, 0.5f);
                
                // Draw line to player if close
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player && Vector3.Distance(player.transform.position, collectible.transform.position) < 5f)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(player.transform.position, collectible.transform.position);
                }
            }
        }
        
        // Draw broken collectibles
        Gizmos.color = Color.red;
        foreach (GameObject broken in brokenCollectibles)
        {
            if (broken != null)
            {
                Gizmos.DrawWireCube(broken.transform.position, Vector3.one);
            }
        }
    }
    
    #endregion
}
