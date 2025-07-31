using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject storing a list of achievement definitions for the AchievementSystem.
/// </summary>
[CreateAssetMenu(fileName = "AchievementDatabase", menuName = "Roll-a-Ball/Achievement Database")]
public class AchievementDatabase : ScriptableObject
{
    public List<Achievement> achievements = new();
}
