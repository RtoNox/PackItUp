using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityWarp : NetworkBehaviour
{
    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.F;

    [Header("Upgrade Settings")]
    [Range(1, 3)] public int abilityLevel = 1;

    private float lastUseTime = -999f;

    public float GetWarpDistance()
    {
        switch (abilityLevel)
        {
            case 1: return 12f;
            case 2: return 18f;
            case 3: return 25f;
            default: return 12f;
        }
    }

    public float GetCooldownTime()
    {
        switch (abilityLevel)
        {
            case 1: return 20f;
            case 2: return 16f;
            case 3: return 12f;
            default: return 20f;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(assignedKey))
        {
            TryUseWarpServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseWarpServerRpc()
    {
        float cooldown = GetCooldownTime();

        if (Time.time < lastUseTime + cooldown)
        {
            Debug.Log("[Warp] Skill masih cooldown!");
            return;
        }

        float distance = GetWarpDistance();
        
        Vector3 warpDirection = transform.forward;
        warpDirection.y = 0;
        warpDirection.Normalize();

        if (warpDirection == Vector3.zero)
        {
            warpDirection = transform.forward;
        }

        Vector3 targetPosition = transform.position + (warpDirection * distance);

        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        if (Physics.Raycast(rayOrigin, warpDirection, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            targetPosition = hit.point - (warpDirection * 1.5f);
            targetPosition.y = transform.position.y;
        }

        CharacterController charController = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        if (charController != null)
        {
            charController.enabled = false;
            transform.position = targetPosition;
            charController.enabled = true;
        }
        else if (rb != null)
        {
            rb.position = targetPosition;
            rb.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }

        lastUseTime = Time.time;
        Debug.Log($"[Warp] Berhasil teleport sejauh {distance} meter ke posisi: {targetPosition}");
    }

    public void UpgradeSkill()
    {
        if (abilityLevel < 3)
        {
            abilityLevel++;
            Debug.Log("[Warp] Skill diupgrade ke level: " + abilityLevel);
        }
    }
}