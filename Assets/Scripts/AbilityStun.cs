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
    private readonly List<float> activeCooldownEndTimes = new List<float>();

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

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

        if (IsOwner)
        {
            SyncStackAndCooldownClientRpc(currentStacks, 0f);
        }
    }

    public float GetRemainingCooldown()
    {
        activeCooldownEndTimes.RemoveAll(endTime => Time.time >= endTime);
        if (activeCooldownEndTimes.Count == 0) return 0f;

        float earliestEnd = float.MaxValue;
        for (int i = 0; i < activeCooldownEndTimes.Count; i++)
        {
            if (activeCooldownEndTimes[i] < earliestEnd)
            {
                earliestEnd = activeCooldownEndTimes[i];
            }
        }

        float remaining = earliestEnd - Time.time;
        return remaining > 0f ? remaining : 0f;
    }

    void Update()
    {
        if (!IsOwner) return;

        activeCooldownEndTimes.RemoveAll(endTime => Time.time >= endTime);

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

                // Jalankan cooldown 30s khusus untuk 1 stack yang baru terpakai
                StartCoroutine(RechargeOneStackRoutine(cooldownTime));

                // Terapkan stun ke target di semua client
                ApplyStunClientRpc(targetNetObj.NetworkObjectId, stunDuration);

                // Sinkronkan jumlah stack & waktu cooldown stack terpakai ke owner
                SyncStackAndCooldownClientRpc(currentStacks, cooldownTime);

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

        SyncStackAndCooldownClientRpc(currentStacks, 0f);
    }

    [ClientRpc]
    private void ApplyStunClientRpc(ulong targetNetworkObjectId, float duration)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj))
        {
            StartCoroutine(StunRoutine(targetNetObj.gameObject, duration));
        }
    }

    [ClientRpc]
    private void SyncStackAndCooldownClientRpc(int newStacks, float addedCooldownDuration)
    {
        if (!IsOwner) return;

        currentStacks = newStacks;

        if (addedCooldownDuration > 0f)
        {
            activeCooldownEndTimes.Add(Time.time + addedCooldownDuration);
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