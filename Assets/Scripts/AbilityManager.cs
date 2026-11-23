using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class AbilityManager : MonoBehaviour
{
    public const int MaxUpgradeLevel = 3;

    [Header("UI Text Currency & Feedback (Opsional)")]
    public TMP_Text currencyItsText;
    public TMP_Text feedbackText;

    [Header("UI Text Status Slots (Opsional)")]
    public TMP_Text slot1Text;
    public TMP_Text slot2Text;

    [Header("Teks Tombol Ability (Equip / UnEquip)")]
    public TMP_Text btnTextSteal;
    public TMP_Text btnTextStun;
    public TMP_Text btnTextJammer;
    public TMP_Text btnTextWarp;
    public TMP_Text btnTextBehindYou;
    public TMP_Text btnTextDuckKun;
    public TMP_Text btnTextMarionette;

    [Header("UI Text Level Indicator")]
    public TMP_Text stealLevelText;
    public TMP_Text stunLevelText;
    public TMP_Text jammerLevelText;
    public TMP_Text warpLevelText;
    public TMP_Text behindYouLevelText;
    public TMP_Text duckKunLevelText;
    public TMP_Text marionetteLevelText;

    [Header("UI Text Harga Upgrade (Opsional)")]
    public TMP_Text costTextSteal;
    public TMP_Text costTextStun;
    public TMP_Text costTextJammer;
    public TMP_Text costTextWarp;
    public TMP_Text costTextBehindYou;
    public TMP_Text costTextDuckKun;
    public TMP_Text costTextMarionette;

    [Header("Tombol Upgrade (Opsional)")]
    public Button upgradeBtnSteal;
    public Button upgradeBtnStun;
    public Button upgradeBtnJammer;
    public Button upgradeBtnWarp;
    public Button upgradeBtnBehindYou;
    public Button upgradeBtnDuckKun;
    public Button upgradeBtnMarionette;

    void Start()
    {
        SetFeedback("");
        UpdateAllUI();
    }

    void OnEnable()
    {
        UpdateAllUI();
    }

    public void OnBackFromAbilityPanel()
    {
        string targetScene = AbilityDataHolder.PreviousScene;

        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MainMenu";
        }

        Debug.Log($"[AbilityUI] Tombol Back diklik. Kembali ke scene: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }

    private bool IsEquippedInSlot1(string abilityName)
    {
        return !string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility1) &&
               AbilityDataHolder.SelectedAbility1.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsEquippedInSlot2(string abilityName)
    {
        return !string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility2) &&
               AbilityDataHolder.SelectedAbility2.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase);
    }

    public void OnAbilityButtonClicked(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return;

        if (IsEquippedInSlot1(abilityName) || IsEquippedInSlot2(abilityName))
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
            SetFeedback($"{abilityName} dipasang ke Slot 1 (Q).");
            Debug.Log($"[AbilityUI] '{abilityName}' berhasil di-Equip ke Slot 1 (Q).");
            return;
        }

        if (string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility2))
        {
            AbilityDataHolder.SelectedAbility2 = abilityName;
            SetFeedback($"{abilityName} dipasang ke Slot 2 (E).");
            Debug.Log($"[AbilityUI] '{abilityName}' berhasil di-Equip ke Slot 2 (E).");
            return;
        }

        SetFeedback("Kedua slot sudah penuh! Lepas salah satu ability dulu.");
        Debug.LogWarning("[AbilityUI] Kedua slot sudah penuh! Unequip salah satu ability terlebih dahulu.");
    }

    private void UnequipAbility(string abilityName)
    {
        if (IsEquippedInSlot1(abilityName))
        {
            AbilityDataHolder.SelectedAbility1 = "";
            SetFeedback($"{abilityName} dilepas dari Slot 1 (Q).");
            Debug.Log($"[AbilityUI] '{abilityName}' di-Unequip dari Slot 1 (Q).");
        }
        else if (IsEquippedInSlot2(abilityName))
        {
            AbilityDataHolder.SelectedAbility2 = "";
            SetFeedback($"{abilityName} dilepas dari Slot 2 (E).");
            Debug.Log($"[AbilityUI] '{abilityName}' di-Unequip dari Slot 2 (E).");
        }
    }

    private void UpdateAllUI()
    {
        if (currencyItsText != null)
        {
            currencyItsText.text = $"{AbilityDataHolder.GetCurrencyIts()} its";
        }

        if (slot1Text != null)
            slot1Text.text = string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility1)
                ? "Slot 1 (Q): Empty"
                : $"Slot 1 (Q): {AbilityDataHolder.SelectedAbility1}";

        if (slot2Text != null)
            slot2Text.text = string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility2)
                ? "Slot 2 (E): Empty"
                : $"Slot 2 (E): {AbilityDataHolder.SelectedAbility2}";

        UpdateSingleButtonText("Steal", btnTextSteal);
        UpdateSingleButtonText("Stun", btnTextStun);
        UpdateSingleButtonText("Jammer", btnTextJammer);
        UpdateSingleButtonText("Warp", btnTextWarp);
        UpdateSingleButtonText("Behind You", btnTextBehindYou);
        UpdateSingleButtonText("Duck-Kun", btnTextDuckKun);
        UpdateSingleButtonText("Marionette", btnTextMarionette);

        UpdateSingleUpgradeUI("Steal", stealLevelText, costTextSteal, upgradeBtnSteal);
        UpdateSingleUpgradeUI("Stun", stunLevelText, costTextStun, upgradeBtnStun);
        UpdateSingleUpgradeUI("Jammer", jammerLevelText, costTextJammer, upgradeBtnJammer);
        UpdateSingleUpgradeUI("Warp", warpLevelText, costTextWarp, upgradeBtnWarp);
        UpdateSingleUpgradeUI("Behind You", behindYouLevelText, costTextBehindYou, upgradeBtnBehindYou);
        UpdateSingleUpgradeUI("Duck-Kun", duckKunLevelText, costTextDuckKun, upgradeBtnDuckKun);
        UpdateSingleUpgradeUI("Marionette", marionetteLevelText, costTextMarionette, upgradeBtnMarionette);
    }

    private void UpdateSingleUpgradeUI(string abilityName, TMP_Text levelText, TMP_Text costText, Button upgradeButton)
    {
        int rawLevel = AbilityDataHolder.GetLevel(abilityName);
        bool isMax = rawLevel >= MaxUpgradeLevel;

        if (levelText != null)
        {
            if (isMax)
            {
                levelText.text = "MAX";
            }
            else
            {
                int displayLevel = Mathf.Clamp(rawLevel, 1, MaxUpgradeLevel - 1);
                levelText.text = displayLevel.ToString();
            }
        }

        if (costText != null)
        {
            if (isMax)
            {
                costText.text = "MAX";
            }
            else
            {
                int cost = AbilityDataHolder.GetUpgradeCost(rawLevel);
                costText.text = $"{cost} its";
            }
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = !isMax && AbilityDataHolder.CanAffordUpgrade(abilityName);
        }
    }

    private void UpdateSingleButtonText(string abilityName, TMP_Text buttonText)
    {
        if (buttonText == null) return;

        bool isEquipped = IsEquippedInSlot1(abilityName) || IsEquippedInSlot2(abilityName);
        buttonText.text = isEquipped ? "UnEquip" : "Equip";
    }

    public void UpgradeAbility(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return;

        int currentLevel = AbilityDataHolder.GetLevel(abilityName);
        if (currentLevel >= MaxUpgradeLevel)
        {
            UpdateAllUI();
            SetFeedback($"{abilityName} sudah level MAX.");
            Debug.Log($"[Upgrade] Ability {abilityName} sudah mencapai level maksimal (MAX / Lv {MaxUpgradeLevel})!");
            return;
        }

        int cost = AbilityDataHolder.GetUpgradeCost(currentLevel);
        if (!AbilityDataHolder.TrySpendCurrency(cost))
        {
            UpdateAllUI();
            SetFeedback($"Currency 'its' kurang! Butuh {cost} its.");
            Debug.LogWarning($"[Upgrade] Currency 'its' tidak cukup untuk upgrade {abilityName} (Lv {currentLevel} -> {currentLevel + 1})! Butuh {cost} its, sisa {AbilityDataHolder.GetCurrencyIts()} its.");
            return;
        }

        AbilityDataHolder.Upgrade(abilityName);
        int newLevel = AbilityDataHolder.GetLevel(abilityName);
        UpdateAllUI();
        SetFeedback(newLevel >= MaxUpgradeLevel
            ? $"{abilityName} naik ke MAX!"
            : $"{abilityName} naik ke Level {newLevel}!");
        Debug.Log($"[Upgrade] Berhasil upgrade {abilityName} ke Level {newLevel} (-{cost} its, sisa {AbilityDataHolder.GetCurrencyIts()} its)!");
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    // Tombol Klik Equip / UnEquip
    public void ClickSteal() { OnAbilityButtonClicked("Steal"); }
    public void ClickStun() { OnAbilityButtonClicked("Stun"); }
    public void ClickJammer() { OnAbilityButtonClicked("Jammer"); }
    public void ClickWarp() { OnAbilityButtonClicked("Warp"); }
    public void ClickBehindYou() { OnAbilityButtonClicked("Behind You"); }
    public void ClickDuckKun() { OnAbilityButtonClicked("Duck-Kun"); }
    public void ClickMarionette() { OnAbilityButtonClicked("Marionette"); }

    // Shortcut Klik Upgrade (Opsional untuk Inspector)
    public void UpgradeSteal() { UpgradeAbility("Steal"); }
    public void UpgradeStun() { UpgradeAbility("Stun"); }
    public void UpgradeJammer() { UpgradeAbility("Jammer"); }
    public void UpgradeWarp() { UpgradeAbility("Warp"); }
    public void UpgradeBehindYou() { UpgradeAbility("Behind You"); }
    public void UpgradeDuckKun() { UpgradeAbility("Duck-Kun"); }
    public void UpgradeMarionette() { UpgradeAbility("Marionette"); }
}