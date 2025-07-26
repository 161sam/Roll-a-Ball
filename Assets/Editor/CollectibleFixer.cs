using UnityEngine;
using UnityEditor;

namespace RollABall.Editor
{
    /// <summary>
    /// One-Time Script to fix all Collectibles across all scenes
    /// </summary>
    public static class CollectibleFixer
    {
        [MenuItem("Roll-a-Ball/🔧 Fix All Collectibles (One-Time)", priority = 5)]
        public static void FixAllCollectibles()
        {
            if (!EditorUtility.DisplayDialog("Fix Collectibles", 
                "This will add CollectibleController to all Collectibles missing it across all scenes.\n\nProceed?", 
                "Yes, Fix", "Cancel"))
            {
                return;
            }

            Debug.Log("=== Fixing All Collectibles ===");
            
            // Get all scene paths
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                
                // Skip example scenes
                if (sceneName.Contains("Sample") || sceneName.Contains("MiniGame"))
                    continue;
                
                Debug.Log($"Fixing collectibles in: {sceneName}");
                FixCollectiblesInScene(scenePath);
            }
            
            Debug.Log("=== All Collectables Fixed! ===");
            EditorUtility.DisplayDialog("Success", "All Collectibles have been fixed!\n\nCollectibleController added where missing.", "OK");
        }
        
        private static void FixCollectiblesInScene(string scenePath)
        {
            // Save current scene
            UnityEngine.SceneManagement.Scene currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            
            // Open target scene
            UnityEngine.SceneManagement.Scene targetScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            
            // Find all GameObjects with "Collectible" tag
            GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
            
            int fixedCount = 0;
            
            foreach (GameObject collectible in collectibles)
            {
                // Check if CollectibleController is missing
                if (collectible.GetComponent<CollectibleController>() == null)
                {
                    collectible.AddComponent<CollectibleController>();
                    Debug.Log($"  Added CollectibleController to: {collectible.name}");
                    fixedCount++;
                }
                
                // Also ensure it has a SphereCollider set as Trigger
                SphereCollider collider = collectible.GetComponent<SphereCollider>();
                if (collider != null && !collider.isTrigger)
                {
                    collider.isTrigger = true;
                    Debug.Log($"  Set SphereCollider as Trigger for: {collectible.name}");
                }
            }
            
            // Fix Player tag if needed
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                // Try to find by name
                player = GameObject.Find("Player");
                if (player != null && !player.CompareTag("Player"))
                {
                    player.tag = "Player";
                    Debug.Log($"  Fixed Player tag in {targetScene.name}");
                }
            }
            
            Debug.Log($"  Fixed {fixedCount} collectibles in {targetScene.name}");
            
            // Save scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(targetScene);
        }
    }
}
