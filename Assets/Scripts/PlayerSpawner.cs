using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("PlayerSpawnManager: NetworkManager NOT FOUND!");
            return;
        }

        Debug.Log("PlayerSpawnManager: Ready.");

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("PlayerSpawnManager: I am NOT the server.");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Debug.Log(
                "Existing client found: " + client.ClientId
            );

            StartCoroutine(SpawnPlayer(client.ClientId));
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        Debug.Log(
            "Client connected: " + clientId
        );

        StartCoroutine(SpawnPlayer(clientId));
    }

    private IEnumerator SpawnPlayer(ulong clientId)
    {
        // Wait a tiny amount in case NGO is still processing the connection
        yield return null;

        // Don't spawn another PlayerObject if one already exists
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            NetworkClient client =
                NetworkManager.Singleton.ConnectedClients[clientId];

            if (client.PlayerObject != null)
            {
                Debug.Log(
                    "PlayerObject already exists for client "
                    + clientId
                );

                MovePlayer(clientId, client.PlayerObject);
                yield break;
            }
        }

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

        int spawnIndex =
            (int)(clientId % (ulong)spawnPoints.Length);

        Transform spawnPoint = spawnPoints[spawnIndex];

        GameObject player = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

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

        networkObject.SpawnAsPlayerObject(clientId);

        Debug.Log(
            "Player "
            + clientId
            + " spawned at SpawnPoint "
            + (spawnIndex + 1)
        );
    }

    private void MovePlayer(
        ulong clientId,
        NetworkObject playerObject)
    {
        int spawnIndex =
            (int)(clientId % (ulong)spawnPoints.Length);

        Transform spawnPoint =
            spawnPoints[spawnIndex];

        playerObject.transform.SetPositionAndRotation(
            spawnPoint.position,
            spawnPoint.rotation
        );

        Debug.Log(
            "Player "
            + clientId
            + " moved to SpawnPoint "
            + (spawnIndex + 1)
        );
    }
}