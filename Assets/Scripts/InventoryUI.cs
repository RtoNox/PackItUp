using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Character List Setup")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotButtonPrefab;

    [Header("Equipped Character Display")]
    [SerializeField] private TMP_Text equippedLoadoutText;

    [Header("Duplicate Conversion UI")]
    [SerializeField] private Button convertDuplicatesButton;
    [SerializeField] private TMP_Text convertDuplicatesButtonText;
    [SerializeField] private TMP_Text conversionFeedbackText;

    [Header("Optional Scene Callback")]
    [Tooltip("Leave empty in RoomScene/GachaScene. In MainMenu, hook InventoryToMainMenu() here.")]
    public UnityEvent onClose;
    [SerializeField] private GameObject mainMenuPanel;

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        mainMenuPanel?.SetActive(true);
        onClose?.Invoke();
    }

    public void ConvertAllDuplicates()
    {
        if (PlayerInventory.Instance == null) return;

        bool converted = PlayerInventory.Instance.ConvertAllDuplicates(
            out int convertedCount,
            out int earnedCurrency
        );

        if (conversionFeedbackText != null)
        {
            if (converted)
            {
                CurrencyType currencyType = PlayerInventory.Instance.DuplicateCurrencyType;
                conversionFeedbackText.text = $"Converted {convertedCount} duplicate(s) into +{earnedCurrency} {currencyType}!";
            }
            else
            {
                conversionFeedbackText.text = "No duplicate characters to convert.";
            }
        }
    }

    private void OnEnable()
    {
        if (conversionFeedbackText != null)
            conversionFeedbackText.text = "";

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        UpdateEquippedCharacterText();
        UpdateDuplicateButtonUI();

        if (slotContainer == null || slotButtonPrefab == null) return;

        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (CharacterEntry entry in PlayerInventory.Instance.GetAllOwnedCharacters())
        {
            GameObject btnObj = Instantiate(slotButtonPrefab, slotContainer);

            bool isEquipped = PlayerInventory.Instance.IsCharacterEquipped(entry.characterID);
            string statusTag = isEquipped ? " [EQUIPPED]" : "";

            TMP_Text label = btnObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = $"[{entry.rarity}] {entry.characterName} (x{entry.count}){statusTag}";
            }

            Transform iconTransform = btnObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                GachaCharacterData asset = PlayerInventory.Instance.GetCharacterAsset(entry.characterID);
                bool hasSprite = asset != null && asset.icon != null;
                iconTransform.gameObject.SetActive(hasSprite);
                if (hasSprite)
                    iconTransform.GetComponent<Image>().sprite = asset.icon;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string capturedID = entry.characterID;
                btn.onClick.AddListener(() => PlayerInventory.Instance.EquipCharacter(capturedID));
            }
        }
    }

    private void UpdateDuplicateButtonUI()
    {
        if (PlayerInventory.Instance == null) return;

        PlayerInventory.Instance.PreviewDuplicateConversion(out int dupCount, out int rewardAmount);
        CurrencyType currencyType = PlayerInventory.Instance.DuplicateCurrencyType;

        if (convertDuplicatesButton != null)
            convertDuplicatesButton.interactable = dupCount > 0;

        if (convertDuplicatesButtonText != null)
        {
            convertDuplicatesButtonText.text = dupCount > 0
                ? $"Convert Duplicates ({dupCount}) -> +{rewardAmount} {currencyType}"
                : "No Duplicates";
        }
    }

    private void UpdateEquippedCharacterText()
    {
        if (equippedLoadoutText == null) return;

        CharacterEntry equipped = PlayerInventory.Instance.GetEquippedCharacter();
        equippedLoadoutText.text = equipped != null
            ? $"Selected Character:\n[{equipped.rarity}] {equipped.characterName}"
            : "Selected Character:\nDefault";
    }
}