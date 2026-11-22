using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GachaMenuState
{
    GachaScreen,
    ShopScreen
}

public class GachaManager : MonoBehaviour
{
    private struct GachaPullResult
    {
        public GachaItemData item;
        public bool isDuplicate;
        public bool convertedToCurrency;
        public int convertedCurrency;
    }

    [Header("Menu Screens")]
    public GameObject gachaScreenUI;
    public GameObject shopScreenUI;
    public GachaMenuState currentState { get; private set; }

    [Header("Gacha Item Pool (Drag your 6+ items here)")]
    [SerializeField] private List<GachaItemData> itemPool = new List<GachaItemData>();

    [Header("Rarity Drop Rates (%)")]
    [SerializeField] private float rDropRate = 70f;
    [SerializeField] private float srDropRate = 25f;
    [SerializeField] private float ssrDropRate = 5f;

    [Header("Duplicate Handling")]
    [Tooltip("Unchecked: duplicates stack in Inventory (x2, x3) for the Convert All button. Checked: auto-converts immediately on pull.")]
    [SerializeField] private bool autoConvertDuplicatesOnPull = false;

    [Header("Gacha Result UI (Optional)")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Image resultIcon;

    private void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        ChangeState(GachaMenuState.GachaScreen);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (resultPanel != null && resultPanel.activeSelf)
            {
                CloseResultPanel();
                return;
            }

            if (currentState == GachaMenuState.ShopScreen)
            {
                ChangeState(GachaMenuState.GachaScreen);
            }
        }
    }

    // =========================================================
    // GACHA ROLL BUTTONS (Hook these to your UI Buttons)
    // =========================================================

    public void PullOnce()
    {
        GachaPullResult? result = RollSingleItem();
        if (!result.HasValue) return;

        GachaPullResult pull = result.Value;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
            resultText.text = FormatPullResultLine(pull);

        if (resultIcon != null)
        {
            bool hasIcon = pull.item.icon != null;
            resultIcon.gameObject.SetActive(hasIcon);
            if (hasIcon) resultIcon.sprite = pull.item.icon;
        }
    }

    public void PullTen()
    {
        List<GachaPullResult> results = new List<GachaPullResult>();

        for (int i = 0; i < 10; i++)
        {
            GachaPullResult? result = RollSingleItem();
            if (result.HasValue)
                results.Add(result.Value);
        }

        if (results.Count == 0) return;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultIcon != null)
            resultIcon.gameObject.SetActive(false);

        if (resultText != null)
        {
            string summary = "";
            foreach (GachaPullResult pull in results)
            {
                summary += FormatPullResultLine(pull) + "\n";
            }
            resultText.text = summary;
        }
    }

    public void CloseResultPanel()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private string FormatPullResultLine(GachaPullResult pull)
    {
        string baseInfo = $"[{pull.item.rarity}] [{pull.item.clothingType}] {pull.item.itemName}";

        if (pull.convertedToCurrency)
        {
            CurrencyType currencyType = PlayerInventory.Instance.DuplicateCurrencyType;
            return $"{baseInfo} (Duplicate -> +{pull.convertedCurrency} {currencyType})";
        }

        if (pull.isDuplicate)
            return $"{baseInfo} (Duplicate)";

        return $"{baseInfo} (NEW!)";
    }

    // =========================================================
    // CORE GACHA LOGIC
    // =========================================================

    private GachaPullResult? RollSingleItem()
    {
        if (itemPool == null || itemPool.Count == 0)
        {
            Debug.LogWarning("GachaManager: Item Pool is empty! Assign GachaItemData assets in the Inspector.");
            return null;
        }

        ItemRarity rolledRarity = RollRarity();

        // Filter pool for items matching the rolled rarity
        List<GachaItemData> matchingItems = itemPool.FindAll(item => item != null && item.rarity == rolledRarity);

        // Fallback if no items exist for that rarity yet
        if (matchingItems.Count == 0)
        {
            matchingItems = itemPool.FindAll(item => item != null);
            if (matchingItems.Count == 0) return null;
        }

        GachaItemData wonItem = matchingItems[Random.Range(0, matchingItems.Count)];

        bool alreadyOwned = !string.IsNullOrEmpty(wonItem.itemID) &&
                            PlayerInventory.Instance.HasItem(wonItem.itemID);

        if (alreadyOwned && autoConvertDuplicatesOnPull)
        {
            int rewardAmount = PlayerInventory.Instance.GetDuplicateCurrencyReward(wonItem.rarity);
            CurrencyType currencyType = PlayerInventory.Instance.DuplicateCurrencyType;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddCurrency(currencyType, rewardAmount);

            return new GachaPullResult
            {
                item = wonItem,
                isDuplicate = true,
                convertedToCurrency = true,
                convertedCurrency = rewardAmount
            };
        }

        // Add item (stacks x2, x3... in inventory if already owned)
        PlayerInventory.Instance.AddItem(wonItem, 1);

        return new GachaPullResult
        {
            item = wonItem,
            isDuplicate = alreadyOwned,
            convertedToCurrency = false,
            convertedCurrency = 0
        };
    }

    private ItemRarity RollRarity()
    {
        float totalWeight = rDropRate + srDropRate + ssrDropRate;
        float roll = Random.Range(0f, totalWeight);

        if (roll < ssrDropRate)
            return ItemRarity.SSR;

        if (roll < ssrDropRate + srDropRate)
            return ItemRarity.SR;

        return ItemRarity.R;
    }

    private void SaveUnlockedItem(GachaItemData item)
    {
        if (string.IsNullOrEmpty(item.itemID)) return;

        PlayerPrefs.SetInt("GachaUnlocked_" + item.itemID, 1);
        PlayerPrefs.Save();
    }

    public bool IsItemUnlocked(string itemID)
    {
        return PlayerPrefs.GetInt("GachaUnlocked_" + itemID, 0) == 1;
    }

    // =========================================================
    // EXISTING UI STATE NAVIGATION
    // =========================================================

    public void ChangeState(GachaMenuState newState)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionToState(newState));
        currentState = newState;
    }

    public void GachaToShop()
    {
        ChangeState(GachaMenuState.ShopScreen);
    }

    public void ShopToGacha()
    {
        ChangeState(GachaMenuState.GachaScreen);
    }

    public void GachaToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator TransitionToState(GachaMenuState newState)
    {
        if (newState != GachaMenuState.GachaScreen)
        {
            yield return new WaitForSecondsRealtime(0);
        }

        currentState = newState;
        HandleStateChange();
    }

    private void HandleStateChange()
    {
        HideAllMenu();

        switch (currentState)
        {
            case GachaMenuState.GachaScreen:
                if (gachaScreenUI != null)
                    gachaScreenUI.SetActive(true);
                break;
            case GachaMenuState.ShopScreen:
                if (shopScreenUI != null)
                    shopScreenUI.SetActive(true);
                break;
        }
    }

    private void HideAllMenu()
    {
        if (gachaScreenUI != null)
            gachaScreenUI.SetActive(false);

        if (shopScreenUI != null)
            shopScreenUI.SetActive(false);
    }
}