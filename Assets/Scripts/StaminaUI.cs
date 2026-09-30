using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color depletedColor = Color.red;

    [Tooltip("Below this % the bar turns red")]
    [SerializeField] private float lowStaminaThreshold = 0.25f;

    void Update()
    {
        if (player == null || fillImage == null) return;

        fillImage.fillAmount = player.StaminaPercent;
        fillImage.color = player.StaminaPercent < lowStaminaThreshold 
            ? depletedColor 
            : normalColor;
    }
}