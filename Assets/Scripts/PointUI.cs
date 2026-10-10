// using UnityEngine;
// using TMPro;
// using Unity.Netcode;
// using System.Collections;

// public class PointUI : MonoBehaviour
// {
//     [SerializeField] private TextMeshProUGUI pointText;

//     private PlayerPoints localPlayer;

//     private void Start()
//     {
//         StartCoroutine(WaitForPlayer());
//     }


//     private IEnumerator WaitForPlayer()
// {
//     while (localPlayer == null)
//     {
//         PlayerPoints[] players =
//             FindObjectsOfType<PlayerPoints>();

//         foreach (PlayerPoints p in players)
//         {
//             if (players.IsOwner)
//             {
//                 localPlayer = players;
//                 break;
//             }
//         }

//         yield return null;
//     }

//     UpdateUI(
//         localPlayer.Points.Value,
//         localPlayer.Points.Value
//      );

//     localPlayer.Points.OnValueChanged += UpdateUI;
// }

//     private void UpdateUI(int oldValue, int newValue)
//     {
//         pointText.text = "Point: " + newValue;
//     }

//     private void OnDestroy()
//     {
//         if (localPlayer != null)
//         {
//             localPlayer.Points.OnValueChanged -= UpdateUI;
//         }
//     }
// }