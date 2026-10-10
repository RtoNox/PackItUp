using UnityEngine;
using Unity.Netcode;

public class PlayerAbilityManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

      
        if (!IsOwner) return;

  
        string chosenAbility = PlayerPrefs.GetString("SelectedAbility", "Steal");
        Debug.Log("[AbilityManager] Ability yang terbaca dari PlayerPrefs: " + chosenAbility);

        AbilitySteal stealComp = GetComponent<AbilitySteal>();
        AbilityStun stunComp = GetComponent<AbilityStun>();

        if (stealComp != null) stealComp.enabled = false;
        if (stunComp != null) stunComp.enabled = false;


        if (string.Equals(chosenAbility, "Stun", System.StringComparison.OrdinalIgnoreCase))
        {
            if (stunComp != null)
            {
                stunComp.enabled = true;
                Debug.Log("[AbilityManager] -> Berhasil mengaktifkan: STUN");
            }
            else
            {
                Debug.LogWarning("[AbilityManager] Komponen AbilityStun tidak ditemukan di Prefab Player!");
            }
        }
        else 
        {
            if (stealComp != null)
            {
                stealComp.enabled = true;
                Debug.Log("[AbilityManager] -> Berhasil mengaktifkan: STEAL");
            }
            else
            {
                Debug.LogWarning("[AbilityManager] Komponen AbilitySteal tidak ditemukan di Prefab Player!");
            }
        }
    }
}