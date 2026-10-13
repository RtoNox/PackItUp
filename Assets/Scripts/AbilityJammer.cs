using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityJammer : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float jammerRadius = 6f;
    public float jammerDuration = 4f;
    public float cooldownTime = 35f;
    private float lastUseTime = -999f;
    private bool wasOnCooldown = false;

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
            Debug.Log("[Jammer] Cooldown selesai! Ability Jammer siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (isOnCooldown)
            {
                Debug.Log("[Jammer] Skill masih cooldown!");
                return;
            }

            TryUseJammerServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseJammerServerRpc()
    {
        if (Time.time < lastUseTime + cooldownTime)
        {
            return;
        }

        bool success = false;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, jammerRadius);
        
        foreach (var hit in hitColliders)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Player"))
            {
                Debug.Log($"[Jammer] Berhasil melakukan jammer pada area sekitar target ({hit.gameObject.name}) selama {jammerDuration} detik!");
                success = true;
                
            }
        }

        if (success)
        {
            lastUseTime = Time.time;
            TriggerCooldownClientRpc(Time.time);
        }
        else
        {
            Debug.Log("[Jammer] Tidak ada player lain dalam jangkauan! Skill tidak masuk cooldown.");
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