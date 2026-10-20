using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public enum CurrencyType
{
    Its
}

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] private CurrencyType currency;
    [SerializeField] private TMP_Text amountText;

    private Dictionary<CurrencyType, int> balances = new();

    private const string SaveKeyPrefix = "Currency_";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCurrencies();
        UpdateCurrencyDisplay();
    }

    public int GetBalance(CurrencyType currency)
    {
        if (balances.TryGetValue(currency, out int balance))
        {
            return balance;
        }

        return 0;
    }

    public void UpdateCurrencyDisplay()
    {
        int amount = GetBalance(currency);
        amountText.text = amount.ToString();
    }

    public void AddCurrency(CurrencyType currency, int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Currency amount must be greater than 0.");
            return;
        }

        int currentBalance = GetBalance(currency);
        balances[currency] = currentBalance + amount;

        SaveCurrency(currency);
        UpdateCurrencyDisplay();

        Debug.Log($"Added {amount} {currency}. New balance: {balances[currency]}");
    }

    public bool SpendCurrency(CurrencyType currency, int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Insufficient");
            return false;
        }

        int currentBalance = GetBalance(currency);

        if (currentBalance < amount)
        {
            Debug.Log($"Not enough {currency}.");
            return false;
        }

        balances[currency] = currentBalance - amount;

        SaveCurrency(currency);
        UpdateCurrencyDisplay();

        Debug.Log($"Spent {amount} {currency}. New balance: {balances[currency]}");

        return true;
    }

    public bool HasEnough(CurrencyType currency, int amount)
    {
        return GetBalance(currency) >= amount;
    }

    private void SaveCurrency(CurrencyType currency)
    {
        string key = SaveKeyPrefix + currency;

        PlayerPrefs.SetInt(key, balances[currency]);
        PlayerPrefs.Save();
    }

    private void LoadCurrencies()
    {
        foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
        {
            string key = SaveKeyPrefix + currency;

            int balance = PlayerPrefs.GetInt(key, 0);
            balances[currency] = balance;
        }
    }

    public void ResetAllCurrencies()
    {
        foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
        {
            string key = SaveKeyPrefix + currency;

            PlayerPrefs.DeleteKey(key);
            balances[currency] = 0;
        }

        PlayerPrefs.Save();
    }

    public void AddTierOneCurrency()
    {
        int currentBalance = GetBalance(currency);
        balances[currency] = currentBalance + 60;

        SaveCurrency(currency);
        UpdateCurrencyDisplay();
    }

    public void AddTierTwoCurrency()
    {
        int currentBalance = GetBalance(currency);
        balances[currency] = currentBalance + 360;

        SaveCurrency(currency);
        UpdateCurrencyDisplay();
    }

    public void AddTierThreeCurrency()
    {
        int currentBalance = GetBalance(currency);
        balances[currency] = currentBalance + 2160;

        SaveCurrency(currency);
        UpdateCurrencyDisplay();
    }

    public void SpendOnePullCurrency()
    {
        int currentBalance = GetBalance(currency);
        balances[currency] = currentBalance - 100;

        SaveCurrency(currency);
        UpdateCurrencyDisplay();
    }

    public void SpendTenPullCurrency()
    {
        int currentBalance = GetBalance(currency);
        balances[currency] = currentBalance - 1000;

        SaveCurrency(currency);
        UpdateCurrencyDisplay();
    }
}