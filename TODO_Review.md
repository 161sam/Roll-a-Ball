# TODO Review
The following tasks from `TODO_Index.md` remain open due to missing files or larger refactors.
They were reviewed again and marked accordingly in `TODO_Index.md`:
- **TODO-OPT#14 - TODO-OPT#15**: `AutoSceneRepair.cs` and `AutoSceneSetup.cs` are missing from the repository, so the proposed helper methods cannot be implemented.
- **TODO-OPT#21 - TODO-OPT#22**: MapGenerator refactoring would require extensive changes and the original files are partially missing.
- **TODO-OPT#32**: Collider pooling implemented in MapGeneratorBatched.
- **TODO-OPT#42 - TODO-OPT#43**: Loading level and achievement data from external configuration needs a dedicated data format.
- **TODO-OPT#49**: Pathfinding based goal placement is a larger feature.
- **TODO-OPT#86 - TODO-OPT#87**: Sequence controller and event deregistration for
  `SteampunkGateController` need broader architecture decisions. No event
  subscriptions are present in the current script, so nothing can be deregistered.
- **TODO-OPT#92 - TODO-OPT#96**: Environment prefabs are missing in multiple
  scenes and require Unity Editor adjustments.
