using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityBehindYou : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float searchRadius = 25f; // Jarak maksimum untuk mencari target acak
    public float cooldownTime = 30f;
    public float stunDuration = 0.3f; // Diperbaiki dari 0.3s menjadi 0.3f
    
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

        // Cari semua objek player di sekitar
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, searchRadius);
        List<Transform> validTargets = new List<Transform>();

        foreach (var hit in hitColliders)
        {
            // Pastikan bukan diri sendiri dan merupakan player
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

        // Pilih satu target secara acak dari daftar musuh yang ditemukan
        Transform randomTarget = validTargets[Random.Range(0, validTargets.Count)];

        // Hitung posisi 0.5 unit tepat di belakang musuh (-forward target)
        Vector3 targetBehindPos = randomTarget.position - (randomTarget.forward * 0.5f);
        targetBehindPos.y = randomTarget.position.y; // Menjaga ketinggian player tetap sejajar

        // Eksekusi teleportasi player pengguna skill
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

        // Hadapkan player ke arah punggung musuh (menghadap ke musuh)
        Vector3 lookDirection = (randomTarget.position - transform.position);
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // Berikan efek stun sementara pada target
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