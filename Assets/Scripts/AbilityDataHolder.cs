using UnityEngine;

public static class AbilityDataHolder
{
    public const int MaxLevel = 3;
    public const int UpgradeCostLv1To2 = 500;
    public const int UpgradeCostLv2To3 = 1000;

    private static int fallbackCurrencyIts = 0;

    // Menyimpan memori asal scene ("MainMenu" atau "RoomScene") untuk navigasi tombol Back
    public static string PreviousScene = "MainMenu";

    // Slot Pilihan Ability untuk Gameplay
    public static string SelectedAbility1 = "";
    public static string SelectedAbility2 = "";

    // Level Masing-Masing Ability (Default Lv 1)
    public static int LevelSteal = 1;
    public static int LevelStun = 1;
    public static int LevelJammer = 1;
    public static int LevelWarp = 1;
    public static int LevelBehindYou = 1;
    public static int LevelDuckKun = 1;
    public static int LevelMarionette = 1;

    public static int GetCurrencyIts()
    {
        if (CurrencyManager.Instance != null)
        {
            return CurrencyManager.Instance.GetBalance(CurrencyType.Its);
        }

        return fallbackCurrencyIts;
    }

    public static int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel <= 1) return UpgradeCostLv1To2; // Lv 1 -> 2 = 500 its
        if (currentLevel == 2) return UpgradeCostLv2To3; // Lv 2 -> MAX = 1000 its
        return 0;
    }

    public static int GetUpgradeCostForAbility(string abilityName)
    {
        int currentLevel = GetLevel(abilityName);
        if (currentLevel >= MaxLevel) return 0;
        return GetUpgradeCost(currentLevel);
    }

    public static bool CanAffordUpgrade(string abilityName)
    {
        int currentLevel = GetLevel(abilityName);
        if (currentLevel >= MaxLevel) return false;

        int cost = GetUpgradeCost(currentLevel);
        return GetCurrencyIts() >= cost;
    }

    public static void AddCurrency(int amount)
    {
        if (amount <= 0) return;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Its, amount);
            return;
        }

        fallbackCurrencyIts += amount;
    }

    public static bool TrySpendCurrency(int amount)
    {
        if (amount < 0) return false;
        if (amount == 0) return true;

        if (CurrencyManager.Instance != null)
        {
            return CurrencyManager.Instance.SpendCurrency(CurrencyType.Its, amount);
        }

        if (fallbackCurrencyIts < amount) return false;
        fallbackCurrencyIts -= amount;
        return true;
    }

    public static int GetLevel(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return 1;

        switch (abilityName.Trim().ToLower())
        {
            case "steal": return Mathf.Clamp(LevelSteal, 1, MaxLevel);
            case "stun": return Mathf.Clamp(LevelStun, 1, MaxLevel);
            case "jammer": return Mathf.Clamp(LevelJammer, 1, MaxLevel);
            case "warp": return Mathf.Clamp(LevelWarp, 1, MaxLevel);
            case "behind you":
            case "behindyou": return Mathf.Clamp(LevelBehindYou, 1, MaxLevel);
            case "duck-kun":
            case "duckkun":
            case "duck kun": return Mathf.Clamp(LevelDuckKun, 1, MaxLevel);
            case "marionette": return Mathf.Clamp(LevelMarionette, 1, MaxLevel);
            default: return 1;
        }
    }

    public static void Upgrade(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return;

        switch (abilityName.Trim().ToLower())
        {
            case "steal":
                LevelSteal = Mathf.Min(LevelSteal + 1, MaxLevel);
                break;
            case "stun":
                LevelStun = Mathf.Min(LevelStun + 1, MaxLevel);
                break;
            case "jammer":
                LevelJammer = Mathf.Min(LevelJammer + 1, MaxLevel);
                break;
            case "warp":
                LevelWarp = Mathf.Min(LevelWarp + 1, MaxLevel);
                break;
            case "behind you":
            case "behindyou":
                LevelBehindYou = Mathf.Min(LevelBehindYou + 1, MaxLevel);
                break;
            case "duck-kun":
            case "duckkun":
            case "duck kun":
                LevelDuckKun = Mathf.Min(LevelDuckKun + 1, MaxLevel);
                break;
            case "marionette":
                LevelMarionette = Mathf.Min(LevelMarionette + 1, MaxLevel);
                break;
        }
    }
}