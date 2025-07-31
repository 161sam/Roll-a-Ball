using UnityEngine;

/// <summary>
/// ScriptableObject containing required project tags and layers.
/// </summary>
[CreateAssetMenu(fileName = "TagConfig", menuName = "Roll-a-Ball/Tag Config")]
public class TagConfig : ScriptableObject
{
    public string[] tags;
    public string[] layers;
}
