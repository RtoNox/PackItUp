using TMPro;
using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private CurrencyType currency;
    [SerializeField] private TMP_Text amountText;

    private void Start()
    {
        UpdateCurrencyDisplay();
    }

    public void UpdateCurrencyDisplay()
    {
        int amount = CurrencyManager.Instance.GetBalance(currency);
        amountText.text = amount.ToString();
    }
}