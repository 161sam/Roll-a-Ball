using UnityEngine;

/// <summary>
/// Configuration for save system settings.
/// </summary>
[CreateAssetMenu(fileName = "SaveConfig", menuName = "Roll-a-Ball/Save Config")]
public class SaveConfig : ScriptableObject
{
    [Header("Encryption")]
    public string encryptionKey = "RollABallGame2025";
}

