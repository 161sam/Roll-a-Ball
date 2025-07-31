# Fix Collectible Rotation

The global rotation issue was traced back to `CollectibleController.HandleRotation`. The method rotated all active collectibles in world space, causing them to share a single Y‑axis. The function has been removed completely and `Update()` now calls `RotateLocally()` instead. `RotateLocally` applies rotation with `transform.localRotation` so each collectible spins around its own pivot:

```csharp
// CollectibleController.cs
private void RotateLocally()
{
    if (!collectibleData.rotateObject) return;

    float angle = collectibleData.rotationSpeed * Time.deltaTime;
    transform.localRotation *= Quaternion.Euler(0f, angle, 0f);
}
```

A search across all scripts in `Assets/` confirmed that no other component overrides this behaviour. Each collectible now rotates independently in place.

