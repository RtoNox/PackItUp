using Unity.Netcode;
using UnityEngine;

public class RewardManager : NetworkBehaviour
{
    [SerializeField] private PointCalculator pointCalculator;

    private bool rewardsGiven;

    public void GiveRewards()
    {
        if (!IsServer || rewardsGiven) return;
        rewardsGiven = true;

        var results = pointCalculator.CalculateRankings();

        foreach (var result in results)
        {
            int amount = GetRewardForRank(result.rank);

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { result.clientId }
                }
            };

            GiveCurrencyClientRpc(amount, rpcParams);
        }
    }

    private int GetRewardForRank(int rank)
    {
        return rank switch
        {
            1 => 100,
            2 => 50,
            3 => 50,
            4 => 50
        };
    }

    public static event System.Action<int> OnRewardReceived;
    public static int LastReward { get; private set; }

    [ClientRpc]
    private void GiveCurrencyClientRpc(int amount, ClientRpcParams rpcParams = default)
    {
        CurrencyManager.Instance.AddCurrency(CurrencyType.Its, amount);
        BPReward.Instance.AddXP(amount * 2);

        LastReward = amount;
        OnRewardReceived?.Invoke(amount);
    }

    public void ResetForNewRound()
    {
        LastReward = 0;
        rewardsGiven = false;
    }
}