# Code Review TODOs

The following TODO comments were added during code review to highlight potential improvements.

| File | Line | Comment |
|------|------|---------|
| Assets/Scripts/GameManager.cs | 164 | Delegate input handling to a centralized InputManager |
| Assets/Scripts/GameManager.cs | 175 | Expose restartKey in settings menu |
| Assets/Scripts/PlayerController.cs | 434 | Expose slide duration as configurable field |
| Assets/Scripts/Map/MapStartupController.cs | 121 | Avoid expensive FindFirstObject calls |
| Assets/Scripts/Map/MapGeneratorBatched.cs | 62 | Support per-building height variation based on OSM tags |
| Assets/Scripts/Map/MapGeneratorBatched.cs | 469 | Consider performing batching in a job |
| Assets/Scripts/Map/OSMUIConnector.cs | 109 | Cache controller references instead of recreating |
| Assets/Scripts/Map/OSMUIConnector.cs | 420 | Use prefabs for quick buttons |
| Assets/Scripts/AudioManager.cs | 420 | Cache PlayerController reference in Awake |
| Assets/Scripts/VFX/RotatingGear.cs | 27 | Make rotation speed variance configurable |
| Assets/Scripts/VFX/SteamEmitter.cs | 249 | Expose randomization ranges via inspector |
| Assets/Scripts/SceneTypeDetector.cs | 18 | Read scene lists from config instead of hardcoding |
| Assets/Scripts/CollectibleDiagnosticTool.cs | 94 | Cache results to avoid allocations |
| Assets/Scripts/Map/MapStartupController.cs | 53 | Allow editing fallback coordinates in inspector |
| Assets/Scripts/Input/InputManager.cs | 21 | Allow runtime key rebinding via settings menu |
| Assets/Scripts/Map/MapGenerator.cs | 1492 | Expose chimney offset factors via inspector fields |
| Assets/Scripts/Map/MapGenerator.cs | 1510 | Make gear decoration ranges configurable in LevelProfile |
| Assets/Scripts/Generators/LevelGenerator.cs | 396 | Replace reflection with an interface for adaptive mode selection |
| Assets/Scripts/Generators/LevelTerrainGenerator.cs | 162 | Refactor to interface-based lookup instead of reflection |
| Assets/Scripts/Map/BatchingPerformanceTest.cs | 174 | Cache object list to avoid allocations during testing |
| Assets/Material/CollectibleMaterial | 3 | Verify metallic/smoothness values for PBR consistency |
| Assets/OSMAssets/Materials/OSM_Park_Material.mat | 3 | Adjust color to match overall scene lighting |
| Assets/Prefabs/Player.prefab | 3 | Separate player stats into dedicated ScriptableObject |
| Assets/Scenes/Level1.unity | 3 | Review occlusion and lighting settings for optimized performance |
| Assets/Scenes/Level_OSM.unity | 3 | Ensure map generation uses prefabs from OSMAssets folder |
| Assets/Resources/LevelProfiles/EasyProfile.asset | 25 | Balance collectible spawn height for easier levels |
| Assets/ScriptableObjects/HardProfile.asset | 34 | Assign default moving platform prefabs for hard difficulty |
| Assets/Scenes/GeneratedLevel.unity | 3 | Replace sample level with procedurally generated layout |
