using UnityEngine;

public enum ItemRarity
{
    R,
    SR,
    SSR
}

public enum ClothingType
{
    Shirt,
    Pants,
    Shoe,
    Hat,
    Cape,
    Trail
}

[CreateAssetMenu(fileName = "NewGachaItem", menuName = "Gacha/Gacha Item")]
public class GachaItemData : ScriptableObject
{
    [Header("Placeholder / Core Info")]
    public string itemID;
    public string itemName;
    public ItemRarity rarity = ItemRarity.R;
    public ClothingType clothingType = ClothingType.Shirt;

    [Header("Visual Assets (Optional - Leave Empty for Now)")]
    public Sprite icon;
    public GameObject modelPrefab;
}