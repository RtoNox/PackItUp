using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityMarionette : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float cooldownTime = 60f;
    public float maxDuration = 30f;
    private float lastUseTime = -999f;
    private bool wasOnCooldown = false;

    [Header("Line of Sight Settings")]
    public LayerMask obstacleLayer; 

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.F; 

    void Update()
    {
        if (!IsOwner) return;

        bool isOnCooldown = Time.time < lastUseTime + cooldownTime;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Marionette] Cooldown selesai! Ability Marionette siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (isOnCooldown)
            {
                Debug.Log("[Marionette] Skill masih cooldown!");
                return;
            }

            if (DuckKunUIController.Instance != null)
            {
                DuckKunUIController.Instance.InitMarionette(this);
                DuckKunUIController.Instance.OpenPanel();
            }
            else
            {
                Debug.LogWarning("[Marionette] DuckKunUIController Instance tidak ditemukan!");
            }
        }
    }

    public void OnTargetSelected(ulong targetNetworkObjectId)
    {
        TryUseMarionetteServerRpc(targetNetworkObjectId);
    }

    [ServerRpc]
    private void TryUseMarionetteServerRpc(ulong targetNetworkObjectId)
    {
        if (Time.time < lastUseTime + cooldownTime) return;

        NetworkObject targetNetObj = null;

        if (targetNetworkObjectId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out targetNetObj))
        {
            if (targetNetObj.gameObject == gameObject)
            {
                Debug.Log("[Marionette] Tidak bisa menargetkan diri sendiri!");
                return;
            }
        }

        if (targetNetObj != null && targetNetObj.gameObject != gameObject)
        {
            lastUseTime = Time.time;
            TriggerCooldownClientRpc(Time.time);

            StartCoroutine(MarionetteRoutine(gameObject, targetNetObj.gameObject));
        }
        else
        {
            Debug.Log("[Marionette] Target tidak valid!");
        }
    }

    private IEnumerator MarionetteRoutine(GameObject caster, GameObject target)
    {
        Debug.Log($"[Marionette] Berhasil mengaitkan Marionette ke target: {target.name} (Durasi: {maxDuration}s).");

        PlayerPoints targetPoints = target.GetComponent<PlayerPoints>();
        NetworkObject casterNetObj = caster.GetComponent<NetworkObject>();

        // Set referensi caster ke PlayerPoints milik target di sisi Server agar poinnya bisa dicuri
        if (targetPoints != null && casterNetObj != null && IsServer)
        {
            targetPoints.marionetteCasterRef.Value = new NetworkObjectReference(casterNetObj);
        }

        float timer = 0f;

        while (timer < maxDuration)
        {
            if (IsSeenByOtherPlayers(caster, target))
            {
                Debug.Log("[Marionette] KETAHUAN! Target terlihat oleh pemain lain. Skill dibatalkan otomatis!");
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Lepaskan kembali ikatan Marionette setelah durasi habis atau dibatalkan
        if (targetPoints != null && IsServer)
        {
            targetPoints.marionetteCasterRef.Value = default;
        }

        Debug.Log("[Marionette] Sesi Marionette berakhir.");
    }

    private bool IsSeenByOtherPlayers(GameObject caster, GameObject target)
    {
        var spawnedObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

        foreach (var kvp in spawnedObjects)
        {
            NetworkObject netObj = kvp.Value;
            GameObject observer = netObj.gameObject;

            if (observer == caster || observer == target) continue;
            if (!observer.CompareTag("Player")) continue;

            Vector3 observerPos = observer.transform.position + Vector3.up * 1.5f; 
            Vector3 targetPos = target.transform.position + Vector3.up * 1.0f;     

            if (!Physics.Linecast(observerPos, targetPos, obstacleLayer))
            {
                return true; 
            }
        }

        return false; 
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