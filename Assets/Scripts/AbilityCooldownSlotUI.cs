using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class AbilityCooldownSlotUI : MonoBehaviour
{
    [Header("Slot Binding")]
    [Tooltip("0 = Slot kiri (Q), 1 = Slot kanan (E)")]
    public int abilitySlotIndex = 0;

    [Tooltip("Set KeyCode.Q untuk kiri, KeyCode.E untuk kanan, atau KeyCode.None untuk otomatis Q (0) / E (1)")]
    public KeyCode targetKey = KeyCode.None;

    [Header("UI References")]
    public TMP_Text statusText;
    public Image abilityIconImage;
    public Image cooldownRingFill;

    [Header("Display Settings")]
    public string readyText = "Ready!";
    public bool showDecimalWhenLow = false;
    public Color readyColor = Color.white;
    public Color cooldownColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private IAbilityCooldown boundAbility;
    private MonoBehaviour boundMono;
    private NetworkBehaviour boundNetBehaviour;
    private bool isBound = false;
    private float rebindTimer = 0f;
    private bool loggedWaitingForOwner = false;

    private void Awake()
    {
        if (cooldownRingFill != null)
        {
            cooldownRingFill.type = Image.Type.Filled;
            cooldownRingFill.fillMethod = Image.FillMethod.Radial360;
            cooldownRingFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownRingFill.fillClockwise = true;
            cooldownRingFill.fillAmount = 1f;
        }

        ClearIconVisual();
        SetReadyVisual();
    }

    private void OnEnable()
    {
        PlayerAbilityManager.OnLocalPlayerReady += HandleLocalPlayerReady;

        if (PlayerAbilityManager.LocalInstance != null)
        {
            HandleLocalPlayerReady(PlayerAbilityManager.LocalInstance);
        }
    }

    private void OnDisable()
    {
        PlayerAbilityManager.OnLocalPlayerReady -= HandleLocalPlayerReady;

        if (PlayerAbilityManager.LocalInstance != null)
        {
            PlayerAbilityManager.LocalInstance.OnAbilitiesUpdated -= HandleAbilitiesUpdated;
        }
    }

    private void HandleLocalPlayerReady(PlayerAbilityManager manager)
    {
        if (manager == null) return;

        manager.OnAbilitiesUpdated -= HandleAbilitiesUpdated;
        manager.OnAbilitiesUpdated += HandleAbilitiesUpdated;

        ForceRebind();
    }

    private void HandleAbilitiesUpdated(string ability1, string ability2)
    {
        ForceRebind();
    }

    private void ForceRebind()
    {
        isBound = false;
        boundAbility = null;
        boundMono = null;
        boundNetBehaviour = null;
        rebindTimer = 0f;
        TryBindLocalPlayerAbility();
    }

    private KeyCode GetExpectedKey()
    {
        if (targetKey != KeyCode.None)
            return targetKey;

        return abilitySlotIndex == 0 ? KeyCode.Q : KeyCode.E;
    }

    private void Update()
    {
        bool invalidNetworkState = boundNetBehaviour != null && (!boundNetBehaviour.IsSpawned || !boundNetBehaviour.IsOwner);
        bool abilityDisabled = boundMono == null || !boundMono.enabled;

        if (!isBound || boundAbility == null || invalidNetworkState || abilityDisabled)
        {
            isBound = false;
            boundAbility = null;
            boundMono = null;
            boundNetBehaviour = null;

            rebindTimer -= Time.deltaTime;
            if (rebindTimer <= 0f)
            {
                rebindTimer = 0.25f;
                TryBindLocalPlayerAbility();
            }

            if (!isBound)
            {
                ClearIconVisual();
                SetReadyVisual();
                return;
            }
        }

        // Keep icon synced in case it changed
        if (abilityIconImage != null && abilityIconImage.sprite != boundAbility.AbilityIcon)
        {
            SetIconVisual(boundAbility.AbilityIcon);
        }

        float remaining = boundAbility.GetRemainingCooldown();
        float duration = Mathf.Max(0.01f, boundAbility.CooldownDuration);

        if (remaining > 0f)
        {
            float normalized = Mathf.Clamp01(remaining / duration);

            if (cooldownRingFill != null)
            {
                cooldownRingFill.fillAmount = normalized;
            }

            if (statusText != null)
            {
                statusText.text = showDecimalWhenLow && remaining < 10f
                    ? $"{remaining:0.0}s"
                    : $"{Mathf.CeilToInt(remaining)}s";
                statusText.color = cooldownColor;
            }
        }
        else
        {
            SetReadyVisual();
        }
    }

    private void SetReadyVisual()
    {
        if (cooldownRingFill != null)
        {
            cooldownRingFill.fillAmount = 1f;
        }

        if (statusText != null)
        {
            statusText.text = readyText;
            statusText.color = readyColor;
        }
    }

    private void SetIconVisual(Sprite icon)
    {
        if (abilityIconImage == null) return;

        if (icon != null)
        {
            abilityIconImage.sprite = icon;
            abilityIconImage.enabled = true;
        }
        else
        {
            ClearIconVisual();
        }
    }

    private void ClearIconVisual()
    {
        if (abilityIconImage == null) return;

        abilityIconImage.sprite = null;
        abilityIconImage.enabled = false;
    }

    private void TryBindLocalPlayerAbility()
    {
        boundAbility = null;
        boundMono = null;
        boundNetBehaviour = null;
        isBound = false;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.SpawnManager == null)
        {
            if (enableDebugLogs && !loggedWaitingForOwner)
            {
                Debug.LogWarning($"[CooldownUI:{name}] NetworkManager / SpawnManager belum siap.");
                loggedWaitingForOwner = true;
            }
            return;
        }

        KeyCode expectedKey = GetExpectedKey();
        bool foundOwnedPlayer = false;

        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            NetworkObject netObj = kvp.Value;
            if (netObj == null || !netObj.IsSpawned || !netObj.IsOwner) continue;
            if (!netObj.CompareTag("Player")) continue;

            foundOwnedPlayer = true;
            loggedWaitingForOwner = false;

            MonoBehaviour[] monos = netObj.GetComponents<MonoBehaviour>();

            // Pass 1: Match strictly by assigned key (KeyCode.Q for Slot 0, KeyCode.E for Slot 1)
            foreach (var mono in monos)
            {
                if (mono != null && mono.enabled && mono is IAbilityCooldown abilityCooldown)
                {
                    if (abilityCooldown.AssignedKey == expectedKey)
                    {
                        BindAbility(mono, abilityCooldown, netObj.OwnerClientId);
                        return;
                    }
                }
            }

            // Pass 2 (Fallback): If the player selected the exact same ability for both slots
            // (so AssignedKey was overwritten to KeyCode.E), match by the active ability name for this slot
            string targetAbilityName = null;
            if (PlayerAbilityManager.LocalInstance != null)
            {
                targetAbilityName = abilitySlotIndex == 0
                    ? PlayerAbilityManager.LocalInstance.CurrentAbility1
                    : PlayerAbilityManager.LocalInstance.CurrentAbility2;
            }

            foreach (var mono in monos)
            {
                if (mono != null && mono.enabled && mono is IAbilityCooldown abilityCooldown)
                {
                    if (!string.IsNullOrEmpty(targetAbilityName) &&
                        string.Equals(abilityCooldown.AbilityName.Trim(), targetAbilityName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    {
                        BindAbility(mono, abilityCooldown, netObj.OwnerClientId);
                        return;
                    }
                }
            }
        }

        if (enableDebugLogs && !foundOwnedPlayer && !loggedWaitingForOwner)
        {
            Debug.LogWarning($"[CooldownUI:{name}] Belum menemukan Spawned Player dengan IsOwner=true.");
            loggedWaitingForOwner = true;
        }
    }

    private void BindAbility(MonoBehaviour mono, IAbilityCooldown abilityCooldown, ulong ownerClientId)
    {
        boundMono = mono;
        boundAbility = abilityCooldown;
        boundNetBehaviour = mono as NetworkBehaviour;
        isBound = true;

        SetIconVisual(abilityCooldown.AbilityIcon);

        if (enableDebugLogs)
        {
            Debug.Log($"[CooldownUI:{name}] Bound ke '{abilityCooldown.AbilityName}' (Key: {abilityCooldown.AssignedKey}) pada Player OwnerClientId={ownerClientId}.");
        }
    }
}