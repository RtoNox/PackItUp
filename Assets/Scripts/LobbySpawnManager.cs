using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LobbySpawnManager : NetworkBehaviour
{
    [Header("Lobby Player")]
    [SerializeField] private GameObject lobbyPlayerPrefab;

    [Header("Lobby Spawn Points (4 Slots)")]
    [SerializeField] private Transform[] lobbySpawnPoints;

    // Tracks which spawn slot each client is occupying so nobody overlaps
    private readonly Dictionary<ulong, int> occupiedSlots = new Dictionary<ulong, int>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Spawn anyone already connected (such as the Host)
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnLobbyPlayer(clientId);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        SpawnLobbyPlayer(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        occupiedSlots.Remove(clientId);
    }

    private void SpawnLobbyPlayer(ulong clientId)
    {
        if (lobbyPlayerPrefab == null || lobbySpawnPoints == null || lobbySpawnPoints.Length == 0)
        {
            Debug.LogError("LobbySpawnManager: Missing prefab or spawn points!");
            return;
        }

        if (occupiedSlots.ContainsKey(clientId)) return;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null) return;

        int slotIndex = GetFirstAvailableSlot(clientId);
        Transform spawnPoint = lobbySpawnPoints[slotIndex];

        GameObject lobbyPlayer = Instantiate(lobbyPlayerPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkObject netObj = lobbyPlayer.GetComponent<NetworkObject>();

        // destroyWithScene defaults to true, so NGO automatically cleans this up when Map1 loads
        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        occupiedSlots[clientId] = slotIndex;
    }

    private int GetFirstAvailableSlot(ulong clientId)
    {
        for (int i = 0; i < lobbySpawnPoints.Length; i++)
        {
            if (!occupiedSlots.ContainsValue(i))
                return i;
        }
        return (int)(clientId % (ulong)lobbySpawnPoints.Length);
    }
}