using UnityEngine;

/// <summary>
/// ScriptableObject holding prefab references used by MapGenerator scripts.
/// Allows central configuration and reuse across generators.
/// </summary>
[CreateAssetMenu(fileName = "MapPrefabsConfig", menuName = "Roll-a-Ball/Map Prefabs Config")]
public class MapPrefabsConfig : ScriptableObject
{
    [Header("Generation Prefabs")]
    public GameObject roadPrefab;
    public GameObject buildingPrefab;
    public GameObject areaPrefab;
    public GameObject collectiblePrefab;
    public GameObject goalZonePrefab;
    public GameObject playerPrefab;

    [Header("Steampunk Decoration Prefabs")]
    public GameObject gearPrefab;
    public GameObject steamPipePrefab;
    public GameObject chimneySmokeParticles;
    public GameObject steamEmitterPrefab;
}
