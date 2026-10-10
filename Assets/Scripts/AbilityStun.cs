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
    
    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.R;

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

        if (Input.GetKeyDown(assignedKey))
        {
            TryUseStunServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStunServerRpc()
    {
        if (currentStacks <= 0)
        {
            Debug.Log("Ability Stun kehabisan stack / cooldown!");
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range);

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
                    Debug.Log("[Stun] Musuh berhasil di-stun!");
                    break;
                }
            }
        }
    }

    private IEnumerator EnableMovementAfterDelay(PlayerMovement movementScript, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (movementScript != null)
        {
            movementScript.enabled = true;
        }
    }
}