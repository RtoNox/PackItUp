using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilitySteal : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Steal";
    public Sprite abilityIcon;
    public float range = 2f;
    public float cooldownTime = 30f;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.Q;

    [Header("Upgrade Settings")]
    [Range(1, 3)] public int abilityLevel = 1;

    [Header("Visual Effects")]
    [Tooltip("Masukkan Prefab Partikel efek Steal yang muncul di pemain korban")]
    public GameObject stealHitVfxPrefab;

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

    public float GetStealPercentage()
    {
        switch (abilityLevel)
        {
            case 1: return 0.10f;
            case 2: return 0.25f;
            case 3: return 0.40f;
            default: return 0.10f;
        }
    }

    public float GetRemainingCooldown()
    {
        if (IsServer)
        {
            float remainingServer = (lastUseTime + cooldownTime) - Time.time;
            return remainingServer > 0f ? remainingServer : 0f;
        }

        if (clientCooldownEndTime > 0f)
        {
            float remainingClient = clientCooldownEndTime - Time.time;
            return remainingClient > 0f ? remainingClient : 0f;
        }

        return 0f;
    }

    void Update()
    {
        if (!IsOwner) return;

        bool isOnCooldown = GetRemainingCooldown() > 0f;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Steal] Cooldown selesai! Ability Steal siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (AbilityJammer.IsPlayerJammed(this, "Steal"))
            {
                return;
            }

            if (isOnCooldown)
            {
                Debug.Log("Ability Steal masih cooldown!");
                return;
            }

            TryUseStealServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStealServerRpc()
    {
        if (AbilityJammer.IsPlayerJammed(this, "Steal"))
        {
            return;
        }

        if (Time.time < lastUseTime + cooldownTime)
        {
            return;
        }

        bool success = false;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range);

        foreach (var hit in hitColliders)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Player"))
            {
                PlayerPoints targetPoints = hit.GetComponent<PlayerPoints>();
                PlayerPoints myPoints = GetComponent<PlayerPoints>();

                if (targetPoints != null && myPoints != null)
                {
                    int targetScore = targetPoints.Points.Value;
                    if (targetScore > 0)
                    {
                        int stolenScore = Mathf.RoundToInt(targetScore * GetStealPercentage());

                        targetPoints.Points.Value -= stolenScore;
                        myPoints.Points.Value += stolenScore;

                        Debug.Log($"[Steal] Berhasil mencuri {stolenScore} poin dari {hit.gameObject.name}!");
                        success = true;

                        NetworkObject targetNetObj = hit.GetComponent<NetworkObject>();
                        if (targetNetObj != null)
                        {
                            ShowStealVfxClientRpc(targetNetObj.NetworkObjectId);
                        }

                        break;
                    }
                }
            }
        }

        if (success)
        {
            lastUseTime = Time.time;
            TriggerCooldownClientRpc(cooldownTime);
        }
        else
        {
            Debug.Log("[Steal] Tidak ada target valid di sekitar! Skill tidak masuk cooldown.");
        }
    }

    [ClientRpc]
    private void TriggerCooldownClientRpc(float duration)
    {
        if (IsOwner)
        {
            clientCooldownEndTime = Time.time + duration;
            wasOnCooldown = true;
        }
    }

    [ClientRpc]
    private void ShowStealVfxClientRpc(ulong targetNetworkObjectId)
    {
        if (stealHitVfxPrefab == null) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj))
        {
            GameObject targetObject = targetNetObj.gameObject;

            Vector3 spawnPosition = targetObject.transform.position + Vector3.up * 1f;

            GameObject vfxInstance = Instantiate(stealHitVfxPrefab, spawnPosition, targetObject.transform.rotation);

            vfxInstance.transform.SetParent(targetObject.transform);

            Destroy(vfxInstance, 2f);
        }
    }
}