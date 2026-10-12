using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class PointUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pointText;

    private PlayerPoints localPlayer;

    private void Start()
    {
        StartCoroutine(WaitForPlayer());
    }

    private IEnumerator WaitForPlayer()
    {
    while (NetworkManager.Singleton.LocalClient?.PlayerObject == null)
        yield return null;

    localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerPoints>();

    UpdateUI(localPlayer.Points.Value,localPlayer.Points.Value);

    localPlayer.Points.OnValueChanged += UpdateUI;
    }

    private void UpdateUI(int oldValue, int newValue)
    {
        pointText.text = "Point: " + newValue;
    }

    private void OnDestroy()
    {
        if (localPlayer != null)
        {
            localPlayer.Points.OnValueChanged -= UpdateUI;
        }
    }
}