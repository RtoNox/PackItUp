using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using ParrelSync;
#endif

public class LobbyUI : MonoBehaviour
{
    [Header("Lobby UI References")]
    public TMP_InputField playerNameInput;

    public void OnHostButtonClicked()
    {
        SavePlayerName();

        PlayerPrefs.SetString("UserRole", "Host");
        PlayerPrefs.Save();

        SceneManager.LoadScene("RoomScene");
    }

    public void OnJoinButtonClicked()
    {
        SavePlayerName();

        PlayerPrefs.SetString("UserRole", "Client");
        PlayerPrefs.Save();

        SceneManager.LoadScene("RoomScene");
    }

    public void OnBackToMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void SavePlayerName()
    {
        string playerName;

        if (playerNameInput != null &&
            !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            playerName = playerNameInput.text.Trim();
        }
        else
        {
            playerName = "Player_" + Random.Range(100, 999);
        }

        string playerNameKey = GetPlayerNameKey();

        PlayerPrefs.SetString(playerNameKey, playerName);
        PlayerPrefs.Save();

        Debug.Log(
            "Saved Player Name: " + playerName +
            " | Key: " + playerNameKey
        );
    }

    private string GetPlayerNameKey()
    {
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string cloneArgument = ClonesManager.GetArgument();

            return "PlayerName_Clone_" + cloneArgument;
        }

        return "PlayerName_Original";
#else
        return "PlayerName";
#endif
    }
}