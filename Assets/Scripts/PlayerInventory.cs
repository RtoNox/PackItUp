using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterEntry
{
    public string characterID;
    public string characterName;
    public ItemRarity rarity;
    public int count;
}

[Serializable]
public class CharacterInventorySaveData
{
    public List<CharacterEntry> entries = new List<CharacterEntry>();
    public string equippedCharacterID;
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

    private const string SAVE_KEY = "Player_Character_Inventory_v1";

    [Header("Character Database (Drag ALL GachaCharacterData assets here)")]
    [SerializeField] private List<GachaCharacterData> allCharacters = new List<GachaCharacterData>();

    [Header("Default Starter Character (Optional)")]
    [Tooltip("Automatically equipped if the player has no character selected yet.")]
    [SerializeField] private GachaCharacterData defaultCharacter;

    [Header("Duplicate Conversion Settings (Adjustable)")]
    [SerializeField] private CurrencyType duplicateCurrencyType = CurrencyType.Its;
    [SerializeField] private int duplicateRewardR = 20;
    [SerializeField] private int duplicateRewardSR = 100;
    [SerializeField] private int duplicateRewardSSR = 500;

    public CurrencyType DuplicateCurrencyType => duplicateCurrencyType;

    private readonly Dictionary<string, GachaCharacterData> assetLookup = new Dictionary<string, GachaCharacterData>();
    private CharacterInventorySaveData saveData = new CharacterInventorySaveData();

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

