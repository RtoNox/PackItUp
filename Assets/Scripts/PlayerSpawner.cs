using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.SceneManagement;

public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private readonly HashSet<ulong> spawnedClients = new HashSet<ulong>();
    private int nextSpawnIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        foreach (ulong clientId in clientsCompleted)
        {
            SpawnPlayer(clientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (spawnedClients.Contains(clientId)) return;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null) return;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("PlayerSpawnManager: No spawn points assigned!");
            return;
        }

        Transform spawnPoint = spawnPoints[nextSpawnIndex % spawnPoints.Length];
        nextSpawnIndex++;

        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        // If the prefab has a CharacterController, briefly disable it so Unity's physics
        // cache doesn't snap the character back to (0, 0, 0) on the first frame.
        if (player.TryGetComponent<CharacterController>(out var cc))
            cc.enabled = false;

        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);

        // Force NetworkTransform to snap immediately instead of starting at (0, 0, 0)
        if (player.TryGetComponent<NetworkTransform>(out var netTransform))
            netTransform.Teleport(spawnPoint.position, spawnPoint.rotation, player.transform.localScale);

        if (cc != null)
            cc.enabled = true;

        spawnedClients.Add(clientId);
        Debug.Log($"[PlayerSpawnManager] Spawned client {clientId} at {spawnPoint.name} ({spawnPoint.position})");
    }
}