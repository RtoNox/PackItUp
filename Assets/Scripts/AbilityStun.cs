using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityStun : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Stun";
    public Sprite abilityIcon;
    public float range = 2f;
    public float stunDuration = 5f;
    public float cooldownTime = 30f;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

    [Header("Upgrade Settings (Max Lv 3)")]
    [Range(1, 3)] public int abilityLevel = 1;
    public int maxAbilityLevel = 3;

    [Header("VFX Settings")]
    public GameObject headVfxPrefab;     // Tarik prefab partikel kepala ke sini
    public GameObject bodyLightningPrefab; // Tarik prefab 'Lighting_Stun' ke sini

    private int currentStacks = 1;
    private readonly List<double> activeCooldownEndServerTimes = new List<double>();
    private readonly List<Coroutine> rechargeCoroutines = new List<Coroutine>();
    private readonly List<Coroutine> activeStunCoroutines = new List<Coroutine>();

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

    public int GetMaxStacks()
    {
        return Mathf.Clamp(abilityLevel, 1, maxAbilityLevel);
    }

    public int GetCurrentStacks()
    {
        return currentStacks;
    }

    public void SetUpgradeLevel(int newLevel, bool refillStacks = true)
    {
        abilityLevel = Mathf.Clamp(newLevel, 1, maxAbilityLevel);
        int maxStacks = GetMaxStacks();

        if (refillStacks)
        {
            currentStacks = maxStacks;
        }
        else
        {
            currentStacks = Mathf.Clamp(currentStacks, 0, maxStacks);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        int savedLevel = AbilityDataHolder.GetLevel(abilityName);
        SetUpgradeLevel(savedLevel, true);
    }

    public override void OnNetworkDespawn()
    {
        CleanupStunCoroutines();
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        CleanupStunCoroutines();
    }

    private void CleanupStunCoroutines()
    {
        foreach (var routine in rechargeCoroutines)
        {
            if (routine != null) StopCoroutine(routine);
        }
        rechargeCoroutines.Clear();

        foreach (var routine in activeStunCoroutines)
        {
            if (routine != null) StopCoroutine(routine);
        }
        activeStunCoroutines.Clear();
    }

    public float GetRemainingCooldown()
    {
        double now = GetCurrentTime();
        activeCooldownEndServerTimes.RemoveAll(endTime => now >= endTime);

        if (activeCooldownEndServerTimes.Count == 0) return 0f;

        double earliestEnd = double.MaxValue;
        for (int i = 0; i < activeCooldownEndServerTimes.Count; i++)
        {
            if (activeCooldownEndServerTimes[i] < earliestEnd)
            {
                earliestEnd = activeCooldownEndServerTimes[i];
            }
        }

        float remaining = (float)(earliestEnd - now);
        return remaining > 0f ? remaining : 0f;
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        double now = GetCurrentTime();
        activeCooldownEndServerTimes.RemoveAll(endTime => now >= endTime);

        if (Input.GetKeyDown(assignedKey))
        {
            if (AbilityJammer.IsPlayerJammed(this, "Stun"))
            {
                return;
            }

            if (currentStacks <= 0)
            {
                Debug.Log($"[Stun] Stack habis (0/{GetMaxStacks()}). Sisa cooldown tercepat: {GetRemainingCooldown():0.0}s");
                return;
            }

            TryUseStunServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStunServerRpc()
    {
        if (!IsSpawned) return;

        if (AbilityJammer.IsPlayerJammed(this, "Stun"))
        {
            return;
        }

        if (currentStacks <= 0)
        {
            return;
        }

        GameObject targetPlayer = null;
        float shortestDistance = Mathf.Infinity;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range);
        foreach (var hit in hitColliders)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Player"))
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    targetPlayer = hit.gameObject;
                }
            }
        }

        if (targetPlayer != null)
        {
            NetworkObject targetNetObj = targetPlayer.GetComponent<NetworkObject>();
            if (targetNetObj != null)
            {
                currentStacks = Mathf.Max(0, currentStacks - 1);

                double cooldownEndTime = GetCurrentTime() + cooldownTime;

                // Jalankan cooldown 30s khusus untuk 1 stack yang baru terpakai
                Coroutine rechargeRoutine = StartCoroutine(RechargeOneStackRoutine(cooldownTime));
                rechargeCoroutines.Add(rechargeRoutine);

                // Terapkan stun ke target di semua client
                ApplyStunClientRpc(targetNetObj.NetworkObjectId, stunDuration);

                // Sinkronkan jumlah stack & waktu selesai cooldown stack terpakai ke owner
                SyncStackAndCooldownClientRpc(currentStacks, cooldownEndTime);

                Debug.Log($"[Stun Lv.{abilityLevel}] Berhasil men-stun {targetPlayer.name} selama {stunDuration}s! Sisa stack: {currentStacks}/{GetMaxStacks()}");
                return;
            }
        }

        Debug.Log("[Stun] Tidak ada target player di dekatmu! Stack tidak berkurang.");
    }

    private IEnumerator RechargeOneStackRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        int maxStacks = GetMaxStacks();
        if (currentStacks < maxStacks)
        {
            currentStacks++;
        }

        SyncStackAndCooldownClientRpc(currentStacks, -1.0);
    }

    [ClientRpc]
    private void ApplyStunClientRpc(ulong targetNetworkObjectId, float duration)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj))
        {
            Coroutine stunRoutine = StartCoroutine(StunRoutine(targetNetObj.gameObject, duration));
            activeStunCoroutines.Add(stunRoutine);
        }
    }

    [ClientRpc]
    private void SyncStackAndCooldownClientRpc(int newStacks, double addedCooldownEndServerTime)
    {
        if (!IsOwner) return;

        currentStacks = newStacks;

        if (addedCooldownEndServerTime > 0.0)
        {
            activeCooldownEndServerTimes.Add(addedCooldownEndServerTime);
        }
    }

    private IEnumerator StunRoutine(GameObject target, float duration)
    {
        // 1. Matikan movement player target
        MonoBehaviour playerMovement = target.GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        List<GameObject> spawnedVfxList = new List<GameObject>();

        // 2. Munculkan VFX di Kepala
        if (headVfxPrefab != null)
        {
            Vector3 headPosition = target.transform.position + new Vector3(0, 2.0f, 0);
            GameObject headVfx = Instantiate(headVfxPrefab, headPosition, Quaternion.identity, target.transform);
            spawnedVfxList.Add(headVfx);
        }

        // 3. Munculkan VFX Petir di Area Badan
        if (bodyLightningPrefab != null)
        {
            Vector3 bodyPosition = target.transform.position + new Vector3(0, 1.0f, 0);
            GameObject bodyVfx = Instantiate(bodyLightningPrefab, bodyPosition, Quaternion.identity, target.transform);
            spawnedVfxList.Add(bodyVfx);
        }

        Debug.Log($"[Stun] Player {target.name} sedang terkena Stun!");

        yield return new WaitForSeconds(duration);

        // 4. Hancurkan semua VFX setelah durasi stun selesai
        foreach (GameObject vfx in spawnedVfxList)
        {
            if (vfx != null)
            {
                Destroy(vfx);
            }
        }

        // 5. Nyalakan kembali movement player target
        if (playerMovement != null) playerMovement.enabled = true;

        Debug.Log($"[Stun] Efek Stun pada {target.name} telah selesai.");
    }
}