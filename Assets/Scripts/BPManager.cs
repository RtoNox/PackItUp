using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using System;
using System.Globalization;


public class BPManager : MonoBehaviour
{
    public GameObject confirmPanel;
    public GameObject premiumCover;

    public Button upgradeButton;

    public TMP_Text timerText;

    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text xpText;

    private const string PremiumKey = "HavePremium";
    private bool havePremium;

    private void Start()
    {
        confirmPanel.SetActive(false);
        RefreshPremiumState();
    }

    private void Update()
    {
        TimeSpan t = BPTimer.Instance.TimeRemaining;
        timerText.text = $"{t.Days}d {t.Hours:00}h {t.Minutes:00}m {t.Seconds:00}s";
    }

    private void OnEnable()
    {
        if (BPTimer.Instance != null)
            BPTimer.Instance.OnSeasonReset += RefreshPremiumState;
        BPReward.Instance.OnBPChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (BPTimer.Instance != null)
            BPTimer.Instance.OnSeasonReset -= RefreshPremiumState;
        if (BPReward.Instance != null)
            BPReward.Instance.OnBPChanged -= Refresh;
    }

    private void Refresh()
    {
        var bp = BPReward.Instance;

        levelText.text = $"Level {bp.Level}";
        xpText.text = bp.IsMaxLevel ? "MAX" : $"{bp.XP} / {bp.XPPerLevel}";
    }

    private void RefreshPremiumState()
    {   
        havePremium = PlayerPrefs.GetInt("HavePremium", 0) == 1;
        premiumCover.SetActive(!havePremium);
    }

    public void BPToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void BuyPremium()
    {
        confirmPanel.SetActive(true);
    }

    public void CancelBuyPremium()
    {
        confirmPanel.SetActive(false);
    }

    public void ConfirmBuyPremium()
    {
        confirmPanel.SetActive(false);
        premiumCover.SetActive(false);
        havePremium = true;

        PlayerPrefs.SetInt(PremiumKey, 1);
        PlayerPrefs.Save();
    }
}