using UnityEngine;
using Unity.Netcode;

public class IT_points : NetworkBehaviour
{
    [SerializeField] private int pointValue = 1;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        NetworkObject playerObject =
            other.GetComponentInParent<NetworkObject>();

        if (playerObject == null)
            return;

        PlayerPoints playerPoints =
            playerObject.GetComponent<PlayerPoints>();

        if (playerPoints == null)
        {
            Debug.LogWarning(
                "Player does not have PlayerPoints component!"
            );

            return;
        }

        // Lock the Point immediately.
        // This prevents another player from collecting it.
        collected = true;

        // Give the point to THIS player.
        playerPoints.AddPoints(pointValue);

        Debug.Log(
            "Player " +
            playerObject.OwnerClientId +
            " collected IT Point! +" +
            pointValue
        );

        NetworkObject networkObject =
            GetComponent<NetworkObject>();

        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn();
        }
    }
}