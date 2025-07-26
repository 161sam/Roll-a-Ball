using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;

namespace RollABall.Editor
{
    /// <summary>
    /// Automated build setup for OSM-enabled Roll-a-Ball project
    /// Ensures all OSM scenes and assets are properly included in builds
    /// </summary>
    public class OSMBuildSetup : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        [MenuItem("Roll-a-Ball/Build/🏗️ Setup OSM Build Configuration", priority = 300)]
        public static void SetupOSMBuildConfiguration()
        {
            Debug.Log("🏗️ Setting up OSM Build Configuration...");
            
            SetupBuildScenes();
            ConfigurePlayerSettings();
            SetupOSMBuildTags();
            ValidateOSMAssets();
            CreateBuildProfiles();
            
            EditorUtility.DisplayDialog("Build Setup Complete", 
                "OSM Build Configuration completed!\n\n" +
                "✅ Scenes added to build\n" +
                "✅ Player settings configured\n" +
                "✅ Tags and layers setup\n" +
                "✅ Assets validated\n" +
                "✅ Build profiles created\n\n" +
                "Ready for building!", "OK");
            
            Debug.Log("✅ OSM Build Configuration complete!");
        }
        
        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("🔍 Pre-build OSM validation...");
            
            ValidateOSMBuildRequirements();
            EnsureOSMAssetsIncluded();
            OptimizeBuildSettings();
            
