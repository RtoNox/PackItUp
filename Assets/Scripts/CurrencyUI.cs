using TMPro;
using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private CurrencyType currency;
    [SerializeField] private TMP_Text amountText;

    private void Start()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        int amount = CurrencyManager.Instance.GetBalance(currency);
        amountText.text = amount.ToString();
    }
}