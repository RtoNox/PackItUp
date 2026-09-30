using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color depletedColor = Color.red;

    [Header("Settings")]
    [Tooltip("Below this percentage the bar turns red")]
    [SerializeField] private float lowStaminaThreshold = 0.25f;

    private PlayerMovement player;

    void Update()
    {
        if (player == null)
        {
            FindLocalPlayer();

            if (player == null)
                return;
        }

        if (fillImage == null)
            return;

        float staminaPercent = player.StaminaPercent;

        fillImage.fillAmount = staminaPercent;

        if (staminaPercent < lowStaminaThreshold)
        {
            fillImage.color = depletedColor;
        }
        else
        {
            fillImage.color = normalColor;
        }
    }

    private void FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null)
            return;

        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

        foreach (PlayerMovement playerMovement in players)
        {
            NetworkObject networkObject =
                playerMovement.GetComponent<NetworkObject>();

            if (networkObject != null && networkObject.IsOwner)
            {
                player = playerMovement;
                break;
            }
        }
    }
}