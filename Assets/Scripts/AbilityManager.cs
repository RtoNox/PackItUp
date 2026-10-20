using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class AbilityManager : MonoBehaviour
{
    // [Header("UI Panels")]
    // public GameObject mainMenuPanel;
    // public GameObject settingsPanel;
    // public GameObject abilitySelectionPanel;

    [Header("UI Text Status Slots (Opsional)")]
    public TMP_Text slot1Text; 
    public TMP_Text slot2Text; 

    [Header("Teks Tombol Ability (Equip / UnEquip)")]
    public TMP_Text btnTextSteal;
    public TMP_Text btnTextStun;
    public TMP_Text btnTextJammer;
    public TMP_Text btnTextWarp;
    public TMP_Text btnTextBehindYou;

    [Header("UI Text Level Indicator")]
    public TMP_Text stealLevelText;
    public TMP_Text stunLevelText;
    public TMP_Text jammerLevelText;
    public TMP_Text warpLevelText;
    public TMP_Text behindYouLevelText;

    void Start()
    {
        // string currentSceneName = SceneManager.GetActiveScene().name;

        // if (currentSceneName == "Ability")
        // {
        //     if (abilitySelectionPanel != null) abilitySelectionPanel.SetActive(true);
        //     if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        //     if (settingsPanel != null) settingsPanel.SetActive(false);
        // }
        // else
        // {
        //     OpenMainMenu();
        // }

        UpdateAllUI();
    }

    // public void OpenMainMenu()
    // {
    //     if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    //     if (settingsPanel != null) settingsPanel.SetActive(false);
    //     if (abilitySelectionPanel != null) abilitySelectionPanel.SetActive(false);
    // }

    // ==========================================
    // LOGIKA TOMBOL BACK CERDAS
    // ==========================================

    public void OnBackFromAbilityPanel()
    {
        string targetScene = AbilityDataHolder.PreviousScene;
        
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MainMenu"; // Fallback aman
        }

        Debug.Log($"[AbilityUI] Tombol Back diklik. Kembali ke scene: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }



    public void OnAbilityButtonClicked(string abilityName)
    {
        bool isEquippedInSlot1 = AbilityDataHolder.SelectedAbility1.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase);
        bool isEquippedInSlot2 = AbilityDataHolder.SelectedAbility2.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase);

        if (isEquippedInSlot1 || isEquippedInSlot2)
        {
            UnequipAbility(abilityName);
        }
        else
        {
            EquipAbility(abilityName);
        }

        UpdateAllUI();
    }

    private void EquipAbility(string abilityName)
    {
        if (string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility1))
        {
            AbilityDataHolder.SelectedAbility1 = abilityName;
            Debug.Log($"[AbilityUI] '{abilityName}' berhasil di-Equip ke Slot 1 (Q).");
            return;
        }

        if (string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility2))
        {
            AbilityDataHolder.SelectedAbility2 = abilityName;
            Debug.Log($"[AbilityUI] '{abilityName}' berhasil di-Equip ke Slot 2 (E).");
            return;
        }

        Debug.LogWarning("[AbilityUI] Kedua slot sudah penuh! Unequip salah satu ability terlebih dahulu.");
    }

    private void UnequipAbility(string abilityName)
    {
        if (AbilityDataHolder.SelectedAbility1.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase))
        {
            AbilityDataHolder.SelectedAbility1 = "";
            Debug.Log($"[AbilityUI] '{abilityName}' di-Unequip dari Slot 1 (Q).");
        }
        else if (AbilityDataHolder.SelectedAbility2.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase))
        {
            AbilityDataHolder.SelectedAbility2 = "";
            Debug.Log($"[AbilityUI] '{abilityName}' di-Unequip dari Slot 2 (E).");
        }
    }



    private void UpdateAllUI()
    {
        // 1. Status Slot Q & E
        if (slot1Text != null)
            slot1Text.text = string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility1) ? "Slot 1 (Q): Empty" : $"Slot 1 (Q): {AbilityDataHolder.SelectedAbility1}";

        if (slot2Text != null)
            slot2Text.text = string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility2) ? "Slot 2 (E): Empty" : $"Slot 2 (E): {AbilityDataHolder.SelectedAbility2}";

        // 2. Teks Tombol Equip / UnEquip Dinamis
        UpdateSingleButtonText("Steal", btnTextSteal);
        UpdateSingleButtonText("Stun", btnTextStun);
        UpdateSingleButtonText("Jammer", btnTextJammer);
        UpdateSingleButtonText("Warp", btnTextWarp);
        UpdateSingleButtonText("Behind You", btnTextBehindYou);

        // 3. Teks Level Indicator
        if (stealLevelText != null) stealLevelText.text = $"Lv {AbilityDataHolder.LevelSteal}";
        if (stunLevelText != null) stunLevelText.text = $"Lv {AbilityDataHolder.LevelStun}";
        if (jammerLevelText != null) jammerLevelText.text = $"Lv {AbilityDataHolder.LevelJammer}";
        if (warpLevelText != null) warpLevelText.text = $"Lv {AbilityDataHolder.LevelWarp}";
        if (behindYouLevelText != null) behindYouLevelText.text = $"Lv {AbilityDataHolder.LevelBehindYou}";
    }

    private void UpdateSingleButtonText(string abilityName, TMP_Text buttonText)
    {
        if (buttonText == null) return;

        bool isEquipped = AbilityDataHolder.SelectedAbility1.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase) ||
                         AbilityDataHolder.SelectedAbility2.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase);

        buttonText.text = isEquipped ? "UnEquip" : "Equip";
    }



    public void UpgradeAbility(string abilityName)
    {
        AbilityDataHolder.Upgrade(abilityName);
        UpdateAllUI();
        Debug.Log($"[Upgrade] Ability {abilityName} sekarang Level {AbilityDataHolder.GetLevel(abilityName)}!");
    }


    public void ClickSteal() { OnAbilityButtonClicked("Steal"); }
    public void ClickStun() { OnAbilityButtonClicked("Stun"); }
    public void ClickJammer() { OnAbilityButtonClicked("Jammer"); }
    public void ClickWarp() { OnAbilityButtonClicked("Warp"); }
    public void ClickBehindYou() { OnAbilityButtonClicked("Behind You"); }
}