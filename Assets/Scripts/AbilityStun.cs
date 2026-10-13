using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityStun : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float range = 5f;
    public float stunDuration = 3f;
    public float cooldownTime = 25f;
    private float lastUseTime = -999f;
    private bool wasOnCooldown = false;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

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
            Debug.Log("[Stun] Cooldown selesai! Ability Stun siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (isOnCooldown)
            {
                Debug.Log("[Stun] Skill masih cooldown!");
                return;
            }

            TryUseStunServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStunServerRpc()
    {
        if (Time.time < lastUseTime + cooldownTime)
        {
            return;
        }

        bool success = false;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range);
        
        foreach (var hit in hitColliders)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Player"))
            {
                Debug.Log($"[Stun] Berhasil men-stun player target ({hit.gameObject.name}) selama {stunDuration} detik!");
                success = true;
                break;
            }
        }

        if (success)
        {
            lastUseTime = Time.time;
            TriggerCooldownClientRpc(Time.time);
        }
        else
        {
            Debug.Log("[Stun] Tidak ada target player di dekatmu! Skill tidak masuk cooldown.");
        }
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
}