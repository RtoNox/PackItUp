using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityStun : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float range = 2f;
    public float cooldownTime = 30f;
    public float stunDuration = 5f;
    
    [Header("Upgrade & Stack Settings")]
    [Range(1, 3)] public int abilityLevel = 1;
    private int maxStacks = 1;
    private int currentStacks = 1;
    private float lastUseTime = -999f;
    private bool isCooldownActive = false;

    void Start()
    {
        UpdateMaxStacks();
        currentStacks = maxStacks;
    }

    public void UpdateMaxStacks()
    {
        maxStacks = abilityLevel;
        if (currentStacks > maxStacks) currentStacks = maxStacks;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (isCooldownActive)
        {
            if (Time.time >= lastUseTime + cooldownTime)
            {
                currentStacks++;
                Debug.Log($"[Stun] Stack pulih! Stack saat ini: {currentStacks}/{maxStacks}");
                
                if (currentStacks < maxStacks)
                {
                    lastUseTime = Time.time; 
                }
                else
                {
                    isCooldownActive = false;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryUseStunServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStunServerRpc()
    {
        if (currentStacks <= 0)
        {
            Debug.Log("Ability Stun kehabisan stack / sedang cooldown!");
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range);
        bool hitTarget = false;

        foreach (var hit in hitColliders)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Player"))
            {
                PlayerMovement targetMovement = hit.GetComponent<PlayerMovement>();
                if (targetMovement != null)
                {
                    currentStacks--;
                    if (!isCooldownActive)
                    {
                        lastUseTime = Time.time;
                        isCooldownActive = true;
                    }

                    targetMovement.enabled = false;
                    StartCoroutine(EnableMovementAfterDelay(targetMovement, stunDuration));

                    hitTarget = true;
                    Debug.Log($"[Stun] Berhasil men-stun target! Sisa stack: {currentStacks}");
                    break;
                }
            }
        }

        if (!hitTarget)
        {
            Debug.Log("[Stun] Tidak ada target dalam jangkauan!");
        }
    }

    private IEnumerator EnableMovementAfterDelay(PlayerMovement movementScript, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (movementScript != null)
        {
            movementScript.enabled = true;
            Debug.Log("[Stun] Efek stun pada target telah berakhir.");
        }
    }

    public void UpgradeSkill()
    {
        if (abilityLevel < 3)
        {
            abilityLevel++;
            UpdateMaxStacks();
            currentStacks = maxStacks;
            Debug.Log("Ability Stun diupgrade ke level: " + abilityLevel + " | Max Stack: " + maxStacks);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0, 1, 0.3f);
        Gizmos.DrawSphere(transform.position, range);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}