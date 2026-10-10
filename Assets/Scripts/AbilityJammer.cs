using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityJammer : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float radius = 3.5f;
    public float cooldownTime = 35f;
    private float lastUseTime = -999f;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.R;

    [Header("Upgrade Settings")]
    [Range(1, 3)] public int abilityLevel = 1;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(assignedKey))
        {
            TryUseJammerServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseJammerServerRpc()
    {
        if (Time.time < lastUseTime + cooldownTime)
        {
            Debug.Log("Ability Jammer masih cooldown!");
            return;
        }

        lastUseTime = Time.time;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        int affectedCount = 0;

        foreach (var hit in hitColliders)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Player"))
            {
                affectedCount++;
            }
        }
        
        Debug.Log($"[Jammer] Berhasil diaktifkan. Mempengaruhi {affectedCount} musuh.");
    }
}