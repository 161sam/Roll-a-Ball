using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

namespace RollABall.Editor
{
    /// <summary>
    /// Repariert die korrupte Level_OSM.unity Szene
    /// </summary>
    public class FixOSMScene : EditorWindow
    {
        [MenuItem("Roll-a-Ball/🛠️ Fix OSM Scene", priority = 200)]
        public static void ShowWindow()
        {
            FixOSMScene window = GetWindow<FixOSMScene>("Fix OSM Scene");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        void OnGUI()
        {
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("🛠️ OSM Scene Repair Tool", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Fixes corrupted Level_OSM.unity scene", EditorStyles.miniLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox("The Level_OSM.unity scene has broken references.\\nThis tool will recreate the scene with proper components.", MessageType.Warning);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔧 Recreate OSM Scene", GUILayout.Height(40)))
            {
                RecreateOSMScene();
            }
            
            if (GUILayout.Button("🗑️ Delete Corrupted Scene", GUILayout.Height(30)))
            {
                DeleteCorruptedScene();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("📋 What this will do:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Delete the corrupted Level_OSM.unity");
            EditorGUILayout.LabelField("• Create a new Level_OSM.unity scene");
            EditorGUILayout.LabelField("• Add all required OSM components");
            EditorGUILayout.LabelField("• Setup UI Canvas and elements");
            EditorGUILayout.LabelField("• Configure proper references");
        }
        
        private static void RecreateOSMScene()
        {
            if (EditorUtility.DisplayDialog("Recreate OSM Scene", 
                "This will delete the corrupted Level_OSM.unity and create a new one.\\n\\nAre you sure?", 
                "Yes, Recreate", "Cancel"))
            {
                Debug.Log("🛠️ Starting OSM Scene recreation...");
                
                // Delete corrupted scene
                string scenePath = "Assets/Scenes/Level_OSM.unity";
                if (File.Exists(scenePath))
                {
                    AssetDatabase.DeleteAsset(scenePath);
                    Debug.Log("🗑️ Deleted corrupted Level_OSM.unity");
                }
                
                // Create new scene
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                
                // Add essential objects
                CreateMainCamera();
                CreateDirectionalLight();
                CreateUICanvas();
                CreateOSMComponents();
                CreateGroundPlane();
                
                // Save scene
                EditorSceneManager.SaveScene(newScene, scenePath);
                Debug.Log("✅ New Level_OSM.unity created successfully!");
                
                EditorUtility.DisplayDialog("Success", "OSM Scene recreated successfully!\\nAll components have been added.", "OK");
            }
        }
        
        private static void DeleteCorruptedScene()
        {
            if (EditorUtility.DisplayDialog("Delete Corrupted Scene", 
                "This will permanently delete the corrupted Level_OSM.unity.\\n\\nAre you sure?", 
                "Yes, Delete", "Cancel"))
            {
                string scenePath = "Assets/Scenes/Level_OSM.unity";
                if (File.Exists(scenePath))
                {
                    AssetDatabase.DeleteAsset(scenePath);
                    Debug.Log("🗑️ Corrupted Level_OSM.unity deleted");
                    EditorUtility.DisplayDialog("Deleted", "Corrupted scene has been deleted.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", "Level_OSM.unity not found.", "OK");
                }
            }
        }
        
        private static void CreateMainCamera()
        {
            GameObject cameraGO = new GameObject("Main Camera");
            Camera camera = cameraGO.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();
            
            // Add CameraController if it exists
            var cameraController = cameraGO.AddComponent<CameraController>();
            
            // Position camera
            cameraGO.transform.position = new Vector3(0, 10, -10);
            cameraGO.transform.rotation = Quaternion.Euler(30, 0, 0);
            
            Debug.Log("📷 Main Camera created");
        }
        
        private static void CreateDirectionalLight()
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            
            // Position light
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            Debug.Log("💡 Directional Light created");
        }
        
        private static void CreateUICanvas()
        {
            GameObject canvasGO = new GameObject("UI_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create AddressInputPanel
            GameObject addressPanel = new GameObject("AddressInputPanel");
            addressPanel.transform.SetParent(canvasGO.transform, false);
            addressPanel.AddComponent<UnityEngine.UI.Image>();
            
            // Create LoadingPanel
            GameObject loadingPanel = new GameObject("LoadingPanel");
            loadingPanel.transform.SetParent(canvasGO.transform, false);
            loadingPanel.AddComponent<UnityEngine.UI.Image>();
            loadingPanel.SetActive(false);
            
            // Create GameUIPanel
            GameObject gameUIPanel = new GameObject("GameUIPanel");
            gameUIPanel.transform.SetParent(canvasGO.transform, false);
            gameUIPanel.SetActive(false);
            
            Debug.Log("🖥️ UI Canvas created");
        }
        
        private static void CreateOSMComponents()
        {
            // Create OSM Controller
            GameObject osmControllerGO = new GameObject("OSM_Controller");
            
            // Add MapStartupController
            var mapStartupController = osmControllerGO.AddComponent<RollABall.Map.MapStartupController>();
            
            // Create AddressResolver
            GameObject addressResolverGO = new GameObject("AddressResolver");
            var addressResolver = addressResolverGO.AddComponent<RollABall.Map.AddressResolver>();
            
            // Create MapGenerator
            GameObject mapGeneratorGO = new GameObject("MapGenerator");
            var mapGenerator = mapGeneratorGO.AddComponent<RollABall.Map.MapGenerator>();
            
            // Create GameManager
            GameObject gameManagerGO = new GameObject("GameManager");
            var gameManager = gameManagerGO.AddComponent<GameManager>();
            
            // Create UIController
            GameObject uiControllerGO = new GameObject("UIController");
            var uiController = uiControllerGO.AddComponent<UIController>();
            
            Debug.Log("🗺️ OSM Components created");
        }
        
        private static void CreateGroundPlane()
        {
            GameObject groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGO.name = "Ground";
            groundGO.transform.localScale = new Vector3(10, 1, 10);
            
            // Create material
            Material groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.color = new Color(0.3f, 0.5f, 0.3f);
            
            groundGO.GetComponent<MeshRenderer>().material = groundMaterial;
            
            Debug.Log("🌍 Ground Plane created");
        }
    }
}
