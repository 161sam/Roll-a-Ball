using UnityEngine;

/// <summary>
/// ScriptableObject container for ground material paths.
/// </summary>
[CreateAssetMenu(fileName = "GroundMaterialConfig", menuName = "Roll-a-Ball/Ground Material Config")]
public class GroundMaterialConfig : ScriptableObject
{
    [Tooltip("Resources paths to load fallback materials from.")]
    public string[] materialPaths;
}

