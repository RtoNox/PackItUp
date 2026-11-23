using System;
using System.Globalization;
using UnityEngine;

public class BPTimer : MonoBehaviour
{
    public static BPTimer Instance { get; private set; }

    [SerializeField] private int seasonLengthDays = 30;

    private const string SeasonEndKey = "BP_SeasonEnd";
    private DateTime seasonEnd;

    // Other scripts subscribe to this to refresh their UI
    public event Action OnSeasonReset;

    public TimeSpan TimeRemaining => seasonEnd - DateTime.UtcNow;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSeasonEnd();
        CheckSeason();
    }

    private void Update()
    {
        CheckSeason();
    }

    private void LoadSeasonEnd()
    {
        string saved = PlayerPrefs.GetString(SeasonEndKey, "");

        if (!DateTime.TryParse(saved, null, DateTimeStyles.RoundtripKind, out seasonEnd))
        {
            // First launch: start the first season now
            seasonEnd = DateTime.UtcNow.AddDays(seasonLengthDays);
            SaveSeasonEnd();
        }
    }

    private void SaveSeasonEnd()
    {
        PlayerPrefs.SetString(SeasonEndKey, seasonEnd.ToString("o"));
        PlayerPrefs.Save();
    }

    private void CheckSeason()
    {
        if (DateTime.UtcNow < seasonEnd) return;

        // Skip ahead if the player was away for several seasons
        while (DateTime.UtcNow >= seasonEnd)
            seasonEnd = seasonEnd.AddDays(seasonLengthDays);

        SaveSeasonEnd();
        ResetBattlePass();
    }

    private void ResetBattlePass()
    {
        PlayerPrefs.SetInt("HavePremium", 0);
        PlayerPrefs.SetInt("BP_Level", 0);
        PlayerPrefs.SetInt("BP_XP", 0);
        // delete/reset any other battlepass keys here
        PlayerPrefs.Save();

        OnSeasonReset?.Invoke();
    }
}