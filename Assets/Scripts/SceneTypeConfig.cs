using UnityEngine;

/// <summary>
/// Configuration asset defining which scenes are procedural or static.
/// Provides data-driven scene type detection for SceneTypeDetector.
/// </summary>
[CreateAssetMenu(fileName = "SceneTypeConfig", menuName = "Roll-a-Ball/Scene Type Config")]
public class SceneTypeConfig : ScriptableObject
{
    [Header("Procedural Scenes")]
    public string[] proceduralScenes = { "GeneratedLevel", "Level_OSM", "MiniGame" };

    [Header("Static Scenes")]
    public string[] staticScenes = { "Level1", "Level2", "Level3" };
}
