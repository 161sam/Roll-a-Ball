using UnityEngine;

/// <summary>
/// ScriptableObject container for lists of addresses.
/// </summary>
[CreateAssetMenu(fileName = "AddressList", menuName = "Roll-a-Ball/Address List")]
public class AddressList : ScriptableObject
{
    public string[] addresses;
}

