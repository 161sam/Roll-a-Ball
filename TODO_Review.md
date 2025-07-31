# TODO Review
The following tasks from `TODO_Index.md` remain open due to missing files or larger refactors:
- **TODO-OPT#14 - TODO-OPT#15**: `AutoSceneRepair.cs` and `AutoSceneSetup.cs` are missing from the repository, so the proposed helper methods cannot be implemented.
- **TODO-OPT#22**: MapGenerator_Original.cs is missing, so refactoring cannot be completed.
- **TODO-OPT#32**: Collider pooling implemented in MapGeneratorBatched.
- **TODO-OPT#42 - TODO-OPT#43**: Loading level and achievement data from external configuration needs a dedicated data format.
- **TODO-OPT#43** additionally requires default moving platform prefabs for `HardProfile.asset`, which are not present in the repository.
- **TODO-OPT#49**: Pathfinding based goal placement is a larger feature.
- **TODO-OPT#86**: Sequence controller for `SteampunkGateController` requires a broader design. Event deregistration (TODO-OPT#87) was deemed unnecessary as no global events are subscribed.
- **TODO-OPT#92 - TODO-OPT#96**: Environment prefabs are missing in multiple
  scenes and require Unity Editor adjustments.

Additional unresolved items from `CodeReview_TODOs.md`:
- `MapGeneratorBatched.cs` line 469 - batching in a job would require a more
  extensive Job System setup.
- `Level1.unity` and `Level_OSM.unity` require scene-specific adjustments that
  cannot be edited without the Unity editor.
- `HardProfile.asset` needs moving platform prefabs which are absent from the
  repository.
- `GeneratedLevel.unity` is a placeholder scene and should be regenerated via
  the level generator once available.
