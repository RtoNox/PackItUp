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

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(assignedKey))
        {
            TryUseStealServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStealServerRpc()
    {
        if (Time.time < lastUseTime + cooldownTime)
        {
            Debug.Log("Ability Steal masih cooldown!");
            return;
        }

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

                        lastUseTime = Time.time;
                        Debug.Log($"[Steal] Berhasil mencuri {stolenScore} poin!");
                        break;
                    }
                }
            }
        }
    }
}