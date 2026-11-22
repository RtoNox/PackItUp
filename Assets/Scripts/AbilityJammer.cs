using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityJammer : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Jammer";
    public Sprite abilityIcon;
    public float cooldownTime = 40f;
    public float maxDuration = 10f;

    [Header("Jammer Target Settings")]
    public bool jamAllEnemies = true;
    public float jamRadius = 15f;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

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

        if (IsServer)
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
        if (Time.time < lastUseTime + cooldownTime) return;

        lastUseTime = Time.time;
        TriggerCooldownClientRpc(cooldownTime);

        if (activeJammerCoroutine != null)
            StopCoroutine(activeJammerCoroutine);

        activeJammerCoroutine = StartCoroutine(JammerRoutine());
    }

    private IEnumerator JammerRoutine()
    {
        Debug.Log($"[Jammer] Aktif selama {maxDuration} detik!");
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

        yield return new WaitForSeconds(maxDuration);

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