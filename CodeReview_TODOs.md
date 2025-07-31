# Code Review TODOs

The following TODO comments were added during code review to highlight potential improvements.

| File | Line | Comment | Status |
|------|------|---------|---------|
| Assets/Scripts/GameManager.cs | 164 | Delegate input handling to a centralized InputManager | **done** |
| Assets/Scripts/GameManager.cs | 175 | Expose restartKey in settings menu | **done** |
| Assets/Scripts/PlayerController.cs | 434 | Expose slide duration as configurable field | **done** |
| Assets/Scripts/Map/MapStartupController.cs | 121 | Avoid expensive FindFirstObject calls | **done** |
| Assets/Scripts/Map/MapGeneratorBatched.cs | 62 | Support per-building height variation based on OSM tags | **done** |
| Assets/Scripts/Map/MapGeneratorBatched.cs | 469 | Consider performing batching in a job |
| Assets/Scripts/Map/OSMUIConnector.cs | 109 | Cache controller references instead of recreating | **done** |
| Assets/Scripts/Map/OSMUIConnector.cs | 420 | Use prefabs for quick buttons | **done** |
| Assets/Scripts/AudioManager.cs | 420 | Cache PlayerController reference in Awake | **done** |
| Assets/Scripts/VFX/RotatingGear.cs | 27 | Make rotation speed variance configurable | **done** |
| Assets/Scripts/VFX/SteamEmitter.cs | 249 | Expose randomization ranges via inspector | **done** |
| Assets/Scripts/SceneTypeDetector.cs | 18 | Read scene lists from config instead of hardcoding | **done** |
| Assets/Scripts/CollectibleDiagnosticTool.cs | 94 | Cache results to avoid allocations | **done** |
| Assets/Scripts/Map/MapStartupController.cs | 53 | Allow editing fallback coordinates in inspector | **done** |
| Assets/Scripts/Input/InputManager.cs | 21 | Allow runtime key rebinding via settings menu | **done** |
| Assets/Scripts/Map/MapGenerator.cs | 1492 | Expose chimney offset factors via inspector fields | **done** |
| Assets/Scripts/Map/MapGenerator.cs | 1510 | Make gear decoration ranges configurable in LevelProfile | **done** |
| Assets/Scripts/Generators/LevelGenerator.cs | 396 | Replace reflection with an interface for adaptive mode selection | **done** |
| Assets/Scripts/Generators/LevelTerrainGenerator.cs | 162 | Refactor to interface-based lookup instead of reflection | **done** |
| Assets/Scripts/Map/BatchingPerformanceTest.cs | 174 | Cache object list to avoid allocations during testing | **done** |
| Assets/Scripts/EGPUPerformanceOptimizer.cs | 81 | Replace OnGUI debug overlay with a Canvas-based UI | **done** |
| Assets/Scripts/Map/PerformanceMonitor.cs | 69 | Replace OnGUI with a UI Canvas overlay | **done** |
| Assets/Scripts/Map/PerformanceMonitor.cs | 116 | Cache created textures if display colors change frequently | **done** |
| Assets/Scripts/Map/BoundingBoxTester.cs | 65 | Expose test locations via inspector to allow custom cases | **done** |
| Assets/Scripts/Map/BoundingBoxTester.cs | 110 | Move bounding box calculation to a shared utility class | **done** |
| Assets/Scripts/Map/MapGeneratorTester.cs | 63 | Move synthetic test data generation to a ScriptableObject | **done** |
| Assets/Scripts/Environment/GateController.cs | 67 | Provide matching TriggerClose() logic for reversible puzzles | **done** |
| Assets/Scripts/Environment/GroundMaterialController.cs | 19 | Move material paths to a configuration ScriptableObject | **done** |
| Assets/Scripts/Environment/RotatingObstacle.cs | 49 | Integrate damage system to penalize the player on contact | **done** |
| Assets/Scripts/TagManager.cs | 16 | Load required tags from a central config file | **done** |
| Assets/Scripts/SceneValidator.cs | 51 | Display progress UI while validation routines run | **done** |
| Assets/Scripts/OSMGoalZoneTrigger.cs | 163 | Use an event from UIController instead of direct lookup | **done** |
| Assets/Material/CollectibleMaterial | 3 | Verify metallic/smoothness values for PBR consistency | **done** |
| Assets/OSMAssets/Materials/OSM_Park_Material.mat | 3 | Adjust color to match overall scene lighting | **done** |
| Assets/Prefabs/Player.prefab | 3 | Separate player stats into dedicated ScriptableObject | **done** |
| Assets/Scenes/Level1.unity | 3 | Review occlusion and lighting settings for optimized performance |
| Assets/Scenes/Level_OSM.unity | 3 | Ensure map generation uses prefabs from OSMAssets folder |
| Assets/Resources/LevelProfiles/EasyProfile.asset | 25 | Balance collectible spawn height for easier levels | **done** |
| Assets/ScriptableObjects/HardProfile.asset | 34 | Assign default moving platform prefabs for hard difficulty |
| Assets/Scenes/GeneratedLevel.unity | 3 | Replace sample level with procedurally generated layout |
| Assets/Scripts/GameManager.cs | 33 | Move basic game settings to a ScriptableObject for easier tuning | **done** |
| Assets/Scripts/GameManager.cs | 77 | Make respawn delay configurable per level/difficulty | **done** |
| Assets/Scripts/PlayerController.cs | 49 | Integrate Cinemachine for camera following | **done** |
| Assets/Scripts/Environment/MovingPlatform.cs | 21 | Expose bounce parameters in inspector for finer control | **done** |
| Assets/Scripts/Utility/LocalizationManager.cs | 23 | Load localization data from external files to support more languages | **done** |
| Assets/Scripts/Utility/PrefabPooler.cs | 24 | Add maximum pool size to prevent uncontrolled growth | **done** |
| Assets/Scripts/Map/MapGenerator.cs | 16 | Store prefab references in a configuration asset | **done** |
| Assets/Scripts/Map/MapGeneratorBatched.cs | 45 | Move decoration prefabs to a centralized asset | **done** |
| Assets/Scripts/SceneTypeDetector.cs | 10 | Allow overriding scene lists at runtime via config file | **done** |
| Assets/Scripts/Input/InputManager.cs | 25 | Support gamepad bindings alongside keyboard controls | **done** |
| Assets/Scripts/SaveSystem.cs | 667 | Create incremental backup before uploading to cloud | **done** |
| Assets/Scripts/GameManager.cs | 36 | Remove legacy key fields once InputManager fully manages bindings | |
| Assets/Scripts/SaveSystem.cs | 187 | Return Task instead of async void to allow awaiting errors | |
| Assets/Scripts/Map/AddressResolver.cs | 20 | Move API URLs to a configuration asset for easier editing | |
| Assets/Scripts/Environment/MovingPlatform.cs | 21 | Move bounce parameters into MovingPlatformProfile for consistency | |
| Assets/Scripts/Testing/OSMTestController.cs | 244 | Replace magic value with configurable boundary constant | |
| Assets/Scripts/Input/InputManager.cs | 225 | Switch to Input System events to capture keys without polling | |
| Assets/Scripts/AudioManager.cs | 265 | Preload next track for seamless transition in shuffle mode | |
| Assets/Scripts/AudioManager.cs | 217 | Cache player reference instead of using FindFirstObjectByType | |
| Assets/Scripts/AudioManager.cs | 224 | Maintain map of playing sources for efficient StopSound lookup | |
| Assets/Scripts/PlayerController.cs | 485 | Replace Debug.Log spam with debug overlay | |
| Assets/Prefabs/Player.prefab | 2 | Consolidate duplicate player prefabs | |
| Assets/Scenes/Level2.unity | 2 | Ensure all objects are prefab instances | |
| Assets/Scripts/Map/MapGenerator.cs | 1643 | Pool map elements instead of destroying them each regeneration | |
| Assets/Scripts/PlayerController.cs | 154 | Inject camera reference via initializer to avoid scene search | |
| Assets/Scripts/SaveSystem.cs | 386 | Replace with a stronger encryption algorithm for real deployments | |
| Assets/Scripts/AchievementSystem.cs | 305 | Inject PlayerController dependency instead of using FindFirstObjectByType | |
| Assets/Scripts/Map/AddressResolver.cs | 263 | Throttle requests and cache results to avoid API rate limits | |
| Assets/Scenes/Level3.unity | 3 | Verify all environment objects use prefab instances | |
| Assets/Scenes/MiniGame.unity | 3 | Ensure mini-game scene uses shared UI prefab setup | |
| Assets/Prefabs/CollectiblePrefab.prefab | 3 | Add LODGroup component for distant rendering | |
| Assets/OSMAssets/Materials/OSM_Road_Material.mat | 3 | Adjust texture tiling for consistent road scale | |
