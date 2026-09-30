using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ITPointSpawner : NetworkBehaviour
{
    [Header("IT Point")]
    [SerializeField] private GameObject itPointPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 10f;

    private List<GameObject> currentItems = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        // Spawn one IT point at every spawn point
        SpawnInitialItems();
    }

    private void SpawnInitialItems()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                SpawnItem(spawnPoints[i]);
            }
        }
    }

    private void SpawnItem(Transform spawnPoint)
    {
        if (itPointPrefab == null)
        {
            Debug.LogError("IT Point Prefab is not assigned!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn Point is null!");
            return;
        }

        GameObject item = Instantiate(
            itPointPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        NetworkObject networkObject =
            item.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError(
                "IT Point prefab needs a NetworkObject component!"
            );

            Destroy(item);
            return;
        }

        networkObject.Spawn();

        currentItems.Add(item);

        // Watch for this item being destroyed/collected
        StartCoroutine(WaitForItemDestroyed(item));
    }

    private IEnumerator WaitForItemDestroyed(GameObject item)
    {
        // Wait until the item is destroyed
        while (item != null)
        {
            yield return null;
        }

        // Item was collected
        currentItems.Remove(item);

        // Wait before spawning the replacement
        yield return new WaitForSeconds(spawnInterval);

        SpawnRandomItem();
    }

    private void SpawnRandomItem()
    {
        List<Transform> availablePoints = new List<Transform>();

        // Find spawn points that don't currently have an item
        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            bool pointOccupied = false;

            foreach (GameObject item in currentItems)
            {
                if (item == null)
                    continue;

                float distance =
                    Vector3.Distance(item.transform.position, point.position);

                if (distance < 0.1f)
                {
                    pointOccupied = true;
                    break;
                }
            }

            if (!pointOccupied)
            {
                availablePoints.Add(point);
            }
        }

        // No available spawn points
        if (availablePoints.Count == 0)
            return;

        // Pick a random empty spawn point
        int randomIndex = Random.Range(0, availablePoints.Count);

        SpawnItem(availablePoints[randomIndex]);
    }
}