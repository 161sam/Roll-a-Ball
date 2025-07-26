using UnityEngine;
using UnityEditor;

/// <summary>
/// Quick runtime script to fix all Collectibles in current scene
/// </summary>
public class QuickCollectibleFix : MonoBehaviour
{
    [ContextMenu("Fix All Collectibles in Scene")]
    public void FixAllCollectiblesInScene()
    {
        Debug.Log("=== Quick Collectible Fix ===");
        
        // Find all GameObjects with "Collectible" tag
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        
        int fixedCount = 0;
        
        foreach (GameObject collectible in collectibles)
        {
            // Check if CollectibleController is missing
            if (collectible.GetComponent<CollectibleController>() == null)
            {
                collectible.AddComponent<CollectibleController>();
                Debug.Log($"Added CollectibleController to: {collectible.name}");
                fixedCount++;
            }
            
            // Ensure SphereCollider is set as Trigger
            SphereCollider collider = collectible.GetComponent<SphereCollider>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
                Debug.Log($"Set SphereCollider as Trigger for: {collectible.name}");
            }
        }
        
        // Fix Player tag if needed
        GameObject player = GameObject.Find("Player");
        if (player != null && !player.CompareTag("Player"))
        {
            player.tag = "Player";
            Debug.Log("Fixed Player tag");
        }
        
        Debug.Log($"=== Fixed {fixedCount} collectibles and player tag ===");
        
        // Auto-destroy this component
        DestroyImmediate(this);
    }
    
    void Start()
    {
        // Auto-run the fix
        FixAllCollectiblesInScene();
    }
}
