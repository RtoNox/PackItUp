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
        GachaItemData wonItem = RollSingleItem();
        if (wonItem == null) return;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
            resultText.text = $"[{wonItem.rarity}] [{wonItem.clothingType}] {wonItem.itemName}";

        if (resultIcon != null)
        {
            bool hasIcon = wonItem.icon != null;
            resultIcon.gameObject.SetActive(hasIcon);
            if (hasIcon) resultIcon.sprite = wonItem.icon;
        }
    }

    public void PullTen()
    {
        List<GachaItemData> results = new List<GachaItemData>();

        for (int i = 0; i < 10; i++)
        {
            GachaItemData item = RollSingleItem();
            if (item != null)
                results.Add(item);
        }

        if (results.Count == 0) return;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultIcon != null)
            resultIcon.gameObject.SetActive(false);

        if (resultText != null)
        {
            string summary = "";
            foreach (GachaItemData item in results)
            {
                summary += $"[{item.rarity}] [{item.clothingType}] {item.itemName}\n";
            }
            resultText.text = summary;
        }
    }

    public void CloseResultPanel()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // =========================================================
    // CORE GACHA LOGIC
    // =========================================================

    private GachaItemData RollSingleItem()
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

        // Transfer the pulled item into the player's persistent inventory
        PlayerInventory.Instance.AddItem(wonItem, 1);

        Debug.Log($"[Gacha] Pulled [{wonItem.rarity}] {wonItem.itemName} -> Sent to Inventory");

        return wonItem;
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