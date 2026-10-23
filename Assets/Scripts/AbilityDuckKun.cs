using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityDuckKun : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float cooldownTime = 90f;
    private float lastUseTime = -999f;
    private bool wasOnCooldown = false;

    [Header("Input Settings (Q = Slot 1, E = Slot 2)")]
    public KeyCode assignedKey = KeyCode.Q;
    public KeyCode switchTargetKey = KeyCode.T;

    [Header("Upgrade Settings")]
    [Range(1, 3)] public int abilityLevel = 1;

    private bool targetSelfMode = true; 

    // Nilai default murni 1.0f
    public NetworkVariable<float> scoreMultiplier = new NetworkVariable<float>(
        1.0f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    // Penanda status untuk memastikan buff benar-benar sedang berjalan
    [HideInInspector]
    public bool isBuffActive = false;

    private Coroutine activeAbilityRoutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Sinkronkan level dari AbilityDataHolder saat spawned
        if (IsOwner)
        {
            int savedLevel = AbilityDataHolder.GetLevel("Duck-Kun");
            if (savedLevel > 0)
            {
                abilityLevel = savedLevel;
            }
        }
    }

    public float GetRemainingCooldown()
    {
        float timeSinceLastUse = Time.time - lastUseTime;
        float remaining = cooldownTime - timeSinceLastUse;
        return remaining > 0f ? remaining : 0f;
    }

    public float GetBuffMultiplier()
    {
        switch (abilityLevel)
        {
            case 1: return 1.5f;
            case 2: return 2.2f;
            case 3: return 3.0f;
            default: return 1.5f;
        }
    }

    public float GetDebuffMultiplier()
    {
        switch (abilityLevel)
        {
            case 1: return 0.9f;
            case 2: return 0.7f;
            case 3: return 0.5f;
            default: return 0.9f;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        bool isOnCooldown = Time.time < lastUseTime + cooldownTime;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Duck-Kun] Cooldown selesai! Ability siap dipakai.");
        }
        wasOnCooldown = isOnCooldown;

        // Tombol 'T' untuk mengganti mode target (Buff / Debuff)
        if (Input.GetKeyDown(switchTargetKey))
        {
            targetSelfMode = !targetSelfMode;
            string modeName = targetSelfMode ? "Diri Sendiri (Buff)" : "Musuh Terdekat (Debuff)";
            Debug.Log($"[Duck-Kun] Mode target diubah ke: {modeName}");
        }

        // Tombol Utama (Q atau E) untuk mengeksekusi skill
        if (Input.GetKeyDown(assignedKey))
        {
            if (isOnCooldown)
            {
                Debug.Log("[Duck-Kun] Skill masih cooldown!");
                return;
            }

            TryUseDuckKunServerRpc(targetSelfMode);
        }
    }

    [ServerRpc]
    private void TryUseDuckKunServerRpc(bool isSelf)
    {
        if (Time.time < lastUseTime + cooldownTime) return;

        if (isSelf)
        {
            float multiplier = GetBuffMultiplier();
            float duration = 60f;

            StartDuckKunEffectClientRpc(NetworkObjectId, multiplier, duration, true);
            Debug.Log($"[Duck-Kun] Mengaktifkan Self-Buff! Multiplier: {multiplier}x selama {duration}s");
        }
        else
        {
            GameObject targetEnemy = null;
            float shortestDistance = Mathf.Infinity;
            float range = 10f;

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, range);
            foreach (var hit in hitColliders)
            {
                if (hit.gameObject != gameObject && hit.CompareTag("Player"))
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < shortestDistance)
                    {
                        shortestDistance = dist;
                        targetEnemy = hit.gameObject;
                    }
                }
            }

            if (targetEnemy != null)
            {
                NetworkObject enemyNetObj = targetEnemy.GetComponent<NetworkObject>();
                if (enemyNetObj != null)
                {
                    float multiplier = GetDebuffMultiplier();
                    float duration = 30f;

                    StartDuckKunEffectClientRpc(enemyNetObj.NetworkObjectId, multiplier, duration, false);
                    Debug.Log($"[Duck-Kun] Berhasil mendebuff musuh {targetEnemy.name}! Multiplier: {multiplier}x selama {duration}s");
                }
                else
                {
                    return;
                }
            }
            else
            {
                Debug.Log("[Duck-Kun] Tidak ada musuh dalam jangkauan untuk didebuff!");
                return;
            }
        }

        lastUseTime = Time.time;
        TriggerCooldownClientRpc(Time.time);
    }

    [ClientRpc]
    private void TriggerCooldownClientRpc(float serverTime)
    {
        if (IsOwner)
        {
            lastUseTime = serverTime;
            wasOnCooldown = true;
        }
    }

    [ClientRpc]
    private void StartDuckKunEffectClientRpc(ulong targetNetworkObjectId, float targetMultiplier, float duration, bool isBuff)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj))
        {
            AbilityDuckKun targetDuckKun = targetNetObj.GetComponent<AbilityDuckKun>();
            if (targetDuckKun != null)
            {
                targetDuckKun.ApplyMultiplierRoutine(targetMultiplier, duration);
            }
        }
    }

    public void ApplyMultiplierRoutine(float newMultiplier, float duration)
    {
        if (activeAbilityRoutine != null)
        {
            StopCoroutine(activeAbilityRoutine);
        }
        activeAbilityRoutine = StartCoroutine(MultiplierDurationRoutine(newMultiplier, duration));
    }

    private IEnumerator MultiplierDurationRoutine(float newMultiplier, float duration)
    {
        // Tandai bahwa buff/debuff sedang aktif
        isBuffActive = true;

        if (IsServer)
        {
            scoreMultiplier.Value = newMultiplier;
        }

        yield return new WaitForSeconds(duration);

        if (IsServer)
        {
            scoreMultiplier.Value = 1.0f;
        }

        // Tandai bahwa durasi telah habis
        isBuffActive = false;
        Debug.Log("[Duck-Kun] Efek durasi skill telah habis. Multiplier kembali normal (1.0x).");
    }
}