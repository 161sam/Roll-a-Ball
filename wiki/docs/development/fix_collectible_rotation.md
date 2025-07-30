# Fix Collectible Rotation

The global rotation issue was traced back to `CollectibleController.HandleRotation`. The function has been removed and replaced with `RotateLocally`, which applies rotation using `transform.localRotation` to ensure every collectible spins around its own pivot. Update loops now call `RotateLocally()`.

