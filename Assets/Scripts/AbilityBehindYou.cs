using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityBehindYou : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float cooldownTime = 40f;
    private float lastUseTime = -999f;
    private bool wasOnCooldown = false;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.R; 

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
            Debug.Log("[BehindYou] Cooldown selesai! Ability Behind You siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (isOnCooldown)
            {
                Debug.Log("[BehindYou] Skill masih cooldown!");
                return;
            }

            TryUseBehindYouServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseBehindYouServerRpc()
    {
        if (Time.time < lastUseTime + cooldownTime)
        {
            return;
        }

        bool success = false;
        GameObject targetPlayer = null;
        float shortestDistance = Mathf.Infinity;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 20f); // Radius pencarian target
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
            Vector3 targetPosition = targetPlayer.transform.position - (targetPlayer.transform.forward * 1.5f);
            targetPosition.y = targetPlayer.transform.position.y;

            CharacterController charController = GetComponent<CharacterController>();
            if (charController != null) charController.enabled = false;

            transform.position = targetPosition;
            transform.LookAt(targetPlayer.transform);

            if (charController != null) charController.enabled = true;

            Debug.Log($"[BehindYou] Berhasil berpindah ke belakang player {targetPlayer.name}!");
            success = true;
        }

        if (success)
        {
            lastUseTime = Time.time;
            TriggerCooldownClientRpc(Time.time);
        }
        else
        {
            Debug.Log("[BehindYou] Tidak ada target player lain di dekat Anda! Skill tidak masuk cooldown.");
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