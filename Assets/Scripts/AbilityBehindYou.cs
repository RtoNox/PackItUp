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
    public float behindDistance = 1.5f;

    [Header("Upgrade Settings (Max Lv 3)")]
    [Range(1, 3)] public int abilityLevel = 1;
    public int maxAbilityLevel = 3;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

    private readonly NetworkVariable<double> cooldownEndServerTime = new NetworkVariable<double>(
        0.0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

    public void SetUpgradeLevel(int newLevel)
    {
        abilityLevel = Mathf.Clamp(newLevel, 1, maxAbilityLevel);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetUpgradeLevel(AbilityDataHolder.GetLevel(abilityName));
    }

    public float GetRemainingCooldown()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            double nowServerTime = NetworkManager.Singleton.ServerTime.Time;
            float remainingNet = (float)(cooldownEndServerTime.Value - nowServerTime);
            if (remainingNet > 0f)
            {
                return remainingNet;
            }
        }

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
        if (!IsOwner || !IsSpawned) return;

        bool isOnCooldown = GetRemainingCooldown() > 0f;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[BehindYou] Cooldown selesai! Ability Behind You siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (AbilityJammer.IsPlayerJammed(this, "Behind You"))
            {
                return;
            }

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
        if (!IsSpawned) return;

        if (AbilityJammer.IsPlayerJammed(this, "Behind You"))
        {
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.ServerTime.Time < cooldownEndServerTime.Value)
                return;
        }
        else if (Time.time < lastUseTime + cooldownTime)
        {
            return;
        }

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
        {
            return;
        }

        List<GameObject> validTargets = new List<GameObject>();
        var spawnedObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

        foreach (var kvp in spawnedObjects)
        {
            NetworkObject netObj = kvp.Value;
            if (netObj != null && netObj.gameObject != gameObject && netObj.CompareTag("Player"))
            {
                validTargets.Add(netObj.gameObject);
            }
        }

        if (validTargets.Count > 0)
        {
            int randomIndex = Random.Range(0, validTargets.Count);
            GameObject targetPlayer = validTargets[randomIndex];

            Vector3 targetPosition = targetPlayer.transform.position - (targetPlayer.transform.forward * behindDistance);
            targetPosition.y = targetPlayer.transform.position.y;

            ExecuteTeleportClientRpc(targetPosition, targetPlayer.transform.position);

            lastUseTime = Time.time;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                cooldownEndServerTime.Value = NetworkManager.Singleton.ServerTime.Time + cooldownTime;
            }

            TriggerCooldownClientRpc(cooldownTime);

            Debug.Log($"[BehindYou Lv.{abilityLevel}] Berpindah secara acak ke belakang player {targetPlayer.name}!");
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

        if (netTransform != null) ((Behaviour)netTransform).enabled = false;
        if (charController != null) charController.enabled = false;

        transform.position = newPosition;
        transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));

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