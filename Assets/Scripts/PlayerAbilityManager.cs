using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerAbilityManager : NetworkBehaviour
{
    public static PlayerAbilityManager LocalInstance { get; private set; }
    public static event Action<PlayerAbilityManager> OnLocalPlayerReady;

    public event Action<string, string> OnAbilitiesUpdated;

    public string CurrentAbility1 { get; private set; } = "Steal";
    public string CurrentAbility2 { get; private set; } = "Stun";

    private NetworkVariable<FixedString32Bytes> netAbility1 = new NetworkVariable<FixedString32Bytes>(
        "Steal",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<FixedString32Bytes> netAbility2 = new NetworkVariable<FixedString32Bytes>(
        "Stun",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        netAbility1.OnValueChanged += OnNetAbilityChanged;
        netAbility2.OnValueChanged += OnNetAbilityChanged;

        if (IsOwner)
        {
            LocalInstance = this;

            // 1st Equip -> Slot 1 (Q), 2nd Equip -> Slot 2 (E)
            string localAbility1 = string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility1)
                ? "Steal"
                : AbilityDataHolder.SelectedAbility1;

            string localAbility2 = string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility2)
                ? "Stun"
                : AbilityDataHolder.SelectedAbility2;

            Debug.Log($"[Client] Kirim ke Server -> 1st Equip (Q): {localAbility1}, 2nd Equip (E): {localAbility2}");

            ApplyAbilities(localAbility1, localAbility2);

            SubmitAbilityChoiceServerRpc(localAbility1, localAbility2);
            OnLocalPlayerReady?.Invoke(this);
        }
        else if (IsServer)
        {
            ApplyAbilities(netAbility1.Value.ToString(), netAbility2.Value.ToString());
        }
    }

    public override void OnNetworkDespawn()
    {
        netAbility1.OnValueChanged -= OnNetAbilityChanged;
        netAbility2.OnValueChanged -= OnNetAbilityChanged;

        if (LocalInstance == this)
            LocalInstance = null;

        base.OnNetworkDespawn();
    }

    private void OnNetAbilityChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal)
    {
        ApplyAbilities(netAbility1.Value.ToString(), netAbility2.Value.ToString());
    }

    [ServerRpc]
    private void SubmitAbilityChoiceServerRpc(string ab1, string ab2, ServerRpcParams rpcParams = default)
    {
        netAbility1.Value = ab1;
        netAbility2.Value = ab2;
    }

    public MonoBehaviour GetAbilityComponentForSlot(int slotIndex)
    {
        string abilityName = slotIndex == 0 ? CurrentAbility1 : CurrentAbility2;
        return GetAbilityComponentByName(abilityName);
    }

    public MonoBehaviour GetAbilityComponentByName(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return null;

        string cleanName = abilityName.Trim();

        if (string.Equals(cleanName, "Steal", StringComparison.OrdinalIgnoreCase))
            return GetComponent<AbilitySteal>();

        if (string.Equals(cleanName, "Stun", StringComparison.OrdinalIgnoreCase))
            return GetComponent<AbilityStun>();

        if (string.Equals(cleanName, "Jammer", StringComparison.OrdinalIgnoreCase))
            return GetComponent<AbilityJammer>();

        if (string.Equals(cleanName, "Warp", StringComparison.OrdinalIgnoreCase))
            return GetComponent<AbilityWarp>();

        if (string.Equals(cleanName, "Behind You", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(cleanName, "BehindYou", StringComparison.OrdinalIgnoreCase))
            return GetComponent<AbilityBehindYou>();

        if (string.Equals(cleanName, "Duck-Kun", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(cleanName, "DuckKun", StringComparison.OrdinalIgnoreCase))
            return GetComponent<AbilityDuckKun>();

        if (string.Equals(cleanName, "Marionette", StringComparison.OrdinalIgnoreCase))
            return GetComponent<AbilityMarionette>();

        return null;
    }

    private void ApplyAbilities(string ability1, string ability2)
    {
        if (!IsOwner && !IsServer) return;

        CurrentAbility1 = ability1;
        CurrentAbility2 = ability2;

        AbilitySteal stealComp = GetComponent<AbilitySteal>();
        AbilityStun stunComp = GetComponent<AbilityStun>();
        AbilityJammer jammerComp = GetComponent<AbilityJammer>();
        AbilityWarp warpComp = GetComponent<AbilityWarp>();
        AbilityBehindYou behindYouComp = GetComponent<AbilityBehindYou>();
        AbilityDuckKun duckKunComp = GetComponent<AbilityDuckKun>();
        AbilityMarionette marionetteComp = GetComponent<AbilityMarionette>();

        if (stealComp != null) stealComp.enabled = false;
        if (stunComp != null) stunComp.enabled = false;
        if (jammerComp != null) jammerComp.enabled = false;
        if (warpComp != null) warpComp.enabled = false;
        if (behindYouComp != null) behindYouComp.enabled = false;
        if (duckKunComp != null) duckKunComp.enabled = false;
        if (marionetteComp != null) marionetteComp.enabled = false;

        ConfigureSingleAbility(ability1, stealComp, stunComp, jammerComp, warpComp, behindYouComp, duckKunComp, marionetteComp, KeyCode.Q);
        ConfigureSingleAbility(ability2, stealComp, stunComp, jammerComp, warpComp, behindYouComp, duckKunComp, marionetteComp, KeyCode.E);

        if (IsOwner)
        {
            OnAbilitiesUpdated?.Invoke(CurrentAbility1, CurrentAbility2);
        }
    }

    private void ConfigureSingleAbility(
        string abilityName,
        AbilitySteal steal,
        AbilityStun stun,
        AbilityJammer jammer,
        AbilityWarp warp,
        AbilityBehindYou behindYou,
        AbilityDuckKun duckKun,
        AbilityMarionette marionette,
        KeyCode assignedKey)
    {
        if (string.IsNullOrEmpty(abilityName)) return;

        string cleanName = abilityName.Trim();

        if (string.Equals(cleanName, "Steal", StringComparison.OrdinalIgnoreCase) && steal != null)
        {
            steal.enabled = true;
            steal.assignedKey = assignedKey;
        }
        else if (string.Equals(cleanName, "Stun", StringComparison.OrdinalIgnoreCase) && stun != null)
        {
            stun.enabled = true;
            stun.assignedKey = assignedKey;
        }
        else if (string.Equals(cleanName, "Jammer", StringComparison.OrdinalIgnoreCase) && jammer != null)
        {
            jammer.enabled = true;
            jammer.assignedKey = assignedKey;
        }
        else if (string.Equals(cleanName, "Warp", StringComparison.OrdinalIgnoreCase) && warp != null)
        {
            warp.enabled = true;
            warp.assignedKey = assignedKey;
        }
        else if ((string.Equals(cleanName, "Behind You", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(cleanName, "BehindYou", StringComparison.OrdinalIgnoreCase)) && behindYou != null)
        {
            behindYou.enabled = true;
            behindYou.assignedKey = assignedKey;
        }
        else if ((string.Equals(cleanName, "Duck-Kun", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(cleanName, "DuckKun", StringComparison.OrdinalIgnoreCase)) && duckKun != null)
        {
            duckKun.enabled = true;
            duckKun.assignedKey = assignedKey;
        }
        else if (string.Equals(cleanName, "Marionette", StringComparison.OrdinalIgnoreCase) && marionette != null)
        {
            marionette.enabled = true;
            marionette.assignedKey = assignedKey;
        }
    }
}