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
        public GachaCharacterData character;
        public bool isDuplicate;
        public bool convertedToCurrency;
        public int convertedCurrency;
    }

    [Header("Menu Screens")]
    public GameObject gachaScreenUI;
    public GameObject shopScreenUI;
    public GachaMenuState currentState { get; private set; }

    [Header("Gacha Character Pool (Drag your GachaCharacterData assets here)")]
    [SerializeField] private List<GachaCharacterData> characterPool = new List<GachaCharacterData>();

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
        GachaPullResult? result = RollSingleCharacter();
        if (!result.HasValue) return;

        GachaPullResult pull = result.Value;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
            resultText.text = FormatPullResultLine(pull);

        if (resultIcon != null)
        {
            bool hasIcon = pull.character.icon != null;
            resultIcon.gameObject.SetActive(hasIcon);
            if (hasIcon) resultIcon.sprite = pull.character.icon;
        }
    }

    public void PullTen()
    {
        List<GachaPullResult> results = new List<GachaPullResult>();

        for (int i = 0; i < 10; i++)
        {
            GachaPullResult? result = RollSingleCharacter();
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
        string baseInfo = $"[{pull.character.rarity}] {pull.character.characterName}";

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

    private GachaPullResult? RollSingleCharacter()
    {
        if (characterPool == null || characterPool.Count == 0)
        {
            Debug.LogWarning("GachaManager: Character Pool is empty! Assign GachaCharacterData assets in the Inspector.");
            return null;
        }

        ItemRarity rolledRarity = RollRarity();

        List<GachaCharacterData> matchingCharacters =
            characterPool.FindAll(c => c != null && c.rarity == rolledRarity);

        if (matchingCharacters.Count == 0)
        {
            matchingCharacters = characterPool.FindAll(c => c != null);
            if (matchingCharacters.Count == 0) return null;
        }

        GachaCharacterData wonCharacter = matchingCharacters[Random.Range(0, matchingCharacters.Count)];

        bool alreadyOwned = !string.IsNullOrEmpty(wonCharacter.characterID) &&
                            PlayerInventory.Instance.HasCharacter(wonCharacter.characterID);

        if (alreadyOwned && autoConvertDuplicatesOnPull)
        {
            int rewardAmount = PlayerInventory.Instance.GetDuplicateCurrencyReward(wonCharacter.rarity);
            CurrencyType currencyType = PlayerInventory.Instance.DuplicateCurrencyType;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddCurrency(currencyType, rewardAmount);

            return new GachaPullResult
            {
                character = wonCharacter,
                isDuplicate = true,
                convertedToCurrency = true,
                convertedCurrency = rewardAmount
            };
        }

        PlayerInventory.Instance.AddCharacter(wonCharacter, 1);

        return new GachaPullResult
        {
            character = wonCharacter,
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