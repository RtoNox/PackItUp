using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityStun : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Stun";
    public Sprite abilityIcon;
    public float range = 5f;
    public float stunDuration = 3f;
    public float cooldownTime = 25f;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

    [Header("VFX Settings")]
    public GameObject headVfxPrefab;     // Tarik prefab partikel kepala ke sini
    public GameObject bodyLightningPrefab; // Tarik prefab 'Lighting_Stun' ke sini

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

    public float GetRemainingCooldown()
    {
        if (IsServer)
        {
            float remainingServer = (lastUseTime + cooldownTime) - Time.time;
            return remainingServer > 0f ? remainingServer : 0f;
        }

        if (clientCooldownEndTime > 0f)
        {
            float remainingClient = clientCooldownEndTime - Time.time;
            return remainingClient > 0f ? remainingClient : 0f;
        }

        return 0f;
    }

    void Update()
    {
        if (!IsOwner) return;

        bool isOnCooldown = GetRemainingCooldown() > 0f;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Stun] Cooldown selesai! Ability Stun siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (AbilityJammer.IsPlayerJammed(this, "Stun"))
            {
                return;
            }

            if (isOnCooldown)
            {
                Debug.Log("[Stun] Skill masih cooldown!");
                return;
            }

            TryUseStunServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseStunServerRpc()
    {
        if (AbilityJammer.IsPlayerJammed(this, "Stun"))
        {
            return;
        }

        if (Time.time < lastUseTime + cooldownTime)
        {
            return;
        }

        bool success = false;
        GameObject targetPlayer = null;
        float shortestDistance = Mathf.Infinity;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range);
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
            NetworkObject targetNetObj = targetPlayer.GetComponent<NetworkObject>();
            if (targetNetObj != null)
            {
                ApplyStunClientRpc(targetNetObj.NetworkObjectId, stunDuration);

                Debug.Log($"[Stun] Berhasil men-stun player target ({targetPlayer.name}) selama {stunDuration} detik!");
                success = true;
            }
        }

        if (success)
        {
            lastUseTime = Time.time;
            TriggerCooldownClientRpc(cooldownTime);
        }
        else
        {
            Debug.Log("[Stun] Tidak ada target player di dekatmu! Skill tidak masuk cooldown.");
        }
    }

    [ClientRpc]
    private void TriggerCooldownClientRpc(float duration)
    {
        if (IsOwner)
        {
            clientCooldownEndTime = Time.time + duration;
            wasOnCooldown = true;
        }
    }

    [ClientRpc]
    private void ApplyStunClientRpc(ulong targetNetworkObjectId, float duration)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj))
        {
            StartCoroutine(StunRoutine(targetNetObj.gameObject, duration));
        }
    }

    private IEnumerator StunRoutine(GameObject target, float duration)
    {
        // 1. Matikan movement player
        MonoBehaviour playerMovement = target.GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        List<GameObject> spawnedVfxList = new List<GameObject>();

        // 2. Munculkan VFX di Kepala (dengan offset Y lebih tinggi)
        if (headVfxPrefab != null)
        {
            Vector3 headPosition = target.transform.position + new Vector3(0, 2.0f, 0);
            GameObject headVfx = Instantiate(headVfxPrefab, headPosition, Quaternion.identity, target.transform);
            spawnedVfxList.Add(headVfx);
        }

        // 3. Munculkan VFX Petir di Area Badan (posisi pusat atau offset dada Y + 1.0f)
        if (bodyLightningPrefab != null)
        {
            Vector3 bodyPosition = target.transform.position + new Vector3(0, 1.0f, 0);
            GameObject bodyVfx = Instantiate(bodyLightningPrefab, bodyPosition, Quaternion.identity, target.transform);
            spawnedVfxList.Add(bodyVfx);
        }

        Debug.Log($"[Stun] Player {target.name} sedang terkena Stun!");

        // Tunggu selama durasi stun
        yield return new WaitForSeconds(duration);

        // 4. Hancurkan semua VFX setelah durasi stun selesai
        foreach (GameObject vfx in spawnedVfxList)
        {
            if (vfx != null)
            {
                Destroy(vfx);
            }
        }

        // 5. Nyalakan kembali movement player
        if (playerMovement != null) playerMovement.enabled = true;

        Debug.Log($"[Stun] Efek Stun pada {target.name} telah selesai.");
    }
}