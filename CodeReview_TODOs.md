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
