using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class AbilityCooldownSlotUI : MonoBehaviour
{
    [Header("Slot Binding")]
    [Tooltip("Automatically maps 1st HUD slot to 1st Equip (Q) and 2nd HUD slot to 2nd Equip (E)")]
    public bool autoDetectSlotOrder = true;

    [Tooltip("0 = 1st Equip (Q), 1 = 2nd Equip (E)")]
    public int abilitySlotIndex = 0;

    [Tooltip("Set KeyCode.Q for 1st Equip, KeyCode.E for 2nd Equip, or None for automatic detection")]
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
    private string boundAbilityName;
    private bool isBound = false;
    private float rebindTimer = 0f;
    private bool loggedWaitingForOwner = false;

    private void Awake()
    {
        GetResolvedSlotIndex();

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
        boundAbilityName = null;
        rebindTimer = 0f;
        TryBindLocalPlayerAbility();
    }

    private int GetResolvedSlotIndex()
    {
        if (targetKey == KeyCode.Q)
        {
            abilitySlotIndex = 0;
            return 0;
        }

        if (targetKey == KeyCode.E)
        {
            abilitySlotIndex = 1;
            return 1;
        }

        if (autoDetectSlotOrder)
        {
            if (transform.parent != null)
            {
                AbilityCooldownSlotUI[] siblingSlots = transform.parent.GetComponentsInChildren<AbilityCooldownSlotUI>(true);
                if (siblingSlots.Length >= 2)
                {
                    int siblingIndex = Array.IndexOf(siblingSlots, this);
                    if (siblingIndex >= 0)
                    {
                        abilitySlotIndex = siblingIndex;
                        return siblingIndex;
                    }
                }
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.rootCanvas != null)
            {
                AbilityCooldownSlotUI[] canvasSlots = parentCanvas.rootCanvas.GetComponentsInChildren<AbilityCooldownSlotUI>(true);
                if (canvasSlots.Length >= 2)
                {
                    int canvasIndex = Array.IndexOf(canvasSlots, this);
                    if (canvasIndex >= 0)
                    {
                        abilitySlotIndex = canvasIndex;
                        return canvasIndex;
                    }
                }
            }
        }

        return Mathf.Clamp(abilitySlotIndex, 0, 1);
    }

    private string GetEquippedSkillNameForSlot(PlayerAbilityManager localManager, int slotIndex)
    {
        if (localManager != null)
        {
            return slotIndex == 0 ? localManager.CurrentAbility1 : localManager.CurrentAbility2;
        }

        if (slotIndex == 0)
            return string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility1) ? "Steal" : AbilityDataHolder.SelectedAbility1;

        return string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility2) ? "Stun" : AbilityDataHolder.SelectedAbility2;
    }

    private void Update()
    {
        PlayerAbilityManager localManager = PlayerAbilityManager.LocalInstance;
        int resolvedSlot = GetResolvedSlotIndex();

        bool slotChoiceChanged = false;
        if (isBound)
        {
            string expectedName = GetEquippedSkillNameForSlot(localManager, resolvedSlot);
            slotChoiceChanged = !string.Equals(boundAbilityName, expectedName, StringComparison.OrdinalIgnoreCase);
        }

        bool invalidNetworkState = boundNetBehaviour != null && (!boundNetBehaviour.IsSpawned || !boundNetBehaviour.IsOwner);
        bool abilityDisabled = boundMono == null || !boundMono.enabled;

        if (!isBound || boundAbility == null || invalidNetworkState || abilityDisabled || slotChoiceChanged)
        {
            isBound = false;
            boundAbility = null;
            boundMono = null;
            boundNetBehaviour = null;
            boundAbilityName = null;

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
        boundAbilityName = null;
        isBound = false;

        PlayerAbilityManager localManager = PlayerAbilityManager.LocalInstance;

        if (localManager == null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.SpawnManager != null)
        {
            foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                NetworkObject netObj = kvp.Value;
                if (netObj != null && netObj.IsSpawned && netObj.IsOwner && netObj.TryGetComponent(out PlayerAbilityManager foundManager))
                {
                    localManager = foundManager;
                    break;
                }
            }
        }

        if (localManager == null)
        {
            if (enableDebugLogs && !loggedWaitingForOwner)
            {
                Debug.LogWarning($"[CooldownUI:{name}] Menunggu PlayerAbilityManager lokal...");
                loggedWaitingForOwner = true;
            }
            return;
        }

        loggedWaitingForOwner = false;

        int resolvedSlot = GetResolvedSlotIndex();
        string equippedName = GetEquippedSkillNameForSlot(localManager, resolvedSlot);
        MonoBehaviour abilityComp = localManager.GetAbilityComponentByName(equippedName);

        if (abilityComp != null && abilityComp.enabled && abilityComp is IAbilityCooldown abilityCooldown)
        {
            boundMono = abilityComp;
            boundAbility = abilityCooldown;
            boundNetBehaviour = abilityComp as NetworkBehaviour;
            boundAbilityName = equippedName;
            isBound = true;

            SetIconVisual(abilityCooldown.AbilityIcon);

            if (enableDebugLogs)
            {
                Debug.Log($"[CooldownUI:{name}] Equip {resolvedSlot + 1} bound ke '{equippedName}' (Key: {abilityCooldown.AssignedKey}).");
            }
        }
    }
}