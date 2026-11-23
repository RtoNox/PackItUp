using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityMarionette : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Marionette";
    public Sprite abilityIcon;
    public float cooldownTime = 60f;
    public float maxDuration = 30f;

    [Header("Upgrade Settings (Max Lv 3)")]
    [Range(1, 3)] public int abilityLevel = 1;
    public int maxAbilityLevel = 3;
    [Tooltip("Penambahan multiplier skor per level (0.05 = +5%)")]
    public float scoreBonusPerLevel = 0.05f;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;
    private Coroutine activeMarionetteRoutine;
    private PlayerPoints activeTargetPoints;

    private readonly NetworkVariable<double> cooldownEndServerTime = new NetworkVariable<double>(
        0.0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Line of Sight Settings")]
    public LayerMask obstacleLayer;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.Q;

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

    /// <summary>
    /// Mengembalikan multiplier skor berdasarkan level Marionette (Lv 1 = 1.00x, Lv 2 = 1.05x, Lv 3 = 1.10x).
    /// </summary>
    public float GetScoreMultiplier()
    {
        int clampedLevel = Mathf.Clamp(abilityLevel, 1, maxAbilityLevel);
        return 1f + (clampedLevel - 1) * scoreBonusPerLevel;
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
        Debug.Log($"[Marionette] OnNetworkSpawn | IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, Lv={abilityLevel}");
    }

    public override void OnNetworkDespawn()
    {
        CleanupMarionette();
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        CleanupMarionette();
    }

    private void CleanupMarionette()
    {
        if (activeMarionetteRoutine != null)
        {
            StopCoroutine(activeMarionetteRoutine);
            activeMarionetteRoutine = null;
        }

        if (IsServer && activeTargetPoints != null)
        {
            activeTargetPoints.marionetteCasterRef.Value = default;
            activeTargetPoints = null;
        }
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
            Debug.Log("[Marionette] Cooldown selesai! Ability Marionette siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            Debug.Log($"[Marionette] Input {assignedKey} ditekan oleh OwnerClientId={OwnerClientId} | isOnCooldown={isOnCooldown}");

            if (AbilityJammer.IsPlayerJammed(this, "Marionette"))
            {
                return;
            }

            if (isOnCooldown)
            {
                Debug.Log($"[Marionette] Skill masih cooldown! Sisa: {GetRemainingCooldown():0.0}s");
                return;
            }

            if (DuckKunUIController.Instance != null)
            {
                DuckKunUIController.Instance.InitMarionette(this);
                DuckKunUIController.Instance.OpenPanel();
            }
            else
            {
                Debug.LogWarning("[Marionette] DuckKunUIController Instance tidak ditemukan!");
            }
        }
    }

    public void OnTargetSelected(ulong targetNetworkObjectId)
    {
        if (!IsOwner || !IsSpawned) return;

        if (AbilityJammer.IsPlayerJammed(this, "Marionette"))
        {
            return;
        }

        TryUseMarionetteServerRpc(targetNetworkObjectId);
    }

    [ServerRpc]
    private void TryUseMarionetteServerRpc(ulong targetNetworkObjectId)
    {
        if (!IsSpawned) return;

        if (AbilityJammer.IsPlayerJammed(this, "Marionette"))
        {
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.ServerTime.Time < cooldownEndServerTime.Value)
                return;
        }
        else if (Time.time < lastUseTime + cooldownTime)
        {
            return;
        }

        NetworkObject targetNetObj = null;

        if (targetNetworkObjectId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out targetNetObj))
        {
            if (targetNetObj.gameObject == gameObject)
            {
                Debug.Log("[Marionette] Tidak bisa menargetkan diri sendiri!");
                return;
            }
        }

        if (targetNetObj != null && targetNetObj.gameObject != gameObject)
        {
            lastUseTime = Time.time;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                cooldownEndServerTime.Value = NetworkManager.Singleton.ServerTime.Time + cooldownTime;
            }

            TriggerCooldownClientRpc(cooldownTime);

            if (activeMarionetteRoutine != null)
            {
                StopCoroutine(activeMarionetteRoutine);
            }

            activeMarionetteRoutine = StartCoroutine(MarionetteRoutine(gameObject, targetNetObj.gameObject));
        }
        else
        {
            Debug.Log("[Marionette] Target tidak valid!");
        }
    }

    private IEnumerator MarionetteRoutine(GameObject caster, GameObject target)
    {
        float multiplier = GetScoreMultiplier();
        Debug.Log($"[Marionette Lv.{abilityLevel}] Mengaitkan ke target: {target.name} (Durasi: {maxDuration}s, Multiplier: {multiplier:0.00}x).");

        PlayerPoints targetPoints = target.GetComponent<PlayerPoints>();
        NetworkObject casterNetObj = caster.GetComponent<NetworkObject>();

        if (targetPoints != null && casterNetObj != null && IsServer)
        {
            activeTargetPoints = targetPoints;
            targetPoints.marionetteCasterRef.Value = new NetworkObjectReference(casterNetObj);
        }

        float timer = 0f;

        while (timer < maxDuration)
        {
            if (IsSeenByOtherPlayers(caster, target))
            {
                Debug.Log("[Marionette] KETAHUAN! Target terlihat oleh pemain lain. Skill dibatalkan otomatis!");
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (targetPoints != null && IsServer)
        {
            targetPoints.marionetteCasterRef.Value = default;
        }

        if (activeTargetPoints == targetPoints)
        {
            activeTargetPoints = null;
        }

        activeMarionetteRoutine = null;
        Debug.Log("[Marionette] Sesi Marionette berakhir.");
    }

    private bool IsSeenByOtherPlayers(GameObject caster, GameObject target)
    {
        var spawnedObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

        foreach (var kvp in spawnedObjects)
        {
            NetworkObject netObj = kvp.Value;
            GameObject observer = netObj.gameObject;

            if (observer == caster || observer == target) continue;
            if (!observer.CompareTag("Player")) continue;

            Vector3 observerPos = observer.transform.position + Vector3.up * 1.5f;
            Vector3 targetPos = target.transform.position + Vector3.up * 1.0f;

            if (!Physics.Linecast(observerPos, targetPos, obstacleLayer))
            {
                return true;
            }
        }

        return false;
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
}