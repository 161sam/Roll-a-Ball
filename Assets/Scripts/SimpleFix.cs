using UnityEngine;
using System.Collections;

/// <summary>
/// Simple and focused fix for the Unity console warnings
/// Addresses the core issues without complex dependencies
/// </summary>
public class SimpleFix : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(RunSimpleFix());
    }

    private IEnumerator RunSimpleFix()
    {
        yield return new WaitForEndOfFrame();
        
        Debug.Log("=== SIMPLE CONSOLE FIX STARTED ===");
        
        // Fix 1: Create Player if missing
        CreatePlayerIfMissing();
        yield return new WaitForEndOfFrame();
        
        // Fix 2: Fix CollectiblePrefab clones
        FixCollectibleClones();
        yield return new WaitForEndOfFrame();
        
        // Fix 3: Setup basic camera follow
        SetupCameraFollow();
        yield return new WaitForEndOfFrame();
        
        Debug.Log("=== SIMPLE CONSOLE FIX COMPLETED ===");
        
        // Self-destruct after 2 seconds
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
    
    private void CreatePlayerIfMissing()
    {
        PlayerController existingPlayer = FindFirstObjectByType<PlayerController>();
        if (existingPlayer != null)
        {
            Debug.Log("[SimpleFix] Player already exists");
            return;
        }
        
        // Create simple player
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1, 0);
        
        // Add physics
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        
        // Add controller
        player.AddComponent<PlayerController>();
        
        Debug.Log("[SimpleFix] ✅ Created Player");
    }
    
    private void FixCollectibleClones()
    {
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        int fixedCount = 0;
        
        foreach (GameObject obj in collectibles)
        {
            if (obj.GetComponent<CollectibleController>() == null)
            {
                obj.AddComponent<CollectibleController>();
                
                // Ensure trigger collider
                Collider col = obj.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
                
                fixedCount++;
            }
        }
        
        Debug.Log($"[SimpleFix] ✅ Fixed {fixedCount} collectibles");
    }
    
    private void SetupCameraFollow()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;
        
        if (mainCam.GetComponent<CameraController>() == null)
        {
            mainCam.gameObject.AddComponent<CameraController>();
            Debug.Log("[SimpleFix] ✅ Added CameraController");
        }
    }
}
