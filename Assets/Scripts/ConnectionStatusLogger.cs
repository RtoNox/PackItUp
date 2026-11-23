using UnityEngine;
using Unity.Netcode;

public class ConnectionStatusLogger : MonoBehaviour
{
    private void Start()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += id =>
            Debug.Log($"[NGO] Client Connected: {id}");

        NetworkManager.Singleton.OnClientDisconnectCallback += id =>
        {
            string reason = NetworkManager.Singleton.DisconnectReason;
            Debug.LogError($"[NGO] Client Disconnected: {id} | Reason: '{reason}'");
        };
    }
}