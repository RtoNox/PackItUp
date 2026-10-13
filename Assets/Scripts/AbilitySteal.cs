using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilitySteal : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float range = 2f;
    public float cooldownTime = 30f;
    private float lastUseTime = -999f;
    private bool wasOnCooldown = false;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.F;

    [Header("Upgrade Settings")]
    [Range(1, 3)] public int abilityLevel = 1;

    public float GetStealPercentage()
    {
        switch (abilityLevel)
        {
            case 1: return 0.10f;
            case 2: return 0.25f;
            case 3: return 0.40f;
            default: return 0.10f;
        }
    }

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
            Debug.Log("[Steal] Cooldown selesai! Ability Steal siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (isOnCooldown)
            {
                Debug.Log("Ability Steal masih cooldown!");
                return;
            }

            TryUseStealServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStealServerRpc()
    {
        // Proteksi tambahan di server
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
                PlayerPoints targetPoints = hit.GetComponent<PlayerPoints>();
                PlayerPoints myPoints = GetComponent<PlayerPoints>();

                if (targetPoints != null && myPoints != null)
                {
                    int targetScore = targetPoints.Points.Value; 
                    if (targetScore > 0)
                    {
                        int stolenScore = Mathf.RoundToInt(targetScore * GetStealPercentage());
                        
                        targetPoints.Points.Value -= stolenScore;
                        myPoints.Points.Value += stolenScore;

                        Debug.Log($"[Steal] Berhasil mencuri {stolenScore} poin!");
                        success = true;
                        break;
                    }
                }
            }
        }

        // Cooldown HANYA aktif jika skill berhasil mengenai target dan mencuri poin
        if (success)
        {
            lastUseTime = Time.time;
            
            TriggerCooldownClientRpc(Time.time);
        }
        else
        {
            Debug.Log("[Steal] Tidak ada target valid di sekitar! Skill tidak masuk cooldown.");
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