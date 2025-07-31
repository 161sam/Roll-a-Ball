using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject storing a list of level definitions for the ProgressionManager.
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Roll-a-Ball/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelInfo> levels = new();
}
