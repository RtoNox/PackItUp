using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityBehindYou : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float searchRadius = 25f;
    public float cooldownTime = 30f;
    public float stunDuration = 0.3f; 
    
    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.F;

    private float lastUseTime = -999f;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(assignedKey))
        {
            TryUseBehindYouServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseBehindYouServerRpc()
    {
        if (Time.time < lastUseTime + cooldownTime)
        {
            Debug.Log("[Behind You] Skill masih cooldown!");
            return;
        }

      
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, searchRadius);
        List<Transform> validTargets = new List<Transform>();

        foreach (var hit in hitColliders)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Player"))
            {
                validTargets.Add(hit.transform);
            }
        }

        if (validTargets.Count == 0)
        {
            Debug.Log("[Behind You] Tidak ada target musuh di sekitar!");
            return;
        }


        Transform randomTarget = validTargets[Random.Range(0, validTargets.Count)];

  
        Vector3 targetBehindPos = randomTarget.position - (randomTarget.forward * 0.5f);
        targetBehindPos.y = randomTarget.position.y;

       
        CharacterController charController = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        if (charController != null)
        {
            charController.enabled = false;
            transform.position = targetBehindPos;
            charController.enabled = true;
        }
        else if (rb != null)
        {
            rb.position = targetBehindPos;
            rb.MovePosition(targetBehindPos);
        }
        else
        {
            transform.position = targetBehindPos;
        }

        
        Vector3 lookDirection = (randomTarget.position - transform.position);
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

   
        PlayerMovement targetMovement = randomTarget.GetComponent<PlayerMovement>();
        if (targetMovement != null)
        {
            targetMovement.enabled = false;
            StartCoroutine(EnableTargetMovementAfterDelay(targetMovement, stunDuration));
        }

        lastUseTime = Time.time;
        Debug.Log($"[Behind You] Berhasil teleport ke belakang musuh dan memberikan stun selama {stunDuration} detik!");
    }

    private IEnumerator EnableTargetMovementAfterDelay(PlayerMovement movementScript, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (movementScript != null)
        {
            movementScript.enabled = true;
        }
    }
}