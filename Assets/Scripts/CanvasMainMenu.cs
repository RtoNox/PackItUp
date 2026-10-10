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

    private string tempSlot1 = "";
    private string tempSlot2 = "";

    void Start()
    {
        ResetSkillSelection();
        OpenMainMenu();
    }

    public void OnStartButtonClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (abilitySelectionPanel != null) abilitySelectionPanel.SetActive(true);
        
        ResetSkillSelection();
        Debug.Log("[MainMenu] Memulai pemilihan skill. Silakan pilih Skill 1 (F).");
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

    private void ResetSkillSelection()
    {
        tempSlot1 = "";
        tempSlot2 = "";
    }

    public void OnSelectSkill(string abilityName)
    {
        if (string.IsNullOrEmpty(tempSlot1))
        {
            tempSlot1 = abilityName;
            Debug.Log($"[MainMenu] Slot 1 (F) terpilih: {tempSlot1}. Silakan pilih Skill 2 (R).");
            return;
        }

        if (string.IsNullOrEmpty(tempSlot2))
        {
            if (abilityName.Equals(tempSlot1, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[MainMenu] Skill tersebut sudah dipilih untuk Slot 1. Pilih skill yang lain!");
                return;
            }

            tempSlot2 = abilityName;
            Debug.Log($"[MainMenu] Slot 2 (R) terpilih: {tempSlot2}. Kedua skill lengkap!");

            PlayerPrefs.SetString("SelectedAbility1", tempSlot1);
            PlayerPrefs.SetString("SelectedAbility2", tempSlot2);
            PlayerPrefs.Save();

            Debug.Log("[MainMenu] Berpindah ke scene Lobby...");
            SceneManager.LoadScene("Lobby");
        }
    }

    // Shortcut fungsi untuk tombol UI di Main Menu Inspector
    public void SelectSteal() { OnSelectSkill("Steal"); }
    public void SelectStun() { OnSelectSkill("Stun"); }
    public void SelectJammer() { OnSelectSkill("Jammer"); }
    public void SelectWarp() { OnSelectSkill("Warp"); }
    public void SelectBehindYou() { OnSelectSkill("Behind You"); }

    public void OnExitButtonClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}