using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityDuckKun : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Duck-Kun";
    public Sprite abilityIcon;
    public float cooldownTime = 90f;

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

    [Header("Buff/Debuff Settings")]
    public float selfBuffDuration = 60f;
    public float selfMultiplier = 2.0f;

    public float enemyDebuffDuration = 30f;
    public float enemyMultiplier = 0.7f;

    [Header("Network Buff Status")]
    public NetworkVariable<bool> isBuffActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> scoreMultiplier = new NetworkVariable<float>(
        1.0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Targeting Settings")]
    [Tooltip("Nama objek indikator di dalam prefab Player")]
    public string targetIndicatorName = "Duck-KunIndicator";

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.Q;

    private readonly List<Coroutine> activeEffectCoroutines = new List<Coroutine>();
    private Coroutine serverBuffCoroutine;

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

    public void SetUpgradeLevel(int newLevel)
    {
        abilityLevel = Mathf.Clamp(newLevel, 1, maxAbilityLevel);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        abilityLevel = Mathf.Clamp(AbilityDataHolder.GetLevel(abilityName), 1, maxAbilityLevel);

        Transform myIndicator = FindDeepChild(transform, targetIndicatorName);
        if (myIndicator != null)
        {
            myIndicator.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        CleanupDuckKun();
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        CleanupDuckKun();
    }

    private void CleanupDuckKun()
    {
        foreach (var routine in activeEffectCoroutines)
        {
            if (routine != null) StopCoroutine(routine);
        }
        activeEffectCoroutines.Clear();

        if (serverBuffCoroutine != null)
        {
            StopCoroutine(serverBuffCoroutine);
            serverBuffCoroutine = null;
        }

        Transform myIndicator = FindDeepChild(transform, targetIndicatorName);
        if (myIndicator != null)
        {
            myIndicator.gameObject.SetActive(false);
        }

        if (IsServer)
        {
            scoreMultiplier.Value = 1.0f;
            isBuffActive.Value = false;
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
            Debug.Log("[Duck-Kun] Cooldown selesai! Ability Duck-Kun siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (AbilityJammer.IsPlayerJammed(this, "Duck-Kun"))
            {
                return;
            }

            if (isOnCooldown)
            {
                Debug.Log("[Duck-Kun] Skill masih cooldown!");
                return;
            }

            if (DuckKunUIController.Instance != null)
            {
                DuckKunUIController.Instance.Init(this);
                DuckKunUIController.Instance.OpenPanel();
            }
            else
            {
                Debug.LogWarning("[Duck-Kun] DuckKunUIController Instance tidak ditemukan di Scene!");
            }
        }
    }

    public void OnTargetSelected(ulong targetNetworkObjectId)
    {
        if (!IsOwner || !IsSpawned) return;

        if (AbilityJammer.IsPlayerJammed(this, "Duck-Kun"))
        {
            return;
        }

        RequestDuckKunServerRpc(targetNetworkObjectId);
    }

    [ServerRpc]
    private void RequestDuckKunServerRpc(ulong targetNetworkObjectId)
    {
        if (!IsSpawned) return;

        if (AbilityJammer.IsPlayerJammed(this, "Duck-Kun"))
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

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj))
        {
            Debug.LogWarning("[Duck-Kun] Target tidak ditemukan di SpawnedObjects!");
            return;
        }

        lastUseTime = Time.time;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            cooldownEndServerTime.Value = NetworkManager.Singleton.ServerTime.Time + cooldownTime;
        }

        bool isSelf = (targetNetworkObjectId == NetworkObjectId);
        float duration = isSelf ? selfBuffDuration : enemyDebuffDuration;
        float multiplierValue = isSelf ? selfMultiplier : enemyMultiplier;

        AbilityDuckKun targetAbility = targetNetObj.GetComponent<AbilityDuckKun>();
        if (targetAbility != null)
        {
            if (targetAbility.serverBuffCoroutine != null)
            {
                targetAbility.StopCoroutine(targetAbility.serverBuffCoroutine);
            }
            targetAbility.serverBuffCoroutine = targetAbility.StartCoroutine(
                targetAbility.ServerBuffTimerRoutine(multiplierValue, duration, isSelf)
            );
        }

        ApplyDuckKunClientRpc(targetNetworkObjectId, NetworkObjectId, duration, multiplierValue, isSelf);
        TriggerCooldownClientRpc(cooldownTime);
    }

    private IEnumerator ServerBuffTimerRoutine(float multiplierValue, float duration, bool isSelf)
    {
        string effectType = isSelf ? "BUFF (Self)" : "DEBUFF (Enemy)";
        Debug.Log($"[Duck-Kun Lv.{abilityLevel}] {gameObject.name} terkena {effectType}! Multiplier={multiplierValue} selama {duration}s.");

        isBuffActive.Value = true;
        scoreMultiplier.Value = multiplierValue;

        yield return new WaitForSeconds(duration);

        scoreMultiplier.Value = 1.0f;
        isBuffActive.Value = false;
        serverBuffCoroutine = null;

        Debug.Log($"[Duck-Kun] Efek pada {gameObject.name} telah habis.");
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
    private void ApplyDuckKunClientRpc(ulong targetNetObjId, ulong casterNetObjId, float duration, float multiplierValue, bool isSelf)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out NetworkObject targetNetObj))
        {
            Coroutine routine = StartCoroutine(DuckKunVisualRoutine(targetNetObj, isSelf, duration, multiplierValue));
            activeEffectCoroutines.Add(routine);
        }
    }

    private IEnumerator DuckKunVisualRoutine(NetworkObject targetNetObj, bool isSelf, float duration, float multiplierValue)
    {
        string effectType = isSelf ? "BUFF (Self)" : "DEBUFF (Enemy)";
        Debug.Log($"[Duck-Kun] Visual {effectType} pada {targetNetObj.gameObject.name} (Multiplier={multiplierValue}, Durasi={duration}s).");

        Transform indicator = FindDeepChild(targetNetObj.transform, targetIndicatorName);
        if (indicator != null)
        {
            indicator.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[Duck-Kun] Objek '{targetIndicatorName}' tidak ditemukan pada prefab target!");
        }

        yield return new WaitForSeconds(duration);

        if (targetNetObj != null)
        {
            Transform endIndicator = FindDeepChild(targetNetObj.transform, targetIndicatorName);
            if (endIndicator != null)
            {
                endIndicator.gameObject.SetActive(false);
            }
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }
}