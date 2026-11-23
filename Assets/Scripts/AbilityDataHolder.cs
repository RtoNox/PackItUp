using UnityEngine;

public static class AbilityDataHolder
{
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

    // Fungsi untuk mengambil level berdasarkan nama ability
    public static int GetLevel(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return 1;

        switch (abilityName.Trim().ToLower())
        {
            case "steal": return LevelSteal;
            case "stun": return LevelStun;
            case "jammer": return LevelJammer;
            case "warp": return LevelWarp;
            case "behind you":
            case "behindyou": return LevelBehindYou;
            default: return 1;
        }
    }

    // Fungsi untuk menaikkan level ability
    public static void Upgrade(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return;

        switch (abilityName.Trim().ToLower())
        {
            case "steal": LevelSteal++; break;
            case "stun": LevelStun++; break;
            case "jammer": LevelJammer++; break;
            case "warp": LevelWarp++; break;
            case "behind you":
            case "behindyou": LevelBehindYou++; break;
        }
    }
}