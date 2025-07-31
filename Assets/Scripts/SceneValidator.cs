using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Comprehensive scene validation and diagnostic tool
/// Validates all objects and provides detailed reports
/// </summary>
[AddComponentMenu("Tools/SceneValidator")]
public class SceneValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField] private bool runValidationOnStart = false;
    [SerializeField] private bool enableDetailedLogging = true;
    [SerializeField] private bool autoFixIssues = false;
    
    [Header("Validation Categories")]
    [SerializeField] private bool validateCollectibles = true;
    [SerializeField] private bool validatePlayer = true;
    [SerializeField] private bool validateUI = true;
    [SerializeField] private bool validateGameSystems = true;
    [SerializeField] private bool validateMaterials = true;
    [SerializeField] private bool validateAudio = true;

    [Header("UI")]
    [Tooltip("Optional progress label updated during validation.")]
    [SerializeField] private TMPro.TextMeshProUGUI progressText;
    
    [Header("Expected Counts")]
    [SerializeField] private int expectedCollectibleCount = 10;
    
    // Validation results
    private ValidationReport validationReport;
    
    void Start()
    {
        if (runValidationOnStart)
        {
            StartCoroutine(ValidateSceneAsync());
        }
    }
    
    [ContextMenu("Validate Scene")]
    public void ValidateScene()
    {
        StartCoroutine(ValidateSceneAsync());
    }
    
    /// <summary>
    /// Main validation routine
    /// </summary>
    public IEnumerator ValidateSceneAsync()
    {
        LogValidation("🔍 Starting comprehensive scene validation...", true);

        int totalSteps = 0;
        if (validateCollectibles) totalSteps++;
        if (validatePlayer) totalSteps++;
        if (validateUI) totalSteps++;
        if (validateGameSystems) totalSteps++;
        if (validateMaterials) totalSteps++;
        if (validateAudio) totalSteps++;
        int currentStep = 0;

        if (progressText) progressText.text = "Initializing...";
        
        validationReport = new ValidationReport();
        validationReport.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        validationReport.validationTime = System.DateTime.Now;
        
        // Run validation categories
        if (validateCollectibles)
        {
            if (progressText) progressText.text = "Validating collectibles...";
            yield return StartCoroutine(ValidateCollectiblesAsync());
            if (progressText) progressText.text = $"{++currentStep}/{totalSteps} collectibles";
        }
        
        if (validatePlayer)
        {
            if (progressText) progressText.text = "Validating player...";
            yield return StartCoroutine(ValidatePlayerAsync());
            if (progressText) progressText.text = $"{++currentStep}/{totalSteps} player";
        }
        
        if (validateUI)
        {
            if (progressText) progressText.text = "Validating UI...";
            yield return StartCoroutine(ValidateUIAsync());
            if (progressText) progressText.text = $"{++currentStep}/{totalSteps} UI";
        }
        
        if (validateGameSystems)
        {
            if (progressText) progressText.text = "Validating systems...";
            yield return StartCoroutine(ValidateGameSystemsAsync());
            if (progressText) progressText.text = $"{++currentStep}/{totalSteps} systems";
        }
        
        if (validateMaterials)
        {
            if (progressText) progressText.text = "Validating materials...";
            yield return StartCoroutine(ValidateMaterialsAsync());
            if (progressText) progressText.text = $"{++currentStep}/{totalSteps} materials";
        }
        
        if (validateAudio)
        {
            if (progressText) progressText.text = "Validating audio...";
            yield return StartCoroutine(ValidateAudioAsync());
            if (progressText) progressText.text = $"{++currentStep}/{totalSteps} audio";
        }
        
        // Generate final report
        GenerateValidationReport();

        if (progressText) progressText.text = "Validation complete";
        LogValidation("✅ Scene validation completed!", true);
    }
    
    #region Collectible Validation
    
    private IEnumerator ValidateCollectiblesAsync()
    {
        LogValidation("🎯 Validating collectibles...");
        
        var collectibleSection = new ValidationSection("Collectibles");
        
        // Find all collectible objects
        CollectibleController[] collectibles = FindObjectsByType<CollectibleController>(FindObjectsSortMode.None);
        GameObject[] potentialCollectibles = GameObject.FindGameObjectsWithTag("Collectible");
        
        collectibleSection.expectedCount = expectedCollectibleCount;
        collectibleSection.actualCount = collectibles.Length;
        
        // Validate each collectible
        foreach (CollectibleController collectible in collectibles)
        {
            var issues = ValidateSingleCollectible(collectible);
            collectibleSection.issues.AddRange(issues);
            
            if (autoFixIssues && issues.Count > 0)
            {
                FixCollectibleIssues(collectible, issues);
            }
        }
        
        // Check for collectibles without CollectibleController
        foreach (GameObject obj in potentialCollectibles)
        {
            if (!obj.GetComponent<CollectibleController>())
            {
                collectibleSection.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    category = "Missing Component",
                    objectName = obj.name,
                    description = "Object tagged as Collectible but missing CollectibleController script"
                });
            }
        }
        
        collectibleSection.isValid = collectibleSection.issues.Count(i => i.severity == IssueSeverity.Error) == 0;
        validationReport.sections.Add(collectibleSection);
        
        LogValidation($"✅ Collectibles: {collectibles.Length} found, {collectibleSection.issues.Count} issues");
        
        yield return null;
    }
    
    private List<ValidationIssue> ValidateSingleCollectible(CollectibleController collectible)
    {
        List<ValidationIssue> issues = new List<ValidationIssue>();
        GameObject obj = collectible.gameObject;
        
        // Check collider
        Collider collider = obj.GetComponent<Collider>();
        if (!collider)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                category = "Missing Collider",
                objectName = obj.name,
                description = "Collectible missing Collider component"
            });
        }
        else if (!collider.isTrigger)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                category = "Incorrect Collider",
                objectName = obj.name,
                description = "Collectible Collider should be set as Trigger"
            });
        }
        
        // Check tag
        if (!obj.CompareTag("Collectible"))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                category = "Incorrect Tag",
                objectName = obj.name,
                description = $"Collectible has tag '{obj.tag}' instead of 'Collectible'"
            });
        }
        
        // Check renderer
        Renderer renderer = obj.GetComponent<Renderer>();
        if (!renderer)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                category = "Missing Renderer",
                objectName = obj.name,
                description = "Collectible missing Renderer component"
            });
        }
        
        // Check material
        if (renderer && renderer.material && renderer.material.name.Contains("Default"))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Info,
                category = "Default Material",
                objectName = obj.name,
                description = "Collectible using default material"
            });
        }
        
        return issues;
    }
    
    private void FixCollectibleIssues(CollectibleController collectible, List<ValidationIssue> issues)
    {
        GameObject obj = collectible.gameObject;
        
        foreach (var issue in issues)
        {
            switch (issue.category)
            {
                case "Missing Collider":
                    SphereCollider sphere = obj.AddComponent<SphereCollider>();
                    sphere.isTrigger = true;
                    sphere.radius = 0.5f;
                    LogValidation($"🔧 Fixed: Added SphereCollider to {obj.name}");
                    break;
                    
                case "Incorrect Collider":
                    Collider collider = obj.GetComponent<Collider>();
                    if (collider) collider.isTrigger = true;
                    LogValidation($"🔧 Fixed: Set Collider as Trigger on {obj.name}");
                    break;
                    
                case "Incorrect Tag":
                    obj.tag = "Collectible";
                    LogValidation($"🔧 Fixed: Set Collectible tag on {obj.name}");
                    break;
            }
        }
    }
    
    #endregion
    
    #region Player Validation
    
    private IEnumerator ValidatePlayerAsync()
    {
        LogValidation("🎮 Validating player...");
        
        var playerSection = new ValidationSection("Player");
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        
        if (!player)
        {
            playerSection.issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                category = "Missing Player",
                objectName = "Scene",
                description = "No GameObject with 'Player' tag found"
            });
        }
        else
        {
            // Validate player components
            if (!playerController)
            {
                playerSection.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    category = "Missing Script",
                    objectName = player.name,
                    description = "Player missing PlayerController script"
                });
            }
            
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (!rb)
            {
                playerSection.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    category = "Missing Rigidbody",
                    objectName = player.name,
                    description = "Player missing Rigidbody component"
                });
            }
            
            Collider playerCollider = player.GetComponent<Collider>();
            if (!playerCollider)
            {
                playerSection.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    category = "Missing Collider",
                    objectName = player.name,
                    description = "Player missing Collider component"
                });
            }
        }
        
        playerSection.isValid = playerSection.issues.Count(i => i.severity == IssueSeverity.Error) == 0;
        validationReport.sections.Add(playerSection);
        
        yield return null;
    }
    
    #endregion
    
    #region UI Validation
    
    private IEnumerator ValidateUIAsync()
    {
        LogValidation("🖥️ Validating UI...");
        
        var uiSection = new ValidationSection("UI");
        
        // Find UI Controller
        UIController uiController = FindFirstObjectByType<UIController>();
        if (!uiController)
        {
            uiSection.issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                category = "Missing UI",
                objectName = "Scene",
                description = "No UIController found in scene"
            });
        }
        
        // Check for Canvas
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases.Length == 0)
        {
            uiSection.issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                category = "Missing Canvas",
                objectName = "Scene",
                description = "No Canvas found in scene"
            });
        }
        
        uiSection.isValid = uiSection.issues.Count(i => i.severity == IssueSeverity.Error) == 0;
        validationReport.sections.Add(uiSection);
        
        yield return null;
    }
    
    #endregion
    
    #region Game Systems Validation
    
    private IEnumerator ValidateGameSystemsAsync()
    {
        LogValidation("⚙️ Validating game systems...");
        
        var systemsSection = new ValidationSection("Game Systems");
        
        // Check GameManager
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (!gameManager)
        {
            systemsSection.issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                category = "Missing Manager",
                objectName = "Scene",
                description = "No GameManager found in scene"
            });
        }
        
        // Check LevelManager
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (!levelManager)
        {
            systemsSection.issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                category = "Missing Manager",
                objectName = "Scene",
                description = "No LevelManager found in scene"
            });
        }
        
        // Check LevelGenerator
        LevelGenerator levelGenerator = FindFirstObjectByType<LevelGenerator>();
        if (!levelGenerator)
        {
            systemsSection.issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Info,
                category = "Missing Generator",
                objectName = "Scene",
                description = "No LevelGenerator found in scene (may be intentional for static levels)"
            });
        }
        
        systemsSection.isValid = systemsSection.issues.Count(i => i.severity == IssueSeverity.Error) == 0;
        validationReport.sections.Add(systemsSection);
        
        yield return null;
    }
    
    #endregion
    
    #region Material Validation
    
    private IEnumerator ValidateMaterialsAsync()
    {
        LogValidation("🎨 Validating materials...");
        
        var materialsSection = new ValidationSection("Materials");
        
        // Find all renderers in scene
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Dictionary<Material, int> materialUsage = new Dictionary<Material, int>();
        int defaultMaterialCount = 0;
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material)
            {
                Material mat = renderer.material;
                if (materialUsage.ContainsKey(mat))
                    materialUsage[mat]++;
                else
                    materialUsage[mat] = 1;
                
                // Check for default materials
                if (mat.name.Contains("Default") || mat.name.Contains("default"))
                {
                    defaultMaterialCount++;
                    materialsSection.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Info,
                        category = "Default Material",
                        objectName = renderer.gameObject.name,
                        description = $"Using default material: {mat.name}"
                    });
                }
            }
            else
            {
                materialsSection.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    category = "Missing Material",
                    objectName = renderer.gameObject.name,
                    description = "Renderer has no material assigned"
                });
            }
        }
        
        LogValidation($"📊 Material usage: {materialUsage.Count} unique materials, {defaultMaterialCount} default materials");
        
        materialsSection.isValid = materialsSection.issues.Count(i => i.severity == IssueSeverity.Error) == 0;
        validationReport.sections.Add(materialsSection);
        
        yield return null;
    }
    
    #endregion
    
    #region Audio Validation
    
    private IEnumerator ValidateAudioAsync()
    {
        LogValidation("🔊 Validating audio...");
        
        var audioSection = new ValidationSection("Audio");
        
        // Check for AudioManager
        AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        if (!audioManager)
        {
            audioSection.issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Info,
                category = "Missing Audio Manager",
                objectName = "Scene",
                description = "No AudioManager found in scene"
            });
        }
        
        // Check AudioSources
        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in audioSources)
        {
            if (!source.clip)
            {
                audioSection.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Info,
                    category = "Missing Audio Clip",
                    objectName = source.gameObject.name,
                    description = "AudioSource has no AudioClip assigned"
                });
            }
        }
        
        audioSection.isValid = audioSection.issues.Count(i => i.severity == IssueSeverity.Error) == 0;
        validationReport.sections.Add(audioSection);
        
        yield return null;
    }
    
    #endregion
    
    #region Report Generation
    
    private void GenerateValidationReport()
    {
        // Calculate overall statistics
        validationReport.totalIssues = validationReport.sections.Sum(s => s.issues.Count);
        validationReport.errorCount = validationReport.sections.Sum(s => s.issues.Count(i => i.severity == IssueSeverity.Error));
        validationReport.warningCount = validationReport.sections.Sum(s => s.issues.Count(i => i.severity == IssueSeverity.Warning));
        validationReport.infoCount = validationReport.sections.Sum(s => s.issues.Count(i => i.severity == IssueSeverity.Info));
        validationReport.isSceneValid = validationReport.errorCount == 0;
        
        // Log summary
        LogValidation($"📊 Validation Summary:", true);
        LogValidation($"   Errors: {validationReport.errorCount}", true);
        LogValidation($"   Warnings: {validationReport.warningCount}", true);
        LogValidation($"   Info: {validationReport.infoCount}", true);
        LogValidation($"   Scene Valid: {(validationReport.isSceneValid ? "✅ Yes" : "❌ No")}", true);
        
        // Save detailed report
        SaveValidationReport();
    }
    
    private void SaveValidationReport()
    {
        string reportPath = "/home/saschi/Games/Roll-a-Ball/Reports/ValidationReport_" + 
                           validationReport.sceneName + "_" + 
                           System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".md";
        
        string report = GenerateMarkdownReport();
        
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath));
            System.IO.File.WriteAllText(reportPath, report);
            LogValidation($"📄 Validation report saved to: {reportPath}");
        }
        catch (System.Exception e)
        {
            LogValidation($"❌ Failed to save validation report: {e.Message}");
        }
    }
    
    private string GenerateMarkdownReport()
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("# Scene Validation Report");
        report.AppendLine($"**Scene:** {validationReport.sceneName}");
        report.AppendLine($"**Validation Time:** {validationReport.validationTime:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Overall Status:** {(validationReport.isSceneValid ? "✅ VALID" : "❌ INVALID")}");
        report.AppendLine();
        
        // Summary
        report.AppendLine("## Summary");
        report.AppendLine($"- **Total Issues:** {validationReport.totalIssues}");
        report.AppendLine($"- **Errors:** {validationReport.errorCount}");
        report.AppendLine($"- **Warnings:** {validationReport.warningCount}");
        report.AppendLine($"- **Info:** {validationReport.infoCount}");
        report.AppendLine();
        
        // Sections
        foreach (var section in validationReport.sections)
        {
            report.AppendLine($"## {section.name}");
            report.AppendLine($"**Status:** {(section.isValid ? "✅ Valid" : "❌ Issues Found")}");
            
            if (section.expectedCount > 0)
            {
                report.AppendLine($"**Expected Count:** {section.expectedCount}");
                report.AppendLine($"**Actual Count:** {section.actualCount}");
            }
            
            if (section.issues.Count > 0)
            {
                report.AppendLine("**Issues:**");
                foreach (var issue in section.issues)
                {
                    string icon = issue.severity == IssueSeverity.Error ? "❌" :
                                 issue.severity == IssueSeverity.Warning ? "⚠️" : "ℹ️";
                    report.AppendLine($"- {icon} **{issue.category}** ({issue.objectName}): {issue.description}");
                }
            }
            else
            {
                report.AppendLine("✅ No issues found");
            }
            
            report.AppendLine();
        }
        
        return report.ToString();
    }
    
    #endregion
    
    #region Utility Methods
    
    private void LogValidation(string message, bool important = false)
    {
        if (!enableDetailedLogging && !important) return;
        Debug.Log($"[SceneValidator] {message}");
    }
    
    #endregion
}

#region Data Structures

[System.Serializable]
public class ValidationReport
{
    public string sceneName;
    public System.DateTime validationTime;
    public List<ValidationSection> sections = new List<ValidationSection>();
    public int totalIssues;
    public int errorCount;
    public int warningCount;
    public int infoCount;
    public bool isSceneValid;
}

[System.Serializable]
public class ValidationSection
{
    public string name;
    public bool isValid;
    public int expectedCount;
    public int actualCount;
    public List<ValidationIssue> issues = new List<ValidationIssue>();
    
    public ValidationSection(string sectionName)
    {
        name = sectionName;
        isValid = true;
    }
}

[System.Serializable]
public class ValidationIssue
{
    public IssueSeverity severity;
    public string category;
    public string objectName;
    public string description;
}

public enum IssueSeverity
{
    Info,
    Warning,
    Error
}

#endregion
