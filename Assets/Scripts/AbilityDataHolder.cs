using UnityEngine;

public static class AbilityDataHolder
{
    public const int MaxLevel = 3;

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

    // Fungsi untuk mengambil level berdasarkan nama ability
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

    // Fungsi untuk menaikkan level ability (Maksimal Lv 3)
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