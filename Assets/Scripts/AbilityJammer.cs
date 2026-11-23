using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityJammer : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Jammer";
    public Sprite abilityIcon;
    public float cooldownTime = 60f;
    public float baseDuration = 10f;
    public float durationPerLevel = 2f;

    [Header("Jammer Target Settings")]
    public bool jamAllEnemies = false;
    public float jamRadius = 6f;

    [Header("Upgrade Settings (Max Lv 3)")]
    [Range(1, 3)] public int abilityLevel = 1;
    public int maxAbilityLevel = 3;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

    private readonly NetworkVariable<double> cooldownEndServerTime = new NetworkVariable<double>(
        0.0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Visual Indicator & VFX")]
    public GameObject indicatorXObject;
    public GameObject jammerVfxPrefab;
    private GameObject spawnedVfx;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

    private Coroutine activeJammerCoroutine;

    // Menyimpan ID player yang sedang terkena Jammer dari instance Jammer mana pun
    private static readonly HashSet<ulong> activeJammedPlayerIds = new HashSet<ulong>();
    private readonly List<ulong> currentAffectedIds = new List<ulong>();

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

    private double GetCurrentTime()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            return NetworkManager.Singleton.ServerTime.Time;
        }
        return Time.time;
    }

    public float GetJammerDuration()
    {
        int clampedLevel = Mathf.Clamp(abilityLevel, 1, maxAbilityLevel);
        return baseDuration + (clampedLevel - 1) * durationPerLevel;
    }

    public void SetUpgradeLevel(int newLevel)
    {
        abilityLevel = Mathf.Clamp(newLevel, 1, maxAbilityLevel);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        int savedLevel = AbilityDataHolder.GetLevel(abilityName);
        SetUpgradeLevel(savedLevel);
        Debug.Log($"[Jammer] OnNetworkSpawn | IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, Lv={abilityLevel}");
    }

    // Dipanggil dari script ability lain untuk cek apakah player ini sedang di-jam
    public static bool IsPlayerJammed(Component abilityComponent, string abilityLabel)
    {
        if (abilityComponent == null) return false;

        NetworkObject netObj = abilityComponent.GetComponent<NetworkObject>();
        if (netObj != null && activeJammedPlayerIds.Contains(netObj.NetworkObjectId))
        {
            Debug.Log($"[{abilityLabel}] Diblokir! Sedang terkena efek Jammer.");
            return true;
        }
        return false;
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

    private void Start()
    {
        if (indicatorXObject != null)
            indicatorXObject.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        CleanupJammer();
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        CleanupJammer();
    }

    private void CleanupJammer()
    {
        if (activeJammerCoroutine != null)
        {
            StopCoroutine(activeJammerCoroutine);
            activeJammerCoroutine = null;
        }

        if (indicatorXObject != null)
            indicatorXObject.SetActive(false);

        if (spawnedVfx != null)
        {
            Destroy(spawnedVfx);
            spawnedVfx = null;
        }

        if (IsServer && IsSpawned)
        {
            SyncJammedPlayersClientRpc(currentAffectedIds.ToArray(), false);
            currentAffectedIds.Clear();
        }
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        bool isOnCooldown = GetRemainingCooldown() > 0f;
        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Jammer] Cooldown selesai! Ability Jammer siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            Debug.Log($"[Jammer] Input {assignedKey} ditekan oleh OwnerClientId={OwnerClientId} | isOnCooldown={isOnCooldown}");

            if (AbilityJammer.IsPlayerJammed(this, "Jammer")) return;

            if (isOnCooldown)
            {
                Debug.Log($"[Jammer] Skill masih cooldown! Sisa: {GetRemainingCooldown():0.0}s");
                return;
            }

            TryUseJammerServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseJammerServerRpc()
    {
        if (!IsSpawned) return;
        if (AbilityJammer.IsPlayerJammed(this, "Jammer")) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.ServerTime.Time < cooldownEndServerTime.Value)
                return;
        }
        else if (Time.time < lastUseTime + cooldownTime)
        {
            return;
        }

        lastUseTime = Time.time;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            cooldownEndServerTime.Value = NetworkManager.Singleton.ServerTime.Time + cooldownTime;
        }

        TriggerCooldownClientRpc(cooldownTime);

        if (activeJammerCoroutine != null)
            StopCoroutine(activeJammerCoroutine);

        activeJammerCoroutine = StartCoroutine(JammerRoutine());
    }

    private IEnumerator JammerRoutine()
    {
        float activeDuration = GetJammerDuration();
        Debug.Log($"[Jammer Lv.{abilityLevel}] Aktif selama {activeDuration:0.#} detik (Radius: {jamRadius} unit)!");
        ToggleJammerEffectClientRpc(true);

        List<ulong> targets = new List<ulong>();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                NetworkObject netObj = kvp.Value;
                if (netObj == null || netObj == NetworkObject) continue;
                if (!netObj.CompareTag("Player")) continue;

                if (!jamAllEnemies)
                {
                    float dist = Vector3.Distance(transform.position, netObj.transform.position);
                    if (dist > jamRadius) continue;
                }

                targets.Add(netObj.NetworkObjectId);
            }
        }

        currentAffectedIds.Clear();
        currentAffectedIds.AddRange(targets);
        SyncJammedPlayersClientRpc(currentAffectedIds.ToArray(), true);

        yield return new WaitForSeconds(activeDuration);

        SyncJammedPlayersClientRpc(currentAffectedIds.ToArray(), false);
        currentAffectedIds.Clear();
        ToggleJammerEffectClientRpc(false);
        activeJammerCoroutine = null;
        Debug.Log("[Jammer] Sesi Jammer berakhir.");
    }

    [ClientRpc]
    private void SyncJammedPlayersClientRpc(ulong[] targetIds, bool jammed)
    {
        if (targetIds == null) return;
        foreach (ulong id in targetIds)
        {
            if (jammed)
                activeJammedPlayerIds.Add(id);
            else
                activeJammedPlayerIds.Remove(id);
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
    private void ToggleJammerEffectClientRpc(bool isActive)
    {
        if (indicatorXObject != null)
            indicatorXObject.SetActive(isActive);

        if (isActive)
        {
            if (jammerVfxPrefab != null && spawnedVfx == null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 2.0f;
                spawnedVfx = Instantiate(jammerVfxPrefab, spawnPos, Quaternion.identity, transform);
            }
        }
        else
        {
            if (spawnedVfx != null)
            {
                Destroy(spawnedVfx);
                spawnedVfx = null;
            }
        }
    }
}