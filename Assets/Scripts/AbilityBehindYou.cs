using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityBehindYou : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Behind You";
    public Sprite abilityIcon;
    public float cooldownTime = 40f;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

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

        // List untuk menampung semua player lain yang ada di map
        List<GameObject> validTargets = new List<GameObject>();

        var spawnedObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjects;
        foreach (var kvp in spawnedObjects)
        {
            NetworkObject netObj = kvp.Value;

            // Masukkan objek yang memiliki komponen AbilityBehindYou (atau tag Player) dan bukan diri sendiri
            if (netObj.gameObject != gameObject && netObj.CompareTag("Player"))
            {
                validTargets.Add(netObj.gameObject);
            }
        }

        if (validTargets.Count > 0)
        {
            // Pilih secara acak (random) satu player dari daftar yang ada di map
            int randomIndex = Random.Range(0, validTargets.Count);
            GameObject targetPlayer = validTargets[randomIndex];

            // Hitung posisi di belakang target
            Vector3 targetPosition = targetPlayer.transform.position - (targetPlayer.transform.forward * 1.5f);
            targetPosition.y = targetPlayer.transform.position.y;

            // Jalankan perpindahan dan sinkronkan ke semua client
            ExecuteTeleportClientRpc(targetPosition, targetPlayer.transform.position);

            lastUseTime = Time.time;
            TriggerCooldownClientRpc(cooldownTime);

            Debug.Log($"[BehindYou] Berpindah secara acak ke belakang player {targetPlayer.name}!");
        }
        else
        {
            Debug.Log("[BehindYou] Tidak ada player lain di map! Skill tidak masuk cooldown.");
        }
    }

    [ClientRpc]
    private void ExecuteTeleportClientRpc(Vector3 newPosition, Vector3 targetPos)
    {
        CharacterController charController = GetComponent<CharacterController>();
        Component netTransform = GetComponent("ClientNetworkTransform");

        // Nonaktifkan sementara komponen pengatur posisi agar teleport tidak diblokir
        if (netTransform != null) ((Behaviour)netTransform).enabled = false;
        if (charController != null) charController.enabled = false;

        transform.position = newPosition;
        transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));

        // Nyalakan kembali komponennya
        if (charController != null) charController.enabled = true;
        if (netTransform != null) ((Behaviour)netTransform).enabled = true;
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
}