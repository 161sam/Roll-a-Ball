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
