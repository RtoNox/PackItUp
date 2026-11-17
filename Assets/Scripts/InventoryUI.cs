using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory List Setup")]
    [Tooltip("The Content transform of your ScrollView or Vertical/Grid Layout Group.")]
    [SerializeField] private Transform slotContainer;

    [Tooltip("A simple UI Button prefab with a child TMP_Text.")]
    [SerializeField] private GameObject slotButtonPrefab;

    [Header("Equipped Loadout Display (Shows all 6 slots)")]
    [SerializeField] private TMP_Text equippedLoadoutText;

    private void OnEnable()
    {
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
        UpdateEquippedLoadoutText();

        if (slotContainer == null || slotButtonPrefab == null) return;

        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (InventoryEntry entry in PlayerInventory.Instance.GetAllOwnedItems())
        {
            GameObject btnObj = Instantiate(slotButtonPrefab, slotContainer);

            bool isEquipped = PlayerInventory.Instance.IsItemEquipped(entry.itemID);
            string statusTag = isEquipped ? " [EQUIPPED]" : "";

            TMP_Text label = btnObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = $"[{entry.rarity}] [{entry.clothingType}] {entry.itemName} (x{entry.count}){statusTag}";
            }

            // Hide optional Icon child if no sprite asset exists yet
            Transform iconTransform = btnObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                GachaItemData asset = PlayerInventory.Instance.GetItemAsset(entry.itemID);
                bool hasSprite = asset != null && asset.icon != null;
                iconTransform.gameObject.SetActive(hasSprite);
                if (hasSprite)
                    iconTransform.GetComponent<Image>().sprite = asset.icon;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string capturedID = entry.itemID;
                btn.onClick.AddListener(() => PlayerInventory.Instance.ToggleEquipItem(capturedID));
            }
        }
    }

    private void UpdateEquippedLoadoutText()
    {
        if (equippedLoadoutText == null) return;

        string summary = "Current Outfit:\n";

        foreach (ClothingType type in Enum.GetValues(typeof(ClothingType)))
        {
            InventoryEntry equipped = PlayerInventory.Instance.GetEquippedInSlot(type);
            string itemDisplay = equipped != null
                ? $"[{equipped.rarity}] {equipped.itemName}"
                : "None";

            summary += $"• {type}: {itemDisplay}\n";
        }

        equippedLoadoutText.text = summary;
    }
}