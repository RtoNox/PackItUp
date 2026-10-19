using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void GoToLobby()
    {
        LoadScene("Lobby");
    }

    public void GoToGacha()
    {
        LoadScene("Gacha");
    }

    public void GoToProfile()
    {
        LoadScene("Profile");
    }

    public void GoToEmotes()
    {
        LoadScene("Emotes");
    }

    public void GoToBP()
    {
        LoadScene("BP");
    }

    public void GoToBaju()
    {
        LoadScene("Baju");
    }

    public void GoToGiveMeMoney()
    {
        LoadScene("GIVEMEMONEYPLSBROINEEDTHIS");
    }

    // Dipanggil dari tombol Ability / Skil di Main Menu
    public void GoToAbility()
    {
        AbilityDataHolder.PreviousScene = "MainMenu";
        Debug.Log("[Navigation] Membuka Ability dari MainMenu");
        LoadScene("Ability");
    }

    // DIPANGGIL KHUSUS DARI TOMBOL SKIL DI ROOMSCENE
    public void OpenAbilityFromRoomScene()
    {
        AbilityDataHolder.PreviousScene = "RoomScene";
        Debug.Log("[Navigation] Membuka Ability dari RoomScene");
        LoadScene("Ability");
    }

    public void GoToSettings()
    {
        LoadScene("Settings");
    }

    // =========================================================
    // LOAD SCENE
    // =========================================================

    private void LoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError(
                "Cannot load scene: " + sceneName +
                ". Make sure it is added to Build Profiles."
            );
        }
    }
}