
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

    public void GoToAbility()
    {
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
