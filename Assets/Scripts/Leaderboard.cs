using TMPro;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI entryText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI xpText;

    private void OnEnable()
    {
        RewardManager.OnRewardReceived += ShowReward;

        if (RewardManager.LastReward > 0)
         ShowReward(RewardManager.LastReward);
    }

    private void OnDisable()
    {
        RewardManager.OnRewardReceived -= ShowReward;
    }

    private void ShowReward(int amount)
    {
        rewardText.text = $"+{amount} Its";
        xpText.text = $"+{amount * 2} XP";
    }

    public void SetData(PlayerResult result)
    {
        entryText.text = $"{result.rank}. {result.playerName} - {result.points} Points";
    }
}