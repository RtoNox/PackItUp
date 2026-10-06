using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private bool initialized = false;

    private void Start()
    {
        StartCoroutine(InitializeSpawner());
    }

    private IEnumerator InitializeSpawner()
    {
        // Wait for NetworkManager to be ready
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        Debug.Log("PlayerSpawnManager: NetworkManager found.");

        // Wait until the network is actually running
        while (!NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        Debug.Log("PlayerSpawnManager: Network is running.");

        // Only the server should spawn players
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("PlayerSpawnManager: I am NOT the server.");
            yield break;
        }

        // Check references
        if (playerPrefab == null)
        {
            Debug.LogError(
                "PlayerSpawnManager: Player Prefab is NOT assigned!"
            );

            yield break;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError(
                "PlayerSpawnManager: No spawn points assigned!"
            );

            yield break;
        }

        // Subscribe to future clients
        NetworkManager.Singleton.OnClientConnectedCallback +=
            OnClientConnected;

        initialized = true;

        Debug.Log(
            "PlayerSpawnManager: Ready. Connected clients: " +
            NetworkManager.Singleton.ConnectedClientsList.Count
        );

        // Spawn players that are already connected
        foreach (
            NetworkClient client
            in NetworkManager.Singleton.ConnectedClientsList
        )
        {
            Debug.Log(
                "Existing client found: " +
                client.ClientId
            );

            StartCoroutine(
                SpawnPlayer(client.ClientId)
            );
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!initialized)
            return;

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsServer)
            return;

        Debug.Log(
            "Client connected: " +
            clientId
        );

        StartCoroutine(
            SpawnPlayer(clientId)
        );
    }

    private IEnumerator SpawnPlayer(ulong clientId)
    {
        // Give NGO one frame to finish registering the client
        yield return null;

        if (NetworkManager.Singleton == null)
            yield break;

        if (!NetworkManager.Singleton.IsServer)
            yield break;

        // Make sure client still exists
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            Debug.LogWarning(
                "Client " +
                clientId +
                " is no longer connected."
            );

            yield break;
        }

        NetworkClient client =
            NetworkManager.Singleton.ConnectedClients[clientId];

        // Prevent duplicate PlayerObjects
        if (client.PlayerObject != null)
        {
            Debug.Log(
                "PlayerObject already exists for client " +
                clientId
            );

            yield break;
        }

        // Check spawn points again
        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError(
                "PlayerSpawnManager: No spawn points!"
            );

            yield break;
        }

        // Choose spawn point based on client ID
        int spawnIndex =
            (int)(clientId % (ulong)spawnPoints.Length);

        Transform spawnPoint =
            spawnPoints[spawnIndex];

        if (spawnPoint == null)
        {
            Debug.LogError(
                "SpawnPoint " +
                (spawnIndex + 1) +
                " is NULL!"
            );

            yield break;
        }

        // Create player
        GameObject player = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (player == null)
        {
            Debug.LogError(
                "PlayerSpawnManager: Failed to instantiate Player!"
            );

            yield break;
        }

        NetworkObject networkObject =
            player.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError(
                "Player Prefab does not have a NetworkObject!"
            );

            Destroy(player);
            yield break;
        }

        // Spawn as this client's PlayerObject
        networkObject.SpawnAsPlayerObject(clientId);

        Debug.Log(
            "SUCCESS: Player " +
            clientId +
            " spawned at SpawnPoint " +
            (spawnIndex + 1)
        );
    }
}