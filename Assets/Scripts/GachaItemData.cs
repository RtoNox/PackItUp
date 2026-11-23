using UnityEngine;

public enum ItemRarity
{
    R,
    SR,
    SSR
}

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Gacha/Character Data")]
public class GachaCharacterData : ScriptableObject
{
    [Header("Character Info")]
    public string characterID;
    public string characterName;
    public ItemRarity rarity = ItemRarity.R;

    [Header("Visual Assets")]
    public Sprite icon;
    public GameObject characterPrefab;
}