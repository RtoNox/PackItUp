using UnityEngine;
using Unity.Netcode;

public class PlayerAbilityManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) return;

        string ability1 = PlayerPrefs.GetString("SelectedAbility1", "Steal");
        string ability2 = PlayerPrefs.GetString("SelectedAbility2", "Stun");

        Debug.Log($"[AbilityManager] Slot 1 (F): {ability1} | Slot 2 (R): {ability2}");

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

        ConfigureAbility(ability1, stealComp, stunComp, jammerComp, warpComp, behindYouComp, KeyCode.F);
        ConfigureAbility(ability2, stealComp, stunComp, jammerComp, warpComp, behindYouComp, KeyCode.R);
    }

    private void ConfigureAbility(string abilityName, AbilitySteal steal, AbilityStun stun, AbilityJammer jammer, AbilityWarp warp, AbilityBehindYou behindYou, KeyCode assignedKey)
    {
        if (string.Equals(abilityName, "Steal", System.StringComparison.OrdinalIgnoreCase) && steal != null)
        {
            steal.enabled = true;
            steal.assignedKey = assignedKey;
        }
        else if (string.Equals(abilityName, "Stun", System.StringComparison.OrdinalIgnoreCase) && stun != null)
        {
            stun.enabled = true;
            stun.assignedKey = assignedKey;
        }
        else if (string.Equals(abilityName, "Jammer", System.StringComparison.OrdinalIgnoreCase) && jammer != null)
        {
            jammer.enabled = true;
            jammer.assignedKey = assignedKey;
        }
        else if (string.Equals(abilityName, "Warp", System.StringComparison.OrdinalIgnoreCase) && warp != null)
        {
            warp.enabled = true;
            warp.assignedKey = assignedKey;
        }
        else if (string.Equals(abilityName, "Behind You", System.StringComparison.OrdinalIgnoreCase) && behindYou != null)
        {
            behindYou.enabled = true;
            behindYou.assignedKey = assignedKey;
        }
    }
}