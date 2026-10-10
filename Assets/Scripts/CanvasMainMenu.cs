using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasMainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject abilitySelectionPanel;

    void Start()
    {
        OpenMainMenu();
    }

    public void OnStartButtonClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (abilitySelectionPanel != null) abilitySelectionPanel.SetActive(true);
    }

    public void OnSettingButtonClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (abilitySelectionPanel != null) abilitySelectionPanel.SetActive(false);
    }

    public void OpenMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (abilitySelectionPanel != null) abilitySelectionPanel.SetActive(false);
    }

    public void OnSelectAbilitySteal()
    {
        PlayerPrefs.SetString("SelectedAbility", "Steal");
        SceneManager.LoadScene("Lobby");
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}