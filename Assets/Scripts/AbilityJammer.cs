using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityJammer : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Jammer";
    public Sprite abilityIcon;
    public float cooldownTime = 40f;
    public float maxDuration = 10f;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

    [Header("Visual Indicator & VFX")]
    public GameObject indicatorXObject; // Assign objek tanda X (bisa World Space UI / Sprite / GameObject 3D)
    public GameObject jammerVfxPrefab;
    private GameObject spawnedVfx;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

    private Coroutine activeJammerCoroutine;

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

    private void Start()
    {
        // PENTING: Pastikan indikator X mati saat pertama kali game/player spawn
        if (indicatorXObject != null)
        {
            indicatorXObject.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        CleanupVisuals();
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        CleanupVisuals();
    }

    private void CleanupVisuals()
    {
        if (activeJammerCoroutine != null)
        {
            StopCoroutine(activeJammerCoroutine);
            activeJammerCoroutine = null;
        }

        if (indicatorXObject != null)
        {
            indicatorXObject.SetActive(false);
        }

        if (spawnedVfx != null)
        {
            Destroy(spawnedVfx);
            spawnedVfx = null;
        }
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        bool isOnCooldown = GetRemainingCooldown() > 0f;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Jammer] Cooldown selesai! Ability Jammer siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            if (isOnCooldown)
            {
                Debug.Log($"[Jammer] Skill masih cooldown! Sisa: {GetRemainingCooldown():0.0}s");
                return;
            }

            Debug.Log($"[Jammer] Tombol {assignedKey} ditekan, mengirim request ke Server...");
            TryUseJammerServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseJammerServerRpc()
    {
        if (!IsSpawned) return;

        if (Time.time < lastUseTime + cooldownTime)
        {
            Debug.LogWarning("[Jammer] Server menolak: masih dalam cooldown.");
            return;
        }

        lastUseTime = Time.time;
        TriggerCooldownClientRpc(cooldownTime);

        if (activeJammerCoroutine != null)
        {
            StopCoroutine(activeJammerCoroutine);
        }
        activeJammerCoroutine = StartCoroutine(JammerRoutine());
    }

    private IEnumerator JammerRoutine()
    {
        Debug.Log($"[Jammer] Skill Jammer diaktifkan selama {maxDuration} detik!");

        ToggleJammerEffectClientRpc(true);

        float timer = 0f;
        while (timer < maxDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        ToggleJammerEffectClientRpc(false);
        activeJammerCoroutine = null;
        Debug.Log("[Jammer] Sesi Jammer berakhir.");
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
    private void ToggleJammerEffectClientRpc(bool isActive)
    {
        if (indicatorXObject != null)
        {
            indicatorXObject.SetActive(isActive);
        }
        else if (isActive)
        {
            Debug.LogWarning("[Jammer] indicatorXObject belum di-assign di Inspector!");
        }

        if (isActive)
        {
            if (jammerVfxPrefab != null && spawnedVfx == null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 2.0f;
                spawnedVfx = Instantiate(jammerVfxPrefab, spawnPos, Quaternion.identity, transform);
            }
            else if (jammerVfxPrefab == null)
            {
                Debug.LogWarning("[Jammer] jammerVfxPrefab belum di-assign di Inspector!");
            }
        }
        else
        {
            if (spawnedVfx != null)
            {
                Destroy(spawnedVfx);
                spawnedVfx = null;
            }
        }
    }
}