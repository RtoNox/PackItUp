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

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

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

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

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
        Transform myIndicator = FindDeepChild(transform, targetIndicatorName);
        if (myIndicator != null)
        {
            myIndicator.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        bool isOnCooldown = GetRemainingCooldown() > 0f;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Duck-Kun] Cooldown selesai! Ability Duck-Kun siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
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
        RequestDuckKunServerRpc(targetNetworkObjectId);
    }

    [ServerRpc]
    private void RequestDuckKunServerRpc(ulong targetNetworkObjectId)
    {
        if (Time.time < lastUseTime + cooldownTime) return;

        lastUseTime = Time.time;

        ApplyDuckKunClientRpc(targetNetworkObjectId, NetworkObjectId);
        TriggerCooldownClientRpc(cooldownTime);
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
    private void ApplyDuckKunClientRpc(ulong targetNetObjId, ulong casterNetObjId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out NetworkObject targetNetObj))
        {
            bool isSelf = (targetNetObjId == casterNetObjId);
            StartCoroutine(DuckKunRoutine(targetNetObj.gameObject, isSelf, targetNetObjId));
        }
    }

    private IEnumerator DuckKunRoutine(GameObject target, bool isSelf, ulong targetNetObjId)
    {
        float duration = isSelf ? selfBuffDuration : enemyDebuffDuration;
        float multiplierValue = isSelf ? selfMultiplier : enemyMultiplier;

        string effectType = isSelf ? "BUFF (Self)" : "DEBUFF (Enemy)";
        Debug.Log($"[Duck-Kun] {target.name} terkena {effectType}! Multiplier diubah menjadi {multiplierValue} selama {duration} detik.");

        AbilityDuckKun targetAbility = target.GetComponent<AbilityDuckKun>();

        if (IsServer && targetAbility != null)
        {
            targetAbility.isBuffActive.Value = true;
            targetAbility.scoreMultiplier.Value = multiplierValue;
        }

        ToggleTargetIndicatorClientRpc(targetNetObjId, true);

        yield return new WaitForSeconds(duration);

        ToggleTargetIndicatorClientRpc(targetNetObjId, false);

        if (IsServer && targetAbility != null)
        {
            targetAbility.scoreMultiplier.Value = 1.0f;
            targetAbility.isBuffActive.Value = false;
        }

        Debug.Log($"[Duck-Kun] Efek pada {target.name} telah habis.");
    }

    [ClientRpc]
    private void ToggleTargetIndicatorClientRpc(ulong targetNetworkObjectId, bool isActive)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj))
        {
            Transform indicator = FindDeepChild(targetNetObj.transform, targetIndicatorName);
            if (indicator != null)
            {
                indicator.gameObject.SetActive(isActive);
            }
            else
            {
                Debug.LogWarning($"[Duck-Kun] Objek '{targetIndicatorName}' tidak ditemukan pada prefab target!");
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