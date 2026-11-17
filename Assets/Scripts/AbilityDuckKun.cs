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

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.Q; 

    public float GetRemainingCooldown()
    {
        float timeSinceLastUse = Time.time - lastUseTime;
        float remaining = cooldownTime - timeSinceLastUse;
        return remaining > 0f ? remaining : 0f;
    }

    void Update()
    {
        if (!IsOwner) return;

        bool isOnCooldown = Time.time < lastUseTime + cooldownTime;

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
    private void ApplyDuckKunClientRpc(ulong targetNetObjId, ulong casterNetObjId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out NetworkObject targetNetObj))
        {
            bool isSelf = (targetNetObjId == casterNetObjId);
            StartCoroutine(DuckKunRoutine(targetNetObj.gameObject, isSelf));
        }
    }

    private IEnumerator DuckKunRoutine(GameObject target, bool isSelf)
    {
        float duration = isSelf ? selfBuffDuration : enemyDebuffDuration;
        float multiplierValue = isSelf ? selfMultiplier : enemyMultiplier;

        string effectType = isSelf ? "BUFF (Self)" : "DEBUFF (Enemy)";
        Debug.Log($"[Duck-Kun] {target.name} terkena {effectType}! Multiplier diubah menjadi {multiplierValue} selama {duration} detik.");

        // Ambil komponen AbilityDuckKun milik TARGET yang dipilih (bukan caster)
        AbilityDuckKun targetAbility = target.GetComponent<AbilityDuckKun>();

        if (IsServer && targetAbility != null)
        {
            targetAbility.isBuffActive.Value = true;
            targetAbility.scoreMultiplier.Value = multiplierValue;
        }

        yield return new WaitForSeconds(duration);

        if (IsServer && targetAbility != null)
        {
            targetAbility.scoreMultiplier.Value = 1.0f;
            targetAbility.isBuffActive.Value = false;
        }

        Debug.Log($"[Duck-Kun] Efek pada {target.name} telah habis.");
    }
}