using System;
using UnityEngine;

[Serializable]
public class BPTier
{
    public int freeReward;
    public int premiumReward;
}

public class BPReward : MonoBehaviour
{
    public static BPReward Instance { get; private set; }

    [SerializeField] private int xpPerLevel = 1000;
    [SerializeField] private BPTier[] tiers;

    private const string XPKey = "BP_XP";
    private const string LevelKey = "BP_Level";
    private const string PremiumKey = "HavePremium";

    public int Level { get; private set; }
    public int XP { get; private set; }
    public int XPPerLevel => xpPerLevel;
    public int MaxLevel => tiers.Length;
    public bool IsMaxLevel => Level >= MaxLevel;
    public bool HasPremium => PlayerPrefs.GetInt(PremiumKey, 0) == 1;

    public event Action OnBPChanged;
    public event Action<int> OnLevelUp;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void Start()
    {
        if (BPTimer.Instance != null)
            BPTimer.Instance.OnSeasonReset += HandleSeasonReset;
    }

    private void OnDestroy()
    {
        if (BPTimer.Instance != null)
            BPTimer.Instance.OnSeasonReset -= HandleSeasonReset;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;

        XP += amount;

        while (XP >= xpPerLevel && Level < MaxLevel)
        {
            XP -= xpPerLevel;
            Level++;
            OnLevelUp?.Invoke(Level);
        }

        if (IsMaxLevel) XP = 0;

        Save();
        OnBPChanged?.Invoke();
    }

    // ---- Reward claiming ----

    public bool IsClaimed(int level, bool premium) =>
        PlayerPrefs.GetInt(ClaimKey(level, premium), 0) == 1;

    public bool CanClaim(int level, bool premium)
    {
        if (level < 1 || level > MaxLevel) return false;
        if (level > Level) return false;               // not reached yet
        if (premium && !HasPremium) return false;      // locked behind premium
        return !IsClaimed(level, premium);
    }

    public bool TryClaim(int level, bool premium)
    {
        if (!CanClaim(level, premium)) return false;

        BPTier tier = tiers[level - 1];
        int amount = premium ? tier.premiumReward : tier.freeReward;

        CurrencyManager.Instance.AddCurrency(CurrencyType.Its, amount);

        PlayerPrefs.SetInt(ClaimKey(level, premium), 1);
        PlayerPrefs.Save();
        OnBPChanged?.Invoke();
        return true;
    }

    private string ClaimKey(int level, bool premium) =>
        $"BP_Claimed_{(premium ? "P" : "F")}_{level}";

    private void Load()
    {
        Level = PlayerPrefs.GetInt(LevelKey, 0);
        XP = PlayerPrefs.GetInt(XPKey, 0);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(LevelKey, Level);
        PlayerPrefs.SetInt(XPKey, XP);
        PlayerPrefs.Save();
    }

    private void HandleSeasonReset()
    {
        for (int i = 1; i <= MaxLevel; i++)
        {
            PlayerPrefs.DeleteKey(ClaimKey(i, false));
            PlayerPrefs.DeleteKey(ClaimKey(i, true));
        }
        Load();
        OnBPChanged?.Invoke();
    }
}