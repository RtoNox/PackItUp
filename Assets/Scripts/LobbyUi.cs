using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    [Header("Lobby UI References")]
    public TMP_InputField playerNameInput;
 

    public void OnHostButtonClicked()
    {
        SavePlayerName();
        PlayerPrefs.SetString("UserRole", "Host");
        SceneManager.LoadScene("RoomScene");
    }

  
    public void OnJoinButtonClicked()
    {
        SavePlayerName();
        PlayerPrefs.SetString("UserRole", "Client");
        SceneManager.LoadScene("RoomScene");
    }


    public void OnBackToMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void SavePlayerName()
    {
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            PlayerPrefs.SetString("PlayerName", playerNameInput.text);
        }
        else
        {
            PlayerPrefs.SetString("PlayerName", "Player_" + Random.Range(100, 999));
        }
    }
}