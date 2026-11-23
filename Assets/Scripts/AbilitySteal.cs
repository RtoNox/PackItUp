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

    private readonly NetworkVariable<double> cooldownEndServerTime = new NetworkVariable<double>(
        0.0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.Q;

    [Header("Upgrade Settings (Max Lv 3)")]
    [Range(1, 3)] public int abilityLevel = 1;
    public int maxAbilityLevel = 3;

    [Tooltip("Batas atas persentase curian (default 0.40 = 40%)")]
    public float maxStealCap = 0.40f;

    [Header("Visual Effects")]
    [Tooltip("Masukkan Prefab Partikel efek Steal yang muncul di pemain korban")]
    public GameObject stealHitVfxPrefab;

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SyncUpgradeLevelFromHolder();
        Debug.Log($"[Steal] OnNetworkSpawn | IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, Lv={abilityLevel}");
    }

    public void SyncUpgradeLevelFromHolder()
    {
        int savedLevel = AbilityDataHolder.GetLevel(abilityName);
        SetUpgradeLevel(savedLevel);
    }

    public void SetUpgradeLevel(int newLevel)
    {
        abilityLevel = Mathf.Clamp(newLevel, 1, maxAbilityLevel);
    }

    // Batas bawah naik tiap level: Lv 1 = 10%, Lv 2 = 20%, Lv 3 = 30%
    public float GetMinStealPercentage()
    {
        switch (abilityLevel)
        {
            case 1: return 0.10f;
            case 2: return 0.20f;
            case 3: return 0.30f;
            default: return 0.10f;
        }
    }

    public float GetMaxStealPercentage()
    {
        return Mathf.Max(GetMinStealPercentage(), maxStealCap);
    }

    public float GetRandomStealPercentage()
    {
        return Random.Range(GetMinStealPercentage(), GetMaxStealPercentage());
    }

    public float GetRemainingCooldown()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            double nowServerTime = NetworkManager.Singleton.ServerTime.Time;
            float remainingNet = (float)(cooldownEndServerTime.Value - nowServerTime);
            if (remainingNet > 0f)
            {
                return remainingNet;
            }
        }

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
        if (!IsOwner || !IsSpawned) return;

        bool isOnCooldown = GetRemainingCooldown() > 0f;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Steal] Cooldown selesai! Ability Steal siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            Debug.Log($"[Steal] Input {assignedKey} ditekan oleh OwnerClientId={OwnerClientId} | isOnCooldown={isOnCooldown}");

            if (AbilityJammer.IsPlayerJammed(this, "Steal"))
            {
                return;
            }

            if (isOnCooldown)
            {
                Debug.Log($"Ability Steal masih cooldown! Sisa: {GetRemainingCooldown():0.0}s");
                return;
            }

            TryUseStealServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStealServerRpc()
    {
        if (!IsSpawned) return;

        Debug.Log($"[Steal] TryUseStealServerRpc diterima di Server untuk OwnerClientId={OwnerClientId}");

        if (AbilityJammer.IsPlayerJammed(this, "Steal"))
        {
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.ServerTime.Time < cooldownEndServerTime.Value)
            {
                return;
            }
        }
        else if (Time.time < lastUseTime + cooldownTime)
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
                        float stealPercent = GetRandomStealPercentage();
                        int stolenScore = Mathf.RoundToInt(targetScore * stealPercent);
                        stolenScore = Mathf.Clamp(stolenScore, 1, targetScore);

                        targetPoints.Points.Value -= stolenScore;
                        myPoints.Points.Value += stolenScore;

                        Debug.Log($"[Steal Lv.{abilityLevel}] Berhasil mencuri {stolenScore} poin ({stealPercent * 100f:0.#}%) dari {hit.gameObject.name}!");
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

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                cooldownEndServerTime.Value = NetworkManager.Singleton.ServerTime.Time + cooldownTime;
            }

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