using System;
using UnityEngine;

[CreateAssetMenu, Serializable]
public class Character_SO : ScriptableObject
{
    public string characterName;
    public Material material;
    public CharacterId characterId;
}