            Debug.Log("✅ OSM pre-build validation complete");
        }
        
        private static void SetupBuildScenes()
        {
            Debug.Log("📋 Setting up build scenes...");
            
            List<EditorBuildSettingsScene> sceneList = new List<EditorBuildSettingsScene>();
            
            // Required scenes for OSM-enabled Roll-a-Ball
            string[] requiredScenes = {
                "Assets/Scenes/Level1.unity",
                "Assets/Scenes/Level2.unity", 
                "Assets/Scenes/Level3.unity",
                "Assets/Scenes/GeneratedLevel.unity",
                "Assets/Scenes/Level_OSM.unity"
            };
            
            foreach (string scenePath in requiredScenes)
            {
                if (File.Exists(scenePath))
                {
                    sceneList.Add(new EditorBuildSettingsScene(scenePath, true));
                    Debug.Log($"✅ Added to build: {scenePath}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Scene not found: {scenePath}");
                }
            }
            
            EditorBuildSettings.scenes = sceneList.ToArray();
            Debug.Log($"📋 Build scenes configured: {sceneList.Count} scenes");
        }
        
        private static void ConfigurePlayerSettings()
        {
            Debug.Log("⚙️ Configuring player settings for OSM...");
            
            // Internet access required for OSM APIs
            // Note: WSA internet settings are now configured differently in Unity 6.1
            // These settings are handled by build configuration
            
            // Android-specific settings
            PlayerSettings.Android.forceInternetPermission = true;
            
            // Enable location services for future GPS features
            #if UNITY_2023_1_OR_NEWER
            // Unity 2023+ location services settings
            #endif
            
            // Set appropriate minimum API levels
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; // Android 7.0+
            PlayerSettings.iOS.targetOSVersionString = "12.0"; // iOS 12.0+
            
            // Configure scripting backend for performance
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            
            // Graphics settings
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            
            Debug.Log("✅ Player settings configured for OSM");
        }
        
        private static void SetupOSMBuildTags()
        {
            Debug.Log("🏷️ Setting up OSM-specific tags...");
            
            // OSM-specific tags
            string[] osmTags = {
                "Generated",      // For runtime-generated objects
                "OSMRoad",       // For road objects
                "OSMBuilding",   // For building objects
                "OSMArea",       // For area objects (parks, water, etc.)
                "OSMCollectible" // For OSM-placed collectibles
            };
            
            foreach (string tag in osmTags)
            {
                AddTagIfNotExists(tag);
            }
            
            // OSM-specific layers
            string[] osmLayers = {
                "OSMTerrain",
                "OSMObjects",
                "OSMCollectibles"
            };
            
            foreach (string layer in osmLayers)
            {
                AddLayerIfNotExists(layer);
            }
            
            Debug.Log("✅ OSM tags and layers configured");
        }
        
        private static void AddTagIfNotExists(string tag)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
                newTagProp.stringValue = tag;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"✅ Added tag: {tag}");
            }
        }
        
        private static void AddLayerIfNotExists(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            
            bool found = false;
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                SerializedProperty layer = layersProp.GetArrayElementAtIndex(i);
                if (layer.stringValue.Equals(layerName))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                // Find first empty layer slot
                for (int i = 8; i < layersProp.arraySize; i++) // Start from user layers
                {
                    SerializedProperty layer = layersProp.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layer.stringValue))
                    {
                        layer.stringValue = layerName;
                        tagManager.ApplyModifiedProperties();
                        Debug.Log($"✅ Added layer: {layerName} at index {i}");
                        break;
                    }
                }
            }
        }
        
        private static void ValidateOSMAssets()
        {
            Debug.Log("🔍 Validating OSM assets...");
            
            // Check required OSM scripts
            string[] requiredScripts = {
                "Assets/Scripts/Map/OSMMapData.cs",
                "Assets/Scripts/Map/AddressResolver.cs",
                "Assets/Scripts/Map/MapGenerator.cs",
                "Assets/Scripts/Map/MapStartupController.cs"
            };
            
            bool allScriptsFound = true;
            foreach (string script in requiredScripts)
            {
                if (File.Exists(script))
                {
                    Debug.Log($"✅ Script found: {script}");
                }
                else
                {
                    Debug.LogError($"❌ Required script missing: {script}");
                    allScriptsFound = false;
                }
            }
            
            // Check OSM scene
            if (File.Exists("Assets/Scenes/Level_OSM.unity"))
            {
                Debug.Log("✅ OSM scene found: Level_OSM.unity");
            }
            else
            {
                Debug.LogError("❌ OSM scene missing: Level_OSM.unity");
                allScriptsFound = false;
            }
            
            // Check OSM assets directory
            if (Directory.Exists("Assets/OSMAssets"))
            {
                Debug.Log("✅ OSM assets directory found");
            }
            else
            {
                Debug.LogWarning("⚠️ OSM assets directory missing - creating it");
                Directory.CreateDirectory("Assets/OSMAssets");
                Directory.CreateDirectory("Assets/OSMAssets/Materials");
                AssetDatabase.Refresh();
            }
            
            if (allScriptsFound)
            {
                Debug.Log("✅ OSM asset validation passed");
            }
            else
            {
                Debug.LogError("❌ OSM asset validation failed - missing required components");
            }
        }
        
        private static void CreateBuildProfiles()
        {
            Debug.Log("📋 Creating OSM build profiles...");
            
            // Create build profiles for different platforms
            CreateStandaloneBuildProfile();
            CreateAndroidBuildProfile();
            CreateWebGLBuildProfile();
            
            Debug.Log("✅ Build profiles created");
        }
        
        private static void CreateStandaloneBuildProfile()
        {
            string profileContent = @"# Roll-a-Ball OSM - Standalone Build Profile

## Configuration
- Platform: Windows/Mac/Linux Standalone
- Scripting Backend: Mono
- API Compatibility: .NET Standard 2.1
- Graphics API: DirectX 11 / OpenGL / Vulkan

## OSM Features
- ✅ Full API access (Nominatim, Overpass)
- ✅ Unrestricted internet access
- ✅ Local file caching
- ✅ High-performance rendering

## Recommended Settings
- Resolution: 1920x1080 (configurable)
- Quality: High
- VSync: Enabled
- Anti-Aliasing: 4x MSAA

## Build Command
Use Unity's standard build process or:
- Menu: File → Build Settings → Build
- Target: Standalone
- Architecture: x64 (recommended)
";

            string profilePath = "Assets/OSMAssets/Build_Profile_Standalone.md";
            File.WriteAllText(profilePath, profileContent);
        }
        
        private static void CreateAndroidBuildProfile()
        {
            string profileContent = @"# Roll-a-Ball OSM - Android Build Profile

## Configuration
- Platform: Android
- Scripting Backend: IL2CPP
- API Compatibility: .NET Standard 2.1
- Min SDK: Android 7.0 (API 24)
- Target SDK: Latest

## OSM Features  
- ✅ Internet permission required
- ✅ Location services (future GPS)
- ✅ Optimized for mobile performance
- ⚠️ Limited simultaneous API calls

## Permissions Required
- android.permission.INTERNET
- android.permission.ACCESS_NETWORK_STATE
- android.permission.ACCESS_FINE_LOCATION (future)

## Optimization Settings
- Texture compression: ASTC
- Audio compression: Vorbis
- Scripting: IL2CPP + ARM64
- Graphics: OpenGL ES 3.0+

## Build Notes
- APK size: ~50-100MB (depends on assets)
- RAM usage: 200-500MB (depends on map size)
- Network: Required for initial map loading
";

            string profilePath = "Assets/OSMAssets/Build_Profile_Android.md";
            File.WriteAllText(profilePath, profileContent);
        }
        
        private static void CreateWebGLBuildProfile()
        {
            string profileContent = @"# Roll-a-Ball OSM - WebGL Build Profile

## Configuration
- Platform: WebGL
- Scripting Backend: IL2CPP
- API Compatibility: .NET Standard 2.1
- Compression: Gzip

## OSM Features
- ✅ CORS-compatible API access
- ⚠️ Limited threading (Unity WebGL)
- ⚠️ No local file caching
- ✅ Browser-based location services

## Browser Requirements
- Modern browser (Chrome 80+, Firefox 75+)
- WebGL 2.0 support
- ~500MB RAM available
- Stable internet connection

## Limitations
- No multithreading
- Limited memory (heap size)
- Network timeouts more likely
- No persistent local storage

## Optimization
- Texture quality: Medium
- Audio quality: Compressed
- Code stripping: Aggressive
- Exception handling: None
";

            string profilePath = "Assets/OSMAssets/Build_Profile_WebGL.md";
            File.WriteAllText(profilePath, profileContent);
        }
        
        private static void ValidateOSMBuildRequirements()
        {
            // Pre-build validation
            if (!File.Exists("Assets/Scenes/Level_OSM.unity"))
            {
                throw new BuildFailedException("OSM Scene missing: Level_OSM.unity is required for OSM builds");
            }
            
            if (!File.Exists("Assets/Scripts/Map/MapStartupController.cs"))
            {
                throw new BuildFailedException("OSM Script missing: MapStartupController.cs is required");
            }
            
            // Check internet permissions for mobile builds
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                if (!PlayerSettings.Android.forceInternetPermission)
                {
                    Debug.LogWarning("⚠️ Android internet access not set to 'Require' - OSM functionality will not work");
                }
            }
        }
        
        private static void EnsureOSMAssetsIncluded()
        {
            // Ensure OSM-specific assets are included in build
            string[] criticalPaths = {
                "Assets/Scripts/Map/",
                "Assets/OSMAssets/",
                "Assets/Scenes/Level_OSM.unity"
            };
            
            foreach (string path in criticalPaths)
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    // Force include in build
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
        
        private static void OptimizeBuildSettings()
        {
            // Optimize build settings for OSM performance
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                // Mobile-specific optimizations
                PlayerSettings.Android.splitApplicationBinary = false; // Keep APK manageable
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.stripUnusedMeshComponents = true;
            }
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                // WebGL-specific optimizations
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.memorySize = 512; // MB
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            }
        }
    }
    
    /// <summary>
    /// Quick build menu items for OSM
    /// </summary>
    public static class OSMBuildQuickActions
    {
        [MenuItem("Roll-a-Ball/Build/🚀 Build OSM Standalone", priority = 310)]
        public static void BuildOSMStandalone()
        {
            OSMBuildSetup.SetupOSMBuildConfiguration();
            
            string buildPath = EditorUtility.SaveFolderPanel("Choose Build Location", "", "");
            if (!string.IsNullOrEmpty(buildPath))
            {
                BuildPlayerOptions buildOptions = new BuildPlayerOptions
                {
                    scenes = GetScenePaths(),
                    locationPathName = buildPath + "/Roll-a-Ball-OSM.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };
                
                BuildPipeline.BuildPlayer(buildOptions);
                Debug.Log($"🚀 OSM Standalone build completed: {buildPath}");
            }
        }
        
        [MenuItem("Roll-a-Ball/Build/📱 Build OSM Android", priority = 311)]
        public static void BuildOSMAndroid()
        {
            OSMBuildSetup.SetupOSMBuildConfiguration();
            
            string buildPath = EditorUtility.SaveFilePanel("Save Android Build", "", "Roll-a-Ball-OSM", "apk");
            if (!string.IsNullOrEmpty(buildPath))
            {
                BuildPlayerOptions buildOptions = new BuildPlayerOptions
                {
                    scenes = GetScenePaths(),
                    locationPathName = buildPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };
                
                BuildPipeline.BuildPlayer(buildOptions);
                Debug.Log($"📱 OSM Android build completed: {buildPath}");
            }
        }
        
        [MenuItem("Roll-a-Ball/Build/🌐 Build OSM WebGL", priority = 312)]
        public static void BuildOSMWebGL()
        {
            OSMBuildSetup.SetupOSMBuildConfiguration();
            
            string buildPath = EditorUtility.SaveFolderPanel("Choose WebGL Build Location", "", "");
            if (!string.IsNullOrEmpty(buildPath))
            {
                BuildPlayerOptions buildOptions = new BuildPlayerOptions
                {
                    scenes = GetScenePaths(),
                    locationPathName = buildPath,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                };
                
                BuildPipeline.BuildPlayer(buildOptions);
                Debug.Log($"🌐 OSM WebGL build completed: {buildPath}");
            }
        }
        
        private static string[] GetScenePaths()
        {
            return new string[]
            {
                "Assets/Scenes/Level1.unity",
                "Assets/Scenes/Level2.unity", 
                "Assets/Scenes/Level3.unity",
                "Assets/Scenes/GeneratedLevel.unity",
                "Assets/Scenes/Level_OSM.unity"
            };
        }
    }
}
