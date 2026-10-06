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

        Points.Value += amount;

        Debug.Log(
            "Player " +
            OwnerClientId +
            " now has " +
            Points.Value +
            " point(s)."
        );
    }
}