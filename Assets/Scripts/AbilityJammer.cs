using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityJammer : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float cooldownTime = 40f;
    public float maxDuration = 10f;
    private float lastUseTime = -999f;

    [Header("Visual Indicator & VFX")]
    public GameObject indicatorXObject; // Assign objek tanda X (bisa World Space UI / Sprite / GameObject 3D)
    public GameObject jammerVfxPrefab;
    private GameObject spawnedVfx;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

    private void Start()
    {
        // PENTING: Pastikan indikator X mati saat pertama kali game/player spawn
        if (indicatorXObject != null)
        {
            indicatorXObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(assignedKey))
        {
            if (Time.time < lastUseTime + cooldownTime)
            {
                Debug.Log("[Jammer] Skill masih cooldown!");
                return;
            }

            TryUseJammerServerRpc();
        }
    }

    [ServerRpc]
    private void TryUseJammerServerRpc()
    {
        if (Time.time < lastUseTime + cooldownTime) return;

        lastUseTime = Time.time;
        TriggerCooldownClientRpc(Time.time);

        StartCoroutine(JammerRoutine());
    }

    private IEnumerator JammerRoutine()
    {
        Debug.Log("[Jammer] Skill Jammer diaktifkan!");
        

        ToggleJammerEffectClientRpc(true);

        float timer = 0f;
        while (timer < maxDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        ToggleJammerEffectClientRpc(false);
        Debug.Log("[Jammer] Sesi Jammer berakhir.");
    }

    [ClientRpc]
    private void TriggerCooldownClientRpc(float serverTime)
    {
        if (IsOwner)
        {
            lastUseTime = serverTime;
        }
    }

    [ClientRpc]
    private void ToggleJammerEffectClientRpc(bool isActive)
    {
        if (indicatorXObject != null)
        {
            indicatorXObject.SetActive(isActive);
        }
        if (isActive)
        {
            if (jammerVfxPrefab != null && spawnedVfx == null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 2.0f;
                spawnedVfx = Instantiate(jammerVfxPrefab, spawnPos, Quaternion.identity, transform);
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