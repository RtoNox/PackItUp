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

        if (player.TryGetComponent<CharacterController>(out var cc))
            cc.enabled = false;

        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);

        if (cc != null)
            cc.enabled = true;

        // Tell the owning client to snap to its spawn point after a short delay
        // so the NetworkObject is guaranteed to exist on the client first
        StartCoroutine(SendSpawnTransformDelayed(netObj, clientId, spawnPoint.position, spawnPoint.rotation));

        spawnedClients.Add(clientId);
        Debug.Log($"[PlayerSpawnManager] Spawned client {clientId} at {spawnPoint.name} ({spawnPoint.position})");
    }

    private System.Collections.IEnumerator SendSpawnTransformDelayed(NetworkObject netObj, ulong clientId, Vector3 pos, Quaternion rot)
    {
        yield return null;

        if (netObj == null || !netObj.IsSpawned) yield break;

        ClientRpcParams targetClient = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };
        SetSpawnTransformClientRpc(netObj, pos, rot, targetClient);
    }

    [ClientRpc]
    private void SetSpawnTransformClientRpc(NetworkObjectReference playerRef, Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        if (!playerRef.TryGet(out NetworkObject playerNetObj))
            return;

        GameObject player = playerNetObj.gameObject;

        if (player.TryGetComponent<CharacterController>(out var cc))
            cc.enabled = false;

        player.transform.SetPositionAndRotation(position, rotation);

        if (player.TryGetComponent<NetworkTransform>(out var netTransform) && netTransform.CanCommitToTransform)
            netTransform.Teleport(position, rotation, player.transform.localScale);

        if (cc != null)
            cc.enabled = true;
    }
}