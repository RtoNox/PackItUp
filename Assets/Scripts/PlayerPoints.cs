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

    public NetworkVariable<NetworkObjectReference> marionetteCasterRef = new NetworkVariable<NetworkObjectReference>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void AddPoints(int amount)
    {
        if (!IsServer)
            return;

        AbilityDuckKun duckKun = GetComponent<AbilityDuckKun>();
        float multiplier = 1.0f;

        if (duckKun != null && duckKun.enabled && duckKun.isBuffActive.Value)
        {
            multiplier = duckKun.scoreMultiplier.Value;
        }

        int finalPoints = Mathf.RoundToInt(amount * multiplier);

        // CEK APAKAH PLAYER INI SEDANG DIKAITKAN OLEH MARIONETTE MILIK ORANG LAIN
        if (marionetteCasterRef.Value.TryGet(out NetworkObject casterNetObj))
        {
            PlayerPoints casterPoints = casterNetObj.GetComponent<PlayerPoints>();
            if (casterPoints != null)
            {
                float marionetteMultiplier = 1.0f;
                AbilityMarionette marionette = casterNetObj.GetComponent<AbilityMarionette>();
                if (marionette != null)
                {
                    marionetteMultiplier = marionette.GetScoreMultiplier();
                }

                int stolenPoints = Mathf.RoundToInt(finalPoints * marionetteMultiplier);

                casterPoints.Points.Value += stolenPoints;

                Debug.Log(
                    $"[Marionette] Poin DICURI! Player {OwnerClientId} mendapat 0 poin. " +
                    $"Sebanyak {stolenPoints} poin (Base: {finalPoints} x Marionette: {marionetteMultiplier:0.00}x) " +
                    $"dialihkan ke Caster (Player {casterNetObj.OwnerClientId}). Total poin Caster sekarang: {casterPoints.Points.Value}."
                );
                return;
            }
        }

        Points.Value += finalPoints;

        Debug.Log(
            "Player " + OwnerClientId +
            " mendapat " + finalPoints + " poin (Base: " + amount + " x Multiplier: " + multiplier + "). " +
            "Total sekarang: " + Points.Value + " point(s)."
        );
    }
}