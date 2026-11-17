using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryEntry
{
    public string itemID;
    public string itemName;
    public ItemRarity rarity;
    public ClothingType clothingType;
    public int count;
}

[Serializable]
public class EquippedSlot
{
    public ClothingType slotType;
    public string itemID;
}

[Serializable]
public class InventorySaveData
{
    public List<InventoryEntry> entries = new List<InventoryEntry>();
    public List<EquippedSlot> equippedSlots = new List<EquippedSlot>();
}

public class PlayerInventory : MonoBehaviour
{
    private static PlayerInventory _instance;
    public static PlayerInventory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerInventory>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerInventory");
                    _instance = go.AddComponent<PlayerInventory>();
                }
            }
            return _instance;
        }
    }

    private const string SAVE_KEY = "Player_Clothing_Inventory_v1";

    // Optional runtime lookup if you attach icons/models later
    private readonly Dictionary<string, GachaItemData> assetLookup = new Dictionary<string, GachaItemData>();
    private InventorySaveData saveData = new InventorySaveData();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadInventory();
    }

    // =========================================================
    // ADD & QUERY ITEMS
    // =========================================================

    public void AddItem(GachaItemData item, int amount = 1)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID))
        {
            Debug.LogWarning("PlayerInventory: Item or Item ID is missing.");
            return;
        }

        assetLookup[item.itemID] = item;

        InventoryEntry existing = saveData.entries.Find(e => e.itemID == item.itemID);
        if (existing != null)
        {
            existing.count += amount;
            existing.itemName = item.itemName;
            existing.rarity = item.rarity;
            existing.clothingType = item.clothingType;
        }
        else
        {
            saveData.entries.Add(new InventoryEntry
            {
                itemID = item.itemID,
                itemName = item.itemName,
                rarity = item.rarity,
                clothingType = item.clothingType,
                count = amount
            });
        }

        SaveInventory();
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Inventory] +1 [{item.rarity}] {item.clothingType}: {item.itemName}");
    }

    public List<InventoryEntry> GetAllOwnedItems()
    {
        return saveData.entries.FindAll(e => e.count > 0);
    }

    public bool HasItem(string itemID)
    {
        InventoryEntry entry = saveData.entries.Find(e => e.itemID == itemID);
        return entry != null && entry.count > 0;
    }

    public GachaItemData GetItemAsset(string itemID)
    {
        assetLookup.TryGetValue(itemID, out GachaItemData asset);
        return asset;
    }

    // =========================================================
    // 6-SLOT EQUIPMENT SYSTEM (1 PER CLOTHING TYPE)
    // =========================================================

    public void ToggleEquipItem(string itemID)
    {
        InventoryEntry item = saveData.entries.Find(e => e.itemID == itemID && e.count > 0);
        if (item == null) return;

        // If already wearing this exact item, take it off
        if (IsItemEquipped(itemID))
        {
            UnequipSlot(item.clothingType);
            return;
        }

        // Otherwise equip it into its matching ClothingType slot
        EquippedSlot slot = saveData.equippedSlots.Find(s => s.slotType == item.clothingType);
        if (slot != null)
        {
            slot.itemID = itemID;
        }
        else
        {
            saveData.equippedSlots.Add(new EquippedSlot
            {
                slotType = item.clothingType,
                itemID = itemID
            });
        }

        SaveInventory();
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Equipped] {item.clothingType} -> {item.itemName}");
    }

    public void UnequipSlot(ClothingType slotType)
    {
        EquippedSlot slot = saveData.equippedSlots.Find(s => s.slotType == slotType);
        if (slot != null)
        {
            slot.itemID = "";
            SaveInventory();
            OnInventoryChanged?.Invoke();
        }
    }

    public InventoryEntry GetEquippedInSlot(ClothingType slotType)
    {
        EquippedSlot slot = saveData.equippedSlots.Find(s => s.slotType == slotType);
        if (slot == null || string.IsNullOrEmpty(slot.itemID))
            return null;

        return saveData.entries.Find(e => e.itemID == slot.itemID && e.count > 0);
    }

    public bool IsItemEquipped(string itemID)
    {
        return saveData.equippedSlots.Exists(s => s.itemID == itemID);
    }

    // =========================================================
    // SAVE & LOAD
    // =========================================================

    private void SaveInventory()
    {
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadInventory()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            saveData = new InventorySaveData();
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        saveData = JsonUtility.FromJson<InventorySaveData>(json) ?? new InventorySaveData();
    }

    public void ClearInventory()
    {
        saveData = new InventorySaveData();
        SaveInventory();
        OnInventoryChanged?.Invoke();
    }
}