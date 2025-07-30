using UnityEngine;

/// <summary>
/// Handles goal zone trigger for OSM-generated levels
/// Completes the level when player enters and all collectibles are collected
/// </summary>
[AddComponentMenu("Roll-a-Ball/OSM Goal Zone Trigger")]
public class OSMGoalZoneTrigger : MonoBehaviour
{
    [Header("Goal Zone Settings")]
    [SerializeField] private bool requireAllCollectibles = true;
    [SerializeField] private bool showCompletionEffect = true;
    [SerializeField] private float effectDuration = 2f;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject completionParticles;
    [SerializeField] private AudioClip completionSound;
    
    // Private fields
    private bool hasTriggered = false;
    private LevelManager levelManager;
    private ParticleSystem particles;
    private AudioSource audioSource;
    
    // Events
    public System.Action OnGoalZoneEntered;
    public System.Action OnLevelCompleted;
    
    void Start()
    {
        SetupGoalZone();
    }
    
    private void SetupGoalZone()
    {
        // Find LevelManager
        levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogWarning("[OSMGoalZone] No LevelManager found in scene! Creating fallback manager.");
            GameObject managerObj = new GameObject("LevelManager");
            levelManager = managerObj.AddComponent<LevelManager>();
        }
        
        // Ensure we have a trigger collider
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
        }
        triggerCollider.isTrigger = true;
        
        // Setup particles if available
        if (completionParticles != null)
        {
            particles = completionParticles.GetComponent<ParticleSystem>();
        }
        
        // Setup audio
        if (completionSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.clip = completionSound;
            audioSource.playOnAwake = false;
        }
        
        Debug.Log("[OSMGoalZone] Goal zone setup complete");
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if player entered
        if (other.CompareTag("Player"))
        {
            HandlePlayerEntry(other.gameObject);
        }
    }
    
    private void HandlePlayerEntry(GameObject player)
    {
        if (hasTriggered)
            return;
            
        Debug.Log("[OSMGoalZone] Player entered goal zone");
        OnGoalZoneEntered?.Invoke();
        
        // Check if level can be completed
        if (CanCompleteLevel())
        {
            CompleteLevel();
        }
        else
        {
            ShowIncompleteMessage();
        }
    }
    
    private bool CanCompleteLevel()
    {
        if (!requireAllCollectibles)
            return true;
            
        if (levelManager == null)
        {
            Debug.LogWarning("[OSMGoalZone] Cannot check completion - no LevelManager");
            return false;
        }
        
        return levelManager.CollectiblesRemaining <= 0;
    }
    
    private void CompleteLevel()
    {
        if (hasTriggered)
            return;
            
        hasTriggered = true;
        
        Debug.Log("[OSMGoalZone] Level completed!");
        OnLevelCompleted?.Invoke();
        
        // Show completion effects
        if (showCompletionEffect)
        {
            ShowCompletionEffects();
        }
        
        // Play completion sound
        if (audioSource != null && completionSound != null)
        {
            audioSource.Play();
        }
        
        // Force complete the level via LevelManager
        if (levelManager != null)
        {
            levelManager.ForceCompleteLevel();
        }
        else
        {
            Debug.LogWarning("[OSMGoalZone] No LevelManager to complete level!");
        }
    }
    
    private void ShowIncompleteMessage()
    {
        if (levelManager == null)
            return;
            
        int remaining = levelManager.CollectiblesRemaining;
        string message = remaining == 1 ? 
            $"Collect {remaining} more item to unlock the goal!" : 
            $"Collect {remaining} more items to unlock the goal!";
            
        // Show message via UI if available
        UIController uiController = FindFirstObjectByType<UIController>();
        if (uiController != null)
        {
            uiController.ShowNotification(message, 3f);
        }
        
        Debug.Log($"[OSMGoalZone] {message}");
    }
    
    private void ShowCompletionEffects()
    {
        // Activate particles
        if (particles != null)
        {
            particles.Play();
        }
        else if (completionParticles != null)
        {
            GameObject effectInstance = Instantiate(completionParticles, transform.position, transform.rotation);
            Destroy(effectInstance, effectDuration);
        }
        
        // Add a simple scale animation
        StartCoroutine(ScaleAnimation());
    }
    
    private System.Collections.IEnumerator ScaleAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float elapsed = 0f;
        float duration = 0.5f;
        
        // Scale up
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Force complete the level regardless of collectibles (debug)
    /// </summary>
    public void ForceComplete()
    {
        Debug.Log("[OSMGoalZone] Force completing level");
        CompleteLevel();
    }
    
    /// <summary>
    /// Reset the goal zone for reuse
    /// </summary>
    public void Reset()
    {
        hasTriggered = false;
        transform.localScale = Vector3.one;
        
        if (particles != null)
        {
            particles.Stop();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw goal zone bounds
        Gizmos.color = hasTriggered ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Draw completion indicator
        if (levelManager != null && levelManager.CollectiblesRemaining <= 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }
}
