using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasMainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

   
    public NetworkButtons networkButtons;

    void Start()
    {
        OpenMainMenu();
    }

    public void OnStartButtonClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (networkButtons != null)
        {
            networkButtons.ShowMenu();
        }
    }

    public void OnSettingButtonClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);


        if (networkButtons != null)
        {
            networkButtons.HideMenu();
        }
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}