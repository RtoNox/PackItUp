// using UnityEngine;
// using TMPro;
// using Unity.Netcode;

// public class AbilityCooldownTextUI : MonoBehaviour
// {
//     public enum AbilityType { Warp, Steal, Stun, Jammer, BehindYou }
//     [Header("Pilih Ability untuk Text Ini")]
//     public AbilityType targetAbility;

//     private TextMeshProUGUI cooldownText;
//     private GameObject localPlayer;

//     void Start()
//     {
//         cooldownText = GetComponent<TextMeshProUGUI>();
//     }

//     void Update()
//     {
//         if (localPlayer == null)
//         {
//             FindLocalPlayer();
//             return;
//         }

//         if (cooldownText == null) return;

//         float remainingCooldown = 0f;

       
//         switch (targetAbility)
//         {
//             case AbilityType.Warp:
//                 AbilityWarp warp = localPlayer.GetComponent<AbilityWarp>();
//                 if (warp != null) remainingCooldown = warp.GetRemainingCooldown();
//                 break;
//             case AbilityType.Steal:
//                 AbilitySteal steal = localPlayer.GetComponent<AbilitySteal>();
//                 if (steal != null) remainingCooldown = steal.GetRemainingCooldown();
//                 break;
//         }

      
//         if (remainingCooldown > 0f)
//         {
//             cooldownText.text = Mathf.Ceil(remainingCooldown).ToString();
//         }
//         else
//         {
//             cooldownText.text = ""; 
//         }
//     }

//     private void FindLocalPlayer()
//     {
//         NetworkObject[] players = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
//         foreach (var player in players)
//         {
//             if (player.IsOwner && player.CompareTag("Player"))
//             {
//                 localPlayer = player.gameObject;
//                 break;
//             }
//         }
//     }
// }