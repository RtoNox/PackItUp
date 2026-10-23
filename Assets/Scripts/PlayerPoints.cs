using UnityEngine;
using Unity.Netcode;

public class PlayerPoints : NetworkBehaviour
{
    public NetworkVariable<int> Points =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public void AddPoints(int amount)
    {
        if (!IsServer)
            return;

        AbilityDuckKun duckKun = GetComponent<AbilityDuckKun>();

        float multiplier = (duckKun != null && duckKun.enabled && duckKun.isBuffActive) ? duckKun.scoreMultiplier.Value : 1.0f;

        int finalPoints = Mathf.RoundToInt(amount * multiplier);

        Points.Value += finalPoints;

        Debug.Log(
            "Player " +
            OwnerClientId +
            " mendapat " + finalPoints + " poin (Base: " + amount + " x Multiplier: " + multiplier + "). Total sekarang: " +
            Points.Value +
            " point(s)."
        );
    }
}