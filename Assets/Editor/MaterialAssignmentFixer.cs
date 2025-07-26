using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace RollABall.Editor
{
    /// <summary>
    /// Automatisches Zuweisen von Materialien zu LevelProfiles
    /// Behebt das "No ground materials assigned" Problem
    /// </summary>
    public static class MaterialAssignmentFixer
    {
        /// <summary>
        /// Weist automatisch alle verfügbaren Materialien den LevelProfiles zu
        /// </summary>
        [MenuItem("Roll-a-Ball/🎨 Fix Material Assignments", priority = 15)]
        public static void FixAllMaterialAssignments()
        {
            Debug.Log("🎨 Starting Material Assignment Fix...");
            
            // Lade alle verfügbaren Materialien
            List<Material> groundMaterials = LoadMaterialsContaining("Ground", "Steam");
            List<Material> wallMaterials = LoadMaterialsContaining("Wall", "Steam");  
            Material goalZoneMaterial = LoadMaterial("GoalZone");
            
            // Debug-Ausgabe
            Debug.Log($"Found {groundMaterials.Count} ground materials");
            Debug.Log($"Found {wallMaterials.Count} wall materials");
            Debug.Log($"Goal zone material: {(goalZoneMaterial != null ? goalZoneMaterial.name : "NOT FOUND")}");
            
            // Lade alle LevelProfiles
            string[] profilePaths = AssetDatabase.FindAssets("t:LevelProfile");
            int fixedProfiles = 0;
            
            foreach (string guid in profilePaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelProfile profile = AssetDatabase.LoadAssetAtPath<LevelProfile>(path);
                
                if (profile != null)
                {
                    bool hasChanges = false;
                    
                    // Assign ground materials if missing
                    if (profile.GroundMaterials == null || profile.GroundMaterials.Length == 0)
                    {
                        SetGroundMaterials(profile, groundMaterials.ToArray());
                        hasChanges = true;
                        Debug.Log($"✅ Assigned {groundMaterials.Count} ground materials to {profile.name}");
                    }
                    
                    // Assign wall materials if missing
                    if (profile.WallMaterials == null || profile.WallMaterials.Length == 0)
                    {
                        SetWallMaterials(profile, wallMaterials.ToArray());
                        hasChanges = true;
                        Debug.Log($"✅ Assigned {wallMaterials.Count} wall materials to {profile.name}");
                    }
                    
                    // Assign goal zone material if missing
                    if (profile.GoalZoneMaterial == null && goalZoneMaterial != null)
                    {
                        SetGoalZoneMaterial(profile, goalZoneMaterial);
                        hasChanges = true;
                        Debug.Log($"✅ Assigned goal zone material to {profile.name}");
                    }
                    
                    if (hasChanges)
                    {
                        EditorUtility.SetDirty(profile);
                        fixedProfiles++;
                    }
                }
            }
            
            // Save all changes
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"🎉 Material assignment complete! Fixed {fixedProfiles} profiles.");
            EditorUtility.DisplayDialog("Success", $"Material assignments fixed!\\n\\nFixed {fixedProfiles} LevelProfiles\\nGround Materials: {groundMaterials.Count}\\nWall Materials: {wallMaterials.Count}", "OK");
        }
        
        /// <summary>
        /// Lädt alle Materialien, die bestimmte Schlüsselwörter enthalten
        /// </summary>
        private static List<Material> LoadMaterialsContaining(params string[] keywords)
        {
            List<Material> materials = new List<Material>();
            
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
            
            foreach (string guid in materialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                // Check if filename contains any of the keywords
                bool containsKeyword = false;
                foreach (string keyword in keywords)
                {
                    if (fileName.ToLower().Contains(keyword.ToLower()))
                    {
                        containsKeyword = true;
                        break;
                    }
                }
                
                if (containsKeyword)
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material != null)
                    {
                        materials.Add(material);
                        Debug.Log($"Found material: {material.name}");
                    }
                }
            }
            
            return materials;
        }
        
        /// <summary>
        /// Lädt ein spezifisches Material nach Namen
        /// </summary>
        private static Material LoadMaterial(string nameContains)
        {
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
            
            foreach (string guid in materialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                if (fileName.ToLower().Contains(nameContains.ToLower()))
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material != null)
                    {
                        Debug.Log($"Found specific material: {material.name}");
                        return material;
                    }
                }
            }
            
            Debug.LogWarning($"Material containing '{nameContains}' not found!");
            return null;
        }
        
        /// <summary>
        /// Setzt Ground Materials per Reflection (da die Felder private sind)
        /// </summary>
        private static void SetGroundMaterials(LevelProfile profile, Material[] materials)
        {
            var field = typeof(LevelProfile).GetField("groundMaterials", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(profile, materials);
            }
        }
        
        /// <summary>
        /// Setzt Wall Materials per Reflection
        /// </summary>
        private static void SetWallMaterials(LevelProfile profile, Material[] materials)
        {
            var field = typeof(LevelProfile).GetField("wallMaterials", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(profile, materials);
            }
        }
        
        /// <summary>
        /// Setzt Goal Zone Material per Reflection
        /// </summary>
        private static void SetGoalZoneMaterial(LevelProfile profile, Material material)
        {
            var field = typeof(LevelProfile).GetField("goalZoneMaterial", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(profile, material);
            }
        }
        
        /// <summary>
        /// Erstellt Fallback-Materialien, falls keine gefunden werden
        /// </summary>
        [MenuItem("Roll-a-Ball/🎨 Create Fallback Materials", priority = 16)]
        public static void CreateFallbackMaterials()
        {
            Debug.Log("🎨 Creating fallback materials...");
            
            string materialPath = "Assets/Material";
            if (!Directory.Exists(materialPath))
            {
                Directory.CreateDirectory(materialPath);
            }
            
            // Standard Ground Material
            CreateMaterialIfNotExists(materialPath + "/StandardGroundMaterial.mat", Color.gray);
            
            // Standard Wall Material  
            CreateMaterialIfNotExists(materialPath + "/StandardWallMaterial.mat", Color.white);
            
            // Standard Goal Zone Material
            CreateMaterialIfNotExists(materialPath + "/StandardGoalZoneMaterial.mat", Color.green);
            
            AssetDatabase.Refresh();
            Debug.Log("✅ Fallback materials created!");
        }
        
        /// <summary>
        /// Erstellt ein Material, falls es nicht existiert
        /// </summary>
        private static void CreateMaterialIfNotExists(string path, Color color)
        {
            if (!File.Exists(path))
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = color;
                AssetDatabase.CreateAsset(material, path);
                Debug.Log($"Created material: {Path.GetFileName(path)}");
            }
        }
        
        /// <summary>
        /// Validiert alle LevelProfile-Materialzuweisungen
        /// </summary>
        [MenuItem("Roll-a-Ball/🔍 Validate Material Assignments", priority = 17)]
        public static void ValidateAllMaterialAssignments()
        {
            Debug.Log("🔍 Validating all LevelProfile material assignments...");
            
            string[] profilePaths = AssetDatabase.FindAssets("t:LevelProfile");
            int validProfiles = 0;
            int invalidProfiles = 0;
            
            foreach (string guid in profilePaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelProfile profile = AssetDatabase.LoadAssetAtPath<LevelProfile>(path);
                
                if (profile != null)
                {
                    bool isValid = profile.ValidateProfile();
                    if (isValid)
                    {
                        validProfiles++;
                        Debug.Log($"✅ {profile.name}: Valid");
                    }
                    else
                    {
                        invalidProfiles++;
                        Debug.Log($"❌ {profile.name}: Invalid");
                    }
                }
            }
            
            string message = $"Validation Complete!\\n\\n✅ Valid: {validProfiles}\\n❌ Invalid: {invalidProfiles}";
            if (invalidProfiles > 0)
            {
                message += "\\n\\nRun 'Fix Material Assignments' to resolve issues.";
            }
            
            EditorUtility.DisplayDialog("Validation Results", message, "OK");
        }
    }
}
