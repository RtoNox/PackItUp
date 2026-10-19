using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerAbilityManager : NetworkBehaviour
{
    private NetworkVariable<FixedString32Bytes> netAbility1 = new NetworkVariable<FixedString32Bytes>("Steal", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString32Bytes> netAbility2 = new NetworkVariable<FixedString32Bytes>("Stun", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            string localAbility1 = string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility1) ? "Steal" : AbilityDataHolder.SelectedAbility1;
            string localAbility2 = string.IsNullOrEmpty(AbilityDataHolder.SelectedAbility2) ? "Stun" : AbilityDataHolder.SelectedAbility2;

            Debug.Log($"[Client] Kirim ke Server -> Slot 1 (Q): {localAbility1}, Slot 2 (E): {localAbility2}");

            SubmitAbilityChoiceServerRpc(localAbility1, localAbility2);
        }

        netAbility1.OnValueChanged += (oldVal, newVal) => ApplyAbilities();
        netAbility2.OnValueChanged += (oldVal, newVal) => ApplyAbilities();

        ApplyAbilities();
    }

    [ServerRpc]
    private void SubmitAbilityChoiceServerRpc(string ab1, string ab2, ServerRpcParams rpcParams = default)
    {
        netAbility1.Value = ab1;
        netAbility2.Value = ab2;
    }

    private void ApplyAbilities()
    {
        if (!IsOwner) return;

        string ability1 = netAbility1.Value.ToString();
        string ability2 = netAbility2.Value.ToString();

        AbilitySteal stealComp = GetComponent<AbilitySteal>();
        AbilityStun stunComp = GetComponent<AbilityStun>();
        AbilityJammer jammerComp = GetComponent<AbilityJammer>();
        AbilityWarp warpComp = GetComponent<AbilityWarp>();
        AbilityBehindYou behindYouComp = GetComponent<AbilityBehindYou>();

        if (stealComp != null) stealComp.enabled = false;
        if (stunComp != null) stunComp.enabled = false;
        if (jammerComp != null) jammerComp.enabled = false;
        if (warpComp != null) warpComp.enabled = false;
        if (behindYouComp != null) behindYouComp.enabled = false;

        ConfigureSingleAbility(ability1, stealComp, stunComp, jammerComp, warpComp, behindYouComp, KeyCode.Q);
        ConfigureSingleAbility(ability2, stealComp, stunComp, jammerComp, warpComp, behindYouComp, KeyCode.E);
    }

    private void ConfigureSingleAbility(string abilityName, AbilitySteal steal, AbilityStun stun, AbilityJammer jammer, AbilityWarp warp, AbilityBehindYou behindYou, KeyCode assignedKey)
    {
        if (string.IsNullOrEmpty(abilityName)) return;

        string cleanName = abilityName.Trim();

        if (string.Equals(cleanName, "Steal", System.StringComparison.OrdinalIgnoreCase) && steal != null)
        {
            steal.enabled = true;
            steal.assignedKey = assignedKey;
        }
        else if (string.Equals(cleanName, "Stun", System.StringComparison.OrdinalIgnoreCase) && stun != null)
        {
            stun.enabled = true;
            stun.assignedKey = assignedKey;
        }
        else if (string.Equals(cleanName, "Jammer", System.StringComparison.OrdinalIgnoreCase) && jammer != null)
        {
            jammer.enabled = true;
            jammer.assignedKey = assignedKey;
        }
        else if (string.Equals(cleanName, "Warp", System.StringComparison.OrdinalIgnoreCase) && warp != null)
        {
            warp.enabled = true;
            warp.assignedKey = assignedKey;
        }
        else if ((string.Equals(cleanName, "Behind You", System.StringComparison.OrdinalIgnoreCase) || 
                  string.Equals(cleanName, "BehindYou", System.StringComparison.OrdinalIgnoreCase)) && behindYou != null)
        {
            behindYou.enabled = true;
            behindYou.assignedKey = assignedKey;
        }
    }
}