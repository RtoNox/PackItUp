using UnityEngine;
using Unity.Netcode;

public class IT_points : NetworkBehaviour
{
    [SerializeField] private int pointValue = 1;

    private void OnTriggerEnter(Collider other)
    {        
        if (!IsServer)
            return;

        if (!other.CompareTag("Player"))
            return;

        Debug.Log("IT Point collected! +" + pointValue);

        NetworkObject networkObject = GetComponent<NetworkObject>();

        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn();
        }
    }
}