        RegisterAllCharacters();
        LoadInventory();
        EnsureStarterCharacter();
    }

    private void RegisterAllCharacters()
    {
        if (defaultCharacter != null && !string.IsNullOrEmpty(defaultCharacter.characterID))
        {
            assetLookup[defaultCharacter.characterID] = defaultCharacter;
        }

        foreach (GachaCharacterData character in allCharacters)
        {
            if (character != null && !string.IsNullOrEmpty(character.characterID))
            {
                assetLookup[character.characterID] = character;
            }
        }
    }

    private void EnsureStarterCharacter()
    {
        if (defaultCharacter == null || string.IsNullOrEmpty(defaultCharacter.characterID))
            return;

        if (!HasCharacter(defaultCharacter.characterID))
        {
            saveData.entries.Add(new CharacterEntry
            {
                characterID = defaultCharacter.characterID,
                characterName = defaultCharacter.characterName,
                rarity = defaultCharacter.rarity,
                count = 1
            });
        }

        if (string.IsNullOrEmpty(saveData.equippedCharacterID))
        {
            saveData.equippedCharacterID = defaultCharacter.characterID;
        }

        SaveInventory();
    }

    // =========================================================
    // ADD & QUERY CHARACTERS
    // =========================================================

    public void AddCharacter(GachaCharacterData character, int amount = 1)
    {
        if (character == null || string.IsNullOrEmpty(character.characterID))
        {
            Debug.LogWarning("PlayerInventory: Character or Character ID is missing.");
            return;
        }

        assetLookup[character.characterID] = character;

        CharacterEntry existing = saveData.entries.Find(e => e.characterID == character.characterID);
        if (existing != null)
        {
            existing.count += amount;
            existing.characterName = character.characterName;
            existing.rarity = character.rarity;
        }
        else
        {
            saveData.entries.Add(new CharacterEntry
            {
                characterID = character.characterID,
                characterName = character.characterName,
                rarity = character.rarity,
                count = amount
            });

            // Auto-equip if nothing is equipped yet
            if (string.IsNullOrEmpty(saveData.equippedCharacterID))
            {
                saveData.equippedCharacterID = character.characterID;
            }
        }

        SaveInventory();
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Inventory] +{amount} [{character.rarity}] Character: {character.characterName}");
    }

    public List<CharacterEntry> GetAllOwnedCharacters()
    {
        return saveData.entries.FindAll(e => e.count > 0);
    }

    public bool HasCharacter(string characterID)
    {
        CharacterEntry entry = saveData.entries.Find(e => e.characterID == characterID);
        return entry != null && entry.count > 0;
    }

    public GachaCharacterData GetCharacterAsset(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
            return defaultCharacter;

        if (assetLookup.TryGetValue(characterID, out GachaCharacterData asset))
            return asset;

        GachaCharacterData found = allCharacters.Find(c => c != null && c.characterID == characterID);
        if (found != null)
        {
            assetLookup[characterID] = found;
            return found;
        }

        return defaultCharacter;
    }

    // =========================================================
    // DUPLICATE CONVERSION
    // =========================================================

    public int GetDuplicateCurrencyReward(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.SSR:
                return duplicateRewardSSR;
            case ItemRarity.SR:
                return duplicateRewardSR;
            case ItemRarity.R:
            default:
                return duplicateRewardR;
        }
    }

    public void PreviewDuplicateConversion(out int totalDuplicates, out int totalCurrency)
    {
        totalDuplicates = 0;
        totalCurrency = 0;

        foreach (CharacterEntry entry in saveData.entries)
        {
            if (entry.count > 1)
            {
                int extraCopies = entry.count - 1;
                totalDuplicates += extraCopies;
                totalCurrency += extraCopies * GetDuplicateCurrencyReward(entry.rarity);
            }
        }
    }

    public bool ConvertAllDuplicates(out int totalDuplicatesConverted, out int totalCurrencyGained)
    {
        totalDuplicatesConverted = 0;
        totalCurrencyGained = 0;

        foreach (CharacterEntry entry in saveData.entries)
        {
            if (entry.count > 1)
            {
                int extraCopies = entry.count - 1;
                int reward = extraCopies * GetDuplicateCurrencyReward(entry.rarity);

                totalDuplicatesConverted += extraCopies;
                totalCurrencyGained += reward;

                // Keep 1 copy so character ownership and active selection remain intact
                entry.count = 1;
            }
        }

        if (totalDuplicatesConverted <= 0)
            return false;

        if (totalCurrencyGained > 0)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCurrency(duplicateCurrencyType, totalCurrencyGained);
            }
            else
            {
                Debug.LogWarning("PlayerInventory: CurrencyManager.Instance not found! Duplicate currency could not be added.");
            }
        }

        SaveInventory();
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Inventory] Converted {totalDuplicatesConverted} duplicate character(s) -> +{totalCurrencyGained} {duplicateCurrencyType}");
        return true;
    }

    // =========================================================
    // CHARACTER EQUIP SYSTEM (1 ACTIVE CHARACTER)
    // =========================================================

    public void EquipCharacter(string characterID)
    {
        CharacterEntry entry = saveData.entries.Find(e => e.characterID == characterID && e.count > 0);
        if (entry == null) return;

        // If clicking the already-equipped character, revert to starter character if one is set
        if (saveData.equippedCharacterID == characterID)
        {
            if (defaultCharacter != null && defaultCharacter.characterID != characterID)
            {
                saveData.equippedCharacterID = defaultCharacter.characterID;
                SaveInventory();
                OnInventoryChanged?.Invoke();
            }
            return;
        }

        saveData.equippedCharacterID = characterID;
        SaveInventory();
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Equipped Character] -> {entry.characterName}");
    }

    public CharacterEntry GetEquippedCharacter()
    {
        if (string.IsNullOrEmpty(saveData.equippedCharacterID))
            return null;

        return saveData.entries.Find(e => e.characterID == saveData.equippedCharacterID && e.count > 0);
    }

    public string GetEquippedCharacterID()
    {
        CharacterEntry equipped = GetEquippedCharacter();
        if (equipped != null)
            return equipped.characterID;

        return defaultCharacter != null ? defaultCharacter.characterID : "";
    }

    public bool IsCharacterEquipped(string characterID)
    {
        return !string.IsNullOrEmpty(characterID) && saveData.equippedCharacterID == characterID;
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
            saveData = new CharacterInventorySaveData();
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        saveData = JsonUtility.FromJson<CharacterInventorySaveData>(json) ?? new CharacterInventorySaveData();
    }

    public void ClearInventory()
    {
        saveData = new CharacterInventorySaveData();
        EnsureStarterCharacter();
        SaveInventory();
        OnInventoryChanged?.Invoke();
    }
}